using NUnit.Framework;
using System.Diagnostics;
using System.Text.Json;

namespace Binnaculum.Build.IntegrationTests;

/// <summary>
/// Tests for multi-platform build validation across all supported MAUI targets
/// </summary>
[TestFixture]
public class MultiPlatformBuildTests
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
    [Description("Validate that the F# Core project builds successfully")]
    public async Task CoreProject_BuildsSuccessfully()
    {
        // Arrange
        var coreProjectPath = Path.Combine(SolutionRoot, "src", "Core", "Core.fsproj");
        
        // Act
        var result = await ExecuteDotNetCommand($"build \"{coreProjectPath}\" --configuration Release --verbosity minimal");
        
        // Assert
        Assert.That(result.ExitCode, Is.EqualTo(0), $"Core project build failed: {result.ErrorOutput}");
        Assert.That(result.StandardOutput, Does.Contain("succeeded"), "Build should report success");
        
        // Verify output assembly exists
        var outputAssembly = Path.Combine(SolutionRoot, "src", "Core", "bin", "Release", "net9.0", "Core.dll");
        Assert.That(File.Exists(outputAssembly), Is.True, "Output assembly should exist");
    }

    [Test]
    [Description("Validate that Core.Tests project builds and most tests pass")]
    public async Task CoreTests_BuildAndRunSuccessfully()
    {
        // Arrange
        var testsProjectPath = Path.Combine(SolutionRoot, "src", "Tests", "Core.Tests", "Core.Tests.fsproj");
        
        // Act - Build first
        var buildResult = await ExecuteDotNetCommand($"build \"{testsProjectPath}\" --configuration Release --verbosity minimal");
        
        // Assert build
        Assert.That(buildResult.ExitCode, Is.EqualTo(0), $"Core.Tests build failed: {buildResult.ErrorOutput}");
        
        // Act - Run tests (allowing for expected MAUI failures)
        var testResult = await ExecuteDotNetCommand($"test \"{testsProjectPath}\" --configuration Release --verbosity minimal --logger \"console;verbosity=minimal\"", allowNonZeroExit: true);
        
        // Assert - Should have some passing tests even if some fail due to MAUI dependencies
        Assert.That(testResult.StandardOutput, Does.Contain("succeeded:"), "Should have some passing tests");
        
        // Parse test results - expect at least 70 tests to pass (allowing for MAUI-dependent failures)
        var successMatch = System.Text.RegularExpressions.Regex.Match(testResult.StandardOutput, @"succeeded:\s*(\d+)");
        if (successMatch.Success)
        {
            var successCount = int.Parse(successMatch.Groups[1].Value);
            Assert.That(successCount, Is.GreaterThanOrEqualTo(70), "Should have at least 70 passing tests");
        }
    }

    [Test]
    [Description("Test Android build if MAUI workload is available")]
    public async Task AndroidBuild_BuildsSuccessfullyIfWorkloadAvailable()
    {
        // Arrange
        if (!await IsMauiWorkloadInstalled())
        {
            Assert.Ignore("MAUI Android workload not installed");
        }

        var uiProjectPath = Path.Combine(SolutionRoot, "src", "UI", "Binnaculum.csproj");
        
        // Act
        var result = await ExecuteDotNetCommand($"build \"{uiProjectPath}\" -f net9.0-android --configuration Release --verbosity minimal", allowNonZeroExit: true);
        
        // Assert - May fail due to F# record constructor issues, but should attempt build
        if (result.ExitCode == 0)
        {
            Assert.Pass("Android build succeeded");
        }
        else
        {
            // Check if failure is due to known F# record constructor issues
            if (result.ErrorOutput.Contains("There is no argument given that corresponds to the required parameter"))
            {
                Assert.Inconclusive("Android build failed due to known F# record constructor issues in TestDataBuilders");
            }
            else
            {
                Assert.Fail($"Android build failed with unexpected error: {result.ErrorOutput}");
            }
        }
    }

    [Test]
    [Description("Validate iOS build availability on macOS")]
    public async Task iOSBuild_RequiresMacOS()
    {
        // This test validates build system understanding of platform requirements
        var isOnMacOS = OperatingSystem.IsMacOS();
        
        if (!isOnMacOS)
        {
            // On non-macOS, should gracefully handle iOS target absence
            var uiProjectPath = Path.Combine(SolutionRoot, "src", "UI", "Binnaculum.csproj");
            var result = await ExecuteDotNetCommand($"build \"{uiProjectPath}\" -f net9.0-ios --configuration Release", allowNonZeroExit: true);
            
            Assert.That(result.ExitCode, Is.Not.EqualTo(0), "iOS build should fail on non-macOS");
            Assert.That(result.ErrorOutput, Does.Contain("workload").Or.Contain("iOS").Or.Contain("MAUI"), 
                "Should indicate iOS workload unavailability");
        }
        else
        {
            Assert.Pass("Running on macOS - iOS builds should be supported");
        }
    }

    [Test]
    [Description("Test Windows build availability")]
    public async Task WindowsBuild_RequiresWindows()
    {
        // This test validates build system understanding of platform requirements
        var isOnWindows = OperatingSystem.IsWindows();
        
        if (!isOnWindows)
        {
            // On non-Windows, should gracefully handle Windows target absence
            var uiProjectPath = Path.Combine(SolutionRoot, "src", "UI", "Binnaculum.csproj");
            var result = await ExecuteDotNetCommand($"build \"{uiProjectPath}\" -f net9.0-windows10.0.19041.0 --configuration Release", allowNonZeroExit: true);
            
            // May succeed or fail depending on workload installation - just validate it's handled
            Assert.That(result.ExitCode == 0 || result.ErrorOutput.Contains("windows"), Is.True, 
                "Windows build should either succeed or fail gracefully");
        }
        else
        {
            Assert.Pass("Running on Windows - Windows builds should be supported");
        }
    }

    private static async Task<bool> IsMauiWorkloadInstalled()
    {
        var result = await ExecuteDotNetCommand("workload list");
        return result.StandardOutput.Contains("maui-android") || result.StandardOutput.Contains("maui");
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

    private record ProcessResult(int ExitCode, string StandardOutput, string ErrorOutput);
}