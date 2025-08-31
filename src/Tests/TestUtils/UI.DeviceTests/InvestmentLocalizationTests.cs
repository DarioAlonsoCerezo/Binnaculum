using System.Globalization;

namespace Binnaculum.UI.DeviceTests;

/// <summary>
/// Multi-culture and localization tests for investment controls.
/// Tests Spanish/English localization and culture-specific number/currency formatting.
/// Phase 2.3: Investment-Specific UI Tests (Issue #151)
/// </summary>
public class InvestmentLocalizationTests
{
    #region Multi-Culture Currency Tests

    [Theory]
    [InlineData("en-US", 1234.56, "$1,234.56")]
    [InlineData("es-ES", 1234.56, "1.234,56 €")]
    [InlineData("en-GB", 1234.56, "£1,234.56")]
    [InlineData("fr-FR", 1234.56, "1 234,56 €")]
    public void CurrencyFormatting_DifferentCultures_FormatsCorrectly(string culture, decimal amount, string expectedFormat)
    {
        // Arrange
        var cultureInfo = CultureInfo.GetCultureInfo(culture);
        
        // Act
        var actualFormat = amount.ToString("C", cultureInfo);
        
        // Assert
        Assert.Equal(expectedFormat, actualFormat);
    }

    [Theory]
    [InlineData("en-US", 1234567.89, "1,234,567.89")]
    [InlineData("es-ES", 1234567.89, "1.234.567,89")]
    [InlineData("fr-FR", 1234567.89, "1 234 567,89")]
    [InlineData("de-DE", 1234567.89, "1.234.567,89")]
    public void NumberFormatting_DifferentCultures_UsesCorrectSeparators(string culture, decimal amount, string expectedFormat)
    {
        // Arrange
        var cultureInfo = CultureInfo.GetCultureInfo(culture);
        
        // Act
        var actualFormat = amount.ToString("N2", cultureInfo);
        
        // Assert
        Assert.Equal(expectedFormat, actualFormat);
    }

    #endregion

    #region Date Formatting Tests

    [Theory]
    [InlineData("en-US", "12/31/2024")]
    [InlineData("es-ES", "31/12/2024")]
    [InlineData("en-GB", "31/12/2024")]
    [InlineData("fr-FR", "31/12/2024")]
    public void DateFormatting_DifferentCultures_FormatsCorrectly(string culture, string expectedFormat)
    {
        // Arrange
        var date = new DateTime(2024, 12, 31);
        var cultureInfo = CultureInfo.GetCultureInfo(culture);
        
        // Act
        var actualFormat = date.ToString("d", cultureInfo);
        
        // Assert
        Assert.Equal(expectedFormat, actualFormat);
    }

    #endregion

    #region Decimal Precision Tests

    [Theory]
    [InlineData("en-US", 15.7, "15.7")]
    [InlineData("es-ES", 15.7, "15,7")]
    [InlineData("de-DE", 15.7, "15,7")]
    [InlineData("fr-FR", 15.7, "15,7")]
    public void DecimalFormatting_SingleDecimalPlace_UsesCorrectSeparator(string culture, decimal value, string expectedFormat)
    {
        // Arrange
        var cultureInfo = CultureInfo.GetCultureInfo(culture);
        
        // Act
        var actualFormat = value.ToString("0.#", cultureInfo);
        
        // Assert
        Assert.Equal(expectedFormat, actualFormat);
    }

    [Fact]
    public void PercentageControl_WithSpanishCulture_FormatsCorrectly()
    {
        // Arrange
        var mockPercentageControl = new MockPercentageControlWithCulture("es-ES");
        var percentage = 15.75m;
        
        // Act
        mockPercentageControl.Percentage = percentage;
        mockPercentageControl.UpdateDisplay();
        
        // Assert - Should still use invariant culture internally for consistency
        Assert.Equal("15", mockPercentageControl.PercentageValue);
        Assert.Equal(".75%", mockPercentageControl.PercentageDecimals);
    }

