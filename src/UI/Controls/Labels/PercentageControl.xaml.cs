namespace Binnaculum.Controls;

public partial class PercentageControl
{
    public static readonly BindableProperty PercentageProperty =
        BindableProperty.Create(
            nameof(Percentage), 
            typeof(decimal), 
            typeof(PercentageControl), 
            0.00m,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is PercentageControl control && newValue is decimal percentage)
                {
                    // Update the UI when Percentage changes
                    control.UpdateDisplay();
                }
            });

    public decimal Percentage
    {
        get => (decimal)GetValue(PercentageProperty);
        set => SetValue(PercentageProperty, value);
    }
    
	public PercentageControl()
	{
		InitializeComponent();
	}
    
    protected override void StartLoad()
    {
        // Ensure UpdateDisplay runs once on the UI thread
        Observable
            .Return(Unit.Default)
            .Take(1)
            .ObserveOn(UiThread)
            .Subscribe(_ => UpdateDisplay())
            .DisposeWith(Disposables);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateDisplay();
    }
    
    private void UpdateDisplay()
    {
        // Check if the percentage is 0. If so, display "0.00%" with neutral color based on AppTheme.
        if (Percentage.Equals(0m))
        {
            PercentageValue.Text = "0";
            PercentageDemcimals.Text = ".00%";
            // Here we setup color based on AppTheme
            PercentageLabel.TextColor = Application.Current!.RequestedTheme == AppTheme.Dark
                ? (Color)Application.Current!.Resources["White"]
                : (Color)Application.Current!.Resources["Black"];
            return;
        }

        // Format the percentage with 2 decimal places
        string formattedPercentage = Percentage.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

        // Split the formatted percentage into whole number and decimal parts
        string[] parts = formattedPercentage.Split('.');

        // Set the whole number part
        PercentageValue.Text = parts[0];
        PercentageLabel.TextColor = Percentage >= 0 
            ? (Color)Application.Current!.Resources["GreenState"] 
            : (Color)Application.Current!.Resources["RedState"];

        if (parts.Length == 1)
        {
            PercentageDemcimals.Text = $".00%";
            return;
        }

        // Set the decimal part with the decimal point
        PercentageDemcimals.Text = $".{parts[1]}%";
    }
}