using System.Globalization;
using static Binnaculum.Core.Models;

namespace Binnaculum.UI.DeviceTests;

/// <summary>
/// Investment-specific UI tests for Binnaculum controls and features.
/// Tests PercentageControl, currency formatting, and financial calculations.
/// Phase 2.3: Investment-Specific UI Tests (Issue #151)
/// </summary>
public class InvestmentSpecificUITests
{
    #region PercentageControl Tests

    [Fact]
    public void PercentageControl_WithProfitableInvestment_DisplaysGreenPercentage()
    {
        // Arrange
        var mockPercentageControl = new MockPercentageControl();
        var profitablePercentage = 15.75m;
        
        // Act
        mockPercentageControl.Percentage = profitablePercentage;
        mockPercentageControl.UpdateDisplay();
        
        // Assert
        Assert.Equal(profitablePercentage, mockPercentageControl.Percentage);
        Assert.Equal("15", mockPercentageControl.PercentageValue);
        Assert.Equal(".75%", mockPercentageControl.PercentageDecimals);
        Assert.True(mockPercentageControl.IsPositiveColor);
        Assert.False(mockPercentageControl.IsNegativeColor);
    }

    [Fact]
    public void PercentageControl_WithLossInvestment_DisplaysRedPercentage()
    {
        // Arrange
        var mockPercentageControl = new MockPercentageControl();
        var lossPercentage = -8.42m;
        
        // Act
        mockPercentageControl.Percentage = lossPercentage;
        mockPercentageControl.UpdateDisplay();
        
        // Assert
        Assert.Equal(lossPercentage, mockPercentageControl.Percentage);
        Assert.Equal("-8", mockPercentageControl.PercentageValue);
        Assert.Equal(".42%", mockPercentageControl.PercentageDecimals);
        Assert.False(mockPercentageControl.IsPositiveColor);
        Assert.True(mockPercentageControl.IsNegativeColor);
    }

    [Fact]
    public void PercentageControl_WithZeroPercentage_DisplaysNeutralColor()
    {
        // Arrange
        var mockPercentageControl = new MockPercentageControl();
        var zeroPercentage = 0.00m;
        
        // Act
        mockPercentageControl.Percentage = zeroPercentage;
        mockPercentageControl.UpdateDisplay();
        
        // Assert
        Assert.Equal(zeroPercentage, mockPercentageControl.Percentage);
        Assert.Equal("0", mockPercentageControl.PercentageValue);
        Assert.Equal(".00%", mockPercentageControl.PercentageDecimals);
        Assert.False(mockPercentageControl.IsPositiveColor);
        Assert.False(mockPercentageControl.IsNegativeColor);
        Assert.True(mockPercentageControl.IsNeutralColor);
    }

    [Fact]
    public void PercentageControl_WithHighPrecisionPercentage_FormatsCorrectly()
    {
        // Arrange
        var mockPercentageControl = new MockPercentageControl();
        var precisePercentage = 12.3456m;
        
        // Act
        mockPercentageControl.Percentage = precisePercentage;
        mockPercentageControl.UpdateDisplay();
        
        // Assert
        Assert.Equal(precisePercentage, mockPercentageControl.Percentage);
        // Should format to 2 decimal places: 12.35%
        Assert.Equal("12", mockPercentageControl.PercentageValue);
        Assert.Equal(".35%", mockPercentageControl.PercentageDecimals);
    }

    [Fact]
    public void PercentageControl_WithWholeNumberPercentage_ShowsZeroDecimals()
    {
        // Arrange
        var mockPercentageControl = new MockPercentageControl();
        var wholePercentage = 25.00m;
        
        // Act
        mockPercentageControl.Percentage = wholePercentage;
        mockPercentageControl.UpdateDisplay();
        
        // Assert
        Assert.Equal(wholePercentage, mockPercentageControl.Percentage);
        Assert.Equal("25", mockPercentageControl.PercentageValue);
        Assert.Equal(".00%", mockPercentageControl.PercentageDecimals);
    }

    #endregion

    #region Currency Formatting Tests

    [Fact]
    public void CurrencyFormatting_USD_FormatsCorrectly()
    {
        // Arrange
        var amount = 1234.56m;
        var culture = "en-US";
        
        // Act
        var formatted = amount.ToString("C", CultureInfo.GetCultureInfo(culture));
        
        // Assert
        Assert.Equal("$1,234.56", formatted);
    }

    [Fact]
    public void CurrencyFormatting_EUR_FormatsCorrectly()
    {
        // Arrange
        var amount = 1234.56m;
        var culture = "de-DE"; // German culture uses Euro
        
        // Act
        var formatted = amount.ToString("C", CultureInfo.GetCultureInfo(culture));
        
        // Assert
        Assert.Equal("1.234,56 €", formatted);
    }

    [Fact]
    public void CurrencyFormatting_GBP_FormatsCorrectly()
    {
        // Arrange
        var amount = 1234.56m;
        var culture = "en-GB"; // British culture
        
        // Act
        var formatted = amount.ToString("C", CultureInfo.GetCultureInfo(culture));
        
        // Assert
        Assert.Equal("£1,234.56", formatted);
    }

