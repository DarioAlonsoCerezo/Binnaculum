namespace Binnaculum.Controls;

public partial class IconControl
{
    public event EventHandler IconClicked;

    public static readonly BindableProperty ImagePathProperty = BindableProperty.Create(
        nameof(ImagePath),
        typeof(string),
        typeof(IconControl),
        default(string));

    public string ImagePath
    {
        get => (string)GetValue(ImagePathProperty);
        set => SetValue(ImagePathProperty, value);
    }

    public static readonly BindableProperty PlaceholderTextProperty = BindableProperty.Create(
        nameof(PlaceholderText),
        typeof(string),
        typeof(IconControl),
        default(string));

    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public IconControl()
    {
        InitializeComponent();
    }

    protected override void StartLoad()
    {
        this.WhenAnyValue(x => x.ImagePath)
            .ObserveOn(UiThread)
            .BindTo(IconImage, x => x.Source)
            .DisposeWith(Disposables);

        this.WhenAnyValue(x => x.ImagePath)
            .Select(x => !string.IsNullOrWhiteSpace(x))
            .ObserveOn(UiThread)
            .BindTo(IconImage, x => x.IsVisible)
            .DisposeWith(Disposables);

        this.WhenAnyValue(x => x.PlaceholderText)
            .Select(SanitizePlaceholderText)
            .ObserveOn(UiThread)
            .BindTo(IconPlaceholder, x => x.Text)
            .DisposeWith(Disposables);

        this.WhenAnyValue(x => x.ImagePath, x => x.PlaceholderText)
            .Where(x => !string.IsNullOrWhiteSpace(x.Item2))
            .Select(x => (x.Item1, SanitizePlaceholderText(x.Item2)))
            .Select(x => string.IsNullOrEmpty(x.Item1) && !string.IsNullOrWhiteSpace(x.Item2))
            .ObserveOn(UiThread)
            .BindTo(IconPlaceholder, x => x.IsVisible)
            .DisposeWith(Disposables);

        ContentGesture.Events().Tapped
            .ObserveOn(UiThread)
            .Do(_ => IconClicked?.Invoke(this, EventArgs.Empty))
            .Subscribe()
            .DisposeWith(Disposables);
    }

    private static string SanitizePlaceholderText(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Keep only letters and numbers (alphanumeric)
        var cleaned = new string(input.Where(c => char.IsLetterOrDigit(c)).ToArray());

        // Limit to 4 characters
        return cleaned.Length > 4 ? cleaned.Substring(0, 4) : cleaned;
    }
}
