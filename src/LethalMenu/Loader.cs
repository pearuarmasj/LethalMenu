using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LethalMenu
{
    /// <summary>
    /// Entry point for the mod. Called by the mono injector.
    /// </summary>
    public static class Loader
    {
        private static GameObject? _modObject;
        private static bool _isLoaded;
        private static string _logPath = string.Empty;

        // Windows console APIs
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetStdHandle(int nStdHandle, IntPtr hHandle);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CreateFile(
            string fileName,
            uint desiredAccess,
            uint shareMode,
            IntPtr securityAttributes,
            uint creationDisposition,
            uint flagsAndAttributes,
            IntPtr templateFile);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        private const int STD_OUTPUT_HANDLE = -11;
        private const int STD_ERROR_HANDLE = -12;
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x1;
        private const uint FILE_SHARE_WRITE = 0x2;
        private const uint OPEN_EXISTING = 3;

        /// <summary>
        /// Main entry point - called by SharpMonoInjector.
        /// </summary>
        public static void Load()
        {
            if (_isLoaded)
            {
                Log("[LethalMenu] Already loaded.");
                return;
            }

            try
            {
                // Set up log file first
                _logPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "C:\\", "LethalMenu.log");
                
                // Try alternate location if that fails
                if (string.IsNullOrEmpty(_logPath) || !Directory.Exists(Path.GetDirectoryName(_logPath)))
                {
                    _logPath = "C:\\LethalMenu.log";
                }

                File.WriteAllText(_logPath, $"[{DateTime.Now}] LethalMenu Loading...\n");

                // Try to allocate console
                AllocateConsole();

                Log("===========================================");
                Log("  LethalMenu - Loading...");
                Log($"  Log file: {_logPath}");
                Log("===========================================");

                LoadEmbeddedAssemblies();
                InitializeMod();
                _isLoaded = true;

                Log("[LethalMenu] Successfully loaded.");
                Log("[LethalMenu] Press INSERT in-game to toggle menu.");
                Log("===========================================");
            }
            catch (Exception ex)
            {
                Log($"[LethalMenu] FATAL ERROR: {ex}");
            }
        }

        private static void AllocateConsole()
        {
            try
            {
                // Allocate a new console
                bool allocated = AllocConsole();
                
                if (allocated)
                {
                    // Get handle to CONOUT$
                    var handle = CreateFile("CONOUT$", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, 
                        IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                    
                    if (handle != IntPtr.Zero && handle != new IntPtr(-1))
                    {
                        SetStdHandle(STD_OUTPUT_HANDLE, handle);
                        SetStdHandle(STD_ERROR_HANDLE, handle);

                        // Initialize Console.Out with UTF-8 encoding
                        Console.OutputEncoding = System.Text.Encoding.UTF8;
                        var standardOutput = new StreamWriter(Console.OpenStandardOutput(), System.Text.Encoding.UTF8);
                        standardOutput.AutoFlush = true;
                        Console.SetOut(standardOutput);
                        Console.SetError(standardOutput);
                    }

                    // Set console title and colors
                    Console.Title = "LethalMenu Debug Console";
                    Console.BackgroundColor = ConsoleColor.Black;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Clear();

                    // Print banner (ASCII art, no Unicode)
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine();
                    Console.WriteLine("  +=============================================+");
                    Console.WriteLine("  |        LETHAL MENU - DEBUG CONSOLE         |");
                    Console.WriteLine("  +=============================================+");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine();

                    // Bring console to front
                    var consoleWindow = GetConsoleWindow();
                    if (consoleWindow != IntPtr.Zero)
                    {
                        SetForegroundWindow(consoleWindow);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log to file if console fails
                try
                {
                    File.AppendAllText(_logPath, $"[{DateTime.Now}] Console allocation failed: {ex}\n");
                }
                catch { }
            }
        }

        /// <summary>
        /// Log to Console, Unity, and file.
        /// </summary>
        public static void Log(string message)
        {
            string timestamped = $"[{DateTime.Now:HH:mm:ss}] {message}";

            // File logging (most reliable)
            try
            {
                if (!string.IsNullOrEmpty(_logPath))
                {
                    File.AppendAllText(_logPath, timestamped + "\n");
                }
            }
            catch { }

            // Console logging with colors
            try
            {
                // Color based on message type
                if (message.Contains("[ERROR]") || message.Contains("Error") || message.Contains("FATAL"))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                else if (message.Contains("SUCCESS") || message.Contains("Successfully") || message.Contains("Sold"))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                else if (message.Contains("[LethalMenu]"))
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                }
                else if (message.Contains("$"))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.White;
                }
                
                Console.WriteLine(timestamped);
                Console.ForegroundColor = ConsoleColor.White; // Reset
            }
            catch { }

            // Unity logging
            try
            {
                Debug.Log(message);
            }
            catch { }
        }

        /// <summary>
        /// Log error to Console, Unity, and file.
        /// </summary>
        public static void LogError(string message)
        {
            string timestamped = $"[{DateTime.Now:HH:mm:ss}] [ERROR] {message}";

            try
            {
                if (!string.IsNullOrEmpty(_logPath))
                {
                    File.AppendAllText(_logPath, timestamped + "\n");
                }
            }
            catch { }

            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(timestamped);
                Console.ForegroundColor = ConsoleColor.White;
            }
            catch { }

            try
            {
                Debug.LogError(message);
            }
            catch { }
        }

        /// <summary>
        /// Unload the mod and clean up.
        /// </summary>
        public static void Unload()
        {
            if (!_isLoaded) return;

            try
            {
                if (_modObject != null)
                {
                    var mod = _modObject.GetComponent<LethalMenuMod>();
                    mod?.Cleanup();
                    UnityEngine.Object.Destroy(_modObject);
                    _modObject = null;
                }

                _isLoaded = false;
                Log("[LethalMenu] Successfully unloaded.");
                FreeConsole();
            }
            catch (Exception ex)
            {
                LogError($"[LethalMenu] Failed to unload: {ex}");
            }
        }

        /// <summary>
        /// Load embedded DLLs (Harmony, etc.) into the AppDomain.
        /// </summary>
        private static void LoadEmbeddedAssemblies()
        {
            Log("[LethalMenu] Loading embedded assemblies...");
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames()
                .Where(name => name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));

            foreach (var resourceName in resourceNames)
            {
                try
                {
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream == null) continue;

                    using var memoryStream = new MemoryStream();
                    stream.CopyTo(memoryStream);
                    AppDomain.CurrentDomain.Load(memoryStream.ToArray());
                    Log($"[LethalMenu] Loaded embedded assembly: {resourceName}");
                }
                catch (Exception ex)
                {
                    LogError($"[LethalMenu] Failed to load {resourceName}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Create the mod GameObject and initialize components.
        /// </summary>
        private static void InitializeMod()
        {
            Log("[LethalMenu] Creating mod GameObject...");
            _modObject = new GameObject("LethalMenu");
            UnityEngine.Object.DontDestroyOnLoad(_modObject);
            Log("[LethalMenu] Adding LethalMenuMod component...");
            _modObject.AddComponent<LethalMenuMod>();
            Log("[LethalMenu] Mod initialized.");
        }
    }
}
