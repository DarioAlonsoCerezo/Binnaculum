namespace Binnaculum.Controls;

public partial class ButtonDiscard
{
    public event EventHandler? DiscardClicked;
    public ButtonDiscard()
	{
		InitializeComponent();
	}

    protected override void StartLoad()
    {
        this.WhenAnyValue(x => x.IsEnabled)
            .ObserveOn(UiThread)
            .BindTo(DiscardText, x => x.IsEnabled)
            .DisposeWith(Disposables);

        DiscardTapped.Events().Tapped
            .Subscribe(_ => DiscardClicked?.Invoke(this, EventArgs.Empty))
            .DisposeWith(Disposables);
    }
}