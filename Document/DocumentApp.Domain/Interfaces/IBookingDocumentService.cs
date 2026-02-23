using DocumentApp.Domain.Entity;
using DocumentApp.Domain.Models;

namespace DocumentApp.Domain.Interfaces;

public interface IBookingDocumentService
{
    Task<Document> GenerateBookingDocumentAsync(BookingDocumentModel model);
    Task<Document> GenerateAndEmailBookingDocumentAsync(BookingDocumentModel model, string recipientEmail);
}
