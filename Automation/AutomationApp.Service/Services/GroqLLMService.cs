using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using AutomationApp.Domain.Enums;
using AutomationApp.Domain.Interfaces;

namespace AutomationApp.Service.Services;

public class GroqLLMService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string BaseUrl = "https://api.groq.com/openai/v1/chat/completions";

    public GroqLLMService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Groq:ApiKey"] ?? throw new InvalidOperationException("Groq API Key is not configured");
    }

    public async Task<LLMResponse> ProcessUserMessageAsync(Guid conversationId, string userMessage, ConversationState currentState, Dictionary<string, object>? context = null, List<AutomationApp.Domain.Entity.ProfessionalInfo>? availableProfessionals = null, List<DomainConfigurationInfo>? domainConfigurations = null)
    {
        var systemPrompt = BuildSystemPrompt(currentState, context, availableProfessionals, domainConfigurations);
        var contextInfo = BuildContextInfo(context, currentState);

        var requestPayload = new
        {
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = $"Context: {contextInfo}\n\nUser Message: {userMessage}" }
            },
            model = "llama-3.1-8b-instant",
            temperature = 1,
            max_completion_tokens = 1024,
            top_p = 1,
            stream = false,
            response_format = new { type = "json_object" }
        };

        var requestJson = JsonConvert.SerializeObject(requestPayload);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        try
        {
            var response = await _httpClient.PostAsync(BaseUrl, content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var groqResponse = JsonConvert.DeserializeObject<GroqResponse>(responseContent);

            if (groqResponse?.Choices?.FirstOrDefault()?.Message?.Content == null)
                return CreateFallbackResponse();

            var aiContent = groqResponse!.Choices![0]!.Message!.Content!;
            if (aiContent == null)
                return CreateFallbackResponse();

            Console.WriteLine($"[DEBUG] AI Response Content: {aiContent}");

            var parsedResponse = ParseAIResponse(aiContent);

            Console.WriteLine($"[DEBUG] Parsed Response - Text: {parsedResponse.ResponseText}, Options: [{string.Join(", ", parsedResponse.SuggestedOptions)}], State: {parsedResponse.SuggestedNextState}");

            return parsedResponse;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calling Groq API: {ex.Message}");
            return CreateFallbackResponse();
        }
    }

    public async Task<string> GenerateGreetingAsync(Guid userId)
    {
        var requestPayload = new
        {
            messages = new[]
            {
                new { role = "system", content = "You are a helpful appointment booking assistant. Greet the user warmly and ask how you can help them today." },
                new { role = "user", content = "Generate a greeting message." }
            },
            model = "llama-3.1-8b-instant",
            temperature = 0.8,
            max_completion_tokens = 200,
            stream = false
        };

        var requestJson = JsonConvert.SerializeObject(requestPayload);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        try
        {
            var response = await _httpClient.PostAsync(BaseUrl, content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var groqResponse = JsonConvert.DeserializeObject<GroqResponse>(responseContent);

            return groqResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? 
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
            messages = new[]
            {
                new { role = "system", content = "Generate 4-5 quick action options for booking an appointment. Return only a JSON array of strings." },
                new { role = "user", content = "Generate booking options." }
            },
            model = "llama-3.1-8b-instant",
            temperature = 0.7,
            max_completion_tokens = 150,
            stream = false,
            response_format = new { type = "json_object" }
        };

        var requestJson = JsonConvert.SerializeObject(requestPayload);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        try
        {
            var response = await _httpClient.PostAsync(BaseUrl, content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var groqResponse = JsonConvert.DeserializeObject<GroqResponse>(responseContent);
            var aiContent = groqResponse?.Choices?.FirstOrDefault()?.Message?.Content;

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

        // Add available professionals to the prompt
        if (availableProfessionals != null && availableProfessionals.Any())
        {
            prompt.AppendLine("Available Professionals:");
            prompt.AppendLine("The following doctors/professionals are available for booking:");
            prompt.AppendLine();
            prompt.AppendLine("=== PROFESSIONAL LIST (USE EXACTLY THESE NAMES) ===");

            foreach (var prof in availableProfessionals)
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

                var title = !string.IsNullOrEmpty(prof.Title) ? prof.Title : "Dr.";
                var specialization = !string.IsNullOrEmpty(prof.Specialization) ? prof.Specialization : "General Practice";
                var qualifications = !string.IsNullOrEmpty(prof.Qualifications) ? prof.Qualifications : "Experienced";

                prompt.AppendLine($"- Doctor Name: {name} (USE THIS EXACT NAME)");
                prompt.AppendLine($"  Title: {title}");
                prompt.AppendLine($"  Specialization: {specialization}");
                prompt.AppendLine($"  Qualifications: {qualifications}");
                if (prof.HourlyRate.HasValue)
                    prompt.AppendLine($"  Rate: ${prof.HourlyRate}/hour");
                if (!string.IsNullOrEmpty(prof.Bio))
                    prompt.AppendLine($"  Bio: {prof.Bio}");
                prompt.AppendLine($"  ID: {prof.UserId}");
                prompt.AppendLine();
            }
            prompt.AppendLine("====================================================");
            prompt.AppendLine("CRITICAL INSTRUCTIONS:");
            prompt.AppendLine("1. USE ONLY the professional names listed above. DO NOT create or hallucinate fake names.");
            prompt.AppendLine("2. When users want to see doctors, your suggestedOptions MUST include BOTH the doctor's name AND their specialization.");
            prompt.AppendLine("3. Format: \"Dr. [Name] - [Specialization]\" or \"[Name] - [Specialization]\"");
            prompt.AppendLine("4. Example: [\"Dr. Sac Ahbdsdi - Cardiology\", \"Dr. Gei sa - Dermiology\"]");
            prompt.AppendLine("5. DO NOT return options like [\"Cardiology\", \"Dermatology\"] or [\"Dr. Cardiology\"] alone.");
            prompt.AppendLine("6. DO NOT create names like \"Dr. John Smith\" or \"Dr. Jane Doe\" unless they are in the list above.");
            prompt.AppendLine("7. Each option must show a specific professional with their ACTUAL name from the list.");
            prompt.AppendLine("8. When a user selects a professional, extract their ID and store it as 'professionalId' in extractedData.");
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
                prompt.AppendLine("Help the user select a professional based on their needs.");
                prompt.AppendLine("Present available professionals with their specialties.");
                prompt.AppendLine("CRITICAL: When generating suggestedOptions for professionals, you MUST include BOTH the doctor's name AND their specialization.");
                prompt.AppendLine("Format: \"Dr. [Name] - [Specialization]\" or \"[Name] - [Specialization]\"");
                prompt.AppendLine("Example suggestedOptions: [\"Dr. John Smith - Cardiology\", \"Dr. Sarah Johnson - Dermatology\", \"Dr. Michael Brown - Pediatrics\"]");
                prompt.AppendLine("DO NOT include only the specialization without the doctor's name.");
                prompt.AppendLine("When user selects a professional, set 'professionalId' in extractedData with the user's ID (e.g., 'professionalId': 'user-id-from-list').");
                prompt.AppendLine("IMPORTANT: After a professional is selected, you MUST set suggestedNextState to 'SelectingDateTime' to ask for date/time.");
                break;
            case ConversationState.SelectingDateTime:
                prompt.AppendLine("Help the user select an available date and time slot.");
                prompt.AppendLine("CRITICAL: You MUST provide date/time options in the suggestedOptions array.");
                prompt.AppendLine("Include multiple date options with specific time slots:");
                prompt.AppendLine($"- Today ({DateTime.Today:MMM dd}): 9:00 AM, 11:00 AM, 2:00 PM, 4:00 PM");
                prompt.AppendLine($"- Tomorrow ({DateTime.Today.AddDays(1):MMM dd}): 9:00 AM, 11:00 AM, 2:00 PM, 4:00 PM");
                prompt.AppendLine($"- Next week ({DateTime.Today.AddDays(7):MMM dd}): 9:00 AM, 11:00 AM, 2:00 PM, 4:00 PM");
                prompt.AppendLine();
                prompt.AppendLine("Example suggestedOptions format:");
                prompt.AppendLine("[\"Today, 9:00 AM\", \"Today, 11:00 AM\", \"Today, 2:00 PM\", \"Tomorrow, 9:00 AM\", \"Tomorrow, 11:00 AM\", \"Tomorrow, 2:00 PM\", \"Next week, 9:00 AM\"]");
                prompt.AppendLine();
                prompt.AppendLine("IMPORTANT: The suggestedOptions MUST contain at least 5-6 specific date/time combinations.");
                prompt.AppendLine("DO NOT ask about availability BEFORE showing the options in suggestedOptions.");
                prompt.AppendLine("Show the options FIRST, then let the user select one.");
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
        prompt.AppendLine("  * SelectingDateTime - User selecting date/time");
        prompt.AppendLine("  * ConfirmingBooking - Confirming booking details");
        prompt.AppendLine("  * BookingComplete - Booking is confirmed and complete");
        prompt.AppendLine("  * FAQ - Answering questions");
        prompt.AppendLine("  * Error - Error state");
        prompt.AppendLine();
        prompt.AppendLine("WARNING: DO NOT use invalid states like 'SelectingNextAction', 'NextStep', etc. Only use the states listed above.");

        return prompt.ToString();
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
            var parsed = JsonConvert.DeserializeObject<AIResponse>(aiContent);
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
        }

        return new LLMResponse
        {
            ResponseText = aiContent,
            SuggestedOptions = new List<string>(),
            DetectedIntent = UserIntent.GeneralInquiry
        };
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

    // Internal classes for JSON deserialization
    private class GroqResponse
    {
        public List<GroqChoice>? Choices { get; set; }
    }

    private class GroqChoice
    {
        public GroqMessage? Message { get; set; }
    }

    private class GroqMessage
    {
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

    public class ProfessionalInfo
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? Title { get; set; }
        public string? Specialization { get; set; }
        public string? Qualifications { get; set; }
        public string? Bio { get; set; }
        public decimal? HourlyRate { get; set; }
        public bool IsAvailable { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
    }
}