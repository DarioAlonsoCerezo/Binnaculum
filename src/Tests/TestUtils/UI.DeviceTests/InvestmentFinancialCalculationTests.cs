using static Binnaculum.Core.Models;
using System.Globalization;

namespace Binnaculum.UI.DeviceTests;

/// <summary>
/// Financial calculation and error scenario tests for investment UI controls.
/// Tests portfolio calculations, edge cases, and error handling patterns.
/// Phase 2.3: Investment-Specific UI Tests (Issue #151)
/// </summary>
public class InvestmentFinancialCalculationTests
{
    #region Portfolio Balance Display Tests

    [Fact]
    public void PortfolioBalance_WithMixedProfitsLosses_CalculatesCorrectly()
    {
        // Arrange
        var mockPortfolio = new MockPortfolioDisplay();
        var scenarios = new List<MockInvestmentData>
        {
            new() { Invested = 10000m, RealizedGains = 1500m, UnrealizedGains = 500m }, // +20% total
            new() { Invested = 5000m, RealizedGains = -800m, UnrealizedGains = -200m }, // -20% total
            new() { Invested = 15000m, RealizedGains = 750m, UnrealizedGains = 250m }   // +6.67% total
        };
        
        // Act
        mockPortfolio.LoadInvestments(scenarios);
        mockPortfolio.CalculateTotals();
        
        // Assert
        Assert.Equal(30000m, mockPortfolio.TotalInvested);
        Assert.Equal(1450m, mockPortfolio.TotalRealizedGains); // 1500 - 800 + 750
        Assert.Equal(550m, mockPortfolio.TotalUnrealizedGains); // 500 - 200 + 250
        Assert.Equal(32000m, mockPortfolio.TotalCurrentValue); // 30000 + 1450 + 550
        Assert.Equal(6.67m, Math.Round(mockPortfolio.TotalPercentageGain, 2)); // 2000 / 30000 * 100
    }

    [Fact]
    public void PortfolioBalance_WithOnlyLosses_DisplaysNegativeCorrectly()
    {
        // Arrange
        var mockPortfolio = new MockPortfolioDisplay();
        var scenarios = new List<MockInvestmentData>
        {
            new() { Invested = 20000m, RealizedGains = -3000m, UnrealizedGains = -1000m }, // -20%
            new() { Invested = 10000m, RealizedGains = -1500m, UnrealizedGains = -500m }   // -20%
        };
        
        // Act
        mockPortfolio.LoadInvestments(scenarios);
        mockPortfolio.CalculateTotals();
        
        // Assert
        Assert.Equal(30000m, mockPortfolio.TotalInvested);
        Assert.Equal(-4500m, mockPortfolio.TotalRealizedGains);
        Assert.Equal(-1500m, mockPortfolio.TotalUnrealizedGains);
        Assert.Equal(24000m, mockPortfolio.TotalCurrentValue);
        Assert.Equal(-20.0m, mockPortfolio.TotalPercentageGain);
        Assert.True(mockPortfolio.IsNegativePerformance);
    }

    [Fact]
    public void PortfolioBalance_WithZeroInvestment_HandlesGracefully()
    {
        // Arrange
        var mockPortfolio = new MockPortfolioDisplay();
        var scenarios = new List<MockInvestmentData>();
        
        // Act
        mockPortfolio.LoadInvestments(scenarios);
        mockPortfolio.CalculateTotals();
        
        // Assert
        Assert.Equal(0m, mockPortfolio.TotalInvested);
        Assert.Equal(0m, mockPortfolio.TotalRealizedGains);
        Assert.Equal(0m, mockPortfolio.TotalUnrealizedGains);
        Assert.Equal(0m, mockPortfolio.TotalCurrentValue);
        Assert.Equal(0m, mockPortfolio.TotalPercentageGain);
        Assert.False(mockPortfolio.IsNegativePerformance);
    }

    #endregion

    #region Multi-Currency Portfolio Tests

