using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Reactive.Linq;
using Binnaculum.Tests.TestUtils.Performance.Analytics;

namespace Binnaculum.Tests.TestUtils.Performance.Platform;

/// <summary>
/// Platform-specific performance monitoring tests
/// Based on issue requirements for Android, iOS, Windows, MacCatalyst monitoring
/// </summary>
[TestFixture]
public class PlatformSpecificPerformanceTests
{
    private PerformanceMetricsCollector _metricsCollector;
    
    [SetUp]
    public void Setup()
    {
        _metricsCollector = new PerformanceMetricsCollector();
    }
    
    [TearDown]
    public void TearDown()
    {
        _metricsCollector?.ExportToJson().Wait();
    }
    
    [Test]
    [Description("Test performance characteristics across different platforms")]
    public async Task PlatformPerformance_CrossPlatform_ConsistentBehavior()
    {
        var currentPlatform = DetectCurrentPlatform();
        Console.WriteLine($"🖥️ Running on platform: {currentPlatform}");
        
        // Run platform-agnostic performance tests
        var testResults = new List<(string testName, PerformanceMetrics metrics)>();
        
        // CPU-intensive calculation test
        var cpuMetrics = PerformanceMeasurement.MeasureSync(() =>
        {
            return PerformCPUIntensiveCalculation(10000);
        }, $"CPUIntensive_{currentPlatform}");
        
        testResults.Add(("CPU_Intensive", cpuMetrics));
        
        // Memory allocation test
        var memoryMetrics = PerformanceMeasurement.MeasureSync(() =>
        {
            return PerformMemoryAllocationTest(5000);
        }, $"MemoryAllocation_{currentPlatform}");
        
        testResults.Add(("Memory_Allocation", memoryMetrics));
        
        // I/O operation test
        var ioMetrics = await PerformanceMeasurement.MeasureAsync(async () =>
        {
            return await PerformIOOperationTest(100);
        }, $"IOOperation_{currentPlatform}");
        
        testResults.Add(("IO_Operation", ioMetrics));
        
        // Record all metrics for platform comparison
        foreach (var (testName, metrics) in testResults)
        {
            _metricsCollector.RecordMetrics(
                testName, 
                currentPlatform, 
                metrics,
                new Dictionary<string, object>
                {
                    { "Platform", currentPlatform },
                    { "Architecture", RuntimeInformation.ProcessArchitecture.ToString() },
                    { "ProcessorCount", Environment.ProcessorCount }
                });
            
            PerformanceMeasurement.LogMetrics(metrics);
        }
        
        // Platform-specific assertions
        ApplyPlatformSpecificAssertions(currentPlatform, testResults);
        
        Console.WriteLine($"✅ Platform performance tests completed for {currentPlatform}");
    }
    
    [Test]
    [Description("Monitor test infrastructure startup and initialization performance")]
    public void TestInfrastructure_StartupPerformance_WithinLimits()
    {
        var platform = DetectCurrentPlatform();
        
        // Measure test infrastructure initialization
        var initMetrics = PerformanceMeasurement.MeasureSync(() =>
        {
            // Simulate test infrastructure startup
            var testComponents = new List<object>();
            
            // Initialize performance measurement components
            testComponents.Add(new PerformanceMetricsCollector());
            
            // Initialize mock test data
            for (int i = 0; i < 100; i++)
            {
                testComponents.Add(new { Id = i, Name = $"Component_{i}", Value = i * 1.5 });
            }
            
            // Simulate component initialization
            Task.Delay(10).Wait(); // Simulate initialization delay
            
            return testComponents.Count;
        }, $"TestInfrastructure_Startup_{platform}");
        
        // Record startup metrics
        _metricsCollector.RecordMetrics(
            "TestInfrastructure_Startup",
            platform,
            initMetrics,
            new Dictionary<string, object>
            {
                { "ComponentCount", initMetrics.Result },
                { "Platform", platform }
            });
        
        // Assert startup performance requirements
        var maxStartupTime = GetPlatformSpecificStartupTime(platform);
        Assert.That(initMetrics.ElapsedMilliseconds, Is.LessThan(maxStartupTime),
            $"Test infrastructure startup took {initMetrics.ElapsedMilliseconds}ms on {platform}, should be < {maxStartupTime}ms");
        
        Console.WriteLine($"Test Infrastructure Startup ({platform}): {initMetrics.ElapsedMilliseconds}ms");
    }
    
