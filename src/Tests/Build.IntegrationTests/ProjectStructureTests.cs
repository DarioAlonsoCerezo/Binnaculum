using NUnit.Framework;
using System.Diagnostics;
using System.Xml.Linq;

namespace Binnaculum.Build.IntegrationTests;

/// <summary>
/// Tests for project structure validation and solution integrity
/// </summary>
[TestFixture]
public class ProjectStructureTests
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
    [Description("Validate solution file integrity and project references")]
    public void Solution_HasValidStructure()
    {
        // Arrange
        var solutionPath = Path.Combine(SolutionRoot, "Binnaculum.sln");
        
        // Act
        Assert.That(File.Exists(solutionPath), Is.True, "Solution file should exist");
        
        var solutionContent = File.ReadAllText(solutionPath);
        
        // Assert - Check for expected projects
        var expectedProjects = new[]
        {
            "Core.fsproj",
            "Binnaculum.csproj", // UI project
            "Core.Tests.fsproj",
            "UI.DeviceTests.csproj",
            "UI.DeviceTests.Runners.csproj",
            "UITest.Core.csproj"
        };
        
        foreach (var project in expectedProjects)
        {
            Assert.That(solutionContent, Does.Contain(project), 
                $"Solution should reference {project}");
        }
        
        // Verify solution format version
        Assert.That(solutionContent, Does.Contain("Microsoft Visual Studio Solution File"),
            "Solution should have proper format header");
    }

    [Test]
    [Description("Validate Directory.Build.props functionality")]
    public void DirectoryBuildProps_HasCorrectConfiguration()
    {
        // Arrange
        var buildPropsPath = Path.Combine(SolutionRoot, "Directory.Build.props");
        
        // Act & Assert
        Assert.That(File.Exists(buildPropsPath), Is.True, "Directory.Build.props should exist");
        
        var buildPropsContent = File.ReadAllText(buildPropsPath);
        
        // Verify key properties are set
        Assert.That(buildPropsContent, Does.Contain("TreatWarningsAsErrors"), 
            "Should treat warnings as errors");
        Assert.That(buildPropsContent, Does.Contain("NoWarn"), 
            "Should have noise warning suppressions");
    }

    [Test]
    [Description("Validate Directory.Packages.props functionality")]
    public void DirectoryPackagesProps_HasCorrectPackages()
    {
        // Arrange
        var packagesPropsPath = Path.Combine(SolutionRoot, "Directory.Packages.props");
        
        // Act & Assert
        Assert.That(File.Exists(packagesPropsPath), Is.True, "Directory.Packages.props should exist");
        
        var packagesPropsContent = File.ReadAllText(packagesPropsPath);
        
        // Verify central package management is enabled
        Assert.That(packagesPropsContent, Does.Contain("ManagePackageVersionsCentrally"), 
            "Should enable central package management");
        Assert.That(packagesPropsContent, Does.Contain("true"), 
            "Central package management should be set to true");
        
        // Verify key package categories exist
        var expectedPackageCategories = new[]
        {
            "Microsoft.Maui.Core",
            "FSharp.Core",
            "NUnit",
            "ReactiveUI",
            "SQLitePCLRaw.bundle_green"
        };
        
        foreach (var package in expectedPackageCategories)
        {
            Assert.That(packagesPropsContent, Does.Contain(package), 
                $"Should include {package} package reference");
        }
    }

    [Test]
    [Description("Validate Core F# project configuration")]
    public void CoreProject_HasCorrectConfiguration()
    {
        // Arrange
        var coreProjectPath = Path.Combine(SolutionRoot, "src", "Core", "Core.fsproj");
        
        // Act & Assert
        Assert.That(File.Exists(coreProjectPath), Is.True, "Core project should exist");
        
        var projectXml = XDocument.Load(coreProjectPath);
        var root = projectXml.Root;
        
        // Verify target framework
        var targetFramework = root?.Descendants("TargetFramework").FirstOrDefault()?.Value;
        Assert.That(targetFramework, Is.EqualTo("net9.0"), "Core should target .NET 9");
        
        // Verify F# project SDK
        var sdk = root?.Attribute("Sdk")?.Value;
        Assert.That(sdk, Is.EqualTo("Microsoft.NET.Sdk"), "Should use correct SDK");
    }

    [Test]
    [Description("Validate UI MAUI project multi-targeting configuration")]
    public void UIProject_HasCorrectMultiTargeting()
    {
        // Arrange
        var uiProjectPath = Path.Combine(SolutionRoot, "src", "UI", "Binnaculum.csproj");
        
        // Act & Assert
        Assert.That(File.Exists(uiProjectPath), Is.True, "UI project should exist");
        
        var projectXml = XDocument.Load(uiProjectPath);
        var root = projectXml.Root;
        
        // Verify multi-targeting
        var targetFrameworks = root?.Descendants("TargetFrameworks").FirstOrDefault()?.Value;
        Assert.That(targetFrameworks, Is.Not.Null.And.Not.Empty, "Should have target frameworks defined");
        Assert.That(targetFrameworks, Does.Contain("net9.0-android"), "Should target Android");
        Assert.That(targetFrameworks, Does.Contain("net9.0-ios"), "Should target iOS");
        Assert.That(targetFrameworks, Does.Contain("net9.0-maccatalyst"), "Should target MacCatalyst");
        
        // Verify MAUI usage
        var useMaui = root?.Descendants("UseMaui").FirstOrDefault()?.Value;
        Assert.That(useMaui, Is.EqualTo("true"), "Should use MAUI");
        
        // Verify essential MAUI properties
        var applicationId = root?.Descendants("ApplicationId").FirstOrDefault()?.Value;
        Assert.That(applicationId, Is.Not.Null.And.Not.Empty, "Should have application ID");
    }

    [Test]
    [Description("Validate project references are correctly configured")]
    public void Projects_HaveValidReferences()
    {
        // Test UI project references Core
        var uiProjectPath = Path.Combine(SolutionRoot, "src", "UI", "Binnaculum.csproj");
        var uiProjectXml = XDocument.Load(uiProjectPath);
        
        var coreReference = uiProjectXml.Root?.Descendants("ProjectReference")
            .FirstOrDefault(pr => pr.Attribute("Include")?.Value.Contains("Core.fsproj") == true);
        
        Assert.That(coreReference, Is.Not.Null, "UI project should reference Core project");
        
        // Test Core.Tests references Core
        var testsProjectPath = Path.Combine(SolutionRoot, "src", "Tests", "Core.Tests", "Core.Tests.fsproj");
        var testsProjectXml = XDocument.Load(testsProjectPath);
        
        var coreReferenceInTests = testsProjectXml.Root?.Descendants("ProjectReference")
            .FirstOrDefault(pr => pr.Attribute("Include")?.Value.Contains("Core.fsproj") == true);
        
        Assert.That(coreReferenceInTests, Is.Not.Null, "Core.Tests should reference Core project");
    }

    [Test]
    [Description("Validate essential configuration files exist")]
    public void ConfigurationFiles_Exist()
    {
        var essentialFiles = new[]
        {
            ".editorconfig",
            ".gitignore",
            "XAMLStylerConfiguration.json",
            "dotnet-install.sh"
        };
        
        foreach (var file in essentialFiles)
        {
            var filePath = Path.Combine(SolutionRoot, file);
            Assert.That(File.Exists(filePath), Is.True, $"{file} should exist");
        }
    }

    [Test]
    [Description("Validate resource structure for MAUI project")]
    public void UIProject_HasRequiredResources()
    {
        var uiResourcesPath = Path.Combine(SolutionRoot, "src", "UI", "Resources");
        
        Assert.That(Directory.Exists(uiResourcesPath), Is.True, "Resources directory should exist");
        
        var expectedResourceDirs = new[]
        {
            "AppIcon",
            "Fonts", 
            "Images",
            "Raw",
            "Splash"
        };
        
        foreach (var dir in expectedResourceDirs)
        {
            var dirPath = Path.Combine(uiResourcesPath, dir);
            Assert.That(Directory.Exists(dirPath), Is.True, $"{dir} resources directory should exist");
        }
    }

    [Test]
    [Description("Validate test project structure")]
    public void TestProjects_HaveCorrectStructure()
    {
        var testsBasePath = Path.Combine(SolutionRoot, "src", "Tests");
        
        // Verify test projects exist
        var expectedTestProjects = new[]
        {
            Path.Combine("Core.Tests", "Core.Tests.fsproj"),
            Path.Combine("TestUtils", "UI.DeviceTests", "UI.DeviceTests.csproj"),
            Path.Combine("TestUtils", "UI.DeviceTests.Runners", "UI.DeviceTests.Runners.csproj"),
            Path.Combine("TestUtils", "UITest.Core", "UITest.Core.csproj")
        };
        
        foreach (var testProject in expectedTestProjects)
        {
            var projectPath = Path.Combine(testsBasePath, testProject);
            Assert.That(File.Exists(projectPath), Is.True, $"Test project {testProject} should exist");
        }
    }
}