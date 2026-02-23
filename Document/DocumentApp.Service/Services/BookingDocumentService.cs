using DocumentApp.Domain.Entity;
using DocumentApp.Domain.Enums;
using DocumentApp.Domain.Interfaces;
using DocumentApp.Domain.Models;
using DocumentApp.Repository.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using StoredDocument = DocumentApp.Domain.Entity.Document;

namespace DocumentApp.Service.Services;

public class BookingDocumentService : IBookingDocumentService
{
    private readonly IDocumentService _documentService;
    private readonly IDocumentRepository _documentRepository;
    private readonly IMinioDocumentStorageService _storageService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BookingDocumentService> _logger;

    public BookingDocumentService(
        IDocumentService documentService,
        IDocumentRepository documentRepository,
        IMinioDocumentStorageService storageService,
        IConfiguration configuration,
        ILogger<BookingDocumentService> logger)
    {
        _documentService = documentService;
        _documentRepository = documentRepository;
        _storageService = storageService;
        _configuration = configuration;
        _logger = logger;

        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<StoredDocument> GenerateBookingDocumentAsync(BookingDocumentModel model)
    {
        var normalizedModel = EnsureDefaults(model);
        var existing = await TryGetExistingBookingDocumentAsync(normalizedModel.OrderId);
        if (existing != null)
        {
            return existing;
        }

        var pdfBytes = GeneratePdf(normalizedModel);
        var fileName = $"booking-confirmation-{normalizedModel.OrderId:N}.pdf";

        using var stream = new MemoryStream(pdfBytes);
        var createdDocument = await _documentService.UploadDocumentAsync(
            stream,
            fileName,
            "application/pdf",
            pdfBytes.LongLength,
            normalizedModel.ClientId,
            normalizedModel.PatientName,
            DocumentType.BookingConfirmation,
            LinkedEntityType.Order,
            normalizedModel.OrderId);

        if (normalizedModel.DoctorId.HasValue && normalizedModel.DoctorId.Value != normalizedModel.ClientId)
        {
            try
            {
                await _documentService.GrantAccessAsync(
                    createdDocument.Id,
                    normalizedModel.DoctorId.Value,
                    AccessControlType.Full,
                    normalizedModel.ClientId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to grant doctor access to booking document {DocumentId}", createdDocument.Id);
            }
        }

        return createdDocument;
    }

    public async Task<StoredDocument> GenerateAndEmailBookingDocumentAsync(BookingDocumentModel model, string recipientEmail)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            throw new ArgumentException("Recipient email is required.", nameof(recipientEmail));
        }

        var document = await GenerateBookingDocumentAsync(model);
        var fileBytes = await DownloadDocumentBytesAsync(document);

        var subject = "Your booking confirmation document";
        var body = BuildEmailBody(model);

        var sent = await SendEmailWithAttachmentAsync(recipientEmail, subject, body, document.OriginalFileName, fileBytes);
        if (!sent)
        {
            throw new InvalidOperationException("Failed to send booking confirmation email.");
        }

        return document;
    }

    private async Task<StoredDocument?> TryGetExistingBookingDocumentAsync(Guid orderId)
    {
        var linkedDocuments = await _documentRepository.GetByLinkedEntityAsync(LinkedEntityType.Order, orderId);

        return linkedDocuments
            .Where(x => !x.IsDeleted && x.DocumentType == DocumentType.BookingConfirmation)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();
    }

