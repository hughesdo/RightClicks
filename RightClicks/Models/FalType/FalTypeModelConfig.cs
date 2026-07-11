using System.Collections.Generic;

namespace RightClicks.Models.FalType;

/// <summary>
/// A model definition for one of the NEW fal.ai category types (Audio-to-Video, Video-to-Video,
/// Swaps, Text-to-Video). Loaded from a JSON file in the category folder (e.g. AudioToVideo\*.json).
///
/// This is deliberately SEPARATE from the frozen Image-to-Video <c>ModelConfig</c> so that the
/// original feature stays untouched. The shared config window (<c>FalTypeConfigWindow</c>) renders
/// itself entirely from this object, so adding a model = adding a JSON file.
/// </summary>
public class FalTypeModelConfig
{
    /// <summary>Category this model belongs to, e.g. "audio_to_video", "swap", "video_to_video", "text_to_video".</summary>
    public string type_id { get; set; } = string.Empty;

    /// <summary>The FULL fal endpoint path (never a short alias) — e.g. "fal-ai/ltx-2.3-quality/audio-to-video/lora".</summary>
    public string model_id { get; set; } = string.Empty;

    public string display_name { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public string pricing { get; set; } = string.Empty;
    public string processing_time { get; set; } = string.Empty;

    /// <summary>What the endpoint returns: "video" (result.video.url) or "image" (result.image.url).</summary>
    public string output_type { get; set; } = "video";

    /// <summary>
    /// File inputs this model needs. Each maps a fal parameter name (e.g. "image_url", "audio_url",
    /// "video_url") to a role used to auto-fill the slot from the right-clicked file. Empty for
    /// text-only models.
    /// </summary>
    public List<FalTypeInputSlot> input_slots { get; set; } = new();

    /// <summary>
    /// If set, the right-clicked file's TEXT contents pre-fill this parameter (used by Text-to-Video,
    /// which reads a .txt into the prompt box). The right-clicked file is not uploaded in that case.
    /// </summary>
    public string? prefill_prompt_from_file { get; set; }

    /// <summary>When true, the window shows the LoRA selector (dropdown + scale + transformer + hint).</summary>
    public bool supports_loras { get; set; }

    /// <summary>
    /// When true, a LoRA MUST be chosen (no "(none)" option; defaults to the first preset; validated
    /// on submit). Set for endpoints whose path ends in /lora, where fal requires the loras[] field.
    /// </summary>
    public bool loras_required { get; set; }

    /// <summary>
    /// If set (to a file-input param name like "audio_url"), the LTX output is silent, so after
    /// generating we mux THAT input's audio back onto the video with an X-safe codec (AAC 48kHz).
    /// Gives a finished, sound-on clip in one step. Only applies to video output.
    /// </summary>
    public string? reattach_audio_from { get; set; }

    /// <summary>Non-file parameters, keyed by the model's own fal parameter names.</summary>
    public Dictionary<string, FalTypeParameterConfig> parameters { get; set; } = new();
}

/// <summary>
/// One file input slot. The window auto-fills the slot whose <see cref="role"/> matches the
/// right-clicked file, and shows a path/URL textbox + Browse button for the others.
/// </summary>
public class FalTypeInputSlot
{
    /// <summary>fal parameter name the uploaded URL is sent under (e.g. "image_url").</summary>
    public string param { get; set; } = string.Empty;

    /// <summary>"image" | "audio" | "video" — used to match the right-clicked file and filter Browse.</summary>
    public string role { get; set; } = string.Empty;

    public string label { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public bool required { get; set; } = true;

    /// <summary>Extensions the Browse dialog accepts, e.g. [".jpg", ".png", ".webp"]. Null = derive from role.</summary>
    public string[]? accept { get; set; }
}

/// <summary>
/// Widget/serialization config for a single non-file parameter. Mirrors the frozen Image-to-Video
/// ParameterConfig shape so the same JSON conventions apply.
/// </summary>
public class FalTypeParameterConfig
{
    /// <summary>Which control to draw: text, dropdown, checkbox, number, slider.</summary>
    public string type { get; set; } = string.Empty;

    /// <summary>The JSON type fal expects: "string", "int", "number", or "bool".</summary>
    public string value_type { get; set; } = "string";

    public string label { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public bool required { get; set; }
    public object? @default { get; set; }
    public bool multiline { get; set; }
    public int rows { get; set; }
    public bool clear_on_focus { get; set; }
    public string[]? options { get; set; }
    public double? min { get; set; }
    public double? max { get; set; }
    public double? step { get; set; }
}
