namespace Binnaculum.Controls;

public partial class ButtonSaveOrDiscard
{
    public event EventHandler? SaveClicked;
    public event EventHandler? DiscardClicked;

    public static readonly BindableProperty IsButtonSaveEnabledProperty =
        BindableProperty.Create(
            nameof(IsButtonSaveEnabled),
            typeof(bool),
            typeof(ButtonSaveOrDiscard),
            defaultValue: true);

    public bool IsButtonSaveEnabled
    {
        get => (bool)GetValue(IsButtonSaveEnabledProperty);
        set => SetValue(IsButtonSaveEnabledProperty, value);
    }

    public ButtonSaveOrDiscard()
    {
        InitializeComponent();
    }

    protected override void StartLoad()
    {
        this.WhenAnyValue(x => x.IsButtonSaveEnabled)
            .ObserveOn(UiThread)
            .BindTo(ButtonSave, x => x.IsEnabled)
            .DisposeWith(Disposables);

        ButtonSave.Events().SaveClicked
            .Subscribe(_ => SaveClicked?.Invoke(this, EventArgs.Empty))
            .DisposeWith(Disposables);

        ButtonDiscard.Events().DiscardClicked
            .Subscribe(_ => DiscardClicked?.Invoke(this, EventArgs.Empty))
            .DisposeWith(Disposables);
    }
}
