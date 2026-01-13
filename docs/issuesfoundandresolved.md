# Issues Found and Resolved

## Debugging Session: January 12, 2026

### Context
After deploying the Video Translation Service (Azure Function App + Blazor UI) to Azure, translation jobs were completing in ~3-7 seconds with status "Completed" but no actual translation was happening. The Azure Speech Video Translation API showed no translations were being created.

---

## Issue 1: Speech API Requires Azure Blob Storage SAS URLs

### Symptoms
- CreateTranslationActivity was returning 400 Bad Request
- Logs showed: `Response status code does not indicate success: 400 (Bad Request)`
- Speech API `/translations` endpoint returned empty `{ "value": [] }`

### Root Cause
The Azure Speech Video Translation API **only accepts Azure Blob Storage URLs with SAS tokens**. It does NOT accept arbitrary public URLs like:
- `https://ai.azure.com/speechassetscache/...` (Microsoft's sample video URL)
- Any other public HTTP/HTTPS URL

The API documentation states:
> Translation video file Azure blob url, .mp4 file format

### Solution
Updated `ValidateInputActivity.cs` to:
1. Download external video URLs to our Azure Blob Storage using `CopyFromUrlAsync`
2. Generate SAS URLs for the Speech API to access the video

**File Changed:** `src/Api/Activities/ValidateInputActivity.cs`

```csharp
// OLD: Just used the direct URL
result.VideoFileUrl = job.Request.VideoUrl;

// NEW: Copy to blob storage and generate SAS URL
var blobPath = $"uploads/{job.JobId}/{fileName}";
await _blobService.CopyFromUrlAsync("videos", blobPath, job.Request.VideoUrl);
result.VideoFileUrl = await _blobService.GenerateSasUrlAsync("videos", blobPath, TimeSpan.FromHours(2));
```

---

## Issue 2: C# `required` Keyword Caused Serialization Failures

### Symptoms
- Jobs completed almost instantly (~5 seconds)
- `GetJobStatus` API returned only basic fields (jobId, status, createdAt, lastUpdatedAt)
- `SerializedOutput` was null or couldn't be deserialized
- Error in job output: `"JSON deserialization for type 'VideoTranslation.Api.Models.SpeechApi.TranslationInput' was missing required properties including: 'videoFileUrl'"`

### Root Cause
C# 11's `required` keyword on model properties caused `System.Text.Json` deserialization failures when Durable Functions serialized/deserialized data between the orchestrator and activities.

The `required` keyword tells the C# compiler and JSON deserializer that a property MUST be present. But during Durable Functions replay, the serialization context may differ, causing deserialization to fail.

### Solution
Removed the `required` keyword from all model classes and made properties nullable or gave them default values.

**Files Changed:**
- `src/Api/Models/SpeechApi/TranslationModels.cs`
- `src/Api/Models/TranslationJob.cs`
- `src/Api/Models/JobStatusResponse.cs`
- `src/Api/Activities/CreateTranslationActivity.cs`
- `src/Api/Activities/CreateIterationActivity.cs`
- `src/Api/Activities/GetIterationStatusActivity.cs`
- `src/Api/Activities/CopyOutputsActivity.cs`

**Example Fix:**
```csharp
// OLD
public required string JobId { get; set; }
public required TranslationJobRequest Request { get; set; }

// NEW
public string JobId { get; set; } = string.Empty;
public TranslationJobRequest Request { get; set; } = new();
```

---

## Issue 3: GetInstanceAsync Not Fetching Input/Output

### Symptoms
- Debug endpoint showed `serializedInput: null` and `serializedOutput: null`
- Could not see the actual job state or error messages
- Jobs appeared to complete but we couldn't verify what happened

### Root Cause
The `DurableTaskClient.GetInstanceAsync()` method does not fetch input and output data by default. You must explicitly request it.

### Solution
Added the `getInputsAndOutputs: true` parameter to the GetInstanceAsync call.

**File Changed:** `src/Api/Functions/TranslationFunctions.cs`

```csharp
// OLD
var instance = await durableClient.GetInstanceAsync(jobId);

// NEW
var instance = await durableClient.GetInstanceAsync(jobId, getInputsAndOutputs: true);
```

---

## Issue 4: Missing Error Logging in Speech Service

### Symptoms
- Could not see the actual error message from the Speech API
- Only knew that a 400 error occurred, not why

### Root Cause
The `CreateTranslationAsync` method was calling `response.EnsureSuccessStatusCode()` which throws a generic exception without the response body.

### Solution
Added error response body logging before throwing the exception.

**File Changed:** `src/Api/Services/SpeechTranslationService.cs`

```csharp
var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

if (!response.IsSuccessStatusCode)
{
    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
    _logger.LogError("CreateTranslation failed with status {StatusCode}: {ErrorContent}", 
        response.StatusCode, errorContent);
    response.EnsureSuccessStatusCode();
}
```

---

## Issue 5: Storage Account Identity-Based Authentication (Earlier Issue)

### Symptoms
- Function App returning 503 Server Unavailable
- App couldn't start because it couldn't connect to storage

### Root Cause
The Azure Storage Account had `disableLocalAuth=true`, meaning it only accepts identity-based authentication (Managed Identity with RBAC), not connection strings with access keys.

### Solution
1. Assigned RBAC roles to the Function App's Managed Identity:
   - Storage Blob Data Owner
   - Storage Queue Data Contributor
   - Storage Table Data Contributor

2. Updated Function App settings to use identity-based connection:
   ```
   AzureWebJobsStorage__accountName = storageama3
   AzureWebJobsStorage__blobServiceUri = https://storageama3.blob.core.windows.net
   AzureWebJobsStorage__queueServiceUri = https://storageama3.queue.core.windows.net
   AzureWebJobsStorage__tableServiceUri = https://storageama3.table.core.windows.net
   ```

3. Updated `Program.cs` to use `BlobStorageOptions` with `DefaultAzureCredential`

---

## Current Status (As of January 12, 2026)

| Component | Status | Notes |
|-----------|--------|-------|
| Function App Deployment | ✅ Working | Responds to requests |
| Static Web App (UI) | ✅ Working | Deployed at https://ashy-glacier-0400c0b0f.1.azurestaticapps.net |
| Storage Authentication | ✅ Fixed | Using Managed Identity |
| Translation Creation | ✅ Working | Translations appear in Speech API |
| Serialization | ✅ Fixed | Input/output properly serialized |
| Iteration Creation | ❌ Pending | Failing with 400 error - needs investigation |

---

## Next Steps

1. **Debug Iteration Creation**: The CreateIterationActivity is failing with a 400 Bad Request. Need to:
   - Check the exact error message from the Speech API
   - Compare our iteration request body against the [API documentation](https://learn.microsoft.com/en-us/rest/api/aiservices/videotranslation/iteration-operations/create-iteration)
   - Fix any mismatches in the request format

2. **End-to-End Testing**: Once iteration creation is fixed, run a complete translation test

3. **CI/CD Pipelines**: Create GitHub Actions workflows for automated deployment

---

## Key Learnings

### Azure Speech Video Translation API Requirements
1. Video files **must be in Azure Blob Storage** with SAS tokens - public URLs are not accepted
2. Creating a translation does **NOT** start the translation process
3. You must create an **iteration** to actually start translating
4. API version `2025-05-20` has specific request body requirements

### Durable Functions Best Practices
1. Avoid C# `required` keyword in models used with Durable Functions
2. Use `getInputsAndOutputs: true` when calling `GetInstanceAsync()` if you need the state
3. Always log error response bodies from external APIs for debugging

### Azure Storage with Managed Identity
1. When `disableLocalAuth=true`, you cannot use connection strings with access keys
2. Use `AzureWebJobsStorage__*` app settings pattern for identity-based auth
3. Assign appropriate RBAC roles (Blob Data Owner, Queue/Table Data Contributor)
