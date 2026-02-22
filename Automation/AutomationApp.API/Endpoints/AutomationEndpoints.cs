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
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

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

        group.MapGet("/conversations", ListConversationsAsync)
            .WithName("ListConversations")
            .WithOpenApi()
            .WithSummary("List all conversations for current user");

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

    private static async Task<IResult> ListConversationsAsync(
        HttpContext httpContext,
        IConversationService conversationService)
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
        {
            return Results.Unauthorized();
        }

        // Get all conversations for user (need to implement this in service)
        // For now, return just the active one
        var conversations = new List<object>();
        var activeConversation = await conversationService.GetActiveConversationByUserIdAsync(userGuid);
        if (activeConversation != null)
        {
            conversations.Add(MapToConversationDTO(activeConversation));
        }

        return Results.Ok(conversations);
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
        await conversationService.AddMessageAsync(conversation.Id, request.Message, true, null, request.SelectedOption);

        // Get or create booking draft
        var bookingDraft = await bookingService.GetBookingDraftByConversationIdAsync(conversation.Id);
        if (bookingDraft == null)
        {
            bookingDraft = await bookingService.CreateBookingDraftAsync(conversation.Id, userGuid);
        }

        // Fetch available professionals
        var availableProfessionals = await bookingService.GetAvailableProfessionalsAsync();
        
        // Fetch domain configurations (service types)
        var domainConfigurations = await bookingService.GetDomainConfigurationsAsync();

        var contextData = conversation.ContextData ?? new Dictionary<string, object>();
        var normalizedInput = (request.SelectedOption ?? request.Message ?? string.Empty).Trim();

        var deterministicResult = ProcessDeterministicBookingFlow(
            normalizedInput,
            conversation.State,
            contextData,
            availableProfessionals);

        LLMResponse llmResponse;
        var finalResponseText = string.Empty;

        if (deterministicResult != null)
        {
            llmResponse = new LLMResponse
            {
                ResponseText = deterministicResult.ResponseText,
                SuggestedOptions = deterministicResult.SuggestedOptions,
                DetectedIntent = UserIntent.BookAppointment,
                SuggestedNextState = deterministicResult.NextState,
                ExtractedData = deterministicResult.ExtractedData
            };

            finalResponseText = deterministicResult.ResponseText;

            await hubContext.Clients.Group($"conversation-{conversation.Id}").SendAsync("ReceiveStreamChunk", new
            {
                chunk = finalResponseText,
                isComplete = false,
                conversationId = conversation.Id
            });
        }
        else
        {
            var streamedResponseBuilder = new StringBuilder();

            llmResponse = await llmService.ProcessUserMessageAsync(
                conversation.Id,
                request.Message,
                conversation.State,
                contextData,
                availableProfessionals,
                domainConfigurations,
                async chunk =>
                {
                    if (string.IsNullOrEmpty(chunk))
                    {
                        return;
                    }

                    streamedResponseBuilder.Append(chunk);
                    await hubContext.Clients.Group($"conversation-{conversation.Id}").SendAsync("ReceiveStreamChunk", new
                    {
                        chunk,
                        isComplete = false,
                        conversationId = conversation.Id
                    });
                });

            finalResponseText = streamedResponseBuilder.ToString();
            if (string.IsNullOrWhiteSpace(finalResponseText))
            {
                finalResponseText = llmResponse.ResponseText;
            }
        }

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

        await hubContext.Clients.Group($"conversation-{conversation.Id}").SendAsync("ReceiveStreamChunk", new
        {
            chunk = string.Empty,
            isComplete = true,
            conversationId = conversation.Id
        });

        // Send typing indicator off via SignalR
        await hubContext.Clients.Group($"conversation-{conversation.Id}").SendAsync("TypingIndicator", false);

        // Add AI response message after streaming is complete
        var aiMessage = await conversationService.AddMessageAsync(
            conversation.Id,
            finalResponseText,
            false,
            llmResponse.SuggestedOptions);

        // Broadcast complete AI response via SignalR with options
        await hubContext.Clients.Group($"conversation-{conversation.Id}").SendAsync("ReceiveMessage", new
        {
            message = new
            {
                id = aiMessage.Id,
                conversationId = conversation.Id,
                content = finalResponseText,
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
            ResponseText = finalResponseText,
            SuggestedOptions = llmResponse.SuggestedOptions,
            CurrentState = llmResponse.SuggestedNextState?.ToString() ?? conversation.State.ToString(),
            IsBookingComplete = isBookingComplete,
            OrderId = finalOrderId
        };

        return Results.Ok(response);
    }

    private static DeterministicBookingResult? ProcessDeterministicBookingFlow(
        string input,
        ConversationState currentState,
        Dictionary<string, object> context,
        List<ProfessionalInfo> professionals)
    {
        var text = (input ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var lower = text.ToLowerInvariant();
        var isBookingStart = lower.Contains("book") || lower.Contains("appointment") || lower.Contains("new appointment");

        if ((currentState == ConversationState.Greeting || currentState == ConversationState.Idle || currentState == ConversationState.CollectingInfo) && isBookingStart)
        {
            var specialties = professionals
                .Where(p => !string.IsNullOrWhiteSpace(p.Specialization))
                .Select(p => p.Specialization!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();

            if (specialties.Count == 0)
            {
                return new DeterministicBookingResult(
                    "I couldn't find available specialties right now. Please try again in a moment.",
                    new List<string> { "Book a new appointment", "Check availability" },
                    ConversationState.Greeting,
                    new Dictionary<string, object>());
            }

            return new DeterministicBookingResult(
                "Great — first choose a specialty.",
                specialties,
                ConversationState.SelectingService,
                new Dictionary<string, object>());
        }

        if (currentState == ConversationState.SelectingService)
        {
            var specialty = MatchSpecialty(text, professionals);
            if (string.IsNullOrWhiteSpace(specialty))
            {
                var options = professionals
                    .Where(p => !string.IsNullOrWhiteSpace(p.Specialization))
                    .Select(p => p.Specialization!.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s)
                    .ToList();

                return new DeterministicBookingResult(
                    "Please select a specialty from the available options.",
                    options,
                    ConversationState.SelectingService,
                    new Dictionary<string, object>());
            }

            var doctorOptions = professionals
                .Where(p => string.Equals((p.Specialization ?? string.Empty).Trim(), specialty, StringComparison.OrdinalIgnoreCase))
                .Select(FormatDoctorOption)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var extracted = new Dictionary<string, object> { ["serviceType"] = specialty };
            return new DeterministicBookingResult(
                $"Great choice. Now select a doctor in {specialty}.",
                doctorOptions,
                ConversationState.SelectingProfessional,
                extracted);
        }

        if (currentState == ConversationState.SelectingProfessional)
        {
            var selectedSpecialty = context.TryGetValue("serviceType", out var s) ? s?.ToString() : null;
            var candidates = professionals
                .Where(p => string.IsNullOrWhiteSpace(selectedSpecialty) || string.Equals((p.Specialization ?? string.Empty).Trim(), selectedSpecialty.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();

            var doctor = candidates.FirstOrDefault(p => IsDoctorMatch(text, p));
            if (doctor == null)
            {
                return new DeterministicBookingResult(
                    "Please select a doctor from the list.",
                    candidates.Select(FormatDoctorOption).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                    ConversationState.SelectingProfessional,
                    new Dictionary<string, object>());
            }

            var extracted = new Dictionary<string, object>
            {
                ["professionalId"] = doctor.Id,
                ["professionalUserId"] = doctor.UserId,
                ["professionalName"] = BuildDoctorName(doctor),
                ["serviceType"] = selectedSpecialty ?? doctor.Specialization ?? string.Empty
            };

            return new DeterministicBookingResult(
                $"Perfect. You selected {BuildDoctorName(doctor)}. Now choose a day.",
                BuildDayOptions(),
                ConversationState.SelectingDateTime,
                extracted);
        }

        if (currentState == ConversationState.SelectingDateTime)
        {
            if (!TryResolveDay(text, out var selectedDayLabel, out var selectedDate))
            {
                return new DeterministicBookingResult(
                    "Please choose a day first.",
                    BuildDayOptions(),
                    ConversationState.SelectingDateTime,
                    new Dictionary<string, object>());
            }

            var extracted = new Dictionary<string, object>
            {
                ["selectedDayLabel"] = selectedDayLabel,
                ["selectedDate"] = selectedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            };

            return new DeterministicBookingResult(
                $"Great. Now choose the time for {selectedDayLabel}.",
                BuildTimeOptions(),
                ConversationState.SelectingTimeSlot,
                extracted);
        }

        if (currentState == ConversationState.SelectingTimeSlot)
        {
            if (!TryResolveTime(text, out var timeLabel, out var timeOfDay))
            {
                return new DeterministicBookingResult(
                    "Please choose a time from the options.",
                    BuildTimeOptions(),
                    ConversationState.SelectingTimeSlot,
                    new Dictionary<string, object>());
            }

            var selectedDateRaw = context.TryGetValue("selectedDate", out var dateObj) ? dateObj?.ToString() : null;
            if (!DateTime.TryParse(selectedDateRaw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var selectedDate))
            {
                return new DeterministicBookingResult(
                    "Let's choose the day again first.",
                    BuildDayOptions(),
                    ConversationState.SelectingDateTime,
                    new Dictionary<string, object>());
            }

            var appointmentLocal = selectedDate.Date.Add(timeOfDay);
            var professionalName = context.TryGetValue("professionalName", out var pn) ? pn?.ToString() ?? "the selected doctor" : "the selected doctor";
            var serviceType = context.TryGetValue("serviceType", out var sv) ? sv?.ToString() ?? "Consultation" : "Consultation";

            var extracted = new Dictionary<string, object>
            {
                ["preferredDateTime"] = appointmentLocal,
                ["timeLabel"] = timeLabel
            };

            return new DeterministicBookingResult(
                $"Please confirm your booking:\n- Specialty: {serviceType}\n- Doctor: {professionalName}\n- Date: {selectedDate:dddd, MMM dd}\n- Time: {timeLabel}\n\nReply with 'Yes, Confirm Appointment' to create it.",
                new List<string> { "Yes, Confirm Appointment", "Change time", "Cancel" },
                ConversationState.ConfirmingBooking,
                extracted);
        }

        if (currentState == ConversationState.ConfirmingBooking)
        {
            if (lower.Contains("yes") || lower.Contains("confirm"))
            {
                var doctorName = context.TryGetValue("professionalName", out var dn) ? dn?.ToString() ?? "the selected doctor" : "the selected doctor";
                return new DeterministicBookingResult(
                    $"Booking request created successfully with {doctorName}. The doctor can now see it and will accept or decline.",
                    new List<string> { "View my appointments", "Book a new appointment" },
                    ConversationState.BookingComplete,
                    new Dictionary<string, object>());
            }

            if (lower.Contains("change"))
            {
                return new DeterministicBookingResult(
                    "Sure — choose a new time.",
                    BuildTimeOptions(),
                    ConversationState.SelectingTimeSlot,
                    new Dictionary<string, object>());
            }

            if (lower.Contains("cancel") || lower.Contains("no"))
            {
                return new DeterministicBookingResult(
                    "Booking cancelled. You can start a new one anytime.",
                    new List<string> { "Book a new appointment", "Check availability" },
                    ConversationState.Greeting,
                    new Dictionary<string, object>());
            }
        }

        return null;
    }

    private static string MatchSpecialty(string input, List<ProfessionalInfo> professionals)
    {
        var specialties = professionals
            .Where(p => !string.IsNullOrWhiteSpace(p.Specialization))
            .Select(p => p.Specialization!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return specialties.FirstOrDefault(s => string.Equals(s, input, StringComparison.OrdinalIgnoreCase)
                                            || input.Contains(s, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
    }

    private static bool IsDoctorMatch(string input, ProfessionalInfo professional)
    {
        var option = FormatDoctorOption(professional);
        var fullName = BuildDoctorName(professional);
        return string.Equals(option, input, StringComparison.OrdinalIgnoreCase)
               || string.Equals(fullName, input, StringComparison.OrdinalIgnoreCase)
               || input.Contains(fullName, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatDoctorOption(ProfessionalInfo professional)
    {
        var name = BuildDoctorName(professional);
        var specialty = string.IsNullOrWhiteSpace(professional.Specialization) ? "General" : professional.Specialization!.Trim();
        return $"{name} - {specialty}";
    }

    private static string BuildDoctorName(ProfessionalInfo professional)
    {
        var firstName = (professional.FirstName ?? string.Empty).Trim();
        var lastName = (professional.LastName ?? string.Empty).Trim();
        var combined = $"{firstName} {lastName}".Trim();
        return string.IsNullOrWhiteSpace(combined) ? "Doctor" : combined;
    }

    private static List<string> BuildDayOptions()
    {
        var today = DateTime.Today;
        return new List<string>
        {
            $"Today ({today:ddd, MMM dd})",
            $"Tomorrow ({today.AddDays(1):ddd, MMM dd})",
            $"{today.AddDays(2):dddd} ({today.AddDays(2):MMM dd})"
        };
    }

    private static List<string> BuildTimeOptions()
    {
        return new List<string>
        {
            "09:00 AM",
            "11:00 AM",
            "02:00 PM",
            "04:00 PM"
        };
    }

    private static bool TryResolveDay(string input, out string label, out DateTime date)
    {
        var today = DateTime.Today;
        label = string.Empty;
        date = today;

        if (input.Contains("today", StringComparison.OrdinalIgnoreCase))
        {
            label = $"Today ({today:ddd, MMM dd})";
            date = today;
            return true;
        }

        if (input.Contains("tomorrow", StringComparison.OrdinalIgnoreCase))
        {
            date = today.AddDays(1);
            label = $"Tomorrow ({date:ddd, MMM dd})";
            return true;
        }

        if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            date = parsed.Date;
            label = $"{date:dddd} ({date:MMM dd})";
            return true;
        }

        var thirdDay = today.AddDays(2);
        if (input.Contains(thirdDay.ToString("dddd", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase))
        {
            date = thirdDay;
            label = $"{thirdDay:dddd} ({thirdDay:MMM dd})";
            return true;
        }

        return false;
    }

    private static bool TryResolveTime(string input, out string timeLabel, out TimeSpan time)
    {
        timeLabel = string.Empty;
        time = TimeSpan.Zero;

        var allowed = BuildTimeOptions();
        var matched = allowed.FirstOrDefault(o => string.Equals(o, input, StringComparison.OrdinalIgnoreCase) || input.Contains(o, StringComparison.OrdinalIgnoreCase));
        if (matched == null)
        {
            return false;
        }

        if (!DateTime.TryParse(matched, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return false;
        }

        timeLabel = matched;
        time = parsed.TimeOfDay;
        return true;
    }

    private sealed record DeterministicBookingResult(
        string ResponseText,
        List<string> SuggestedOptions,
        ConversationState NextState,
        Dictionary<string, object> ExtractedData);

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
            {
                // Convert to UTC if not already UTC
                preferredDateTime = dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();
            }
            else if (dateTime is string dtStr && DateTime.TryParse(dtStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDt))
            {
                // Convert to UTC if not already UTC
                preferredDateTime = parsedDt.Kind == DateTimeKind.Utc ? parsedDt : parsedDt.ToUniversalTime();
            }
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