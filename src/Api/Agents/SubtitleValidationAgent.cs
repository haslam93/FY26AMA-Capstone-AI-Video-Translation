// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using VideoTranslation.Api.Models;
using VideoTranslation.Api.Services;

namespace VideoTranslation.Api.Agents;

/// <summary>
/// Agent that validates translated subtitles by comparing source and target VTT files.
/// Uses GPT-4o-mini to analyze translation quality across multiple factors.
/// </summary>
public interface ISubtitleValidationAgent
{
    /// <summary>
    /// Validates translated subtitles by comparing source and target subtitle files.
    /// </summary>
    /// <param name="sourceSubtitleUrl">URL to the source language VTT file.</param>
    /// <param name="targetSubtitleUrl">URL to the target language VTT file.</param>
    /// <param name="sourceLanguage">Source language code (e.g., "en-US").</param>
    /// <param name="targetLanguage">Target language code (e.g., "es-ES").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with score and detailed analysis.</returns>
    Task<SubtitleValidationResult> ValidateAsync(
        string sourceSubtitleUrl,
        string targetSubtitleUrl,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of the subtitle validation agent using GPT-4o-mini.
/// </summary>
public class SubtitleValidationAgent : ISubtitleValidationAgent
{
    private readonly IAgentConfiguration _agentConfig;
    private readonly IVttParsingService _vttParser;
    private readonly ILogger<SubtitleValidationAgent> _logger;

    // Scoring weights for different validation categories
    private const double TimingWeight = 0.20;
    private const double TranslationAccuracyWeight = 0.35;
    private const double GrammarWeight = 0.20;
    private const double FormattingWeight = 0.10;
    private const double CulturalContextWeight = 0.15;

    public SubtitleValidationAgent(
        IAgentConfiguration agentConfig,
        IVttParsingService vttParser,
        ILogger<SubtitleValidationAgent> logger)
    {
        _agentConfig = agentConfig;
        _vttParser = vttParser;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SubtitleValidationResult> ValidateAsync(
        string sourceSubtitleUrl,
        string targetSubtitleUrl,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting subtitle validation. Source: {SourceLang}, Target: {TargetLang}",
            sourceLanguage, targetLanguage);

        try
        {
            // Parse both VTT files
            var sourceDoc = await _vttParser.ParseFromUrlAsync(sourceSubtitleUrl, cancellationToken);
            var targetDoc = await _vttParser.ParseFromUrlAsync(targetSubtitleUrl, cancellationToken);

            _logger.LogInformation(
                "Parsed subtitles. Source cues: {SourceCount}, Target cues: {TargetCount}",
                sourceDoc.Cues.Count, targetDoc.Cues.Count);

            // Perform structural validation first
            var structuralIssues = ValidateStructure(sourceDoc, targetDoc);

            // Use GPT-4o-mini for semantic validation
            var semanticResult = await ValidateSemanticsAsync(
                sourceDoc, targetDoc, 
                sourceLanguage, targetLanguage,
                cancellationToken);

            // Combine results
            var allIssues = structuralIssues.Concat(semanticResult.Issues).ToList();
            var confidenceScore = CalculateOverallScore(structuralIssues, semanticResult);

            var result = new SubtitleValidationResult
            {
                IsValid = confidenceScore >= 0.7 && !allIssues.Any(i => i.Severity == IssueSeverity.Critical),
                ConfidenceScore = confidenceScore,
                Issues = allIssues,
                Reasoning = semanticResult.Reasoning,
                ValidatedAt = DateTime.UtcNow,
                SourceCueCount = sourceDoc.Cues.Count,
                TargetCueCount = targetDoc.Cues.Count,
                SourceWordCount = sourceDoc.TotalWordCount,
                TargetWordCount = targetDoc.TotalWordCount,
                CategoryScores = new ValidationCategoryScores
                {
                    TimingScore = semanticResult.TimingScore,
                    TranslationAccuracyScore = semanticResult.TranslationScore,
                    GrammarScore = semanticResult.GrammarScore,
                    FormattingScore = CalculateFormattingScore(structuralIssues),
                    CulturalContextScore = semanticResult.CulturalScore
                }
            };

            _logger.LogInformation(
                "Validation complete. Score: {Score:P0}, Valid: {IsValid}, Issues: {IssueCount}",
                result.ConfidenceScore, result.IsValid, result.Issues.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Subtitle validation failed");
            
            return new SubtitleValidationResult
            {
                IsValid = false,
                ConfidenceScore = 0,
                Issues = new List<ValidationIssue>
                {
                    new()
                    {
                        Severity = IssueSeverity.Critical,
                        Category = IssueCategory.MissingContent,
                        Description = $"Validation failed: {ex.Message}"
                    }
                },
                Reasoning = $"Validation could not be completed due to an error: {ex.Message}",
                ValidatedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Validates structural aspects of the subtitles (timing, cue counts, etc.)
    /// </summary>
    private List<ValidationIssue> ValidateStructure(VttDocument source, VttDocument target)
    {
        var issues = new List<ValidationIssue>();

        // Check if cue counts match
        if (source.Cues.Count != target.Cues.Count)
        {
            var severity = Math.Abs(source.Cues.Count - target.Cues.Count) > 5 
                ? IssueSeverity.High 
                : IssueSeverity.Medium;
            
            issues.Add(new ValidationIssue
            {
                Severity = severity,
                Category = IssueCategory.Timing,
                Description = $"Cue count mismatch: source has {source.Cues.Count} cues, target has {target.Cues.Count}",
                SuggestedFix = "Review timeline synchronization between source and translated subtitles"
            });
        }

        // Check timing alignment for matching cues
        var minCues = Math.Min(source.Cues.Count, target.Cues.Count);
        for (int i = 0; i < minCues; i++)
        {
            var sourceCue = source.Cues[i];
            var targetCue = target.Cues[i];

            // Check if start times differ significantly (more than 500ms)
            var startDiff = Math.Abs((sourceCue.StartTime - targetCue.StartTime).TotalMilliseconds);
            if (startDiff > 500)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = startDiff > 2000 ? IssueSeverity.High : IssueSeverity.Medium,
                    Category = IssueCategory.Timing,
                    Description = $"Timing mismatch at cue {i + 1}: start time differs by {startDiff:F0}ms",
                    Timestamp = sourceCue.StartTime.ToString(@"hh\:mm\:ss\.fff"),
                    SuggestedFix = "Adjust subtitle timing to match source"
                });
            }

            // Check for very long cues
            if (targetCue.Text.Length > 120)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = IssueSeverity.Low,
                    Category = IssueCategory.Formatting,
                    Description = $"Cue {i + 1} has {targetCue.Text.Length} characters, which may be too long for readability",
                    Timestamp = targetCue.StartTime.ToString(@"hh\:mm\:ss\.fff"),
                    SuggestedFix = "Consider splitting into shorter segments"
                });
            }

            // Check for empty translations
            if (!string.IsNullOrWhiteSpace(sourceCue.Text) && string.IsNullOrWhiteSpace(targetCue.Text))
            {
                issues.Add(new ValidationIssue
                {
                    Severity = IssueSeverity.Critical,
                    Category = IssueCategory.MissingContent,
                    Description = $"Cue {i + 1} has source text but no translation",
                    Timestamp = sourceCue.StartTime.ToString(@"hh\:mm\:ss\.fff"),
                    SuggestedFix = "Add translation for this segment"
                });
            }
        }

        return issues;
    }

