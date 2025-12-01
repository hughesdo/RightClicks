using RightClicks.Models;

namespace RightClicks.Features.Video;

/// <summary>
/// Feature to perform AI-powered lip sync using fal.ai VEED Lipsync API.
/// Uses the synchronous endpoint - processing typically takes 1-3 minutes.
/// Requires fal.ai API key configured in API Config tab.
/// Output file: {original_name}_LIPSYNC.{ext} (next to source file)
/// 
/// Pricing: $0.40 per minute of video (2x more expensive than Pixverse)
/// 
/// NOTE: Documentation shows queue-based endpoint (https://queue.fal.run/veed/lipsync),
/// but we're trying synchronous first (https://fal.run/veed/lipsync) based on success
/// with Pixverse. If this fails with timeout or "endpoint not found" errors, we'll need
/// to implement queue-based logic with status polling.
/// </summary>
public class FalAiVeedLipSyncFeature : FalAiLipSyncFeatureBase
{
    public override string Id => "FalAiVeedLipSync";

    public override string DisplayName => "Lip Sync > ☁️ fal.ai.VEED $.40/min";

    public override string Description => "AI-powered lip sync using fal.ai VEED model (standard quality)";

    protected override string FalAiEndpoint => "veed/lipsync";
}

