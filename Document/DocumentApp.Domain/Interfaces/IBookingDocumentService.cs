using DocumentApp.Domain.Entity;
using DocumentApp.Domain.Models;

namespace DocumentApp.Domain.Interfaces;

/// <summary>
/// Service interface for generating booking confirmation documents
/// Creates PDF documents for appointment bookings and manages email delivery
/// </summary>
public interface IBookingDocumentService
{
    /// <summary>
    /// Generates a PDF booking confirmation document for an order
    /// Returns existing document if already generated for this order
    /// </summary>
    /// <param name="model">Booking details including order ID, patient, doctor, appointment info</param>
    /// <returns>Generated or existing stored document with MinIO download URL</returns>
    Task<Document> GenerateBookingDocumentAsync(BookingDocumentModel model);

    /// <summary>
    /// Generates a PDF booking confirmation and emails it to the recipient
    /// Combines document generation and email delivery in one operation
    /// </summary>
    /// <param name="model">Booking details for document generation</param>
    /// <param name="recipientEmail">Email address to send the document to</param>
    /// <returns>Generated and emailed stored document</returns>
    Task<Document> GenerateAndEmailBookingDocumentAsync(BookingDocumentModel model, string recipientEmail);
}