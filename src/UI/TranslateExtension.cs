namespace Binnaculum;

public partial class LocalizationResourceManager : INotifyPropertyChanged
{
    private LocalizationResourceManager()
    {
        AppResources.Culture = CultureInfo.CurrentCulture;
    }

    public static LocalizationResourceManager Instance { get; } = new();

    public object this[string resourceKey]
    {
        get
        {
            var result = AppResources.ResourceManager.GetObject(resourceKey, AppResources.Culture);

            if (result == null)
            {
                // Resource key validation strategy:
                // 1. In DEBUG builds: Throw KeyNotFoundException to immediately identify missing translations
                //    during development, enabling quick detection and resolution of missing resources.
                // 2. In RELEASE builds: Consider returning the key itself as fallback to prevent app crashes
                //    in production while still providing some visible text.
                // 
                // To configure this behavior conditionally, use:
#if DEBUG
                throw new KeyNotFoundException($"Missing resource: '{resourceKey}'");
#else
                return resourceKey; // Fallback for production
#endif

            }

            return result;
        }
    }

    // Add this method for formatted strings with parameters
    public string GetString(string resourceKey, params object[] args)
    {
        var format = AppResources.ResourceManager.GetString(resourceKey, AppResources.Culture);

        if (string.IsNullOrEmpty(format))
        {
            // Resource key validation strategy:
            // 1. In DEBUG builds: Throw KeyNotFoundException to immediately identify missing translations
            //    during development, enabling quick detection and resolution of missing resources.
            // 2. In RELEASE builds: Consider returning the key itself as fallback to prevent app crashes
            //    in production while still providing some visible text.
            // 
            // To configure this behavior conditionally, use:
#if DEBUG
            throw new KeyNotFoundException($"Missing resource: '{resourceKey}'");
#else
            return resourceKey; // Fallback for production
#endif

        }

        return string.Format(format, args);
    }

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
    /// Sets a localized text with format parameters on a Label
    /// </summary>
    public static Label SetLocalizedText(this Label label, string resourceKey, params object[] args)
    {
        label.Text = GetLocalizedString(resourceKey, args);
        return label;
    }

    /// <summary>
    /// Sets a localized text on a Span using a type-safe resource key
    /// </summary>
    public static Span SetLocalizedText(this Span span, string resourceKey)
    {
        span.SetBinding(Span.TextProperty, new Binding
        {
            Source = LocalizationResourceManager.Instance,
            Path = $"[{resourceKey}]",
            Mode = BindingMode.OneWay
        });
        return span;
    }

    /// <summary>
    /// Sets a localized text with format parameters on a Label
    /// </summary>
    public static Span SetLocalizedText(this Span span, string resourceKey, params object[] args)
    {
        span.Text = GetLocalizedString(resourceKey, args);
        return span;
    }

    /// <summary>
    /// Gets the localized string for a specific resource key
    /// </summary>
    public static string GetLocalizedString(string resourceKey)
    {
        return LocalizationResourceManager.Instance[resourceKey]?.ToString() ?? resourceKey;
    }

    /// <summary>
    /// Gets the localized string with format parameters for a specific resource key
    /// </summary>
    public static string GetLocalizedString(string resourceKey, params object[] args)
    {
        return LocalizationResourceManager.Instance.GetString(resourceKey, args);
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
    /// Sets a localized text with format parameters on any BindableObject that has a Text property
    /// </summary>
    public static T SetLocalizedText<T>(this T control, string resourceKey, BindableProperty textProperty, params object[] args)
        where T : BindableObject
    {
        if (control is BindableObject bo)
        {
            bo.SetValue(textProperty, GetLocalizedString(resourceKey, args));
        }

        return control;
    }

    /// <summary>
    /// Gets the localized string for a resource key
    /// </summary>
    public static string Localized(this string resourceKey)
    {
        return LocalizationResourceManager.Instance[resourceKey]?.ToString() ?? resourceKey;
    }

    /// <summary>
    /// Gets the localized string with format parameters for a resource key
    /// </summary>
    public static string Localized(this string resourceKey, params object[] args)
    {
        return LocalizationResourceManager.Instance.GetString(resourceKey, args);
    }
}
