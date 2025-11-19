# Cloudinary Configuration for RightClicks

## Overview

RightClicks uses **Cloudinary** as the primary file hosting service for features that require temporary cloud storage (e.g., fal.ai lip sync models). Cloudinary provides enterprise-grade, reliable file hosting with a generous free tier.

## Why Cloudinary?

During development, we initially used **0x0.st** (a free, anonymous file hosting service) because it was simple and required no authentication. However, we discovered that **0x0.st has critical server-side bugs**:

- **Segmentation faults** when external APIs (like fal.ai) try to retrieve uploaded files
- Server crashes at `fhost.c:139` with null pointer dereference
- Completely unreliable for production use

After testing multiple alternatives (fal.ai storage, file.io), we settled on **Cloudinary** because:
- ✅ Enterprise-grade reliability
- ✅ Generous free tier (25 GB storage, 25 GB bandwidth/month)
- ✅ Fast global CDN
- ✅ Supports unsigned uploads (no API keys exposed in client code)
- ✅ Works perfectly with fal.ai and other external APIs

## Required Setup for End Users

### 1. Create a Free Cloudinary Account

1. Go to [https://cloudinary.com/users/register_free](https://cloudinary.com/users/register_free)
2. Sign up for a free account
3. Verify your email address
4. Log in to your Cloudinary dashboard

### 2. Get Your Cloud Name

1. In the Cloudinary dashboard, look at the top-left corner
2. You'll see your **Cloud name** (e.g., `do15ttvsq`)
3. Copy this value - you'll need it for configuration

### 3. Get Your API Credentials

1. In the Cloudinary dashboard, go to **Settings** → **Access Keys**
2. You'll see:
   - **API Key** (e.g., `123456789012345`)
   - **API Secret** (e.g., `abcdefghijklmnopqrstuvwxyz123456`)
3. Copy both values

### 4. Create an Unsigned Upload Preset

This is **required** for RightClicks to upload files without exposing your API secret.

1. Go to **Settings** → **Upload** → **Upload presets**
2. Click **"Add upload preset"**
3. Configure the preset:
   - **Upload preset name**: `RightClicks` (must be exactly this name)
   - **Signing Mode**: Select **"Unsigned"**
   - **Asset folder**: `RightClicks` (optional, but recommended for organization)
   - **Use filename as display name**: Enable (optional)
4. Click **"Save"**

### 5. Configure Cloudinary in RightClicks

RightClicks stores API keys securely in **Windows environment variables** (not in config files). You can configure Cloudinary using the built-in UI.

#### Using RightClicks UI (Recommended)

1. Launch RightClicks (system tray icon)
2. Right-click the tray icon → **"Settings"**
3. Go to the **"API Config"** tab
4. Scroll down to the **"Cloudinary Configuration"** section
5. Enter your credentials:
   - **Cloud Name**: Your cloud name from step 2 (e.g., `do15ttvsq`)
   - **API Key**: Your API Key from step 3
   - **API Secret**: Your API Secret from step 3
6. Click **"Save Cloudinary Configuration"**
7. **Restart RightClicks** for changes to take effect

**What this does:**
- Saves your Cloud Name to `config.json`
- Creates Windows environment variables:
  - `CLOUDINARY_API_KEY` (your API key)
  - `CLOUDINARY_API_SECRET` (your API secret)

#### Manual Configuration (Advanced)

If you prefer to configure manually:

1. **Set environment variables:**
   - Open **Windows Settings** → **System** → **About** → **Advanced system settings**
   - Click **"Environment Variables"**
   - Under **"User variables"**, click **"New"**
   - Add two variables:
     - Variable name: `CLOUDINARY_API_KEY`, Value: Your API Key
     - Variable name: `CLOUDINARY_API_SECRET`, Value: Your API Secret
   - Click **"OK"** to save

2. **Update config.json:**
   - Location: `%LOCALAPPDATA%\RightClicks\config.json`
   - Edit the `Cloudinary` section:
     ```json
     {
       "Cloudinary": {
         "CloudName": "your_cloud_name_here",
         "ApiKeyEnvVar": "CLOUDINARY_API_KEY",
         "ApiSecretEnvVar": "CLOUDINARY_API_SECRET"
       }
     }
     ```

3. **Restart RightClicks** for changes to take effect

## Configuration Touchpoints

### 1. AppConfig.cs (Code)

**Location**: `RightClicks/Models/AppConfig.cs`

```csharp
public class CloudinaryConfig
{
    public string CloudName { get; set; } = "do15ttvsq";
    public string ApiKeyEnvVar { get; set; } = "CLOUDINARY_API_KEY";
    public string ApiSecretEnvVar { get; set; } = "CLOUDINARY_API_SECRET";
}
```

### 2. config.json (User Configuration)

**Location**: `%LOCALAPPDATA%\RightClicks\config.json`

```json
{
  "Cloudinary": {
    "CloudName": "do15ttvsq",
    "ApiKeyEnvVar": "CLOUDINARY_API_KEY",
    "ApiSecretEnvVar": "CLOUDINARY_API_SECRET"
  }
}
```

### 3. Environment Variables (Secure Storage)

- `CLOUDINARY_API_KEY` - Your Cloudinary API key
- `CLOUDINARY_API_SECRET` - Your Cloudinary API secret

### 4. Cloudinary Dashboard (Upload Preset)

- **Preset Name**: `RightClicks` (hardcoded in `CloudinaryStorageService.cs`)
- **Signing Mode**: Unsigned
- **Asset Folder**: `RightClicks` (optional)

## How It Works

1. **User triggers a feature** (e.g., right-click video → Lip Sync → fal.ai.Kling)
2. **RightClicks uploads files to Cloudinary** using the unsigned upload preset
3. **Cloudinary returns public URLs** (e.g., `https://res.cloudinary.com/...`)
4. **RightClicks sends URLs to fal.ai API** for processing
5. **fal.ai downloads files from Cloudinary** (fast, reliable)
6. **fal.ai processes the files** and returns the result
7. **RightClicks downloads the result** and saves it next to the original file

## File Management

### Automatic Cleanup

✅ **RightClicks automatically deletes files from Cloudinary after processing completes** (both success and failure cases).

**How it works:**
1. File is uploaded to Cloudinary before processing
2. Processing occurs (e.g., fal.ai lip sync)
3. Result is downloaded and saved locally
4. **Original file is automatically deleted from Cloudinary**

This ensures:
- ✅ No manual cleanup required
- ✅ Minimal storage usage on your Cloudinary account
- ✅ Privacy - files are only stored temporarily

### Manual Cleanup (If Needed)

If you need to manually delete files (e.g., after a crash or error):

1. Log in to your Cloudinary dashboard
2. Go to **Media Library**
3. Navigate to the **"RightClicks"** folder
4. Select files you want to delete
5. Click **"Delete"**

## Troubleshooting

### "Upload preset must be specified when using unsigned upload"

**Cause**: The unsigned upload preset is not configured correctly in your Cloudinary account.

**Solution**:
1. Go to Cloudinary dashboard → Settings → Upload → Upload presets
2. Verify that a preset named **"RightClicks"** exists
3. Verify that **Signing Mode** is set to **"Unsigned"**
4. If the preset doesn't exist, create it (see step 4 above)

### "Failed to upload file to Cloudinary: Unauthorized"

**Cause**: API credentials are missing or incorrect.

**Solution**:
1. Verify that environment variables are set correctly:
   ```powershell
   $env:CLOUDINARY_API_KEY
   $env:CLOUDINARY_API_SECRET
   ```
2. If empty, set them using the RightClicks UI or manually (see step 5 above)
3. Restart RightClicks after setting environment variables

### "Cloudinary upload failed, falling back to 0x0.st"

**Cause**: Cloudinary upload failed for some reason (network issue, quota exceeded, etc.)

**Solution**:
1. Check your Cloudinary dashboard for quota usage
2. Check the RightClicks log file for detailed error messages:
   - Location: `%LOCALAPPDATA%\RightClicks\logs\RightClicks-YYYYMMDD-HHMMSS.log`
3. If quota is exceeded, upgrade your Cloudinary plan or delete old files

## Free Tier Limits

Cloudinary's free tier includes:
- **25 GB** storage
- **25 GB** bandwidth per month
- **Unlimited** transformations
- **Unlimited** uploads

For typical RightClicks usage (lip sync videos), this is more than enough for personal use.

## Security Notes

- ✅ API keys are stored in **environment variables**, not in config files
- ✅ Unsigned uploads use a **preset**, not direct API credentials
- ✅ The preset name is public, but it's restricted to your Cloudinary account
- ✅ No sensitive data is exposed in logs or error messages
- ⚠️ Uploaded files are **publicly accessible** via their URLs (but URLs are unguessable)

## Support

For Cloudinary-specific issues, see:
- [Cloudinary Documentation](https://cloudinary.com/documentation)
- [Cloudinary Support](https://support.cloudinary.com/)

For RightClicks-specific issues, check the logs:
- Location: `%LOCALAPPDATA%\RightClicks\logs\`

