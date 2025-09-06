using System.IO.Compression;

namespace UI.Tests;

// ADB-based app management methods
public class AppInstalation
{
    public static void PrepareAndInstallApp()
    {
        const string packageName = "com.darioalonso.binnacle";

        try
        {
            TestContext.Out.WriteLine("=== Starting App Preparation and Installation ===");

            // Step 1: Check for APK and build if necessary
            var apkPath = EnsureApkExists();
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

    private static string EnsureApkExists()
    {
        // Calculate workspace root from test directory
        var testDirectory = TestContext.CurrentContext.TestDirectory;

        // Navigate from test directory to workspace root
        // Need to go up 6 levels: bin -> Debug -> UI.Tests -> Tests -> src -> Binnaculum (workspace root)
        var workspaceRoot = Path.GetFullPath(Path.Combine(testDirectory, "..", "..", "..", "..", "..", ".."));

        // Use Release build for better performance and production-like testing
        var apkDirectory = Path.Combine(workspaceRoot, "src", "UI", "bin", "Release", "net9.0-android");

        TestContext.Out.WriteLine($"Test directory: {testDirectory}");
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

        // If no Release APK found, try Debug as fallback
        if (apkPath == null)
        {
            TestContext.Out.WriteLine("No Release APK found. Checking Debug build as fallback...");
            var debugApkDirectory = Path.Combine(workspaceRoot, "src", "UI", "bin", "Debug", "net9.0-android");

            if (Directory.Exists(debugApkDirectory))
            {
                var apkFiles = Directory.GetFiles(debugApkDirectory, "*.apk");
                if (apkFiles.Length > 0)
                {
                    apkPath = apkFiles[0];
                    TestContext.Out.WriteLine($"Found Debug APK as fallback: {apkPath}");
                }
            }
        }

        // If no APK found at all, build the project in Release mode
        if (apkPath == null)
        {
            TestContext.Out.WriteLine("No APK found. Building Binnaculum project in Release mode...");
            BuildBinnaculumProject(workspaceRoot);

            // Check again for APK after build using wildcard search (Release first, then Debug fallback)
            if (Directory.Exists(apkDirectory))
            {
                var apkFiles = Directory.GetFiles(apkDirectory, "*.apk");
                if (apkFiles.Length > 0)
                {
                    apkPath = apkFiles[0];
                    TestContext.Out.WriteLine($"APK created after Release build: {apkPath}");
                }
            }

            // Fallback to Debug directory after build
            if (apkPath == null)
            {
                var debugApkDirectory = Path.Combine(workspaceRoot, "src", "UI", "bin", "Debug", "net9.0-android");
                if (Directory.Exists(debugApkDirectory))
                {
                    var apkFiles = Directory.GetFiles(debugApkDirectory, "*.apk");
                    if (apkFiles.Length > 0)
                    {
                        apkPath = apkFiles[0];
                        TestContext.Out.WriteLine($"APK created after build (Debug fallback): {apkPath}");
                    }
                }
            }

            if (apkPath == null)
            {
                // List all files in both directories for debugging
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

                var debugApkDirectory = Path.Combine(workspaceRoot, "src", "UI", "bin", "Debug", "net9.0-android");
                TestContext.Out.WriteLine($"Debug directory contents of {debugApkDirectory}:");
                if (Directory.Exists(debugApkDirectory))
                {
                    var files = Directory.GetFiles(debugApkDirectory);
                    foreach (var file in files)
                    {
                        TestContext.Out.WriteLine($"  - {Path.GetFileName(file)}");
                    }
                }
                else
                {
                    TestContext.Out.WriteLine("  Debug directory does not exist");
                }

                throw new FileNotFoundException($"APK not found after build in Release or Debug directories");
            }
        }

        return apkPath;
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

        // Build the project targeting Android with signing enabled
        // This matches the Visual Studio build process that creates signed APKs
        var buildResult = ExecuteCommand("dotnet", 
            $"build \"{projectPath}\" -f net9.0-android -c Release " +
            "-p:AndroidPackageFormat=aab " +
            "-p:AndroidKeyStore=true " +
            "-p:AndroidSigningKeyStore=\"$([System.Environment]::GetFolderPath(SpecialFolder.LocalApplicationData))\\Xamarin\\Mono for Android\\debug.keystore\" " +
            "-p:AndroidSigningKeyAlias=androiddebugkey " +
            "-p:AndroidSigningKeyPass=android " +
            "-p:AndroidSigningStorePass=android", 
            workspaceRoot);

        if (buildResult.ExitCode != 0)
        {
            TestContext.Out.WriteLine("❌ Release build with signing failed. Attempting simple Release build as fallback...");

            // Fallback to simple Release build without explicit signing parameters
            var simpleBuildResult = ExecuteCommand("dotnet", $"build \"{projectPath}\" -f net9.0-android -c Release", workspaceRoot);

            if (simpleBuildResult.ExitCode != 0)
            {
                TestContext.Out.WriteLine("❌ Simple Release build failed. Attempting Debug build as final fallback...");

                // Final fallback to Debug build
                var debugBuildResult = ExecuteCommand("dotnet", $"build \"{projectPath}\" -f net9.0-android -c Debug", workspaceRoot);

                if (debugBuildResult.ExitCode != 0)
                {
                    throw new Exception($"All build attempts failed. " +
                        $"Signed Release error: {buildResult.Error}. " +
                        $"Simple Release error: {simpleBuildResult.Error}. " +
                        $"Debug error: {debugBuildResult.Error}");
                }
                else
                {
                    TestContext.Out.WriteLine("⚠️ Debug build succeeded as final fallback");
                    TestContext.Out.WriteLine($"Debug build output: {debugBuildResult.Output}");
                }
            }
            else
            {
                TestContext.Out.WriteLine("⚠️ Simple Release build succeeded as fallback");
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
                
                // Use bundletool to create universal APK (similar to what Visual Studio does)
                var bundletoolResult = ExecuteCommand("java", 
                    $"-jar \"{GetBundletoolPath()}\" build-apks " +
                    $"--mode universal " +
                    $"--bundle \"{bundlePath}\" " +
                    $"--output \"{outputApkPath.Replace(".apk", ".apks")}\" " +
                    $"--ks \"{GetDebugKeystorePath()}\" " +
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
        // Try to find bundletool in common locations
        var possiblePaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "packs", "Microsoft.Android.Sdk.Windows", "35.0.78", "tools", "bundletool.jar"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Android", "android-sdk", "cmdline-tools", "latest", "bin", "bundletool.jar")
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        throw new FileNotFoundException("bundletool.jar not found in expected locations");
    }

    private static string GetDebugKeystorePath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Xamarin", "Mono for Android", "debug.keystore");
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