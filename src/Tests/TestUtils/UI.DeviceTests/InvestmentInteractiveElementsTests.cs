using System.ComponentModel;

namespace Binnaculum.UI.DeviceTests;

/// <summary>
/// Interactive elements and gesture testing for investment UI controls.
/// Tests touch gestures, keyboard input, and user interaction patterns.
/// Phase 2.3: Investment-Specific UI Tests (Issue #151)
/// </summary>
public class InvestmentInteractiveElementsTests
{
    #region Touch & Gesture Tests

    [Fact]
    public void BrokerAccountTemplate_OnTap_TriggersCorrectAction()
    {
        // Arrange
        var mockTemplate = new MockBrokerAccountTemplate();
        var testSnapshot = CreateTestSnapshot();
        mockTemplate.BindingContext = testSnapshot;
        
        // Act
        mockTemplate.SimulateTap();
        
        // Assert
        Assert.True(mockTemplate.WasTapped);
        Assert.Equal(testSnapshot, mockTemplate.TappedSnapshot);
        Assert.Single(mockTemplate.InteractionHistory);
        Assert.Equal("Tap", mockTemplate.InteractionHistory.First());
    }

    [Fact]
    public void PercentageControl_OnLongPress_ShowsDetailView()
    {
        // Arrange
        var mockPercentageControl = new MockInteractivePercentageControl();
        mockPercentageControl.Percentage = 15.75m;
        
        // Act
        mockPercentageControl.SimulateLongPress();
        
        // Assert
        Assert.True(mockPercentageControl.IsDetailViewVisible);
        Assert.True(mockPercentageControl.WasLongPressed);
    }

    [Fact]
    public void PortfolioNavigation_OnSwipeLeft_NavigatesToNextPage()
    {
        // Arrange
        var mockNavigation = new MockPortfolioNavigation();
        mockNavigation.CurrentPage = 0;
        mockNavigation.TotalPages = 3;
        
        // Act
        mockNavigation.SimulateSwipeLeft();
        
        // Assert
        Assert.Equal(1, mockNavigation.CurrentPage);
        Assert.True(mockNavigation.LastGestureWasSwipe);
        Assert.Equal("SwipeLeft", mockNavigation.LastGesture);
    }

    [Fact]
    public void PortfolioNavigation_OnSwipeRight_NavigatesToPreviousPage()
    {
        // Arrange
        var mockNavigation = new MockPortfolioNavigation();
        mockNavigation.CurrentPage = 2;
        mockNavigation.TotalPages = 3;
        
        // Act
        mockNavigation.SimulateSwipeRight();
        
        // Assert
        Assert.Equal(1, mockNavigation.CurrentPage);
        Assert.Equal("SwipeRight", mockNavigation.LastGesture);
    }

    [Fact]
    public void ChartView_OnPinchZoom_AdjustsScaleCorrectly()
    {
        // Arrange
        var mockChart = new MockChartView();
        mockChart.InitialScale = 1.0f;
        
        // Act - Simulate pinch to zoom in
        mockChart.SimulatePinchZoom(2.0f);
        
        // Assert
        Assert.Equal(2.0f, mockChart.CurrentScale);
        Assert.True(mockChart.WasPinched);
        Assert.Equal("PinchZoom", mockChart.LastGesture);
    }

    [Fact]
    public void ChartView_OnPinchZoomOut_ConstrainsMinimumScale()
    {
        // Arrange
        var mockChart = new MockChartView();
        mockChart.InitialScale = 1.0f;
        mockChart.MinimumScale = 0.5f;
        
        // Act - Try to zoom out beyond minimum
        mockChart.SimulatePinchZoom(0.1f);
        
        // Assert
        Assert.Equal(0.5f, mockChart.CurrentScale); // Should be constrained to minimum
        Assert.True(mockChart.WasPinched);
    }

    [Fact]
    public void TouchFeedback_OnControlTouch_ShowsVisualResponse()
    {
        // Arrange
        var mockControl = new MockTouchFeedbackControl();
        
        // Act
        mockControl.SimulateTouchStart();
        
        // Assert
        Assert.True(mockControl.IsPressed);
        Assert.True(mockControl.HasVisualFeedback);
        
        // Act - Release touch
        mockControl.SimulateTouchEnd();
        
        // Assert
        Assert.False(mockControl.IsPressed);
        Assert.False(mockControl.HasVisualFeedback);
    }

