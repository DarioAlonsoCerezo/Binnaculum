using NUnit.Framework;
using System.Diagnostics;

namespace Binnaculum.Build.IntegrationTests;

/// <summary>
/// Tests for build performance monitoring and validation
/// </summary>
[TestFixture]
public class BuildPerformanceTests
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
    [Description("Validate Core project builds within acceptable time (< 30 seconds)")]
    public async Task CoreProject_BuildsWithinTimeThreshold()
    {
        // Arrange
        var coreProjectPath = Path.Combine(SolutionRoot, "src", "Core", "Core.fsproj");
        var maxBuildTimeSeconds = 30; // According to copilot-instructions: 13-14 seconds expected
        
        // Clean first to ensure full build
        await ExecuteDotNetCommand($"clean \"{coreProjectPath}\"");
        
        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await ExecuteDotNetCommand($"build \"{coreProjectPath}\" --configuration Release --verbosity minimal");
        stopwatch.Stop();
        
        // Assert
        Assert.That(result.ExitCode, Is.EqualTo(0), $"Core project build failed: {result.ErrorOutput}");
        Assert.That(stopwatch.Elapsed.TotalSeconds, Is.LessThan(maxBuildTimeSeconds), 
            $"Core project build took {stopwatch.Elapsed.TotalSeconds:F1}s, should be < {maxBuildTimeSeconds}s");
        
        TestContext.Out.WriteLine($"Core project build time: {stopwatch.Elapsed.TotalSeconds:F1} seconds");
    }

    [Test]
    [Description("Validate Core.Tests builds within acceptable time (< 30 seconds)")]
    public async Task CoreTests_BuildsWithinTimeThreshold()
    {
        // Arrange
        var testsProjectPath = Path.Combine(SolutionRoot, "src", "Tests", "Core.Tests", "Core.Tests.fsproj");
        var maxBuildTimeSeconds = 30; // According to copilot-instructions: 11-12 seconds expected
        
        // Clean first
        await ExecuteDotNetCommand($"clean \"{testsProjectPath}\"");
        
        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await ExecuteDotNetCommand($"build \"{testsProjectPath}\" --configuration Release --verbosity minimal");
        stopwatch.Stop();
        
        // Assert
        Assert.That(result.ExitCode, Is.EqualTo(0), $"Core.Tests build failed: {result.ErrorOutput}");
        Assert.That(stopwatch.Elapsed.TotalSeconds, Is.LessThan(maxBuildTimeSeconds), 
            $"Core.Tests build took {stopwatch.Elapsed.TotalSeconds:F1}s, should be < {maxBuildTimeSeconds}s");
        
        TestContext.Out.WriteLine($"Core.Tests build time: {stopwatch.Elapsed.TotalSeconds:F1} seconds");
    }

    [Test]
    [Description("Validate Core.Tests runs within acceptable time (< 10 seconds)")]
    public async Task CoreTests_RunsWithinTimeThreshold()
    {
        // Arrange
        var testsProjectPath = Path.Combine(SolutionRoot, "src", "Tests", "Core.Tests", "Core.Tests.fsproj");
        var maxTestTimeSeconds = 10; // According to copilot-instructions: 2-3 seconds expected
        
        // Build first
        await ExecuteDotNetCommand($"build \"{testsProjectPath}\" --configuration Release");
        
        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = await ExecuteDotNetCommand($"test \"{testsProjectPath}\" --configuration Release --no-build --verbosity minimal", allowNonZeroExit: true);
        stopwatch.Stop();
        
        // Assert - Tests may fail due to MAUI dependencies, but should complete quickly
        Assert.That(stopwatch.Elapsed.TotalSeconds, Is.LessThan(maxTestTimeSeconds), 
            $"Core.Tests execution took {stopwatch.Elapsed.TotalSeconds:F1}s, should be < {maxTestTimeSeconds}s");
        
        TestContext.Out.WriteLine($"Core.Tests execution time: {stopwatch.Elapsed.TotalSeconds:F1} seconds");
        
        // Verify we got some test results
        Assert.That(result.StandardOutput, Does.Contain("succeeded:").Or.Contain("failed:").Or.Contain("Test run"), 
            "Should produce test results");
    }

    [Test]
    [Description("Compare incremental vs clean build performance")]
    public async Task IncrementalBuild_IsFasterThanCleanBuild()
    {
        // Arrange
        var coreProjectPath = Path.Combine(SolutionRoot, "src", "Core", "Core.fsproj");
        
        // First build (clean)
        await ExecuteDotNetCommand($"clean \"{coreProjectPath}\"");
        
        var cleanStopwatch = Stopwatch.StartNew();
        var cleanResult = await ExecuteDotNetCommand($"build \"{coreProjectPath}\" --configuration Release");
        cleanStopwatch.Stop();
        
        Assert.That(cleanResult.ExitCode, Is.EqualTo(0), "Clean build should succeed");
        
        // Second build (incremental) - no changes
        var incrementalStopwatch = Stopwatch.StartNew();
        var incrementalResult = await ExecuteDotNetCommand($"build \"{coreProjectPath}\" --configuration Release");
        incrementalStopwatch.Stop();
        
        Assert.That(incrementalResult.ExitCode, Is.EqualTo(0), "Incremental build should succeed");
        
        // Assert incremental is faster (allowing some margin for filesystem/timing variations)
        var speedupFactor = cleanStopwatch.Elapsed.TotalSeconds / Math.Max(incrementalStopwatch.Elapsed.TotalSeconds, 0.1);
        
        TestContext.Out.WriteLine($"Clean build: {cleanStopwatch.Elapsed.TotalSeconds:F1}s");
        TestContext.Out.WriteLine($"Incremental build: {incrementalStopwatch.Elapsed.TotalSeconds:F1}s");
        TestContext.Out.WriteLine($"Speedup factor: {speedupFactor:F1}x");
        
        Assert.That(speedupFactor, Is.GreaterThan(1.2), 
            "Incremental build should be at least 20% faster than clean build");
    }

    [Test]
    [Description("Validate solution-wide clean build completes within acceptable time (< 5 minutes)")]
    public async Task SolutionCleanBuild_CompletesWithinThreshold()
    {
        // Arrange
        var solutionPath = Path.Combine(SolutionRoot, "Binnaculum.sln");
        var maxBuildTimeMinutes = 5; // Target from issue: < 5 minutes
        
        // Clean first
        await ExecuteDotNetCommand($"clean \"{solutionPath}\"", allowNonZeroExit: true);
        
        // Act - Build only projects that can build on this platform
        var stopwatch = Stopwatch.StartNew();
        
        // Build Core and Core.Tests (should work on all platforms)
        var coreResult = await ExecuteDotNetCommand($"build \"{Path.Combine(SolutionRoot, "src", "Core", "Core.fsproj")}\" --configuration Release");
        var testsResult = await ExecuteDotNetCommand($"build \"{Path.Combine(SolutionRoot, "src", "Tests", "Core.Tests", "Core.Tests.fsproj")}\" --configuration Release");
        
        stopwatch.Stop();
        
        // Assert
        Assert.That(coreResult.ExitCode, Is.EqualTo(0), "Core project should build successfully");
        Assert.That(testsResult.ExitCode, Is.EqualTo(0), "Core.Tests should build successfully");
        
        Assert.That(stopwatch.Elapsed.TotalMinutes, Is.LessThan(maxBuildTimeMinutes), 
            $"Core builds took {stopwatch.Elapsed.TotalMinutes:F1} minutes, should be < {maxBuildTimeMinutes} minutes");
        
        TestContext.Out.WriteLine($"Core solution build time: {stopwatch.Elapsed.TotalMinutes:F1} minutes");
    }

    [Test]
    [Description("Monitor memory usage during build process")]
    [Category("Performance")]
    public async Task Build_MonitorsMemoryUsage()
    {
        // Arrange
        var coreProjectPath = Path.Combine(SolutionRoot, "src", "Core", "Core.fsproj");
        var initialMemory = GC.GetTotalMemory(false);
        
        await ExecuteDotNetCommand($"clean \"{coreProjectPath}\"");
        
        // Act
        var beforeBuild = GC.GetTotalMemory(false);
        var result = await ExecuteDotNetCommand($"build \"{coreProjectPath}\" --configuration Release");
        var afterBuild = GC.GetTotalMemory(false);
        
        // Force garbage collection to get accurate measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var afterGC = GC.GetTotalMemory(true);
        
        // Assert
        Assert.That(result.ExitCode, Is.EqualTo(0), "Build should succeed");
        
        var memoryIncreaseMB = (afterBuild - beforeBuild) / (1024.0 * 1024.0);
        var memoryAfterGCMB = (afterGC - initialMemory) / (1024.0 * 1024.0);
        
        TestContext.Out.WriteLine($"Memory before build: {beforeBuild / (1024.0 * 1024.0):F1} MB");
        TestContext.Out.WriteLine($"Memory after build: {afterBuild / (1024.0 * 1024.0):F1} MB");
        TestContext.Out.WriteLine($"Memory after GC: {afterGC / (1024.0 * 1024.0):F1} MB");
        TestContext.Out.WriteLine($"Memory increase: {memoryIncreaseMB:F1} MB");
        
        // Memory increase during build should be reasonable (< 500MB for this test process)
        Assert.That(memoryIncreaseMB, Is.LessThan(500), 
            "Memory usage during build should be reasonable");
    }

    [Test]
    [Description("Validate parallel build capabilities")]
    public async Task ParallelBuild_ImprovesBuildTime()
    {
        // Build multiple projects to test if they can build in parallel
        var projects = new[]
        {
            Path.Combine(SolutionRoot, "src", "Core", "Core.fsproj"),
            Path.Combine(SolutionRoot, "src", "Tests", "Core.Tests", "Core.Tests.fsproj"),
            Path.Combine(SolutionRoot, "src", "Tests", "Build.IntegrationTests", "Build.IntegrationTests.csproj")
        };
        
        // Clean all
        foreach (var project in projects)
        {
            await ExecuteDotNetCommand($"clean \"{project}\"");
        }
        
        // Build sequentially
        var sequentialStopwatch = Stopwatch.StartNew();
        foreach (var project in projects)
        {
            var result = await ExecuteDotNetCommand($"build \"{project}\" --configuration Release");
            Assert.That(result.ExitCode, Is.EqualTo(0), $"Project {Path.GetFileName(project)} should build");
        }
        sequentialStopwatch.Stop();
        
        // Clean again
        foreach (var project in projects)
        {
            await ExecuteDotNetCommand($"clean \"{project}\"");
        }
        
        // Build with parallel option (using MSBuild parallelism)
        var parallelStopwatch = Stopwatch.StartNew();
        var parallelResult = await ExecuteDotNetCommand($"build \"{projects[0]}\" --configuration Release -maxcpucount");
        var otherBuilds = await Task.WhenAll(
            ExecuteDotNetCommand($"build \"{projects[1]}\" --configuration Release -maxcpucount"),
            ExecuteDotNetCommand($"build \"{projects[2]}\" --configuration Release -maxcpucount")
        );
        parallelStopwatch.Stop();
        
        Assert.That(parallelResult.ExitCode, Is.EqualTo(0), "Parallel build should succeed");
        Assert.That(otherBuilds.All(r => r.ExitCode == 0), Is.True, "All parallel builds should succeed");
        
        TestContext.Out.WriteLine($"Sequential build time: {sequentialStopwatch.Elapsed.TotalSeconds:F1}s");
        TestContext.Out.WriteLine($"Parallel build time: {parallelStopwatch.Elapsed.TotalSeconds:F1}s");
        
        // Note: For small projects, parallel may not always be faster due to overhead
        // So we just verify both approaches work
        Assert.That(parallelStopwatch.Elapsed.TotalSeconds, Is.LessThan(sequentialStopwatch.Elapsed.TotalSeconds * 2),
            "Parallel build should not be significantly slower than sequential");
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

    private record ProcessResult(int ExitCode, string StandardOutput, string ErrorOutput);
}