namespace Binnaculum.Controls;

public partial class TimeStampControl
{
    private CultureInfo? _culture;

    public static readonly BindableProperty DateTimeProperty =
        BindableProperty.Create(
            nameof(DateTime),
            typeof(DateTime),
            typeof(TimeStampControl),
            DateTime.Now,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is TimeStampControl control && newValue is DateTime dt)
                {
                    control.UpdateDisplay();
                }
            });

    public DateTime DateTime
    {
        get => (DateTime)GetValue(DateTimeProperty);
        set => SetValue(DateTimeProperty, value);
    }

    public static readonly BindableProperty ShowTimeProperty =
        BindableProperty.Create(
            nameof(ShowTime),
            typeof(bool),
            typeof(TimeStampControl),
            false, // Default to hiding time
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is TimeStampControl control)
                {
                    control.UpdateDisplay();
                }
            });

    public bool ShowTime
    {
        get => (bool)GetValue(ShowTimeProperty);
        set => SetValue(ShowTimeProperty, value);
    }
    
    public TimeStampControl()
    {
        InitializeComponent();

        Core.UI.SavedPrefereces.UserPreferences
            .ObserveOn(UiThread)
            .Subscribe(preferences =>
            {
                _culture = preferences.Language switch
                {
                    "en" => new CultureInfo("en-US"),
                    "es" => new CultureInfo("es-ES"),
                    _ => new CultureInfo("en-US")
                };
                UpdateDisplay();
            })
            .DisposeWith(Disposables);
    }
    
    protected override void StartLoad()
    {

    }
    
    private void UpdateDisplay()
    {
        if (DateSpan == null || TimePartSpan == null || _culture == null)
            return;
            
        // Format the date based on the specified rules
        string formattedDate = FormatDate(DateTime);
        DateSpan.Text = formattedDate;
        
        // Format and show/hide time based on ShowTime property
        if (ShowTime)
        {
            TimePartSpan.Text = $" {DateTime:t}"; // Format time using short time pattern
        }
        else
        {
            TimePartSpan.Text = string.Empty;
        }
    }
    
    private string FormatDate(DateTime dateTime)
    {
        DateTime today = DateTime.Today;
        CultureInfo currentCulture = _culture!;
        
        // Check if date is today - use localized string if available
        if (dateTime.Date == today)
        {
            // Try to get localized "Today" string, fallback to English if not found
            return LocalizationExtensions.GetLocalizedString(Core.ResourceKeys.Today);
        }
        
        // Calculate start of week based on current culture
        int firstDayOfWeek = (int)currentCulture.DateTimeFormat.FirstDayOfWeek;
        int currentDayOfWeek = (int)today.DayOfWeek;
        int diff = (currentDayOfWeek - firstDayOfWeek + 7) % 7;
        DateTime startOfWeek = today.AddDays(-diff);
        
        // Check if date is yesterday - use localized string if available
        if (dateTime.Date == today.AddDays(-1))
        {
            // Try to get localized "Yesterday" string, fallback to English if not found
            return LocalizationExtensions.GetLocalizedString(Core.ResourceKeys.Yesterday);
        }
        
        // Check if date is within the current week
        if (dateTime.Date >= startOfWeek && dateTime.Date < today)
        {
            // Return day name for dates in current week with first letter uppercase
            return CapitalizeFirstLetter(dateTime.ToString("dddd", currentCulture));
        }
        
        // For previous weeks, return date in culture-specific format
        return dateTime.ToString("d", currentCulture); // "d" is short date pattern
    }
    
    // Helper method to capitalize just the first letter of a string
    private string CapitalizeFirstLetter(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
            
        // Use TextInfo.ToTitleCase for culture-aware capitalization of the first letter
        // This properly handles special rules for different cultures
        return _culture!.TextInfo.ToTitleCase(text);
    }
}