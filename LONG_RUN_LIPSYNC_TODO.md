# Long-Running Lip Sync Jobs - Queue-Based API Implementation TODO

**Date Created:** 2025-11-25  
**Status:** Research & Planning  
**Priority:** Medium (Current synchronous approach works for short videos)

---

## Problem Statement

### Current Behavior
- **Synchronous API calls** to `https://fal.run/{model}` with **5-minute timeout**
- Works perfectly for short videos (< 3 minutes)
- **Fails for longer videos** (especially Kling model) with timeout errors
- **Jobs actually complete on fal.ai side** but RightClicks doesn't receive the result

### Evidence from Logs (2025-11-24)

**Kling Failure Example:**
```
2025-11-24 15:10:51.745 [INF] FalAiKlingLipSyncFeature: Starting execution for file: Opening Kling_Good_But.mp4
2025-11-24 15:11:23.066 [INF] Sending POST request to https://fal.run/fal-ai/kling-video/lipsync/audio-to-video...
2025-11-24 15:16:25.082 [INF] Sending POST request to https://fal.run/fal-ai/kling-video/lipsync/audio-to-video...
2025-11-24 15:21:27.114 [INF] Sending POST request to https://fal.run/fal-ai/kling-video/lipsync/audio-to-video...
2025-11-24 15:26:28.566 [ERR] Job failed: "5b8ed4c5-0d10-42b5-9c53-f70d5ac346cd" - Lip Sync > ☁️ fal.ai.Kling $.17/min - Failed to generate lip sync: The request was canceled due to the configured HttpClient.Timeout of 300 seconds elapsing.
```

**Timeline:**
- Started: 15:10:51
- Retry 1: 15:11:23 (POST request)
- Retry 2: 15:16:25 (5 minutes later)
- Retry 3: 15:21:27 (5 minutes later)
- **Failed: 15:26:28** (after 15 minutes 37 seconds total)
- **HttpClient.Timeout: 300 seconds (5 minutes)** - hit on each retry attempt

**User Report:**
> "They 'fail' in the logs but later I find they actually worked over in fal.ai itself. Our RightClicks application does not get a copy of the result but fal.ai does."

### Root Cause Analysis

**Current Implementation:**
- `FalAiService.cs` line 51: `Timeout = TimeSpan.FromMinutes(5)` (300 seconds)
- `FalAiService.cs` line 27-28: `MaxRetries = 3`, `RetryDelayMs = 2000`
- Synchronous endpoint blocks until completion or timeout
- For long videos, fal.ai processing time > 5 minutes
- Retry logic re-sends the POST request (creates new job each time!)
- Each retry hits the 5-minute timeout again

**Why It Fails:**
1. Video processing takes > 5 minutes (especially Kling for long videos)
2. HttpClient times out after 5 minutes
3. Retry logic creates a **new job** instead of checking status of existing job
4. After 3 retries × 5 minutes = 15 minutes, gives up
5. Original job continues processing on fal.ai and completes successfully
6. RightClicks never retrieves the result

---

## Solution: Queue-Based API

### fal.ai Queue API Architecture

**Two API Patterns:**

1. **Synchronous (Current):** `https://fal.run/{model}`
   - Blocks until completion
   - Returns result directly in response
   - ✅ Works for short jobs (< 5 minutes)
   - ❌ Fails for long jobs (> 5 minutes)

2. **Queue-Based (Needed):** `https://queue.fal.run/{model}`
   - Submit job → get `request_id`
   - Poll status endpoint until `COMPLETED`
   - Fetch result from `response_url`
   - ✅ Works for any job length
   - ✅ No timeout issues
   - ✅ Can track progress via logs

### Queue API Workflow

**Step 1: Submit Job**
```bash
POST https://queue.fal.run/fal-ai/kling-video/lipsync/audio-to-video
Authorization: Key {FAL_KEY}
Content-Type: application/json

{
  "video_url": "https://...",
  "audio_url": "https://..."
}
```

**Response:**
```json
{
  "request_id": "3e3e5b55-45fb-4e5c-b4d1-05702dffc8bf",
  "status_url": "https://queue.fal.run/fal-ai/kling-video/lipsync/audio-to-video/requests/{request_id}/status",
  "response_url": "https://queue.fal.run/fal-ai/kling-video/lipsync/audio-to-video/requests/{request_id}",
  "cancel_url": "https://queue.fal.run/fal-ai/kling-video/lipsync/audio-to-video/requests/{request_id}/cancel"
}
```

**Step 2: Poll Status**
```bash
GET https://queue.fal.run/fal-ai/kling-video/lipsync/audio-to-video/requests/{request_id}/status?logs=1
Authorization: Key {FAL_KEY}
```

**Response (In Progress):**
```json
{
  "status": "IN_PROGRESS",
  "request_id": "3e3e5b55-45fb-4e5c-b4d1-05702dffc8bf",
  "logs": [
    {"timestamp": "2024-12-20T15:37:17.120314", "message": "INFO:Processing video...", "level": "INFO"}
  ]
}
```

**Response (Completed):**
```json
{
  "status": "COMPLETED",
  "request_id": "3e3e5b55-45fb-4e5c-b4d1-05702dffc8bf",
  "response_url": "https://queue.fal.run/fal-ai/kling-video/lipsync/audio-to-video/requests/{request_id}"
}
```

**Step 3: Fetch Result**
```bash
GET https://queue.fal.run/fal-ai/kling-video/lipsync/audio-to-video/requests/{request_id}
Authorization: Key {FAL_KEY}
```

**Response:** Same as synchronous endpoint result (FalAiLipSyncResult)

