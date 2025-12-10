using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml.Linq;
using SystemTrayApp.Properties;
using System.Linq;
using System.Drawing;
using System.Text.RegularExpressions;
using System.IO;
using System.Net;

/*  I would want to work on ballon type notification if I came back here. 
 * 
 * 
 */



namespace SystemTrayApp
{
	/// <summary>
	/// 
	/// </summary>
	class ProcessIcon : IDisposable
	{
		/// <summary>
		/// The NotifyIcon object.
		/// </summary>
		NotifyIcon ni;
        static string previousClipboardText;
        private string logFilePath;


        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessIcon"/> class.
        /// </summary>
        public ProcessIcon()
		{
			// Instantiate the NotifyIcon object.
			ni = new NotifyIcon();

            // Initialize log file in the application directory
            logFilePath = Path.Combine(Application.StartupPath, "SystemTrayApp_Debug.log");
            LogMessage("=== Application Started ===");
            LogMessage($"Log file location: {logFilePath}");
		}

		/// <summary>
		/// Displays the icon in the system tray.
		/// </summary>
		public void Display()
		{
			// Put the icon in the system tray and allow it react to mouse clicks.
			ni.MouseClick += new MouseEventHandler(ni_MouseClick);
			ni.Icon = Resources.SystemTrayApp;
			ni.Text = "System Tray Utility Application Demonstration Program";
			ni.Visible = true;


			// Attach a context menu.
			ni.ContextMenuStrip = new ContextMenus().Create();


            // Check if yt-dlp.exe exists
            if (!CheckYtDlpExists())
            {
                LogMessage("yt-dlp.exe not found. Offering to download...");

                DialogResult result = MessageBox.Show(
                    "yt-dlp.exe not found!\n\n" +
                    "This application requires yt-dlp.exe to download videos.\n\n" +
                    "Would you like to download it automatically now?\n\n" +
                    "(It will be downloaded from: https://github.com/yt-dlp/yt-dlp/releases)",
                    "Download yt-dlp.exe?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    if (DownloadYtDlp())
                    {
                        LogMessage("yt-dlp.exe downloaded successfully!");
                        ShowNotification("yt-dlp.exe downloaded successfully!");
                    }
                    else
                    {
                        LogMessage("Failed to download yt-dlp.exe");
                        MessageBox.Show(
                            "Failed to download yt-dlp.exe automatically.\n\n" +
                            "Please download it manually from:\n" +
                            "https://github.com/yt-dlp/yt-dlp/releases\n\n" +
                            "Place yt-dlp.exe in:\n" +
                            Application.StartupPath + "\n" +
                            "or C:\\bin\\",
                            "Download Failed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);

                        try
                        {
                            Process.Start("https://github.com/yt-dlp/yt-dlp/releases");
                        }
                        catch { }

                        Application.Exit();
                        return;
                    }
                }
                else
                {
                    Application.Exit();
                    return;
                }
            }

            // delete this shit
            //get a list of URLS that I will react to
            //Debug.WriteLine("call LoadUrlsFromXml.");
            //LoadUrlsFromXml();

            previousClipboardText = Clipboard.GetText();

            // Create a timer to periodically check the clipboard
            Timer clipboardTimer = new Timer();
            clipboardTimer.Interval = 1000; // Check every 1 second
            clipboardTimer.Tick += ClipboardCheck;
            clipboardTimer.Start();
        }

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		public void Dispose()
		{
			// When the application closes, this will remove the icon from the system tray immediately.
			ni.Dispose();
		}

		/// <summary>
		/// Handles the MouseClick event of the ni control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.Forms.MouseEventArgs"/> instance containing the event data.</param>
		void ni_MouseClick(object sender, MouseEventArgs e)
		{
			// Handle mouse button clicks.
			if (e.Button == MouseButtons.Left)
			{
				// Open today's Memes folder
				OpenTodaysMemeFolder();
			}
		}


        /// <summary>
        /// this is shit to delete
        /// </summary>
        private List<string> urlsToCheck;
        private void LoadUrlsFromXml()
        {
            Debug.WriteLine("in LoadUrlsFromXml.");
            urlsToCheck = new List<string>();
            try
            {
                XDocument xmlDoc = XDocument.Load("urls.xml");
                var urls = xmlDoc.Descendants("Url").Select(url => url.Value);
                urlsToCheck.AddRange(urls);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("LoadUrlsFromXml error.");
                //Console.WriteLine("Error loading URLs from xml file: " + ex.Message);
                ShowNotification("Error loading URLs from xml file: " + ex.Message);
            }
        }


        private void ShowNotification(string text)
        {

            // Show the NotifyIcon
            ni.Visible = true;

            ni.BalloonTipText = text;
            ni.ShowBalloonTip(2000); // Show notification for 2 seconds


        }

        /// <summary>
        /// Opens today's Memes folder in Windows Explorer
        /// </summary>
        private void OpenTodaysMemeFolder()
        {
            try
            {
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
                string memesFolderPath = Path.Combine(documentsPath, $"Memes_{currentDate}");

                LogMessage($"Opening folder: {memesFolderPath}");

                // Check if today's folder exists
                if (Directory.Exists(memesFolderPath))
                {
                    // Open the folder
                    Process.Start("explorer.exe", memesFolderPath);
                    LogMessage("Folder opened successfully");
                }
                else
                {
                    // Folder doesn't exist yet, create it and open
                    LogMessage("Today's folder doesn't exist yet, creating it...");
                    Directory.CreateDirectory(memesFolderPath);
                    Process.Start("explorer.exe", memesFolderPath);
                    LogMessage("Folder created and opened");

                    ShowNotification($"Created today's folder: Memes_{currentDate}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error opening folder: {ex.Message}");
                // Fallback to opening Documents folder
                try
                {
                    string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    Process.Start("explorer.exe", documentsPath);
                    LogMessage("Opened Documents folder as fallback");
                }
                catch
                {
                    // Last resort - just open Explorer
                    Process.Start("explorer.exe");
                    LogMessage("Opened Explorer as last resort");
                }
            }
        }

        /// <summary>
        /// Finds yt-dlp.exe in multiple locations
        /// </summary>
        /// <returns>Full path to yt-dlp.exe if found, null otherwise</returns>
        private string FindYtDlpPath()
        {
            LogMessage("Searching for yt-dlp.exe...");

            // 1. Check application directory
            string appDirPath = Path.Combine(Application.StartupPath, "yt-dlp.exe");
            LogMessage($"  Checking: {appDirPath}");
            if (File.Exists(appDirPath))
            {
                LogMessage($"  ✓ Found at: {appDirPath}");
                return appDirPath;
            }

            // 2. Check C:\bin
            string cBinPath = @"C:\bin\yt-dlp.exe";
            LogMessage($"  Checking: {cBinPath}");
            if (File.Exists(cBinPath))
            {
                LogMessage($"  ✓ Found at: {cBinPath}");
                return cBinPath;
            }

            // 3. Check PATH environment variable
            string pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (pathEnv != null)
            {
                foreach (string path in pathEnv.Split(';'))
                {
                    try
                    {
                        string fullPath = Path.Combine(path.Trim(), "yt-dlp.exe");
                        if (File.Exists(fullPath))
                        {
                            LogMessage($"  ✓ Found in PATH at: {fullPath}");
                            return fullPath;
                        }
                    }
                    catch { }
                }
            }

            LogMessage("  ✗ yt-dlp.exe not found in any location");
            return null;
        }

        /// <summary>
        /// Checks if yt-dlp.exe exists in any known location
        /// </summary>
        /// <returns>True if yt-dlp.exe exists, false otherwise</returns>
        private bool CheckYtDlpExists()
        {
            string ytDlpPath = FindYtDlpPath();
            return ytDlpPath != null;
        }

        /// <summary>
        /// Logs a message to the debug log file with timestamp
        /// </summary>
        private void LogMessage(string message)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logEntry = $"[{timestamp}] {message}";
                File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
                Debug.WriteLine(logEntry);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write to log: {ex.Message}");
            }
        }