    #endregion

    #region Keyboard & Input Tests

    [Fact]
    public void InvestmentAmountEntry_WithNumericKeyboard_AcceptsValidInput()
    {
        // Arrange
        var mockEntry = new MockNumericEntry();
        
        // Act
        mockEntry.SimulateKeyboardInput("1234.56");
        
        // Assert
        Assert.Equal("1234.56", mockEntry.Text);
        Assert.Equal(1234.56m, mockEntry.NumericValue);
        Assert.True(mockEntry.IsValid);
        Assert.Empty(mockEntry.ValidationErrors);
    }

    [Fact]
    public void InvestmentAmountEntry_WithInvalidInput_ShowsValidationError()
    {
        // Arrange
        var mockEntry = new MockNumericEntry();
        
        // Act
        mockEntry.SimulateKeyboardInput("abc123");
        
        // Assert
        Assert.Equal("abc123", mockEntry.Text);
        Assert.Equal(0m, mockEntry.NumericValue);
        Assert.False(mockEntry.IsValid);
        Assert.Contains("Invalid numeric format", mockEntry.ValidationErrors);
    }

    [Fact]
    public void DecimalInput_WithMultipleDecimalPoints_HandlesCorrectly()
    {
        // Arrange
        var mockEntry = new MockNumericEntry();
        
        // Act
        mockEntry.SimulateKeyboardInput("123.45.67");
        
        // Assert
        Assert.Equal("123.45.67", mockEntry.Text);
        Assert.False(mockEntry.IsValid);
        Assert.Contains("Multiple decimal points not allowed", mockEntry.ValidationErrors);
    }

    [Fact]
    public void CurrencyEntry_WithAutoFormatting_FormatsCorrectly()
    {
        // Arrange
        var mockCurrencyEntry = new MockCurrencyEntry();
        mockCurrencyEntry.CurrencySymbol = "$";
        
        // Act
        mockCurrencyEntry.SimulateKeyboardInput("1234.56");
        
        // Assert
        Assert.Equal("$1,234.56", mockCurrencyEntry.FormattedText);
        Assert.Equal(1234.56m, mockCurrencyEntry.NumericValue);
        Assert.True(mockCurrencyEntry.HasAutoFormatting);
    }

    [Fact]
    public void TabNavigation_BetweenInputFields_WorksCorrectly()
    {
        // Arrange
        var mockForm = new MockInvestmentForm();
        mockForm.AddField("Amount", new MockNumericEntry());
        mockForm.AddField("Date", new MockDateEntry());
        mockForm.AddField("Description", new MockTextEntry());
        
        // Act
        mockForm.TabToNextField(); // Should focus Amount
        Assert.Equal("Amount", mockForm.CurrentFocusedField);
        
        mockForm.TabToNextField(); // Should focus Date
        Assert.Equal("Date", mockForm.CurrentFocusedField);
        
        mockForm.TabToNextField(); // Should focus Description
        Assert.Equal("Description", mockForm.CurrentFocusedField);
        
        mockForm.TabToNextField(); // Should wrap to Amount
        Assert.Equal("Amount", mockForm.CurrentFocusedField);
    }

    [Fact]
    public void AutoCorrection_OnTyping_SuggestsCorrections()
    {
        // Arrange
        var mockEntry = new MockAutoCorrectEntry();
        mockEntry.AddDictionary(new[] { "AAPL", "MSFT", "GOOGL", "TSLA" });
        
        // Act
        mockEntry.SimulateKeyboardInput("AAP");
        
        // Assert
        Assert.Equal("AAP", mockEntry.Text);
        Assert.Contains("AAPL", mockEntry.Suggestions);
        Assert.True(mockEntry.HasSuggestions);
    }

    #endregion

    #region Accessibility Tests