    private async Task<byte[]> DownloadDocumentBytesAsync(StoredDocument document)
    {
        await using var stream = await _storageService.DownloadFileAsync(document.MinioPath, document.MinioBucket);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    private static BookingDocumentModel EnsureDefaults(BookingDocumentModel model)
    {
        if (model.LineItems.Count == 0)
        {
            model.LineItems.Add(new BookingDocumentLineItem
            {
                Quantity = 1,
                Description = $"Consultation with {model.DoctorName}",
                UnitPrice = 100m
            });
        }

        if (string.IsNullOrWhiteSpace(model.BookingNumber))
        {
            model.BookingNumber = model.OrderId.ToString("N")[..8].ToUpperInvariant();
        }

        if (model.BookingDateUtc == default)
        {
            model.BookingDateUtc = DateTime.UtcNow;
        }

        if (string.IsNullOrWhiteSpace(model.Status))
        {
            model.Status = "Pending";
        }

        if (string.IsNullOrWhiteSpace(model.AdditionalInformation))
        {
            model.AdditionalInformation = "Please arrive 10 minutes before your scheduled appointment.";
        }

        return model;
    }

    private static byte[] GeneratePdf(BookingDocumentModel model)
    {
        return QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Content().Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Text(model.FacilityName)
                        .Bold()
                        .FontSize(30)
                        .FontColor("#1B4E8C")
                        .AlignCenter();

                    column.Item().AlignCenter().Text(text =>
                    {
                        text.Span($"{model.FacilityAddress}   ");
                        text.Span($"{model.FacilityPhone}   ");
                        text.Span(model.FacilityEmail);
                    });

                    if (!string.IsNullOrWhiteSpace(model.FacilityWebsite))
                    {
                        column.Item().AlignCenter().Text(model.FacilityWebsite).FontColor("#6B7280");
                    }

                    column.Item().PaddingVertical(6).LineHorizontal(1).LineColor("#D1D5DB");

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Spacing(3);
                            left.Item().Text("Booking Details").Bold().FontColor("#1B4E8C");
                            left.Item().Text($"Date: {model.ScheduledDateTimeUtc:dddd, MMMM dd, yyyy}");
                            left.Item().Text($"Time: {model.ScheduledDateTimeUtc:HH:mm} UTC");
                            left.Item().Text($"Duration: {model.DurationMinutes} minutes");
                            left.Item().Text($"Doctor: {model.DoctorName}");
                        });

