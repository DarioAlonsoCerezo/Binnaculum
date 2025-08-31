using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Binnaculum.Tests.TestUtils.Performance.Gates;

/// <summary>
/// Performance gate validation system for CI/CD pipeline integration
/// Based on patterns from BuildPerformanceTests and issue requirements
/// </summary>
public static class PerformanceGate
{
    /// <summary>
    /// Validate performance metrics against defined thresholds
    /// </summary>
    public static PerformanceGateResult ValidateMetrics(List<PerformanceMetrics> metrics, PerformanceGateConfig config)
    {
        var result = new PerformanceGateResult
        {
            Config = config,
            ValidationTime = DateTime.UtcNow,
            TotalMetrics = metrics.Count
        };
        
        var violations = new List<PerformanceViolation>();
        
        foreach (var metric in metrics)
        {
            // Check execution time thresholds
            if (metric.ElapsedMilliseconds > config.MaxExecutionTimeMs)
            {
                violations.Add(new PerformanceViolation
                {
                    TestName = metric.OperationName,
                    ViolationType = "ExecutionTime",
                    ActualValue = metric.ElapsedMilliseconds,
                    ThresholdValue = config.MaxExecutionTimeMs,
                    Severity = metric.ElapsedMilliseconds > config.CriticalExecutionTimeMs ? "Critical" : "Warning",
                    Message = $"Execution time {metric.ElapsedMilliseconds}ms exceeds threshold {config.MaxExecutionTimeMs}ms"
                });
            }
            
            // Check memory usage thresholds
            var memoryMB = metric.MemoryUsedMB;
            if (Math.Abs(memoryMB) > config.MaxMemoryUsageMB)
            {
                violations.Add(new PerformanceViolation
                {
                    TestName = metric.OperationName,
                    ViolationType = "MemoryUsage",
                    ActualValue = (long)Math.Abs(memoryMB),
                    ThresholdValue = (long)config.MaxMemoryUsageMB,
                    Severity = Math.Abs(memoryMB) > config.CriticalMemoryUsageMB ? "Critical" : "Warning",
                    Message = $"Memory usage {Math.Abs(memoryMB):F1}MB exceeds threshold {config.MaxMemoryUsageMB}MB"
                });
            }
            
            // Check GC pressure thresholds
            if (metric.TotalGCCollections > config.MaxGCCollections)
            {
                violations.Add(new PerformanceViolation
                {
                    TestName = metric.OperationName,
                    ViolationType = "GCPressure",
                    ActualValue = metric.TotalGCCollections,
                    ThresholdValue = config.MaxGCCollections,
                    Severity = metric.TotalGCCollections > config.CriticalGCCollections ? "Critical" : "Warning",
                    Message = $"GC collections {metric.TotalGCCollections} exceeds threshold {config.MaxGCCollections}"
                });
            }
        }
        
        result.Violations = violations;
        result.HasViolations = violations.Any();
        result.CriticalViolations = violations.Where(v => v.Severity == "Critical").ToList();
        result.WarningViolations = violations.Where(v => v.Severity == "Warning").ToList();
        
        return result;
    }
    
    /// <summary>
    /// Load performance gate configuration from file
    /// </summary>
    public static async Task<PerformanceGateConfig> LoadConfig(string configPath = "performance-gate.json")
    {
        if (!File.Exists(configPath))
        {
            var defaultConfig = CreateDefaultConfig();
            await SaveConfig(defaultConfig, configPath);
            Console.WriteLine($"📋 Created default performance gate configuration: {configPath}");
            return defaultConfig;
        }
        
        var json = await File.ReadAllTextAsync(configPath);
        var config = JsonSerializer.Deserialize<PerformanceGateConfig>(json, 
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        
        return config ?? CreateDefaultConfig();
    }
    
    /// <summary>
    /// Save performance gate configuration to file
    /// </summary>
    public static async Task SaveConfig(PerformanceGateConfig config, string configPath = "performance-gate.json")
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        await File.WriteAllTextAsync(configPath, json);
    }
    
    /// <summary>
    /// Create default performance gate configuration
    /// Based on mobile device performance requirements
    /// </summary>
    public static PerformanceGateConfig CreateDefaultConfig()
    {
        return new PerformanceGateConfig
        {
            Name = "Binnaculum TestUtils Performance Gates",
            Version = "1.0",
            Description = "Performance thresholds for Binnaculum investment app testing infrastructure",
            
            // Execution time thresholds (mobile-optimized)
            MaxExecutionTimeMs = 2000,      // 2 seconds for most operations
            CriticalExecutionTimeMs = 5000, // 5 seconds critical threshold
            
            // Memory usage thresholds (mobile-optimized)
            MaxMemoryUsageMB = 50,          // 50MB max memory usage
            CriticalMemoryUsageMB = 100,    // 100MB critical threshold
            
            // GC pressure thresholds
            MaxGCCollections = 5,           // Max 5 GC collections per test
            CriticalGCCollections = 10,     // 10 GC collections critical
            
            // Test-specific overrides for known intensive operations
            TestSpecificLimits = new Dictionary<string, PerformanceTestLimits>
            {
                ["BrokerAccountTemplate_LargePortfolio"] = new PerformanceTestLimits
                {
                    MaxExecutionTimeMs = 3000,
                    MaxMemoryUsageMB = 75,
                    MaxGCCollections = 8
                },
                ["PercentageControl_ExtraLarge"] = new PerformanceTestLimits
                {
                    MaxExecutionTimeMs = 1500,
                    MaxMemoryUsageMB = 30,
                    MaxGCCollections = 3
                },
                ["ObservableChain_StressTest"] = new PerformanceTestLimits
                {
                    MaxExecutionTimeMs = 5000,
                    MaxMemoryUsageMB = 100,
                    MaxGCCollections = 15
                }
            },
            
            // Platform-specific adjustments
            PlatformAdjustments = new Dictionary<string, PlatformPerformanceAdjustment>
            {
                ["Android"] = new PlatformPerformanceAdjustment
                {
                    ExecutionTimeMultiplier = 1.5,  // Allow 50% more time on mobile
                    MemoryMultiplier = 0.8           // Stricter memory limits on mobile
                },
                ["iOS"] = new PlatformPerformanceAdjustment
                {
                    ExecutionTimeMultiplier = 1.3,  // iOS is typically faster
                    MemoryMultiplier = 0.7           // iOS has strict memory management
                },
                ["Windows"] = new PlatformPerformanceAdjustment
                {
                    ExecutionTimeMultiplier = 1.0,  // Baseline for desktop
                    MemoryMultiplier = 2.0           // More relaxed memory limits on desktop
                }
            }
        };
    }
    
