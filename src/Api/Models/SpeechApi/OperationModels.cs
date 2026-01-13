using System.Text.Json.Serialization;

namespace VideoTranslation.Api.Models.SpeechApi;

/// <summary>
/// Response from checking operation status.
/// GET /videotranslation/operations/{operationId}
/// </summary>
public class OperationResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

/// <summary>
/// Status values returned by the Speech API.
/// </summary>
public static class SpeechApiStatus
{
    public const string NotStarted = "NotStarted";
    public const string Running = "Running";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";

    public static bool IsTerminal(string? status)
    {
        return status == Succeeded || status == Failed;
    }

    public static bool IsSuccess(string? status)
    {
        return status == Succeeded;
    }
}
