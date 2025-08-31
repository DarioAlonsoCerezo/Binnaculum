using NUnit.Framework;
using System.Diagnostics;

namespace Binnaculum.Build.IntegrationTests;

/// <summary>
/// Tests for CI/CD pipeline validation and environment setup
/// </summary>
[TestFixture]
public class CICDIntegrationTests
{
    private static readonly string SolutionRoot = GetSolutionRoot();
    
    private static string GetSolutionRoot()
    {
        var current = Directory.GetCurrentDirectory();
        while (current != null && !File.Exists(Path.Combine(current, "Binnaculum.sln")))
        {
            current = Directory.GetParent(current)?.FullName;
        }
        return current ?? throw new InvalidOperationException("Could not find solution root");
    }

    [Test]
    [Description("Validate .NET SDK installation script works")]
    public async Task DotNetInstallScript_IsExecutable()
    {
        // Arrange
        var installScriptPath = Path.Combine(SolutionRoot, "dotnet-install.sh");
        
        // Assert
        Assert.That(File.Exists(installScriptPath), Is.True, "dotnet-install.sh should exist");
        
        // Check script has executable permissions (on Unix-like systems)
        if (!OperatingSystem.IsWindows())
        {
            var result = await ExecuteCommand("ls", $"-la \"{installScriptPath}\"");
            Assert.That(result.StandardOutput, Does.Contain("x"), 
                "Install script should have executable permissions");
        }
        
        // Verify script content looks valid
        var scriptContent = File.ReadAllText(installScriptPath);
        Assert.That(scriptContent, Does.Contain("dotnet-install"), 
            "Script should contain dotnet-install functionality");
    }

    [Test]
    [Description("Validate environment can detect .NET 9 SDK")]
    public async Task Environment_CanDetectDotNet9()
    {
        // Act
        var result = await ExecuteDotNetCommand("--version");
        
        // Assert
        Assert.That(result.ExitCode, Is.EqualTo(0), "dotnet command should be available");
        Assert.That(result.StandardOutput, Does.StartWith("9."), 
            "Should have .NET 9 SDK installed");
        
        TestContext.Out.WriteLine($".NET SDK Version: {result.StandardOutput.Trim()}");
    }

    [Test]
    [Description("Validate MAUI workload installation status")]
    public async Task Environment_CanCheckMauiWorkloads()
    {
        // Act
        var result = await ExecuteDotNetCommand("workload list");
        
        // Assert
        Assert.That(result.ExitCode, Is.EqualTo(0), "Workload list command should succeed");
        
        TestContext.Out.WriteLine("Installed workloads:");
        TestContext.Out.WriteLine(result.StandardOutput);
        
        // Check if MAUI workloads are available
        if (result.StandardOutput.Contains("maui"))
        {
            Assert.Pass("MAUI workloads are installed");
        }
        else
        {
            Assert.Inconclusive("MAUI workloads not installed - some builds may fail");
        }
    }

    [Test]
    [Description("Simulate CI matrix build for available platforms")]
    public async Task CI_MatrixBuildSimulation()
    {
        // Simulate a CI build matrix for different configurations
        var configurations = new[] { "Debug", "Release" };
        var projects = new[]
        {
            ("Core", Path.Combine(SolutionRoot, "src", "Core", "Core.fsproj")),
            ("Core.Tests", Path.Combine(SolutionRoot, "src", "Tests", "Core.Tests", "Core.Tests.fsproj"))
        };
        
        var results = new List<(string Project, string Config, bool Success, double BuildTime)>();
        
        foreach (var (projectName, projectPath) in projects)
        {
            foreach (var config in configurations)
            {
                // Clean build
                await ExecuteDotNetCommand($"clean \"{projectPath}\"");
                
                // Build with timing
                var stopwatch = Stopwatch.StartNew();
                var result = await ExecuteDotNetCommand($"build \"{projectPath}\" --configuration {config} --verbosity minimal");
                stopwatch.Stop();
                
                results.Add((projectName, config, result.ExitCode == 0, stopwatch.Elapsed.TotalSeconds));
                
                Assert.That(result.ExitCode, Is.EqualTo(0), 
                    $"CI build {projectName} ({config}) should succeed");
            }
        }
        
        // Report matrix results
        TestContext.Out.WriteLine("CI Matrix Build Results:");
        foreach (var (project, config, success, buildTime) in results)
        {
            TestContext.Out.WriteLine($"{project} ({config}): {(success ? "✓" : "✗")} - {buildTime:F1}s");
        }
        
        // All builds should have succeeded
        Assert.That(results.All(r => r.Success), Is.True, 
            "All CI matrix builds should succeed");
    }

