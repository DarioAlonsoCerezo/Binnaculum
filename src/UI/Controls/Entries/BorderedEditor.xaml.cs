namespace Binnaculum.Controls;

public partial class BorderedEditor
{
    public event EventHandler<TextChangedEventArgs> TextChanged;
    public event EventHandler Completed;

    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(
            nameof(Text), 
            typeof(string), 
            typeof(BorderedEditor), 
            default(string));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly BindableProperty PlaceholderProperty =
    BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(BorderedEditor), default(string),
        propertyChanged: (bindable, oldValue, newValue) =>
        {
            if (bindable is BorderedEditor BorderedEditor)
                BorderedEditor.BorderlessEditor.Placeholder = (string)newValue;
        });

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public static readonly BindableProperty PlaceholderColorProperty =
        BindableProperty.Create(nameof(PlaceholderColor), typeof(Color), typeof(BorderedEditor), null,
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                if (bindable is BorderedEditor BorderedEditor)
                    BorderedEditor.BorderlessEditor.PlaceholderColor = (Color)newValue;
            });

    public Color PlaceholderColor
    {
        get => (Color)GetValue(PlaceholderColorProperty);
        set => SetValue(PlaceholderColorProperty, value);
    }

    public BorderedEditor()
	{
		InitializeComponent();
	}

    protected override void StartLoad()
    {
        BorderlessEditor.Events().TextChanged
            .ObserveOn(UiThread)
            .Subscribe(e =>
            {
                Text = e.NewTextValue;
                TextChanged?.Invoke(this, e);
            }).DisposeWith(Disposables);

        BorderlessEditor.Events().Completed
            .Subscribe(e =>
            {
                Completed?.Invoke(this, e);
            }).DisposeWith(Disposables);
    }
}