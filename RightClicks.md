# RightClicks – Windows context menu extension

## Scope
* Adds a top-level context menu entry called RightClicks when the user right-clicks certain objects.
* Entry expands to a cascade of actions tailored to the object type.

## File type: .mp4

**Top menu entry:** RightClicks

**Cascaded actions:**

### Context
* When the user right clicks a .mp4 file in Windows Explorer, the top menu item is RightClicks.
* Hovering RightClicks opens a cascade of actions tailored for .mp4.

### Actions and exact behaviors (order alphanumeric ascending by name)

#### 1. Audio to mp3
* Extract only the stereo audio track from the selected .mp4.
* Create a new file in the same folder.
* Output name pattern: `<same base name>.mp3`.

#### 2. Audio to wav
* Extract only the stereo audio track from the selected .mp4.
* Create a new file in the same folder.
* Output name pattern: `<same base name>.wav`.

#### 3. First Frame to Jpg
* Capture the first video frame.
* Create a new image file in the same folder.
* Output name pattern: `<same base name>_First.jpg`.

#### 4. Last Frame to Jpg
* Capture the last video frame.
* Create a new image file in the same folder.
* Output name pattern: `<same base name>_Last.jpg`.

#### 5. Reverse video
* Create a reversed version of the original .mp4 video.
* Do not alter the original file.
* Output name pattern: `<same base name>_Reverse.mp4`.

#### 6. Forward2Reverse
* Create a new .mp4 that is the original video followed by its reversed version.
* Do not alter the original file.
* Output name pattern: `<same base name>_Forward2Reverse.mp4`.

#### 7. Time Stretch
* Open a dialog displaying the exact original duration.
* Provide a control to lengthen or shorten the video duration.
* If lengthened, produce a file where playback is stretched to the target duration by smoothly adding frames throughout, copying previous frames as needed to evenly spread added frames over the entire timeline.
* If shortened, produce a file where frames are dropped throughout the timeline to evenly reach the target duration.
* Create the new file in the same folder.
* Output name pattern: `<same base name>_Stretch.mp4`.

### UI details for Time Stretch dialog
* Read only display of original duration.
* One slider to set target duration relative to the original.
* Numeric input for precise target duration.
* Ok and Cancel buttons.

### File handling rules for all actions above
* Operate on the selected .mp4.
* Write outputs to the same folder as the source.
* Use the exact output name patterns listed.
* Original file is never modified.

---

## On a text highlight:
* Rephrase, set tone to business
* Rephrase, set tone to snarky

---

## File type: .mp3
* Mp3 to transcribe

---

## File Types: .TXT
* Content to Clipboard
* Clipboard to File (only if zero bytes)

---

## Image Conversions
* Jpg to Png
* Png to Jpg
* Webm, WebP

---

## GLSL Shader Files (.glsl, .frag)
* Shadertoy to GLSL
* GLSL to ShaderToy

