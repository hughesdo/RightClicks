using System.Text.Json.Serialization;

namespace RightClicks.Models.Karaoke;

/// <summary>
/// Configuration model for karaoke subtitle styles.
/// Each style defines fonts, colors, animations, and positioning for ASS subtitle rendering.
/// </summary>
public class KaraokeStyleConfig
{
    [JsonPropertyName("styleName")]
    public string StyleName { get; set; } = "Classic";

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("fontName")]
    public string FontName { get; set; } = "Arial";

    [JsonPropertyName("fontSize")]
    public int FontSize { get; set; } = 88;

    [JsonPropertyName("defaultPrimaryColor")]
    public string DefaultPrimaryColor { get; set; } = "&H00FFFFFF";

    [JsonPropertyName("defaultOutlineColor")]
    public string DefaultOutlineColor { get; set; } = "&H00000000";

    [JsonPropertyName("defaultOutlineThickness")]
    public int DefaultOutlineThickness { get; set; } = 2;

    [JsonPropertyName("highlightPrimaryColor")]
    public string HighlightPrimaryColor { get; set; } = "&H0000FFFF";

    [JsonPropertyName("highlightOutlineColor")]
    public string HighlightOutlineColor { get; set; } = "&H00000000";

    [JsonPropertyName("highlightOutlineThickness")]
    public int HighlightOutlineThickness { get; set; } = 5;

    [JsonPropertyName("karaokeAnimation")]
    public string KaraokeAnimation { get; set; } = "fill";

    [JsonPropertyName("highlightFadeTime")]
    public double HighlightFadeTime { get; set; } = 0.4;

    [JsonPropertyName("positioning")]
    public string Positioning { get; set; } = "Bottom";

    [JsonPropertyName("marginV")]
    public int MarginV { get; set; } = 30;

    [JsonPropertyName("marginL")]
    public int MarginL { get; set; } = 10;

    [JsonPropertyName("marginR")]
    public int MarginR { get; set; } = 10;

    [JsonPropertyName("alignment")]
    public int Alignment { get; set; } = 2;

    [JsonPropertyName("playResX")]
    public int PlayResX { get; set; } = 1920;

    [JsonPropertyName("playResY")]
    public int PlayResY { get; set; } = 1080;

    [JsonPropertyName("whisperModel")]
    public string WhisperModel { get; set; } = "tiny.en";
}

