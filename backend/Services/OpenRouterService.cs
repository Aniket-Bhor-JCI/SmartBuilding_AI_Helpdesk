using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using backend.Dtos;

namespace backend.Services;

public class OpenRouterService(HttpClient httpClient, IConfiguration configuration) : IOpenRouterService
{
    private static readonly KnowledgeArticle[] KnowledgeBase =
    [
        new("WiFi Troubleshooting", "IT", "Reconnect to building WiFi, restart the device, forget and rejoin the network, and confirm whether nearby users are affected too."),
        new("Printer Recovery", "IT", "Check printer power, toner, paper, and network connection. If shared, confirm the device is online on the office network."),
        new("HVAC Temperature Check", "HVAC", "Confirm thermostat settings, local power, and whether the issue affects one room or a wider area before dispatch."),
        new("Plumbing Leak Response", "Plumbing", "Keep the area clear, avoid using nearby fixtures, and isolate the water source if safe while maintenance is notified."),
        new("Cleaning Spill SOP", "Cleaning", "Mark the affected area, keep foot traffic away, and report the exact location and spill type."),
        new("Access Control Guide", "Security", "Check badge validity, try another approved access point, and confirm whether the issue affects one door or a wider access zone."),
        new("Power Disruption Check", "Electrical", "Confirm whether only one outlet, one room, or the whole area is affected. Avoid unsafe resets if there is odor, heat, or sparking.")
    ];

    private static readonly string[] SafetyKeywords = ["fire", "smoke", "burning", "electrical smell", "gas", "sparks", "flood", "major leak", "alarm", "shock"];

    private static string Prompt =>
        $"""
        You are a smart building helpdesk specialist.

        Your job is to manage user queries for a building helpdesk system.

        You can handle:
        - greeting/general chat
        - troubleshooting requests
        - ticket creation requests
        - ticket status questions
        - unrelated questions by politely redirecting back to building support

        Use the knowledge base below when it is relevant. Prefer these building-specific guides over generic answers.

        Knowledge base:
        {string.Join("\n", KnowledgeBase.Select(article => $"- {article.Title} [{article.Category}]: {article.Guidance}"))}

        Return JSON only with these fields:
        - issue
        - category
        - location
        - priority
        - solution
        - intent: greeting_general_chat | troubleshooting_request | ticket_creation_request | ticket_status_query | unrelated_query
        - confidence: number from 0 to 1
        - shouldOfferTicket: boolean
        - requiresHumanHandoff: boolean
        - handoffReason: string or null
        - botMessage

        Rules:
        - If the user is greeting, respond warmly and briefly.
        - If the user asks something unrelated to building support, explain what you can help with.
        - If the user asks for ticket status, tell them to review My Tickets or the dashboard.
        - If the issue is common, provide a practical suggestion.
        - If the user directly asks to create a ticket, set intent to ticket_creation_request.
        - If the issue appears safety-related, set priority to High, requiresHumanHandoff to true, and shouldOfferTicket to true.
        - Keep botMessage user-facing, concise, and natural.
        - Return JSON only. No markdown.
        """;

    public async Task<ChatAnalysisDto> AnalyzeIssueAsync(string message, CancellationToken cancellationToken)
    {
        var normalizedMessage = message.Trim();
        var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? configuration["OpenRouter:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return BuildFallbackAnalysis(normalizedMessage);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, configuration["OpenRouter:Url"]);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Headers.Add("HTTP-Referer", configuration["Frontend:PublicUrl"] ?? "http://localhost:4200");
            request.Headers.Add("X-Title", "Smart Building Helpdesk");

            var payload = new
            {
                model = configuration["OpenRouter:Model"],
                temperature = 0.2,
                messages = new object[]
                {
                    new { role = "system", content = Prompt },
                    new { role = "user", content = normalizedMessage }
                }
            };

            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            response.EnsureSuccessStatusCode();

            using var outerJson = JsonDocument.Parse(body);
            var content = outerJson.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(content))
            {
                return BuildFallbackAnalysis(normalizedMessage);
            }

            var cleanedJson = content.Trim().Trim('`');
            if (cleanedJson.StartsWith("json", StringComparison.OrdinalIgnoreCase))
            {
                cleanedJson = cleanedJson[4..].Trim();
            }

            var aiResponse = JsonSerializer.Deserialize<AiResponse>(cleanedJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return aiResponse is null
                ? BuildFallbackAnalysis(normalizedMessage)
                : NormalizeResponse(normalizedMessage, aiResponse);
        }
        catch
        {
            return BuildFallbackAnalysis(normalizedMessage);
        }
    }

