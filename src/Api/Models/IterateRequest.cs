using System.Text.Json.Serialization;

namespace VideoTranslation.Api.Models;

/// <summary>
/// Request model for creating a new iteration with an edited WebVTT file.
/// </summary>
public class IterateRequest
{
    /// <summary>
    /// URL to the edited WebVTT file.
    /// Can be a direct URL or a blob path in storage.
    /// </summary>
    [JsonPropertyName("webvttUrl")]
    public string? WebvttUrl { get; set; }

    /// <summary>
    /// Blob path to the edited WebVTT file in storage.
    /// </summary>
    [JsonPropertyName("webvttBlobPath")]
    public string? WebvttBlobPath { get; set; }

    /// <summary>
    /// Optional: Override speaker count for this iteration.
    /// </summary>
    [JsonPropertyName("speakerCount")]
    public int? SpeakerCount { get; set; }

    /// <summary>
    /// Optional: Override max characters per subtitle segment.
    /// </summary>
    [JsonPropertyName("subtitleMaxCharCountPerSegment")]
    public int? SubtitleMaxCharCountPerSegment { get; set; }

    /// <summary>
    /// Optional: Override whether to export subtitles in video.
    /// </summary>
    [JsonPropertyName("exportSubtitleInVideo")]
    public bool? ExportSubtitleInVideo { get; set; }
}
