# MiraTTS Integration Planning

**Status:** 🟡 **Deferred - Test Standalone First**  
**Last Updated:** 2026-01-14  
**Priority:** Low (Future Enhancement)

---

## Overview

MiraTTS is a high-quality, fast text-to-speech (TTS) model that can generate realistic speech at 100x realtime speeds. This document outlines the potential integration of MiraTTS into RightClicks for converting text files into narrated audio.

**Key Repository:** https://github.com/ysharma3501/MiraTTS  
**Hugging Face Model:** https://huggingface.co/YatharthS/MiraTTS

---

## Why MiraTTS is Interesting

### **Technical Advantages:**
- ✅ **Incredibly fast:** 100x realtime generation (vs 10x for most TTS)
- ✅ **High quality:** 48kHz audio output (much higher than typical 22kHz)
- ✅ **Memory efficient:** Works within 6GB VRAM
- ✅ **Low latency:** Can be as low as 100ms
- ✅ **Voice cloning:** Can clone any voice from a reference audio sample

### **Potential Use Cases:**
- Convert text documents to audiobooks
- Narrate markdown files with custom voices
- Generate voiceovers for scripts
- Create audio versions of documentation

---

## ⚠️ Critical Limitation: Training Required

### **The Reality Check:**

Unlike RVC (which provides pre-trained voice models), **MiraTTS requires training** to create custom voices:

1. **Base Model:** MiraTTS comes with a base model that can clone voices
2. **Voice Cloning:** Requires 10-30 seconds of reference audio
3. **Custom Voices:** Requires training on ~10 hours of audio data
4. **Training Time:** Can take hours to days depending on dataset size

**This is why integration is deferred** - it's not a simple "drop in models and go" like RVC.

---

## RVC vs MiraTTS: The Fundamental Difference

### **Why RVC is Still Superior for Voice Conversion:**

| Aspect | RVC (Voice Conversion) | MiraTTS (Text-to-Speech) |
|--------|------------------------|--------------------------|
| **Input** | Audio with existing performance | Plain text |
| **Preserves** | ✅ Acting, emotion, nuance, timing | ❌ Generates from scratch |
| **Southern drawl** | ✅ Preserved from source audio | ❌ Lost (unless trained specifically) |
| **Inflections** | ✅ Maintained from original | ❌ Generic/flat delivery |
| **Sarcasm** | ✅ Carried over | ❌ Not understood |
| **Pauses/Timing** | ✅ Kept from source | ❌ Auto-generated |
| **Emotional range** | ✅ From original performance | ❌ Limited/synthetic |
| **Use case** | Converting existing audio | Generating new audio from text |

### **The Acting Problem:**

**RVC Example:**
```
Original: Morgan Freeman reading with gravitas, pauses, emphasis
→ RVC converts to Trump's voice
→ Result: Trump's voice WITH Morgan Freeman's acting/delivery
```

**MiraTTS Example:**
```
Text: "Well, I'll be darned, that's mighty fine."
→ MiraTTS generates speech
→ Result: Words are spoken, but southern drawl is LOST
         (unless model was specifically trained on southern accent data)
```

**Key Insight:** TTS generates speech from text, but it doesn't capture the **human performance** - the pauses, emphasis, emotion, regional accents, and acting choices that make speech compelling.

---

## Available Models

### **Base Model:**
- **YatharthS/MiraTTS** - English + Chinese, 0.5B params, 48kHz output

### **Finetuned/Specialized Models:**
- **SebastianBodza/MiraToffel_miraTTS_german** - German language
- **edwixx/miraTTS-hindi** - Hindi language
- **mradermacher/MiraTTS-GGUF** - Quantized (smaller, faster)
- **uetuluk2/MiraTTS-onnx-int4** - ONNX optimized

### **Training Resources:**
- **Kaggle Notebook:** https://www.kaggle.com/code/yatharthsharma888/miratts-training
- **Colab Notebook:** https://colab.research.google.com/drive/1IprDyaMKaZrIvykMfNrxWFeuvj-DQPII

---

## Proposed Integration Architecture

### **Folder Structure:**
```
RightClicks/
├── MiraTTS/                    # Standalone subfolder (like RVC/)
│   ├── venv/                   # Python virtual environment
│   │   └── Scripts/
│   │       └── python.exe
│   ├── mira/                   # MiraTTS library code
│   │   ├── model.py
│   │   └── ...
│   ├── reference_voices/       # User's reference audio files
│   │   ├── Morgan_Freeman.wav
│   │   ├── David_Attenborough.wav
│   │   └── Custom_Voice_1.mp3
│   ├── trained_models/         # Custom trained models (future)
│   │   └── Southern_Accent/
│   ├── mira_cli.py            # CLI wrapper (like infer_cli.py)
│   └── requirements.txt
```

