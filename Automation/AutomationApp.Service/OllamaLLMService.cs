using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using AutomationApp.Domain.Enums;
using AutomationApp.Domain.Interfaces;
using AutomationApp.Domain.Entity;

namespace AutomationApp.Service;

/// <summary>
/// LLM service integration with Ollama for AI-powered conversational booking assistance
/// Supports streaming responses, context-aware conversations, and deterministic booking flows
/// </summary>
public class OllamaLLMService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly string _baseUrl;
    private readonly string _modelName;
    private readonly int _numPredict;
    private readonly int _numCtx;
    private readonly int _numThread;
    private readonly int _requestTimeoutSeconds;

    public OllamaLLMService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _baseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _modelName = configuration["Ollama:ModelName"] ?? "tinyllama";
        _numPredict = int.TryParse(configuration["Ollama:NumPredict"], out var numPredict) ? numPredict : 128;
        _numCtx = int.TryParse(configuration["Ollama:NumCtx"], out var numCtx) ? numCtx : 1536;
        _numThread = int.TryParse(configuration["Ollama:NumThread"], out var numThread)
            ? numThread
            : Math.Max(2, Environment.ProcessorCount / 2);
        _requestTimeoutSeconds = ResolveRequestTimeoutSeconds(configuration);
        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_requestTimeoutSeconds);
    }

    /// <summary>
    /// Processes a user message through the LLM with optional streaming response
    /// Builds context-aware system prompt including conversation state and available professionals
    /// </summary>
    /// <param name="conversationId">ID of the current conversation</param>
    /// <param name="userMessage">User's input message</param>
    /// <param name="currentState">Current conversation state in booking flow</param>
    /// <param name="context">Additional context data (selected doctor, date, etc.)</param>
    /// <param name="availableProfessionals">List of available professionals for booking</param>
    /// <param name="domainConfigurations">Available service types/specialties</param>
    /// <param name="onPartialResponse">Optional callback for streaming response chunks</param>
    /// <returns>LLM response with extracted data, suggestions, and next state</returns>
    public async Task<LLMResponse> ProcessUserMessageAsync(
        Guid conversationId,
        string userMessage,
        ConversationState currentState,
        Dictionary<string, object>? context = null,
        List<AutomationApp.Domain.Entity.ProfessionalInfo>? availableProfessionals = null,
        List<DomainConfigurationInfo>? domainConfigurations = null,
        Func<string, Task>? onPartialResponse = null)
    {
        var systemPrompt = BuildSystemPrompt(currentState, context, availableProfessionals, domainConfigurations);
        var contextInfo = BuildContextInfo(context, currentState);
        var shouldStream = onPartialResponse != null;

        _httpClient.Timeout = TimeSpan.FromSeconds(_requestTimeoutSeconds);

        var requestPayload = new
        {
            model = _modelName,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = $"Context: {contextInfo}\n\nUser Message: {userMessage}" }
            },
            stream = shouldStream,
            options = new
            {
                temperature = 0.7,
                num_predict = 150,
                top_p = 0.95,
                top_k = 50,
                num_ctx = 2048,
                num_thread = 8,
                num_batch = 1024,
                num_keep = 32,
                mirostat = 2,
                mirostat_tau = 5.0,
                mirostat_eta = 0.1
            },
            keep_alive = "5m"
        };

        var requestJson = JsonConvert.SerializeObject(requestPayload);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
        {
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };

        try
        {
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            if (!shouldStream)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var ollamaResponse = JsonConvert.DeserializeObject<OllamaResponse>(responseContent);

                if (ollamaResponse?.Message?.Content == null)
                    return CreateFallbackResponse();

                var aiContent = ollamaResponse.Message.Content;
                Console.WriteLine($"[DEBUG] AI Response Content: {aiContent}");

                var parsedResponse = ParseAIResponse(aiContent);

                Console.WriteLine($"[DEBUG] Parsed Response - Text: {parsedResponse.ResponseText}, Options: [{string.Join(", ", parsedResponse.SuggestedOptions)}], State: {parsedResponse.SuggestedNextState}");

                return parsedResponse;
            }

            var fullResponseBuilder = new StringBuilder();
            await using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                try
                {
                    var chunk = JsonConvert.DeserializeObject<OllamaResponse>(line);
                    var contentChunk = chunk?.Message?.Content;
                    if (!string.IsNullOrEmpty(contentChunk))
                    {
                        fullResponseBuilder.Append(contentChunk);
                        await onPartialResponse!(contentChunk);
                    }

                    if (chunk?.Done == true)
                    {
                        break;
                    }
                }
                catch
                {
                    continue;
                }
            }

            var fullContent = fullResponseBuilder.ToString();
            if (string.IsNullOrWhiteSpace(fullContent))
            {
                return CreateFallbackResponse();
            }

            return ParseAIResponse(fullContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calling Ollama API: {ex.Message}");

            // Fallback to mock response WITHOUT streaming (to avoid duplicates)
            var mockResponse = GetMockResponse(userMessage, currentState, availableProfessionals, domainConfigurations);
            var extractedData = ExtractDataFromMessage(userMessage, currentState, availableProfessionals, domainConfigurations);

            // Return proper LLMResponse for mock data
            return new LLMResponse
            {
                ResponseText = mockResponse,
                SuggestedOptions = GetSuggestedOptionsFromMessage(userMessage, currentState, availableProfessionals, domainConfigurations),
                DetectedIntent = DetectIntentFromMessage(userMessage),
                SuggestedNextState = GetNextStateFromMessage(userMessage, currentState),
                ExtractedData = extractedData
            };
        }
    }

    public async Task<string> GenerateGreetingAsync(Guid userId)
    {
        _httpClient.Timeout = TimeSpan.FromSeconds(_requestTimeoutSeconds);

        var requestPayload = new
        {
            model = _modelName,
            messages = new[]
            {
                new { role = "system", content = "You are a helpful appointment booking assistant. Greet the user warmly and ask how you can help them today. Keep it brief and friendly." },
                new { role = "user", content = "Generate a greeting message." }
            },
            stream = false,
            options = new
            {
                temperature = 0.7,
                num_predict = 150,
                num_ctx = 2048,
                num_thread = 2
            }
        };

        var requestJson = JsonConvert.SerializeObject(requestPayload);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("/api/chat", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var ollamaResponse = JsonConvert.DeserializeObject<OllamaResponse>(responseContent);

            return ollamaResponse?.Message?.Content ?? 
                "Hello! I'm your AI booking assistant. How can I help you today?";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating greeting: {ex.Message}");
            return "Hello! I'm your AI booking assistant. How can I help you today?";
        }
    }

    public async Task<List<string>> GenerateBookingOptionsAsync()
    {
        var requestPayload = new
        {
            model = _modelName,
            messages = new[]
            {
                new { role = "system", content = "Generate 4-5 quick action options for booking an appointment. Return only a JSON array of strings. Keep it simple and clear." },
                new { role = "user", content = "Generate booking options." }
            },
            stream = false,
            options = new
            {
                temperature = 0.7,
                num_predict = 150,
                num_ctx = 2048,
                num_thread = 2
            },
            format = "json"
        };

        var requestJson = JsonConvert.SerializeObject(requestPayload);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("/api/chat", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var ollamaResponse = JsonConvert.DeserializeObject<OllamaResponse>(responseContent);
            var aiContent = ollamaResponse?.Message?.Content;

            if (!string.IsNullOrEmpty(aiContent))
            {
                var options = JsonConvert.DeserializeObject<OptionsResponse>(aiContent);
                return options?.Options ?? GetDefaultBookingOptions();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating options: {ex.Message}");
        }

        return GetDefaultBookingOptions();
    }

    private string BuildSystemPrompt(ConversationState currentState, Dictionary<string, object>? context, List<AutomationApp.Domain.Entity.ProfessionalInfo>? availableProfessionals, List<DomainConfigurationInfo>? domainConfigurations)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("You are an intelligent appointment booking assistant for a booking platform.");
        prompt.AppendLine("Your role is to help users book appointments through natural conversation.");
        prompt.AppendLine();

        prompt.AppendLine("BOOKING FLOW SEQUENCE (STRICT - DO NOT SKIP STEPS):");

                prompt.AppendLine("1. Greeting → Ask what they need");

                prompt.AppendLine("2. SelectingService → Ask which service type (Medical Consultation, Legal Consultation, etc.) - MUST provide options in suggestedOptions");

                prompt.AppendLine("3. SelectingProfessional → Show doctors with name + specialization (e.g., 'Dr. John Smith - Cardiology') - MUST provide options in suggestedOptions");

                prompt.AppendLine("4. SelectingDateTime → Show date/time slots (e.g., 'Today, 2:00 PM', 'Tomorrow, 10:00 AM') - MUST provide options in suggestedOptions");

                prompt.AppendLine("5. ConfirmingBooking → Show summary and ask for confirmation");

                prompt.AppendLine("6. BookingComplete → Confirm booking was successful");

                prompt.AppendLine();

                prompt.AppendLine("CRITICAL RULES:");

                prompt.AppendLine("- ALWAYS include suggestedOptions when asking the user to select something");

                prompt.AppendLine("- suggestedOptions MUST be clickable options the user can choose from");

                prompt.AppendLine("- suggestedOptions MUST contain 3-6 specific choices");

                prompt.AppendLine("- DO NOT skip to the next step without showing options in suggestedOptions");

                prompt.AppendLine("- When a user selects a professional, ALWAYS move to SelectingDateTime state");

                prompt.AppendLine("- When in SelectingDateTime state, DO NOT skip showing date/time options");

                prompt.AppendLine();

                prompt.AppendLine("Current conversation state: " + currentState.ToString());

                prompt.AppendLine();

        // Add domain configurations (service types)
        if (domainConfigurations != null && domainConfigurations.Any())
        {
            prompt.AppendLine("Available Service Types (Domain Configurations):");
            prompt.AppendLine("Users can book appointments for the following service types:");
            prompt.AppendLine();

            foreach (var config in domainConfigurations)
            {
                prompt.AppendLine($"- {config.Name}");
                prompt.AppendLine($"  ID: {config.Id}");
                if (!string.IsNullOrEmpty(config.Description))
                    prompt.AppendLine($"  Description: {config.Description}");
                prompt.AppendLine($"  Default Duration: {config.DefaultDurationMinutes} minutes");
                prompt.AppendLine();
            }
            prompt.AppendLine("IMPORTANT: When users want to book an appointment, first ask them which service type they need.");
            prompt.AppendLine("Present the service types above in suggestedOptions when asking.");
            prompt.AppendLine();
        }

        // Add domain configurations (service types)
        if (domainConfigurations != null && domainConfigurations.Any())
        {
            prompt.AppendLine("Available Service Types (Domain Configurations):");
            prompt.AppendLine("Users can book appointments for the following service types:");
            prompt.AppendLine();

            foreach (var config in domainConfigurations)
            {
                prompt.AppendLine($"- {config.Name}");
                prompt.AppendLine($"  ID: {config.Id}");
                if (!string.IsNullOrEmpty(config.Description))
                    prompt.AppendLine($"  Description: {config.Description}");
                prompt.AppendLine($"  Default Duration: {config.DefaultDurationMinutes} minutes");
                prompt.AppendLine();
            }
            prompt.AppendLine("IMPORTANT: When users want to book an appointment, first ask them which service type they need.");
            prompt.AppendLine("Present the service types above in suggestedOptions when asking.");
            prompt.AppendLine();
        }

        // Add available professionals to the prompt
        if (availableProfessionals != null && availableProfessionals.Any())
        {
            prompt.AppendLine("Available Professionals:");
            prompt.AppendLine("The following doctors/professionals are available for booking:");
            prompt.AppendLine();
            prompt.AppendLine("=== PROFESSIONAL LIST BY SPECIALIZATION ===");

            // Group professionals by specialization
            var professionalsBySpecialization = availableProfessionals
                .Where(p => p.IsAvailable)
                .GroupBy(p => p.Specialization ?? "General")
                .OrderBy(g => g.Key);

            foreach (var group in professionalsBySpecialization)
            {
                prompt.AppendLine($"Specialization: {group.Key}");
                foreach (var prof in group)
                {
                    var firstName = !string.IsNullOrEmpty(prof.FirstName) && prof.FirstName != "Doctor" ? prof.FirstName : "";
                    var lastName = !string.IsNullOrEmpty(prof.LastName) && prof.LastName != "Profile" ? prof.LastName : "";
                    var name = $"{firstName} {lastName}".Trim();

                    // If name is still empty or generic, create a name from specialization
                    if (string.IsNullOrEmpty(name) || name == "Doctor Profile")
                    {
                        if (!string.IsNullOrEmpty(prof.Specialization))
                        {
                            name = $"Dr. {prof.Specialization}";
                        }
                        else
                        {
                            name = "Doctor";
                        }
                    }

                    prompt.AppendLine($"  - ID: {prof.Id} | Name: {name} | Specialization: {prof.Specialization} | UserId: {prof.UserId}");
                }
                prompt.AppendLine();
            }
            prompt.AppendLine("====================================================");
            prompt.AppendLine("CRITICAL INSTRUCTIONS FOR BOOKING FLOW:");
            prompt.AppendLine();
            prompt.AppendLine("STEP 1: SELECTING SERVICE");
            prompt.AppendLine("- When user wants to book, FIRST ask which service type (Cardiology, Dermatology, etc.)");
            prompt.AppendLine("- Show service types in suggestedOptions format: [\"Cardiology\", \"Dermatology\"]");
            prompt.AppendLine("- Extract selected service as 'serviceType' in extractedData");
            prompt.AppendLine("- Move to SelectingProfessional state");
            prompt.AppendLine();
            prompt.AppendLine("STEP 2: SELECTING PROFESSIONAL");
            prompt.AppendLine("- Filter professionals by the selected serviceType/specialization");
            prompt.AppendLine("- Show ONLY professionals matching the selected service");
            prompt.AppendLine("- Format options as: [\"Dr. [Name] - [Specialization]\", \"Dr. [Name2] - [Specialization]\"]");
            prompt.AppendLine("- Example: [\"Dr. Cardiology - Cardiology\", \"Dr. Dermatology - Dermatology\"]");
            prompt.AppendLine("- Extract selected professional's ID as 'professionalId' in extractedData");
            prompt.AppendLine("- Move to SelectingDateTime state");
            prompt.AppendLine();
            prompt.AppendLine("STEP 3: SELECTING DATE");
            prompt.AppendLine("- Show date options: [\"Today\", \"Tomorrow\", \"Monday\", \"Tuesday\", etc.]");
            prompt.AppendLine("- Extract selected date as 'preferredDate' in extractedData");
            prompt.AppendLine("- Move to SelectingTimeSlot state");
            prompt.AppendLine();
            prompt.AppendLine("STEP 4: SELECTING TIME");
            prompt.AppendLine("- Show time slots: [\"9:00 AM\", \"11:00 AM\", \"2:00 PM\", \"4:00 PM\"]");
            prompt.AppendLine("- Extract selected time as 'preferredTime' in extractedData");
            prompt.AppendLine("- Combine date and time into 'preferredDateTime' in extractedData");
            prompt.AppendLine("- Move to ConfirmingBooking state");
            prompt.AppendLine();
            prompt.AppendLine("STEP 5: CONFIRMING BOOKING");
            prompt.AppendLine("- Show booking summary with all details");
            prompt.AppendLine("- Options: [\"Yes, Confirm\", \"No, Cancel\", \"Change Details\"]");
            prompt.AppendLine("- On confirm, move to BookingComplete state");
            prompt.AppendLine();
        }

        switch (currentState)
        {
            case ConversationState.Greeting:
                prompt.AppendLine("Welcome the user and determine what they need help with.");
                prompt.AppendLine("Detect their intent: booking an appointment, checking availability, asking questions, etc.");
                if (domainConfigurations != null && domainConfigurations.Any())
                {
                    prompt.AppendLine("If they want to book an appointment, first ask them which service type they need.");
                    prompt.AppendLine("Present the available service types from the domain configurations in suggestedOptions.");
                }
                if (availableProfessionals != null && availableProfessionals.Any())
                {
                    prompt.AppendLine("After they select a service type, offer to show them available doctors/professionals.");
                    prompt.AppendLine("If you suggest professionals in suggestedOptions, format as \"Dr. [Name] - [Specialization]\"");
                }
                break;
            case ConversationState.CollectingInfo:
                prompt.AppendLine("Collect necessary information for booking:");
                prompt.AppendLine("- Service type they need");
                prompt.AppendLine("- Preferred date and time");
                prompt.AppendLine("- Any specific requirements");
                prompt.AppendLine("- Which professional they want to see (if multiple available)");
                break;
            case ConversationState.SelectingService:
                prompt.AppendLine("Help the user select a service type from the available domain configurations.");
                if (domainConfigurations != null && domainConfigurations.Any())
                {
                    prompt.AppendLine("Present each service type with its name and description in suggestedOptions.");
                    prompt.AppendLine("Format: \"[Service Name] - [Description]\"");
                    prompt.AppendLine("Example: [\"Medical Consultation - General medical consultation appointments\", \"Legal Consultation - Legal advice and consultation appointments\"]");
                }
                else
                {
                    prompt.AppendLine("Present clear options they can choose from.");
                }
                prompt.AppendLine("When user selects a service type, extract it as 'serviceType' in extractedData.");
                break;
            case ConversationState.SelectingProfessional:
                prompt.AppendLine("Help the user select a professional based on the selected service type.");
                prompt.AppendLine("CRITICAL: You MUST filter professionals by the serviceType that was selected in the previous step.");
                prompt.AppendLine("Only show professionals whose specialization matches the selected service type.");
                prompt.AppendLine("CRITICAL: When generating suggestedOptions for professionals, you MUST include BOTH the doctor's identifier AND their specialization.");
                prompt.AppendLine("Format: \"Dr. [Name/Identifier] - [Specialization]\" or \"[Name/Identifier] - [Specialization]\"");
                prompt.AppendLine("Example suggestedOptions: [\"Dr. Cardiology - Cardiology\", \"Dr. Dermatology - Dermatology\"]");
                prompt.AppendLine("DO NOT include professionals from other specializations.");
                prompt.AppendLine("When user selects a professional, set 'professionalId' in extractedData with the professional's ID (e.g., 'professionalId': '55123dbb-59b8-435d-837e-209431f04025').");
                prompt.AppendLine("IMPORTANT: After a professional is selected, you MUST set suggestedNextState to 'SelectingDateTime' to ask for date.");
                break;
            case ConversationState.SelectingDateTime:
                prompt.AppendLine("Help the user select an available date first.");
                prompt.AppendLine("CRITICAL: Show ONLY date options in the suggestedOptions array. Time slots will be selected in the next step.");
                prompt.AppendLine("Provide multiple date options:");
                prompt.AppendLine($"- Today ({DateTime.Today:MMM dd})");
                prompt.AppendLine($"- Tomorrow ({DateTime.Today.AddDays(1):MMM dd})");
                prompt.AppendLine($"- Next week ({DateTime.Today.AddDays(7):MMM dd})");
                prompt.AppendLine($"- Monday, Tuesday, Wednesday, Thursday, Friday");
                prompt.AppendLine();
                prompt.AppendLine("Example suggestedOptions format:");
                prompt.AppendLine("[\"Today\", \"Tomorrow\", \"Monday\", \"Tuesday\", \"Wednesday\", \"Thursday\", \"Friday\", \"Next week\"]");
                prompt.AppendLine();
                prompt.AppendLine("IMPORTANT: The suggestedOptions MUST contain only dates, NOT times.");
                prompt.AppendLine("When user selects a date, extract it as 'preferredDate' in extractedData.");
                prompt.AppendLine("After date is selected, set suggestedNextState to 'SelectingTimeSlot' to show time options.");
                break;
            case ConversationState.SelectingTimeSlot:
                prompt.AppendLine("Help the user select a time slot for the selected date.");
                prompt.AppendLine("CRITICAL: You MUST provide time slot options in the suggestedOptions array.");
                prompt.AppendLine("Provide multiple time slot options:");
                prompt.AppendLine("- 9:00 AM, 10:00 AM, 11:00 AM, 12:00 PM");
                prompt.AppendLine("- 1:00 PM, 2:00 PM, 3:00 PM, 4:00 PM, 5:00 PM");
                prompt.AppendLine();
                prompt.AppendLine("Example suggestedOptions format:");
                prompt.AppendLine("[\"9:00 AM\", \"10:00 AM\", \"11:00 AM\", \"12:00 PM\", \"2:00 PM\", \"3:00 PM\", \"4:00 PM\"]");
                prompt.AppendLine();
                prompt.AppendLine("IMPORTANT: The suggestedOptions MUST contain only time slots.");
                prompt.AppendLine("When user selects a time, extract it as 'preferredTime' in extractedData.");
                prompt.AppendLine("Combine the preferredDate and preferredTime into 'preferredDateTime' in extractedData as a full DateTime string.");
                prompt.AppendLine("After time is selected, set suggestedNextState to 'ConfirmingBooking' to confirm the booking.");
                break;
            case ConversationState.ConfirmingBooking:
                prompt.AppendLine("Confirm all booking details with the user.");
                prompt.AppendLine("Show summary including: selected professional, service type, date/time, and any notes.");
                prompt.AppendLine("Ask for confirmation before submitting.");
                prompt.AppendLine();
                prompt.AppendLine("CRITICAL: Provide confirmation options in suggestedOptions:");
                prompt.AppendLine("Format: [\"Yes, Confirm Appointment\", \"No, Cancel\", \"Change Details\"]");
                prompt.AppendLine();
                prompt.AppendLine("When the user confirms (e.g., 'Yes', 'Confirm', 'OK', 'Go ahead'):");
                prompt.AppendLine("- Set suggestedNextState to 'BookingComplete'");
                prompt.AppendLine("- Include the booking confirmation message in responseText");
                prompt.AppendLine();
                prompt.AppendLine("When the user wants to cancel or change:");
                prompt.AppendLine("- Set suggestedNextState appropriately (e.g., 'SelectingDateTime' for change)");
                prompt.AppendLine("- Ask what they want to change");
                break;
            case ConversationState.FAQ:
                prompt.AppendLine("Answer user's questions about the booking process, services, or policies.");
                prompt.AppendLine("Provide helpful, accurate information.");
                break;
        }

        prompt.AppendLine();
        prompt.AppendLine("IMPORTANT: Return your response in this JSON format:");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"responseText\": \"Your conversational response to the user\",");
        prompt.AppendLine("  \"suggestedOptions\": [\"Option 1\", \"Option 2\", \"Option 3\"],");
        prompt.AppendLine("  \"detectedIntent\": \"BookAppointment|CheckAvailability|AskFAQ|ViewServices|CancelAppointment|RescheduleAppointment|GeneralInquiry\",");
        prompt.AppendLine("  \"suggestedNextState\": \"Greeting|CollectingInfo|SelectingService|SelectingProfessional|SelectingDateTime|ConfirmingBooking|BookingComplete|FAQ|Error\",");
        prompt.AppendLine("  \"extractedData\": {");
        prompt.AppendLine("    \"serviceType\": \"extracted service if mentioned\",");
        prompt.AppendLine("    \"preferredDate\": \"extracted date if mentioned\",");
        prompt.AppendLine("    \"preferredTime\": \"extracted time if mentioned\",");
        prompt.AppendLine("    \"notes\": \"any notes from user\"");
        prompt.AppendLine("  }");
        prompt.AppendLine("}");
        prompt.AppendLine();
        prompt.AppendLine("CRITICAL VALID VALUES:");
        prompt.AppendLine("- suggestedOptions MUST be a simple array of strings. Do NOT use nested objects or complex structures.");
        prompt.AppendLine("Example of CORRECT suggestedOptions: [\"Today, 9:00 AM\", \"Tomorrow, 10:00 AM\"]");
        prompt.AppendLine("Example of WRONG suggestedOptions: {\"dateOptions\": [...], \"timeOptions\": [...]}");
        prompt.AppendLine();
        prompt.AppendLine("- suggestedNextState MUST be one of these EXACT values:");
                        prompt.AppendLine("  * Greeting - Initial state when starting conversation");
                        prompt.AppendLine("  * CollectingInfo - Collecting information from user");
                        prompt.AppendLine("  * SelectingService - User selecting service type");
                        prompt.AppendLine("  * SelectingProfessional - User selecting doctor/professional");
                        prompt.AppendLine("  * SelectingDateTime - User selecting date");
                        prompt.AppendLine("  * SelectingTimeSlot - User selecting time slot");
                        prompt.AppendLine("  * ConfirmingBooking - Confirming booking details");
                        prompt.AppendLine("  * BookingComplete - Booking is confirmed and complete");
                        prompt.AppendLine("  * FAQ - Answering questions");
                        prompt.AppendLine("  * Error - Error state");
                        prompt.AppendLine();
                        prompt.AppendLine("WARNING: DO NOT use invalid states like 'SelectingNextAction', 'NextStep', etc. Only use the states listed above.");
        return prompt.ToString();
    }

    private static int ResolveRequestTimeoutSeconds(IConfiguration configuration)
    {
        var configuredValue = int.TryParse(configuration["Ollama:RequestTimeoutSeconds"], out var timeout)
            ? timeout
            : 90;

        return Math.Max(15, configuredValue);
    }

    private string BuildContextInfo(Dictionary<string, object>? context, ConversationState currentState)
    {
        if (context == null || context.Count == 0)
            return "No previous context.";

        var info = new StringBuilder();
        
        if (currentState == ConversationState.ConfirmingBooking)
        {
            info.AppendLine("=== BOOKING DETAILS FOR CONFIRMATION ===");
            foreach (var kvp in context)
            {
                info.AppendLine($"- {kvp.Key}: {kvp.Value}");
            }
            info.AppendLine("========================================");
        }
        else
        {
            foreach (var kvp in context)
            {
                info.AppendLine($"{kvp.Key}: {kvp.Value}");
            }
        }
        return info.ToString();
    }

    private LLMResponse ParseAIResponse(string aiContent)
    {
        try
        {
            // Clean the AI content - sometimes LLMs wrap JSON in markdown code blocks
            var cleanedContent = aiContent.Trim();
            
            // Remove markdown code block markers if present
            if (cleanedContent.StartsWith("```json"))
            {
                cleanedContent = cleanedContent.Substring(7);
            }
            else if (cleanedContent.StartsWith("```"))
            {
                cleanedContent = cleanedContent.Substring(3);
            }
            
            if (cleanedContent.EndsWith("```"))
            {
                cleanedContent = cleanedContent.Substring(0, cleanedContent.Length - 3);
            }
            
            cleanedContent = cleanedContent.Trim();
            
            // Try to parse as JSON
            var parsed = JsonConvert.DeserializeObject<AIResponse>(cleanedContent);
            if (parsed != null)
            {
                var intent = Enum.TryParse<UserIntent>(parsed.DetectedIntent, out var result) ? result : UserIntent.Unknown;
                ConversationState? nextState = null;
                if (Enum.TryParse<ConversationState>(parsed.SuggestedNextState, out var stateResult))
                {
                    nextState = stateResult;
                }

                // Handle suggestedOptions - it might be a list or a complex object
                List<string> options = new List<string>();
                if (parsed.SuggestedOptions != null && parsed.SuggestedOptions.Count > 0)
                {
                    options = parsed.SuggestedOptions;
                }
                else if (parsed.SuggestedOptionsRaw != null)
                {
                    // Try to extract from raw object
                    var rawJson = JsonConvert.SerializeObject(parsed.SuggestedOptionsRaw);
                    var rawObj = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(rawJson);
                    if (rawObj != null)
                    {
                        // Flatten all options from any nested arrays
                        foreach (var kvp in rawObj)
                        {
                            if (kvp.Value != null)
                            {
                                options.AddRange(kvp.Value);
                            }
                        }
                    }
                }

                return new LLMResponse
                {
                    ResponseText = parsed.ResponseText,
                    SuggestedOptions = options,
                    DetectedIntent = intent,
                    SuggestedNextState = nextState,
                    ExtractedData = parsed.ExtractedData
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing AI response: {ex.Message}");
            Console.WriteLine($"AI Content: {aiContent}");
            
            // Fallback: try to extract responseText if JSON parsing fails
            try
            {
                // Try to find responseText in the content
                var responseTextMatch = Regex.Match(aiContent, @"""responseText""\s*:\s*""([^""]+)""");
                if (responseTextMatch.Success)
                {
                    return new LLMResponse
                    {
                        ResponseText = responseTextMatch.Groups[1].Value,
                        SuggestedOptions = new List<string>(),
                        DetectedIntent = UserIntent.GeneralInquiry,
                        SuggestedNextState = ConversationState.Error
                    };
                }
            }
            catch
            {
                // If all parsing fails, use the raw content
            }
        }

        return new LLMResponse
        {
            ResponseText = aiContent,
            SuggestedOptions = new List<string>(),
            DetectedIntent = UserIntent.GeneralInquiry
        };
    }

    private string ExtractResponseTextForStreaming(string rawContent)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
        {
            return string.Empty;
        }

        try
        {
            var parsed = JObject.Parse(rawContent);
            var responseTextToken = parsed["responseText"];
            if (responseTextToken?.Type == JTokenType.String)
            {
                return responseTextToken.Value<string>() ?? string.Empty;
            }
        }
        catch
        {
            // Partial JSON is expected during streaming.
        }

        var match = Regex.Match(rawContent, @"""responseText""\s*:\s*""(?<txt>(?:\\.|[^""]*)*)""", RegexOptions.Singleline);
        if (!match.Success)
        {
            return string.Empty;
        }

        var captured = match.Groups["txt"].Value;
        captured = Regex.Replace(captured, @"\\u[0-9a-fA-F]{0,3}$", string.Empty);
        captured = Regex.Replace(captured, @"\\[\\/""bfnrt]?$", string.Empty);

        try
        {
            var asJsonString = $"\"{captured}\"";
            return JsonConvert.DeserializeObject<string>(asJsonString) ?? string.Empty;
        }
        catch
        {
            return captured.Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\\"", "\"").Replace("\\\\", "\\");
        }
    }

    private LLMResponse CreateFallbackResponse()
    {
        return new LLMResponse
        {
            ResponseText = "I'm sorry, I'm having trouble processing your request right now. Please try again or contact support.",
            SuggestedOptions = GetDefaultBookingOptions(),
            DetectedIntent = UserIntent.GeneralInquiry,
            SuggestedNextState = ConversationState.Error
        };
    }

    private string GetMockResponse(
        string userMessage, 
        ConversationState currentState,
        List<ProfessionalInfo>? availableProfessionals = null,
        List<DomainConfigurationInfo>? domainConfigurations = null)
    {
        var lowerMessage = userMessage.ToLower();
        
        if (currentState == ConversationState.Greeting || currentState == ConversationState.Idle)
        {
            // Check if user wants to start booking
            if (lowerMessage.Contains("book") || lowerMessage.Contains("appointment") || lowerMessage.Contains("schedule"))
            {
                var services = domainConfigurations?.Select(d => d.Name).Distinct().ToList() ?? new List<string> { "Cardiology", "Dermatology" };
                var servicesList = string.Join(", ", services);
                return $"I'd be happy to help you book an appointment! We offer the following services: {servicesList}.\n\nPlease select a service by clicking on it above.";
            }
            
            return $"Hello! I'm your AI booking assistant. I can help you book appointments, check availability, and answer questions about our services. How can I assist you today?";
        }
        
        if (currentState == ConversationState.CollectingInfo)
        {
            // Check for service type selection
            if (domainConfigurations != null)
            {
                foreach (var service in domainConfigurations)
                {
                    if (lowerMessage.Contains(service.Name.ToLower()) || 
                        (service.Name == "Dermatology" && lowerMessage.Contains("derm")) ||
                        (service.Name == "Cardiology" && lowerMessage.Contains("cardio")))
                    {
                        var professionals = availableProfessionals?.Where(p => 
                            p.Specialization != null && 
                            p.Specialization.Contains(service.Name, StringComparison.OrdinalIgnoreCase) &&
                            p.IsAvailable
                        ).ToList();
                        
                        if (professionals.Any())
                        {
                            var prosText = string.Join("\n", professionals.Select(p => {
                                var doctorName = !string.IsNullOrEmpty(p.FirstName) && !string.IsNullOrEmpty(p.LastName)
                                    ? $"Dr. {p.FirstName} {p.LastName}"
                                    : !string.IsNullOrEmpty(p.FirstName)
                                        ? $"Dr. {p.FirstName}"
                                        : $"Dr. {p.Specialization}";
                                return $"• {doctorName} - {p.Specialization} (${p.HourlyRate}/hour)";
                            }));
                            return $"Great! For {service.Name}, we have the following doctors available:\n{prosText}\n\nPlease let me know which doctor you prefer by clicking on their name above.";
                        }
                        else
                        {
                            return $"I'm sorry, but we don't have any {service.Name} specialists available at the moment. Would you like to try a different service?";
                        }
                    }
                }
            }
            
            // Check for doctor name selection
                    if (availableProfessionals != null)
                    {
                        foreach (var pro in availableProfessionals.Where(p => p.IsAvailable))
                        {
                            if (lowerMessage.Contains(pro.FirstName?.ToLower() ?? "") || 
                                lowerMessage.Contains(pro.LastName?.ToLower() ?? "") ||
                                lowerMessage.Contains(pro.Id.ToString().ToLower()) ||
                                lowerMessage.Contains("dr.") && lowerMessage.Contains(pro.Specialization?.ToLower() ?? ""))
                            {
                                var doctorName = !string.IsNullOrEmpty(pro.FirstName) && !string.IsNullOrEmpty(pro.LastName)
                                    ? $"Dr. {pro.FirstName} {pro.LastName}"
                                    : !string.IsNullOrEmpty(pro.FirstName)
                                        ? $"Dr. {pro.FirstName}"
                                        : $"Dr. {pro.Specialization}";
                                
                                return $"Excellent! You've selected {doctorName} ({pro.Specialization}).\n\nWhat date and time would you like to schedule your appointment? Please provide a date (e.g., 'tomorrow', 'Monday') and time (e.g., '10:00 AM', '2:00 PM').";
                            }
                        }
                    }            
            // Check for date/time preference
            if (lowerMessage.ContainsAny(new[] { "tomorrow", "today", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday" }) ||
                lowerMessage.Contains(":") || // Time format
                lowerMessage.Contains("am") || lowerMessage.Contains("pm"))
            {
                return $"Thank you! I've noted your preferred date and time.\n\nPlease provide your phone number so I can contact you if needed: ";
            }
            
            // Check for phone number
            if (lowerMessage.Contains("phone") || lowerMessage.Contains("contact") || 
                lowerMessage.ContainsAny(new[] { "123", "456", "789", "012", "345", "678", "901" }) ||
                System.Text.RegularExpressions.Regex.IsMatch(userMessage, @"\d{3}[-.\s]?\d{3}[-.\s]?\d{4}"))
            {
                return $"Perfect! I have all the information needed for your booking.\n\nTo confirm your appointment, please click 'Confirm Booking' below.";
            }
            
            return "I'm collecting information for your booking. Please let me know:\n• Which service you need (Cardiology or Dermatology)\n• Which doctor you prefer\n• Your preferred date and time\n• Your phone number";
        }
        
        if (currentState == ConversationState.ConfirmingBooking)
        {
            if (lowerMessage.Contains("yes") || lowerMessage.Contains("confirm") || lowerMessage.Contains("proceed"))
            {
                return "Thank you for confirming! Your appointment has been successfully booked.\n\nYou'll receive a confirmation email with all the details shortly.\n\nIs there anything else I can help you with?";
            }
            
            if (lowerMessage.Contains("no") || lowerMessage.Contains("cancel") || lowerMessage.Contains("change"))
            {
                return "No problem. We can modify your booking details. What would you like to change?";
            }
        }
        
        if (currentState == ConversationState.BookingComplete)
        {
            return "Your booking is complete! What would you like to do next?\n• Book another appointment\n• View my appointments\n• Ask a question";
        }
        
        // Default booking initiation
        if (lowerMessage.Contains("book") || lowerMessage.Contains("appointment") || lowerMessage.Contains("schedule"))
        {
            var services = domainConfigurations?.Select(d => d.Name).Distinct().ToList() ?? new List<string> { "Cardiology", "Dermatology" };
            var servicesList = string.Join(", ", services);
            return $"I'd be happy to help you book an appointment! We offer the following services: {servicesList}.\n\nPlease select a service by clicking on it above.";
        }
        
        if (lowerMessage.Contains("available") || lowerMessage.Contains("availability"))
        {
            var availablePros = availableProfessionals?.Where(p => p.IsAvailable).ToList() ?? new List<ProfessionalInfo>();
            if (availablePros.Any())
            {
                var prosText = string.Join("\n", availablePros.Select(p => 
                    $"• Dr. {p.FirstName} {p.LastName} - {p.Specialization} (${p.HourlyRate}/hour)"));
                return $"Here are our currently available doctors:\n{prosText}\n\nWould you like to book with any of them? Just say which doctor you'd like to see.";
            }
            else
            {
                return "I'm sorry, but we don't have any doctors available at the moment. Please check back later or contact us directly.";
            }
        }
        
        return "I'm here to help you book an appointment. You can:\n• Say 'Book an appointment' to start\n• Say 'Check availability' to see available doctors\n• Click on any suggested option above\n\nWhat would you like to do?";
    }

    private List<string> GetDefaultBookingOptions()
    {
        return new List<string>
        {
            "Book a new appointment",
            "Check availability",
            "View my appointments",
            "Ask a question",
            "Cancel appointment"
        };
    }

    private List<string> GetSuggestedOptionsFromMessage(string userMessage, ConversationState currentState, List<ProfessionalInfo>? availableProfessionals, List<DomainConfigurationInfo>? domainConfigurations)
    {
        var lowerMessage = userMessage.ToLower();
        
        if (currentState == ConversationState.Greeting || currentState == ConversationState.Idle)
        {
            return new List<string> { "Book a new appointment", "Check availability", "View my appointments", "Ask a question" };
        }
        
        if (currentState == ConversationState.CollectingInfo)
        {
            // Check if user is selecting a service
            if (domainConfigurations != null)
            {
                foreach (var service in domainConfigurations)
                {
                    if (lowerMessage.Contains(service.Name.ToLower()) || 
                        (service.Name == "Dermatology" && lowerMessage.Contains("derm")) ||
                        (service.Name == "Cardiology" && lowerMessage.Contains("cardio")))
                    {
                        // Return available doctors for this service
                        var professionals = availableProfessionals?.Where(p => 
                            p.Specialization != null && 
                            p.Specialization.Contains(service.Name, StringComparison.OrdinalIgnoreCase) &&
                            p.IsAvailable
                        ).ToList();
                        
                        if (professionals.Any())
                        {
                            return professionals.Take(3).Select(p => $"Dr. {p.FirstName} {p.LastName}").ToList();
                        }
                    }
                }
            }
            
            // Check if user is selecting a doctor
            if (availableProfessionals != null)
            {
                foreach (var pro in availableProfessionals.Where(p => p.IsAvailable))
                {
                    if (lowerMessage.Contains(pro.FirstName?.ToLower() ?? "") || 
                        lowerMessage.Contains(pro.LastName?.ToLower() ?? "") ||
                        lowerMessage.Contains(pro.Id.ToString().ToLower()) ||
                        lowerMessage.Contains("dr.") && lowerMessage.Contains(pro.Specialization?.ToLower() ?? ""))
                    {
                        return new List<string> { "Tomorrow 10:00 AM", "Tomorrow 2:00 PM", "Monday 9:00 AM", "Monday 3:00 PM", "Next week" };
                    }
                }
            }
            
            // Check if user provided date/time - show confirm option
            if (lowerMessage.ContainsAny(new[] { "tomorrow", "today", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday" }) ||
                lowerMessage.Contains(":") || lowerMessage.Contains("am") || lowerMessage.Contains("pm"))
            {
                return new List<string> { "Provide phone number", "Skip phone number" };
            }
            
            // Check if user provided phone - show confirm option
            if (System.Text.RegularExpressions.Regex.IsMatch(userMessage, @"\d{3}[-.\s]?\d{3}[-.\s]?\d{4}") ||
                lowerMessage.Contains("phone") || lowerMessage.Contains("contact"))
            {
                return new List<string> { "Confirm Booking", "Cancel Booking", "Change Details" };
            }
            
            // Default: show services if just started collecting
            var services = domainConfigurations?.Select(d => d.Name).Distinct().ToList() ?? new List<string> { "Cardiology", "Dermatology" };
            return services;
        }
        
        if (currentState == ConversationState.ConfirmingBooking)
        {
            return new List<string> { "Confirm Booking", "Cancel Booking", "Change Details" };
        }
        
        if (currentState == ConversationState.BookingComplete)
        {
            return new List<string> { "Book another appointment", "View my appointments", "Ask a question" };
        }
        
        if (lowerMessage.Contains("available") || lowerMessage.Contains("availability"))
        {
            var availablePros = availableProfessionals?.Where(p => p.IsAvailable).ToList() ?? new List<ProfessionalInfo>();
            if (availablePros.Any())
            {
                return availablePros.Take(3).Select(p => $"Book with Dr. {p.FirstName} {p.LastName}").ToList();
            }
        }
        
        return new List<string>();
    }

    private UserIntent DetectIntentFromMessage(string userMessage)
    {
        var lowerMessage = userMessage.ToLower();
        
        if (lowerMessage.Contains("book") || lowerMessage.Contains("appointment") || lowerMessage.Contains("schedule"))
            return UserIntent.BookAppointment;
        
        if (lowerMessage.Contains("available") || lowerMessage.Contains("availability"))
            return UserIntent.CheckAvailability;
        
        if (lowerMessage.Contains("cancel"))
            return UserIntent.CancelAppointment;
        
        if (lowerMessage.Contains("reschedule"))
            return UserIntent.RescheduleAppointment;
        
        return UserIntent.GeneralInquiry;
    }

    private ConversationState GetNextStateFromMessage(string userMessage, ConversationState currentState)
    {
        var lowerMessage = userMessage.ToLower();
        
        if (currentState == ConversationState.Greeting || currentState == ConversationState.Idle)
        {
            if (lowerMessage.Contains("book") || lowerMessage.Contains("appointment") || lowerMessage.Contains("schedule"))
                return ConversationState.CollectingInfo;
        }
        
        if (currentState == ConversationState.CollectingInfo)
        {
            // Check if phone number is provided - that's the last step before confirmation
            if (System.Text.RegularExpressions.Regex.IsMatch(userMessage, @"\d{3}[-.\s]?\d{3}[-.\s]?\d{4}") ||
                lowerMessage.Contains("phone") || lowerMessage.Contains("contact"))
            {
                return ConversationState.ConfirmingBooking;
            }
            
            // Stay in collecting info state while gathering service, doctor, and time
            return ConversationState.CollectingInfo;
        }
        
        if (currentState == ConversationState.ConfirmingBooking)
        {
            if (lowerMessage.Contains("yes") || lowerMessage.Contains("confirm") || lowerMessage.Contains("proceed"))
                return ConversationState.BookingComplete;
        }
        
        return currentState;
    }

    private Dictionary<string, object> ExtractDataFromMessage(
        string userMessage,
        ConversationState currentState,
        List<ProfessionalInfo>? availableProfessionals,
        List<DomainConfigurationInfo>? domainConfigurations)
    {
        var extractedData = new Dictionary<string, object>();
        var lowerMessage = userMessage.ToLower();

        if (currentState == ConversationState.CollectingInfo)
        {
            // Extract service type
            if (domainConfigurations != null)
            {
                foreach (var service in domainConfigurations)
                {
                    if (lowerMessage.Contains(service.Name.ToLower()) ||
                        (service.Name == "Dermatology" && lowerMessage.Contains("derm")) ||
                        (service.Name == "Cardiology" && lowerMessage.Contains("cardio")))
                    {
                        extractedData["serviceType"] = service.Name;
                        extractedData["domainConfigurationId"] = service.Id;
                        break;
                    }
                }
            }

            // Extract doctor ID
            if (availableProfessionals != null)
            {
                foreach (var pro in availableProfessionals.Where(p => p.IsAvailable))
                {
                    if (lowerMessage.Contains(pro.FirstName?.ToLower() ?? "") ||
                        lowerMessage.Contains(pro.LastName?.ToLower() ?? "") ||
                        lowerMessage.Contains(pro.Id.ToString().ToLower()) ||
                        (lowerMessage.Contains("dr.") && lowerMessage.Contains(pro.Specialization?.ToLower() ?? "")))
                    {
                        extractedData["professionalId"] = pro.Id;
                        extractedData["professionalUserId"] = pro.UserId;
                        break;
                    }
                }
            }

            // Extract date/time
            if (lowerMessage.ContainsAny(new[] { "tomorrow", "today", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday" }) ||
                lowerMessage.Contains(":") || lowerMessage.Contains("am") || lowerMessage.Contains("pm"))
            {
                var dateTime = ParseDateTimeFromMessage(userMessage);
                if (dateTime.HasValue)
                {
                    extractedData["preferredDateTime"] = dateTime.Value;
                }
            }

            // Extract phone number
            var phoneMatch = Regex.Match(userMessage, @"\d{3}[-.\s]?\d{3}[-.\s]?\d{4}");
            if (phoneMatch.Success)
            {
                extractedData["phone"] = phoneMatch.Value;
            }
        }

        return extractedData;
    }

    private DateTime? ParseDateTimeFromMessage(string message)
    {
        var lowerMessage = message.ToLower();
        var now = DateTime.Now;

        // Handle "tomorrow"
        if (lowerMessage.Contains("tomorrow"))
        {
            var date = now.AddDays(1).Date;
            var time = ExtractTimeFromMessage(message);
            if (time.HasValue)
            {
                return date.Add(time.Value);
            }
            return date.AddHours(10); // Default 10 AM
        }

        // Handle "today"
        if (lowerMessage.Contains("today"))
        {
            var date = now.Date;
            var time = ExtractTimeFromMessage(message);
            if (time.HasValue)
            {
                return date.Add(time.Value);
            }
            return date.AddHours(14); // Default 2 PM
        }

        // Handle day names
        var days = new[] { "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday" };
        for (int i = 0; i < days.Length; i++)
        {
            if (lowerMessage.Contains(days[i]))
            {
                var targetDay = (DayOfWeek)i;
                var daysUntil = ((int)targetDay - (int)now.DayOfWeek + 7) % 7;
                if (daysUntil == 0) daysUntil = 7; // Next week, not today
                var date = now.AddDays(daysUntil).Date;
                var time = ExtractTimeFromMessage(message);
                if (time.HasValue)
                {
                    return date.Add(time.Value);
                }
                return date.AddHours(9); // Default 9 AM
            }
        }

        // Handle "next week"
        if (lowerMessage.Contains("next week"))
        {
            var date = now.AddDays(7 - (int)now.DayOfWeek + (int)DayOfWeek.Monday).Date;
            return date.AddHours(9); // Default 9 AM
        }

        return null;
    }

    private TimeSpan? ExtractTimeFromMessage(string message)
    {
        var lowerMessage = message.ToLower();

        // Try to match time patterns like "10:00 AM", "2:00 PM", "10am", etc.
        var timeMatch = Regex.Match(message, @"(\d{1,2}):(\d{2})\s*(am|pm)?", RegexOptions.IgnoreCase);
        if (timeMatch.Success)
        {
            var hours = int.Parse(timeMatch.Groups[1].Value);
            var minutes = int.Parse(timeMatch.Groups[2].Value);
            var period = timeMatch.Groups[3].Value.ToLower();

            if (period == "pm" && hours < 12)
            {
                hours += 12;
            }
            else if (period == "am" && hours == 12)
            {
                hours = 0;
            }

            return new TimeSpan(hours, minutes, 0);
        }

        return null;
    }

    // Internal classes for JSON deserialization
    private class OllamaResponse
    {
        public OllamaMessage? Message { get; set; }
        public bool Done { get; set; }
    }

    private class OllamaMessage
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
    }

    private class AIResponse
    {
        public string ResponseText { get; set; } = string.Empty;
        public List<string>? SuggestedOptions { get; set; }
        public object? SuggestedOptionsRaw { get; set; }
        public string DetectedIntent { get; set; } = "GeneralInquiry";
        public string SuggestedNextState { get; set; } = "Greeting";
        public Dictionary<string, object>? ExtractedData { get; set; }
    }

    private class OptionsResponse
    {
        public List<string> Options { get; set; } = new();
    }
}

public static class StringExtensions
{
    public static bool ContainsAny(this string source, IEnumerable<string> substrings)
    {
        return substrings.Any(s => source.Contains(s, StringComparison.OrdinalIgnoreCase));
    }
}