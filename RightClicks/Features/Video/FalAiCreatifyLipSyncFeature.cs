using RightClicks.Models;

namespace RightClicks.Features.Video;

/// <summary>
/// Feature to perform AI-powered lip sync using Creatify Lipsync API.
/// Uses the synchronous endpoint - processing time varies based on video length.
/// Requires fal.ai API key configured in API Config tab.
/// Output file: {original_name}_LIPSYNC.{ext} (next to source file)
/// 
/// Pricing: $1.00 per minute - Premium quality option
/// 
/// Features:
/// - High-quality lip sync
/// - Optional loop parameter (default: true)
/// 
/// NOTE: Documentation shows queue-based endpoint (https://queue.fal.run/creatify/lipsync),
/// but we're trying synchronous first (https://fal.run/creatify/lipsync) based on success
/// with Pixverse and VEED. If this fails with timeout or "endpoint not found" errors, we'll need
/// to implement queue-based logic with status polling.
/// </summary>
public class FalAiCreatifyLipSyncFeature : FalAiLipSyncFeatureBase
{
    public override string Id => "FalAiCreatifyLipSync";

    public override string DisplayName => "Lip Sync > ☁️ fal.ai.Creatify $1/min";

    public override string Description => "AI-powered lip sync using Creatify model (premium quality)";

    protected override string FalAiEndpoint => "creatify/lipsync";
}

