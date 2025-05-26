namespace Binnaculum.Controls;

public partial class MoneyControl
{
    public static readonly BindableProperty MoneyProperty =
        BindableProperty.Create(nameof(Money), typeof(Core.Models.Currency), typeof(MoneyControl), default(Core.Models.Currency),
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is MoneyControl control && newValue is Core.Models.Currency currency)
                {
                    // Update UI when currency changes
                    control.UpdateControl();
                }
            });

    public Core.Models.Currency Money
    {
        get => (Core.Models.Currency)GetValue(MoneyProperty);
        set => SetValue(MoneyProperty, value);
    }

    public static readonly BindableProperty HideProperty =
        BindableProperty.Create(nameof(Hide), typeof(bool), typeof(MoneyControl), false,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is MoneyControl control && newValue is bool hideSymbol)
                {
                    // Update the UI based on whether to hide the currency symbol
                    control.UpdateControl();
                }
            });

    public bool Hide
    {
        get => (bool)GetValue(HideProperty);
        set => SetValue(HideProperty, value);
    }

    public static readonly BindableProperty AmountProperty =
        BindableProperty.Create(nameof(Amount), typeof(decimal), typeof(MoneyControl), 0m,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is MoneyControl control && newValue is decimal amount)
                {
                    // Update display with formatted amount
                    control.UpdateControl();
                }
            });

    public decimal Amount
    {
        get => (decimal)GetValue(AmountProperty);
        set => SetValue(AmountProperty, value);
    }

    public static readonly BindableProperty IsNegativeProperty =
        BindableProperty.Create(nameof(IsNegative), typeof(bool), typeof(MoneyControl), false,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is MoneyControl control && newValue is bool isNegative)
                {
                    // Update display with formatted amount
                    control.UpdateControl();
                }
            });

    public bool IsNegative
    {
        get => (bool)GetValue(IsNegativeProperty);
        set => SetValue(IsNegativeProperty, value);
    }

    public static readonly BindableProperty ChangeColorProperty =
        BindableProperty.Create(nameof(ChangeColor), typeof(bool), typeof(MoneyControl), false,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is MoneyControl control && newValue is bool changeColor)
                {
                    // Update the color of the amount based on the changeColor property
                    control.UpdateControl();
                }
            });

    public bool ChangeColor
    {
        get => (bool)GetValue(ChangeColorProperty);
        set => SetValue(ChangeColorProperty, value);
    }

    public MoneyControl()
	{
		InitializeComponent();
	}

    protected override void StartLoad()
    {
        
    }

    private void UpdateControl()
    {
        // Update the currency symbol visibility based on the Hide property
        if (Money != null && !Hide)
        {
            CurrencySymbol.Text = Money.Code + " ";
        }
        else
        {
            CurrencySymbol.Text = string.Empty;
        }

        // Format the amount to always have exactly two decimal places
        string formattedAmount = Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        
        // Split the formatted amount into whole number and decimal parts
        string[] parts = formattedAmount.Split('.');
        
        // Set the whole number part
        AmountValue.Text = parts[0];
        
        // Set the decimal part with the decimal point
        AmountDecimals.Text = "." + parts[1];

        if (IsNegative)
        {
            NegativeStart.Text = "(";
            NegativeEnd.Text = ")";
        }

        if (ChangeColor)
        {
            AmountValue.TextColor = IsNegative 
                ? (Color)Application.Current!.Resources["RedState"] 
                : (Color)Application.Current!.Resources["GreenState"];
            AmountDecimals.TextColor = IsNegative 
                ? (Color)Application.Current!.Resources["RedState"] 
                : (Color)Application.Current!.Resources["GreenState"];
        }
    }
}