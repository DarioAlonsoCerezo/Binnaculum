using Binnaculum.Resources.Languages;

namespace Binnaculum;

public class LocalizationResourceManager : INotifyPropertyChanged
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
public class TranslateExtension : BindableObject, IMarkupExtension<BindingBase>
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