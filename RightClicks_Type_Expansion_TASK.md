# RightClicks — Expand fal.ai "Types" Beyond Image-to-Video

## Context for you (the CLI session)

RightClicks is a Windows right-click / context-menu app (Python/C#) that routes file
actions to pipelines. It already has a working **Image-to-Video** "type": right-click a
`.jpg/.png/.bmp` → menu lists fal.ai models as `{cloud icon} {model name} ~ ${price}/s`
(e.g. "Kling v3 ~$0.13/s", "Seedance 2.0 ~$0.30/s", "Veo 3.1 Fast ~$0.15/s",
"Vidu Q3 ~$0.07/s", "Wan 2.7 ~$0.10/s"). Selecting a model opens a **second config
window** ("Image to Video - API Config") with: Source Image preview + path, API Model
dropdown (with a description + pricing line), Prompt box, Negative Prompt box, Duration
dropdown, Generate Audio checkbox, Shot Type dropdown, CFG Scale slider, Cancel/Submit.

**Goal:** replicate that exact two-stage pattern (context menu → config window → fal API
call) for FOUR NEW TYPES, each triggered by right-clicking the file type that makes sense.
Do not rebuild Image-to-Video; mirror its architecture. Reuse the existing model-registry
+ config-window abstractions if they exist; if the current code hardcodes Image-to-Video,
refactor to a generic "VideoType" registry first (see Task 0).

The model IDs and prices below were verified current on 2026-07-11. Treat the registry as
data (JSON/config), NOT hardcoded, so new endpoints can be added without code changes.

---

## Task 0 — Refactor to a generic type registry (do this first)

Abstract whatever powers Image-to-Video into a declarative registry. Each "Type" is an
entry describing:

- `type_id` (e.g. `image_to_video`, `audio_to_video`, `swap`, `video_to_video`, `text_to_video`)
- `trigger_extensions` (which file right-clicks expose this type)
- `menu_label` (submenu name)
- `models[]` — each with:
  - `label` (shown as `{cloud} {label} ~ ${price}`)
  - `endpoint_id` (fal model string)
  - `price_display` and `price_note`
  - `input_fields[]` — schema for the config window (field name, widget type, default, hint, enum options)
  - `primary_input` — which file the right-click maps to (image_url / audio_url / video_url / none)
  - optional `loras[]` — selectable LoRA presets (see Audio-to-Video)

The config window should render itself from `input_fields[]` so every type reuses ONE window
builder. This is the key architectural win: adding a model = adding a registry row.

fal call pattern is identical across all of them (Python `fal_client`):

```python
import fal_client
result = fal_client.subscribe(ENDPOINT_ID, arguments={...}, with_logs=True)
# video output at result["video"]["url"]  (image-output models use result["image"]["url"])
```

Endpoints that may run long should use the queue pattern (`submit` → `status` → `result`).
Note the earlier NotFound bug: endpoint strings must be the FULL path (e.g.
`fal-ai/bytedance/seedance/v1/lite/image-to-video`, never `seedance`). Validate every
endpoint string against the registry at call time and surface a clear error if fal returns
`NotFound: Application ...`.

---

## Task 1 — NEW TYPE: Audio-to-Video (right-click audio AND image files)

**This is the priority.** It's the LTX audio-reactive workflow Don uses most.

**Triggers:** right-click on `.wav/.mp3/.m4a/.aac/.ogg` (audio is the reactive driver) AND
on `.jpg/.png/.bmp/.webp` (image is the first frame). Because these models take BOTH an
audio clip and a first-frame image, expose the "Audio-to-Video" submenu on both file types.
Whichever file was right-clicked pre-fills its slot; the config window must provide a
file-picker/URL field for the OTHER input.

**Config-window fields** (mirror Image-to-Video, adjust to schema below):
Source Image (path/URL), Audio (path/URL), Prompt, Negative Prompt, Duration, Resolution,
FPS, Generate Audio, Image Strength, LoRA selector (Audio-to-Video only), LoRA Scale,
Transformer, Submit/Cancel.

**Models for the dropdown:**

| Label | endpoint_id | price | notes |
|---|---|---|---|
| LTX 2.3 Quality — Audio-to-Video (LoRA) | `fal-ai/ltx-2.3-quality/audio-to-video/lora` | ~$0.06/s 1080p | THE one Don uses. Accepts `loras[]`. |
| LTX 2.3 — Audio-to-Video | `fal-ai/ltx-2.3/audio-to-video` | ~$0.08/s 1080p | Base audio-to-video, no custom LoRA field needed |

**Verified input schema for `fal-ai/ltx-2.3-quality/audio-to-video/lora`:**
```json
{
  "prompt": "...",
  "audio_url": "https://...",
  "image_url": "https://...",
  "match_audio_length": true,
  "resolution": { "width": 1024, "height": 1024 },
  "frames_per_second": 24,
  "num_inference_steps": 15,
  "guidance_scale": 1,
  "generate_audio": true,
  "image_strength": 0.62,
  "negative_prompt": "",
  "enable_prompt_expansion": false,
  "video_quality": "high",
  "video_write_mode": "balanced",
  "loras": [
    { "path": "<HF .safetensors URL>", "scale": 1.2, "transformer": "both" }
  ]
}
```
Defaults Don has dialed in: image_strength 0.62, fps 24, scale 1.2–1.5, transformer "both",
pre-trim audio to the hook before upload.

**LoRA selector (the "options" menu Don wants):** on the LoRA model only, provide a dropdown
of ready-made HuggingFace LoRAs. Each option = display name + `.safetensors` URL + a
one-line "good for…" hint shown on hover/popup. Populate with:

- **fal Audio-Reactive (v1)** — good for: abstract art / geometry dancing to music, beat-locked motion.
  `https://huggingface.co/fal/ltx2.3-audio-reactive-lora/resolve/main/ltx2.3_audio_reactive_lora.safetensors`
- **fal Audio-Reactive (v2)** — same family, alternate weights, try if v1 drifts.
  `https://huggingface.co/fal/ltx2.3-audio-reactive-lora/resolve/main/ltx2.3_audio_reactive_lora_v2.safetensors`
- **100percentrobot Audio-Reactive** — good for: phased-morph clips (subject→evolve→morph→chaos), scale 1.4+, works better at 10–20s.
  `https://huggingface.co/100percentrobot/LTX-2.3-Audio-Reactive-LORA` (resolve the .safetensors filename from the repo tree)
- **fal 3DREAL Strong v2** — good for: talking characters / lip-sync / making a face or "cat" sing; turns grey 3D blockout into photoreal. NOTE: this pairs with the render-to-real / reference-video-to-video endpoint, not the audio-to-video endpoint — flag in UI as "different endpoint".
  `https://huggingface.co/fal/LTX-2.3-3DREAL-LoRA/resolve/main/3DREAL-strong-v2.safetensors`

Make the LoRA list a registry array so Don can add more later. Store the "good for" text as
a `use_hint` field per LoRA and render it as tooltip + a small caption under the dropdown.

**LoRA scale field:** numeric, default 1.2, range 1.0–2.0 (hint: "1.2–1.5 typical; higher = stronger reaction").

---

## Task 2 — NEW TYPE: Swaps (right-click a video)

**Trigger:** right-click on `.mp4/.mov/.webm/.m4v/.gif`.
**Submenu:** "Swaps". Each model opens a config window whose primary input is the
right-clicked video; the window adds a Reference Image field for the swap target.

**Models:**

| Label | endpoint_id | price | notes |
|---|---|---|---|
| Pixverse Swap | `fal-ai/pixverse/swap` | $0.15–$0.40 / 5s | person/object/background swap; keyframe control; keeps original audio |
| Face Swap (image) | `fal-ai/face-swap` | low | swaps faces between images (image-output, not video) |
| Easel Advanced Face Swap | `easel-ai/advanced-face-swap` | low | image face swap, gender + workflow_type params |

**Verified Pixverse Swap notable params:** `keyframe_id` (frame 1..last), swap `mode`
(person/object/background), `reference image` (jpg/png/webp/gif), `original_sound_switch`
(audio preservation), resolution 360p–1080p. Accepts video (mp4/mov/webm/m4v/gif).

Flag in UI which swap models are **video-output** (Pixverse) vs **image-output**
(face-swap, easel) so the config window shows the right result handling.

---

## Task 3 — NEW TYPE: Video-to-Video (right-click a video)

**Trigger:** right-click on `.mp4/.mov/.webm/.m4v`.
**Submenu:** "Video to Video". Primary input = the right-clicked video (`video_url`).

**Models (lead with the LTX ones Don likes):**

| Label | endpoint_id | price | notes |
|---|---|---|---|
| LTX 2.3 — Extend Video | `fal-ai/ltx-2.3/extend-video` | ~$0.06/s | adds frames at start/end; `video_url` in, optional `prompt` to steer |
| LTX 2.3 — Retake Video | `fal-ai/ltx-2.3/retake-video` | ~$0.06/s | regenerate/vary an existing clip (confirm exact id in playground) |
| LTX 2.3 Quality — Reference Video-to-Video (LoRA) | `fal-ai/ltx-2.3-quality/reference-video-to-video/lora` | ~$0.06/s | video-to-video WITH LoRA; used by 3DREAL render-to-real |
| LTX 2.3 Quality — Render-to-Real | `fal-ai/ltx-2.3-quality/render-to-real` | ~$0.06/s | grey 3D blockout → photoreal; pairs with 3DREAL LoRA + enable_detail_refine |
| Pixverse v3.5 — Transition | `fal-ai/pixverse/v3.5/transition` | $0.15–$0.40 / 5s | morph between two frames |

**Verified LTX video-to-video params (extend-video family):** `video_url` (required),
`prompt` (optional, describes how to animate/extend), `guidance_scale` (default 5 t2v / 9
with image), `aspect_ratio` "auto", `end_image_url` (optional, makes a transition),
`duration`. The reference-video-to-video/lora endpoint accepts the same `loras[]` array as
audio-to-video, so REUSE the LoRA-selector widget from Task 1 here (offer 3DREAL Strong v2
as the featured LoRA, use_hint: "3D/CG/game render → photoreal, holds composition").

---

## Task 4 — NEW TYPE: Text-to-Video (right-click a .txt)

**Trigger:** right-click on `.txt` (e.g. `animation.txt`, any prompt file).
**Behavior:** read the `.txt` contents and **pre-fill the Prompt box** in the config window.
Treat the file as a reusable prompt template. Primary input = none (text only); no
image/audio/video slot required, though image-to-video models could optionally accept an
attached first frame later.

**Models (lead with LTX):**

| Label | endpoint_id | price | notes |
|---|---|---|---|
| LTX 2.3 — Text-to-Video | `fal-ai/ltx-2.3/text-to-video` | $0.06/s 1080p | duration 6/8/10; resolution 1080p/1440p/2160p; native audio |
| LTX 2.3 Fast — Text-to-Video | `fal-ai/ltx-2.3/text-to-video/fast` | $0.04/s | speed-optimized variant |
| LTX 2.3 Quality — Text-to-Video | `fal-ai/ltx-2.3-quality/text-to-video` | ~$0.06/s | distilled single-stage; guidance default 1 |
| Veo 3.1 — Text-to-Video | `fal-ai/veo3.1/text-to-video` (confirm id) | ~$0.20/s | premium, strong audio/lip-sync |
| Seedance 2.0 — Text-to-Video | `bytedance/seedance-2.0/text-to-video` (confirm id) | ~$0.30/s | native audio, multi-shot |

**Verified LTX text-to-video schema:** `prompt` (required), `duration` enum 6/8/10 (default
6), `resolution` enum 1080p/1440p/2160p (default 1080p), `aspect_ratio` enum, `generate_audio`
(default true), `negative_prompt` (has a long sensible default), `seed`,
`enable_prompt_expansion` (default true), `safety checker` toggle.

---

## Cross-cutting requirements

1. **Registry-driven config window.** One window builder renders from `input_fields[]`.
   Field widget types needed: text, textarea, dropdown/enum, checkbox, number, slider,
   file/URL picker, and the LoRA-selector (dropdown + scale + transformer + use_hint tooltip).
2. **Price display** in menu labels: `{cloud icon} {label} ~ ${price}` exactly like the
   existing Image-to-Video menu. Pull from registry `price_display`.
3. **Model description + pricing line** under the API Model dropdown (mirror the existing
   "Kling v3 (standard tier). Strong motion..." line).
4. **File slot mapping:** the right-clicked file auto-fills its role; the window exposes
   pickers for any other required inputs. Audio-to-Video is special: exposed on BOTH audio
   and image right-clicks.
5. **Endpoint validation:** full-path strings only; catch fal `NotFound` and show the
   offending id. Add a tiny self-test that pings the registry ids format-wise.
6. **fal upload:** local files (image/audio/video) must be uploaded to fal storage (or sent
   as base64 data URI) to get a URL before the call. Reuse whatever Image-to-Video already
   does for the source image; extend it to audio and video inputs.
7. **Output handling:** video-output → save `result["video"]["url"]`; image-output (face
   swaps) → save `result["image"]["url"]`. Offer the existing X-compatible ffmpeg re-encode
   step where a video is produced:
   `ffmpeg -i in.mp4 -c:v copy -c:a aac -b:a 192k -ar 48000 -movflags +faststart out.mp4`
8. **LoRA presets are data.** Store the HuggingFace LoRA list (name, url, use_hint,
   default_scale, compatible_endpoints[]) in config so Don can add more without code edits.
   Show `use_hint` on hover AND as a caption. Gate each LoRA to the endpoints it's valid for
   (audio-reactive → audio-to-video; 3DREAL → render-to-real / reference-video-to-video).

## Suggested build order
Task 0 (registry refactor) → Task 1 (Audio-to-Video + LoRA selector, the priority) →
Task 3 (Video-to-Video, reuses LoRA widget) → Task 2 (Swaps) → Task 4 (Text-to-Video).

## Verify before shipping
For each endpoint_id marked "(confirm id)", hit the fal model page or run a dry
`fal_client.subscribe` with minimal args and confirm no `NotFound`. IDs without that note
were verified on 2026-07-11.
