using System.Reflection;
using Binnaculum.UI.DeviceTests.Runners.VisualRunner.ViewModels;
using Xunit;
using Xunit.Sdk;

namespace Binnaculum.UI.DeviceTests.Runners.VisualRunner.Services;

/// <summary>
/// Service responsible for discovering and executing device tests in the visual runner.
/// </summary>
public class VisualDeviceRunner
{
    private readonly ILogger<VisualDeviceRunner>? _logger;

    public VisualDeviceRunner(ILogger<VisualDeviceRunner>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Discovers all available tests from loaded assemblies.
    /// </summary>
    public async Task<List<TestAssemblyViewModel>> DiscoverTestsAsync()
    {
        try
        {
            _logger?.LogInformation("Starting test discovery");

            var testAssemblies = new List<TestAssemblyViewModel>();
            
            // Get all loaded assemblies that might contain tests
            var assemblies = GetTestAssemblies();
            
            foreach (var assembly in assemblies)
            {
                _logger?.LogDebug($"Examining assembly: {assembly.FullName}");
                
                var assemblyViewModel = await DiscoverTestsInAssemblyAsync(assembly);
                if (assemblyViewModel != null && assemblyViewModel.TestClasses.Any())
                {
                    testAssemblies.Add(assemblyViewModel);
                }
            }

            _logger?.LogInformation($"Test discovery completed. Found {testAssemblies.Sum(a => a.TotalTests)} tests in {testAssemblies.Count} assemblies");
            return testAssemblies;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during test discovery");
            throw;
        }
    }

    /// <summary>
    /// Executes a collection of selected tests.
    /// </summary>
    public async Task<TestExecutionResults> ExecuteTestsAsync(
        IEnumerable<TestCaseViewModel> selectedTests,
        IProgress<TestExecutionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation($"Starting execution of {selectedTests.Count()} tests");

            var results = new TestExecutionResults();
            var testList = selectedTests.ToList();
            
            for (int i = 0; i < testList.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var test = testList[i];
                
                // Report progress
                var progressReport = new TestExecutionProgress
                {
                    CurrentTest = test,
                    CompletedTests = i,
                    TotalTests = testList.Count,
                    Progress = (double)i / testList.Count
                };
                progress?.Report(progressReport);

                // Execute the test
                var result = await ExecuteSingleTestAsync(test, cancellationToken);
                results.AddResult(result);

                // Update test status
                test.Status = result.Status;
                test.Duration = result.Duration;
                test.ErrorMessage = result.ErrorMessage ?? string.Empty;
                test.StackTrace = result.StackTrace ?? string.Empty;
            }

            // Report completion
            progress?.Report(new TestExecutionProgress
            {
                CompletedTests = testList.Count,
                TotalTests = testList.Count,
                Progress = 1.0,
                IsCompleted = true
            });

            _logger?.LogInformation($"Test execution completed. {results.PassedCount} passed, {results.FailedCount} failed, {results.SkippedCount} skipped");
            return results;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during test execution");
            throw;
        }
    }

    private List<Assembly> GetTestAssemblies()
    {
        var assemblies = new List<Assembly>();
        
        // Get the current assembly (DeviceTests.Runners) and related test assemblies
        var currentAssembly = Assembly.GetExecutingAssembly();
        assemblies.Add(currentAssembly);
        
        // Try to find the DeviceTests assembly
        try
        {
            var deviceTestsAssembly = Assembly.Load("UI.DeviceTests");
            if (deviceTestsAssembly != null)
            {
                assemblies.Add(deviceTestsAssembly);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Could not load UI.DeviceTests assembly");
        }

        // Look for any other assemblies with test attributes
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (HasXunitTests(assembly) && !assemblies.Contains(assembly))
            {
                assemblies.Add(assembly);
            }
        }

        return assemblies;
    }

    private static bool HasXunitTests(Assembly assembly)
    {
        try
        {
            // Check if assembly contains any types with Fact or Theory attributes
            return assembly.GetTypes()
                .Any(type => type.GetMethods()
                    .Any(method => method.GetCustomAttributes(typeof(FactAttribute), false).Any() ||
                                  method.GetCustomAttributes(typeof(TheoryAttribute), false).Any()));
        }
        catch
        {
            // Assembly might not be accessible or have security restrictions
            return false;
        }
    }

