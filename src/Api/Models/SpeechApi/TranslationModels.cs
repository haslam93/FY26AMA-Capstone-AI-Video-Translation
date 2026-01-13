using System.Text.Json.Serialization;

namespace VideoTranslation.Api.Models.SpeechApi;

/// <summary>
/// Request body for creating a translation via Speech API.
/// PUT /videotranslation/translations/{translationId}
/// </summary>
public class CreateTranslationRequest
{
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("input")]
    public TranslationInput Input { get; set; } = new();
}

public class TranslationInput
{
    [JsonPropertyName("sourceLocale")]
    public string SourceLocale { get; set; } = string.Empty;

    [JsonPropertyName("targetLocale")]
    public string TargetLocale { get; set; } = string.Empty;

    [JsonPropertyName("voiceKind")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VoiceKind { get; set; } = "PlatformVoice";

    [JsonPropertyName("speakerCount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? SpeakerCount { get; set; }

    [JsonPropertyName("subtitleMaxCharCountPerSegment")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? SubtitleMaxCharCountPerSegment { get; set; }

    [JsonPropertyName("exportSubtitleInVideo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool ExportSubtitleInVideo { get; set; } = false;

    [JsonPropertyName("enableLipSync")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool EnableLipSync { get; set; } = false;

    [JsonPropertyName("videoFileUrl")]
    public string VideoFileUrl { get; set; } = string.Empty;
}

/// <summary>
/// Response from creating or getting a translation.
/// </summary>
public class TranslationResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("input")]
    public TranslationInput? Input { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("createdDateTime")]
    public DateTime? CreatedDateTime { get; set; }

    [JsonPropertyName("lastActionDateTime")]
    public DateTime? LastActionDateTime { get; set; }
}
