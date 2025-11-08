namespace Binnaculum.Controls;

public partial class MoneyControl
{
    private bool _updateScheduled = false;
    private Color _redColor = (Color)Application.Current!.Resources["RedState"];
    private Color _greenColor = (Color)Application.Current!.Resources["GreenState"];

    public static readonly BindableProperty MoneyProperty =
        BindableProperty.Create(nameof(Money), typeof(Core.Models.Currency), typeof(MoneyControl), default(Core.Models.Currency),
            propertyChanged: OnPropertyChanged);

    public Core.Models.Currency Money
    {
        get => (Core.Models.Currency)GetValue(MoneyProperty);
        set => SetValue(MoneyProperty, value);
    }

    public static readonly BindableProperty HideProperty =
        BindableProperty.Create(nameof(Hide), typeof(bool), typeof(MoneyControl), false,
            propertyChanged: OnPropertyChanged);

    public bool Hide
    {
        get => (bool)GetValue(HideProperty);
        set => SetValue(HideProperty, value);
    }

    public static readonly BindableProperty AmountProperty =
        BindableProperty.Create(nameof(Amount), typeof(decimal), typeof(MoneyControl), 0m,
            propertyChanged: OnPropertyChanged);

    public decimal Amount
    {
        get => (decimal)GetValue(AmountProperty);
        set => SetValue(AmountProperty, value);
    }

    public static readonly BindableProperty IsNegativeProperty =
        BindableProperty.Create(nameof(IsNegative), typeof(bool), typeof(MoneyControl), false,
            propertyChanged: OnPropertyChanged);

    public bool IsNegative
    {
        get => (bool)GetValue(IsNegativeProperty);
        set => SetValue(IsNegativeProperty, value);
    }

    public static readonly BindableProperty ChangeColorProperty =
        BindableProperty.Create(nameof(ChangeColor), typeof(bool), typeof(MoneyControl), false,
            propertyChanged: OnPropertyChanged);

    public bool ChangeColor
    {
        get => (bool)GetValue(ChangeColorProperty);
        set => SetValue(ChangeColorProperty, value);
    }

    private static void OnPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MoneyControl control)
        {
            control.ScheduleUpdate();
        }
    }

    private void ScheduleUpdate()
    {
        if (_updateScheduled) return;

        _updateScheduled = true;

        Dispatcher.Dispatch(() =>
        {
            _updateScheduled = false;
            UpdateControl();
        });
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
        CurrencySymbol.Text = Money != null && !Hide ? Money.Code + " " : string.Empty;

        // Format the amount to always have exactly two decimal places
        string formattedAmount = Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);

        // Split the formatted amount into whole number and decimal parts
        string[] parts = formattedAmount.Split('.');

        // Set the whole number part
        AmountValue.Text = parts[0];

        // Set the decimal part with the decimal point
        AmountDecimals.Text = "." + parts[1];

        CurrencySymbol.TextColor =
            AmountValue.TextColor =
            AmountDecimals.TextColor = IsNegative
            ? _redColor
            : _greenColor;
    }
}