    private static ChatAnalysisDto NormalizeResponse(string message, AiResponse aiResponse)
    {
        var lowerMessage = message.ToLowerInvariant();
        var issue = string.IsNullOrWhiteSpace(aiResponse.Issue) ? message : aiResponse.Issue.Trim();
        var category = string.IsNullOrWhiteSpace(aiResponse.Category) ? DetectCategory(lowerMessage) : aiResponse.Category.Trim();
        var location = string.IsNullOrWhiteSpace(aiResponse.Location) ? DetectLocation(message) : aiResponse.Location.Trim();
        var priority = NormalizePriority(aiResponse.Priority, lowerMessage);
        var intent = NormalizeIntent(aiResponse.Intent, lowerMessage);
        var confidence = Math.Clamp(aiResponse.Confidence <= 0 ? 0.6 : aiResponse.Confidence, 0.0, 1.0);
        var solution = string.IsNullOrWhiteSpace(aiResponse.Solution) ? DefaultSolution(category) : aiResponse.Solution.Trim();
        var botMessage = string.IsNullOrWhiteSpace(aiResponse.BotMessage) ? solution : aiResponse.BotMessage.Trim();
        var shouldOfferTicket = aiResponse.ShouldOfferTicket;
        var requiresHumanHandoff = aiResponse.RequiresHumanHandoff;
        var handoffReason = string.IsNullOrWhiteSpace(aiResponse.HandoffReason) ? null : aiResponse.HandoffReason.Trim();

        if (IsSafetyRelated(lowerMessage))
        {
            priority = "High";
            requiresHumanHandoff = true;
            shouldOfferTicket = true;
            handoffReason ??= "Potential safety-related incident";
            if (!botMessage.Contains("ticket", StringComparison.OrdinalIgnoreCase))
            {
                botMessage = $"{solution} This may need urgent human follow-up. Do you want me to create a high-priority ticket?";
            }
        }

        if (confidence < 0.4 && intent == "troubleshooting_request")
        {
            requiresHumanHandoff = true;
            shouldOfferTicket = true;
            handoffReason ??= "Low confidence classification";
            if (!botMessage.Contains("ticket", StringComparison.OrdinalIgnoreCase))
            {
                botMessage = $"{solution} I am not fully confident about this issue, so human follow-up is recommended. Do you want me to create a ticket?";
            }
        }

        if (intent == "ticket_status_query")
        {
            shouldOfferTicket = false;
            requiresHumanHandoff = false;
            handoffReason = null;
        }

        if (intent is "greeting_general_chat" or "unrelated_query")
        {
            shouldOfferTicket = false;
            requiresHumanHandoff = false;
            handoffReason = null;
            priority = "Low";
            category = "General";
            location = null;
        }

        if (intent == "ticket_creation_request")
        {
            shouldOfferTicket = true;
            if (!botMessage.Contains("ticket", StringComparison.OrdinalIgnoreCase))
            {
                botMessage = $"{solution} I can create a ticket for this now. Do you want me to continue?";
            }
        }

        return new ChatAnalysisDto(
            Issue: issue,
            Category: category,
            Location: location,
            Priority: priority,
            Solution: solution,
            Intent: intent,
            Confidence: Math.Round(confidence, 2),
            RequiresHumanHandoff: requiresHumanHandoff,
            HandoffReason: handoffReason,
            ShouldOfferTicket: shouldOfferTicket,
            BotMessage: botMessage);
    }

    private static ChatAnalysisDto BuildFallbackAnalysis(string message)
    {
        var lowerMessage = message.ToLowerInvariant();
        var intent = NormalizeIntent(null, lowerMessage);
        var category = DetectCategory(lowerMessage);
        var location = DetectLocation(message);
        var priority = NormalizePriority(null, lowerMessage);
        var solution = DefaultSolution(category);
        var requiresHumanHandoff = IsSafetyRelated(lowerMessage);
        var shouldOfferTicket = intent == "ticket_creation_request" || requiresHumanHandoff || intent == "troubleshooting_request";
        var botMessage = intent switch
        {
            "greeting_general_chat" => "Hello. I can help with WiFi, HVAC, plumbing, cleaning, access, and other building support issues.",
            "ticket_status_query" => "You can review your current tickets in the My Tickets section below. Admins can also open the dashboard for the full queue.",
            "unrelated_query" => "I can help with building support requests such as WiFi, HVAC, plumbing, cleaning, power, and access issues.",
            "ticket_creation_request" => $"{solution} I can create a ticket for this now. Do you want me to continue?",
            _ when requiresHumanHandoff => $"{solution} This may need urgent human follow-up. Do you want me to create a high-priority ticket?",
            _ => $"{solution} Do you want me to create a ticket?"
        };

        return new ChatAnalysisDto(
            Issue: message,
            Category: intent is "greeting_general_chat" or "unrelated_query" or "ticket_status_query" ? "General" : category,
            Location: intent is "greeting_general_chat" or "unrelated_query" or "ticket_status_query" ? null : location,
            Priority: intent is "greeting_general_chat" or "unrelated_query" or "ticket_status_query" ? "Low" : priority,
            Solution: solution,
            Intent: intent,
            Confidence: 0.45,
            RequiresHumanHandoff: requiresHumanHandoff,
            HandoffReason: requiresHumanHandoff ? "Potential safety-related incident" : null,
            ShouldOfferTicket: intent != "greeting_general_chat" && intent != "unrelated_query" && intent != "ticket_status_query" && shouldOfferTicket,
            BotMessage: botMessage);
    }

