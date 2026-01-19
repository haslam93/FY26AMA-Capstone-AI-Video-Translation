using VideoTranslation.Api.Models;
using Xunit;

namespace VideoTranslation.Integration.Tests;

/// <summary>
/// Integration tests for multi-agent validation scenarios.
/// Tests the scoring logic, threshold calculations, and issue aggregation
/// that would occur in a real validation pipeline.
/// </summary>
public class MultiAgentValidationIntegrationTests : IntegrationTestBase
{
    #region Score Calculation Integration Tests

    [Fact]
    public void FullValidationPipeline_HighQualityTranslation_ReturnsApprove()
    {
        // Simulate a high-quality translation validation pipeline
        // Arrange
        var result = new MultiAgentValidationResult
        {
            TranslationReview = new AgentReviewResult
            {
                AgentName = "TranslationReviewAgent",
                AgentType = "translation",
                Score = 92,
                Reasoning = "Excellent semantic accuracy, natural fluency",
                Issues = new List<MultiAgentIssue>
                {
                    new() { Severity = "minor", Category = "translation", Description = "Minor word choice preference" }
                }
            },
            TechnicalReview = new AgentReviewResult
            {
                AgentName = "TechnicalReviewAgent",
                AgentType = "technical",
                Score = 88,
                Reasoning = "Good timing sync, proper CPS rates",
                Issues = new List<MultiAgentIssue>()
            },
            CulturalReview = new AgentReviewResult
            {
                AgentName = "CulturalReviewAgent",
                AgentType = "cultural",
                Score = 85,
                Reasoning = "Culturally appropriate adaptations",
                Issues = new List<MultiAgentIssue>
                {
                    new() { Severity = "suggestion", Category = "cultural", Description = "Consider localizing date format" }
                }
            }
        };

        // Act
        result.CalculateOverallScore();
        result.MergeIssues();

        // Assert
        // Expected: (92 * 0.4) + (88 * 0.3) + (85 * 0.3) = 36.8 + 26.4 + 25.5 = 88.7
        Assert.Equal(88.7, result.OverallScore, precision: 1);
        Assert.Equal("Approve", result.Recommendation);
        Assert.True(result.IsValid);
        Assert.Equal(2, result.AllIssues.Count);
    }

    [Fact]
    public void FullValidationPipeline_MediumQualityTranslation_ReturnsNeedsReview()
    {
        // Simulate a medium-quality translation needing review
        // Arrange
        var result = new MultiAgentValidationResult
        {
            TranslationReview = new AgentReviewResult
            {
                AgentName = "TranslationReviewAgent",
                AgentType = "translation",
                Score = 70,
                Reasoning = "Some inaccuracies in complex sentences",
                Issues = new List<MultiAgentIssue>
                {
                    new() { Severity = "major", Category = "translation", Description = "Idiomatic expression lost" },
                    new() { Severity = "minor", Category = "translation", Description = "Passive voice overused" }
                }
            },
            TechnicalReview = new AgentReviewResult
            {
                AgentName = "TechnicalReviewAgent",
                AgentType = "technical",
                Score = 85,
                Reasoning = "Good technical quality",
                Issues = new List<MultiAgentIssue>()
            },
            CulturalReview = new AgentReviewResult
            {
                AgentName = "CulturalReviewAgent",
                AgentType = "cultural",
                Score = 65,
                Reasoning = "Some cultural references not adapted",
                Issues = new List<MultiAgentIssue>
                {
                    new() { Severity = "major", Category = "cultural", Description = "Cultural reference may confuse audience" }
                }
            }
        };

        // Act
        result.CalculateOverallScore();
        result.MergeIssues();

        // Assert
        // Expected: (70 * 0.4) + (85 * 0.3) + (65 * 0.3) = 28 + 25.5 + 19.5 = 73
        Assert.Equal(73, result.OverallScore);
        Assert.Equal("NeedsReview", result.Recommendation);
        Assert.True(result.IsValid);
        Assert.Equal(3, result.AllIssues.Count);
    }

    [Fact]
    public void FullValidationPipeline_LowQualityTranslation_ReturnsReject()
    {
        // Simulate a low-quality translation that should be rejected
        // Arrange
        var result = new MultiAgentValidationResult
        {
            TranslationReview = new AgentReviewResult
            {
                AgentName = "TranslationReviewAgent",
                AgentType = "translation",
                Score = 35,
                Reasoning = "Significant meaning loss, poor grammar",
                Issues = new List<MultiAgentIssue>
                {
                    new() { Severity = "critical", Category = "translation", Description = "Key message lost in translation" },
                    new() { Severity = "critical", Category = "translation", Description = "Grammar errors throughout" },
                    new() { Severity = "major", Category = "translation", Description = "Wrong terminology used" }
                }
            },
            TechnicalReview = new AgentReviewResult
            {
                AgentName = "TechnicalReviewAgent",
                AgentType = "technical",
                Score = 60,
                Reasoning = "Timing issues in several cues",
                Issues = new List<MultiAgentIssue>
                {
                    new() { Severity = "major", Category = "technical", Description = "CPS exceeds 25 in multiple cues" }
                }
            },
            CulturalReview = new AgentReviewResult
            {
                AgentName = "CulturalReviewAgent",
                AgentType = "cultural",
                Score = 40,
                Reasoning = "Cultural inappropriateness detected",
                Issues = new List<MultiAgentIssue>
                {
                    new() { Severity = "critical", Category = "cultural", Description = "Potentially offensive phrase" }
                }
            }
        };

        // Act
        result.CalculateOverallScore();
        result.MergeIssues();

        // Assert
        // Expected: (35 * 0.4) + (60 * 0.3) + (40 * 0.3) = 14 + 18 + 12 = 44
        Assert.Equal(44, result.OverallScore);
        Assert.Equal("Reject", result.Recommendation);
        Assert.False(result.IsValid);
        Assert.Equal(5, result.AllIssues.Count);
        Assert.Equal(3, result.AllIssues.Count(i => i.Severity == "critical"));
    }

