using AutomationApp.Domain.Entity;
using AutomationApp.Domain.Enums;
using AutomationApp.Domain.Interfaces;
using AutomationApp.Repository.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace AutomationApp.Service.Services;

public class BookingAutomationService : IBookingAutomationService
{
    private readonly IBookingDraftRepository _draftRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public BookingAutomationService(
        IBookingDraftRepository draftRepository,
        IConversationRepository conversationRepository,
        IUnitOfWork unitOfWork,
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _draftRepository = draftRepository;
        _conversationRepository = conversationRepository;
        _unitOfWork = unitOfWork;
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<BookingDraft> CreateBookingDraftAsync(Guid conversationId, Guid userId)
    {
        var draft = new BookingDraft
        {
            ConversationId = conversationId,
            UserId = userId,
            Status = BookingDraftStatus.InProgress,
            CreatedAt = DateTime.UtcNow
        };
        return await _draftRepository.AddAsync(draft);
    }

    public async Task<BookingDraft?> GetBookingDraftAsync(Guid draftId)
    {
        return await _draftRepository.GetByIdAsync(draftId);
    }

    public async Task<BookingDraft?> GetBookingDraftByConversationIdAsync(Guid conversationId)
    {
        return await _draftRepository.GetByConversationIdAsync(conversationId);
    }

    public async Task<BookingDraft> UpdateBookingDraftAsync(Guid draftId, Guid? professionalId = null, string? serviceType = null, DateTime? preferredDateTime = null, string? clientNotes = null)
    {
        var draft = await _draftRepository.GetByIdAsync(draftId);
        if (draft == null)
            throw new InvalidOperationException($"Booking draft with id {draftId} not found");

        if (professionalId.HasValue)
            draft.ProfessionalId = professionalId.Value;
        if (serviceType != null)
            draft.ServiceType = serviceType;
        if (preferredDateTime.HasValue)
            draft.PreferredDateTime = preferredDateTime.Value;
        if (clientNotes != null)
            draft.ClientNotes = clientNotes;

        draft.UpdatedAt = DateTime.UtcNow;

        // Check if draft is ready for submission
        if (IsDraftComplete(draft))
        {
            draft.Status = BookingDraftStatus.ReadyForSubmission;
        }

        return await _draftRepository.UpdateAsync(draft);
    }

    public async Task<BookingDraft> SubmitBookingDraftAsync(Guid draftId)
    {
        var draft = await _draftRepository.GetByIdAsync(draftId);
        if (draft == null)
            throw new InvalidOperationException($"Booking draft with id {draftId} not found");

        if (!IsDraftComplete(draft))
            throw new InvalidOperationException("Draft is not complete. Missing required information.");

        draft.Status = BookingDraftStatus.Submitted;

        // Submit to Appointment Service
        var orderId = await SubmitToAppointmentServiceAsync(draft);
        if (orderId.HasValue)
        {
            draft.FinalOrderId = orderId.Value;
            draft.Status = BookingDraftStatus.Completed;
        }

        await _unitOfWork.SaveChangesAsync();
        return draft;
    }

    public async Task<BookingDraft> CancelBookingDraftAsync(Guid draftId)
    {
        var draft = await _draftRepository.GetByIdAsync(draftId);
        if (draft == null)
            throw new InvalidOperationException($"Booking draft with id {draftId} not found");

        draft.Status = BookingDraftStatus.Cancelled;
        draft.UpdatedAt = DateTime.UtcNow;
        return await _draftRepository.UpdateAsync(draft);
    }

    private bool IsDraftComplete(BookingDraft draft)
    {
        return !string.IsNullOrEmpty(draft.ServiceType) &&
               draft.PreferredDateTime.HasValue &&
               draft.ProfessionalId.HasValue;
    }

    private async Task<Guid?> SubmitToAppointmentServiceAsync(BookingDraft draft)
    {
        var appointmentServiceUrl = _configuration["AppointmentService:BaseUrl"] ?? "http://appointment-service:5001";
        var identityServiceUrl = _configuration["IdentityService:BaseUrl"] ?? "http://identity-service:5005";

        try
        {
            // Get auth token
            var token = await GetAuthTokenAsync(identityServiceUrl);
            if (string.IsNullOrEmpty(token))
                return null;

            var orderPayload = new
            {
                clientId = draft.UserId,
                professionalId = draft.ProfessionalId,
                scheduledDateTime = draft.PreferredDateTime,
                durationMinutes = draft.DurationMinutes ?? 60,
                title = draft.ServiceType,
                description = draft.ClientNotes,
                notes = $"Created via AI Automation. Draft ID: {draft.Id}"
            };

            var requestJson = System.Text.Json.JsonSerializer.Serialize(orderPayload);
            var content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var response = await _httpClient.PostAsync($"{appointmentServiceUrl}/api/orders", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<OrderResponse>(responseContent);
                return result?.Id;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error submitting booking: {error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception submitting booking: {ex.Message}");
        }

        return null;
    }

    private Task<string?> GetAuthTokenAsync(string identityServiceUrl)
    {
        // This should use the actual auth flow - for now, return a placeholder
        // In production, this should properly authenticate the automation service
        return Task.FromResult<string?>("automation-service-token");
    }

    public async Task<List<ProfessionalInfo>> GetAvailableProfessionalsAsync()
    {
        var appointmentServiceUrl = _configuration["AppointmentService:BaseUrl"] ?? "http://appointment-service:5001";
        var identityServiceUrl = _configuration["IdentityService:BaseUrl"] ?? "http://identity-service:5005";

        try
        {
            var token = await GetAuthTokenAsync(identityServiceUrl);
            if (string.IsNullOrEmpty(token))
                return new List<ProfessionalInfo>();

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var response = await _httpClient.GetAsync($"{appointmentServiceUrl}/api/professionals?onlyAvailable=true");

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Professionals API Response: {responseContent}");
                var professionals = JsonSerializer.Deserialize<List<JsonElement>>(responseContent);

                if (professionals != null)
                {
                    var result = new List<ProfessionalInfo>();
                    foreach (var prof in professionals)
                    {
                        // Helper function to get property regardless of case
                        string? GetProperty(JsonElement element, string propertyName)
                        {
                            if (element.TryGetProperty(propertyName, out var value))
                                return value.GetString();
                            // Try camelCase
                            var camelCase = char.ToLower(propertyName[0]) + propertyName.Substring(1);
                            if (element.TryGetProperty(camelCase, out var camelValue))
                                return camelValue.GetString();
                            return null;
                        }

                        // Helper function to get nested property
                        string? GetNestedProperty(JsonElement element, string parent, string child)
                        {
                            if (element.TryGetProperty(parent, out var parentElem) && parentElem.ValueKind != JsonValueKind.Null)
                            {
                                if (parentElem.TryGetProperty(child, out var childElem))
                                    return childElem.GetString();
                                // Try camelCase for child
                                var camelChild = char.ToLower(child[0]) + child.Substring(1);
                                if (parentElem.TryGetProperty(camelChild, out var camelChildElem))
                                    return camelChildElem.GetString();
                            }
                            return null;
                        }

                        var firstName = GetNestedProperty(prof, "user", "firstName");
                        var lastName = GetNestedProperty(prof, "user", "lastName");
                        var specialization = GetProperty(prof, "specialization");
                        var title = GetProperty(prof, "title");
                        var userIdStr = GetProperty(prof, "userId");
                        var idStr = GetProperty(prof, "id");

                        Console.WriteLine($"Professional: ID={idStr}, UserID={userIdStr}, FirstName={firstName}, LastName={lastName}, Title={title}, Specialization={specialization}");
                        
                        result.Add(new ProfessionalInfo
                        {
                            Id = Guid.Parse(idStr ?? Guid.Empty.ToString()),
                            UserId = Guid.Parse(userIdStr ?? Guid.Empty.ToString()),
                            Title = title,
                            Specialization = specialization,
                            Qualifications = GetProperty(prof, "qualifications"),
                            Bio = GetProperty(prof, "bio"),
                            HourlyRate = prof.TryGetProperty("hourlyRate", out var rate) && rate.ValueKind != JsonValueKind.Null ? rate.GetDecimal() : null,
                            IsAvailable = prof.TryGetProperty("isAvailable", out var avail) ? avail.GetBoolean() : false,
                            FirstName = firstName,
                            LastName = lastName,
                            Email = GetNestedProperty(prof, "user", "email")
                        });
                    }
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching professionals: {ex.Message}");
        }

        return new List<ProfessionalInfo>();
    }

    public async Task<List<DomainConfigurationInfo>> GetDomainConfigurationsAsync()
    {
        var appointmentServiceUrl = _configuration["AppointmentService:BaseUrl"] ?? "http://appointment-service:5001";
        var identityServiceUrl = _configuration["IdentityService:BaseUrl"] ?? "http://identity-service:5005";

        try
        {
            var token = await GetAuthTokenAsync(identityServiceUrl);
            if (string.IsNullOrEmpty(token))
                return new List<DomainConfigurationInfo>();

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var response = await _httpClient.GetAsync($"{appointmentServiceUrl}/api/domain-configurations?onlyActive=true");

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var configurations = JsonSerializer.Deserialize<List<JsonElement>>(responseContent);

                if (configurations != null)
                {
                    var result = new List<DomainConfigurationInfo>();
                    foreach (var config in configurations)
                    {
                        // Helper function to get property regardless of case
                        string? GetProperty(JsonElement element, string propertyName)
                        {
                            if (element.TryGetProperty(propertyName, out var value))
                                return value.GetString();
                            var camelCase = char.ToLower(propertyName[0]) + propertyName.Substring(1);
                            if (element.TryGetProperty(camelCase, out var camelValue))
                                return camelValue.GetString();
                            return null;
                        }

                        var domainType = config.TryGetProperty("domainType", out var dt) ? dt.GetInt32() : 0;
                        var name = GetProperty(config, "name");
                        var description = GetProperty(config, "description");
                        var duration = config.TryGetProperty("defaultDurationMinutes", out var dur) ? dur.GetInt32() : 60;
                        var idStr = GetProperty(config, "id");

                        Console.WriteLine($"DomainConfig: ID={idStr}, Name={name}, Type={domainType}, Duration={duration}");

                        result.Add(new DomainConfigurationInfo
                        {
                            Id = Guid.Parse(idStr ?? Guid.Empty.ToString()),
                            DomainType = domainType,
                            Name = name ?? "Unknown",
                            Description = description,
                            DefaultDurationMinutes = duration,
                            RequiredFields = null // Could parse this if needed
                        });
                    }
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching domain configurations: {ex.Message}");
        }

        return new List<DomainConfigurationInfo>();
    }

    private class OrderResponse
    {
        public Guid Id { get; set; }
    }
}