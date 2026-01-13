using System.Text.Json.Serialization;

namespace VideoTranslation.Api.Models.SpeechApi;

/// <summary>
/// Request body for creating an iteration.
/// PUT /videotranslation/translations/{translationId}/iterations/{iterationId}
/// </summary>
public class CreateIterationRequest
{
    [JsonPropertyName("input")]
    public IterationInput Input { get; set; } = new();
}

public class IterationInput
{
    [JsonPropertyName("speakerCount")]
    public int? SpeakerCount { get; set; }

    [JsonPropertyName("subtitleMaxCharCountPerSegment")]
    public int? SubtitleMaxCharCountPerSegment { get; set; }

    [JsonPropertyName("exportSubtitleInVideo")]
    public bool? ExportSubtitleInVideo { get; set; }

    /// <summary>
    /// WebVTT file reference (required for iterations after the first).
    /// </summary>
    [JsonPropertyName("webvttFile")]
    public WebvttFileReference? WebvttFile { get; set; }
}

public class WebvttFileReference
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// Response from creating or getting an iteration.
/// </summary>
public class IterationResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("input")]
    public IterationInput? Input { get; set; }

    [JsonPropertyName("result")]
    public IterationResult? Result { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("createdDateTime")]
    public DateTime? CreatedDateTime { get; set; }

    [JsonPropertyName("lastActionDateTime")]
    public DateTime? LastActionDateTime { get; set; }
}

/// <summary>
/// Result of a completed iteration containing output URLs.
/// </summary>
public class IterationResult
{
    [JsonPropertyName("translatedVideoFileUrl")]
    public string? TranslatedVideoFileUrl { get; set; }

    [JsonPropertyName("sourceLocaleSubtitleWebvttFileUrl")]
    public string? SourceLocaleSubtitleWebvttFileUrl { get; set; }

    [JsonPropertyName("targetLocaleSubtitleWebvttFileUrl")]
    public string? TargetLocaleSubtitleWebvttFileUrl { get; set; }

    [JsonPropertyName("metadataJsonWebvttFileUrl")]
    public string? MetadataJsonWebvttFileUrl { get; set; }
}