    #endregion

    #region Boundary Threshold Tests

    [Theory]
    [InlineData(80, 80, 80, "Approve")]      // Exactly at Approve threshold
    [InlineData(79.9, 80, 80, "NeedsReview")] // Just below Approve
    [InlineData(50, 50, 50, "NeedsReview")]  // Exactly at NeedsReview lower bound
    [InlineData(49.9, 50, 50, "Reject")]     // Just below NeedsReview
    public void ThresholdBoundaryTests(double transScore, double techScore, double cultScore, string expectedRecommendation)
    {
        // Arrange
        var result = new MultiAgentValidationResult
        {
            TranslationReview = new AgentReviewResult { Score = transScore },
            TechnicalReview = new AgentReviewResult { Score = techScore },
            CulturalReview = new AgentReviewResult { Score = cultScore }
        };

        // Act
        result.CalculateOverallScore();

        // Assert
        Assert.Equal(expectedRecommendation, result.Recommendation);
    }

    #endregion

    #region Issue Aggregation Tests

    [Fact]
    public void IssueAggregation_MaintainsCorrectCategories()
    {
        // Arrange
        var result = new MultiAgentValidationResult
        {
            TranslationReview = new AgentReviewResult
            {
                Issues = new List<MultiAgentIssue>
                {
                    new() { Category = "translation", Description = "Trans issue 1" },
                    new() { Category = "translation", Description = "Trans issue 2" }
                }
            },
            TechnicalReview = new AgentReviewResult
            {
                Issues = new List<MultiAgentIssue>
                {
                    new() { Category = "technical", Description = "Tech issue 1" }
                }
            },
            CulturalReview = new AgentReviewResult
            {
                Issues = new List<MultiAgentIssue>
                {
                    new() { Category = "cultural", Description = "Culture issue 1" },
                    new() { Category = "cultural", Description = "Culture issue 2" },
                    new() { Category = "cultural", Description = "Culture issue 3" }
                }
            }
        };

        // Act
        result.MergeIssues();

        // Assert
        Assert.Equal(6, result.AllIssues.Count);
        Assert.Equal(2, result.AllIssues.Count(i => i.Category == "translation"));
        Assert.Single(result.AllIssues.Where(i => i.Category == "technical"));
        Assert.Equal(3, result.AllIssues.Count(i => i.Category == "cultural"));
    }

    [Fact]
    public void IssueAggregation_SeverityCounts()
    {
        // Arrange
        var result = new MultiAgentValidationResult
        {
            TranslationReview = new AgentReviewResult
            {
                Issues = new List<MultiAgentIssue>
                {
                    new() { Severity = "critical" },
                    new() { Severity = "major" }
                }
            },
            TechnicalReview = new AgentReviewResult
            {
                Issues = new List<MultiAgentIssue>
                {
                    new() { Severity = "minor" },
                    new() { Severity = "minor" }
                }
            },
            CulturalReview = new AgentReviewResult
            {
                Issues = new List<MultiAgentIssue>
                {
                    new() { Severity = "suggestion" }
                }
            }
        };

        // Act
        result.MergeIssues();

        // Assert
        Assert.Single(result.AllIssues.Where(i => i.Severity == "critical"));
        Assert.Single(result.AllIssues.Where(i => i.Severity == "major"));
        Assert.Equal(2, result.AllIssues.Count(i => i.Severity == "minor"));
        Assert.Single(result.AllIssues.Where(i => i.Severity == "suggestion"));
    }

    #endregion

    #region Thread ID Management Tests

    [Fact]
    public void ThreadIdManagement_AllAgentsHaveUniqueThreads()
    {
        // Arrange & Act
        var result = new MultiAgentValidationResult
        {
            OrchestratorThreadId = "thread_orch_123",
            TranslationAgentThreadId = "thread_trans_456",
            TechnicalAgentThreadId = "thread_tech_789",
            CulturalAgentThreadId = "thread_cult_012",
            TranslationReview = new AgentReviewResult { ThreadId = "thread_trans_456" },
            TechnicalReview = new AgentReviewResult { ThreadId = "thread_tech_789" },
            CulturalReview = new AgentReviewResult { ThreadId = "thread_cult_012" }
        };

        // Assert
        var allThreadIds = new[]
        {
            result.OrchestratorThreadId,
            result.TranslationAgentThreadId,
            result.TechnicalAgentThreadId,
            result.CulturalAgentThreadId
        };

        Assert.Equal(4, allThreadIds.Distinct().Count());
        Assert.All(allThreadIds, id => Assert.NotNull(id));
    }

    #endregion
}
