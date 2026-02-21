using AutomationApp.Domain.Enums;
using AutomationApp.Domain.Interfaces;

namespace AutomationApp.Service.Services;

public class DataCollectionService : IDataCollectionService
{
    public async Task<Dictionary<string, object>> ExtractBookingInfoAsync(string userMessage, ConversationState currentState)
    {
        var extractedData = new Dictionary<string, object>();

        // Simple extraction logic - in production, use more sophisticated NLP
        var lowerMessage = userMessage.ToLower();

        // Extract service type
        var serviceKeywords = new[] { "haircut", "massage", "consultation", "meeting", "appointment", "session", "health check", "check-up", "routine", "cardiology", "gp", "general practitioner" };
        foreach (var keyword in serviceKeywords)
        {
            if (lowerMessage.Contains(keyword))
            {
                extractedData["serviceType"] = keyword;
                break;
            }
        }

        // Extract date and convert to DateTime
        var datePatterns = new[]
        {
            @"(\d{1,2})[/-](\d{1,2})[/-](\d{2,4})", // MM/DD/YYYY or DD/MM/YYYY
            @"(\d{4})[/-](\d{1,2})[/-](\d{1,2})", // YYYY-MM-DD
            @"tomorrow", @"today"
        };

        DateTime? preferredDateTime = null;

        foreach (var pattern in datePatterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(lowerMessage, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var dateValue = match.Value.ToLower();
                DateTime parsedDate;

                if (dateValue == "today")
                {
                    parsedDate = DateTime.Today;
                }
                else if (dateValue == "tomorrow")
                {
                    parsedDate = DateTime.Today.AddDays(1);
                }
                else if (DateTime.TryParse(dateValue, out parsedDate))
                {
                    // Successfully parsed date
                }
                else
                {
                    // Try MM/DD or DD/MM format
                    if (System.Text.RegularExpressions.Regex.IsMatch(dateValue, @"^\d{1,2}[/-]\d{1,2}[/-]\d{2,4}$"))
                    {
                        var parts = dateValue.Split(new[] { '/', '-' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 3 && int.TryParse(parts[0], out int first) && int.TryParse(parts[1], out int second) && int.TryParse(parts[2], out int year))
                        {
                            // Assume MM/DD format (adjust for your locale)
                            if (year < 100) year += 2000;
                            parsedDate = new DateTime(year, first, second);
                        }
                    }
                }

                if (parsedDate != default)
                {
                    extractedData["preferredDate"] = parsedDate.ToString("yyyy-MM-dd");

                    // Now try to extract time
                    var timePatterns = new[]
                    {
                        @"(\d{1,2}):(\d{2})\s*(am|pm)?", // HH:MM AM/PM
                        @"(\d{1,2})\s*(am|pm)" // HH AM/PM
                    };

                    foreach (var timePattern in timePatterns)
                    {
                        var timeMatch = System.Text.RegularExpressions.Regex.Match(lowerMessage, timePattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (timeMatch.Success)
                        {
                            var timeStr = timeMatch.Value;
                            if (DateTime.TryParse(timeStr, out var time))
                            {
                                preferredDateTime = parsedDate.AddHours(time.Hour).AddMinutes(time.Minute);
                            }
                            else if (System.Text.RegularExpressions.Regex.IsMatch(timeStr, @"(\d{1,2})\s*(am|pm)"))
                            {
                                var hourMatch = System.Text.RegularExpressions.Regex.Match(timeStr, @"(\d{1,2})\s*(am|pm)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                if (hourMatch.Success && int.TryParse(hourMatch.Groups[1].Value, out int hour))
                                {
                                    var isPm = hourMatch.Groups[2].Value.ToLower() == "pm";
                                    if (isPm && hour < 12) hour += 12;
                                    if (!isPm && hour == 12) hour = 0;
                                    preferredDateTime = parsedDate.AddHours(hour);
                                }
                            }
                            break;
                        }
                    }

                    if (preferredDateTime.HasValue)
                    {
                        extractedData["preferredDateTime"] = preferredDateTime.Value;
                    }
                }
                break;
            }
        }

        // Extract notes/preferences
        if (lowerMessage.Contains("note") || lowerMessage.Contains("prefer") || lowerMessage.Contains("request"))
        {
            extractedData["notes"] = userMessage;
        }

        return await Task.FromResult(extractedData);
    }

    public async Task<bool> ValidateBookingInfoAsync(Dictionary<string, object> bookingData)
    {
        // Validate required fields based on current state
        var isValid = true;

        if (bookingData.ContainsKey("serviceType"))
        {
            var serviceType = bookingData["serviceType"]?.ToString();
            if (string.IsNullOrEmpty(serviceType))
                isValid = false;
        }

        if (bookingData.ContainsKey("preferredDate") && bookingData.ContainsKey("preferredTime"))
        {
            var dateStr = bookingData["preferredDate"]?.ToString();
            var timeStr = bookingData["preferredTime"]?.ToString();

            if (string.IsNullOrEmpty(dateStr) || string.IsNullOrEmpty(timeStr))
                isValid = false;
        }

        return await Task.FromResult(isValid);
    }
}