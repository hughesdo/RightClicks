# fal.ai Type Expansion — Task Tracker & Session Continuity

> **Purpose:** Single source of truth for the fal.ai category expansion. This task spans
> multiple part-time sessions. **Read the "Current Status" block first, every session, to
> know where to pick up.** Update it at the end of every working session.

---

## 📍 Current Status (UPDATE THIS EVERY SESSION)

- **Phase:** ✅✅ ALL FOUR CATEGORIES BUILT AND CONFIRMED WORKING BY DON (2026-07-11). Audio-to-Video,
  Video-to-Video, Swaps, Text-to-Video all generate successfully. 82 total features. The "known
  guesses" (unverified endpoint ids + guessed Swaps param names) all turned out correct in practice.
- **install.bat:** no change needed — it xcopies the whole build output, which already contains all
  four category JSON folders + loras.json (via csproj globs).
- **Remaining (optional polish, not blocking):** verify 100percentrobot LoRA filename in loras.json;
  Pixverse `style:"None"` sentinel (only matters if that option is used); consider a git commit.
- **Last session:** 2026-07-11 — Fixed 3 live bugs in sequence (fal-storage endpoint → Cloudinary;
  loras_required; then learned the match_audio_length frame cap). Ended on a clean success.
- **Phase 1 fully accepted by Don** (clip plays with muxed audio; guard works). Done.
- **Phases 2, 3, 4 DELEGATED to 3 parallel subagents on 2026-07-11** (Don approved picking up pace
  without per-phase live testing). Agents create JSON + thin feature classes only; engine untouched.
  Parent (me) does the single build + discovery verification + fixes after they report.
  DESIGN CALLS baked into the briefs: Swaps → Pixverse on video right-clicks (video out),
  face-swap/Easel on image right-clicks (image out); Text-to-Video resolution = plain string
  dropdown (1080p/1440p/2160p), not the WxH widget.
- **After agents land:** build, verify discovery, then Don live-tests each category (endpoints
  marked "(confirm id)" — retake-video, veo3.1 t2v, seedance-2.0 t2v — are UNVERIFIED and may 404).
- **Blockers awaiting Don:** none.
- **Still unverified:** `loras.json` **100percentrobot** entry — guessed `.safetensors` filename;
  verify in the HF repo tree before relying on it. Other 3 LoRA URLs came from the task file and
  one (fal Audio-Reactive v1) is confirmed working in the successful run.

### 🩹 Known constraints / UX papercuts (Phase 1 follow-ups)
1. ✅ FIXED (code, 2026-07-11) **match_audio_length 481-frame cap** — `GuardMatchAudioLength()` in
   FalTypeConfigWindow probes audio duration (FFProbe) on submit; if frames > 481 at the chosen fps,
   warns and auto-unchecks match_audio_length. Awaiting Don's confirmation the warning fires.
2. **Prompt "in reaction to the music" prefix** — Don manually typed this; the LoRA likely wants a
   cue. Consider a default/boilerplate prompt prefix for the reactive LoRA.
