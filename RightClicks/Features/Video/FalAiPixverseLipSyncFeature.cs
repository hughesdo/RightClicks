using RightClicks.Models;

namespace RightClicks.Features.Video;

/// <summary>
/// Feature to perform AI-powered lip sync using fal.ai Pixverse Lipsync API.
/// Uses the synchronous endpoint - processing typically takes 1-3 minutes.
/// Requires fal.ai API key configured in API Config tab.
/// Output file: {original_name}_LIPSYNC.{ext} (next to source file)
/// 
/// Pricing: $0.20 per minute of video
/// </summary>
public class FalAiPixverseLipSyncFeature : FalAiLipSyncFeatureBase
{
    public override string Id => "FalAiPixverseLipSync";

    public override string DisplayName => "Lip Sync > ☁️ fal.ai.Pixverse $.20/min";

    public override string Description => "AI-powered lip sync using fal.ai Pixverse model (budget option)";

    protected override string FalAiEndpoint => "fal-ai/pixverse/lipsync";
}

