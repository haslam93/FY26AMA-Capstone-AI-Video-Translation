using System.Text.Json.Serialization;

namespace VideoTranslation.Api.Models;

/// <summary>
/// Request model for creating a new translation job.
/// Supports video input via URL, blob path, or direct upload.
/// </summary>
public class TranslationJobRequest
{
    /// <summary>
    /// Display name for the translation job.
    /// </summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Optional description for the translation job.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Direct URL to the video file (must be publicly accessible or SAS URL).
    /// </summary>
    [JsonPropertyName("videoUrl")]
    public string? VideoUrl { get; set; }

    /// <summary>
    /// Blob path in the storage account (e.g., "videos/myvideo.mp4").
    /// </summary>
    [JsonPropertyName("blobPath")]
    public string? BlobPath { get; set; }

    /// <summary>
    /// Source language code (e.g., "es-ES", "en-US").
    /// </summary>
    [JsonPropertyName("sourceLocale")]
    public string SourceLocale { get; set; } = string.Empty;

    /// <summary>
    /// Target language code (e.g., "en-US", "es-ES").
    /// </summary>
    [JsonPropertyName("targetLocale")]
    public string TargetLocale { get; set; } = string.Empty;

    /// <summary>
    /// Voice type: "PlatformVoice" or "PersonalVoice".
    /// </summary>
    [JsonPropertyName("voiceKind")]
    public string VoiceKind { get; set; } = "PlatformVoice";

    /// <summary>
    /// Number of speakers in the video. Null for auto-detection.
    /// </summary>
    [JsonPropertyName("speakerCount")]
    public int? SpeakerCount { get; set; }

    /// <summary>
    /// Maximum characters per subtitle segment.
    /// </summary>
    [JsonPropertyName("subtitleMaxCharCountPerSegment")]
    public int? SubtitleMaxCharCountPerSegment { get; set; }

    /// <summary>
    /// Whether to embed subtitles in the output video.
    /// </summary>
    [JsonPropertyName("exportSubtitleInVideo")]
    public bool ExportSubtitleInVideo { get; set; } = false;

    /// <summary>
    /// Whether to enable lip sync (requires additional processing).
    /// </summary>
    [JsonPropertyName("enableLipSync")]
    public bool EnableLipSync { get; set; } = false;
}