    /// <summary>
    /// Apply platform-specific adjustments to configuration
    /// </summary>
    public static PerformanceGateConfig ApplyPlatformAdjustments(PerformanceGateConfig config, string platform)
    {
        if (!config.PlatformAdjustments.ContainsKey(platform))
            return config;
        
        var adjustment = config.PlatformAdjustments[platform];
        var adjustedConfig = new PerformanceGateConfig
        {
            Name = config.Name,
            Version = config.Version,
            Description = $"{config.Description} (Adjusted for {platform})",
            MaxExecutionTimeMs = (long)(config.MaxExecutionTimeMs * adjustment.ExecutionTimeMultiplier),
            CriticalExecutionTimeMs = (long)(config.CriticalExecutionTimeMs * adjustment.ExecutionTimeMultiplier),
            MaxMemoryUsageMB = config.MaxMemoryUsageMB * adjustment.MemoryMultiplier,
            CriticalMemoryUsageMB = config.CriticalMemoryUsageMB * adjustment.MemoryMultiplier,
            MaxGCCollections = config.MaxGCCollections,
            CriticalGCCollections = config.CriticalGCCollections,
            TestSpecificLimits = config.TestSpecificLimits,
            PlatformAdjustments = config.PlatformAdjustments
        };
        
        return adjustedConfig;
    }
    
    /// <summary>
    /// Generate performance gate report
    /// </summary>
    public static string GenerateReport(PerformanceGateResult result)
    {
        var report = $"🚦 Performance Gate Report - {result.ValidationTime:yyyy-MM-dd HH:mm:ss} UTC\n";
        report += $"Configuration: {result.Config.Name} v{result.Config.Version}\n";
        report += $"Total Metrics Validated: {result.TotalMetrics}\n\n";
        
        if (!result.HasViolations)
        {
            report += "✅ ALL PERFORMANCE GATES PASSED!\n";
            report += "No performance threshold violations detected.\n";
            return report;
        }
        
        report += $"❌ Performance Gate Failures Detected!\n";
        report += $"Critical Violations: {result.CriticalViolations.Count}\n";
        report += $"Warning Violations: {result.WarningViolations.Count}\n\n";
        
        if (result.CriticalViolations.Any())
        {
            report += "🔴 CRITICAL VIOLATIONS:\n";
            foreach (var violation in result.CriticalViolations)
            {
                report += $"  {violation.TestName} - {violation.ViolationType}\n";
                report += $"    {violation.Message}\n";
            }
            report += "\n";
        }
        
        if (result.WarningViolations.Any())
        {
            report += "🟡 WARNING VIOLATIONS:\n";
            foreach (var violation in result.WarningViolations)
            {
                report += $"  {violation.TestName} - {violation.ViolationType}\n";
                report += $"    {violation.Message}\n";
            }
        }
        
        return report;
    }
}

// Configuration and result classes
public class PerformanceGateConfig
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Global thresholds
    public long MaxExecutionTimeMs { get; set; }
    public long CriticalExecutionTimeMs { get; set; }
    public double MaxMemoryUsageMB { get; set; }
    public double CriticalMemoryUsageMB { get; set; }
    public int MaxGCCollections { get; set; }
    public int CriticalGCCollections { get; set; }
    
    // Test-specific overrides
    public Dictionary<string, PerformanceTestLimits> TestSpecificLimits { get; set; } = new();
    
    // Platform-specific adjustments
    public Dictionary<string, PlatformPerformanceAdjustment> PlatformAdjustments { get; set; } = new();
}

public class PerformanceTestLimits
{
    public long MaxExecutionTimeMs { get; set; }
    public double MaxMemoryUsageMB { get; set; }
    public int MaxGCCollections { get; set; }
}

public class PlatformPerformanceAdjustment
{
    public double ExecutionTimeMultiplier { get; set; } = 1.0;
    public double MemoryMultiplier { get; set; } = 1.0;
}

public class PerformanceGateResult
{
    public PerformanceGateConfig Config { get; set; } = new();
    public DateTime ValidationTime { get; set; }
    public int TotalMetrics { get; set; }
    public bool HasViolations { get; set; }
    public List<PerformanceViolation> Violations { get; set; } = new();
    public List<PerformanceViolation> CriticalViolations { get; set; } = new();
    public List<PerformanceViolation> WarningViolations { get; set; } = new();
    
    public bool HasCriticalViolations => CriticalViolations.Any();
    public bool ShouldFailBuild => HasCriticalViolations;
}

public class PerformanceViolation
{
    public string TestName { get; set; } = string.Empty;
    public string ViolationType { get; set; } = string.Empty;
    public long ActualValue { get; set; }
    public long ThresholdValue { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}