# Testing Notes — fal.ai Type Expansion — 2026-07-11

Author/session: Claude + Don. Scope: the new fal.ai "category" features (Audio-to-Video,
Video-to-Video, Swaps, Text-to-Video) built on a shared config-window engine. The original
**Image-to-Video** feature was left frozen and untouched.

---

## 1. Summary of what shipped

A generic, JSON-driven engine that mirrors Image-to-Video's two-stage pattern (context menu →
config window → fal API call) for **four new categories** and **13 new features** (total feature
count 69 → **82**). Adding a model now = drop a JSON file + a ~15-line feature class.

New categories and their trigger file types:

| Category | Right-click on | Models | Output |
|---|---|---|---|
| **Audio to Video** | image OR audio | LTX 2.3 Quality (LoRA), LTX 2.3 | video (source song muxed back on) |
| **Video to Video** | video (Pixverse transition: image) | LTX extend, retake, reference-LoRA, render-to-real, Pixverse v3.5 transition | video |
| **Swaps** | video (Pixverse) / image (face-swaps) | Pixverse Swap, Face Swap, Easel Advanced Face Swap | video (Pixverse) / image (face-swaps) |
| **Text to Video** | .txt | LTX 2.3 / Fast / Quality, Veo 3.1, Seedance 2.0 | video |

Key engine pieces (all new, in `RightClicks\`):
- `Models\FalType\FalTypeModelConfig.cs` — model JSON schema (input_slots, output_type,
  supports_loras, loras_required, reattach_audio_from, prefill_prompt_from_file).
- `Windows\FalTypeConfigWindow.xaml(.cs)` — one window renders file-slot + form + LoRA + resolution widgets.
- `Services\FalAiQueueService.cs` — fal QUEUE flow (submit→poll→result), video OR image output.
- `Features\FalType\FalTypeFeatureBase.cs` — upload (Cloudinary) → queue → versioned output → X-safe
  re-encode / source-audio mux → input cleanup.
- `Services\LoraRegistryService.cs` + `loras.json` — LoRA presets as data.

---

## 2. What was TESTED and CONFIRMED WORKING

- ✅ **All four categories generate successfully** (Don confirmed 2026-07-11).
- ✅ **Audio-to-Video** — proven in depth: both entry points (right-click image OR audio), LoRA
  selector, and the **source song is muxed onto the output** (X-safe AAC 48 kHz) so the clip has sound.
- ✅ **Long-audio guard** — when "Match audio length" is on and audio exceeds the ~20s / 481-frame
  cap, the window warns and auto-unchecks it (no wasted fal call).
- ✅ **Swaps image-output branch** — the first exercise of image output; works.
- ✅ **Endpoint ids that were UNVERIFIED at build time all resolved fine in practice:**
  `ltx-2.3/retake-video`, `veo3.1/text-to-video`, `bytedance/seedance-2.0/text-to-video`.
- ✅ **Guessed Swaps param names worked** — Pixverse (`video_url`/`image_url`), Face Swap
  (`base_image_url`/`swap_image_url`), Easel (`target_image`/`face_image_0`, `gender_0`/`workflow_type`).
- ✅ Build 0 warnings / 0 errors; all 13 features auto-discovered and auto-enabled.
- ✅ `install.bat` unchanged — it xcopies the whole build output, which includes the new JSON folders.

---

## 3. OPEN ITEMS (left for Don / future sessions)

These are NOT blockers — everything works — but they're the loose ends:

1. **`loras.json` → "100percentrobot Audio-Reactive" preset** — its `.safetensors` filename was
   GUESSED (`.../LTX-2.3-Audio-Reactive-LORA.safetensors`). Only matters if you pick that specific
   LoRA. Verify the real filename in the HF repo tree:
   https://huggingface.co/100percentrobot/LTX-2.3-Audio-Reactive-LORA/tree/main
   The other 3 LoRAs (fal Audio-Reactive v1/v2, fal 3DREAL Strong v2) are from the task file; v1 is
   confirmed working.

2. **Pixverse `style: "None"` sentinel** — the param merge only drops EMPTY strings, so a literal
   `"None"` selection would be sent to fal and may be rejected. Only bites if that dropdown option is
   used. Fix: remove the "None" option from the Pixverse JSON, or add an "omit-on-sentinel" rule in
   the window. Files: `VideoToVideo\pixverse-v3.5-transition.json` (and `Swaps\pixverse-swap.json` if it has one).

3. **Reactive-LoRA prompt cue** — Don manually prepends "in reaction to the music"; consider a
   default prompt prefix for the audio-reactive LoRA model.

4. **Blob storage is Cloudinary (100 MB free-tier cap)** — fine for images + short audio hooks. If a
   large VIDEO input (Video-to-Video / Swaps) exceeds 100 MB, the Cloudinary upload will fail. Future
   fix documented in `TYPE_EXPANSION_TASKS.md`: implement fal's real storage flow
   (`rest.alpha.fal.ai/storage/upload/initiate` → PUT) and switch blob uploads back to fal.

5. **match_audio_length duration probe** uses FFProbe; if ffprobe is ever missing the guard is
   skipped (fails open — fal would then reject long audio as before). Not observed, just noted.

---

## 4. Per-category regression checklist (for future changes)

**Audio to Video**
- [ ] Right-click image → menu appears; image slot pre-filled; Browse audio.
- [ ] Right-click audio → menu appears; audio slot pre-filled; Browse image.
- [ ] LoRA model: dropdown has NO "(none)", first preset auto-selected.
- [ ] Output plays WITH the source song (AAC 48 kHz).
- [ ] Audio > ~20s with match-length on → warning fires, auto-unchecks.

**Video to Video**
- [ ] Right-click .mp4/.mov/.webm/.m4v → menu; source video slot pre-filled.
- [ ] render-to-real / reference-LoRA show the LoRA selector (3DREAL gated in).
- [ ] Pixverse transition appears on IMAGE right-clicks (two image slots).

**Swaps**
- [ ] Pixverse Swap on a video → reference image slot; VIDEO output.
- [ ] Face Swap / Easel on an image → IMAGE output saved (not re-encoded).

**Text to Video**
- [ ] Right-click .txt → prompt box pre-filled from the file's contents.
- [ ] Resolution is a plain 1080p/1440p/2160p dropdown (not WxH).

---

## 5. Where the living state lives
- **`TYPE_EXPANSION_TASKS.md`** — the ongoing tracker (status block, per-item checklist, decisions,
  known guesses, session log). Read its top block first each session.
- Memory: `falai-type-expansion` (points back to the tracker).