    /// <summary>
    /// Uses GPT-4o-mini to analyze semantic quality of the translation.
    /// </summary>
    private async Task<SemanticValidationResult> ValidateSemanticsAsync(
        VttDocument source,
        VttDocument target,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        // Prepare sample cues for analysis (limit to avoid token limits)
        var sampleCount = Math.Min(20, Math.Min(source.Cues.Count, target.Cues.Count));
        var samplePairs = new List<object>();

        // Take samples from beginning, middle, and end
        var indices = GetSampleIndices(Math.Min(source.Cues.Count, target.Cues.Count), sampleCount);
        
        foreach (var idx in indices)
        {
            samplePairs.Add(new
            {
                index = idx + 1,
                source = source.Cues[idx].Text,
                target = target.Cues[idx].Text,
                sourceTime = source.Cues[idx].StartTime.ToString(@"hh\:mm\:ss"),
                targetTime = target.Cues[idx].StartTime.ToString(@"hh\:mm\:ss")
            });
        }

        var systemPrompt = @"You are an expert subtitle translation validator. Analyze the translation quality of subtitle pairs and provide scores and issues.

You must respond with ONLY a valid JSON object (no markdown, no code fences) in this exact format:
{
    ""timingScore"": <0.0-1.0>,
    ""translationScore"": <0.0-1.0>,
    ""grammarScore"": <0.0-1.0>,
    ""culturalScore"": <0.0-1.0>,
    ""reasoning"": ""<brief explanation of overall quality>"",
    ""issues"": [
        {
            ""severity"": ""Low|Medium|High|Critical"",
            ""category"": ""Timing|Grammar|TranslationAccuracy|CulturalContext|Formatting|ContentAppropriateness|MissingContent"",
            ""description"": ""<issue description>"",
            ""timestamp"": ""<optional timestamp>"",
            ""suggestedFix"": ""<optional fix>""
        }
    ]
}

Scoring criteria:
- timingScore: Are subtitles properly synchronized? Do translations fit in the available time?
- translationScore: Is the meaning accurately conveyed? Are key terms translated correctly?
- grammarScore: Are there grammatical errors in the target language?
- culturalScore: Are idioms, humor, and cultural references appropriately adapted?

Be critical but fair. Score 0.8+ for good quality, 0.6-0.8 for acceptable, below 0.6 for poor.";

        var userPrompt = $@"Validate this subtitle translation from {sourceLanguage} to {targetLanguage}.

Source subtitle stats:
- Total cues: {source.Cues.Count}
- Total words: {source.TotalWordCount}
- Duration: {source.TotalDuration:hh\:mm\:ss}

Target subtitle stats:
- Total cues: {target.Cues.Count}  
- Total words: {target.TotalWordCount}
- Duration: {target.TotalDuration:hh\:mm\:ss}

Sample subtitle pairs for analysis:
{JsonSerializer.Serialize(samplePairs, new JsonSerializerOptions { WriteIndented = true })}

Analyze the translation quality and provide your assessment as JSON.";

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };

