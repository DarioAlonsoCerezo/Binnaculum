namespace Binnaculum.Controls;

public partial class OptionTradeBuilder
{
	public OptionTradeBuilder()
	{
		InitializeComponent();
	}

    protected override void StartLoad()
    {
        PutRadioButton.Events().CheckedChanged
            .Where(x => x)
            .Subscribe(_ => CallRadioButton.IsChecked = false)
            .DisposeWith(Disposables);

        CallRadioButton.Events().CheckedChanged
            .Where(x => x)
            .Subscribe(_ => PutRadioButton.IsChecked = false)
            .DisposeWith(Disposables);
    }
}