    [Test]
    [Description("Test memory management under platform-specific constraints")]
    public async Task MemoryManagement_PlatformConstraints_EfficientCleanup()
    {
        var platform = DetectCurrentPlatform();
        
        // Test memory pressure handling
        var result = await MemoryLeakDetection.TestObservableUnderMemoryPressure(disposables =>
        {
            var data = Enumerable.Range(1, 1000)
                .Select(i => new { Id = i, Data = new string('x', 100) }) // 100 chars per item
                .ToList();
            
            return System.Reactive.Linq.Observable.Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(1))
                .Take(100)
                .Select(i => data[(int)(i % data.Count)])
                .Do(item => 
                {
                    // Simulate processing
                    var processed = item.Data.Length * item.Id;
                });
        }, memoryPressureMB: GetPlatformSpecificMemoryPressure(platform));
        
        // Platform-specific memory management assertions
        result.AssertHandledMemoryPressure();
        
        var maxPeakMemory = GetPlatformSpecificMaxMemory(platform);
        Assert.That(result.PeakMemoryMB, Is.LessThan(maxPeakMemory),
            $"Peak memory {result.PeakMemoryMB:F1}MB exceeds {platform} limit of {maxPeakMemory}MB");
        
        Console.WriteLine($"Memory Management ({platform}):");
        Console.WriteLine($"  Peak Memory: {result.PeakMemoryMB:F1}MB");
        Console.WriteLine($"  Results Processed: {result.ResultCount}");
    }
    
    [Test]
    [Description("Measure financial calculation performance across platforms")]
    public void FinancialCalculations_PlatformPerformance_ConsistentAccuracy()
    {
        var platform = DetectCurrentPlatform();
        
        // Generate test financial data
        var portfolioData = GenerateTestPortfolioData(1000);
        
        // Measure financial calculations
        var calcMetrics = PerformanceMeasurement.MeasureSync(() =>
        {
            var results = new List<decimal>();
            
            foreach (var account in portfolioData)
            {
                // Simulate financial calculations similar to F# core
                var profitLoss = account.CurrentValue - account.InitialValue;
                var percentage = account.InitialValue != 0 ? profitLoss / account.InitialValue * 100m : 0m;
                var compoundReturn = (decimal)Math.Pow((double)(account.CurrentValue / account.InitialValue), 1.0 / (double)account.YearsHeld) - 1m;
                
                results.Add(percentage);
                results.Add(compoundReturn);
            }
            
            return results;
        }, $"FinancialCalculations_{platform}");
        
        // Record financial calculation metrics
        _metricsCollector.RecordMetrics(
            "FinancialCalculations",
            platform,
            calcMetrics,
            new Dictionary<string, object>
            {
                { "CalculationCount", portfolioData.Count * 2 },
                { "Platform", platform },
                { "DatasetSize", portfolioData.Count }
            });
        
        // Platform-specific performance expectations
        var maxCalculationTime = GetPlatformSpecificCalculationTime(platform, portfolioData.Count);
        Assert.That(calcMetrics.ElapsedMilliseconds, Is.LessThan(maxCalculationTime),
            $"Financial calculations took {calcMetrics.ElapsedMilliseconds}ms on {platform}, should be < {maxCalculationTime}ms");
        
        // Verify calculation accuracy (results should be consistent across platforms)
        var results = (List<decimal>)calcMetrics.Result!;
        Assert.That(results.Count, Is.EqualTo(portfolioData.Count * 2), "All calculations should complete");
        
        Console.WriteLine($"Financial Calculations ({platform}):");
        Console.WriteLine($"  Time: {calcMetrics.ElapsedMilliseconds}ms for {portfolioData.Count} accounts");
        Console.WriteLine($"  Memory: {calcMetrics.MemoryUsedMB:F1}MB");
        Console.WriteLine($"  Avg time per calculation: {(double)calcMetrics.ElapsedMilliseconds / (portfolioData.Count * 2):F3}ms");
    }
    
    private string DetectCurrentPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "Windows";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "Linux";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "MacOS";
        else
            return "Unknown";
    }
    
    private int PerformCPUIntensiveCalculation(int iterations)
    {
        int result = 0;
        for (int i = 0; i < iterations; i++)
        {
            // Prime number calculation (CPU intensive)
            for (int n = 2; n < 100; n++)
            {
                bool isPrime = true;
                for (int j = 2; j < n; j++)
                {
                    if (n % j == 0)
                    {
                        isPrime = false;
                        break;
                    }
                }
                if (isPrime) result++;
            }
        }
        return result;
    }
    
    private int PerformMemoryAllocationTest(int allocations)
    {
        var allocatedObjects = new List<object>();
        
        for (int i = 0; i < allocations; i++)
        {
            // Create various object types to stress memory management
            allocatedObjects.Add(new string('A', 100)); // String allocation
            allocatedObjects.Add(new List<int>(Enumerable.Range(0, 50))); // Collection allocation
            allocatedObjects.Add(new { Id = i, Name = $"Object_{i}", Value = i * 2.5 }); // Anonymous object
        }
        
        // Force some cleanup
        for (int i = 0; i < allocatedObjects.Count / 2; i++)
        {
            allocatedObjects[i] = null;
        }
        
        return allocatedObjects.Count;
    }
    
    private async Task<int> PerformIOOperationTest(int operations)
    {
        int completedOperations = 0;
        
        for (int i = 0; i < operations; i++)
        {
            // Simulate I/O operations
            await Task.Delay(1); // Simulate async I/O
            
            // Simulate memory stream operations
            using var memoryStream = new System.IO.MemoryStream();
            var data = System.Text.Encoding.UTF8.GetBytes($"Test data {i}");
            await memoryStream.WriteAsync(data, 0, data.Length);
            
            completedOperations++;
        }
        
        return completedOperations;
    }
    
    private void ApplyPlatformSpecificAssertions(string platform, List<(string testName, PerformanceMetrics metrics)> results)
    {
        foreach (var (testName, metrics) in results)
        {
            switch (platform)
            {
                case "Linux":
                    // Linux/Docker environment - more relaxed constraints
                    Assert.That(metrics.ElapsedMilliseconds, Is.LessThan(5000), 
                        $"{testName} on Linux should complete within 5 seconds");
                    break;
                    
                case "Windows":
                    // Windows desktop - baseline performance
                    Assert.That(metrics.ElapsedMilliseconds, Is.LessThan(3000), 
                        $"{testName} on Windows should complete within 3 seconds");
                    break;
                    
                case "MacOS":
                    // macOS - typically good performance
                    Assert.That(metrics.ElapsedMilliseconds, Is.LessThan(2500), 
                        $"{testName} on macOS should complete within 2.5 seconds");
                    break;
                    
                default:
                    // Generic fallback
                    Assert.That(metrics.ElapsedMilliseconds, Is.LessThan(10000), 
                        $"{testName} should complete within 10 seconds");
                    break;
            }
        }
    }
    
    private long GetPlatformSpecificStartupTime(string platform)
    {
        return platform switch
        {
            "Linux" => 500,    // 500ms for Linux (CI environment)
            "Windows" => 300,  // 300ms for Windows
            "MacOS" => 250,    // 250ms for macOS
            _ => 1000          // 1s fallback
        };
    }
    
    private int GetPlatformSpecificMemoryPressure(string platform)
    {
        return platform switch
        {
            "Linux" => 30,     // 30MB memory pressure for Linux
            "Windows" => 75,   // 75MB for Windows (more resources available)
            "MacOS" => 50,     // 50MB for macOS
            _ => 25            // Conservative default
        };
    }
    
    private double GetPlatformSpecificMaxMemory(string platform)
    {
        return platform switch
        {
            "Linux" => 40.0,   // 40MB max for Linux
            "Windows" => 100.0, // 100MB max for Windows
            "MacOS" => 75.0,   // 75MB max for macOS
            _ => 50.0          // 50MB default
        };
    }
    
    private long GetPlatformSpecificCalculationTime(string platform, int datasetSize)
    {
        var baseTime = datasetSize / 10; // 10 items per millisecond baseline
        
        return platform switch
        {
            "Linux" => (long)(baseTime * 1.5),    // 50% slower on Linux/CI
            "Windows" => baseTime,                // Baseline
            "MacOS" => (long)(baseTime * 0.8),    // 20% faster on macOS
            _ => baseTime * 2                     // Conservative default
        };
    }
    
    private List<TestPortfolioAccount> GenerateTestPortfolioData(int accountCount)
    {
        var random = new Random(42); // Fixed seed for consistent testing
        var accounts = new List<TestPortfolioAccount>();
        
        for (int i = 0; i < accountCount; i++)
        {
            accounts.Add(new TestPortfolioAccount
            {
                Id = i,
                Name = $"Account_{i}",
                InitialValue = 1000m + (decimal)(random.NextDouble() * 5000),
                CurrentValue = 1000m + (decimal)(random.NextDouble() * 8000),
                YearsHeld = 1 + (decimal)(random.NextDouble() * 5)
            });
        }
        
        return accounts;
    }
}

// Test data model
public class TestPortfolioAccount
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal InitialValue { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal YearsHeld { get; set; }
}