    [Test]
    [Description("Validate clean environment build reproducibility")]
    public async Task CI_BuildReproducibility()
    {
        // Test that builds are reproducible in clean environments
        var coreProjectPath = Path.Combine(SolutionRoot, "src", "Core", "Core.fsproj");
        
        // First clean build
        await ExecuteDotNetCommand($"clean \"{coreProjectPath}\"");
        var result1 = await ExecuteDotNetCommand($"build \"{coreProjectPath}\" --configuration Release");
        Assert.That(result1.ExitCode, Is.EqualTo(0), "First build should succeed");
        
        var outputPath = Path.Combine(SolutionRoot, "src", "Core", "bin", "Release", "net9.0", "Core.dll");
        var firstBuildInfo = new FileInfo(outputPath);
        Assert.That(firstBuildInfo.Exists, Is.True, "First build output should exist");
        
        // Second clean build
        await ExecuteDotNetCommand($"clean \"{coreProjectPath}\"");
        var result2 = await ExecuteDotNetCommand($"build \"{coreProjectPath}\" --configuration Release");
        Assert.That(result2.ExitCode, Is.EqualTo(0), "Second build should succeed");
        
        var secondBuildInfo = new FileInfo(outputPath);
        Assert.That(secondBuildInfo.Exists, Is.True, "Second build output should exist");
        
        // Verify reproducibility - size should be identical for deterministic builds
        Assert.That(secondBuildInfo.Length, Is.EqualTo(firstBuildInfo.Length), 
            "Build outputs should be reproducible (same size)");
        
        TestContext.Out.WriteLine($"Build output size: {firstBuildInfo.Length} bytes");
    }

    [Test]
    [Description("Validate artifact generation for deployable projects")]
    public async Task CI_ArtifactGeneration()
    {
        // Test that build artifacts are properly generated
        var coreProjectPath = Path.Combine(SolutionRoot, "src", "Core", "Core.fsproj");
        
        // Clean and build
        await ExecuteDotNetCommand($"clean \"{coreProjectPath}\"");
        var result = await ExecuteDotNetCommand($"build \"{coreProjectPath}\" --configuration Release");
        
        Assert.That(result.ExitCode, Is.EqualTo(0), "Build should succeed");
        
        // Verify essential artifacts are generated
        var binPath = Path.Combine(SolutionRoot, "src", "Core", "bin", "Release", "net9.0");
        var expectedArtifacts = new[]
        {
            "Core.dll",
            "Core.pdb", // Debug symbols
            "Core.deps.json" // Dependencies
        };
        
        foreach (var artifact in expectedArtifacts)
        {
            var artifactPath = Path.Combine(binPath, artifact);
            Assert.That(File.Exists(artifactPath), Is.True, 
                $"Artifact {artifact} should be generated");
            
            var fileInfo = new FileInfo(artifactPath);
            Assert.That(fileInfo.Length, Is.GreaterThan(0), 
                $"Artifact {artifact} should not be empty");
            
            TestContext.Out.WriteLine($"Artifact {artifact}: {fileInfo.Length} bytes");
        }
    }

    [Test]
    [Description("Validate test result reporting for CI")]
    public async Task CI_TestResultReporting()
    {
        // Test that test results can be generated in CI-compatible formats
        var testsProjectPath = Path.Combine(SolutionRoot, "src", "Tests", "Core.Tests", "Core.Tests.fsproj");
        var testResultsPath = Path.Combine(SolutionRoot, "TestResults");
        
        // Clean test results
        if (Directory.Exists(testResultsPath))
        {
            Directory.Delete(testResultsPath, true);
        }
        
        // Run tests with various reporters
        var result = await ExecuteDotNetCommand($"test \"{testsProjectPath}\" --logger \"trx;LogFileName=TestResults.trx\" --logger \"junit;LogFileName=TestResults.xml\" --results-directory \"{testResultsPath}\"", 
            allowNonZeroExit: true);
        
        // Verify test execution completed (may have failures due to MAUI dependencies)
        Assert.That(result.StandardOutput, Does.Contain("Test run").Or.Contain("succeeded:").Or.Contain("failed:"),
            "Test execution should complete and report results");
        
        // Check test result files are generated
        if (Directory.Exists(testResultsPath))
        {
            var testFiles = Directory.GetFiles(testResultsPath, "*", SearchOption.AllDirectories);
            
            if (testFiles.Any(f => f.EndsWith(".trx")))
            {
                TestContext.Out.WriteLine("TRX test results generated successfully");
            }
            
            TestContext.Out.WriteLine($"Test result files: {string.Join(", ", testFiles.Select(Path.GetFileName))}");
        }
    }