### **Discovery Pattern (Mirrors RVC):**
```csharp
// Services/MiraTtsReferenceDiscoveryService.cs
public static class MiraTtsReferenceDiscoveryService
{
    public static string GetMiraTtsPath() { /* Priority: deployed, dev, fallback */ }
    public static List<string> DiscoverReferenceVoices() { /* Scan reference_voices/ */ }
    public static bool IsMiraTtsInstalled() { /* Check venv + mira module */ }
}

// Services/MiraTtsFeatureFactory.cs
public static class MiraTtsFeatureFactory
{
    public static List<IFileFeature> CreateMiraTtsFeatures()
    {
        // Discover reference voices
        // Create one feature per voice
        // Return dynamic features
    }
}
```

### **Right-Click Menu:**
```
Right-click document.txt
├── RightClicks ▶
│   ├── MiraTTS ▶
│   │   ├── Morgan Freeman
│   │   ├── David Attenborough
│   │   ├── Custom Voice 1
│   │   └── [Future: Trained Models ▶]
```

---

## Future: Training Integration

### **Vision: RightClicks as Training Hub**

Eventually, RightClicks could support **training custom voices**:

1. **User provides:** 10+ hours of audio + transcripts
2. **RightClicks triggers:** Training job (local or cloud)
3. **Training completes:** New voice model appears in menu
4. **User selects:** Custom trained voice for TTS

### **Training Workflow:**
```
Right-click audio_dataset/
├── RightClicks ▶
│   ├── MiraTTS ▶
│   │   └── Train New Voice Model...
│   │       ├── Select audio files
│   │       ├── Provide transcripts
│   │       ├── Configure training (epochs, batch size)
│   │       └── Start training (background job)
```

### **Challenges:**
- ⏱️ **Time:** Training takes hours/days
- 💾 **Data:** Requires 10+ hours of clean audio + transcripts
- 🖥️ **Resources:** GPU recommended (6GB+ VRAM)
- 📊 **Complexity:** Hyperparameter tuning, dataset preparation

**This is why it's deferred** - significant engineering effort required.

---

## Installation (When Ready)

### **Prerequisites:**
- Python 3.10+
- 6GB+ VRAM (GPU recommended, CPU fallback available)
- ~500MB disk space for base model

### **Setup:**
```bash
# Create Python venv
python -m venv MiraTTS\venv

# Install MiraTTS
MiraTTS\venv\Scripts\pip install git+https://github.com/ysharma3501/MiraTTS.git

# Test standalone
cd MiraTTS
venv\Scripts\python mira_cli.py --text "Hello world" --ref_audio "reference_voices/test.wav" --output "test.mp3"
```

---

## Output Format & Naming

### **Recommended Settings:**
- **Format:** MP3 or AAC (compressed, smaller than WAV)
- **Sample Rate:** 48kHz (MiraTTS native output)
- **Bitrate:** 192kbps (good quality, reasonable size)
- **Naming:** `{original_name}_{voice_name}.mp3`

### **Examples:**
```
document.txt → document_Morgan_Freeman.mp3
chapter1.md  → chapter1_David_Attenborough.mp3
script.txt   → script_Custom_Voice_1.mp3
```

---

## Testing Outside RightClicks (Recommended First Step)

### **Why Test Standalone:**
1. ✅ Understand MiraTTS capabilities and limitations
2. ✅ Experiment with voice cloning quality
3. ✅ Test training process (if pursuing custom voices)
4. ✅ Evaluate output quality vs expectations
5. ✅ Determine if it's worth integrating

### **Standalone Testing Workflow:**

#### **Step 1: Install MiraTTS**
```bash
# Create test directory
mkdir MiraTTS_Test
cd MiraTTS_Test

# Create virtual environment
python -m venv venv

# Activate venv
venv\Scripts\activate

# Install MiraTTS
pip install git+https://github.com/ysharma3501/MiraTTS.git
```