### Alternative: Streaming Status (SSE)
```bash
GET https://queue.fal.run/fal-ai/kling-video/lipsync/audio-to-video/requests/{request_id}/status/stream
Authorization: Key {FAL_KEY}
```
- Server-Sent Events (text/event-stream)
- Real-time status updates
- Connection stays open until COMPLETED
- No polling needed!

---

## Implementation Plan

### Phase 1: Research & Design (CURRENT)
- [x] Analyze log failures
- [x] Research fal.ai queue API documentation
- [x] Identify root cause (timeout + retry creating new jobs)
- [x] Document queue-based workflow
- [ ] Decide on polling vs. streaming approach
- [ ] Design service architecture

### Phase 2: Code Implementation (FUTURE)
- [ ] Create `FalAiQueueService.cs` (queue-based API client)
- [ ] Implement `SubmitJobAsync()` method
- [ ] Implement `PollStatusAsync()` method (with exponential backoff)
- [ ] Implement `FetchResultAsync()` method
- [ ] Add timeout configuration (e.g., 30 minutes for long videos)
- [ ] Add cancellation support

### Phase 3: Integration (FUTURE)
- [ ] Create `FalAiLipSyncQueueFeatureBase.cs` (queue-based base class)
- [ ] Refactor Kling feature to use queue-based API
- [ ] Add configuration option: `UseQueueApi` (bool)
- [ ] Update UI to show progress (via polling logs)
- [ ] Test with long videos (> 5 minutes)

### Phase 4: Testing & Validation (FUTURE)
- [ ] Test Kling with 10-minute video
- [ ] Test Kling with 20-minute video
- [ ] Verify result retrieval after timeout
- [ ] Test cancellation
- [ ] Test error handling (FAILED status)

---

## Existing Code Assets

### Models (Already Exist!)
- ✅ `RightClicks/Models/FalAi/FalAiQueueSubmitResponse.cs` - Submit response model
- ✅ `RightClicks/Models/FalAi/FalAiStatusResponse.cs` - Status polling response model
- ✅ `RightClicks/Models/FalAi/FalAiLipSyncResult.cs` - Final result model (same for both APIs)

### Services (Need Creation)
- ❌ `FalAiQueueService.cs` - Queue-based API client (TO BE CREATED)

### Features (Need Refactoring)
- ✅ `FalAiLipSyncFeatureBase.cs` - Current synchronous base class
- ❌ `FalAiLipSyncQueueFeatureBase.cs` - Queue-based base class (TO BE CREATED)

---

## Technical Considerations

### Polling Strategy
**Option A: Simple Polling (Recommended for MVP)**
- Poll every 5 seconds initially
- Increase to 10 seconds after 1 minute
- Increase to 30 seconds after 5 minutes
- Max timeout: 30 minutes

**Option B: Exponential Backoff**
- Start: 2 seconds
- Double each time: 2s → 4s → 8s → 16s → 32s (max)
- More efficient but harder to predict

**Option C: Server-Sent Events (SSE)**
- Use `/status/stream` endpoint
- Real-time updates
- No polling overhead
- Requires SSE client implementation

### Timeout Configuration
```csharp
public class QueueApiSettings
{
    public int InitialPollDelayMs { get; set; } = 5000; // 5 seconds
    public int MaxPollDelayMs { get; set; } = 30000; // 30 seconds
    public int MaxWaitMinutes { get; set; } = 30; // 30 minutes total
}
```

### Error Handling
- `IN_QUEUE` → Keep polling
- `IN_PROGRESS` → Keep polling, log progress
- `COMPLETED` → Fetch result
- `FAILED` → Return error to user
- Timeout → Cancel job, return error

---

## Decision Points

### When to Use Queue API?
**Option 1: Always use queue API** (Simplest)
- Pro: Consistent behavior for all video lengths
- Pro: No timeout issues ever
- Con: Slightly more complex for short videos

**Option 2: Hybrid approach** (Smartest)
- Use synchronous API for videos < 3 minutes
- Use queue API for videos ≥ 3 minutes
- Pro: Best of both worlds
- Con: More complex logic

**Option 3: Per-model configuration** (Most Flexible)
- Kling: Always use queue API (known to be slow)
- Pixverse/VEED: Use synchronous API (fast enough)
- Pro: Optimized per model
- Con: Requires model-specific knowledge

### Recommendation
**Start with Option 1** (Always use queue API for Kling), then expand to other models if needed.

---

## References

- **fal.ai Queue API Docs:** https://docs.fal.ai/model-apis/model-endpoints/queue
- **Kling Lipsync Model:** https://fal.ai/models/fal-ai/kling-video/lipsync/audio-to-video
- **Current Implementation:** `RightClicks/Services/FalAiService.cs`
- **Feature Base Class:** `RightClicks/Features/Video/FalAiLipSyncFeatureBase.cs`
- **Kling Feature:** `RightClicks/Features/Video/FalAiKlingLipSyncFeature.cs`

---

## Notes

- **DO NOT CHANGE CURRENT IMPLEMENTATION YET** - It works for short videos
- This is a research document for future implementation
- Queue API models already exist in codebase (created during initial development)
- All other lip sync models (Pixverse, VEED, Sync, Creatify) work fine with synchronous API
- Only Kling has timeout issues with long videos

---

**Next Steps:**
1. Review this document with Don
2. Decide on polling strategy (Simple vs. SSE)
3. Decide on when to use queue API (Always vs. Hybrid vs. Per-model)
4. Implement `FalAiQueueService.cs` when ready
5. Test with real long videos

