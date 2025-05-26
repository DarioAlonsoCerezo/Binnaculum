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

        STORadioButton.Events().CheckedChanged
            .Where(x => x)
            .Subscribe(_ =>
            {
                BTORadioButton.IsChecked = false;
                STCRadioButton.IsChecked = false;
                BTCRadioButton.IsChecked = false;
            })
            .DisposeWith(Disposables);

        BTORadioButton.Events().CheckedChanged
            .Where(x => x)
            .Subscribe(_ =>
            {
                STORadioButton.IsChecked = false;
                STCRadioButton.IsChecked = false;
                BTCRadioButton.IsChecked = false;
            })
            .DisposeWith(Disposables);

        STCRadioButton.Events().CheckedChanged
            .Where(x => x)
            .Subscribe(_ =>
            {
                STORadioButton.IsChecked = false;
                BTORadioButton.IsChecked = false;
                BTCRadioButton.IsChecked = false;
            })
            .DisposeWith(Disposables);

        BTCRadioButton.Events().CheckedChanged
            .Where(x => x)
            .Subscribe(_ =>
            {
                STORadioButton.IsChecked = false;
                STCRadioButton.IsChecked = false;
                BTORadioButton.IsChecked = false;
            })
            .DisposeWith(Disposables);
    }
}