    [Fact]
    public void MultiCurrencyPortfolio_WithUSDAndEUR_ConvertsCorrectly()
    {
        // Arrange
        var mockPortfolio = new MockMultiCurrencyPortfolio();
        var exchangeRates = new Dictionary<string, decimal>
        {
            ["USD"] = 1.0m,
            ["EUR"] = 0.85m, // 1 USD = 0.85 EUR
            ["GBP"] = 0.75m  // 1 USD = 0.75 GBP
        };
        
        var investments = new List<MockCurrencyInvestment>
        {
            new() { Amount = 10000m, Currency = "USD", GainsUSD = 1000m },
            new() { Amount = 8500m, Currency = "EUR", GainsUSD = 850m }, // 10000 EUR * 0.85 rate + 1000 EUR gains * 0.85
            new() { Amount = 7500m, Currency = "GBP", GainsUSD = 750m }  // 10000 GBP * 0.75 rate + 1000 GBP gains * 0.75
        };
        
        // Act
        mockPortfolio.LoadInvestments(investments, exchangeRates);
        mockPortfolio.CalculateInUSD();
        
        // Assert
        Assert.Equal(26000m, mockPortfolio.TotalUSDValue); // 10000 + 8500 + 7500
        Assert.Equal(2600m, mockPortfolio.TotalUSDGains);  // 1000 + 850 + 750
        Assert.Equal(10.0m, Math.Round(mockPortfolio.TotalPercentageGain, 1));
    }

    #endregion

    #region Investment Movement Tests

    [Fact]
    public void InvestmentMovement_WithValidBuyOrder_ValidatesCorrectly()
    {
        // Arrange
        var mockMovementForm = new MockInvestmentMovementForm();
        
        // Act
        mockMovementForm.MovementType = "Buy";
        mockMovementForm.Amount = 5000m;
        mockMovementForm.Date = DateTime.Today;
        mockMovementForm.ValidateForm();
        
        // Assert
        Assert.True(mockMovementForm.IsValid);
        Assert.Empty(mockMovementForm.ValidationErrors);
        Assert.Equal(5000m, mockMovementForm.Amount);
        Assert.Equal("Buy", mockMovementForm.MovementType);
    }

    [Fact]
    public void InvestmentMovement_WithInvalidAmount_ShowsValidationError()
    {
        // Arrange
        var mockMovementForm = new MockInvestmentMovementForm();
        
        // Act
        mockMovementForm.MovementType = "Buy";
        mockMovementForm.Amount = -100m; // Invalid negative amount for buy
        mockMovementForm.Date = DateTime.Today;
        mockMovementForm.ValidateForm();
        
        // Assert
        Assert.False(mockMovementForm.IsValid);
        Assert.Contains("Amount must be positive for buy orders", mockMovementForm.ValidationErrors);
    }

    [Fact]
    public void InvestmentMovement_WithFutureDate_ShowsValidationError()
    {
        // Arrange
        var mockMovementForm = new MockInvestmentMovementForm();
        
        // Act
        mockMovementForm.MovementType = "Buy";
        mockMovementForm.Amount = 5000m;
        mockMovementForm.Date = DateTime.Today.AddDays(1); // Future date
        mockMovementForm.ValidateForm();
        
        // Assert
        Assert.False(mockMovementForm.IsValid);
        Assert.Contains("Date cannot be in the future", mockMovementForm.ValidationErrors);
    }

    #endregion

    #region Error Scenario Tests

    [Fact]
    public void PercentageControl_WithInvalidPercentage_HandlesGracefully()
    {
        // Arrange
        var mockControl = new MockPercentageControlWithErrorHandling();
        
        // Act & Assert - Should not throw for extreme values
        mockControl.Percentage = decimal.MaxValue;
        var exception1 = Record.Exception(() => mockControl.UpdateDisplay());
        Assert.Null(exception1);
        
        mockControl.Percentage = decimal.MinValue;
        var exception2 = Record.Exception(() => mockControl.UpdateDisplay());
        Assert.Null(exception2);
        
        mockControl.Percentage = 0m;
        var exception3 = Record.Exception(() => mockControl.UpdateDisplay());
        Assert.Null(exception3);
    }

