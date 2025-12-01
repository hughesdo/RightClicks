using RightClicks.Models;

namespace RightClicks.Features.Video;

/// <summary>
/// Feature to perform AI-powered lip sync using fal.ai Sync Lipsync API (v1.9).
/// Uses the synchronous endpoint - processing time varies based on video length.
/// Requires fal.ai API key configured in API Config tab.
/// Output file: {original_name}_LIPSYNC.{ext} (next to source file)
/// 
/// Pricing: $0.70 per minute - High-quality option
/// 
/// Features:
/// - Version 1.9.0-beta (latest)
/// - Multiple sync modes: cut_off, loop, bounce, silence, remap
/// - High-quality lip sync results
/// 
/// NOTE: Documentation shows queue-based endpoint (https://queue.fal.run/fal-ai/sync-lipsync),
/// but we're trying synchronous first (https://fal.run/fal-ai/sync-lipsync) based on success
/// with Pixverse and VEED. If this fails with timeout or "endpoint not found" errors, we'll need
/// to implement queue-based logic with status polling.
/// </summary>
public class FalAiSyncLipSyncFeature : FalAiLipSyncFeatureBase
{
    public override string Id => "FalAiSyncLipSync";

    public override string DisplayName => "Lip Sync > ☁️ fal.ai.Sync $.70/min";

    public override string Description => "AI-powered lip sync using fal.ai Sync v1.9 model (high quality)";

    protected override string FalAiEndpoint => "fal-ai/sync-lipsync";
}