        /// <summary>
        /// Downloads yt-dlp.exe from GitHub releases
        /// </summary>
        /// <returns>True if download successful, false otherwise</returns>
        private bool DownloadYtDlp()
        {
            try
            {
                LogMessage("Starting yt-dlp.exe download...");

                // Latest release URL for yt-dlp.exe
                string downloadUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
                string targetPath = Path.Combine(Application.StartupPath, "yt-dlp.exe");

                // Download to TEMP first to avoid antivirus interference
                string tempPath = Path.Combine(Path.GetTempPath(), "yt-dlp-download.exe");

                LogMessage($"Download URL: {downloadUrl}");
                LogMessage($"Temporary download path: {tempPath}");
                LogMessage($"Final target path: {targetPath}");

                // Delete temp file if it exists from previous failed download
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                    LogMessage("Deleted existing temporary file");
                }

                using (WebClient client = new WebClient())
                {
                    // Add user agent to avoid 403 errors
                    client.Headers.Add("User-Agent", "SystemTrayApp/1.0");

                    // Show progress
                    client.DownloadProgressChanged += (s, e) =>
                    {
                        if (e.ProgressPercentage % 10 == 0)
                        {
                            LogMessage($"Download progress: {e.ProgressPercentage}%");
                        }
                    };

                    // Download to TEMP folder first (avoids antivirus corruption)
                    client.DownloadFile(downloadUrl, tempPath);
                }

                LogMessage($"Download complete. File size: {new FileInfo(tempPath).Length} bytes");

                // Verify the downloaded file exists and has reasonable size (should be several MB)
                if (!File.Exists(tempPath) || new FileInfo(tempPath).Length < 1000000)
                {
                    LogMessage("Downloaded file is invalid or too small");
                    return false;
                }

                LogMessage("Download verified. Copying to application folder...");

                // Copy from TEMP to target location
                File.Copy(tempPath, targetPath, true);

                // Verify the final file
                if (File.Exists(targetPath) && new FileInfo(targetPath).Length > 1000000)
                {
                    LogMessage($"yt-dlp.exe successfully installed. Final size: {new FileInfo(targetPath).Length} bytes");

                    // Clean up temp file
                    try
                    {
                        File.Delete(tempPath);
                        LogMessage("Cleaned up temporary download file");
                    }
                    catch
                    {
                        // Not critical if cleanup fails
                        LogMessage("Note: Could not delete temporary file (not critical)");
                    }

                    return true;
                }
                else
                {
                    LogMessage("Final file verification failed");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error downloading yt-dlp.exe: {ex.Message}");
                LogMessage($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }


        private void ClipboardCheck(object sender, EventArgs e)
        {
            string currentClipboardText = Clipboard.GetText();

            if (currentClipboardText != previousClipboardText)
            {
                previousClipboardText = currentClipboardText;

                // Trim whitespace from clipboard text
                string clipboardText = currentClipboardText.Trim();

                LogMessage("=== CLIPBOARD CHANGED ===");
                LogMessage($"Raw clipboard text: '{currentClipboardText}'");
                LogMessage($"Trimmed clipboard text: '{clipboardText}'");
                LogMessage($"Length: {clipboardText.Length} characters");

                // Log each character for debugging invisible characters
                if (clipboardText.Length > 0 && clipboardText.Length < 200)
                {
                    LogMessage($"Character codes: {string.Join(",", clipboardText.Select(c => ((int)c).ToString()))}");
                }

                bool video = IsTwitterVideoUrl(clipboardText);
                bool video2 = IsYouTubeVideoUrl(clipboardText);

                LogMessage($"Twitter match result: {video}");
                LogMessage($"YouTube match result: {video2}");

                if (video || video2)
                {
                    LogMessage("✓ VIDEO URL DETECTED - Starting download!");
                    StartSomeShit(clipboardText);
                }
                else
                {
                    LogMessage("✗ No video URL match");
                }

                LogMessage(""); // Empty line for readability
            }
        }


        public static bool IsTwitterVideoUrl(string input)
        {
            // Define the regex pattern
            string pattern = @"https://x\.com/i/status/\d+";

            // Use Regex.IsMatch to check if the input matches the pattern
            bool result = Regex.IsMatch(input, pattern);
            return result;
        }

        public static bool IsYouTubeVideoUrl(string input)
        {
            // Define regex patterns for various YouTube URL formats
            // Matches: https://youtu.be/VIDEO_ID
            //          https://www.youtube.com/watch?v=VIDEO_ID
            //          https://youtube.com/watch?v=VIDEO_ID
            //          https://m.youtube.com/watch?v=VIDEO_ID
            // Also matches http:// variants and additional query parameters

            // Pattern breakdown:
            // ^https?:\/\/ - starts with http:// or https://
            // (www\.|m\.)? - optional www. or m. subdomain
            // (youtube\.com\/watch\?v=[\w-]+|youtu\.be\/[\w-]+) - either format with video ID
            // .*$ - any additional query parameters or content after
            string pattern = @"^https?:\/\/(www\.|m\.)?(youtube\.com\/watch\?v=[\w-]+|youtu\.be\/[\w-]+).*$";

            // Use Regex.IsMatch to check if the input matches the pattern
            bool result = Regex.IsMatch(input, pattern);
            return result;
        }

        private bool ClipboardContainsMatchingURL(string clipboardText)
        {
            foreach (string url in urlsToCheck)
            {
                if (clipboardText.Contains(url))
                {
                    return true;
                }
            }
            return false;
        }

        private void StartSomeShit(string videourl)
        {
            LogMessage($"Starting download for URL: {videourl}");

            // Find yt-dlp.exe
            string ytDlpPath = FindYtDlpPath();
            if (ytDlpPath == null)
            {
                LogMessage("ERROR: yt-dlp.exe not found!");
                ShowNotification("Error: yt-dlp.exe not found!");
                return;
            }

            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            // Get the current date in the format "YYYY-MM-DD"
            string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
            string memesFolderPath = Path.Combine(documentsPath, $"Memes_{currentDate}");

            LogMessage($"Target folder: {memesFolderPath}");
            LogMessage($"Using yt-dlp at: {ytDlpPath}");

            if (!Directory.Exists(memesFolderPath))
            {
                // The folder doesn't exist, so create it
                Directory.CreateDirectory(memesFolderPath);
                LogMessage("Created folder: " + memesFolderPath);
            }
            else
            {
                LogMessage("Folder already exists: " + memesFolderPath);
            }

            // Create a batch file with the specified command and parameter
            string batchFilePath = Path.Combine(memesFolderPath, "DownloadVideo.bat");

            if (!File.Exists(batchFilePath))
            {
                // Create the batch file if it doesn't exist
                using (StreamWriter sw = new StreamWriter(batchFilePath))
                {
                    sw.WriteLine("@echo off");
                    sw.WriteLine($"CD \"{memesFolderPath}\"");
                    sw.WriteLine($"\"{ytDlpPath}\" %1");
                }
                LogMessage("Created batch file: " + batchFilePath);
            }

            // Run the batch file with a URL parameter
            LogMessage($"Executing: {batchFilePath} with parameter: {videourl}");
            Process.Start(batchFilePath, $" {videourl}");
            LogMessage("Download process started");

            // Show notification to user
            ShowNotification("Video download started!");
        }

    }
}