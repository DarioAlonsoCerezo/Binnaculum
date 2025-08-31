using Binnaculum.UITest.Core;

namespace Binnaculum.UITest.Appium.PageObjects;

/// <summary>
/// Page Object Model for the movement creator page.
/// Handles creating new investment movements (buy/sell/dividend etc).
/// </summary>
public class MovementCreatorPage : BasePage
{
    // Element selectors based on BrokerMovementCreatorPage.xaml structure
    private readonly IQuery _movementTypeSelector = null!;
    private readonly IQuery _tickerInput = null!;
    private readonly IQuery _quantityInput = null!;
    private readonly IQuery _priceInput = null!;
    private readonly IQuery _dateInput = null!;
    private readonly IQuery _notesInput = null!;
    private readonly IQuery _saveButton = null!;
    private readonly IQuery _cancelButton = null!;

    public MovementCreatorPage(IApp app) : base(app)
    {
        // Initialize queries based on expected BrokerMovementCreatorPage elements
        _movementTypeSelector = _app.Query().ById("MovementTypeControl");
        _tickerInput = _app.Query().ById("TickerInput");
        _quantityInput = _app.Query().ById("QuantityInput");
        _priceInput = _app.Query().ById("PriceInput");
        _dateInput = _app.Query().ById("DateInput");
        _notesInput = _app.Query().ById("NotesInput");
        _saveButton = _app.Query().ById("Save").First();
        _cancelButton = _app.Query().ById("CancelButton").First();
    }

    public override void WaitForPageToLoad()
    {
        WaitForElement(_movementTypeSelector);
        WaitForElement(_saveButton);
    }

    public override bool IsCurrentPage()
    {
        try
        {
            var typeElement = _app.WaitForElement(_movementTypeSelector, TimeSpan.FromSeconds(3));
            var saveElement = _app.WaitForElement(_saveButton, TimeSpan.FromSeconds(3));
            return typeElement.IsDisplayed && saveElement.IsDisplayed;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Select the type of movement (Buy, Sell, Dividend, etc.).
    /// </summary>
    public void SelectMovementType(string movementType)
    {
        Tap(_movementTypeSelector);
        
        // Wait for dropdown/picker to appear and select the option
        var optionQuery = _app.Query().ByText(movementType);
        WaitForElement(optionQuery);
        Tap(optionQuery);
    }

    /// <summary>
    /// Enter the ticker symbol for the investment.
    /// </summary>
    public void EnterTicker(string ticker)
    {
        EnterText(_tickerInput, ticker);
    }

    /// <summary>
    /// Enter the quantity/shares for the movement.
    /// </summary>
    public void EnterQuantity(string quantity)
    {
        EnterText(_quantityInput, quantity);
    }

    /// <summary>
    /// Enter the price per share.
    /// </summary>
    public void EnterPrice(string price)
    {
        EnterText(_priceInput, price);
    }

    /// <summary>
    /// Enter the date for the movement.
    /// </summary>
    public void EnterDate(string date)
    {
        Tap(_dateInput);
        // Handle date picker interaction - this would need platform-specific implementation
        EnterText(_dateInput, date);
    }

    /// <summary>
    /// Enter notes for the movement.
    /// </summary>
    public void EnterNotes(string notes)
    {
        EnterText(_notesInput, notes);
    }

    /// <summary>
    /// Save the movement and return to the broker account page.
    /// </summary>
    public BrokerAccountDetailsPage SaveMovement()
    {
        Tap(_saveButton);
        return new BrokerAccountDetailsPage(_app);
    }

    /// <summary>
    /// Cancel movement creation and return to the broker account page.
    /// </summary>
    public BrokerAccountDetailsPage CancelMovement()
    {
        Tap(_cancelButton);
        return new BrokerAccountDetailsPage(_app);
    }

    /// <summary>
    /// Enter complete movement data using a test data object.
    /// </summary>
    public void EnterMovementData(InvestmentMovementData movementData)
    {
        SelectMovementType(movementData.MovementType);
        EnterTicker(movementData.Ticker);
        EnterQuantity(movementData.Quantity.ToString());
        EnterPrice(movementData.Price.ToString("F2"));
        EnterDate(movementData.Date.ToString("yyyy-MM-dd"));
        
        if (!string.IsNullOrEmpty(movementData.Notes))
        {
            EnterNotes(movementData.Notes);
        }
    }

    /// <summary>
    /// Check if the save button is enabled (indicating form is valid).
    /// </summary>
    public bool IsSaveButtonEnabled()
    {
        var element = WaitForElement(_saveButton);
        return element.IsEnabled;
    }
}

/// <summary>
/// Test data structure for investment movement data.
/// </summary>
public class InvestmentMovementData
{
    public string MovementType { get; set; } = "";
    public string Ticker { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public DateTime Date { get; set; }
    public string Notes { get; set; } = "";
    public string Description => $"{MovementType} {Quantity} {Ticker} @ {Price:F2}";
}