    [Fact]
    public void PercentageControl_WithGermanCulture_FormatsCorrectly()
    {
        // Arrange
        var mockPercentageControl = new MockPercentageControlWithCulture("de-DE");
        var percentage = 8.42m;
        
        // Act
        mockPercentageControl.Percentage = percentage;
        mockPercentageControl.UpdateDisplay();
        
        // Assert - Should still use invariant culture internally for consistency
        Assert.Equal("8", mockPercentageControl.PercentageValue);
        Assert.Equal(".42%", mockPercentageControl.PercentageDecimals);
    }

    #endregion

    #region RTL (Right-to-Left) Layout Tests

    [Fact]
    public void CurrencySymbol_InRTLLanguage_PositionsCorrectly()
    {
        // Arrange - Arabic culture (RTL)
        var amount = 1234.56m;
        var culture = "ar-SA"; // Arabic (Saudi Arabia)
        
        // Act
        var formatted = amount.ToString("C", CultureInfo.GetCultureInfo(culture));
        
        // Assert - Arabic currency format
        Assert.Contains("ر.س.", formatted); // Saudi Riyal symbol
        Assert.Contains("1,234.56", formatted);
    }

    [Fact]
    public void NumberFormatting_InRTLLanguage_UsesCorrectFormat()
    {
        // Arrange - Hebrew culture (RTL)
        var amount = 1234.56m;
        var culture = "he-IL"; // Hebrew (Israel)
        
        // Act
        var formatted = amount.ToString("C", CultureInfo.GetCultureInfo(culture));
        
        // Assert
        Assert.Contains("₪", formatted); // Israeli New Shekel symbol
        Assert.Contains("1,234.56", formatted);
    }

    #endregion

    #region Spanish/English Localization Tests

    [Fact]
    public void SpanishLocalization_PercentageDisplay_UsesSpanishFormatting()
    {
        // Arrange
        var culture = "es-ES";
        var percentage = -12.34m;
        
        // Act - Test percentage formatting in Spanish culture
        var formattedPercentage = percentage.ToString("P2", CultureInfo.GetCultureInfo(culture));
        
        // Assert
        Assert.Contains("-12,34", formattedPercentage); // Spanish uses comma for decimal separator
        Assert.Contains("%", formattedPercentage);
    }

    [Fact]
    public void EnglishLocalization_PercentageDisplay_UsesEnglishFormatting()
    {
        // Arrange
        var culture = "en-US";
        var percentage = -12.34m;
        
        // Act - Test percentage formatting in English culture
        var formattedPercentage = percentage.ToString("P2", CultureInfo.GetCultureInfo(culture));
        
        // Assert
        Assert.Contains("-12.34", formattedPercentage); // English uses period for decimal separator
        Assert.Contains("%", formattedPercentage);
    }

    #endregion

    #region Mock Classes for Culture Testing

    /// <summary>
    /// Mock PercentageControl with culture support for testing localization.
    /// </summary>
    public class MockPercentageControlWithCulture
    {
        private readonly CultureInfo _culture;
        
        public decimal Percentage { get; set; }
        public string PercentageValue { get; private set; } = string.Empty;
        public string PercentageDecimals { get; private set; } = string.Empty;
        public bool IsPositiveColor { get; private set; }
        public bool IsNegativeColor { get; private set; }
        public bool IsNeutralColor { get; private set; }

        public MockPercentageControlWithCulture(string cultureName)
        {
            _culture = CultureInfo.GetCultureInfo(cultureName);
        }

        public void UpdateDisplay()
        {
            // Check if the percentage is 0
            if (Percentage.Equals(0m))
            {
                PercentageValue = "0";
                PercentageDecimals = ".00%";
                IsNeutralColor = true;
                IsPositiveColor = false;
                IsNegativeColor = false;
                return;
            }

            // Format the percentage with invariant culture for consistency
            // (This matches the actual implementation which uses InvariantCulture)
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

            // Set the decimal part, ensuring 2 digits
            var decimalPart = parts[1];
            if (decimalPart.Length == 1)
                decimalPart += "0";
            else if (decimalPart.Length > 2)
                decimalPart = decimalPart.Substring(0, 2);
                
            PercentageDecimals = $".{decimalPart}%";
        }
    }

    #endregion
}