using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Reactive.Linq;
using Binnaculum.Tests.TestUtils.Performance.Analytics;
using Binnaculum.Tests.TestUtils.Performance.Gates;

namespace Binnaculum.Tests.TestUtils.Performance;

/// <summary>
/// Comprehensive performance monitoring integration test
/// Demonstrates the complete performance testing and monitoring workflow
/// </summary>
[TestFixture]
public class PerformanceMonitoringIntegrationTests
{
    private PerformanceMetricsCollector _metricsCollector;
    private string _tempDirectory;
    
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"BinnaculumPerformanceTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
        
        _metricsCollector = new PerformanceMetricsCollector(_tempDirectory);
        
        Console.WriteLine($"📁 Performance test output directory: {_tempDirectory}");
    }
    
    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        // Export final metrics
        await _metricsCollector.ExportToJson();
        
        Console.WriteLine($"🧹 Cleaning up test directory: {_tempDirectory}");
        try
        {
            if (Directory.Exists(_tempDirectory))
                Directory.Delete(_tempDirectory, recursive: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not clean up temp directory: {ex.Message}");
        }
    }
    
    [Test]
    [Order(1)]
    [Description("Run comprehensive TestUtils performance suite and collect metrics")]
    public async Task RunComprehensivePerformanceSuite()
    {
        Console.WriteLine("🚀 Starting comprehensive performance monitoring test suite...\n");
        
        var testSuiteStart = DateTime.UtcNow;
        var allMetrics = new List<PerformanceMetrics>();
        
        // 1. Component Performance Tests
        Console.WriteLine("📊 Running Component Performance Tests...");
        await RunComponentPerformanceTests(allMetrics);
        
        // 2. Observable Chain Performance Tests
        Console.WriteLine("\n🔄 Running Observable Chain Performance Tests...");
        await RunObservableChainTests(allMetrics);
        
        // 3. Large Dataset Performance Tests
        Console.WriteLine("\n📈 Running Large Dataset Performance Tests...");
        await RunLargeDatasetTests(allMetrics);
        
        // 4. Memory Management Tests
        Console.WriteLine("\n🧠 Running Memory Management Tests...");
        await RunMemoryManagementTests(allMetrics);
        
        // 5. Platform-Specific Tests
        Console.WriteLine("\n🖥️ Running Platform-Specific Tests...");
        await RunPlatformSpecificTests(allMetrics);
        
        var testSuiteEnd = DateTime.UtcNow;
        var totalDuration = testSuiteEnd - testSuiteStart;
        
        // Record test suite execution summary
        var summary = new TestExecutionSummary
        {
            TotalTests = allMetrics.Count,
            PassedTests = allMetrics.Count, // All completed successfully if we got here
            FailedTests = 0,
            SkippedTests = 0,
            TotalDuration = totalDuration,
            AverageTestTime = TimeSpan.FromMilliseconds(allMetrics.Average(m => m.ElapsedMilliseconds))
        };
        
        _metricsCollector.RecordTestExecution("ComprehensivePerformanceSuite", summary);
        
        Console.WriteLine($"\n✅ Comprehensive performance suite completed!");
        Console.WriteLine($"   Total Duration: {totalDuration.TotalSeconds:F1} seconds");
        Console.WriteLine($"   Tests Executed: {allMetrics.Count}");
        Console.WriteLine($"   Average Test Time: {summary.AverageTestTime.TotalMilliseconds:F1}ms");
        
        // Validate all metrics are reasonable
        Assert.That(allMetrics.Count, Is.GreaterThan(10), "Should have executed multiple performance tests");
        Assert.That(totalDuration.TotalMinutes, Is.LessThan(10), "Full suite should complete within 10 minutes");
    }
    
    [Test]
    [Order(2)]
    [Description("Generate comprehensive performance report and analytics")]
    public async Task GeneratePerformanceReport()
    {
        Console.WriteLine("📋 Generating comprehensive performance report...\n");
        
        // Generate performance report
        var report = _metricsCollector.GenerateReport();
        
        Console.WriteLine("Performance Report Summary:");
        Console.WriteLine($"  Generated at: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine($"  Total records: {report.TotalRecords}");
        Console.WriteLine($"  Categories: {report.Categories.Count}");
        
        foreach (var category in report.Categories)
        {
            Console.WriteLine($"\n📊 {category.Category}:");
            Console.WriteLine($"   Tests: {category.RecordCount}");
            Console.WriteLine($"   Avg Time: {category.AverageExecutionTime:F1}ms");
            Console.WriteLine($"   Time Range: {category.MinExecutionTime}ms - {category.MaxExecutionTime}ms");
            Console.WriteLine($"   Avg Memory: {category.AverageMemoryUsed / 1024 / 1024:F1}MB");
            Console.WriteLine($"   GC Collections: {category.TotalGCCollections}");
        }
        
        // Export detailed metrics
        var exportPath = await _metricsCollector.ExportToJson("comprehensive-performance-report.json");
        
        Console.WriteLine($"\n📁 Detailed report exported to: {exportPath}");
        
        // Validate report completeness
        Assert.That(report.TotalRecords, Is.GreaterThan(0), "Report should contain performance records");
        Assert.That(report.Categories.Count, Is.GreaterThan(3), "Should have multiple test categories");
        Assert.That(File.Exists(exportPath), "Export file should be created");
    }
    
    [Test]
    [Order(3)]
    [Description("Validate performance against defined gates and thresholds")]
    public async Task ValidatePerformanceGates()
    {
        Console.WriteLine("🚦 Validating performance against defined gates...\n");
        
        // Create performance gate configuration
        var gateConfig = PerformanceGate.CreateDefaultConfig();
        
        // Adjust for current platform (since we're running in CI/test environment)
        var platform = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Linux) ? "Linux" : "Windows";
        
        gateConfig = PerformanceGate.ApplyPlatformAdjustments(gateConfig, platform);
        
        Console.WriteLine($"Using platform-adjusted configuration for: {platform}");
        
        // Create sample metrics for gate validation (simulate collected metrics)
        var sampleMetrics = CreateSampleMetricsForGateValidation();
        
        // Validate against performance gates
        var gateResult = PerformanceGate.ValidateMetrics(sampleMetrics, gateConfig);
        
        // Generate gate validation report
        var gateReport = PerformanceGate.GenerateReport(gateResult);
        Console.WriteLine(gateReport);
        
        // Save gate configuration for future use
        var configPath = Path.Combine(_tempDirectory, "performance-gate.json");
        await PerformanceGate.SaveConfig(gateConfig, configPath);
        
        Console.WriteLine($"📋 Performance gate configuration saved to: {configPath}");
        
        // Validate gate functionality
        Assert.That(gateResult.TotalMetrics, Is.EqualTo(sampleMetrics.Count), "All metrics should be validated");
        
        // For this test, we expect some violations since we're using intentionally diverse metrics
        if (gateResult.HasViolations)
        {
            Console.WriteLine($"⚠️ Found {gateResult.Violations.Count} performance gate violations (expected for test)");
            Assert.That(gateResult.CriticalViolations.Count, Is.LessThan(3), "Should not have too many critical violations");
        }
        else
        {
            Console.WriteLine("✅ All performance gates passed!");
        }
    }
    
    [Test]
    [Order(4)]
    [Description("Test performance regression detection against baseline")]
    public async Task TestPerformanceRegressionDetection()
    {
        Console.WriteLine("📈 Testing performance regression detection...\n");
        
        // Create a baseline performance file
        var baselineMetrics = CreateBaselinePerformanceMetrics();
        var baselineExport = new PerformanceMetricsExport
        {
            ExportTime = DateTime.UtcNow.AddDays(-1), // Simulate baseline from yesterday
            Records = baselineMetrics.Select(m => new PerformanceTestRecord
            {
                TestName = m.OperationName,
                Category = "Baseline",
                Timestamp = DateTime.UtcNow.AddDays(-1),
                ElapsedMilliseconds = m.ElapsedMilliseconds,
                MemoryUsedBytes = m.MemoryUsedBytes,
                Gen0Collections = m.Gen0Collections,
                Gen1Collections = m.Gen1Collections,
                Gen2Collections = m.Gen2Collections
            }).ToList()
        };
        
        // Save baseline to file
        var baselineFilePath = Path.Combine(_tempDirectory, "baseline-performance.json");
        var json = System.Text.Json.JsonSerializer.Serialize(baselineExport, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });
        await File.WriteAllTextAsync(baselineFilePath, json);
        
        // Simulate current performance metrics (with some regressions)
        var currentMetrics = CreateCurrentPerformanceMetrics();
        var currentCollector = new PerformanceMetricsCollector(_tempDirectory);
        
        foreach (var metric in currentMetrics)
        {
            currentCollector.RecordMetrics(metric.OperationName, "Current", metric);
        }
        
        // Check for regressions
        var regressionReport = await currentCollector.CheckForRegressions(baselineFilePath, thresholdPercent: 15.0);
        
        Console.WriteLine($"Regression Analysis Results:");
        Console.WriteLine($"  Has baseline: {regressionReport.HasBaseline}");
        Console.WriteLine($"  Tests compared: {regressionReport.TotalTestsCompared}");
        Console.WriteLine($"  Regressions found: {regressionReport.RegressionCount}");
        Console.WriteLine($"  Improvements found: {regressionReport.ImprovementCount}");
        
        if (regressionReport.HasRegressions)
        {
            Console.WriteLine("\n🔴 Performance Regressions:");
            foreach (var regression in regressionReport.Regressions.Where(r => r.IsRegression))
            {
                Console.WriteLine($"  {regression.TestName}: {regression.TimeChangePercent:F1}% slower");
            }
        }
        
        if (regressionReport.HasImprovements)
        {
            Console.WriteLine("\n🟢 Performance Improvements:");
            foreach (var improvement in regressionReport.Regressions.Where(r => !r.IsRegression))
            {
                Console.WriteLine($"  {improvement.TestName}: {Math.Abs(improvement.TimeChangePercent):F1}% faster");
            }
        }
        
        // Validate regression detection functionality
        Assert.That(regressionReport.HasBaseline, Is.True, "Should successfully load baseline");
        Assert.That(regressionReport.TotalTestsCompared, Is.GreaterThan(0), "Should compare some tests");
        
        Console.WriteLine("✅ Performance regression detection test completed");
    }
    
    private async Task RunComponentPerformanceTests(List<PerformanceMetrics> allMetrics)
    {
        // Simulate BrokerAccountTemplate performance tests
        var templateMetrics = await PerformanceMeasurement.MeasureAsync(async () =>
        {
            // Simulate template rendering
            var accounts = Enumerable.Range(1, 100).Select(i => new
            {
                Name = $"Account_{i}",
                Balance = 1000m * i,
                FormattedBalance = $"${1000m * i:F2}"
            }).ToList();
            
            await Task.Delay(50); // Simulate rendering time
            return accounts.Count;
        }, "BrokerAccountTemplate_MediumDataset");
        
        _metricsCollector.RecordMetrics("BrokerAccountTemplate_MediumDataset", "ComponentPerformance", templateMetrics);
        allMetrics.Add(templateMetrics);
        
        // Simulate PercentageControl performance tests
        var percentageMetrics = PerformanceMeasurement.MeasureSync(() =>
        {
            var calculations = new List<decimal>();
            for (int i = 1; i <= 1000; i++)
            {
                var current = 1000m * i;
                var initial = 800m * i;
                var percentage = (current - initial) / initial * 100m;
                calculations.Add(percentage);
            }
            return calculations.Count;
        }, "PercentageControl_1000Calculations");
        
        _metricsCollector.RecordMetrics("PercentageControl_1000Calculations", "ComponentPerformance", percentageMetrics);
        allMetrics.Add(percentageMetrics);
    }
    
    private async Task RunObservableChainTests(List<PerformanceMetrics> allMetrics)
    {
        var result = await MemoryLeakDetection.TestObservableMemoryLeak(disposables =>
        {
            return System.Reactive.Linq.Observable.Range(1, 100)
                .Select(i => new { Id = i, Value = i * 2.5m })
                .Where(item => item.Value > 10)
                .Do(item => { /* Process item */ });
        }, iterations: 100);
        
        var observableMetrics = new PerformanceMetrics
        {
            OperationName = "ObservableChain_MemoryLeakTest",
            ElapsedMilliseconds = (long)result.AfterDisposeSnapshot.ElapsedTime.TotalMilliseconds,
            MemoryUsedBytes = (long)(result.PeakMemoryMB * 1024 * 1024)
        };
        
        _metricsCollector.RecordMetrics("ObservableChain_MemoryLeakTest", "ObservablePerformance", observableMetrics);
        allMetrics.Add(observableMetrics);
    }
    
    private Task RunLargeDatasetTests(List<PerformanceMetrics> allMetrics)
    {
        var largeDatasetMetrics = PerformanceMeasurement.MeasureSync(() =>
        {
            // Simulate large portfolio processing
            var portfolio = Enumerable.Range(1, 5000).Select(i => new
            {
                Id = i,
                InitialValue = 1000m + i,
                CurrentValue = 1000m + i + (decimal)(new Random(i).NextDouble() * 500),
                Transactions = Enumerable.Range(1, 10).Select(j => new { Amount = j * 10m }).ToList()
            }).ToList();
            
            // Process portfolio
            var totalValue = portfolio.Sum(p => p.CurrentValue);
            var totalGain = portfolio.Sum(p => p.CurrentValue - p.InitialValue);
            var totalTransactions = portfolio.Sum(p => p.Transactions.Count);
            
            return new { TotalValue = totalValue, TotalGain = totalGain, TotalTransactions = totalTransactions };
        }, "LargeDataset_5000Accounts");
        
        _metricsCollector.RecordMetrics("LargeDataset_5000Accounts", "LargeDatasetPerformance", largeDatasetMetrics);
        allMetrics.Add(largeDatasetMetrics);
        return Task.CompletedTask;
    }
    
    private Task RunMemoryManagementTests(List<PerformanceMetrics> allMetrics)
    {
        var memoryTestMetrics = PerformanceMeasurement.MeasureSync(() =>
        {
            var allocations = new List<object>();
            
            // Allocate memory in chunks
            for (int i = 0; i < 1000; i++)
            {
                allocations.Add(new string('X', 1000)); // 1KB strings
                
                if (i % 100 == 0)
                {
                    // Periodic cleanup
                    for (int j = 0; j < 50 && j < allocations.Count; j++)
                    {
                        allocations[j] = null;
                    }
                    
                    if (i % 200 == 0)
                    {
                        GC.Collect();
                    }
                }
            }
            
            return allocations.Count;
        }, "MemoryManagement_ChunkedAllocation");
        
        _metricsCollector.RecordMetrics("MemoryManagement_ChunkedAllocation", "MemoryManagement", memoryTestMetrics);
        allMetrics.Add(memoryTestMetrics);
        return Task.CompletedTask;
    }
    
    private Task RunPlatformSpecificTests(List<PerformanceMetrics> allMetrics)
    {
        var platformMetrics = PerformanceMeasurement.MeasureSync(() =>
        {
            var platform = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Linux) ? "Linux" : "Windows";
            
            // CPU-intensive calculation
            int primeCount = 0;
            for (int n = 2; n < 1000; n++)
            {
                bool isPrime = true;
                for (int i = 2; i * i <= n; i++)
                {
                    if (n % i == 0)
                    {
                        isPrime = false;
                        break;
                    }
                }
                if (isPrime) primeCount++;
            }
            
            return new { Platform = platform, PrimeCount = primeCount };
        }, "PlatformSpecific_CPUIntensive");
        
        _metricsCollector.RecordMetrics("PlatformSpecific_CPUIntensive", "PlatformSpecific", platformMetrics);
        allMetrics.Add(platformMetrics);
        return Task.CompletedTask;
    }
    
    private List<PerformanceMetrics> CreateSampleMetricsForGateValidation()
    {
        return new List<PerformanceMetrics>
        {
            new() { OperationName = "FastOperation", ElapsedMilliseconds = 50, MemoryUsedBytes = 1024 * 1024 }, // 1MB
            new() { OperationName = "SlowOperation", ElapsedMilliseconds = 3000, MemoryUsedBytes = 5 * 1024 * 1024 }, // 5MB
            new() { OperationName = "MemoryIntensiveOperation", ElapsedMilliseconds = 500, MemoryUsedBytes = 75 * 1024 * 1024 }, // 75MB
            new() { OperationName = "GCHeavyOperation", ElapsedMilliseconds = 1000, MemoryUsedBytes = 10 * 1024 * 1024, Gen0Collections = 8, Gen1Collections = 2 },
            new() { OperationName = "OptimalOperation", ElapsedMilliseconds = 100, MemoryUsedBytes = 2 * 1024 * 1024, Gen0Collections = 1 }
        };
    }
    
    private List<PerformanceMetrics> CreateBaselinePerformanceMetrics()
    {
        return new List<PerformanceMetrics>
        {
            new() { OperationName = "Operation_A", ElapsedMilliseconds = 100, MemoryUsedBytes = 5 * 1024 * 1024 },
            new() { OperationName = "Operation_B", ElapsedMilliseconds = 200, MemoryUsedBytes = 10 * 1024 * 1024 },
            new() { OperationName = "Operation_C", ElapsedMilliseconds = 300, MemoryUsedBytes = 15 * 1024 * 1024 }
        };
    }
    
    private List<PerformanceMetrics> CreateCurrentPerformanceMetrics()
    {
        return new List<PerformanceMetrics>
        {
            new() { OperationName = "Operation_A", ElapsedMilliseconds = 120, MemoryUsedBytes = 6 * 1024 * 1024 }, // 20% slower
            new() { OperationName = "Operation_B", ElapsedMilliseconds = 180, MemoryUsedBytes = 9 * 1024 * 1024 }, // 10% faster
            new() { OperationName = "Operation_C", ElapsedMilliseconds = 350, MemoryUsedBytes = 18 * 1024 * 1024 } // 17% slower
        };
    }
}