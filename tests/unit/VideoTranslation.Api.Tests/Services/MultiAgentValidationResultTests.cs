using VideoTranslation.Api.Models;
using Xunit;

namespace VideoTranslation.Api.Tests.Services;

/// <summary>
/// Unit tests for MultiAgentValidationResult and related models.
/// Tests score calculation, threshold logic, and issue merging.
/// </summary>
public class MultiAgentValidationResultTests
{
    #region CalculateOverallScore Tests

    [Fact]
    public void CalculateOverallScore_WithAllAgentScores_ReturnsWeightedAverage()
    {
        // Arrange
        var result = new MultiAgentValidationResult
        {
            TranslationReview = new AgentReviewResult { Score = 80 },  // 40% weight
            TechnicalReview = new AgentReviewResult { Score = 90 },    // 30% weight
            CulturalReview = new AgentReviewResult { Score = 70 }      // 30% weight
        };

        // Act
        result.CalculateOverallScore();

        // Assert
        // Expected: (80 * 0.4) + (90 * 0.3) + (70 * 0.3) = 32 + 27 + 21 = 80
        Assert.Equal(80, result.OverallScore);
    }

    [Fact]
    public void CalculateOverallScore_ReturnsApprove_WhenScoreIs80()
    {
        // Arrange
        var result = new MultiAgentValidationResult
        {
            TranslationReview = new AgentReviewResult { Score = 80 },
            TechnicalReview = new AgentReviewResult { Score = 80 },
            CulturalReview = new AgentReviewResult { Score = 80 }
        };

        // Act
        result.CalculateOverallScore();

        // Assert
        Assert.Equal(80, result.OverallScore);
        Assert.Equal("Approve", result.Recommendation);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CalculateOverallScore_ReturnsApprove_WhenScoreAbove80()
    {
        // Arrange
        var result = new MultiAgentValidationResult
        {
            TranslationReview = new AgentReviewResult { Score = 95 },
            TechnicalReview = new AgentReviewResult { Score = 90 },
            CulturalReview = new AgentReviewResult { Score = 85 }
        };

        // Act
        result.CalculateOverallScore();

        // Assert
        // Expected: (95 * 0.4) + (90 * 0.3) + (85 * 0.3) = 38 + 27 + 25.5 = 90.5
        Assert.Equal(90.5, result.OverallScore);
        Assert.Equal("Approve", result.Recommendation);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CalculateOverallScore_ReturnsNeedsReview_WhenScoreIs79()
    {
        // Arrange
        var result = new MultiAgentValidationResult
        {
            TranslationReview = new AgentReviewResult { Score = 79 },
            TechnicalReview = new AgentReviewResult { Score = 79 },
            CulturalReview = new AgentReviewResult { Score = 79 }
        };

        // Act
        result.CalculateOverallScore();

        // Assert
        Assert.Equal(79, result.OverallScore);
        Assert.Equal("NeedsReview", result.Recommendation);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CalculateOverallScore_ReturnsNeedsReview_WhenScoreIs50()
    {
        // Arrange
        var result = new MultiAgentValidationResult
        {
            TranslationReview = new AgentReviewResult { Score = 50 },
            TechnicalReview = new AgentReviewResult { Score = 50 },
            CulturalReview = new AgentReviewResult { Score = 50 }
        };

        // Act
        result.CalculateOverallScore();

        // Assert
        Assert.Equal(50, result.OverallScore);
        Assert.Equal("NeedsReview", result.Recommendation);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CalculateOverallScore_ReturnsReject_WhenScoreIs49()
    {
        // Arrange
        var result = new MultiAgentValidationResult
        {
            TranslationReview = new AgentReviewResult { Score = 49 },
            TechnicalReview = new AgentReviewResult { Score = 49 },
            CulturalReview = new AgentReviewResult { Score = 49 }
        };

        // Act
        result.CalculateOverallScore();

        // Assert
        Assert.Equal(49, result.OverallScore);
        Assert.Equal("Reject", result.Recommendation);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void CalculateOverallScore_ReturnsReject_WhenScoreIsZero()
    {
        // Arrange
        var result = new MultiAgentValidationResult
        {
            TranslationReview = new AgentReviewResult { Score = 0 },
            TechnicalReview = new AgentReviewResult { Score = 0 },
            CulturalReview = new AgentReviewResult { Score = 0 }
        };

        // Act
        result.CalculateOverallScore();

        // Assert
        Assert.Equal(0, result.OverallScore);
        Assert.Equal("Reject", result.Recommendation);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void CalculateOverallScore_WithNullAgentReviews_TreatsAsZero()
    {
        // Arrange
        var result = new MultiAgentValidationResult
        {
            TranslationReview = null,
            TechnicalReview = null,
            CulturalReview = null
        };

        // Act
        result.CalculateOverallScore();

        // Assert
        Assert.Equal(0, result.OverallScore);
        Assert.Equal("Reject", result.Recommendation);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void CalculateOverallScore_WithMixedNullAndValues_CalculatesCorrectly()
    {
        // Arrange
        var result = new MultiAgentValidationResult
        {
            TranslationReview = new AgentReviewResult { Score = 100 },  // 40% = 40
            TechnicalReview = null,                                     // 30% = 0
            CulturalReview = new AgentReviewResult { Score = 100 }      // 30% = 30
        };

        // Act
        result.CalculateOverallScore();

        // Assert
        // Expected: (100 * 0.4) + (0 * 0.3) + (100 * 0.3) = 40 + 0 + 30 = 70
        Assert.Equal(70, result.OverallScore);
        Assert.Equal("NeedsReview", result.Recommendation);
    }

    [Theory]
    [InlineData(80, 80, 80, 80, "Approve")]
    [InlineData(100, 100, 100, 100, "Approve")]
    [InlineData(79, 79, 79, 79, "NeedsReview")]
    [InlineData(50, 50, 50, 50, "NeedsReview")]
    [InlineData(49, 49, 49, 49, "Reject")]
    [InlineData(0, 0, 0, 0, "Reject")]
    public void CalculateOverallScore_ThresholdBoundaries_ReturnsCorrectRecommendation(
        double translationScore, double technicalScore, double culturalScore,
        double expectedOverall, string expectedRecommendation)
    {
        // Arrange
        var result = new MultiAgentValidationResult
        {
            TranslationReview = new AgentReviewResult { Score = translationScore },
            TechnicalReview = new AgentReviewResult { Score = technicalScore },
            CulturalReview = new AgentReviewResult { Score = culturalScore }
        };

        // Act
        result.CalculateOverallScore();

        // Assert
        Assert.Equal(expectedOverall, result.OverallScore);
        Assert.Equal(expectedRecommendation, result.Recommendation);
    }

    #endregion

    #region MergeIssues Tests

    [Fact]
    public void MergeIssues_CombinesIssuesFromAllAgents()
    {
        // Arrange
        var result = new MultiAgentValidationResult
        {
            TranslationReview = new AgentReviewResult
            {
                Issues = new List<MultiAgentIssue>
                {
                    new() { Category = "translation", Description = "Issue 1" },
                    new() { Category = "translation", Description = "Issue 2" }
                }
            },
            TechnicalReview = new AgentReviewResult
            {
                Issues = new List<MultiAgentIssue>
                {
                    new() { Category = "technical", Description = "Issue 3" }
                }
            },
            CulturalReview = new AgentReviewResult
            {
                Issues = new List<MultiAgentIssue>
                {
                    new() { Category = "cultural", Description = "Issue 4" },
                    new() { Category = "cultural", Description = "Issue 5" }
                }
            }
        };

        // Act
        result.MergeIssues();

        // Assert
        Assert.Equal(5, result.AllIssues.Count);
        Assert.Equal(2, result.AllIssues.Count(i => i.Category == "translation"));
        Assert.Single(result.AllIssues.Where(i => i.Category == "technical"));
        Assert.Equal(2, result.AllIssues.Count(i => i.Category == "cultural"));
    }

    [Fact]
    public void MergeIssues_HandlesNullAgentReviews()
    {
        // Arrange
        var result = new MultiAgentValidationResult
        {
            TranslationReview = null,
            TechnicalReview = new AgentReviewResult
            {
                Issues = new List<MultiAgentIssue>
                {
                    new() { Category = "technical", Description = "Issue 1" }
                }
            },
            CulturalReview = null
        };

        // Act
        result.MergeIssues();

        // Assert
        Assert.Single(result.AllIssues);
        Assert.Equal("technical", result.AllIssues[0].Category);
    }

    [Fact]
    public void MergeIssues_HandlesNullIssuesLists()
    {
        // Arrange
        var result = new MultiAgentValidationResult
        {
            TranslationReview = new AgentReviewResult { Issues = null! },
            TechnicalReview = new AgentReviewResult { Issues = new List<MultiAgentIssue>() },
            CulturalReview = new AgentReviewResult
            {
                Issues = new List<MultiAgentIssue>
                {
                    new() { Category = "cultural", Description = "Issue 1" }
                }
            }
        };

        // Act
        result.MergeIssues();

        // Assert
        Assert.Single(result.AllIssues);
    }

    [Fact]
    public void MergeIssues_ClearsExistingIssuesBeforeMerging()
    {
        // Arrange
        var result = new MultiAgentValidationResult
        {
            AllIssues = new List<MultiAgentIssue>
            {
                new() { Description = "Pre-existing issue" }
            },
            TranslationReview = new AgentReviewResult
            {
                Issues = new List<MultiAgentIssue>
                {
                    new() { Description = "New issue" }
                }
            }
        };

        // Act
        result.MergeIssues();

        // Assert
        Assert.Single(result.AllIssues);
        Assert.Equal("New issue", result.AllIssues[0].Description);
    }

    [Fact]
    public void MergeIssues_WithEmptyAgentIssues_ReturnsEmptyList()
    {
        // Arrange
        var result = new MultiAgentValidationResult
        {
            TranslationReview = new AgentReviewResult { Issues = new List<MultiAgentIssue>() },
            TechnicalReview = new AgentReviewResult { Issues = new List<MultiAgentIssue>() },
            CulturalReview = new AgentReviewResult { Issues = new List<MultiAgentIssue>() }
        };

        // Act
        result.MergeIssues();

        // Assert
        Assert.Empty(result.AllIssues);
    }

    #endregion

    #region AgentReviewResult Tests

    [Fact]
    public void AgentReviewResult_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var review = new AgentReviewResult();

        // Assert
        Assert.Equal(string.Empty, review.AgentName);
        Assert.Equal(string.Empty, review.AgentType);
        Assert.Equal(0, review.Score);
        Assert.Equal(string.Empty, review.Reasoning);
        Assert.NotNull(review.Issues);
        Assert.Empty(review.Issues);
        Assert.Null(review.ThreadId);
    }

    [Fact]
    public void AgentReviewResult_CanSetAllProperties()
    {
        // Arrange
        var issues = new List<MultiAgentIssue>
        {
            new() { Description = "Test issue" }
        };

        // Act
        var review = new AgentReviewResult
        {
            AgentName = "TranslationReviewAgent",
            AgentType = "translation",
            Score = 85.5,
            Reasoning = "Good translation quality",
            Issues = issues,
            ThreadId = "thread-123"
        };

        // Assert
        Assert.Equal("TranslationReviewAgent", review.AgentName);
        Assert.Equal("translation", review.AgentType);
        Assert.Equal(85.5, review.Score);
        Assert.Equal("Good translation quality", review.Reasoning);
        Assert.Single(review.Issues);
        Assert.Equal("thread-123", review.ThreadId);
    }

    #endregion

    #region MultiAgentIssue Tests

    [Fact]
    public void MultiAgentIssue_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var issue = new MultiAgentIssue();

        // Assert
        Assert.Equal("minor", issue.Severity);
        Assert.Equal(string.Empty, issue.Category);
        Assert.Equal(string.Empty, issue.Description);
        Assert.Null(issue.Location);
        Assert.Null(issue.Suggestion);
    }

    [Fact]
    public void MultiAgentIssue_CanSetAllProperties()
    {
        // Act
        var issue = new MultiAgentIssue
        {
            Severity = "critical",
            Category = "translation",
            Description = "Meaning lost in translation",
            Location = "Cue 5, 00:01:23.000",
            Suggestion = "Use 'greeting' instead of 'hello'"
        };

        // Assert
        Assert.Equal("critical", issue.Severity);
        Assert.Equal("translation", issue.Category);
        Assert.Equal("Meaning lost in translation", issue.Description);
        Assert.Equal("Cue 5, 00:01:23.000", issue.Location);
        Assert.Equal("Use 'greeting' instead of 'hello'", issue.Suggestion);
    }

    [Theory]
    [InlineData("critical")]
    [InlineData("major")]
    [InlineData("minor")]
    [InlineData("suggestion")]
    public void MultiAgentIssue_SeverityLevels_AreValid(string severity)
    {
        // Arrange & Act
        var issue = new MultiAgentIssue { Severity = severity };

        // Assert
        Assert.Equal(severity, issue.Severity);
    }

    [Theory]
    [InlineData("translation")]
    [InlineData("technical")]
    [InlineData("cultural")]
    public void MultiAgentIssue_Categories_AreValid(string category)
    {
        // Arrange & Act
        var issue = new MultiAgentIssue { Category = category };

        // Assert
        Assert.Equal(category, issue.Category);
    }

    #endregion

    #region MultiAgentValidationResult Default Values Tests

    [Fact]
    public void MultiAgentValidationResult_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var result = new MultiAgentValidationResult();

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(0, result.OverallScore);
        Assert.Equal("NeedsReview", result.Recommendation);
        Assert.Equal(string.Empty, result.Summary);
        Assert.Null(result.TranslationReview);
        Assert.Null(result.TechnicalReview);
        Assert.Null(result.CulturalReview);
        Assert.Null(result.OrchestratorThreadId);
        Assert.Null(result.TranslationAgentThreadId);
        Assert.Null(result.TechnicalAgentThreadId);
        Assert.Null(result.CulturalAgentThreadId);
        Assert.NotNull(result.AllIssues);
        Assert.Empty(result.AllIssues);
    }

    [Fact]
    public void MultiAgentValidationResult_CanSetAllThreadIds()
    {
        // Act
        var result = new MultiAgentValidationResult
        {
            OrchestratorThreadId = "orch-thread-1",
            TranslationAgentThreadId = "trans-thread-1",
            TechnicalAgentThreadId = "tech-thread-1",
            CulturalAgentThreadId = "cult-thread-1"
        };

        // Assert
        Assert.Equal("orch-thread-1", result.OrchestratorThreadId);
        Assert.Equal("trans-thread-1", result.TranslationAgentThreadId);
        Assert.Equal("tech-thread-1", result.TechnicalAgentThreadId);
        Assert.Equal("cult-thread-1", result.CulturalAgentThreadId);
    }

    #endregion
}
