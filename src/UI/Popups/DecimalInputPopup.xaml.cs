namespace Binnaculum.Popups;

public partial class DecimalInputPopup
{
    private decimal _target;
    public DecimalInputPopup(decimal target, string i18nTitle)
	{
		InitializeComponent();

        LabelTitle.SetLocalizedText(i18nTitle);
        _target = target;

        ForceFillWidth();

        MultiplierEntry.Text = _target.ToString("N0");

        SaveOrDiscard.Events().DiscardClicked
            .Subscribe(_ => Close())
            .DisposeWith(Disposables);

        SaveOrDiscard.Events().SaveClicked
            .Subscribe(_ =>
            {
                Close(_target);
            }).DisposeWith(Disposables);

        MultiplierEntry.Events().TextChanged
            .Do(x => _target = x.NewTextValue.ToDecimalOrZero())
            .Subscribe()
            .DisposeWith(Disposables);
    }
}