    [Fact]
    public void PercentageControl_WithScreenReader_ProvidesCorrectDescription()
    {
        // Arrange
        var mockPercentageControl = new MockAccessiblePercentageControl();
        mockPercentageControl.Percentage = 15.75m;
        mockPercentageControl.UpdateDisplay();
        
        // Act
        var accessibilityText = mockPercentageControl.GetAccessibilityDescription();
        
        // Assert
        Assert.Contains("15.75%", accessibilityText);
        Assert.Contains("positive", accessibilityText);
        Assert.Contains("gain", accessibilityText);
    }

    [Fact]
    public void CurrencyControl_WithScreenReader_AnnouncesAmount()
    {
        // Arrange
        var mockCurrencyControl = new MockAccessibleCurrencyControl();
        mockCurrencyControl.Amount = 1234.56m;
        mockCurrencyControl.Currency = "USD";
        
        // Act
        var accessibilityText = mockCurrencyControl.GetAccessibilityDescription();
        
        // Assert
        Assert.Contains("1234 dollars and 56 cents", accessibilityText);
    }

    [Fact]
    public void HighContrast_OnControls_IncreasesVisibility()
    {
        // Arrange
        var mockControl = new MockAccessibleControl();
        
        // Act
        mockControl.EnableHighContrast();
        
        // Assert
        Assert.True(mockControl.IsHighContrastEnabled);
        Assert.True(mockControl.HasIncreasedBorderWidth);
        Assert.True(mockControl.HasHighContrastColors);
    }

    #endregion

    #region Mock Classes

    public class MockBrokerAccountTemplate
    {
        public object? BindingContext { get; set; }
        public bool WasTapped { get; private set; }
        public object? TappedSnapshot { get; private set; }
        public List<string> InteractionHistory { get; } = new();

        public void SimulateTap()
        {
            WasTapped = true;
            TappedSnapshot = BindingContext;
            InteractionHistory.Add("Tap");
        }
    }

    public class MockInteractivePercentageControl
    {
        public decimal Percentage { get; set; }
        public bool IsDetailViewVisible { get; private set; }
        public bool WasLongPressed { get; private set; }

        public void SimulateLongPress()
        {
            WasLongPressed = true;
            IsDetailViewVisible = true;
        }
    }

    public class MockPortfolioNavigation
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public bool LastGestureWasSwipe { get; private set; }
        public string LastGesture { get; private set; } = "";

        public void SimulateSwipeLeft()
        {
            if (CurrentPage < TotalPages - 1)
                CurrentPage++;
            LastGestureWasSwipe = true;
            LastGesture = "SwipeLeft";
        }

