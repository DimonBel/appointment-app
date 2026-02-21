using AutomationApp.API.DTOs;
using AutomationApp.API.Hubs;
using AutomationApp.Domain.Entity;
using AutomationApp.Domain.Enums;
using AutomationApp.Domain.Interfaces;
using AutomationApp.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace AutomationApp.API.Endpoints;

public static class AutomationEndpoints
{
    public static void MapAutomationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/automation")
            .RequireAuthorization();

        // Conversation endpoints
        group.MapPost("/conversations/start", StartConversationAsync)
            .WithName("StartConversation")
            .WithOpenApi()
            .WithSummary("Start a new AI conversation (returns existing if active)");

        group.MapPost("/conversations/new", CreateNewConversationAsync)
            .WithName("CreateNewConversation")
            .WithOpenApi()
            .WithSummary("Create a brand new conversation (ignores existing active)");

        group.MapGet("/conversations/active", GetActiveConversationAsync)
            .WithName("GetActiveConversation")
            .WithOpenApi()
            .WithSummary("Get active conversation for current user");

        group.MapGet("/conversations/{id}/messages", GetConversationMessagesAsync)
            .WithName("GetConversationMessages")
            .WithOpenApi()
            .WithSummary("Get all messages in a conversation");

        group.MapPost("/conversations/send", SendMessageAsync)
            .WithName("SendMessage")
            .WithOpenApi()
            .WithSummary("Send a message to the AI assistant");

        // Booking endpoints
        group.MapGet("/booking/draft/{conversationId}", GetBookingDraftAsync)
            .WithName("GetBookingDraft")
            .WithOpenApi()
            .WithSummary("Get booking draft for a conversation");

        group.MapPost("/booking/submit", SubmitBookingAsync)
            .WithName("SubmitBooking")
            .WithOpenApi()
            .WithSummary("Submit a booking draft");

        group.MapPost("/booking/cancel/{draftId}", CancelBookingDraftAsync)
            .WithName("CancelBookingDraft")
            .WithOpenApi()
            .WithSummary("Cancel a booking draft");

        // Quick actions
        group.MapGet("/options", GetBookingOptionsAsync)
            .WithName("GetBookingOptions")
            .WithOpenApi()
            .WithSummary("Get quick booking options");