    private static string NormalizeIntent(string? intent, string lowerMessage)
    {
        var normalized = intent?.Trim().ToLowerInvariant();
        if (normalized is "greeting_general_chat" or "troubleshooting_request" or "ticket_creation_request" or "ticket_status_query" or "unrelated_query")
        {
            return normalized;
        }

        if (lowerMessage is "hello" or "hi" or "hey" || lowerMessage.StartsWith("hello ") || lowerMessage.StartsWith("hi ") || lowerMessage.StartsWith("hey "))
        {
            return "greeting_general_chat";
        }

        if (lowerMessage.Contains("ticket status") || lowerMessage.Contains("my ticket") || lowerMessage.Contains("my tickets") || lowerMessage.Contains("status of ticket"))
        {
            return "ticket_status_query";
        }

        if (lowerMessage.Contains("create a ticket") || lowerMessage.Contains("open a ticket") || lowerMessage.Contains("raise a ticket") || lowerMessage.Contains("log a ticket"))
        {
            return "ticket_creation_request";
        }

        if (LooksLikeIssue(lowerMessage))
        {
            return "troubleshooting_request";
        }

        return "unrelated_query";
    }

    private static string NormalizePriority(string? priority, string lowerMessage)
    {
        var normalized = priority?.Trim();
        if (normalized is "Low" or "Medium" or "High")
        {
            return normalized;
        }

        if (IsSafetyRelated(lowerMessage))
        {
            return "High";
        }

        if (lowerMessage.Contains("not working") || lowerMessage.Contains("broken") || lowerMessage.Contains("down") || lowerMessage.Contains("offline"))
        {
            return "Medium";
        }

        return "Low";
    }

    private static string DetectCategory(string lowerMessage)
    {
        if (lowerMessage.Contains("wifi") || lowerMessage.Contains("network") || lowerMessage.Contains("internet") || lowerMessage.Contains("computer") || lowerMessage.Contains("printer"))
        {
            return "IT";
        }

        if (lowerMessage.Contains("ac") || lowerMessage.Contains("air") || lowerMessage.Contains("cooling") || lowerMessage.Contains("heating") || lowerMessage.Contains("hvac"))
        {
            return "HVAC";
        }

        if (lowerMessage.Contains("leak") || lowerMessage.Contains("pipe") || lowerMessage.Contains("toilet") || lowerMessage.Contains("water") || lowerMessage.Contains("sink"))
        {
            return "Plumbing";
        }

        if (lowerMessage.Contains("dirty") || lowerMessage.Contains("trash") || lowerMessage.Contains("spill") || lowerMessage.Contains("clean"))
        {
            return "Cleaning";
        }

        if (lowerMessage.Contains("door") || lowerMessage.Contains("badge") || lowerMessage.Contains("access"))
        {
            return "Security";
        }

        if (lowerMessage.Contains("power") || lowerMessage.Contains("electrical"))
        {
            return "Electrical";
        }

        return "General";
    }

    private static string? DetectLocation(string message)
    {
        var markers = new[] { "floor", "room", "building", "office", "level", "hall", "block", "wing", "lobby" };
        var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        for (var index = 0; index < parts.Length; index++)
        {
            var token = parts[index].Trim(',', '.', ';', ':').ToLowerInvariant();
            if (!markers.Contains(token))
            {
                continue;
            }

            var start = Math.Max(0, index - 1);
            var end = Math.Min(parts.Length - 1, index + 2);
            return string.Join(' ', parts[start..(end + 1)]);
        }

        return null;
    }

    private static bool IsSafetyRelated(string lowerMessage)
    {
        return SafetyKeywords.Any(lowerMessage.Contains);
    }

    private static bool LooksLikeIssue(string lowerMessage)
    {
        return lowerMessage.Contains("not working")
            || lowerMessage.Contains("broken")
            || lowerMessage.Contains("down")
            || lowerMessage.Contains("offline")
            || lowerMessage.Contains("issue")
            || lowerMessage.Contains("problem")
            || lowerMessage.Contains("fault")
            || lowerMessage.Contains("error")
            || DetectCategory(lowerMessage) is not "General";
    }

    private static string DefaultSolution(string category)
    {
        return KnowledgeBase.FirstOrDefault(item => item.Category.Equals(category, StringComparison.OrdinalIgnoreCase))?.Guidance
            ?? "Please describe the issue and location clearly so the helpdesk can guide you or raise a ticket if needed.";
    }

    private sealed class AiResponse
    {
        public string Issue { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string Solution { get; set; } = string.Empty;
        public string Intent { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public bool ShouldOfferTicket { get; set; }
        public bool RequiresHumanHandoff { get; set; }
        public string? HandoffReason { get; set; }
        public string BotMessage { get; set; } = string.Empty;
    }

    private sealed record KnowledgeArticle(string Title, string Category, string Guidance);
}
