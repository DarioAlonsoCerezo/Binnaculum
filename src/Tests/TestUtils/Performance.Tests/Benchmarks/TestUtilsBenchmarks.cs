using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using NUnit.Framework;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Binnaculum.Tests.TestUtils.Performance.Benchmarks;

/// <summary>
/// BenchmarkDotNet-based performance tests for TestUtils infrastructure
/// Based on patterns from InvestmentPerformanceExamples.md
/// </summary>
[TestFixture]
public class TestUtilsBenchmarkTests
{
    /// <summary>
    /// Run BenchmarkDotNet tests programmatically within NUnit
    /// </summary>
    [Test, Explicit("Benchmark tests are resource-intensive")]
    [Description("Run comprehensive BenchmarkDotNet performance tests")]
    public void RunAllBenchmarks()
    {
        Console.WriteLine("Running comprehensive TestUtils benchmarks...");
        
        // Run individual benchmark classes
        BenchmarkRunner.Run<ComponentRenderingBenchmarks>();
        BenchmarkRunner.Run<ObservableChainBenchmarks>();
        BenchmarkRunner.Run<DataProcessingBenchmarks>();
        
        Console.WriteLine("All benchmarks completed successfully");
    }
}

/// <summary>
/// Benchmarks for UI component rendering performance
/// Simulates BrokerAccountTemplate and PercentageControl performance testing
/// </summary>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 5)]
public class ComponentRenderingBenchmarks
{
    private List<InvestmentTestData> _smallDataset = new();
    private List<InvestmentTestData> _mediumDataset = new();
    private List<InvestmentTestData> _largeDataset = new();
    
    [GlobalSetup]
    public void Setup()
    {
        _smallDataset = GenerateInvestmentData(10);
        _mediumDataset = GenerateInvestmentData(100);
        _largeDataset = GenerateInvestmentData(1000);
    }
    
    [Benchmark]
    [Arguments(10)]
    [Arguments(100)]
    [Arguments(1000)]
    public async Task SimulateBrokerAccountTemplateRendering(int itemCount)
    {
        // Simulate component rendering with investment data
        var data = GenerateInvestmentData(itemCount);
        
        // Simulate template processing
        var processedData = data
            .Select(item => new
            {
                item.AccountName,
                item.Balance,
                FormattedBalance = FormatCurrency(item.Balance),
                ProfitLoss = CalculateProfitLoss(item.Balance, item.InitialValue),
                LastUpdated = DateTime.Now
            })
            .ToList();
        
        // Simulate async rendering delay
        await Task.Delay(1);
        
        // Simulate final rendering step
        var rendered = processedData.Count;
    }
    
    [Benchmark]
    public void PercentageControlCalculation_SmallDataset() => PerformPercentageCalculations(_smallDataset);
    
    [Benchmark]
    public void PercentageControlCalculation_MediumDataset() => PerformPercentageCalculations(_mediumDataset);
    
    [Benchmark]
    public void PercentageControlCalculation_LargeDataset() => PerformPercentageCalculations(_largeDataset);
    
    private void PerformPercentageCalculations(List<InvestmentTestData> data)
    {
        var calculations = data
            .Select(item => new
            {
                Percentage = (item.Balance - item.InitialValue) / item.InitialValue * 100m,
                AbsoluteChange = item.Balance - item.InitialValue,
                IsProfit = item.Balance > item.InitialValue
            })
            .ToList();
    }
    
    private List<InvestmentTestData> GenerateInvestmentData(int count)
    {
        var random = new Random(42); // Fixed seed for consistent benchmarks
        var data = new List<InvestmentTestData>();
        
        for (int i = 0; i < count; i++)
        {
            data.Add(new InvestmentTestData
            {
                AccountName = $"Account_{i}",
                Balance = 1000m + (decimal)(random.NextDouble() * 10000),
                InitialValue = 1000m,
                Currency = i % 3 == 0 ? "USD" : i % 3 == 1 ? "EUR" : "GBP",
                LastUpdated = DateTime.Now.AddDays(-random.Next(365))
            });
        }
        
        return data;
    }
    
    private string FormatCurrency(decimal amount) => $"${amount:F2}";
    
    private decimal CalculateProfitLoss(decimal current, decimal initial) => current - initial;
}