        // Webhook endpoint for booking status updates from Appointment Service
        group.MapPost("/webhook/booking-status", UpdateBookingStatusAsync)
            .AllowAnonymous()
            .WithName("UpdateBookingStatus")
            .WithOpenApi()
            .WithSummary("Webhook to receive booking status updates");
    }

    private static async Task<IResult> StartConversationAsync(
        HttpContext httpContext,
        IConversationService conversationService,
        ILLMService llmService)
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Results.Unauthorized();
        }

        // Check for existing active conversation
        var existingConversation = await conversationService.GetActiveConversationByUserIdAsync(userGuid);
        if (existingConversation != null)
        {
            var conversationDto = MapToConversationDTO(existingConversation);
            return Results.Ok(conversationDto);
        }

        // Create new conversation
        var conversation = await conversationService.CreateConversationAsync(userGuid);
        
        // Generate AI greeting
        var greeting = await llmService.GenerateGreetingAsync(userGuid);
        
        // Add greeting as first message
        await conversationService.AddMessageAsync(conversation.Id, greeting, false);

        var dto = MapToConversationDTO(conversation);
        return Results.Created($"/api/automation/conversations/{conversation.Id}", dto);
    }

    private static async Task<IResult> CreateNewConversationAsync(
        HttpContext httpContext,
        IConversationService conversationService,
        ILLMService llmService)
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Results.Unauthorized();
        }

        // Create new conversation without checking for existing ones
        var conversation = await conversationService.CreateConversationAsync(userGuid);
        
        // Generate AI greeting
        var greeting = await llmService.GenerateGreetingAsync(userGuid);
        
        // Add greeting as first message
        await conversationService.AddMessageAsync(conversation.Id, greeting, false);

        var dto = MapToConversationDTO(conversation);
        return Results.Created($"/api/automation/conversations/{conversation.Id}", dto);
    }

    private static async Task<IResult> GetActiveConversationAsync(
        HttpContext httpContext,
        IConversationService conversationService)
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Results.Unauthorized();
        }

        var conversation = await conversationService.GetActiveConversationByUserIdAsync(userGuid);
        if (conversation == null)
        {
            return Results.NotFound(new { message = "No active conversation found" });
        }

        var dto = MapToConversationDTO(conversation);
        return Results.Ok(dto);
    }

    private static async Task<IResult> GetConversationMessagesAsync(
        Guid id,
        IConversationService conversationService,
        HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Results.Unauthorized();
        }

        var conversation = await conversationService.GetConversationByIdAsync(id);
        if (conversation == null || conversation.UserId != userGuid)
        {
            return Results.NotFound();
        }

        var messages = await conversationService.GetConversationMessagesAsync(id);
        var messageDtos = messages.Select(MapToMessageDTO).ToList();
        return Results.Ok(messageDtos);
    }

    private static async Task<IResult> SendMessageAsync(
        SendMessageRequest request,
        HttpContext httpContext,
        IConversationService conversationService,
        ILLMService llmService,
        IBookingAutomationService bookingService,
        IDataCollectionService dataCollectionService,
        IHubContext<AutomationHub> hubContext)
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Results.Unauthorized();
        }

        Conversation? conversation;

        // Use existing conversation or create new one
        if (request.ConversationId.HasValue)
        {
            conversation = await conversationService.GetConversationByIdAsync(request.ConversationId.Value);
            if (conversation == null || conversation.UserId != userGuid)
            {
                return Results.NotFound(new { message = "Conversation not found" });
            }
        }
        else
        {
            conversation = await conversationService.GetActiveConversationByUserIdAsync(userGuid);
            if (conversation == null)
            {
                conversation = await conversationService.CreateConversationAsync(userGuid);
            }
        }

        // Send typing indicator via SignalR
        await hubContext.Clients.Group($"conversation-{conversation.Id}").SendAsync("TypingIndicator", true);

        // Add user message
        var userMessage = await conversationService.AddMessageAsync(conversation.Id, request.Message, true, null, request.SelectedOption);

        // Get or create booking draft
        var bookingDraft = await bookingService.GetBookingDraftByConversationIdAsync(conversation.Id);
        if (bookingDraft == null && conversation.State == ConversationState.CollectingInfo)
        {
            bookingDraft = await bookingService.CreateBookingDraftAsync(conversation.Id, userGuid);
        }

        // Fetch available professionals
        var availableProfessionals = await bookingService.GetAvailableProfessionalsAsync();
        
        // Fetch domain configurations (service types)
        var domainConfigurations = await bookingService.GetDomainConfigurationsAsync();

        // Process with LLM
        var llmResponse = await llmService.ProcessUserMessageAsync(
            conversation.Id,
            request.Message,
            conversation.State,
            conversation.ContextData,
            availableProfessionals,
            domainConfigurations);

        // Update conversation state
        if (llmResponse.SuggestedNextState.HasValue)
        {
            await conversationService.UpdateConversationStateAsync(conversation.Id, llmResponse.SuggestedNextState.Value);
            
            // Broadcast state change via SignalR
            await hubContext.Clients.Group($"conversation-{conversation.Id}").SendAsync("ConversationStateChanged", llmResponse.SuggestedNextState.ToString());
        }

        // Update context with extracted data
        if (llmResponse.ExtractedData != null && llmResponse.ExtractedData.Count > 0)
        {
            var currentContext = conversation.ContextData ?? new Dictionary<string, object>();
            foreach (var kvp in llmResponse.ExtractedData)
            {
                currentContext[kvp.Key] = kvp.Value;
            }
            await conversationService.UpdateConversationContextAsync(conversation.Id, currentContext);

            // Update booking draft if data is relevant
            if (bookingDraft != null)
            {
                await UpdateBookingDraftFromExtractedData(bookingService, bookingDraft.Id, llmResponse.ExtractedData);
            }
        }

        // Add AI response message
        var aiMessage = await conversationService.AddMessageAsync(
            conversation.Id,
            llmResponse.ResponseText,
            false,
            llmResponse.SuggestedOptions);

        // Send typing indicator off via SignalR
        await hubContext.Clients.Group($"conversation-{conversation.Id}").SendAsync("TypingIndicator", false);

        // Broadcast AI response via SignalR
        await hubContext.Clients.Group($"conversation-{conversation.Id}").SendAsync("ReceiveMessage", new
        {
            message = new
            {
                id = aiMessage.Id,
                conversationId = conversation.Id,
                content = llmResponse.ResponseText,
                isFromUser = false,
                sentAt = aiMessage.SentAt,
                suggestedOptions = llmResponse.SuggestedOptions
            },
            currentState = llmResponse.SuggestedNextState?.ToString() ?? conversation.State.ToString(),
            extractedData = llmResponse.ExtractedData
        });

        // Check if booking should be submitted
        bool isBookingComplete = false;
        Guid? finalOrderId = null;
        if (llmResponse.SuggestedNextState == ConversationState.BookingComplete && bookingDraft != null)
        {
            var submittedDraft = await bookingService.SubmitBookingDraftAsync(bookingDraft.Id);
            isBookingComplete = true;
            finalOrderId = submittedDraft.FinalOrderId;

            // Send notification to the professional (doctor)
            if (finalOrderId.HasValue)
            {
                await SendNotificationToProfessionalAsync(bookingDraft.ProfessionalId, bookingDraft.UserId, finalOrderId.Value);
            }
        }

        var response = new SendMessageResponse
        {
            ConversationId = conversation.Id,
            MessageId = aiMessage.Id,
            ResponseText = llmResponse.ResponseText,
            SuggestedOptions = llmResponse.SuggestedOptions,
            CurrentState = llmResponse.SuggestedNextState?.ToString() ?? conversation.State.ToString(),
            IsBookingComplete = isBookingComplete,
            OrderId = finalOrderId
        };

        return Results.Ok(response);
    }

    private static async Task<IResult> GetBookingDraftAsync(
        Guid conversationId,
        IBookingAutomationService bookingService,
        HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Results.Unauthorized();
        }

        var draft = await bookingService.GetBookingDraftByConversationIdAsync(conversationId);
        if (draft == null)
        {
            return Results.NotFound(new { message = "No booking draft found for this conversation" });
        }

        var dto = MapToBookingDraftDTO(draft);
        return Results.Ok(dto);
    }

    private static async Task<IResult> SubmitBookingAsync(
        SubmitBookingRequest request,
        IBookingAutomationService bookingService,
        HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Results.Unauthorized();
        }

        var draft = await bookingService.GetBookingDraftByConversationIdAsync(request.ConversationId);
        if (draft == null || draft.UserId != userGuid)
        {
            return Results.NotFound(new { message = "Booking draft not found" });
        }

        var updatedDraft = await bookingService.SubmitBookingDraftAsync(draft.Id);
        var dto = MapToBookingDraftDTO(updatedDraft);
        return Results.Ok(dto);
    }

    private static async Task<IResult> CancelBookingDraftAsync(
        Guid draftId,
        IBookingAutomationService bookingService,
        HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Results.Unauthorized();
        }

        var draft = await bookingService.GetBookingDraftAsync(draftId);
        if (draft == null || draft.UserId != userGuid)
        {
            return Results.NotFound(new { message = "Booking draft not found" });
        }

        var cancelledDraft = await bookingService.CancelBookingDraftAsync(draftId);
        var dto = MapToBookingDraftDTO(cancelledDraft);
        return Results.Ok(dto);
    }

    private static async Task<IResult> GetBookingOptionsAsync(
        ILLMService llmService)
    {
        var options = await llmService.GenerateBookingOptionsAsync();
        return Results.Ok(new { options });
    }

    private static async Task<IResult> UpdateBookingStatusAsync(
        BookingStatusWebhookDto webhookData,
        IConversationService conversationService,
        IBookingAutomationService bookingService)
    {
        try
        {
            // Find booking draft associated with this order
            var bookingDraft = await bookingService.GetBookingDraftByConversationIdAsync(webhookData.OrderId);
            if (bookingDraft == null)
            {
                // Try to find by final order ID
                // Note: This might need additional repository method to search by FinalOrderId
                return Results.Ok(new { message = "No booking draft found for this order" });
            }

            // Update the booking draft status
            var statusMessage = webhookData.Status switch
            {
                "Confirmed" => "Your booking has been confirmed by the doctor!",
                "Rejected" => "Your booking request was rejected by the doctor. Please try booking with another professional.",
                "Completed" => "Your appointment has been completed. Thank you for using our service!",
                "Cancelled" => "Your booking has been cancelled.",
                _ => $"Your booking status has been updated to: {webhookData.Status}"
            };

            // Add a message to the conversation about the status update
            var suggestedOptions = new List<string>();
            if (webhookData.Status == "Confirmed")
            {
                suggestedOptions.Add("View appointment details");
                suggestedOptions.Add("Book another appointment");
            }
            else if (webhookData.Status == "Rejected")
            {
                suggestedOptions.Add("Book with another doctor");
                suggestedOptions.Add("View available doctors");
            }

            await conversationService.AddMessageAsync(
                bookingDraft.ConversationId,
                statusMessage,
                false,
                suggestedOptions);

            // Update conversation state based on booking status
            var newState = webhookData.Status switch
            {
                "Confirmed" => ConversationState.BookingComplete,
                "Rejected" => ConversationState.SelectingProfessional,
                "Completed" => ConversationState.BookingComplete,
                "Cancelled" => ConversationState.Greeting,
                _ => ConversationState.Error
            };

            await conversationService.UpdateConversationStateAsync(bookingDraft.ConversationId, newState);

            return Results.Ok(new { message = "Booking status updated", status = webhookData.Status });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task SendNotificationToProfessionalAsync(
        Guid? professionalId,
        Guid clientId,
        Guid orderId,
        HttpClient? httpClient = null)
    {
        try
        {
            if (!professionalId.HasValue)
                return;

            // In a real implementation, you would:
            // 1. Get the notification service URL from configuration
            // 2. Send a notification to the professional
            // 3. Include order details, client info, and timestamp

            var notificationPayload = new
            {
                recipientId = professionalId.Value,
                type = "NewAppointment",
                title = "New Appointment Request",
                message = $"You have a new appointment request. Order ID: {orderId}",
                orderId = orderId,
                clientId = clientId,
                createdAt = DateTime.UtcNow
            };

            // Log for now - implement actual notification service call
            Console.WriteLine($"[Notification] Would send notification to professional {professionalId.Value}");
            Console.WriteLine($"[Notification] New appointment request - Order ID: {orderId}");

            // Example implementation (requires Notification service URL in configuration):
            // if (httpClient != null)
            // {
            //     var notificationServiceUrl = configuration["NotificationService:BaseUrl"];
            //     var response = await httpClient.PostAsJsonAsync($"{notificationServiceUrl}/api/notifications", notificationPayload);
            //     response.EnsureSuccessStatusCode();
            // }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending notification to professional: {ex.Message}");
        }
    }

    private static async Task UpdateBookingDraftFromExtractedData(
        IBookingAutomationService bookingService,
        Guid draftId,
        Dictionary<string, object> extractedData)
    {
        Guid? professionalId = null;
        string? serviceType = null;
        DateTime? preferredDateTime = null;
        string? notes = null;

        if (extractedData.TryGetValue("professionalId", out var profId))
        {
            if (profId is Guid guidProf)
                professionalId = guidProf;
            else if (profId is string profIdStr && Guid.TryParse(profIdStr, out var parsedGuid))
                professionalId = parsedGuid;
        }

        if (extractedData.TryGetValue("serviceType", out var service))
            serviceType = service?.ToString();

        if (extractedData.TryGetValue("preferredDateTime", out var dateTime))
        {
            if (dateTime is DateTime dt)
                preferredDateTime = dt;
            else if (dateTime is string dtStr && DateTime.TryParse(dtStr, out var parsedDt))
                preferredDateTime = parsedDt;
        }

        if (extractedData.TryGetValue("notes", out var note))
            notes = note?.ToString();

        await bookingService.UpdateBookingDraftAsync(draftId, professionalId, serviceType, preferredDateTime, notes);
    }

    private static ConversationDTO MapToConversationDTO(Conversation conversation)
    {
        return new ConversationDTO
        {
            Id = conversation.Id,
            UserId = conversation.UserId,
            State = conversation.State.ToString(),
            DetectedIntent = conversation.DetectedIntent?.ToString(),
            StartedAt = conversation.StartedAt,
            LastActivityAt = conversation.LastActivityAt,
            IsActive = conversation.IsActive
        };
    }

    private static ConversationMessageDTO MapToMessageDTO(ConversationMessage message)
    {
        return new ConversationMessageDTO
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            Content = message.Content,
            IsFromUser = message.IsFromUser,
            SentAt = message.SentAt,
            SuggestedOptions = message.SuggestedOptions,
            SelectedOption = message.SelectedOption
        };
    }

    private static BookingDraftDTO MapToBookingDraftDTO(BookingDraft draft)
    {
        return new BookingDraftDTO
        {
            Id = draft.Id,
            ConversationId = draft.ConversationId,
            ProfessionalId = draft.ProfessionalId,
            ServiceType = draft.ServiceType,
            PreferredDateTime = draft.PreferredDateTime,
            ClientNotes = draft.ClientNotes,
            Status = draft.Status.ToString(),
            FinalOrderId = draft.FinalOrderId
        };
    }
}