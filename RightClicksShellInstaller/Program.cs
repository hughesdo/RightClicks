using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RightClicksShellInstaller
{
    /// <summary>
    /// Shell extension installer/uninstaller using RegAsm.exe
    /// </summary>
    class Program
    {
        private static readonly string SHELL_EXTENSION_DLL = "RightClicksShellExtension.dll";
        private static readonly string REGASM_PATH = @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\regasm.exe";

        static int Main(string[] args)
        {
            // Create log file
            string logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RightClicks",
                "logs",
                $"ShellInstaller-{DateTime.Now:yyyyMMdd-HHmmss}.log"
            );

            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            var logWriter = new StreamWriter(logPath, false);
            logWriter.AutoFlush = true;

            void Log(string message)
            {
                Console.WriteLine(message);
                logWriter.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
            }

            Log("=== RightClicks Shell Extension Installer ===");
            Log("");

            if (args.Length == 0)
            {
                ShowUsage();
                logWriter.Close();
                return 1;
            }

            string command = args[0].ToLowerInvariant();
            Log($"Command: {command}");

            try
            {
                int exitCode;
                switch (command)
                {
                    case "/install":
                    case "-install":
                        exitCode = Install(Log);
                        break;

                    case "/uninstall":
                    case "-uninstall":
                        exitCode = Uninstall(Log);
                        break;

                    default:
                        Log($"Unknown command: {command}");
                        ShowUsage();
                        exitCode = 1;
                        break;
                }

                Log($"Exit code: {exitCode}");
                logWriter.Close();

                // Keep console open for 3 seconds so user can see result
                System.Threading.Thread.Sleep(3000);

                return exitCode;
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex.Message}");
                Log("");
                Log(ex.StackTrace ?? "");
                logWriter.Close();

                // Keep console open for 5 seconds on error
                System.Threading.Thread.Sleep(5000);

                return 1;
            }
        }

        static void ShowUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  RightClicksShellInstaller.exe /install    - Install shell extension");
            Console.WriteLine("  RightClicksShellInstaller.exe /uninstall  - Uninstall shell extension");
            Console.WriteLine();
            Console.WriteLine("Note: Requires administrator privileges");
        }

        static int Install(Action<string> Log)
        {
            Log("Installing RightClicks shell extension...");
            Log("");

            // Use %LOCALAPPDATA%\RightClicks as the installation directory
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string installDir = Path.Combine(localAppData, "RightClicks");
            Log($"Installation directory: {installDir}");

            string dllPath = Path.Combine(installDir, SHELL_EXTENSION_DLL);
            Log($"DLL path: {dllPath}");

            // Check if DLL exists
            if (!File.Exists(dllPath))
            {
                Log($"ERROR: {SHELL_EXTENSION_DLL} not found!");
                Log($"Expected location: {dllPath}");
                Log($"Make sure you have built the solution and files are copied to %LOCALAPPDATA%\\RightClicks\\");
                return 1;
            }
            Log($"DLL found: {dllPath}");

            // Check if RegAsm exists
            if (!File.Exists(REGASM_PATH))
            {
                Log($"ERROR: RegAsm.exe not found!");
                Log($"Expected location: {REGASM_PATH}");
                return 1;
            }
            Log($"RegAsm found: {REGASM_PATH}");

            // Uninstall first (in case already installed)
            Log("Uninstalling existing installation (if any)...");
            RunRegAsm(dllPath, false, Log);

            // Kill Windows Explorer
            Log("Stopping Windows Explorer...");
            KillExplorer(Log);
            System.Threading.Thread.Sleep(1000);

            // Install
            Log($"Registering {SHELL_EXTENSION_DLL}...");
            bool success = RunRegAsm(dllPath, true, Log);

            if (success)
            {
                // Approve the shell extension in the registry
                Log("Approving shell extension in registry...");
                success = ApproveShellExtension(Log);
            }

            // Restart Windows Explorer
            Log("Starting Windows Explorer...");
            StartExplorer(Log);

            if (success)
            {
                Log("");
                Log("SUCCESS: Shell extension installed successfully!");
                Log("You can now right-click on files in Windows Explorer to see the RightClicks menu.");
                return 0;
            }
            else
            {
                Log("");
                Log("WARNING: Installation may have failed. Check the output above for errors.");
                return 1;
            }
        }

        static int Uninstall(Action<string> Log)
        {
            Log("Uninstalling RightClicks shell extension...");
            Log("");

            // Get the directory where this executable is located
            string? exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Log($"Executable directory: {exeDir}");

            string dllPath = Path.Combine(exeDir!, SHELL_EXTENSION_DLL);
            Log($"DLL path: {dllPath}");

            // Check if DLL exists
            if (!File.Exists(dllPath))
            {
                Log($"WARNING: {SHELL_EXTENSION_DLL} not found!");
                Log($"Expected location: {dllPath}");
                Log("Attempting to uninstall anyway...");
            }
            else
            {
                Log($"DLL found: {dllPath}");
            }

            // Check if RegAsm exists
            if (!File.Exists(REGASM_PATH))
            {
                Log($"ERROR: RegAsm.exe not found!");
                Log($"Expected location: {REGASM_PATH}");
                return 1;
            }
            Log($"RegAsm found: {REGASM_PATH}");

            // Kill Windows Explorer
            Log("Stopping Windows Explorer...");
            KillExplorer(Log);
            System.Threading.Thread.Sleep(1000);

            // Uninstall
            Log($"Unregistering {SHELL_EXTENSION_DLL}...");
            bool success = RunRegAsm(dllPath, false, Log);

            // Restart Windows Explorer
            Log("Starting Windows Explorer...");
            StartExplorer(Log);

            if (success)
            {
                Log("");
                Log("SUCCESS: Shell extension uninstalled successfully!");
                return 0;
            }
            else
            {
                Log("");
                Log("WARNING: Uninstallation may have failed. Check the output above for errors.");
                return 1;
            }
        }

        /// <summary>
        /// Runs RegAsm.exe to register or unregister the shell extension DLL
        /// </summary>
        static bool RunRegAsm(string dllPath, bool install, Action<string> Log)
        {
            string arguments = install
                ? $"\"{dllPath}\" /codebase"
                : $"\"{dllPath}\" /u";

            var startInfo = new ProcessStartInfo
            {
                FileName = REGASM_PATH,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Log($"Running: {REGASM_PATH} {arguments}");
            Log("");

            var process = Process.Start(startInfo);

            if (process == null)
            {
                Log("ERROR: Failed to start RegAsm process!");
                return false;
            }

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            Log($"RegAsm exit code: {process.ExitCode}");

            if (!string.IsNullOrWhiteSpace(output))
            {
                Log("RegAsm output:");
                Log(output);
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                Log("RegAsm STDERR:");
                Log(error);
            }

            return process.ExitCode == 0;
        }

        /// <summary>
        /// Kills all Windows Explorer processes
        /// </summary>
        static void KillExplorer(Action<string> Log)
        {
            try
            {
                var explorerProcesses = Process.GetProcessesByName("explorer");
                Log($"Found {explorerProcesses.Length} Explorer process(es)");

                foreach (var process in explorerProcesses)
                {
                    Log($"Killing Explorer process {process.Id}");
                    process.Kill();
                    process.WaitForExit();
                }

                Log("Explorer stopped successfully");
            }
            catch (Exception ex)
            {
                Log($"Warning: Error killing Explorer: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts Windows Explorer
        /// </summary>
        static void StartExplorer(Action<string> Log)
        {
            try
            {
                Process.Start("explorer.exe");
                Log("Explorer started successfully");
            }
            catch (Exception ex)
            {
                Log($"Warning: Error starting Explorer: {ex.Message}");
            }
        }

        /// <summary>
        /// Approves the shell extension in the Windows registry
        /// This is required for Windows to load the shell extension
        /// </summary>
        static bool ApproveShellExtension(Action<string> Log)
        {
            try
            {
                const string CLSID = "{AADE67F3-4DA5-35F9-A229-571E789EE4C2}";
                const string APPROVAL_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved";

                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(APPROVAL_KEY, true))
                {
                    if (key == null)
                    {
                        Log($"ERROR: Could not open registry key: HKLM\\{APPROVAL_KEY}");
                        return false;
                    }

                    key.SetValue(CLSID, "RightClicks Context Menu Extension", Microsoft.Win32.RegistryValueKind.String);
                    Log($"Shell extension approved in registry: {CLSID}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log($"ERROR approving shell extension: {ex.Message}");
                return false;
            }
        }
    }
}