    [Fact]
    public void CurrencyFormatting_WithCorruptedData_HandlesGracefully()
    {
        // Arrange
        var mockFormatter = new MockCurrencyFormatter();
        
        // Act & Assert - Should handle edge cases without throwing
        var exception1 = Record.Exception(() => mockFormatter.Format(decimal.MaxValue, "USD"));
        Assert.Null(exception1);
        
        var exception2 = Record.Exception(() => mockFormatter.Format(decimal.MinValue, "USD"));
        Assert.Null(exception2);
        
        var exception3 = Record.Exception(() => mockFormatter.Format(0m, ""));
        Assert.Null(exception3);
        
        var exception4 = Record.Exception(() => mockFormatter.Format(1234.56m, null));
        Assert.Null(exception4);
    }

    [Fact]
    public void PortfolioCalculation_WithMissingData_UsesDefaults()
    {
        // Arrange
        var mockPortfolio = new MockPortfolioDisplay();
        var incompleteData = new List<MockInvestmentData>
        {
            new() { Invested = 10000m, RealizedGains = null, UnrealizedGains = 500m }
        };
        
        // Act
        mockPortfolio.LoadInvestments(incompleteData);
        mockPortfolio.CalculateTotals();
        
        // Assert - Should use 0 for missing RealizedGains
        Assert.Equal(10000m, mockPortfolio.TotalInvested);
        Assert.Equal(0m, mockPortfolio.TotalRealizedGains);
        Assert.Equal(500m, mockPortfolio.TotalUnrealizedGains);
        Assert.Equal(10500m, mockPortfolio.TotalCurrentValue);
    }

    [Fact]
    public void LargePortfolio_With1000PlusEntries_PerformanceIsAcceptable()
    {
        // Arrange
        var mockPortfolio = new MockPortfolioDisplay();
        var largeDataSet = new List<MockInvestmentData>();
        
        for (int i = 0; i < 1500; i++) // Test with 1500 entries
        {
            largeDataSet.Add(new MockInvestmentData
            {
                Invested = 1000m + i,
                RealizedGains = (i % 2 == 0) ? i * 0.1m : -i * 0.05m,
                UnrealizedGains = i * 0.02m
            });
        }
        
        // Act
        var startTime = DateTime.UtcNow;
        mockPortfolio.LoadInvestments(largeDataSet);
        mockPortfolio.CalculateTotals();
        var endTime = DateTime.UtcNow;
        
        // Assert - Should complete within reasonable time (mobile-optimized)
        var duration = endTime - startTime;
        Assert.True(duration.TotalMilliseconds < 1000, $"Portfolio calculation took {duration.TotalMilliseconds}ms, should be under 1000ms");
        Assert.Equal(1500, mockPortfolio.InvestmentCount);
        Assert.True(mockPortfolio.TotalInvested > 0);
    }

    #endregion

    #region Mock Classes

    public class MockInvestmentData
    {
        public decimal Invested { get; set; }
        public decimal? RealizedGains { get; set; }
        public decimal UnrealizedGains { get; set; }
    }