#### **Step 2: Test Voice Cloning**
```python
# test_voice_clone.py
from mira.model import MiraTTS
import soundfile as sf

# Load model
mira_tts = MiraTTS('YatharthS/MiraTTS')

# Provide reference audio (10-30 seconds of clear speech)
reference_file = "reference_voice.wav"

# Text to generate
text = "This is a test of voice cloning. How does it sound?"

# Encode reference audio
context_tokens = mira_tts.encode_audio(reference_file)

# Generate speech
audio = mira_tts.generate(text, context_tokens)

# Save output
sf.write("output.wav", audio, 48000)
print("Generated: output.wav")
```

#### **Step 3: Evaluate Quality**
- ❓ Does it sound like the reference voice?
- ❓ Is the pronunciation clear?
- ❓ Are inflections/emotions preserved? (Likely NO)
- ❓ Does it handle long text well?
- ❓ Is the speed acceptable?

#### **Step 4: Test Training (Optional)**
- Follow Kaggle/Colab notebooks
- Prepare 10+ hours of audio + transcripts
- Train custom voice model
- Evaluate if training improves quality for your use case

### **Decision Point:**
After standalone testing, decide:
- ✅ **Integrate:** If quality meets expectations
- ❌ **Defer:** If quality is insufficient or training is too complex
- 🔄 **Revisit:** If technology improves in the future

---

## Comparison: When to Use What

### **Use RVC When:**
- ✅ You have existing audio with good performance/acting
- ✅ You want to preserve emotional nuance and timing
- ✅ You need regional accents/dialects maintained
- ✅ Source audio already has the "feel" you want
- ✅ Converting voice in songs, speeches, podcasts

### **Use MiraTTS When:**
- ✅ You only have text (no source audio)
- ✅ You need narration for documents/books
- ✅ Consistent, neutral delivery is acceptable
- ✅ Speed is critical (100x realtime)
- ✅ You can train custom models for your specific needs

### **Don't Use MiraTTS When:**
- ❌ You need emotional acting/performance
- ❌ Regional accents/dialects are critical
- ❌ Sarcasm, humor, or subtle inflections matter
- ❌ You want "character" in the voice
- ❌ You need the "human touch"

---

## Technical Specifications

### **Model Details:**
- **Architecture:** LLM-based TTS (built on Qwen 2.5)
- **Parameters:** 0.5B (base model)
- **Output:** 48kHz audio (high quality)
- **Speed:** 100x realtime (with GPU + batching)
- **Memory:** 6GB VRAM (GPU), works on CPU (slower)
- **Languages:** English, Chinese (base), others via finetuning

### **Dependencies:**
- Python 3.10+
- PyTorch
- Lmdeploy (for optimization)
- FlashSR (for audio upsampling)
- librosa (for audio processing)

### **Training Requirements:**
- **Dataset:** 10+ hours of clean audio + transcripts
- **GPU:** Recommended (A100, H100, or similar)
- **Time:** Hours to days depending on dataset size
- **Expertise:** Understanding of TTS training, hyperparameters

---

## Integration Roadmap (When Resumed)

### **Phase 1: Standalone Testing** ⬅️ **START HERE**
- [ ] Install MiraTTS in separate test directory
- [ ] Test voice cloning with 3-5 reference voices
- [ ] Evaluate output quality vs expectations
- [ ] Test training process (optional)
- [ ] Document findings and limitations

### **Phase 2: Proof of Concept**
- [ ] Create `MiraTTS/` subfolder in RightClicks root
- [ ] Set up Python venv and install MiraTTS
- [ ] Create `mira_cli.py` wrapper script
- [ ] Test CLI: `python mira_cli.py --text "..." --ref_audio "..." --output "..."`
- [ ] Collect 3-5 reference audio files

### **Phase 3: Discovery Service**
- [ ] Create `MiraTtsReferenceDiscoveryService.cs`
- [ ] Implement `GetMiraTtsPath()` (deployed, dev, fallback)
- [ ] Implement `DiscoverReferenceVoices()` (scan reference_voices/)
- [ ] Implement `IsMiraTtsInstalled()` (check venv + mira module)

### **Phase 4: Feature Factory**
- [ ] Create `MiraTtsFeatureFactory.cs`
- [ ] Implement `CreateMiraTtsFeatures()` (dynamic feature generation)
- [ ] Create `MiraTtsFeatureBase.cs` (base class for TTS features)
- [ ] Implement `ExecuteAsync()` (call Python CLI, handle output)

### **Phase 5: Integration**
- [ ] Add to `FeatureDiscoveryService.cs` (like RVC)
- [ ] Test via CLI: `RightClicks.exe --feature MiraTtsMorganFreeman --file document.txt`
- [ ] Verify output files created correctly
- [ ] Check logs for errors

