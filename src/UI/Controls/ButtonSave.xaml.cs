namespace Binnaculum.Controls;

public partial class ButtonSave
{
    public event EventHandler? SaveClicked;
    public ButtonSave()
	{
		InitializeComponent();
	}

    protected override void StartLoad()
    {
        this.WhenAnyValue(x => x.IsEnabled)
            .ObserveOn(UiThread)
            .BindTo(SaveButton, x => x.IsEnabled)
            .DisposeWith(Disposables);

        SaveButton.Events().Clicked
            .Subscribe(_ => SaveClicked?.Invoke(this, EventArgs.Empty))
            .DisposeWith(Disposables);
    }
}