using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Binnaculum.Tests.TestUtils.Performance.Analytics;

/// <summary>
/// Collects and stores performance metrics for analysis and reporting
/// Based on patterns from CI/CD integration guides
/// </summary>
public class PerformanceMetricsCollector
{
    private readonly List<PerformanceTestRecord> _records = new();
    private readonly string _outputDirectory;
    
    public PerformanceMetricsCollector(string outputDirectory = "performance-results")
    {
        _outputDirectory = outputDirectory;
        Directory.CreateDirectory(_outputDirectory);
    }
    
    /// <summary>
    /// Record performance metrics from a test execution
    /// </summary>
    public void RecordMetrics(string testName, string category, PerformanceMetrics metrics, 
        Dictionary<string, object>? additionalData = null)
    {
        var record = new PerformanceTestRecord
        {
            TestName = testName,
            Category = category,
            Timestamp = DateTime.UtcNow,
            ElapsedMilliseconds = metrics.ElapsedMilliseconds,
            MemoryUsedBytes = metrics.MemoryUsedBytes,
            Gen0Collections = metrics.Gen0Collections,
            Gen1Collections = metrics.Gen1Collections,
            Gen2Collections = metrics.Gen2Collections,
            AdditionalData = additionalData ?? new Dictionary<string, object>()
        };
        
        _records.Add(record);
        
        Console.WriteLine($"📊 Recorded metrics for {testName}: {metrics.ElapsedMilliseconds}ms, {metrics.MemoryUsedMB:F1}MB");
    }
    
    /// <summary>
    /// Record test execution summary
    /// </summary>
    public void RecordTestExecution(string testSuite, TestExecutionSummary summary)
    {
        var record = new PerformanceTestRecord
        {
            TestName = testSuite,
            Category = "TestExecution",
            Timestamp = DateTime.UtcNow,
            ElapsedMilliseconds = (long)summary.TotalDuration.TotalMilliseconds,
            AdditionalData = new Dictionary<string, object>
            {
                { "TotalTests", summary.TotalTests },
                { "PassedTests", summary.PassedTests },
                { "FailedTests", summary.FailedTests },
                { "SkippedTests", summary.SkippedTests },
                { "FailureRate", summary.FailureRate },
                { "AverageTestTime", summary.AverageTestTime.TotalMilliseconds }
            }
        };
        
        _records.Add(record);
    }
    
    /// <summary>
    /// Export all collected metrics to JSON file
    /// </summary>
    public async Task<string> ExportToJson(string fileName = null)
    {
        fileName ??= $"performance-metrics-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
        var filePath = Path.Combine(_outputDirectory, fileName);
        
        var export = new PerformanceMetricsExport
        {
            ExportTime = DateTime.UtcNow,
            SystemInfo = await GetSystemInfo(),
            Records = _records.ToList()
        };
        
        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        await File.WriteAllTextAsync(filePath, json);
        
        Console.WriteLine($"📈 Performance metrics exported to: {filePath}");
        return filePath;
    }
    
    /// <summary>
    /// Generate performance summary report
    /// </summary>
    public PerformanceReport GenerateReport()
    {
        if (!_records.Any())
        {
            return new PerformanceReport { Summary = "No performance data collected" };
        }
        
        var categories = _records.GroupBy(r => r.Category).ToList();
        var report = new PerformanceReport
        {
            GeneratedAt = DateTime.UtcNow,
            TotalRecords = _records.Count,
            Categories = categories.Select(cat => new PerformanceCategoryReport
            {
                Category = cat.Key,
                RecordCount = cat.Count(),
                AverageExecutionTime = cat.Average(r => r.ElapsedMilliseconds),
                MaxExecutionTime = cat.Max(r => r.ElapsedMilliseconds),
                MinExecutionTime = cat.Min(r => r.ElapsedMilliseconds),
                TotalMemoryUsed = cat.Sum(r => r.MemoryUsedBytes),
                AverageMemoryUsed = cat.Average(r => r.MemoryUsedBytes),
                TotalGCCollections = cat.Sum(r => r.Gen0Collections + r.Gen1Collections + r.Gen2Collections)
            }).ToList()
        };
        
        // Overall summary
        report.Summary = GenerateSummaryText(report);
        
        return report;
    }
    
