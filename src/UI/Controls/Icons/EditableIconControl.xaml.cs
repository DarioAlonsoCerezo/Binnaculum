namespace Binnaculum.Controls;

public partial class EditableIconControl
{
    public event EventHandler IconClicked;

    public static readonly BindableProperty ImagePathProperty = BindableProperty.Create(
        nameof(ImagePath),
        typeof(string),
        typeof(EditableIconControl),
        default(string));

    public string ImagePath
    {
        get => (string)GetValue(ImagePathProperty);
        set => SetValue(ImagePathProperty, value);
    }

    public static readonly BindableProperty PlaceholderTextProperty = BindableProperty.Create(
        nameof(PlaceholderText),
        typeof(string),
        typeof(EditableIconControl),
        default(string));

    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    public EditableIconControl()
	{
		InitializeComponent();
	}

    protected override void StartLoad()
    {
        this.WhenAnyValue(x => x.ImagePath)
            .ObserveOn(UiThread)
            .BindTo(Icon, x => x.ImagePath)
            .DisposeWith(Disposables);

        this.WhenAnyValue(x => x.PlaceholderText)
            .ObserveOn(UiThread)
            .BindTo(Icon, x => x.PlaceholderText)
            .DisposeWith(Disposables);

        Icon.Events().IconClicked
            .ObserveOn(UiThread)
            .Subscribe(_ => IconClicked?.Invoke(this, EventArgs.Empty))
            .DisposeWith(Disposables);
    }
}