### **Phase 6: Context Menu**
- [ ] Add `.txt`, `.md` to supported extensions
- [ ] Test right-click menu (should show "MiraTTS" submenu)
- [ ] Verify dynamic population of reference voices
- [ ] Test cascading menu behavior

### **Phase 7: Deployment**
- [ ] Update `install.bat` to deploy MiraTTS
- [ ] Copy venv, mira/, reference_voices/ to %LOCALAPPDATA%\RightClicks\MiraTTS\
- [ ] Test on clean machine
- [ ] Document installation requirements

### **Phase 8: Training Support (Future)**
- [ ] Design training workflow UI
- [ ] Implement background training jobs
- [ ] Add progress tracking
- [ ] Auto-discover trained models
- [ ] Add to context menu

---

## Known Limitations

### **Current Limitations:**
1. **No Pre-Trained Voices:** Unlike RVC, no ready-to-use voice models
2. **Training Required:** Custom voices need 10+ hours of data + training time
3. **Flat Delivery:** TTS lacks human acting/emotion/nuance
4. **Accent Loss:** Regional accents not preserved unless trained specifically
5. **No Inflection Control:** Can't specify sarcasm, emphasis, pauses
6. **GPU Recommended:** CPU inference is much slower
7. **Large Model:** ~500MB base model download

### **Comparison to RVC:**
- RVC: 24 pre-trained voices, ready to use immediately
- MiraTTS: 0 pre-trained voices, requires training or reference audio

---

## Resources

### **Official:**
- **GitHub:** https://github.com/ysharma3501/MiraTTS
- **Hugging Face:** https://huggingface.co/YatharthS/MiraTTS
- **Demo Space:** https://huggingface.co/spaces/Gapeleon/Mira-TTS

### **Training:**
- **Kaggle Notebook:** https://www.kaggle.com/code/yatharthsharma888/miratts-training
- **Colab Notebook:** https://colab.research.google.com/drive/1IprDyaMKaZrIvykMfNrxWFeuvj-DQPII

### **Documentation:**
- **How LLM TTS Works:** https://huggingface.co/blog/YatharthS/llm-tts-models
- **Optimization Guide:** https://huggingface.co/blog/YatharthS/making-neutts-200x-realtime

### **Community:**
- **Reddit Discussion:** https://www.reddit.com/r/LocalLLaMA/comments/1pper90/miratts_high_quality_and_fast_tts_model/
- **Hacker News:** https://news.ycombinator.com/item?id=46314749

---

## Decision: Why Deferred

### **Reasons for Postponement:**

1. **✅ RVC is Superior for Voice Conversion**
   - Preserves acting, emotion, nuance, timing
   - 24 ready-to-use voices
   - No training required

2. **⏱️ Training Time Investment**
   - Requires 10+ hours of audio data
   - Training takes hours/days
   - Hyperparameter tuning needed

3. **🎭 Acting/Nuance Loss**
   - TTS can't replicate human performance
   - Southern drawl, sarcasm, emphasis lost
   - Flat, robotic delivery

4. **🧪 Need Standalone Testing First**
   - Must evaluate quality before integration
   - Understand limitations hands-on
   - Determine if worth the effort

5. **🔮 Technology May Improve**
   - TTS models evolving rapidly
   - Future models may have better emotion/acting
   - Revisit when technology matures

### **When to Revisit:**

- ✅ After standalone testing shows acceptable quality
- ✅ When pre-trained voices become available
- ✅ If training process becomes simpler/faster
- ✅ When TTS models gain emotion/acting capabilities
- ✅ If user demand for TTS features emerges

---

## Final Notes

**MiraTTS is interesting technology**, but it serves a different purpose than RVC:

- **RVC:** Voice conversion (preserves performance)
- **MiraTTS:** Text-to-speech (generates from scratch)

**For RightClicks, RVC is the better fit** because:
1. Pre-trained voices ready to use
2. Preserves emotional nuance and acting
3. No training required
4. Better for converting existing audio

**MiraTTS may be valuable in the future** for:
1. Narrating text documents
2. Generating voiceovers from scripts
3. Creating audiobooks from markdown

**Recommendation:** Test MiraTTS standalone first, understand its capabilities and limitations, then decide if integration makes sense for RightClicks' use cases.

---

**Document Version:** 1.0
**Author:** RightClicks Development
**Status:** Planning / Deferred
**Next Action:** Standalone testing outside RightClicks