    /// <summary>
    /// Check for performance regressions against baseline
    /// </summary>
    public async Task<PerformanceRegressionReport> CheckForRegressions(string baselineFilePath, double thresholdPercent = 20.0)
    {
        if (!File.Exists(baselineFilePath))
        {
            return new PerformanceRegressionReport
            {
                HasBaseline = false,
                Message = $"Baseline file not found: {baselineFilePath}. Creating new baseline."
            };
        }
        
        var baselineJson = await File.ReadAllTextAsync(baselineFilePath);
        var baseline = JsonSerializer.Deserialize<PerformanceMetricsExport>(baselineJson, 
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        
        if (baseline == null || !baseline.Records.Any())
        {
            return new PerformanceRegressionReport
            {
                HasBaseline = false,
                Message = "Invalid or empty baseline data"
            };
        }
        
        var regressions = new List<PerformanceRegression>();
        
        foreach (var currentRecord in _records)
        {
            var baselineRecord = baseline.Records
                .FirstOrDefault(b => b.TestName == currentRecord.TestName && b.Category == currentRecord.Category);
            
            if (baselineRecord == null) continue;
            
            var timeDelta = currentRecord.ElapsedMilliseconds - baselineRecord.ElapsedMilliseconds;
            var timePercentChange = (double)timeDelta / baselineRecord.ElapsedMilliseconds * 100;
            
            var memoryDelta = currentRecord.MemoryUsedBytes - baselineRecord.MemoryUsedBytes;
            var memoryPercentChange = baselineRecord.MemoryUsedBytes != 0 
                ? (double)memoryDelta / Math.Abs(baselineRecord.MemoryUsedBytes) * 100 
                : 0;
            
            if (Math.Abs(timePercentChange) > thresholdPercent || Math.Abs(memoryPercentChange) > thresholdPercent)
            {
                regressions.Add(new PerformanceRegression
                {
                    TestName = currentRecord.TestName,
                    Category = currentRecord.Category,
                    BaselineTime = baselineRecord.ElapsedMilliseconds,
                    CurrentTime = currentRecord.ElapsedMilliseconds,
                    TimeChangePercent = timePercentChange,
                    BaselineMemory = baselineRecord.MemoryUsedBytes,
                    CurrentMemory = currentRecord.MemoryUsedBytes,
                    MemoryChangePercent = memoryPercentChange,
                    IsRegression = timePercentChange > thresholdPercent || memoryPercentChange > thresholdPercent
                });
            }
        }
        
        return new PerformanceRegressionReport
        {
            HasBaseline = true,
            ThresholdPercent = thresholdPercent,
            Regressions = regressions,
            TotalTestsCompared = _records.Count,
            RegressionCount = regressions.Count(r => r.IsRegression),
            ImprovementCount = regressions.Count(r => !r.IsRegression)
        };
    }
    
    private Task<SystemInfo> GetSystemInfo()
    {
        return Task.FromResult(new SystemInfo
        {
            DotNetVersion = Environment.Version.ToString(),
            OSVersion = Environment.OSVersion.ToString(),
            ProcessorCount = Environment.ProcessorCount,
            WorkingSet = Environment.WorkingSet,
            MachineName = Environment.MachineName,
            UserName = Environment.UserName,
            CLRVersion = Environment.Version.ToString()
        });
    }
    
    private string GenerateSummaryText(PerformanceReport report)
    {
        var summary = $"Performance Report Generated at {report.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC\n";
        summary += $"Total Records: {report.TotalRecords}\n";
        summary += $"Categories: {report.Categories.Count}\n\n";
        
        foreach (var category in report.Categories)
        {
            summary += $"{category.Category}:\n";
            summary += $"  Tests: {category.RecordCount}\n";
            summary += $"  Avg Time: {category.AverageExecutionTime:F1}ms\n";
            summary += $"  Range: {category.MinExecutionTime}ms - {category.MaxExecutionTime}ms\n";
            summary += $"  Memory: {category.AverageMemoryUsed / 1024 / 1024:F1}MB avg\n";
            summary += $"  GC Collections: {category.TotalGCCollections}\n\n";
        }
        
        return summary;
    }
}

// Data model classes
public class PerformanceTestRecord
{
    public string TestName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public long MemoryUsedBytes { get; set; }
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

public class PerformanceMetricsExport
{
    public DateTime ExportTime { get; set; }
    public SystemInfo SystemInfo { get; set; } = new();
    public List<PerformanceTestRecord> Records { get; set; } = new();
}

public class SystemInfo
{
    public string DotNetVersion { get; set; } = string.Empty;
    public string OSVersion { get; set; } = string.Empty;
    public int ProcessorCount { get; set; }
    public long WorkingSet { get; set; }
    public string MachineName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string CLRVersion { get; set; } = string.Empty;
}

public class TestExecutionSummary
{
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public int SkippedTests { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan AverageTestTime { get; set; }
    public double FailureRate => TotalTests > 0 ? (double)FailedTests / TotalTests : 0;
}

public class PerformanceReport
{
    public DateTime GeneratedAt { get; set; }
    public int TotalRecords { get; set; }
    public List<PerformanceCategoryReport> Categories { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

public class PerformanceCategoryReport
{
    public string Category { get; set; } = string.Empty;
    public int RecordCount { get; set; }
    public double AverageExecutionTime { get; set; }
    public long MaxExecutionTime { get; set; }
    public long MinExecutionTime { get; set; }
    public long TotalMemoryUsed { get; set; }
    public double AverageMemoryUsed { get; set; }
    public int TotalGCCollections { get; set; }
}

public class PerformanceRegressionReport
{
    public bool HasBaseline { get; set; }
    public double ThresholdPercent { get; set; }
    public List<PerformanceRegression> Regressions { get; set; } = new();
    public int TotalTestsCompared { get; set; }
    public int RegressionCount { get; set; }
    public int ImprovementCount { get; set; }
    public string Message { get; set; } = string.Empty;
    
    public bool HasRegressions => RegressionCount > 0;
    public bool HasImprovements => ImprovementCount > 0;
}

public class PerformanceRegression
{
    public string TestName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public long BaselineTime { get; set; }
    public long CurrentTime { get; set; }
    public double TimeChangePercent { get; set; }
    public long BaselineMemory { get; set; }
    public long CurrentMemory { get; set; }
    public double MemoryChangePercent { get; set; }
    public bool IsRegression { get; set; }
}