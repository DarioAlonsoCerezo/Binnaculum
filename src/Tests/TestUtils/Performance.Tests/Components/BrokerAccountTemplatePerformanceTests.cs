using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Binnaculum.Tests.TestUtils.Performance;

namespace Binnaculum.Tests.TestUtils.Performance.Components;

/// <summary>
/// Performance tests for BrokerAccountTemplate component rendering and data processing
/// Based on patterns from InvestmentPerformanceExamples.md and issue requirements
/// </summary>
[TestFixture]
public class BrokerAccountTemplatePerformanceTests : IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    
    [Test]
    [Description("Benchmark BrokerAccountTemplate rendering performance with different data sizes")]
    [TestCase(10, 50, "Small portfolio")]
    [TestCase(100, 200, "Medium portfolio")]
    [TestCase(1000, 1000, "Large portfolio - mobile stress test")]
    public async Task BrokerAccountTemplate_RenderingPerformance_WithMovements(
        int movementCount, long maxTimeMs, string description)
    {
        // Arrange
        var testData = InvestmentTestDataBuilder.CreatePortfolio(
            brokerAccounts: 3,
            transactionsPerAccount: movementCount / 3,
            currencies: new[] { "USD", "EUR", "GBP" },
            timeSpan: TimeSpan.FromDays(365)
        );
        
        // Act - Measure template rendering simulation
        var metrics = await PerformanceMeasurement.MeasureAsync(async () =>
        {
            // Simulate BrokerAccountTemplate data processing
            var processedAccounts = new List<BrokerAccountViewModel>();
            
            foreach (var account in testData)
            {
                var viewModel = new BrokerAccountViewModel
                {
                    AccountName = account.BrokerName,
                    Balance = account.CurrentBalance,
                    FormattedBalance = FormatCurrency(account.CurrentBalance, account.Currency),
                    ProfitLoss = account.CurrentBalance - account.InitialBalance,
                    ProfitLossPercentage = CalculatePercentage(account.CurrentBalance, account.InitialBalance),
                    LastUpdated = DateTime.Now,
                    HasMovements = account.Transactions.Any(),
                    MovementCount = account.Transactions.Count
                };
                
                // Simulate Observable chain for reactive updates
                var observable = Observable.Return(viewModel)
                    .Delay(TimeSpan.FromMilliseconds(1)) // Simulate UI update delay
                    .Do(vm => ProcessViewModel(vm));
                
                await observable.FirstAsync();
                processedAccounts.Add(viewModel);
            }
            
            return processedAccounts;
        }, $"BrokerAccountTemplate_Rendering_{description}");
        
        // Assert - Performance requirements for mobile devices
        PerformanceMeasurement.AssertPerformanceRequirements(
            metrics, 
            maxTimeMs, 
            50 * 1024 * 1024, // 50MB memory limit
            5); // Max 5 GC collections
        
        PerformanceMeasurement.LogMetrics(metrics);
        
        Console.WriteLine($"Rendered {movementCount} movements in {metrics.ElapsedMilliseconds}ms");
        Console.WriteLine($"Average time per item: {(double)metrics.ElapsedMilliseconds / movementCount:F3}ms");
    }
    
    [Test]
    [Description("Test BrokerAccountTemplate performance with real-time data updates")]
    public async Task BrokerAccountTemplate_RealTimeUpdates_ResponsiveUI()
    {
        const int updateCount = 100;
        const int maxUpdateTime = 16; // 60 FPS = 16.67ms per frame
        
        // Arrange
        var baseAccount = InvestmentTestDataBuilder.CreateBrokerAccount("TestBroker", 1000m, "USD");
        var updateTimes = new List<long>();
        
        // Act - Simulate rapid UI updates
        var overallMetrics = await PerformanceMeasurement.MeasureAsync(async () =>
        {
            for (int i = 0; i < updateCount; i++)
            {
                var updateMetrics = PerformanceMeasurement.MeasureSync(() =>
                {
                    // Simulate market data update
                    var updatedAccount = new BrokerAccountData
                    {
                        BrokerName = baseAccount.BrokerName,
                        CurrentBalance = baseAccount.CurrentBalance * (1 + (decimal)(Random.Shared.NextSingle() - 0.5f) * 0.02m), // ±1% variation
                        InitialBalance = baseAccount.InitialBalance,
                        Currency = baseAccount.Currency,
                        Transactions = baseAccount.Transactions
                    };
                    
                    // Simulate UI update
                    var viewModel = CreateViewModel(updatedAccount);
                    ProcessViewModel(viewModel);
                    
                    return viewModel;
                }, $"UIUpdate_{i}");
                
                updateTimes.Add(updateMetrics.ElapsedMilliseconds);
                
                // Simulate realistic update frequency (10 FPS)
                await Task.Delay(100);
            }
            
            return updateTimes;
        }, "RealTimeUpdates_Complete");
        
        // Assert - UI responsiveness requirements
        var averageUpdateTime = updateTimes.Average();
        var maxUpdateTimeActual = updateTimes.Max();
        
        Assert.That(averageUpdateTime, Is.LessThan(maxUpdateTime),
            $"Average UI update {averageUpdateTime:F2}ms exceeds 60 FPS requirement ({maxUpdateTime}ms)");
        
        Assert.That(maxUpdateTimeActual, Is.LessThan(maxUpdateTime * 2),
            $"Max UI update {maxUpdateTimeActual}ms exceeds acceptable threshold");
        
        Console.WriteLine($"UI Responsiveness Results:");
        Console.WriteLine($"  Average update time: {averageUpdateTime:F2}ms (Target: <{maxUpdateTime}ms)");
        Console.WriteLine($"  Max update time: {maxUpdateTimeActual}ms");
        Console.WriteLine($"  Updates completed: {updateTimes.Count}/{updateCount}");
        
        PerformanceMeasurement.LogMetrics(overallMetrics);
    }
    
    [Test]
    [Description("Test Observable chain performance and memory cleanup in BrokerAccountTemplate")]
    public async Task BrokerAccountTemplate_ObservableChains_NoMemoryLeaks()
    {
        // Test Observable memory leak detection
        var result = await MemoryLeakDetection.TestObservableMemoryLeak(disposables =>
        {
            var testData = InvestmentTestDataBuilder.CreatePortfolio(3, 10);
            
            var observable = Observable.Interval(TimeSpan.FromMilliseconds(10))
                .Take(1000)
                .Select(i => 
                {
                    var accountIndex = (int)(i % testData.Count);
                    var account = testData[accountIndex];
                    return CreateViewModel(account);
                })
                .Where(vm => vm.HasMovements)
                .Do(vm => ProcessViewModel(vm));
            
            disposables.Add(observable.Subscribe());
            return observable;
        }, iterations: 1000, testDuration: TimeSpan.FromSeconds(15));
        
        // Assert no memory leaks
        result.AssertNoMemoryLeak();
        
        Console.WriteLine($"Observable Chain Performance:");
        Console.WriteLine($"  Results processed: {result.ResultCount}");
        Console.WriteLine($"  Peak memory: {result.PeakMemoryMB:F1}MB");
        Console.WriteLine($"  Memory before dispose: {result.BeforeDisposeSnapshot.MemoryDeltaMB:F1}MB");
        Console.WriteLine($"  Memory after dispose: {result.AfterDisposeSnapshot.MemoryDeltaMB:F1}MB");
    }
    
    [Test]
    [Description("Test BrokerAccountTemplate performance under memory pressure")]
    public async Task BrokerAccountTemplate_MemoryPressure_Resilience()
    {
        // Test performance under memory pressure
        var result = await MemoryLeakDetection.TestObservableUnderMemoryPressure(disposables =>
        {
            var testData = InvestmentTestDataBuilder.CreatePortfolio(5, 20);
            
            var observable = Observable.Timer(TimeSpan.Zero, TimeSpan.FromMilliseconds(50))
                .Take(100)
                .Select(i =>
                {
                    var accountIndex = (int)(i % testData.Count);
                    var account = testData[accountIndex];
                    
                    // Simulate complex processing
                    return CreateViewModelWithComplexCalculations(account);
                });
            
            disposables.Add(observable.Subscribe());
            return observable;
        }, memoryPressureMB: 75); // 75MB memory pressure
        
        // Assert handled memory pressure well
        result.AssertHandledMemoryPressure();
        
        Console.WriteLine($"Memory Pressure Test Results:");
        Console.WriteLine($"  Peak memory: {result.PeakMemoryMB:F1}MB");
        Console.WriteLine($"  Results processed: {result.ResultCount}");
        Console.WriteLine($"  Memory cleaned up successfully");
    }
    
    private BrokerAccountViewModel CreateViewModel(BrokerAccountData account)
    {
        return new BrokerAccountViewModel
        {
            AccountName = account.BrokerName,
            Balance = account.CurrentBalance,
            FormattedBalance = FormatCurrency(account.CurrentBalance, account.Currency),
            ProfitLoss = account.CurrentBalance - account.InitialBalance,
            ProfitLossPercentage = CalculatePercentage(account.CurrentBalance, account.InitialBalance),
            LastUpdated = DateTime.Now,
            HasMovements = account.Transactions.Any(),
            MovementCount = account.Transactions.Count
        };
    }
    
    private BrokerAccountViewModel CreateViewModelWithComplexCalculations(BrokerAccountData account)
    {
        var vm = CreateViewModel(account);
        
        // Add complex calculations to simulate real app processing
        vm.DailyProfitLoss = account.Transactions
            .Where(t => t.Date >= DateTime.Today.AddDays(-1))
            .Sum(t => t.Amount);
            
        vm.MonthlyProfitLoss = account.Transactions
            .Where(t => t.Date >= DateTime.Today.AddDays(-30))
            .Sum(t => t.Amount);
            
        vm.YearlyProfitLoss = account.Transactions
            .Where(t => t.Date >= DateTime.Today.AddDays(-365))
            .Sum(t => t.Amount);
        
        return vm;
    }
    
    private void ProcessViewModel(BrokerAccountViewModel vm)
    {
        // Simulate UI processing
        var processed = vm.FormattedBalance.Length + vm.AccountName.Length;
    }
    
    private string FormatCurrency(decimal amount, string currency) => $"{currency} {amount:F2}";
    
    private decimal CalculatePercentage(decimal current, decimal initial) =>
        initial != 0 ? (current - initial) / initial * 100m : 0m;
    
    public void Dispose()
    {
        _disposables?.Dispose();
    }
}