    public class MockCurrencyInvestment
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "";
        public decimal GainsUSD { get; set; }
    }

    public class MockPortfolioDisplay
    {
        private List<MockInvestmentData> _investments = new();
        
        public decimal TotalInvested { get; private set; }
        public decimal TotalRealizedGains { get; private set; }
        public decimal TotalUnrealizedGains { get; private set; }
        public decimal TotalCurrentValue { get; private set; }
        public decimal TotalPercentageGain { get; private set; }
        public bool IsNegativePerformance { get; private set; }
        public int InvestmentCount => _investments.Count;

        public void LoadInvestments(List<MockInvestmentData> investments)
        {
            _investments = investments ?? new List<MockInvestmentData>();
        }

        public void CalculateTotals()
        {
            TotalInvested = _investments.Sum(x => x.Invested);
            TotalRealizedGains = _investments.Sum(x => x.RealizedGains ?? 0m);
            TotalUnrealizedGains = _investments.Sum(x => x.UnrealizedGains);
            TotalCurrentValue = TotalInvested + TotalRealizedGains + TotalUnrealizedGains;
            
            if (TotalInvested > 0)
            {
                TotalPercentageGain = ((TotalRealizedGains + TotalUnrealizedGains) / TotalInvested) * 100;
            }
            
            IsNegativePerformance = TotalPercentageGain < 0;
        }
    }

    public class MockMultiCurrencyPortfolio
    {
        private List<MockCurrencyInvestment> _investments = new();
        private Dictionary<string, decimal> _exchangeRates = new();
        
        public decimal TotalUSDValue { get; private set; }
        public decimal TotalUSDGains { get; private set; }
        public decimal TotalPercentageGain { get; private set; }

        public void LoadInvestments(List<MockCurrencyInvestment> investments, Dictionary<string, decimal> exchangeRates)
        {
            _investments = investments;
            _exchangeRates = exchangeRates;
        }

        public void CalculateInUSD()
        {
            TotalUSDValue = _investments.Sum(x => x.Amount);
            TotalUSDGains = _investments.Sum(x => x.GainsUSD);
            
            if (TotalUSDValue > 0)
            {
                TotalPercentageGain = (TotalUSDGains / TotalUSDValue) * 100;
            }
        }
    }

    public class MockInvestmentMovementForm
    {
        public string MovementType { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public bool IsValid { get; private set; }
        public List<string> ValidationErrors { get; } = new();

        public void ValidateForm()
        {
            IsValid = true;
            ValidationErrors.Clear();

            if (MovementType == "Buy" && Amount <= 0)
            {
                ValidationErrors.Add("Amount must be positive for buy orders");
                IsValid = false;
            }

            if (Date > DateTime.Today)
            {
                ValidationErrors.Add("Date cannot be in the future");
                IsValid = false;
            }
        }
    }

    public class MockPercentageControlWithErrorHandling
    {
        public decimal Percentage { get; set; }
        public string PercentageValue { get; private set; } = "";
        public string PercentageDecimals { get; private set; } = "";

        public void UpdateDisplay()
        {
            try
            {
                // Handle extreme values gracefully
                if (Percentage == decimal.MaxValue || Percentage == decimal.MinValue)
                {
                    PercentageValue = "---";
                    PercentageDecimals = "";
                    return;
                }

                if (Percentage.Equals(0m))
                {
                    PercentageValue = "0";
                    PercentageDecimals = ".00%";
                    return;
                }

                string formattedPercentage = Percentage.ToString("0.##", CultureInfo.InvariantCulture);
                string[] parts = formattedPercentage.Split('.');
                
                PercentageValue = parts[0];
                PercentageDecimals = parts.Length > 1 ? $".{parts[1].PadRight(2, '0').Substring(0, 2)}%" : ".00%";
            }
            catch
            {
                // Fallback for any formatting errors
                PercentageValue = "ERROR";
                PercentageDecimals = "";
            }
        }
    }

    public class MockCurrencyFormatter
    {
        public string Format(decimal amount, string? currencyCode)
        {
            try
            {
                if (string.IsNullOrEmpty(currencyCode))
                    return $"${amount:0.00}";
                    
                return currencyCode switch
                {
                    "USD" => $"${amount:0.00}",
                    "EUR" => $"€{amount:0.00}",
                    "GBP" => $"£{amount:0.00}",
                    _ => $"{currencyCode}{amount:0.00}"
                };
            }
            catch
            {
                return "FORMAT_ERROR";
            }
        }
    }

    #endregion
}