/// <summary>
/// Benchmarks for Observable chain performance and memory usage
/// Based on ReactiveUI patterns used in the app
/// </summary>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 5)]
public class ObservableChainBenchmarks
{
    [Benchmark]
    [Arguments(100)]
    [Arguments(1000)]
    [Arguments(10000)]
    public async Task ObservableChain_Processing(int itemCount)
    {
        using var disposables = new CompositeDisposable();
        var results = new List<string>();
        
        // Create Observable chain similar to investment app patterns
        var observable = Observable.Range(1, itemCount)
            .Select(i => new { Id = i, Value = i * 1.5m, Name = $"Item_{i}" })
            .Where(item => item.Value > 5)
            .Select(item => $"{item.Name}: {item.Value:F2}")
            .Buffer(10) // Buffer for UI updates
            .SelectMany(batch => batch)
            .Take(itemCount);
        
        var subscription = observable
            .Subscribe(item => results.Add(item))
            .DisposeWith(disposables);
        
        // Wait for completion
        var timeout = TimeSpan.FromSeconds(30);
        var start = DateTime.UtcNow;
        
        while (results.Count < itemCount && DateTime.UtcNow - start < timeout)
        {
            await Task.Delay(1);
        }
    }
    
    [Benchmark]
    public async Task ObservableChain_WithDisposal_MemoryCleanup()
    {
        var results = new List<decimal>();
        
        for (int round = 0; round < 10; round++)
        {
            using var disposables = new CompositeDisposable();
            
            var observable = Observable.Range(1, 100)
                .Select(i => i * 2.5m)
                .Where(value => value > 10)
                .Take(50);
            
            var subscription = observable
                .Subscribe(value => results.Add(value))
                .DisposeWith(disposables);
            
            // Wait for completion
            await Task.Delay(10);
        }
    }
}

/// <summary>
/// Benchmarks for financial data processing performance
/// Simulates large investment portfolio calculations
/// </summary>
[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 5)]
public class DataProcessingBenchmarks
{
    [Benchmark]
    [Arguments(1000)]
    [Arguments(5000)]
    [Arguments(10000)]
    public void ProcessLargePortfolio(int transactionCount)
    {
        var transactions = GenerateTransactions(transactionCount);
        
        // Simulate complex financial calculations
        var processed = transactions
            .GroupBy(t => t.AccountId)
            .Select(group => new
            {
                AccountId = group.Key,
                TotalValue = group.Sum(t => t.Amount),
                TransactionCount = group.Count(),
                AverageValue = group.Average(t => t.Amount),
                DateRange = new
                {
                    Start = group.Min(t => t.Date),
                    End = group.Max(t => t.Date)
                },
                ProfitLoss = group.Sum(t => t.Amount) - group.Sum(t => t.CostBasis)
            })
            .OrderByDescending(account => account.TotalValue)
            .ToList();
    }
    
    [Benchmark]
    [Arguments(10000)]
    public void CurrencyConversion_Performance(int conversionCount)
    {
        var random = new Random(42);
        var currencies = new[] { "USD", "EUR", "GBP", "JPY", "CAD" };
        var exchangeRates = new Dictionary<string, decimal>
        {
            { "USD", 1.0m },
            { "EUR", 0.85m },
            { "GBP", 0.73m },
            { "JPY", 110.0m },
            { "CAD", 1.25m }
        };
        
        var conversions = new List<decimal>();
        
        for (int i = 0; i < conversionCount; i++)
        {
            var amount = (decimal)(random.NextDouble() * 10000);
            var fromCurrency = currencies[random.Next(currencies.Length)];
            var toCurrency = currencies[random.Next(currencies.Length)];
            
            // Simple conversion calculation
            var converted = amount * exchangeRates[fromCurrency] / exchangeRates[toCurrency];
            conversions.Add(converted);
        }
    }
    
    private List<Transaction> GenerateTransactions(int count)
    {
        var random = new Random(42);
        var transactions = new List<Transaction>();
        
        for (int i = 0; i < count; i++)
        {
            transactions.Add(new Transaction
            {
                Id = i,
                AccountId = random.Next(1, 11), // 10 accounts
                Amount = (decimal)(random.NextDouble() * 1000),
                CostBasis = (decimal)(random.NextDouble() * 800),
                Date = DateTime.Now.AddDays(-random.Next(365)),
                Type = random.Next(2) == 0 ? "Buy" : "Sell"
            });
        }
        
        return transactions;
    }
}

// Test data classes
public class InvestmentTestData
{
    public string AccountName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal InitialValue { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}

public class Transaction
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public decimal Amount { get; set; }
    public decimal CostBasis { get; set; }
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
}