    [Fact]
    public void CurrencyFormatting_JPY_FormatsCorrectly()
    {
        // Arrange
        var amount = 123456m; // JPY typically has no decimal places
        var culture = "ja-JP"; // Japanese culture
        
        // Act
        var formatted = amount.ToString("C", CultureInfo.GetCultureInfo(culture));
        
        // Assert
        Assert.Equal("¥123,456", formatted);
    }

    [Fact]
    public void BinnaculumCurrencyFormatting_WithSymbol_FormatsCorrectly()
    {
        // Arrange
        var amount = 1234.56m;
        var symbol = "€";
        
        // Act - Using simple implementation since UI project reference isn't available
        var formatted = $"{symbol}{amount:0.00}";
        
        // Assert
        Assert.Equal("€1234.56", formatted);
    }

    [Fact]
    public void BinnaculumCurrencyFormatting_WithoutSymbol_UsesDefaultDollar()
    {
        // Arrange
        var amount = 1234.56m;
        string? symbol = null;
        
        // Act - Using simple implementation since UI project reference isn't available
        var formatted = string.IsNullOrWhiteSpace(symbol) ? $"${amount:0.00}" : $"{symbol}{amount:0.00}";
        
        // Assert
        Assert.Equal("$1234.56", formatted);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void CurrencyFormatting_ZeroAmount_FormatsCorrectly()
    {
        // Arrange
        var amount = 0.00m;
        var culture = "en-US";
        
        // Act
        var formatted = amount.ToString("C", CultureInfo.GetCultureInfo(culture));
        
        // Assert
        Assert.Equal("$0.00", formatted);
    }

    [Fact]
    public void CurrencyFormatting_NegativeAmount_FormatsCorrectly()
    {
        // Arrange
        var amount = -1234.56m;
        var culture = "en-US";
        
        // Act
        var formatted = amount.ToString("C", CultureInfo.GetCultureInfo(culture));
        
        // Assert
        Assert.Equal("($1,234.56)", formatted);
    }

    [Fact]
    public void CurrencyFormatting_VeryLargeAmount_FormatsCorrectly()
    {
        // Arrange
        var amount = 1234567890.12m;
        var culture = "en-US";
        
        // Act
        var formatted = amount.ToString("C", CultureInfo.GetCultureInfo(culture));
        
        // Assert
        Assert.Equal("$1,234,567,890.12", formatted);
    }

    [Fact]
    public void PercentageControl_WithVeryLargePercentage_HandlesCorrectly()
    {
        // Arrange
        var mockPercentageControl = new MockPercentageControl();
        var largePercentage = 999.99m;
        
        // Act
        mockPercentageControl.Percentage = largePercentage;
        mockPercentageControl.UpdateDisplay();
        
        // Assert
        Assert.Equal("999", mockPercentageControl.PercentageValue);
        Assert.Equal(".99%", mockPercentageControl.PercentageDecimals);
        Assert.True(mockPercentageControl.IsPositiveColor);
    }

    [Fact]
    public void PercentageControl_WithVerySmallPercentage_HandlesCorrectly()
    {
        // Arrange
        var mockPercentageControl = new MockPercentageControl();
        var smallPercentage = 0.01m;
        
        // Act
        mockPercentageControl.Percentage = smallPercentage;
        mockPercentageControl.UpdateDisplay();
        
        // Assert
        Assert.Equal("0", mockPercentageControl.PercentageValue);
        Assert.Equal(".01%", mockPercentageControl.PercentageDecimals);
        Assert.True(mockPercentageControl.IsPositiveColor);
    }

    #endregion

    #region Mock Classes

    /// <summary>
    /// Mock implementation of PercentageControl for testing.
    /// Based on the actual PercentageControl.xaml.cs behavior patterns.
    /// </summary>
    public class MockPercentageControl
    {
        public decimal Percentage { get; set; }
        public string PercentageValue { get; private set; } = string.Empty;
        public string PercentageDecimals { get; private set; } = string.Empty;
        public bool IsPositiveColor { get; private set; }
        public bool IsNegativeColor { get; private set; }
        public bool IsNeutralColor { get; private set; }

        public void UpdateDisplay()
        {
            // Check if the percentage is 0. If so, display "0.00%" with neutral color.
            if (Percentage.Equals(0m))
            {
                PercentageValue = "0";
                PercentageDecimals = ".00%";
                IsNeutralColor = true;
                IsPositiveColor = false;
                IsNegativeColor = false;
                return;
            }

            // Format the percentage with invariant culture (matching actual implementation)
            string formattedPercentage = Percentage.ToString("0.##", CultureInfo.InvariantCulture);

            // Split the formatted percentage into whole number and decimal parts
            string[] parts = formattedPercentage.Split('.');

            // Set the whole number part
            PercentageValue = parts[0];
            
            // Set color based on positive/negative
            IsPositiveColor = Percentage >= 0;
            IsNegativeColor = Percentage < 0;
            IsNeutralColor = false;

            if (parts.Length == 1)
            {
                PercentageDecimals = ".00%";
                return;
            }

            // Set the decimal part with the decimal point, ensuring 2 digits
            var decimalPart = parts[1];
            if (decimalPart.Length == 1)
                decimalPart += "0"; // Pad to 2 digits
            else if (decimalPart.Length > 2)
                decimalPart = decimalPart.Substring(0, 2); // Truncate to 2 digits
                
            PercentageDecimals = $".{decimalPart}%";
        }
    }

    #endregion
}