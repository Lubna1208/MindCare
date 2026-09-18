using System.Net.Http.Json;
using System.Text.Json;

namespace MindCare.Services.AI;

public sealed class GeminiAIService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiAIService> logger) : IAIService
{
    private const int MaximumQuestionLength = 1000;
    private const int MaximumResourceContentLength = 12_000;
    private const int MaximumMatchCandidates = 3;
    private const string GenericFailureMessage = "Sorry, I couldn't generate an answer right now. Please try again.";

    public async Task<string> AskFaqAsync(string question, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("A FAQ question is required.", nameof(question));
        }

        var trimmedQuestion = question.Trim();
        if (trimmedQuestion.Length > MaximumQuestionLength)
        {
            throw new ArgumentException($"A FAQ question must not exceed {MaximumQuestionLength} characters.", nameof(question));
        }

        var responseText = await GenerateContentAsync(new
        {
            systemInstruction = new
            {
                parts = new[]
                {
                    new { text = MindCareFaqKnowledge.SystemInstruction },
                    new { text = MindCareFaqKnowledge.Context }
                }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = trimmedQuestion } }
                }
            }
        }, "FAQ", cancellationToken);

        return responseText ?? GenericFailureMessage;
    }

    public async Task<ResourceSummaryResult> SummarizeResourceAsync(
        string title,
        string content,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("A resource title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Resource content is required.", nameof(content));
        }

        var sourceContent = content.Trim();
        if (sourceContent.Length > MaximumResourceContentLength)
        {
            sourceContent = sourceContent[..MaximumResourceContentLength];
        }

        var responseText = await GenerateContentAsync(new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = MindCareResourceSummaryPrompt.SystemInstruction } }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = $"Resource title:\n{title.Trim()}\n\nResource article:\n{sourceContent}" } }
                }
            },
            generationConfig = new { responseMimeType = "application/json" }
        }, "resource summary", cancellationToken);

        if (string.IsNullOrWhiteSpace(responseText) || !TryParseResourceSummary(responseText, out var summary))
        {
            throw new InvalidOperationException("AI service could not generate a resource summary.");
        }

        return summary;
    }

    public async Task<IReadOnlyDictionary<int, string>> ExplainCounsellorMatchesAsync(
        string concern,
        IReadOnlyList<CounsellorMatchExplanationCandidate> candidates,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(concern))
        {
            throw new ArgumentException("A selected concern is required.", nameof(concern));
        }

        if (candidates.Count == 0)
        {
            return new Dictionary<int, string>();
        }

        var safeCandidates = candidates.Take(MaximumMatchCandidates).ToList();
        var permittedIds = safeCandidates.Select(candidate => candidate.CounsellorProfileId).ToHashSet();
        var responseText = await GenerateContentAsync(new
        {
            systemInstruction = new
            {
                parts = new[]
                {
                    new
                    {
                        text = "You explain why a counsellor's listed professional areas may relate to a concern explicitly selected by a user. " +
                               "Do not diagnose, assess severity, rank counsellors, choose a best counsellor, tell the user whom to choose, make medical or clinical claims, or infer facts not supplied. " +
                               "Use only supplied specialization and availability information. Keep each explanation neutral and concise (one or two short sentences). " +
                               "Return JSON only: {\\\"matches\\\":[{\\\"candidateId\\\":number,\\\"explanation\\\":string}]}. Return entries only for supplied candidate IDs."
                    }
                }
            },
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new
                        {
                            text = JsonSerializer.Serialize(new
                            {
                                concern = concern.Trim(),
                                candidates = safeCandidates.Select(candidate => new
                                {
                                    candidateId = candidate.CounsellorProfileId,
                                    displayName = candidate.CounsellorName,
                                    specialization = candidate.Specialization,
                                    availability = candidate.AvailabilitySummary
                                })
                            })
                        }
                    }
                }
            },
            generationConfig = new { responseMimeType = "application/json" }
        }, "counsellor match explanation", cancellationToken);

        return string.IsNullOrWhiteSpace(responseText)
            ? new Dictionary<int, string>()
            : ParseCounsellorMatchExplanations(responseText, permittedIds);
    }

    private async Task<string?> GenerateContentAsync(object payload, string operation, CancellationToken cancellationToken)
    {
        var apiKey = configuration["AI:Gemini:ApiKey"];
        var model = configuration["AI:Gemini:Model"]?.Trim();
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(model))
        {
            throw new InvalidOperationException("AI service is not configured.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"v1beta/models/{Uri.EscapeDataString(model)}:generateContent");
        request.Headers.Add("x-goog-api-key", apiKey);
        request.Content = JsonContent.Create(payload);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Gemini {Operation} request failed with HTTP status {StatusCode}.", operation, (int)response.StatusCode);
                return null;
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
            return ExtractAnswer(document.RootElement);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Gemini {Operation} request timed out.", operation);
            return null;
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Gemini {Operation} request failed.", operation);
            return null;
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Gemini {Operation} response could not be parsed.", operation);
            return null;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Gemini {Operation} request failed unexpectedly.", operation);
            return null;
        }
    }

    private static string? ExtractAnswer(JsonElement root)
    {
        if (!root.TryGetProperty("candidates", out var candidates) || candidates.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var candidate in candidates.EnumerateArray())
        {
            if (!candidate.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts) ||
                parts.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var text) &&
                    text.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(text.GetString()))
                {
                    return text.GetString()!.Trim();
                }
            }
        }

        return null;
    }

    private static bool TryParseResourceSummary(string responseText, out ResourceSummaryResult summary)
    {
        summary = default!;
        try
        {
            using var document = JsonDocument.Parse(responseText);
            var root = document.RootElement;
            if (!root.TryGetProperty("summary", out var summaryElement) ||
                summaryElement.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(summaryElement.GetString()) ||
                !root.TryGetProperty("keyPoints", out var keyPointsElement) ||
                keyPointsElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            var keyPoints = keyPointsElement.EnumerateArray()
                .Where(point => point.ValueKind == JsonValueKind.String)
                .Select(point => point.GetString()?.Trim())
                .Where(point => !string.IsNullOrWhiteSpace(point))
                .Select(point => point!)
                .Take(5)
                .ToList();

            if (keyPoints.Count < 3)
            {
                return false;
            }

            summary = new ResourceSummaryResult(summaryElement.GetString()!.Trim(), keyPoints);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static IReadOnlyDictionary<int, string> ParseCounsellorMatchExplanations(
        string responseText,
        ISet<int> permittedIds)
    {
        try
        {
            using var document = JsonDocument.Parse(responseText);
            if (!document.RootElement.TryGetProperty("matches", out var matches) || matches.ValueKind != JsonValueKind.Array)
            {
                return new Dictionary<int, string>();
            }

            return matches.EnumerateArray()
                .Where(match => match.TryGetProperty("candidateId", out var id) && id.TryGetInt32(out _) &&
                                match.TryGetProperty("explanation", out var explanation) && explanation.ValueKind == JsonValueKind.String)
                .Select(match => new
                {
                    Id = match.GetProperty("candidateId").GetInt32(),
                    Explanation = match.GetProperty("explanation").GetString()?.Trim()
                })
                .Where(match => permittedIds.Contains(match.Id) && !string.IsNullOrWhiteSpace(match.Explanation))
                .GroupBy(match => match.Id)
                .ToDictionary(group => group.Key, group => group.First().Explanation!);
        }
        catch (JsonException)
        {
            return new Dictionary<int, string>();
        }
    }
}