3. ✅ FIXED (code, 2026-07-11) **silent output** — model JSON `reattach_audio_from: "audio_url"`
   makes FalTypeFeatureBase mux the source audio onto the video via `MuxAudioXSafeAsync`
   (video copy, AAC 48kHz per Don's X requirement, -shortest, +faststart). Awaiting Don's confirm
   the clip plays with sound. NOTE: falls back to plain re-encode if the audio input was a URL.
- **DECISION (REVISED 2026-07-11):** blob inputs upload via **Cloudinary** (upload → use → delete
  in a finally). Originally chose fal storage, but `FalAiFileStorageService`'s endpoint
  (`fal.run/storage/upload`) is WRONG — fal returns `NotFound: Application 'upload' not found`.
  Cloudinary is proven in this codebase (Image-to-Video) and inputs are small, so switched to it.
  FUTURE: if large video inputs (Phase 2/3) hit Cloudinary's ~100 MB cap, implement fal's real
  storage flow (`rest.alpha.fal.ai/storage/upload/initiate` → PUT to signed url) and switch back.

### Cloudinary — CLEARED 2026-07-11
- Preset `RightClicks`: Don confirmed in dashboard — unsigned, no resource-type restriction, no
  allowed-formats limit, accepts image/video/audio. Not the cause of rejections.
- Code endpoint verified: `CloudinaryStorageService.cs:96-103` computes resource_type per-file
  (`video` for known av, else `auto`) → `/video/upload` or `/auto/upload`. **Never** hardcoded
  `/image/upload`. The `?? "image"` at :156 is only a response-parse fallback, not the endpoint.

---

## 🎯 Goal

Don makes **music videos**. Expand RightClicks' fal.ai usage beyond the existing
**Image-to-Video** feature into **four new right-click category menus**, each triggered by the
file type that makes sense, each with its own model dropdown + config window.

Source request: `RightClicks_Type_Expansion_TASK.md`.

---

## 🔒 Locked Decisions (do not relitigate without Don)

1. **Four new category menus**, each its own submenu (NOT reusing Image-to-Video's menu):
   - **Audio-to-Video** — LTX audio-reactive + LoRA. **Priority.**
   - **Video-to-Video** — LTX extend/retake/render-to-real, Pixverse transition.
   - **Swaps** — Pixverse video-swap **and** image face-swap.
   - **Text-to-Video** — LTX from a `.txt` file.
2. **Image-to-Video is FROZEN.** Do not touch `ImageToVideoConfigWindow`,
   `ImageToVideoFeatureBase`, `ImageToVideo\*.json`, or its feature classes.
3. **One shared window engine** drives all four new categories (invisible to Don — he sees four
   distinct windows). Bugs/widgets fixed once. Adding a model = drop a JSON file.
4. **Within a category, models share a common option shape**; per-model quirks live in that
   model's JSON (mirrors how Image-to-Video juggles Kling/Wan/Veo).
5. **Swaps handles BOTH outputs:** Pixverse = video output; face-swap/Easel = image output.
   The engine must support image-output as well as video-output.
6. **Audio-to-Video = both entry points:** appears on audio right-clicks (pre-fills audio, pick
   image) AND image right-clicks (pre-fills image, pick audio).
7. **Output = auto-version + X-safe re-encode** for every video-producing generation:
   `{name}_ltx_01.mp4`, `_02`, ... never overwrite, then
   `ffmpeg -i in.mp4 -c:v copy -c:a aac -b:a 192k -ar 48000 -movflags +faststart out.mp4`.
8. **LoRA presets are data** (`name`, `url`, `use_hint`, `default_scale`,
   `compatible_endpoints[]`) so Don can add more without code edits.

---

## 🔬 Verified Technical Findings (2026-07-11)

| Area | Finding | Action |
|---|---|---|
| **Cloudinary blobs** | `UploadFileAsync` routes by extension → known av become `video`, else `/auto/upload`. Endpoint never hardcoded to `/image/upload`. ✅ Verified. | Add `.ogg`, `.m4v`; confirm `.gif` for Pixverse. |
| **Cloudinary preset** | ✅ CLEARED — Don confirmed preset accepts image/video/audio; code endpoint confirmed dynamic. Not a blocker. | none |
| **Alt storage** | `FalAiFileStorageService` (`fal.run/storage/upload`) already exists (lip-sync). No preset, no ~100 MB cap, self-expiring. | Phase 0.5 decision: Cloudinary vs fal storage for blobs. |
| **fal service (sync)** | `FalAiImageToVideoService` uses synchronous `fal.run/{endpoint}` + 10-min timeout. Long LTX jobs may exceed this. | Build queue-capable service (`queue.fal.run` submit→poll→result). |
| **fal service (output)** | Throws if `result.Video == null` — image-output swaps crash it. | New service needs an image-output branch (`result.image.url`). |
| **Config window** | `ImageToVideoConfigWindow` is single-image only; widgets exist for text/dropdown/checkbox/number/slider. No `file` or `lora` widget. | New shared window adds `file` + `lora` widgets + multi-input. |
| **Feature discovery** | `FeatureDiscoveryService` auto-discovers `IFileFeature`/`IConfigurableFeature` (non-abstract, public ctor). | New category features auto-register the same way. |

---

## 🗂️ Architecture (target shape)

```
Existing (FROZEN):
  ImageToVideoConfigWindow.xaml(.cs)     ImageToVideoFeatureBase.cs     ImageToVideo\*.json

New (shared engine):
  Windows\FalTypeConfigWindow.xaml(.cs)  ← ONE builder, reads per-model JSON, renders
                                            text/dropdown/checkbox/number/slider/FILE/LORA
  Features\...\FalTypeFeatureBase.cs      ← multi-input upload, queue call, video|image output,
                                            versioned + re-encoded output
  Services\FalAiQueueService.cs           ← queue.fal.run submit→poll→result, video|image
  Services\LoraRegistry (loras.json)      ← LoRA presets as data

  AudioToVideo\*.json   VideoToVideo\*.json   Swaps\*.json   TextToVideo\*.json
  Features\Audio\AudioToVideo*Feature.cs (thin menu-label classes, one per model)
  ... etc per category
```

**Model JSON schema additions** (backward-compatible with existing ImageToVideo JSON):
`type_id`, `primary_input` (image_url|audio_url|video_url|none), `output_type` (video|image),
`input_slots[]` (secondary file inputs with their fal param name), optional `loras` widget.

---

## ✅ Task Breakdown

> Legend: [x] = code complete & deployed; [D] = awaiting Don's live acceptance test.

### Phase 0 — Foundation (the bulk of the work; unblocks everything)
- [x] 0.1 Model JSON schema — `Models/FalType/FalTypeModelConfig.cs` (type_id, output_type, input_slots[], prefill_prompt_from_file, supports_loras).
- [x] 0.2 `Windows/FalTypeConfigWindow.xaml(.cs)` — dynamic form + file-slot widget (auto-fills right-clicked file, Browse for others) + LoRA widget + `resolution` nested-object widget.
- [x] 0.3 `Services/FalAiQueueService.cs` — queue submit→poll→result, video-or-image output, full-path guard + `NotFound` surfacing. [D] not hit live yet.
- [x] 0.4 `Features/FalType/FalTypeFeatureBase.cs` — upload N inputs (fal storage), build payload, queue call, save output.
- [x] 0.5 Storage = fal storage (`FalAiFileStorageService`); extended its MIME map (.ogg/.m4v/.aac/.flac/.gif/images). [D] upload not hit live yet.
- [x] 0.6 Output: auto-version `{name}_{suffix}_NN` + X-safe ffmpeg re-encode (FFMpegCore: copy video, aac 192k, -ar 48000, +faststart).
- [x] 0.7 `loras.json` + `Services/LoraRegistryService.cs`, gated by `compatible_endpoints[]`.

### Phase 1 — Audio-to-Video (PRIORITY)
- [x] 1.1 `AudioToVideo\*.json`: LTX 2.3 Quality reactive-LoRA + LTX 2.3 base.
- [x] 1.2 Both entry points — features list audio AND image extensions (`AudioToVideoLtxLoraFeature`, `AudioToVideoLtxFeature`).
- [x] 1.3 LoRA selector + Don's defaults baked into JSON (image_strength 0.62, fps 24; scale/ transformer from preset).
- [x] 1.4 Live pipeline PROVEN 2026-07-11: right-click .wav → Audio to Video (LoRA) → generated
      `Bill and me sing_002_a2v_01.mp4`. [D] Don still to confirm the CLIP ITSELF is good (plays,
      reactive, audio present — see papercut #3).

### Phase 2 — Video-to-Video  (built by subagent; compiles + discovers)
- [x] 2.1 `VideoToVideo\*.json`: extend, retake, reference-video-to-video/lora, render-to-real, pixverse v3.5 transition.
- [x] 2.2 LoRA widget reused (3DREAL gated to render-to-real + reference-lora).
- [x] 2.3 Don live-tested — WORKS. (retake-video id resolved fine in practice.)

### Phase 3 — Swaps (both output paths)  (built by subagent; compiles + discovers)
- [x] 3.1 `Swaps\*.json`: Pixverse Swap (video-out, video-trigger) + Face Swap + Easel (image-out, image-trigger).
- [x] 3.2 Don live-tested — WORKS (image-output branch validated).
- [x] 3.3 Guessed swap param names turned out correct in practice.

### Phase 4 — Text-to-Video  (built by subagent; compiles + discovers)
- [x] 4.1 `TextToVideo\*.json`: LTX t2v / fast / quality + Veo 3.1 + Seedance 2.0.
- [x] 4.2 `.txt` trigger → prefill_prompt_from_file="prompt", empty input_slots; resolution = plain string dropdown.
- [x] 4.3 Don live-tested — WORKS (Veo 3.1 + Seedance 2.0 ids resolved fine).

---

## 🤖 Subagent Playbook (activate AFTER Phase 1 proves the pattern)

Once Audio-to-Video works end-to-end, adding a new **model within an existing category** is a
mechanical, well-bounded job — ideal to hand to a subagent. Don't spawn agents for Phase 0
(too much shared design); use them for Phases 2–4 model additions once the template is proven.

**A subagent adding a model to an existing category must:**
1. Read this file's "Locked Decisions" + "Architecture" sections and the target category's
   existing `*.json` for the exact shape.
2. Verify the `endpoint_id` is a FULL fal path (never a short alias) — cross-check the fal model
   page. Task file flagged a past `NotFound` bug from short ids.
3. Create the model JSON (copy a sibling, adjust params to the fal schema, set correct
   `value_type` per field so it serializes as the right JSON type).
4. Create the thin feature class (menu label `☁️ {name} ~ ${price}`, `DefaultModelId`).
5. Build, then CLI-test per CLAUDE.md workflow; review the TEST log; report to Don.
6. **Do NOT** mark the checkbox here complete — only Don's acceptance does that.

**Endpoints still marked "(confirm id)" in the task file** (verify before use): Veo 3.1
text-to-video, Seedance 2.0 text-to-video, LTX retake-video.

---

## ⚠️ Known guesses to verify on first live test (from subagent builds)
- **Unverified endpoint ids (may 404 with fal NotFound — the error names the id):**
  `fal-ai/ltx-2.3/retake-video`, `fal-ai/veo3.1/text-to-video`, `bytedance/seedance-2.0/text-to-video`.
- **Swaps param/slot names are all guesses** — highest chance of a 422 "field required/unknown".
  Fix by editing the model JSON in `Swaps\` to the real fal param names (no code change needed).
- **Pixverse `style:"None"`** would be sent literally; drop that option or the base needs an
  "omit on sentinel" rule. Applies to `VideoToVideo\pixverse-v3.5-transition.json` (and pixverse swap if present).
- **100percentrobot LoRA** filename still unverified in `loras.json`.

## 📝 Session Log

- **2026-07-11 (a)** — Grilled & locked all requirements (scope = 4 categories; Image-to-Video
  frozen; shared engine; Swaps both outputs; A-to-V both entry points; versioned+re-encoded
  output). Verified Cloudinary blob routing, fal sync-vs-queue gap, image-output gap. Wrote this
  tracker + memory.
- **2026-07-11 (b)** — Cleared Cloudinary. Built Phase 0 foundation + Phase 1 Audio-to-Video.
  Build 0/0, deployed, features discovered + auto-enabled (67 → 69).
- **2026-07-11 (c) — LIVE TESTING, ended on SUCCESS.** Three real bugs found & fixed in order:
  (1) fal-storage upload endpoint `fal.run/storage/upload` is bogus (`NotFound: Application
  'upload'`) → switched blob uploads to Cloudinary (upload→use→delete, proven path). (2) `/lora`
  endpoint requires `loras` field → added `loras_required` flag: LoRA selector drops "(none)",
  auto-selects first preset, validates on submit; set on the LoRA JSON. (3) Learned
  match_audio_length=true caps at 481 frames (~20s@24fps) — not a code bug, a fal limit → papercut #1.
  **First success 13:22:** right-clicked .wav, LoRA=fal Audio-Reactive v1, match_audio_length=false,
  720x1280 → produced `Bill and me sing_002_a2v_01.mp4`. Engine fully validated.
- **2026-07-11 (d)** — Phase 1 accepted (clip plays w/ muxed audio). Added auto-mux source song
  (X-safe AAC 48k via reattach_audio_from) + long-audio guard (GuardMatchAudioLength probes FFProbe).
- **2026-07-11 (e)** — Don asked to pick up pace w/o per-phase testing + delegate. **Fanned out 3
  parallel subagents** for Phases 2/3/4 (data + thin feature classes only; engine/csproj untouched).
  All three landed, build 0/0 first try, 13 new features discovered + auto-enabled (69 → 82), all
  category JSON valid. Engine proved general enough to need ZERO changes. Not yet live-tested — see
  "Known guesses" above. Next: Don live-tests each category; fix guessed ids/param names as they surface.
