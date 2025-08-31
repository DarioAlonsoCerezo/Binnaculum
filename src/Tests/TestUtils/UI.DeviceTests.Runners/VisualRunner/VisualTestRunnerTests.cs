using Binnaculum.UI.DeviceTests.Runners.VisualRunner.Services;
using Binnaculum.UI.DeviceTests.Runners.VisualRunner.ViewModels;

namespace Binnaculum.UI.DeviceTests.Runners.VisualRunner;

/// <summary>
/// Tests for the Visual Test Runner infrastructure to validate it works correctly.
/// </summary>
public class VisualTestRunnerTests
{
    [Fact]
    public void TestRunnerViewModel_Initialization_ShouldSucceed()
    {
        // Arrange & Act
        var viewModel = new TestRunnerViewModel();

        // Assert
        Assert.NotNull(viewModel);
        Assert.Equal("Ready", viewModel.StatusMessage);
        Assert.Equal(TestRunnerState.Ready, viewModel.State);
        Assert.False(viewModel.IsDiscovering);
        Assert.False(viewModel.IsRunning);
        Assert.Empty(viewModel.TestAssemblies);
    }

    [Fact]
    public async Task VisualDeviceRunner_DiscoverTests_ShouldFindTests()
    {
        // Arrange
        var runner = new VisualDeviceRunner();

        // Act
        var assemblies = await runner.DiscoverTestsAsync();

        // Assert
        Assert.NotNull(assemblies);
        // We should find at least this test assembly
        Assert.True(assemblies.Count > 0, "Should discover at least one test assembly");
        
        // Check if we found tests in this assembly
        var thisAssembly = assemblies.FirstOrDefault(a => a.Name.Contains("DeviceTests.Runners"));
        Assert.NotNull(thisAssembly);
        Assert.True(thisAssembly.TotalTests > 0, "Should find tests in the current assembly");
    }

    [Fact]
    public void TestCaseViewModel_StatusConversion_ShouldWork()
    {
        // Arrange
        var testCase = new TestCaseViewModel
        {
            Name = "TestMethod",
            FullName = "TestClass.TestMethod",
            DisplayName = "Test Method",
            Status = TestCaseStatus.Pending
        };

        // Act & Assert
        Assert.Equal(TestCaseStatus.Pending, testCase.Status);
        Assert.False(testCase.IsCompleted);
        Assert.False(testCase.HasError);

        // Test status change
        testCase.Status = TestCaseStatus.Passed;
        Assert.True(testCase.IsCompleted);
        Assert.False(testCase.HasError);

        testCase.Status = TestCaseStatus.Failed;
        testCase.ErrorMessage = "Test failed";
        Assert.True(testCase.IsCompleted);
        Assert.True(testCase.HasError);
    }

    [Fact]
    public void TestAssemblyViewModel_SelectionCascade_ShouldWork()
    {
        // Arrange
        var assembly = new TestAssemblyViewModel { Name = "TestAssembly" };
        var testClass = new TestClassViewModel { Name = "TestClass" };
        var testCase1 = new TestCaseViewModel { Name = "Test1" };
        var testCase2 = new TestCaseViewModel { Name = "Test2" };
        
        testClass.TestCases.Add(testCase1);
        testClass.TestCases.Add(testCase2);
        assembly.TestClasses.Add(testClass);
        assembly.UpdateTestCounts();

        // Act
        assembly.IsSelected = true;

        // Assert
        Assert.True(testClass.IsSelected, "Test class should be selected when assembly is selected");
        Assert.True(testCase1.IsSelected, "Test case 1 should be selected when assembly is selected");
        Assert.True(testCase2.IsSelected, "Test case 2 should be selected when assembly is selected");

        // Act - Deselect
        assembly.IsSelected = false;

        // Assert
        Assert.False(testClass.IsSelected, "Test class should be deselected when assembly is deselected");
        Assert.False(testCase1.IsSelected, "Test case 1 should be deselected when assembly is deselected");
        Assert.False(testCase2.IsSelected, "Test case 2 should be deselected when assembly is deselected");
    }

    [Fact]
    public void TestRunnerViewModel_SearchFiltering_ShouldWork()
    {
        // Arrange
        var viewModel = new TestRunnerViewModel();
        var assembly = new TestAssemblyViewModel { Name = "TestAssembly" };
        var testClass = new TestClassViewModel { Name = "SampleTestClass" };
        var testCase1 = new TestCaseViewModel 
        { 
            Name = "TestMethod1", 
            DisplayName = "Sample Test Method 1" 
        };
        var testCase2 = new TestCaseViewModel 
        { 
            Name = "AnotherMethod", 
            DisplayName = "Different Test" 
        };
        
        testClass.TestCases.Add(testCase1);
        testClass.TestCases.Add(testCase2);
        assembly.TestClasses.Add(testClass);
        assembly.UpdateTestCounts();
        viewModel.TestAssemblies.Add(assembly);

        // Act - No filter
        var filteredAssemblies = viewModel.FilteredTestAssemblies;
        Assert.Single(filteredAssemblies);
        Assert.Equal(2, filteredAssemblies.First().TotalTests);

        // Act - Filter by "Sample"
        viewModel.SearchText = "Sample";
        filteredAssemblies = viewModel.FilteredTestAssemblies;
        
        // Assert
        Assert.Single(filteredAssemblies);
        Assert.Equal(1, filteredAssemblies.First().TotalTests);
        Assert.Contains("TestMethod1", filteredAssemblies.First().TestClasses.First().TestCases.Select(tc => tc.Name));
    }
}