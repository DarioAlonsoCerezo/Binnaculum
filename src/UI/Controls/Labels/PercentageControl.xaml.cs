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
        UpdateDisplay();
    }
    
    private void UpdateDisplay()
    {
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