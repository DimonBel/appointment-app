using DocumentApp.API.DTOs;
using DocumentApp.Domain.Entity;
using DocumentApp.Domain.Enums;
using DocumentApp.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DocumentApp.API.Endpoints;

public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/documents")
            .WithTags("Documents")
            .RequireAuthorization();

        // Upload document
        group.MapPost("/upload", UploadDocumentAsync)
            .Accepts<IFormFile>("multipart/form-data")
            .DisableAntiforgery();

        // Get document by ID
        group.MapGet("/{id:guid}", GetDocumentByIdAsync);

        // Download document
        group.MapGet("/{id:guid}/download", DownloadDocumentAsync);

        // Get all documents (admin/management)
        group.MapGet("/", GetAllDocumentsAsync)
            .RequireAuthorization("AdminOnly");

        // Get documents by owner
        group.MapGet("/owner/{ownerId:guid}", GetDocumentsByOwnerAsync);

        // Get documents by linked entity
        group.MapGet("/linked/{entityType}/{entityId:guid}", GetDocumentsByLinkedEntityAsync);

        // Delete document
        group.MapDelete("/{id:guid}", DeleteDocumentAsync);

        // Grant access to document
        group.MapPost("/{id:guid}/access", GrantAccessAsync);

        // Revoke access from document
        group.MapDelete("/{id:guid}/access/{userId:guid}", RevokeAccessAsync);

        // Get presigned URL for document
        group.MapGet("/{id:guid}/presigned-url", GetPresignedUrlAsync);

        // Update document's linked entity
        group.MapPatch("/{id:guid}/linked-entity", UpdateLinkedEntityAsync);
    }

    private static async Task<IResult> UploadDocumentAsync(
        [FromForm] IFormFile file,
        [FromForm] DocumentType documentType,
        [FromForm] LinkedEntityType? linkedEntityType,
        [FromForm] Guid? linkedEntityId,
        IDocumentService documentService,
        ClaimsPrincipal user,
        HttpContext context)
    {
        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var ownerId))
            {
                return Results.Unauthorized();
            }

            var userName = user.FindFirst(ClaimTypes.Name)?.Value ?? user.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

            using var stream = file.OpenReadStream();
            var document = await documentService.UploadDocumentAsync(
                stream,
                file.FileName,
                file.ContentType,
                file.Length,
                ownerId,
                userName,
                documentType,
                linkedEntityType ?? LinkedEntityType.None,
                linkedEntityId);

            var response = MapToResponseDto(document, context);
            return Results.Created($"/api/documents/{document.Id}", response);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }

    private static async Task<IResult> GetDocumentByIdAsync(
        Guid id,
        IDocumentService documentService,
        ClaimsPrincipal user,
        HttpContext context)
    {
        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Results.Unauthorized();
            }

            var document = await documentService.GetDocumentByIdAsync(id);
            if (document == null)
            {
                return Results.NotFound();
            }

            // Check access
            var hasAccess = await documentService.HasAccessAsync(id, userGuid, AccessControlType.View);
            if (!hasAccess && !user.IsInRole("Admin") && !user.IsInRole("Management"))
            {
                return Results.Forbid();
            }

            var response = MapToResponseDto(document, context);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }

    private static async Task<IResult> DownloadDocumentAsync(
        Guid id,
        IDocumentService documentService,
        ClaimsPrincipal user)
    {
        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Results.Unauthorized();
            }

            var document = await documentService.GetDocumentByIdAsync(id);
            if (document == null)
            {
                return Results.NotFound();
            }

            // Check access - admins and management can download any document
            var hasAccess = await documentService.HasAccessAsync(id, userGuid, AccessControlType.Download);
            if (!hasAccess && !user.IsInRole("Admin") && !user.IsInRole("Management"))
            {
                return Results.Forbid();
            }

            var bypassAccessControl = user.IsInRole("Admin") || user.IsInRole("Management");
            var stream = await documentService.DownloadDocumentAsync(id, userGuid, bypassAccessControl);
            return Results.File(stream, document.ContentType, document.OriginalFileName);
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (FileNotFoundException)
        {
            return Results.NotFound();
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }

    private static async Task<IResult> GetAllDocumentsAsync(
        IDocumentService documentService,
        HttpContext context,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] DocumentType? documentType = null)
    {
        try
        {
            var documents = await documentService.GetAllDocumentsAsync(page, pageSize, documentType);
            var response = documents.Select(d => MapToResponseDto(d, context));
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }

    private static async Task<IResult> GetDocumentsByOwnerAsync(
        Guid ownerId,
        IDocumentService documentService,
        ClaimsPrincipal user,
        HttpContext context,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var currentUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var userGuid))
            {
                return Results.Unauthorized();
            }

            // Users can only view their own documents unless they are admin
            if (ownerId != userGuid && !user.IsInRole("Admin") && !user.IsInRole("Management"))
            {
                return Results.Forbid();
            }

            var documents = await documentService.GetDocumentsByOwnerAsync(ownerId, page, pageSize);
            var response = documents.Select(d => MapToResponseDto(d, context));
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }

    private static async Task<IResult> GetDocumentsByLinkedEntityAsync(
        string entityType,
        Guid entityId,
        IDocumentService documentService,
        ClaimsPrincipal user,
        HttpContext context)
    {
        try
        {
            if (!Enum.TryParse<LinkedEntityType>(entityType, true, out var linkedEntityType))
            {
                return Results.BadRequest("Invalid entity type");
            }

            var documents = await documentService.GetDocumentsByLinkedEntityAsync(linkedEntityType, entityId);
            var response = documents.Select(d => MapToResponseDto(d, context));
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }

    private static async Task<IResult> DeleteDocumentAsync(
        Guid id,
        IDocumentService documentService,
        ClaimsPrincipal user)
    {
        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Results.Unauthorized();
            }

            var document = await documentService.GetDocumentByIdAsync(id);
            if (document == null)
            {
                return Results.NotFound();
            }

            // Check if user is owner or admin/management
            var bypassOwnershipCheck = user.IsInRole("Admin") || user.IsInRole("Management");

            var result = await documentService.DeleteDocumentAsync(id, userGuid, bypassOwnershipCheck);
            if (!result)
            {
                return Results.NotFound();
            }

            return Results.NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }

    private static async Task<IResult> GrantAccessAsync(
        Guid id,
        [FromBody] GrantAccessDto dto,
        IDocumentService documentService,
        ClaimsPrincipal user)
    {
        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Results.Unauthorized();
            }

            var result = await documentService.GrantAccessAsync(id, dto.UserId, dto.AccessType, userGuid);
            if (!result)
            {
                return Results.NotFound();
            }

            return Results.Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Forbid();
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }

    private static async Task<IResult> RevokeAccessAsync(
        Guid id,
        Guid userId,
        IDocumentService documentService,
        ClaimsPrincipal user)
    {
        try
        {
            var currentUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId) || !Guid.TryParse(currentUserId, out var userGuid))
            {
                return Results.Unauthorized();
            }

            var result = await documentService.RevokeAccessAsync(id, userId);
            if (!result)
            {
                return Results.NotFound();
            }

            return Results.NoContent();
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }

    private static async Task<IResult> GetPresignedUrlAsync(
        Guid id,
        IDocumentService documentService,
        ClaimsPrincipal user)
    {
        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Results.Unauthorized();
            }

            var document = await documentService.GetDocumentByIdAsync(id);
            if (document == null)
            {
                return Results.NotFound();
            }

            // Check access
            var hasAccess = await documentService.HasAccessAsync(id, userGuid, AccessControlType.View);
            if (!hasAccess && !user.IsInRole("Admin") && !user.IsInRole("Management"))
            {
                return Results.Forbid();
            }

            // Return a download URL (in production, use presigned URL from MinIO)
            return Results.Ok(new { DownloadUrl = $"/api/documents/{id}/download" });
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }

    private static async Task<IResult> UpdateLinkedEntityAsync(
        Guid id,
        [FromBody] UpdateLinkedEntityDto dto,
        IDocumentService documentService,
        ClaimsPrincipal user,
        HttpContext context)
    {
        try
        {
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Results.Unauthorized();
            }

            var document = await documentService.GetDocumentByIdAsync(id);
            if (document == null)
            {
                return Results.NotFound();
            }

            // Only the owner can update the linked entity
            if (document.OwnerId != userGuid && !user.IsInRole("Admin") && !user.IsInRole("Management"))
            {
                return Results.Forbid();
            }

            await documentService.UpdateLinkedEntityAsync(id, dto.LinkedEntityType, dto.LinkedEntityId);

            var updatedDocument = await documentService.GetDocumentByIdAsync(id);
            var response = MapToResponseDto(updatedDocument, context);
            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message, statusCode: 500);
        }
    }

    private static DocumentResponseDto MapToResponseDto(Document document, HttpContext context)
    {
        var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
        return new DocumentResponseDto
        {
            Id = document.Id,
            FileName = document.FileName,
            OriginalFileName = document.OriginalFileName,
            ContentType = document.ContentType,
            FileSize = document.FileSize,
            DocumentType = document.DocumentType,
            LinkedEntityType = document.LinkedEntityType,
            LinkedEntityId = document.LinkedEntityId,
            OwnerId = document.OwnerId,
            OwnerName = document.OwnerName,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt,
            DownloadUrl = $"{baseUrl}/api/documents/{document.Id}/download"
        };
    }
}