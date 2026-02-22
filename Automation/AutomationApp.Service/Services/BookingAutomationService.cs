using AutomationApp.Domain.Entity;
using AutomationApp.Domain.Enums;
using AutomationApp.Domain.Interfaces;
using AutomationApp.Repository.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Globalization;

namespace AutomationApp.Service.Services;

public class BookingAutomationService : IBookingAutomationService
{
    private readonly IBookingDraftRepository _draftRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _memoryCache;
    private readonly NotificationServiceClient _notificationServiceClient;
    private readonly ILogger<BookingAutomationService> _logger;

    private const string AvailableProfessionalsCacheKey = "automation:available-professionals";
    private const string DomainConfigurationsCacheKey = "automation:domain-configurations";

    public BookingAutomationService(
        IBookingDraftRepository draftRepository,
        IConversationRepository conversationRepository,
        IUnitOfWork unitOfWork,
        HttpClient httpClient,
        IConfiguration configuration,
        IMemoryCache memoryCache,
        NotificationServiceClient notificationServiceClient,
        ILogger<BookingAutomationService> logger)
    {
        _draftRepository = draftRepository;
        _conversationRepository = conversationRepository;
        _unitOfWork = unitOfWork;
        _httpClient = httpClient;
        _configuration = configuration;
        _memoryCache = memoryCache;
        _notificationServiceClient = notificationServiceClient;
        _logger = logger;
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

    public async Task<BookingDraft> SubmitBookingDraftAsync(Guid draftId, string? accessToken = null)
    {
        var draft = await _draftRepository.GetByIdAsync(draftId);
        if (draft == null)
            throw new InvalidOperationException($"Booking draft with id {draftId} not found");

        if (!IsDraftComplete(draft))
            throw new InvalidOperationException("Draft is not complete. Missing required information.");

        draft.Status = BookingDraftStatus.Submitted;

        // Submit to Appointment Service
        var result = await SubmitToAppointmentServiceAsync(draft, accessToken);
        if (result.HasValue)
        {
            draft.FinalOrderId = result.Value.OrderId;
            draft.Status = BookingDraftStatus.Completed;

            // Send notification to the doctor
            if (result.Value.DoctorUserId.HasValue && result.Value.ClientName != null)
            {
                await _notificationServiceClient.SendBookingRequestNotificationAsync(
                    result.Value.DoctorUserId.Value,
                    result.Value.ClientName,
                    draft.ServiceType ?? "Consultation",
                    draft.PreferredDateTime ?? DateTime.UtcNow,
                    result.Value.OrderId);
            }
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

    private async Task<(Guid? OrderId, Guid? DoctorUserId, string? ClientName)? > SubmitToAppointmentServiceAsync(BookingDraft draft, string? accessToken)
    {
        var appointmentServiceUrl = _configuration["AppointmentService:BaseUrl"] ?? "http://appointment-service:5001";
        var identityServiceUrl = _configuration["IdentityService:BaseUrl"] ?? "http://identity-service:5005";

        try
        {
            // Get auth token
            var token = accessToken;
            if (string.IsNullOrWhiteSpace(token))
            {
                token = await GetAuthTokenAsync(identityServiceUrl);
            }
            if (string.IsNullOrEmpty(token))
                return null;

            // Get client name from identity service
            var clientName = await GetClientNameAsync(identityServiceUrl, token, draft.UserId);

            // Get doctor's UserId from professional ID
            var doctorUserId = await GetDoctorUserIdAsync(appointmentServiceUrl, token, draft.ProfessionalId ?? Guid.Empty);

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
                return (result?.Id, doctorUserId, clientName);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Error submitting booking: {error}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception submitting booking: {ex.Message}");
        }

        return null;
    }

    private Task<string?> GetAuthTokenAsync(string identityServiceUrl)
    {
        // This should use the actual auth flow - for now, return a placeholder
        // In production, this should properly authenticate the automation service
        return Task.FromResult<string?>("automation-service-token");
    }

    public async Task<List<ProfessionalInfo>> GetAvailableProfessionalsAsync(string? accessToken = null)
    {
        if (_memoryCache.TryGetValue<List<ProfessionalInfo>>(AvailableProfessionalsCacheKey, out var cachedProfessionals) && cachedProfessionals != null)
        {
            return cachedProfessionals;
        }

        var appointmentServiceUrl = _configuration["AppointmentService:BaseUrl"] ?? "http://appointment-service:5001";
        var identityServiceUrl = _configuration["IdentityService:BaseUrl"] ?? "http://identity-service:5005";

        try
        {
            var token = accessToken;
            if (string.IsNullOrWhiteSpace(token))
            {
                token = await GetAuthTokenAsync(identityServiceUrl);
            }
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

                        string? GetAnyNestedProperty(JsonElement element, string[] parents, string child)
                        {
                            foreach (var parent in parents)
                            {
                                var value = GetNestedProperty(element, parent, child);
                                if (!string.IsNullOrWhiteSpace(value))
                                {
                                    return value;
                                }
                            }

                            var topLevel = GetProperty(element, child);
                            return string.IsNullOrWhiteSpace(topLevel) ? null : topLevel;
                        }

                        Guid? GetGuidProperty(JsonElement element, params string[] propertyNames)
                        {
                            foreach (var propertyName in propertyNames)
                            {
                                var stringValue = GetProperty(element, propertyName);
                                if (!string.IsNullOrWhiteSpace(stringValue) && Guid.TryParse(stringValue, out var parsed))
                                {
                                    return parsed;
                                }
                            }

                            return null;
                        }

                        var firstName = GetAnyNestedProperty(prof, new[] { "user", "userInfo", "doctor", "professionalUser" }, "firstName");
                        var lastName = GetAnyNestedProperty(prof, new[] { "user", "userInfo", "doctor", "professionalUser" }, "lastName");
                        var specialization = GetProperty(prof, "specialization");
                        var title = GetProperty(prof, "title");
                        var userId = GetGuidProperty(prof, "userId", "doctorUserId");
                        var professionalId = GetGuidProperty(prof, "id", "professionalId");

                        Console.WriteLine($"Professional: ID={professionalId}, UserID={userId}, FirstName={firstName}, LastName={lastName}, Title={title}, Specialization={specialization}");

                        if (!professionalId.HasValue || !userId.HasValue)
                        {
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                        {
                            var identityUser = await GetUserSummaryAsync(identityServiceUrl, token, userId.Value);
                            if (identityUser != null)
                            {
                                if (string.IsNullOrWhiteSpace(firstName))
                                {
                                    firstName = identityUser.Value.FirstName;
                                }

                                if (string.IsNullOrWhiteSpace(lastName))
                                {
                                    lastName = identityUser.Value.LastName;
                                }
                            }
                        }
                        
                        result.Add(new ProfessionalInfo
                        {
                            Id = professionalId.Value,
                            UserId = userId.Value,
                            Title = title,
                            Specialization = specialization,
                            Qualifications = GetProperty(prof, "qualifications"),
                            Bio = GetProperty(prof, "bio"),
                            HourlyRate = prof.TryGetProperty("hourlyRate", out var rate) && rate.ValueKind != JsonValueKind.Null ? rate.GetDecimal() : null,
                            IsAvailable = prof.TryGetProperty("isAvailable", out var avail) ? avail.GetBoolean() : false,
                            FirstName = firstName,
                            LastName = lastName,
                            Email = GetAnyNestedProperty(prof, new[] { "user", "userInfo", "doctor", "professionalUser" }, "email")
                        });
                    }
                    _memoryCache.Set(AvailableProfessionalsCacheKey, result, TimeSpan.FromSeconds(45));
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

    public async Task<List<DomainConfigurationInfo>> GetDomainConfigurationsAsync(string? accessToken = null)
    {
        if (_memoryCache.TryGetValue<List<DomainConfigurationInfo>>(DomainConfigurationsCacheKey, out var cachedConfigs) && cachedConfigs != null)
        {
            return cachedConfigs;
        }

        var appointmentServiceUrl = _configuration["AppointmentService:BaseUrl"] ?? "http://appointment-service:5001";
        var identityServiceUrl = _configuration["IdentityService:BaseUrl"] ?? "http://identity-service:5005";

        try
        {
            var token = accessToken;
            if (string.IsNullOrWhiteSpace(token))
            {
                token = await GetAuthTokenAsync(identityServiceUrl);
            }
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
                    _memoryCache.Set(DomainConfigurationsCacheKey, result, TimeSpan.FromSeconds(90));
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

    public async Task<List<AvailableSlotInfo>> GetAvailableSlotsAsync(Guid professionalId, DateTime date, string? accessToken = null)
    {
        var appointmentServiceUrl = _configuration["AppointmentService:BaseUrl"] ?? "http://appointment-service:5001";
        var identityServiceUrl = _configuration["IdentityService:BaseUrl"] ?? "http://identity-service:5005";

        try
        {
            var token = accessToken;
            if (string.IsNullOrWhiteSpace(token))
            {
                token = await GetAuthTokenAsync(identityServiceUrl);
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                return new List<AvailableSlotInfo>();
            }

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var response = await _httpClient.GetAsync($"{appointmentServiceUrl}/api/availabilities/slots/{professionalId}?date={date:yyyy-MM-dd}");
            if (!response.IsSuccessStatusCode)
            {
                return new List<AvailableSlotInfo>();
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var slots = JsonSerializer.Deserialize<List<JsonElement>>(responseContent);
            if (slots == null || slots.Count == 0)
            {
                return new List<AvailableSlotInfo>();
            }

            var result = new List<AvailableSlotInfo>();
            foreach (var slot in slots)
            {
                var isAvailable = slot.TryGetProperty("isAvailable", out var availableProp) && availableProp.GetBoolean();
                if (!isAvailable)
                {
                    continue;
                }

                var startTimeText = slot.TryGetProperty("startTime", out var startProp) ? startProp.GetString() : null;
                var endTimeText = slot.TryGetProperty("endTime", out var endProp) ? endProp.GetString() : null;
                var slotDateText = slot.TryGetProperty("slotDate", out var dateProp) ? dateProp.GetString() : null;

                if (string.IsNullOrWhiteSpace(startTimeText) || !TimeSpan.TryParse(startTimeText, CultureInfo.InvariantCulture, out var startTime))
                {
                    continue;
                }

                var endTime = TimeSpan.Zero;
                if (!string.IsNullOrWhiteSpace(endTimeText))
                {
                    TimeSpan.TryParse(endTimeText, CultureInfo.InvariantCulture, out endTime);
                }

                var slotDate = date.Date;
                if (!string.IsNullOrWhiteSpace(slotDateText) && DateTime.TryParse(slotDateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedSlotDate))
                {
                    slotDate = parsedSlotDate.Date;
                }

                var label = DateTime.Today.Add(startTime).ToString("hh:mm tt", CultureInfo.InvariantCulture);
                result.Add(new AvailableSlotInfo
                {
                    SlotDate = slotDate,
                    StartTime = startTime,
                    EndTime = endTime,
                    DisplayLabel = label
                });
            }

            return result
                .OrderBy(s => s.StartTime)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available slots for professional {ProfessionalId} on {Date}", professionalId, date);
            return new List<AvailableSlotInfo>();
        }
    }

    private async Task<string?> GetClientNameAsync(string identityServiceUrl, string token, Guid userId)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var response = await _httpClient.GetAsync($"{identityServiceUrl}/api/users/{userId}");

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (user.TryGetProperty("firstName", out var firstName) && user.TryGetProperty("lastName", out var lastName))
                {
                    return $"{firstName.GetString()} {lastName.GetString()}";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching client name: {ex.Message}");
        }

        return "Patient";
    }

    private async Task<Guid?> GetDoctorUserIdAsync(string appointmentServiceUrl, string token, Guid professionalId)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var response = await _httpClient.GetAsync($"{appointmentServiceUrl}/api/professionals/{professionalId}");

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var professional = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (professional.TryGetProperty("userId", out var userIdProp))
                {
                    var userIdStr = userIdProp.GetString();
                    if (Guid.TryParse(userIdStr, out var userId))
                    {
                        return userId;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching doctor user ID: {ex.Message}");
        }

        return null;
    }

    private async Task<(string FirstName, string LastName)? > GetUserSummaryAsync(string identityServiceUrl, string token, Guid userId)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var response = await _httpClient.GetAsync($"{identityServiceUrl}/api/users/{userId}");
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var user = JsonSerializer.Deserialize<JsonElement>(content);

            string? firstName = null;
            string? lastName = null;

            if (user.TryGetProperty("firstName", out var fn) && fn.ValueKind == JsonValueKind.String)
            {
                firstName = fn.GetString();
            }

            if (user.TryGetProperty("lastName", out var ln) && ln.ValueKind == JsonValueKind.String)
            {
                lastName = ln.GetString();
            }

            if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
            {
                return null;
            }

            return (firstName ?? string.Empty, lastName ?? string.Empty);
        }
        catch
        {
            return null;
        }
    }

    private class OrderResponse
    {
        public Guid Id { get; set; }
    }
}