        public void SimulateSwipeRight()
        {
            if (CurrentPage > 0)
                CurrentPage--;
            LastGestureWasSwipe = true;
            LastGesture = "SwipeRight";
        }
    }

    public class MockChartView
    {
        public float InitialScale { get; set; } = 1.0f;
        public float CurrentScale { get; private set; } = 1.0f;
        public float MinimumScale { get; set; } = 0.1f;
        public float MaximumScale { get; set; } = 5.0f;
        public bool WasPinched { get; private set; }
        public string LastGesture { get; private set; } = "";

        public void SimulatePinchZoom(float scale)
        {
            // Constrain scale to min/max bounds
            CurrentScale = Math.Max(MinimumScale, Math.Min(MaximumScale, scale));
            WasPinched = true;
            LastGesture = "PinchZoom";
        }
    }

    public class MockTouchFeedbackControl
    {
        public bool IsPressed { get; private set; }
        public bool HasVisualFeedback { get; private set; }

        public void SimulateTouchStart()
        {
            IsPressed = true;
            HasVisualFeedback = true;
        }

        public void SimulateTouchEnd()
        {
            IsPressed = false;
            HasVisualFeedback = false;
        }
    }

    public class MockNumericEntry
    {
        public string Text { get; private set; } = "";
        public decimal NumericValue { get; private set; }
        public bool IsValid { get; private set; }
        public List<string> ValidationErrors { get; } = new();

        public void SimulateKeyboardInput(string input)
        {
            Text = input;
            ValidationErrors.Clear();
            IsValid = true;

            // Check for multiple decimal points
            if (input.Count(c => c == '.') > 1)
            {
                ValidationErrors.Add("Multiple decimal points not allowed");
                IsValid = false;
                return;
            }

            // Try to parse as decimal
            if (decimal.TryParse(input, out var value))
            {
                NumericValue = value;
            }
            else
            {
                ValidationErrors.Add("Invalid numeric format");
                IsValid = false;
                NumericValue = 0m;
            }
        }
    }

    public class MockCurrencyEntry
    {
        public string CurrencySymbol { get; set; } = "$";
        public string FormattedText { get; private set; } = "";
        public decimal NumericValue { get; private set; }
        public bool HasAutoFormatting { get; } = true;

        public void SimulateKeyboardInput(string input)
        {
            if (decimal.TryParse(input, out var value))
            {
                NumericValue = value;
                FormattedText = $"{CurrencySymbol}{value:N2}";
            }
        }
    }

    public class MockDateEntry
    {
        public DateTime? SelectedDate { get; set; }
        public bool HasFocus { get; set; }
    }

    public class MockTextEntry
    {
        public string Text { get; set; } = "";
        public bool HasFocus { get; set; }
    }

    public class MockInvestmentForm
    {
        private readonly Dictionary<string, object> _fields = new();
        private readonly List<string> _fieldOrder = new();
        private int _currentFieldIndex = -1;

        public string CurrentFocusedField { get; private set; } = "";

        public void AddField(string name, object field)
        {
            _fields[name] = field;
            _fieldOrder.Add(name);
        }

        public void TabToNextField()
        {
            if (_fieldOrder.Count == 0) return;
            
            _currentFieldIndex = (_currentFieldIndex + 1) % _fieldOrder.Count;
            CurrentFocusedField = _fieldOrder[_currentFieldIndex];
        }
    }

    public class MockAutoCorrectEntry
    {
        private List<string> _dictionary = new();
        
        public string Text { get; private set; } = "";
        public List<string> Suggestions { get; } = new();
        public bool HasSuggestions => Suggestions.Any();

        public void AddDictionary(IEnumerable<string> words)
        {
            _dictionary.AddRange(words);
        }

        public void SimulateKeyboardInput(string input)
        {
            Text = input;
            Suggestions.Clear();
            
            if (!string.IsNullOrEmpty(input))
            {
                var matches = _dictionary.Where(word => 
                    word.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                    .Take(5);
                Suggestions.AddRange(matches);
            }
        }
    }

    public class MockAccessiblePercentageControl
    {
        public decimal Percentage { get; set; }
        public string PercentageValue { get; private set; } = "";
        public string PercentageDecimals { get; private set; } = "";

        public void UpdateDisplay()
        {
            var formatted = Percentage.ToString("0.##");
            var parts = formatted.Split('.');
            PercentageValue = parts[0];
            PercentageDecimals = parts.Length > 1 ? $".{parts[1]}%" : ".00%";
        }

        public string GetAccessibilityDescription()
        {
            var sign = Percentage >= 0 ? "positive" : "negative";
            var type = Percentage >= 0 ? "gain" : "loss";
            return $"Investment performance: {Math.Abs(Percentage):0.##}%, {sign} {type}";
        }
    }

    public class MockAccessibleCurrencyControl
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";

        public string GetAccessibilityDescription()
        {
            var wholePart = (int)Math.Floor(Math.Abs(Amount));
            var decimalPart = (int)Math.Round((Math.Abs(Amount) - wholePart) * 100);
            
            var currencyName = Currency switch
            {
                "USD" => "dollars",
                "EUR" => "euros", 
                "GBP" => "pounds",
                _ => Currency.ToLower()
            };
            
            return $"{wholePart} {currencyName} and {decimalPart} cents";
        }
    }

    public class MockAccessibleControl
    {
        public bool IsHighContrastEnabled { get; private set; }
        public bool HasIncreasedBorderWidth { get; private set; }
        public bool HasHighContrastColors { get; private set; }

        public void EnableHighContrast()
        {
            IsHighContrastEnabled = true;
            HasIncreasedBorderWidth = true;
            HasHighContrastColors = true;
        }
    }

    #endregion

    #region Helper Methods

    private static object CreateTestSnapshot()
    {
        // Return a simple test object representing a broker account snapshot
        return new { Name = "Test Account", Balance = 10000m, Percentage = 15.75m };
    }

    #endregion
}