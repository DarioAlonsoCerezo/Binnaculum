namespace Binnaculum;

public partial class LocalizationResourceManager : INotifyPropertyChanged
{
    private LocalizationResourceManager()
    {
        AppResources.Culture = CultureInfo.CurrentCulture;
    }
    public static LocalizationResourceManager Instance { get; } = new();
    public object this[string resourceKey] => AppResources.ResourceManager.GetObject(resourceKey, AppResources.Culture) ?? Array.Empty<byte>();
    public event PropertyChangedEventHandler? PropertyChanged;
    public void SetCulture(CultureInfo culture)
    {
        AppResources.Culture = culture;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }
}

[AcceptEmptyServiceProvider]
[ContentProperty(nameof(Name))]
public partial class TranslateExtension : BindableObject, IMarkupExtension<BindingBase>
{
    public string? Name { get; set; }
    public BindingBase ProvideValue(IServiceProvider serviceProvider)
    {
        return new Binding
        {
            Mode = BindingMode.OneWay,
            Path = $"[{Name}]",
            Source = LocalizationResourceManager.Instance
        };
    }
    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider)
    {
        return ProvideValue(serviceProvider);
    }
}

/// <summary>
/// Extension methods for handling localization in code-behind
/// </summary>
public static partial class LocalizationExtensions
{
    /// <summary>
    /// Sets a localized text on a Label using a type-safe resource key
    /// </summary>
    public static Label SetLocalizedText(this Label label, string resourceKey)
    {
        label.SetBinding(Label.TextProperty, new Binding
        {
            Source = LocalizationResourceManager.Instance,
            Path = $"[{resourceKey}]",
            Mode = BindingMode.OneWay
        });

        return label;
    }

    /// <summary>
    /// Gets the localized string for a specific resource key
    /// </summary>
    public static string GetLocalizedString(string resourceKey)
    {
        return LocalizationResourceManager.Instance[resourceKey]?.ToString() ?? resourceKey;
    }

    /// <summary>
    /// Sets a localized text on any BindableObject that has a Text property
    /// </summary>
    public static T SetLocalizedText<T>(this T control, string resourceKey, BindableProperty? textProperty = null)
        where T : BindableObject
    {
        // Default to Label.TextProperty if not specified
        var property = textProperty ?? Label.TextProperty;

        control.SetBinding(property, new Binding
        {
            Source = LocalizationResourceManager.Instance,
            Path = $"[{resourceKey}]",
            Mode = BindingMode.OneWay
        });

        return control; // Return the control for method chaining
    }

    /// <summary>
    /// Gets the localized string for a resource key
    /// </summary>
    public static string Localized(this string resourceKey)
    {
        return LocalizationResourceManager.Instance[resourceKey]?.ToString() ?? resourceKey;
    }
}