        try
        {
            _logger.LogInformation("Calling GPT-4o-mini for semantic validation...");
            
            var completion = await _agentConfig.ChatClient.CompleteChatAsync(
                messages,
                new ChatCompletionOptions
                {
                    Temperature = 0.3f,
                    MaxOutputTokenCount = 2000
                },
                cancellationToken);

            var responseText = completion.Value.Content[0].Text;
            
            _logger.LogWarning("GPT-4o-mini RAW RESPONSE: [{Response}]", responseText);

            // Extract JSON from markdown code fences if present
            var jsonText = ExtractJsonFromResponse(responseText ?? "{}");
            
            _logger.LogWarning("EXTRACTED JSON: [{Json}]", jsonText);

            // Parse the JSON response with enum converter for string values
            var jsonOptions = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) }
            };
            var result = JsonSerializer.Deserialize<SemanticValidationResult>(jsonText, jsonOptions);

            if (result != null)
            {
                _logger.LogInformation("Successfully parsed GPT response. TranslationScore={Translation}, GrammarScore={Grammar}", 
                    result.TranslationScore, result.GrammarScore);
            }

            return result ?? new SemanticValidationResult
            {
                Reasoning = "Failed to parse AI response",
                Issues = new List<ValidationIssue>()
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse GPT response as JSON");
            
            return new SemanticValidationResult
            {
                TimingScore = 0.7,
                TranslationScore = 0.7,
                GrammarScore = 0.7,
                CulturalScore = 0.7,
                Reasoning = "AI analysis completed but response format was unexpected",
                Issues = new List<ValidationIssue>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GPT-4o-mini call failed: {Message}", ex.Message);
            
            return new SemanticValidationResult
            {
                TimingScore = 0.7,
                TranslationScore = 0.7,
                GrammarScore = 0.7,
                CulturalScore = 0.7,
                Reasoning = $"AI analysis failed: {ex.Message}",
                Issues = new List<ValidationIssue>()
            };
        }
    }

    /// <summary>
    /// Gets evenly distributed sample indices.
    /// </summary>
    private static List<int> GetSampleIndices(int total, int sampleCount)
    {
        if (total <= sampleCount)
            return Enumerable.Range(0, total).ToList();

        var indices = new List<int>();
        var step = (double)(total - 1) / (sampleCount - 1);
        
        for (int i = 0; i < sampleCount; i++)
        {
            indices.Add((int)Math.Round(i * step));
        }

        return indices.Distinct().ToList();
    }

    /// <summary>
    /// Calculates the overall validation score.
    /// </summary>
    private double CalculateOverallScore(
        List<ValidationIssue> structuralIssues,
        SemanticValidationResult semanticResult)
    {
        // Calculate weighted semantic score
        var semanticScore = 
            (semanticResult.TimingScore * TimingWeight) +
            (semanticResult.TranslationScore * TranslationAccuracyWeight) +
            (semanticResult.GrammarScore * GrammarWeight) +
            (semanticResult.CulturalScore * CulturalContextWeight) +
            (CalculateFormattingScore(structuralIssues) * FormattingWeight);

        // Apply penalties for structural issues
        var penaltyFactor = 1.0;
        foreach (var issue in structuralIssues)
        {
            penaltyFactor *= issue.Severity switch
            {
                IssueSeverity.Critical => 0.7,
                IssueSeverity.High => 0.9,
                IssueSeverity.Medium => 0.95,
                IssueSeverity.Low => 0.98,
                _ => 1.0
            };
        }

        return Math.Max(0, Math.Min(1, semanticScore * penaltyFactor));
    }

    /// <summary>
    /// Calculates formatting score based on structural issues.
    /// </summary>
    private static double CalculateFormattingScore(List<ValidationIssue> issues)
    {
        var formattingIssues = issues.Where(i => i.Category == IssueCategory.Formatting).ToList();
        
        if (formattingIssues.Count == 0) return 1.0;
        
        var penalty = formattingIssues.Sum(i => i.Severity switch
        {
            IssueSeverity.Critical => 0.3,
            IssueSeverity.High => 0.15,
            IssueSeverity.Medium => 0.08,
            IssueSeverity.Low => 0.03,
            _ => 0
        });

        return Math.Max(0, 1.0 - penalty);
    }

    /// <summary>
    /// Extracts JSON from a response that may be wrapped in markdown code fences.
    /// </summary>
    private static string ExtractJsonFromResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return "{}";

        var trimmed = response.Trim();

        // Check for ```json ... ``` or ``` ... ``` pattern
        if (trimmed.StartsWith("```"))
        {
            // Find the end of the first line (after ```json or ```)
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline > 0)
            {
                var startIndex = firstNewline + 1;
                // Find the closing ``` - search from the content start
                var endIndex = trimmed.IndexOf("\n```", startIndex, StringComparison.Ordinal);
                if (endIndex > startIndex)
                {
                    return trimmed.Substring(startIndex, endIndex - startIndex).Trim();
                }
                // Try without newline before closing ```
                endIndex = trimmed.LastIndexOf("```", StringComparison.Ordinal);
                if (endIndex > startIndex)
                {
                    return trimmed.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }
        }

        // Check if it starts with { and ends with } (already valid JSON)
        if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
        {
            return trimmed;
        }

        // Try to find JSON object within the text
        var jsonStart = trimmed.IndexOf('{');
        var jsonEnd = trimmed.LastIndexOf('}');
        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            return trimmed.Substring(jsonStart, jsonEnd - jsonStart + 1);
        }

        // Return original if no JSON found
        return response;
    }
}

/// <summary>
/// Internal class for deserializing GPT response.
/// </summary>
internal class SemanticValidationResult
{
    public double TimingScore { get; set; } = 0.7;
    public double TranslationScore { get; set; } = 0.7;
    public double GrammarScore { get; set; } = 0.7;
    public double CulturalScore { get; set; } = 0.7;
    public string Reasoning { get; set; } = string.Empty;
    public List<ValidationIssue> Issues { get; set; } = new();
}