    private Task<TestAssemblyViewModel?> DiscoverTestsInAssemblyAsync(Assembly assembly)
    {
        try
        {
            var assemblyViewModel = new TestAssemblyViewModel
            {
                Name = assembly.GetName().Name ?? "Unknown",
                FullPath = assembly.Location
            };

            var testTypes = assembly.GetTypes()
                .Where(type => type.GetMethods()
                    .Any(method => method.GetCustomAttributes(typeof(FactAttribute), false).Any() ||
                                  method.GetCustomAttributes(typeof(TheoryAttribute), false).Any()))
                .ToList();

            foreach (var testType in testTypes)
            {
                var testClassViewModel = new TestClassViewModel
                {
                    Name = testType.Name,
                    FullName = testType.FullName ?? testType.Name
                };

                var testMethods = testType.GetMethods()
                    .Where(method => method.GetCustomAttributes(typeof(FactAttribute), false).Any() ||
                                    method.GetCustomAttributes(typeof(TheoryAttribute), false).Any());

                foreach (var testMethod in testMethods)
                {
                    var testCaseViewModel = new TestCaseViewModel
                    {
                        Name = testMethod.Name,
                        FullName = $"{testType.FullName}.{testMethod.Name}",
                        DisplayName = GetTestDisplayName(testMethod),
                        Status = TestCaseStatus.Pending
                    };

                    // Store reflection info for later execution
                    testCaseViewModel.SetMethodInfo(testType, testMethod);
                    
                    testClassViewModel.TestCases.Add(testCaseViewModel);
                }

                if (testClassViewModel.TestCases.Any())
                {
                    assemblyViewModel.TestClasses.Add(testClassViewModel);
                }
            }

            assemblyViewModel.UpdateTestCounts();

            return Task.FromResult(assemblyViewModel.TestClasses.Any() ? assemblyViewModel : null);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, $"Error discovering tests in assembly {assembly.FullName}");
            return Task.FromResult<TestAssemblyViewModel?>(null);
        }
    }

    private static string GetTestDisplayName(MethodInfo method)
    {
        // Look for DisplayName from System.ComponentModel or just use method name
        var displayNameAttr = method.GetCustomAttributes(false)
            .Where(attr => attr.GetType().Name == "DisplayNameAttribute")
            .FirstOrDefault();
            
        if (displayNameAttr != null)
        {
            var displayNameProperty = displayNameAttr.GetType().GetProperty("DisplayName");
            var displayName = displayNameProperty?.GetValue(displayNameAttr)?.ToString();
            if (!string.IsNullOrEmpty(displayName))
                return displayName;
        }

        return method.Name;
    }

    private async Task<TestExecutionResult> ExecuteSingleTestAsync(TestCaseViewModel test, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            test.Status = TestCaseStatus.Running;
            
            // Get the test method info
            var (testType, testMethod) = test.GetMethodInfo();
            if (testType == null || testMethod == null)
            {
                return new TestExecutionResult
                {
                    TestName = test.Name,
                    Status = TestCaseStatus.Failed,
                    Duration = TimeSpan.Zero,
                    ErrorMessage = "Could not locate test method",
                    StackTrace = string.Empty
                };
            }

            // Create test instance
            var testInstance = Activator.CreateInstance(testType);
            
            // Execute the test method
            await Task.Run(() =>
            {
                testMethod.Invoke(testInstance, null);
            }, cancellationToken);

            var duration = DateTime.UtcNow - startTime;
            
            return new TestExecutionResult
            {
                TestName = test.Name,
                Status = TestCaseStatus.Passed,
                Duration = duration,
                ErrorMessage = null,
                StackTrace = null
            };
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            
            return new TestExecutionResult
            {
                TestName = test.Name,
                Status = TestCaseStatus.Failed,
                Duration = duration,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace ?? string.Empty
            };
        }
    }
}

/// <summary>
/// Results of test execution.
/// </summary>
public class TestExecutionResults
{
    private readonly List<TestExecutionResult> _results = new();

    public void AddResult(TestExecutionResult result)
    {
        _results.Add(result);
    }

    public int TotalCount => _results.Count;
    public int PassedCount => _results.Count(r => r.Status == TestCaseStatus.Passed);
    public int FailedCount => _results.Count(r => r.Status == TestCaseStatus.Failed);
    public int SkippedCount => _results.Count(r => r.Status == TestCaseStatus.Skipped);
    
    public IReadOnlyList<TestExecutionResult> Results => _results.AsReadOnly();
}

/// <summary>
/// Result of a single test execution.
/// </summary>
public class TestExecutionResult
{
    public required string TestName { get; set; }
    public TestCaseStatus Status { get; set; }
    public TimeSpan Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
}

/// <summary>
/// Progress information during test execution.
/// </summary>
public class TestExecutionProgress
{
    public TestCaseViewModel? CurrentTest { get; set; }
    public int CompletedTests { get; set; }
    public int TotalTests { get; set; }
    public double Progress { get; set; }
    public bool IsCompleted { get; set; }
    public string StatusMessage => CurrentTest != null ? $"Running: {CurrentTest.DisplayName}" : "Preparing tests...";
}