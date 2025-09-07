using System.Collections.Concurrent;

namespace UI.Tests;

// ADB-based app management methods
public class AppInstalation
{
    private static readonly object _lockObject = new object();
    private static readonly ConcurrentDictionary<string, string> _builtApkCache = new();
    private static volatile bool _isBuilding = false;
    private static readonly ManualResetEventSlim _buildCompleteEvent = new(false);
    
    public static void PrepareAndInstallApp()
    {
        const string packageName = "com.darioalonso.binnacle";

        try
        {
            TestContext.Out.WriteLine("=== Starting App Preparation and Installation ===");

            // Step 1: Check for APK and build if necessary (thread-safe)
            var apkPath = EnsureApkExistsThreadSafe();
            TestContext.Out.WriteLine($"APK verified at: {apkPath}");

            // Step 2: Check if app is installed on emulator and uninstall if found
            if (IsAppInstalledOnDevice(packageName))
            {
                TestContext.Out.WriteLine($"App {packageName} found on device. Uninstalling...");
                UninstallAppFromDevice(packageName);
                TestContext.Out.WriteLine("App uninstalled successfully");
            }
            else
            {
                TestContext.Out.WriteLine($"App {packageName} not found on device. Proceeding with fresh installation.");
            }

            // Step 3: Install the app
            TestContext.Out.WriteLine("Installing app on device...");
            InstallAppOnDevice(apkPath);
            TestContext.Out.WriteLine("App installed successfully");

            // Step 4: Verify installation
            if (IsAppInstalledOnDevice(packageName))
            {
                TestContext.Out.WriteLine("✅ App installation verified successfully");
            }
            else
            {
                throw new Exception("❌ App installation verification failed");
            }

        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"❌ App preparation failed: {ex.Message}");
            throw;
        }
    }

    public static string EnsureApkExistsThreadSafe()
    {
        var testDirectory = TestContext.CurrentContext.TestDirectory;
        var workspaceRoot = FindWorkspaceRoot(testDirectory);
        var cacheKey = $"{workspaceRoot}:Release"; // Cache key based on workspace and configuration

        // Check if we already have a built APK for this configuration
        if (_builtApkCache.TryGetValue(cacheKey, out var cachedApkPath) && File.Exists(cachedApkPath))
        {
            TestContext.Out.WriteLine($"✅ Using cached APK: {cachedApkPath}");
            return cachedApkPath;
        }

        // Use double-checked locking pattern for thread safety
        lock (_lockObject)
        {
            // Check again inside the lock in case another thread built it while we were waiting
            if (_builtApkCache.TryGetValue(cacheKey, out cachedApkPath) && File.Exists(cachedApkPath))
            {
                TestContext.Out.WriteLine($"✅ Using APK built by another thread: {cachedApkPath}");
                return cachedApkPath;
            }

            // If another thread is currently building, wait for it to complete
            if (_isBuilding)
            {
                TestContext.Out.WriteLine("⏳ Another thread is building the APK. Waiting for completion...");
                _buildCompleteEvent.Wait();
                
                // After waiting, check the cache again
                if (_builtApkCache.TryGetValue(cacheKey, out cachedApkPath) && File.Exists(cachedApkPath))
                {
                    TestContext.Out.WriteLine($"✅ Using APK built by another thread after waiting: {cachedApkPath}");
                    return cachedApkPath;
                }
            }

            // Mark that we're building and reset the event
            _isBuilding = true;
            _buildCompleteEvent.Reset();

            try
            {
                TestContext.Out.WriteLine("🔨 This thread will build the APK...");
                var apkPath = EnsureApkExists(workspaceRoot);
                
                // Cache the result for other threads
                _builtApkCache.TryAdd(cacheKey, apkPath);
                TestContext.Out.WriteLine($"✅ APK built and cached: {apkPath}");
                
                return apkPath;
            }
            finally
            {
                // Mark building as complete and notify waiting threads
                _isBuilding = false;
                _buildCompleteEvent.Set();
            }
        }
    }

    private static string EnsureApkExists(string workspaceRoot)
    {
        // Use Release build for better performance and production-like testing
        var apkDirectory = Path.Combine(workspaceRoot, "src", "UI", "bin", "Release", "net9.0-android");

        TestContext.Out.WriteLine($"Workspace root: {workspaceRoot}");
        TestContext.Out.WriteLine($"APK directory (Release): {apkDirectory}");

        // Look for common APK naming patterns (including wildcards for generated names)
        string? apkPath = null;

        if (Directory.Exists(apkDirectory))
        {
            // First try exact naming patterns
            var possibleApkNames = new[]
            {
                "com.darioalonso.binnacle-Signed.apk",
                "com.darioalonso.binnacle.apk",
                "Binnaculum-Signed.apk",
                "Binnaculum.apk"
            };

            foreach (var apkName in possibleApkNames)
            {
                var candidatePath = Path.Combine(apkDirectory, apkName);
                if (File.Exists(candidatePath))
                {
                    apkPath = candidatePath;
                    TestContext.Out.WriteLine($"Found existing APK (Release): {apkPath}");
                    break;
                }
                else
                {
                    TestContext.Out.WriteLine($"APK not found: {candidatePath}");
                }
            }

            // If no exact match found, look for any APK files
            if (apkPath == null)
            {
                var apkFiles = Directory.GetFiles(apkDirectory, "*.apk");
                if (apkFiles.Length > 0)
                {
                    apkPath = apkFiles[0]; // Use first APK found
                    TestContext.Out.WriteLine($"Found APK by wildcard search (Release): {apkPath}");
                }
            }
        }
        else
        {
            TestContext.Out.WriteLine($"Release APK directory does not exist: {apkDirectory}");
        }

        // If no Release APK found, build the project in Release mode
        if (apkPath == null)
        {
            TestContext.Out.WriteLine("No APK found. Building Binnaculum project in Release mode...");
            BuildBinnaculumProjectThreadSafe(workspaceRoot);

            // Check again for APK after build using wildcard search (Release only)
            if (Directory.Exists(apkDirectory))
            {
                var apkFiles = Directory.GetFiles(apkDirectory, "*.apk");
                if (apkFiles.Length > 0)
                {
                    apkPath = apkFiles[0];
                    TestContext.Out.WriteLine($"APK created after Release build: {apkPath}");
                }
            }

            if (apkPath == null)
            {
                // List all files in Release directory for debugging
                TestContext.Out.WriteLine($"Release directory contents of {apkDirectory}:");
                if (Directory.Exists(apkDirectory))
                {
                    var files = Directory.GetFiles(apkDirectory);
                    foreach (var file in files)
                    {
                        TestContext.Out.WriteLine($"  - {Path.GetFileName(file)}");
                    }
                }
                else
                {
                    TestContext.Out.WriteLine("  Release directory does not exist");
                }

                throw new FileNotFoundException($"Release APK not found after build in Release directory: {apkDirectory}");
            }
        }

        return apkPath;
    }

    public static string FindWorkspaceRoot(string startDirectory)
    {
        var currentDir = new DirectoryInfo(startDirectory);
        
        // Look for workspace indicators (solution files, specific directory structure)
        while (currentDir != null)
        {
            // Check for solution file
            if (currentDir.GetFiles("*.sln").Length > 0)
            {
                TestContext.Out.WriteLine($"Found workspace root by .sln file: {currentDir.FullName}");
                return currentDir.FullName;
            }
            
            // Check for src directory with expected structure
            var srcDir = Path.Combine(currentDir.FullName, "src");
            if (Directory.Exists(srcDir))
            {
                var uiDir = Path.Combine(srcDir, "UI");
                var coreDir = Path.Combine(srcDir, "Core");
                
                if (Directory.Exists(uiDir) && Directory.Exists(coreDir))
                {
                    TestContext.Out.WriteLine($"Found workspace root by src structure: {currentDir.FullName}");
                    return currentDir.FullName;
                }
            }
            
            // Check for git repository
            if (Directory.Exists(Path.Combine(currentDir.FullName, ".git")))
            {
                TestContext.Out.WriteLine($"Found workspace root by .git directory: {currentDir.FullName}");
                return currentDir.FullName;
            }
            
            currentDir = currentDir.Parent;
        }
        
        // Fallback to original method if no indicators found
        TestContext.Out.WriteLine("Could not find workspace root by indicators, using fallback method");
        return Path.GetFullPath(Path.Combine(startDirectory, "..", "..", "..", "..", "..", ".."));
    }

    private static void BuildBinnaculumProjectThreadSafe(string workspaceRoot)
    {
        // Additional safety check - ensure we're not building concurrently
        lock (_lockObject)
        {
            TestContext.Out.WriteLine("🔨 Starting thread-safe build process...");
            BuildBinnaculumProject(workspaceRoot);
            TestContext.Out.WriteLine("✅ Thread-safe build process completed");
        }
    }

    private static void BuildBinnaculumProject(string workspaceRoot)
    {
        // Correct path based on workspace structure
        var projectPath = Path.Combine(workspaceRoot, "src", "UI", "Binnaculum.csproj");

        TestContext.Out.WriteLine($"Looking for project at: {projectPath}");

        if (!File.Exists(projectPath))
        {
            // List contents of src directory for debugging
            var srcDirectory = Path.Combine(workspaceRoot, "src");
            TestContext.Out.WriteLine($"Contents of {srcDirectory}:");
            if (Directory.Exists(srcDirectory))
            {
                var directories = Directory.GetDirectories(srcDirectory);
                foreach (var dir in directories)
                {
                    TestContext.Out.WriteLine($"  - {Path.GetFileName(dir)}");
                }
            }

            throw new FileNotFoundException($"Binnaculum project not found at: {projectPath}");
        }

        TestContext.Out.WriteLine($"Building and signing project in Release mode: {projectPath}");

        // Get the debug keystore path first
        string? debugKeystorePath = null;
        try
        {
            debugKeystorePath = GetDebugKeystorePath();
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"⚠️ Could not find debug keystore: {ex.Message}");
            TestContext.Out.WriteLine("Proceeding with simple build without explicit signing...");
            debugKeystorePath = null;
        }

        // Build the project targeting Android with signing enabled (if keystore available)
        (int ExitCode, string Output, string Error) buildResult;
        
        if (!string.IsNullOrEmpty(debugKeystorePath))
        {
            // Build with explicit signing parameters
            buildResult = ExecuteCommand("dotnet", 
                $"build \"{projectPath}\" -f net9.0-android -c Release " +
                "-p:AndroidPackageFormat=aab " +
                "-p:AndroidKeyStore=true " +
                $"-p:AndroidSigningKeyStore=\"{debugKeystorePath}\" " +
                "-p:AndroidSigningKeyAlias=androiddebugkey " +
                "-p:AndroidSigningKeyPass=android " +
                "-p:AndroidSigningStorePass=android", 
                workspaceRoot);
        }
        else
        {
            // Build without explicit signing (let the build system handle it)
            buildResult = ExecuteCommand("dotnet", 
                $"build \"{projectPath}\" -f net9.0-android -c Release -p:AndroidPackageFormat=aab", 
                workspaceRoot);
        }

        if (buildResult.ExitCode != 0)
        {
            TestContext.Out.WriteLine("❌ Release build with signing failed. Attempting simple Release build as fallback...");

            // Fallback to simple Release build without explicit signing parameters
            var simpleBuildResult = ExecuteCommand("dotnet", $"build \"{projectPath}\" -f net9.0-android -c Release", workspaceRoot);

            if (simpleBuildResult.ExitCode != 0)
            {
                // No Debug fallback - fail immediately
                throw new Exception($"Release build failed. " +
                    $"Signed Release error: {buildResult.Error}. " +
                    $"Simple Release error: {simpleBuildResult.Error}. " +
                    $"Debug builds are not allowed for testing.");
            }
            else
            {
                TestContext.Out.WriteLine("✅ Simple Release build succeeded as fallback");
                TestContext.Out.WriteLine($"Simple Release build output: {simpleBuildResult.Output}");
            }
        }
        else
        {
            TestContext.Out.WriteLine("✅ Signed Release build completed successfully");
            TestContext.Out.WriteLine($"Signed Release build output: {buildResult.Output}");
        }

        // After successful build, create Universal APK if we have the bundle
        CreateUniversalApkFromBundle(workspaceRoot);
    }

    private static void CreateUniversalApkFromBundle(string workspaceRoot)
    {
        try
        {
            var bundlePath = Path.Combine(workspaceRoot, "src", "UI", "bin", "Release", "net9.0-android", "com.darioalonso.binnacle-Signed.aab");
            var outputApkPath = Path.Combine(workspaceRoot, "src", "UI", "bin", "Release", "net9.0-android", "com.darioalonso.binnacle-Signed.apk");
            
            if (File.Exists(bundlePath))
            {
                TestContext.Out.WriteLine($"Creating Universal APK from bundle: {bundlePath}");
                
                // Get the debug keystore path (project-specific or fallback)
                string keystorePath;
                try
                {
                    keystorePath = GetDebugKeystorePath();
                }
                catch (Exception ex)
                {
                    TestContext.Out.WriteLine($"⚠️ Could not find keystore for Universal APK creation: {ex.Message}");
                    TestContext.Out.WriteLine("Skipping Universal APK creation...");
                    return;
                }
                
                // Use bundletool to create universal APK (similar to what Visual Studio does)
                var bundletoolResult = ExecuteCommand("java", 
                    $"-jar \"{GetBundletoolPath()}\" build-apks " +
                    $"--mode universal " +
                    $"--bundle \"{bundlePath}\" " +
                    $"--output \"{outputApkPath.Replace(".apk", ".apks")}\" " +
                    $"--ks \"{keystorePath}\" " +
                    $"--ks-key-alias androiddebugkey " +
                    $"--key-pass pass:android " +
                    $"--ks-pass pass:android", 
                    workspaceRoot);

                if (bundletoolResult.ExitCode == 0)
                {
                    TestContext.Out.WriteLine("✅ Universal APK created successfully");
                    
                    // Extract the APK from the .apks file
                    ExtractApkFromApks(outputApkPath.Replace(".apk", ".apks"), outputApkPath);
                }
                else
                {
                    TestContext.Out.WriteLine($"⚠️ Failed to create Universal APK: {bundletoolResult.Error}");
                    TestContext.Out.WriteLine("Continuing with existing build outputs...");
                }
            }
            else
            {
                TestContext.Out.WriteLine($"⚠️ Bundle not found at {bundlePath}, skipping Universal APK creation");
            }
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"⚠️ Error creating Universal APK: {ex.Message}");
            TestContext.Out.WriteLine("Continuing with existing build outputs...");
        }
    }

    private static string GetBundletoolPath()
    {
        // Try to find bundletool in common locations with dynamic version detection
        var possibleBasePaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "packs"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "dotnet", "packs"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Android", "android-sdk", "cmdline-tools", "latest", "bin"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Android", "android-sdk", "cmdline-tools", "tools", "bin")
        };

        // Search for Microsoft.Android.Sdk.Windows with any version
        foreach (var basePath in possibleBasePaths)
        {
            if (Directory.Exists(basePath))
            {
                // Look for Microsoft.Android.Sdk.Windows directories
                var androidSdkDirs = Directory.GetDirectories(basePath, "Microsoft.Android.Sdk*", SearchOption.TopDirectoryOnly);
                
                foreach (var sdkDir in androidSdkDirs.OrderByDescending(d => d)) // Try newest versions first
                {
                    var bundletoolPath = Path.Combine(sdkDir, "tools", "bundletool.jar");
                    if (File.Exists(bundletoolPath))
                    {
                        TestContext.Out.WriteLine($"Found bundletool at: {bundletoolPath}");
                        return bundletoolPath;
                    }
                }
                
                // Also check for direct bundletool.jar in cmdline-tools
                var directBundletoolPath = Path.Combine(basePath, "bundletool.jar");
                if (File.Exists(directBundletoolPath))
                {
                    TestContext.Out.WriteLine($"Found bundletool at: {directBundletoolPath}");
                    return directBundletoolPath;
                }
            }
        }

        // Try to use dotnet to find the path
        try
        {
            var dotnetResult = ExecuteCommand("dotnet", "--list-sdks");
            if (dotnetResult.ExitCode == 0)
            {
                TestContext.Out.WriteLine("Available .NET SDKs:");
                TestContext.Out.WriteLine(dotnetResult.Output);
            }
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"Could not query dotnet SDKs: {ex.Message}");
        }

        // List what we actually found for debugging
        TestContext.Out.WriteLine("Searched for bundletool in the following locations:");
        foreach (var basePath in possibleBasePaths)
        {
            TestContext.Out.WriteLine($"  - {basePath} (exists: {Directory.Exists(basePath)})");
            if (Directory.Exists(basePath))
            {
                try
                {
                    var subdirs = Directory.GetDirectories(basePath);
                    foreach (var subdir in subdirs.Take(5)) // Limit output
                    {
                        TestContext.Out.WriteLine($"    - {Path.GetFileName(subdir)}");
                    }
                }
                catch (Exception ex)
                {
                    TestContext.Out.WriteLine($"    Error listing subdirectories: {ex.Message}");
                }
            }
        }

        throw new FileNotFoundException("bundletool.jar not found in expected locations. Ensure Android SDK or MAUI workload is installed.");
    }

    private static string GetDebugKeystorePath()
    {
        // Try to find workspace root first to locate project-specific keystore
        var testDirectory = TestContext.CurrentContext.TestDirectory;
        var workspaceRoot = FindWorkspaceRoot(testDirectory);
        
        // Try project-specific keystore first (highest priority)
        var projectKeystorePath = Path.Combine(workspaceRoot, "debug.keystore");
        if (File.Exists(projectKeystorePath))
        {
            TestContext.Out.WriteLine($"Found project-specific debug keystore at: {projectKeystorePath}");
            return projectKeystorePath;
        }
        else
        {
            TestContext.Out.WriteLine($"Project-specific keystore not found at: {projectKeystorePath}");
        }

        // Fallback to system keystore locations
        var possiblePaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Xamarin", "Mono for Android", "debug.keystore"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".android", "debug.keystore"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Xamarin", "Mono for Android", "debug.keystore")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                TestContext.Out.WriteLine($"Found system debug keystore at: {path}");
                return path;
            }
            else
            {
                TestContext.Out.WriteLine($"System keystore not found at: {path}");
            }
        }

        // Create a temporary debug keystore if none found
        var tempKeystorePath = CreateTemporaryDebugKeystore();
        if (tempKeystorePath != null)
        {
            return tempKeystorePath;
        }

        throw new FileNotFoundException("Debug keystore not found and could not create temporary keystore. Ensure the project debug.keystore exists at solution root or Android development tools are installed.");
    }

    private static string? CreateTemporaryDebugKeystore()
    {
        try
        {
            var tempKeystorePath = Path.Combine(Path.GetTempPath(), "debug.keystore");
            
            if (File.Exists(tempKeystorePath))
            {
                TestContext.Out.WriteLine($"Using existing temporary keystore: {tempKeystorePath}");
                return tempKeystorePath;
            }

            TestContext.Out.WriteLine("Creating temporary debug keystore...");
            
            // Use keytool to create a debug keystore
            var keytoolResult = ExecuteCommand("keytool", 
                $"-genkey -v -keystore \"{tempKeystorePath}\" " +
                "-alias androiddebugkey -keyalg RSA -keysize 2048 -validity 10000 " +
                "-storepass android -keypass android " +
                "-dname \"CN=Android Debug,O=Android,C=US\"");

            if (keytoolResult.ExitCode == 0 && File.Exists(tempKeystorePath))
            {
                TestContext.Out.WriteLine($"✅ Created temporary debug keystore: {tempKeystorePath}");
                return tempKeystorePath;
            }
            else
            {
                TestContext.Out.WriteLine($"❌ Failed to create temporary keystore: {keytoolResult.Error}");
                return null;
            }
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"❌ Error creating temporary keystore: {ex.Message}");
            return null;
        }
    }

    private static void ExtractApkFromApks(string apksPath, string outputApkPath)
    {
        try
        {
            // The .apks file is a ZIP archive containing the universal APK
            // We can extract it using standard ZIP operations
            using var archive = System.IO.Compression.ZipFile.OpenRead(apksPath);
            var universalApkEntry = archive.GetEntry("universal.apk");
            
            if (universalApkEntry != null)
            {
                using var apkStream = universalApkEntry.Open();
                using var outputStream = File.Create(outputApkPath);
                apkStream.CopyTo(outputStream);
                
                TestContext.Out.WriteLine($"✅ Extracted Universal APK to: {outputApkPath}");
            }
            else
            {
                TestContext.Out.WriteLine("⚠️ universal.apk not found in .apks archive");
            }
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"⚠️ Error extracting APK from .apks: {ex.Message}");
        }
    }

    private static bool IsAppInstalledOnDevice(string packageName)
    {
        var result = ExecuteCommand("adb", $"shell pm list packages {packageName}");
        var isInstalled = !string.IsNullOrEmpty(result.Output) && result.Output.Contains(packageName);

        TestContext.Out.WriteLine($"Checking if {packageName} is installed: {(isInstalled ? "YES" : "NO")}");

        return isInstalled;
    }

    private static void UninstallAppFromDevice(string packageName)
    {
        var result = ExecuteCommand("adb", $"uninstall {packageName}");

        if (result.ExitCode != 0 && !result.Output.Contains("DELETE_FAILED_INTERNAL_ERROR"))
        {
            TestContext.Out.WriteLine($"⚠️ Uninstall warning - Exit code: {result.ExitCode}, Output: {result.Output}, Error: {result.Error}");
        }
        else
        {
            TestContext.Out.WriteLine("App uninstalled successfully");
        }
    }

    private static void InstallAppOnDevice(string apkPath)
    {
        var result = ExecuteCommand("adb", $"install -r \"{apkPath}\"");

        if (result.ExitCode != 0)
        {
            throw new Exception($"APK installation failed with exit code {result.ExitCode}. Output: {result.Output}. Error: {result.Error}");
        }

        if (result.Output.Contains("INSTALL_FAILED") || result.Error.Contains("INSTALL_FAILED"))
        {
            throw new Exception($"APK installation failed: {result.Output} {result.Error}");
        }

        TestContext.Out.WriteLine($"Installation result: {result.Output}");
    }

    private static (int ExitCode, string Output, string Error) ExecuteCommand(string command, string arguments, string? workingDirectory = null)
    {
        TestContext.Out.WriteLine($"Executing: {command} {arguments}");

        using var process = new Process();
        process.StartInfo.FileName = command;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;

        if (!string.IsNullOrEmpty(workingDirectory))
        {
            process.StartInfo.WorkingDirectory = workingDirectory;
        }

        var output = new System.Text.StringBuilder();
        var error = new System.Text.StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
                TestContext.Out.WriteLine($"[OUT] {e.Data}");
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                error.AppendLine(e.Data);
                TestContext.Out.WriteLine($"[ERR] {e.Data}");
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Set timeout for long-running operations like builds
        var timeout = command == "dotnet" ? TimeSpan.FromMinutes(5) : TimeSpan.FromMinutes(2);

        if (!process.WaitForExit((int)timeout.TotalMilliseconds))
        {
            process.Kill();
            throw new TimeoutException($"Command '{command} {arguments}' timed out after {timeout.TotalMinutes} minutes");
        }

        return (process.ExitCode, output.ToString(), error.ToString());
    }
}