// Test data and view model classes for simulation
public class BrokerAccountData
{
    public string BrokerName { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public decimal InitialBalance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public List<TransactionData> Transactions { get; set; } = new();
}

public class TransactionData
{
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class BrokerAccountViewModel
{
    public string AccountName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string FormattedBalance { get; set; } = string.Empty;
    public decimal ProfitLoss { get; set; }
    public decimal ProfitLossPercentage { get; set; }
    public DateTime LastUpdated { get; set; }
    public bool HasMovements { get; set; }
    public int MovementCount { get; set; }
    public decimal DailyProfitLoss { get; set; }
    public decimal MonthlyProfitLoss { get; set; }
    public decimal YearlyProfitLoss { get; set; }
}

public static class InvestmentTestDataBuilder
{
    public static List<BrokerAccountData> CreatePortfolio(
        int brokerAccounts,
        int transactionsPerAccount,
        string[] currencies = null,
        TimeSpan? timeSpan = null)
    {
        currencies ??= new[] { "USD" };
        var span = timeSpan ?? TimeSpan.FromDays(365);
        var random = new Random(42);
        var portfolio = new List<BrokerAccountData>();
        
        for (int i = 0; i < brokerAccounts; i++)
        {
            var currency = currencies[i % currencies.Length];
            var initialBalance = 1000m + (decimal)(random.NextDouble() * 5000);
            
            var account = new BrokerAccountData
            {
                BrokerName = $"Broker_{i + 1}",
                InitialBalance = initialBalance,
                CurrentBalance = initialBalance,
                Currency = currency,
                Transactions = new List<TransactionData>()
            };
            
            // Generate transactions
            for (int j = 0; j < transactionsPerAccount; j++)
            {
                var transaction = new TransactionData
                {
                    Amount = (decimal)((random.NextDouble() - 0.5) * 500), // -250 to +250
                    Date = DateTime.Now.AddDays(-random.Next((int)span.TotalDays)),
                    Type = random.Next(2) == 0 ? "Buy" : "Sell"
                };
                
                account.Transactions.Add(transaction);
                account.CurrentBalance += transaction.Amount;
            }
            
            portfolio.Add(account);
        }
        
        return portfolio;
    }
    
    public static BrokerAccountData CreateBrokerAccount(string name, decimal balance, string currency)
    {
        return new BrokerAccountData
        {
            BrokerName = name,
            InitialBalance = balance,
            CurrentBalance = balance,
            Currency = currency,
            Transactions = new List<TransactionData>()
        };
    }
}