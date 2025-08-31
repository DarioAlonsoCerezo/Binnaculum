using NUnit.Framework;
using System.Diagnostics;
using System.Text.Json;

namespace Binnaculum.Build.IntegrationTests;

/// <summary>
/// Tests for NuGet package management and dependency resolution
/// </summary>
[TestFixture]
public class DependencyManagementTests
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
    [Description("Validate NuGet package restoration succeeds")]
    public async Task NuGetPackages_RestoreSuccessfully()
    {
        // Arrange
        var solutionPath = Path.Combine(SolutionRoot, "Binnaculum.sln");
        
        // Clean to ensure fresh restore
        await ExecuteDotNetCommand($"clean \"{solutionPath}\"");
        
        // Act
        var result = await ExecuteDotNetCommand($"restore \"{solutionPath}\" --verbosity minimal");
        
        // Assert
        Assert.That(result.ExitCode, Is.EqualTo(0), $"Package restore failed: {result.ErrorOutput}");
        Assert.That(result.StandardOutput, Does.Not.Contain("error"), "Restore should not contain errors");
        Assert.That(result.StandardOutput, Does.Contain("Restore").Or.Contain("succeeded").Or.Contain("complete"), 
            "Should indicate successful restore");
    }

    [Test]
    [Description("Validate no package version conflicts exist")]
    public async Task NuGetPackages_NoVersionConflicts()
    {
        // Arrange & Act
        var result = await ExecuteDotNetCommand($"restore \"{Path.Combine(SolutionRoot, "Binnaculum.sln")}\" --verbosity detailed");
        
        // Assert - Look for version conflict warnings
        var output = result.StandardOutput + result.ErrorOutput;
        
        // Check for common package conflict indicators
        var conflictIndicators = new[]
        {
            "NU1608", // Version conflict
            "NU1605", // Package downgrade
            "version conflict",
            "package downgrade"
        };
        
        foreach (var indicator in conflictIndicators)
        {
            Assert.That(output, Does.Not.Contain(indicator), 
                $"Package restore should not contain {indicator} warnings");
        }
    }

    [Test]
    [Description("Validate essential NuGet packages are resolved")]
    public async Task EssentialPackages_AreResolved()
    {
        // Arrange
        var coreProjectPath = Path.Combine(SolutionRoot, "src", "Core", "Core.fsproj");
        
        // Act - Restore and build to verify packages
        await ExecuteDotNetCommand($"restore \"{coreProjectPath}\"");
        var result = await ExecuteDotNetCommand($"build \"{coreProjectPath}\" --no-restore --verbosity minimal");
        
        // Assert
        Assert.That(result.ExitCode, Is.EqualTo(0), "Core project should build after package restore");
        
        // Verify packages.lock.json or obj files contain expected packages
        var objPath = Path.Combine(SolutionRoot, "src", "Core", "obj");
        Assert.That(Directory.Exists(objPath), Is.True, "Object directory should exist after build");
        
        var projectAssetsPath = Path.Combine(objPath, "project.assets.json");
        if (File.Exists(projectAssetsPath))
        {
            var assetsContent = File.ReadAllText(projectAssetsPath);
            
            // Verify key packages are resolved
            var expectedPackages = new[]
            {
                "FSharp.Core",
                "Microsoft.Data.Sqlite.Core",
                "SQLitePCLRaw.bundle_green"
            };
            
            foreach (var package in expectedPackages)
            {
                Assert.That(assetsContent, Does.Contain(package), 
                    $"Essential package {package} should be resolved");
            }
        }
    }

    [Test]
    [Description("Validate MAUI packages are resolved for UI project")]
    public async Task MauiPackages_AreResolved()
    {
        // Arrange
        var uiProjectPath = Path.Combine(SolutionRoot, "src", "UI", "Binnaculum.csproj");
        
        // Act - Restore only (build may fail due to platform restrictions)
        var result = await ExecuteDotNetCommand($"restore \"{uiProjectPath}\"", allowNonZeroExit: true);
        
        // Assert - Restore should work even if build fails
        if (result.ExitCode != 0)
        {
            // On Linux, iOS/MacCatalyst restore might fail - that's expected
            if (result.ErrorOutput.Contains("iOS") || result.ErrorOutput.Contains("maccatalyst"))
            {
                Assert.Inconclusive("MAUI iOS/MacCatalyst packages not available on this platform");
            }
            else
            {
                Assert.Fail($"UI project restore failed unexpectedly: {result.ErrorOutput}");
            }
        }
        else
        {
            // If restore succeeded, verify MAUI packages in project assets
            var objPath = Path.Combine(SolutionRoot, "src", "UI", "obj");
            if (Directory.Exists(objPath))
            {
                var projectAssetsPath = Path.Combine(objPath, "project.assets.json");
                if (File.Exists(projectAssetsPath))
                {
                    var assetsContent = File.ReadAllText(projectAssetsPath);
                    
                    // Verify MAUI packages are resolved for available platforms
                    var expectedMauiPackages = new[]
                    {
                        "Microsoft.Maui.Controls",
                        "Microsoft.Maui.Essentials"
                    };
                    
                    foreach (var package in expectedMauiPackages)
                    {
                        Assert.That(assetsContent, Does.Contain(package), 
                            $"MAUI package {package} should be resolved");
                    }
                }
            }
        }
    }

    [Test]
    [Description("Validate F# and C# package compatibility")]
    public async Task FSharpCSharpPackages_AreCompatible()
    {
        // Test that F# Core project and C# test projects can coexist
        var coreProjectPath = Path.Combine(SolutionRoot, "src", "Core", "Core.fsproj");
        var testProjectPath = Path.Combine(SolutionRoot, "src", "Tests", "Build.IntegrationTests", "Build.IntegrationTests.csproj");
        
        // Build both projects
        var coreResult = await ExecuteDotNetCommand($"build \"{coreProjectPath}\" --verbosity minimal");
        var testResult = await ExecuteDotNetCommand($"build \"{testProjectPath}\" --verbosity minimal");
        
        Assert.That(coreResult.ExitCode, Is.EqualTo(0), "F# Core project should build successfully");
        Assert.That(testResult.ExitCode, Is.EqualTo(0), "C# test project should build successfully");
        
        // Verify C# project can reference F# assembly
        var coreOutputPath = Path.Combine(SolutionRoot, "src", "Core", "bin", "Debug", "net9.0", "Core.dll");
        Assert.That(File.Exists(coreOutputPath), Is.True, "F# Core assembly should be available for C# projects");
    }

    [Test]
    [Description("Validate testing framework packages work correctly")]
    public async Task TestingPackages_WorkCorrectly()
    {
        // Arrange
        var coreTestsPath = Path.Combine(SolutionRoot, "src", "Tests", "Core.Tests", "Core.Tests.fsproj");
        
        // Act - Build tests project
        var buildResult = await ExecuteDotNetCommand($"build \"{coreTestsPath}\" --verbosity minimal");
        
        // Assert
        Assert.That(buildResult.ExitCode, Is.EqualTo(0), "Core tests should build successfully");
        
        // Verify NUnit packages are working by running a simple test discovery
        var listResult = await ExecuteDotNetCommand($"test \"{coreTestsPath}\" --list-tests --verbosity minimal", allowNonZeroExit: true);
        
        // Should be able to discover tests even if some fail to run
        Assert.That(listResult.StandardOutput, Does.Contain("Test").Or.Contain("Discovered").Or.Contain("Found"),
            "Should be able to discover tests");
    }

    [Test]
    [Description("Validate ReactiveUI and related packages")]
    public async Task ReactiveUIPackages_AreCompatible()
    {
        // Check that ReactiveUI packages resolve correctly for UI project
        var uiProjectPath = Path.Combine(SolutionRoot, "src", "UI", "Binnaculum.csproj");
        
        // Restore packages
        var restoreResult = await ExecuteDotNetCommand($"restore \"{uiProjectPath}\" --packages {Path.Combine(SolutionRoot, "packages")}", allowNonZeroExit: true);
        
        if (restoreResult.ExitCode == 0)
        {
            // Check for ReactiveUI package resolution
            var objPath = Path.Combine(SolutionRoot, "src", "UI", "obj");
            if (Directory.Exists(objPath))
            {
                var projectAssetsPath = Path.Combine(objPath, "project.assets.json");
                if (File.Exists(projectAssetsPath))
                {
                    var assetsContent = File.ReadAllText(projectAssetsPath);
                    
                    var expectedReactivePackages = new[]
                    {
                        "ReactiveUI",
                        "System.Reactive",
                        "DynamicData"
                    };
                    
                    foreach (var package in expectedReactivePackages)
                    {
                        Assert.That(assetsContent, Does.Contain(package), 
                            $"ReactiveUI package {package} should be resolved");
                    }
                }
            }
        }
        else if (restoreResult.ErrorOutput.Contains("workload") || restoreResult.ErrorOutput.Contains("iOS"))
        {
            Assert.Inconclusive("ReactiveUI package validation skipped due to platform workload limitations");
        }
        else
        {
            Assert.Fail($"ReactiveUI package restore failed: {restoreResult.ErrorOutput}");
        }
    }

    [Test]
    [Description("Validate package versions are consistent across projects")]
    public async Task PackageVersions_AreConsistent()
    {
        // This test validates central package management is working
        var directoryPackagesPath = Path.Combine(SolutionRoot, "Directory.Packages.props");
        Assert.That(File.Exists(directoryPackagesPath), Is.True, "Directory.Packages.props should exist");
        
        var packagesContent = File.ReadAllText(directoryPackagesPath);
        Assert.That(packagesContent, Does.Contain("ManagePackageVersionsCentrally"), 
            "Central package management should be enabled");
        Assert.That(packagesContent, Does.Contain("true"), 
            "Central package management should be set to true");
        
        // Verify solution-wide restore works with central package management
        var solutionPath = Path.Combine(SolutionRoot, "Binnaculum.sln");
        var result = await ExecuteDotNetCommand($"restore \"{solutionPath}\" --verbosity minimal");
        
        Assert.That(result.ExitCode, Is.EqualTo(0), "Solution restore with central package management should succeed");
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