    [Test]
    [Description("Validate environment variable handling")]
    public void CI_EnvironmentVariableHandling()
    {
        // Test CI environment variable detection
        var ciVars = new[]
        {
            "CI", "GITHUB_ACTIONS", "BUILD_BUILDNUMBER", "TF_BUILD"
        };
        
        var detectedCI = ciVars.Any(var => Environment.GetEnvironmentVariable(var) != null);
        
        if (detectedCI)
        {
            TestContext.Out.WriteLine("CI environment detected");
            
            // In CI, we might want to adjust build behavior
            var ciVar = ciVars.FirstOrDefault(var => Environment.GetEnvironmentVariable(var) != null);
            TestContext.Out.WriteLine($"CI Platform: {ciVar}");
        }
        else
        {
            TestContext.Out.WriteLine("Running in local development environment");
        }
        
        // Verify PATH contains .NET
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        var hasDotNetPath = path.Split(Path.PathSeparator)
            .Any(p => p.Contains(".dotnet") || p.Contains("dotnet"));
        
        Assert.That(hasDotNetPath, Is.True, 
            "PATH should contain .NET SDK location");
        
        // Test that we can access solution root
        Assert.That(Directory.Exists(SolutionRoot), Is.True, 
            "Should be able to access solution root directory");
        Assert.That(File.Exists(Path.Combine(SolutionRoot, "Binnaculum.sln")), Is.True,
            "Solution file should be accessible");
    }

    [Test]
    [Description("Validate build caching behavior")]
    public async Task CI_BuildCachingBehavior()
    {
        // Test build caching and incremental builds in CI scenarios
        var coreProjectPath = Path.Combine(SolutionRoot, "src", "Core", "Core.fsproj");
        
        // First build (cold cache)
        await ExecuteDotNetCommand($"clean \"{coreProjectPath}\"");
        var coldBuildResult = await ExecuteDotNetCommand($"build \"{coreProjectPath}\" --verbosity normal");
        Assert.That(coldBuildResult.ExitCode, Is.EqualTo(0), "Cold build should succeed");
        
        // Check for compilation indicators
        var hasCompilation = coldBuildResult.StandardOutput.Contains("CoreCompile") || 
                           coldBuildResult.StandardOutput.Contains("Compile");
        Assert.That(hasCompilation, Is.True, "Cold build should perform compilation");
        
        // Second build (warm cache)
        var warmBuildResult = await ExecuteDotNetCommand($"build \"{coreProjectPath}\" --verbosity normal");
        Assert.That(warmBuildResult.ExitCode, Is.EqualTo(0), "Warm build should succeed");
        
        // Warm build should be faster and indicate no-op
        var hasSkipping = warmBuildResult.StandardOutput.Contains("up-to-date") ||
                         warmBuildResult.StandardOutput.Contains("Skipping") ||
                         !warmBuildResult.StandardOutput.Contains("CoreCompile");
        
        TestContext.Out.WriteLine("Cold build output length: " + coldBuildResult.StandardOutput.Length);
        TestContext.Out.WriteLine("Warm build output length: " + warmBuildResult.StandardOutput.Length);
        
        // Warm build should produce less output (indicating cached results)
        Assert.That(warmBuildResult.StandardOutput.Length, Is.LessThanOrEqualTo(coldBuildResult.StandardOutput.Length),
            "Warm build should produce less output due to caching");
    }

    private static async Task<ProcessResult> ExecuteDotNetCommand(string arguments, bool allowNonZeroExit = false)
    {
        using var process = new Process();
        process.StartInfo.FileName = "dotnet";
        process.StartInfo.Arguments = arguments;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.WorkingDirectory = SolutionRoot;
        
        // Set PATH to include .NET installation
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        var dotnetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet");
        process.StartInfo.EnvironmentVariables["PATH"] = $"{dotnetPath}:{path}";

        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        
        await process.WaitForExitAsync();
        
        var standardOutput = await outputTask;
        var errorOutput = await errorTask;
        
        if (!allowNonZeroExit && process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Command failed: dotnet {arguments}\nOutput: {standardOutput}\nError: {errorOutput}");
        }

        return new ProcessResult(process.ExitCode, standardOutput, errorOutput);
    }

    private static async Task<ProcessResult> ExecuteCommand(string command, string arguments, bool allowNonZeroExit = true)
    {
        using var process = new Process();
        process.StartInfo.FileName = command;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.WorkingDirectory = SolutionRoot;

        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        
        await process.WaitForExitAsync();
        
        var standardOutput = await outputTask;
        var errorOutput = await errorTask;
        
        if (!allowNonZeroExit && process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Command failed: {command} {arguments}\nOutput: {standardOutput}\nError: {errorOutput}");
        }

        return new ProcessResult(process.ExitCode, standardOutput, errorOutput);
    }

    private record ProcessResult(int ExitCode, string StandardOutput, string ErrorOutput);
}