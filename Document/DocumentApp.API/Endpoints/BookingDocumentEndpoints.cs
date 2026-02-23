using DocumentApp.API.DTOs;
using DocumentApp.Domain.Interfaces;

namespace DocumentApp.API.Endpoints;

public static class BookingDocumentEndpoints
{
    public static void MapBookingDocumentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/documents/bookings/internal")
            .WithTags("Booking Documents Internal");

        group.MapPost("/generate", GenerateBookingDocumentAsync);
        group.MapPost("/send-confirmation-email", SendBookingDocumentEmailAsync);
    }

    private static async Task<IResult> GenerateBookingDocumentAsync(
        GenerateBookingDocumentDto dto,
        IBookingDocumentService bookingDocumentService,
        HttpContext context,
        IConfiguration configuration)
    {
        if (!IsInternalRequest(context, configuration))
        {
            return Results.Unauthorized();
        }

        try
        {
            var document = await bookingDocumentService.GenerateBookingDocumentAsync(dto.ToModel());
            var response = new BookingDocumentResponseDto
            {
                DocumentId = document.Id,
                FileName = document.OriginalFileName,
                DownloadUrl = $"/api/documents/{document.Id}/download"
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }

    private static async Task<IResult> SendBookingDocumentEmailAsync(
        SendBookingDocumentEmailDto dto,
        IBookingDocumentService bookingDocumentService,
        HttpContext context,
        IConfiguration configuration)
    {
        if (!IsInternalRequest(context, configuration))
        {
            return Results.Unauthorized();
        }

        try
        {
            var document = await bookingDocumentService.GenerateAndEmailBookingDocumentAsync(dto.Booking.ToModel(), dto.RecipientEmail);
            var response = new BookingDocumentResponseDto
            {
                DocumentId = document.Id,
                FileName = document.OriginalFileName,
                DownloadUrl = $"/api/documents/{document.Id}/download"
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }

    private static bool IsInternalRequest(HttpContext context, IConfiguration configuration)
    {
        var expected = configuration["InternalServiceKey"] ?? "internal-dev-key";
        var provided = context.Request.Headers["X-Internal-Key"].FirstOrDefault();

        return !string.IsNullOrWhiteSpace(provided)
            && string.Equals(expected, provided, StringComparison.Ordinal);
    }
}
