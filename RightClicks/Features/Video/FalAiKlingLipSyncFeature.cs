using RightClicks.Models;

namespace RightClicks.Features.Video;

/// <summary>
/// Feature to perform AI-powered lip sync using fal.ai Kling Lipsync API.
/// Uses the synchronous endpoint - processing time varies based on video length.
/// Requires fal.ai API key configured in API Config tab.
/// Output file: {original_name}_LIPSYNC.{ext} (next to source file)
/// 
/// Pricing: $0.014 per 5 seconds (~$0.168 per minute) - Most affordable option!
/// 
/// Constraints:
/// - Video: .mp4/.mov, ≤100MB, 2-10s duration, 720p/1080p, width/height 720-1920px
/// - Audio: 2-60s duration, ≤5MB
/// 
/// NOTE: Documentation shows queue-based endpoint (https://queue.fal.run/fal-ai/kling-video/lipsync/audio-to-video),
/// but we're trying synchronous first (https://fal.run/fal-ai/kling-video/lipsync/audio-to-video) based on success
/// with Pixverse and VEED. If this fails with timeout or "endpoint not found" errors, we'll need
/// to implement queue-based logic with status polling.
/// </summary>
public class FalAiKlingLipSyncFeature : FalAiLipSyncFeatureBase
{
    public override string Id => "FalAiKlingLipSync";

    public override string DisplayName => "Lip Sync > ☁️ fal.ai.Kling $.17/min";

    public override string Description => "AI-powered lip sync using fal.ai Kling model (most affordable)";

    protected override string FalAiEndpoint => "fal-ai/kling-video/lipsync/audio-to-video";
}

