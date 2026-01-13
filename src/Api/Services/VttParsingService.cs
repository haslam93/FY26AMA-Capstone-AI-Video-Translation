// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.RegularExpressions;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace VideoTranslation.Api.Services;

/// <summary>
/// Service for parsing WebVTT (Web Video Text Tracks) subtitle files.
/// </summary>
public interface IVttParsingService
{
    /// <summary>
    /// Parses a WebVTT file content into structured cues.
    /// </summary>
    VttDocument Parse(string vttContent);

    /// <summary>
    /// Downloads and parses a VTT file from a URL.
    /// </summary>
    Task<VttDocument> ParseFromUrlAsync(string url, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a parsed WebVTT document.
/// </summary>
public class VttDocument
{
    /// <summary>
    /// The raw VTT content.
    /// </summary>
    public string RawContent { get; set; } = string.Empty;

    /// <summary>
    /// List of subtitle cues.
    /// </summary>
    public List<VttCue> Cues { get; set; } = new();

    /// <summary>
    /// Total duration of the subtitles.
    /// </summary>
    public TimeSpan TotalDuration => Cues.Count > 0 
        ? Cues.Max(c => c.EndTime) 
        : TimeSpan.Zero;

    /// <summary>
    /// Total word count across all cues.
    /// </summary>
    public int TotalWordCount => Cues.Sum(c => c.WordCount);

    /// <summary>
    /// Average cue duration.
    /// </summary>
    public TimeSpan AverageCueDuration => Cues.Count > 0
        ? TimeSpan.FromMilliseconds(Cues.Average(c => c.Duration.TotalMilliseconds))
        : TimeSpan.Zero;
}

/// <summary>
/// Represents a single subtitle cue in a VTT file.
/// </summary>
public class VttCue
{
    /// <summary>
    /// Optional cue identifier.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Start time of the cue.
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// End time of the cue.
    /// </summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// Duration of the cue.
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// The subtitle text content.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Number of words in the cue.
    /// </summary>
    public int WordCount => string.IsNullOrWhiteSpace(Text) 
        ? 0 
        : Text.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

    /// <summary>
    /// Number of characters in the cue (excluding whitespace).
    /// </summary>
    public int CharacterCount => Text?.Replace(" ", "").Replace("\n", "").Replace("\r", "").Length ?? 0;
}

/// <summary>
/// Implementation of VTT parsing service.
/// Uses Managed Identity to access Azure Blob Storage for private containers.
/// </summary>
public class VttParsingService : IVttParsingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<VttParsingService> _logger;
    private static readonly Regex TimestampRegex = new(
        @"(\d{2}):(\d{2}):(\d{2})\.(\d{3})\s*-->\s*(\d{2}):(\d{2}):(\d{2})\.(\d{3})",
        RegexOptions.Compiled);
    
    // Regex to parse Azure Blob URLs
    private static readonly Regex BlobUrlRegex = new(
        @"https://([^.]+)\.blob\.core\.windows\.net/([^/]+)/(.+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public VttParsingService(HttpClient httpClient, ILogger<VttParsingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public VttDocument Parse(string vttContent)
    {
        var document = new VttDocument
        {
            RawContent = vttContent
        };

        if (string.IsNullOrWhiteSpace(vttContent))
        {
            return document;
        }

        var lines = vttContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var cues = new List<VttCue>();

        int i = 0;

        // Skip WEBVTT header and any metadata
        while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
        {
            i++;
        }

        // Process cues
        while (i < lines.Length)
        {
            // Skip empty lines
            while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i]))
            {
                i++;
            }

            if (i >= lines.Length)
                break;

            var cue = new VttCue();

            // Check if this line is a cue identifier (doesn't contain -->)
            if (!lines[i].Contains("-->"))
            {
                cue.Id = lines[i].Trim();
                i++;
            }

            if (i >= lines.Length)
                break;

            // Parse timestamp line
            var match = TimestampRegex.Match(lines[i]);
            if (match.Success)
            {
                cue.StartTime = new TimeSpan(
                    0,
                    int.Parse(match.Groups[1].Value),
                    int.Parse(match.Groups[2].Value),
                    int.Parse(match.Groups[3].Value),
                    int.Parse(match.Groups[4].Value));

                cue.EndTime = new TimeSpan(
                    0,
                    int.Parse(match.Groups[5].Value),
                    int.Parse(match.Groups[6].Value),
                    int.Parse(match.Groups[7].Value),
                    int.Parse(match.Groups[8].Value));

                i++;

                // Collect text lines until empty line
                var textLines = new List<string>();
                while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
                {
                    textLines.Add(lines[i]);
                    i++;
                }

                cue.Text = string.Join("\n", textLines);
                cues.Add(cue);
            }
            else
            {
                // Skip unrecognized line
                i++;
            }
        }

        document.Cues = cues;
        return document;
    }

    /// <inheritdoc />
    public async Task<VttDocument> ParseFromUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        string content;
        
        // Check if this is an Azure Blob URL
        var blobMatch = BlobUrlRegex.Match(url);
        if (blobMatch.Success)
        {
            // Use Managed Identity to access the blob
            var accountName = blobMatch.Groups[1].Value;
            var containerName = blobMatch.Groups[2].Value;
            var blobPath = blobMatch.Groups[3].Value;
            
            _logger.LogInformation("Downloading VTT from Azure Blob: {Account}/{Container}/{Path}", 
                accountName, containerName, blobPath);
            
            var blobServiceClient = new BlobServiceClient(
                new Uri($"https://{accountName}.blob.core.windows.net"),
                new DefaultAzureCredential());
            
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(blobPath);
            
            var downloadResult = await blobClient.DownloadContentAsync(cancellationToken);
            content = downloadResult.Value.Content.ToString();
        }
        else
        {
            // Use regular HTTP for non-Azure URLs
            _logger.LogInformation("Downloading VTT from HTTP: {Url}", url);
            content = await _httpClient.GetStringAsync(url, cancellationToken);
        }
        
        return Parse(content);
    }
}