                        row.RelativeItem().AlignRight().Column(right =>
                        {
                            right.Item().Text("BOOKING").Bold().FontSize(28).FontColor("#1B4E8C");
                        });
                    });

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Spacing(2);
                            left.Item().Text("Booked By").Bold().FontColor("#1B4E8C");
                            left.Item().Text(model.PatientName);
                            left.Item().Text(model.PatientEmail);
                        });

                        row.RelativeItem().AlignRight().Column(right =>
                        {
                            right.Spacing(2);
                            right.Item().Text($"Booking #: {model.BookingNumber}").Bold();
                            right.Item().Text($"Booking Date: {model.BookingDateUtc:dd-MM-yyyy}");
                            right.Item().Text($"Status: {model.Status}").Bold().FontColor("#1B4E8C");
                        });
                    });

                    column.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(70);
                            columns.RelativeColumn();
                            columns.ConstantColumn(110);
                            columns.ConstantColumn(110);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCellStyle).Text("Quantity").FontColor(Colors.White).Bold();
                            header.Cell().Element(HeaderCellStyle).Text("Description").FontColor(Colors.White).Bold();
                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("Unit Price").FontColor(Colors.White).Bold();
                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("Amount").FontColor(Colors.White).Bold();
                        });

                        foreach (var item in model.LineItems)
                        {
                            table.Cell().Element(BodyCellStyle).Text(item.Quantity.ToString("0.00", CultureInfo.InvariantCulture));
                            table.Cell().Element(BodyCellStyle).Text(item.Description);
                            table.Cell().Element(BodyCellStyle).AlignRight().Text(FormatMoney(item.UnitPrice));
                            table.Cell().Element(BodyCellStyle).AlignRight().Text(FormatMoney(item.Amount));
                        }

                        AddSummaryRow(table, "Subtotal", model.Subtotal);
                        AddSummaryRow(table, $"Tax ({model.TaxRate * 100m:0.##}%)", model.TaxAmount);
                        AddSummaryRow(table, "Total", model.TotalAmount, true);
                    });

                    column.Item().PaddingTop(8).Column(info =>
                    {
                        info.Spacing(3);
                        info.Item().Text("Additional Information").Bold().FontColor("#1B4E8C");
                        info.Item().Text(model.AdditionalInformation);
                    });
                });
            });
        }).GeneratePdf();

        static IContainer HeaderCellStyle(IContainer container)
        {
            return container
                .Background("#1B4E8C")
                .Border(1)
                .BorderColor("#1B4E8C")
                .PaddingVertical(6)
                .PaddingHorizontal(8);
        }

        static IContainer BodyCellStyle(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor("#D1D5DB")
                .PaddingVertical(6)
                .PaddingHorizontal(8);
        }

        static void AddSummaryRow(TableDescriptor table, string label, decimal value, bool isTotal = false)
        {
            var labelStyle = isTotal ? "#1B4E8C" : "#111827";

            table.Cell().ColumnSpan(2).Element(BodyCellStyle).Text(string.Empty);
            table.Cell().Element(BodyCellStyle).AlignRight().Text(label).Bold().FontColor(labelStyle);
            table.Cell().Element(BodyCellStyle).AlignRight().Text(FormatMoney(value)).Bold().FontColor(labelStyle);
        }

        static string FormatMoney(decimal value)
        {
            return value.ToString("$0.00", CultureInfo.InvariantCulture);
        }
    }

    private async Task<bool> SendEmailWithAttachmentAsync(
        string toEmail,
        string subject,
        string htmlBody,
        string attachmentName,
        byte[] attachmentBytes)
    {
        try
        {
            var smtpSettings = _configuration.GetSection("Smtp");
            var host = smtpSettings["Host"] ?? throw new InvalidOperationException("SMTP Host not configured");
            var port = int.Parse(smtpSettings["Port"] ?? "587");
            var username = smtpSettings["Username"] ?? throw new InvalidOperationException("SMTP Username not configured");
            var password = smtpSettings["Password"] ?? throw new InvalidOperationException("SMTP Password not configured");
            var fromEmail = smtpSettings["FromEmail"] ?? username;
            var fromName = smtpSettings["FromName"] ?? "Healthcare Hub";
            var useSsl = bool.Parse(smtpSettings["UseSsl"] ?? "false");
            var useStartTls = bool.Parse(smtpSettings["UseStartTls"] ?? "true");
            var secureSocketRaw = smtpSettings["SecureSocket"];

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = htmlBody,
                TextBody = "Your booking confirmation document is attached as PDF."
            };
            builder.Attachments.Add(attachmentName, attachmentBytes, ContentType.Parse("application/pdf"));

            message.Body = builder.ToMessageBody();

            SecureSocketOptions socketOptions;
            if (!string.IsNullOrWhiteSpace(secureSocketRaw)
                && Enum.TryParse<SecureSocketOptions>(secureSocketRaw, true, out var parsedOption))
            {
                socketOptions = parsedOption;
            }
            else if (useSsl)
            {
                socketOptions = SecureSocketOptions.SslOnConnect;
            }
            else if (useStartTls)
            {
                socketOptions = SecureSocketOptions.StartTls;
            }
            else
            {
                socketOptions = SecureSocketOptions.Auto;
            }

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(host, port, socketOptions);
            await smtp.AuthenticateAsync(username, password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("Booking PDF email sent to {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send booking PDF email to {Email}", toEmail);
            return false;
        }
    }

    private static string BuildEmailBody(BookingDocumentModel model)
    {
        return $@"
<h2>Booking Confirmed</h2>
<p>Hello {model.PatientName},</p>
<p>Your booking is confirmed. Your PDF confirmation document is attached to this email.</p>
<ul>
  <li><strong>Booking #:</strong> {model.BookingNumber}</li>
  <li><strong>Doctor:</strong> {model.DoctorName}</li>
  <li><strong>Date:</strong> {model.ScheduledDateTimeUtc:yyyy-MM-dd}</li>
  <li><strong>Time:</strong> {model.ScheduledDateTimeUtc:HH:mm} UTC</li>
</ul>
<p>Thank you for using Healthcare Hub.</p>";
    }
}
