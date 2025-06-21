namespace Binnaculum.Popups;

public partial class LeveragePopup
{
	public LeveragePopup()
	{
		InitializeComponent();

        ApplyHeightPercentage(this, 0.5, true);

        SaveOrDiscard.Events().DiscardClicked
            .Subscribe(_ => Close())
            .DisposeWith(Disposables);

        SaveOrDiscard.Events().SaveClicked
            .Select(_ => Leverage.Text.ToDecimalOrZero())
            .Subscribe(leverage =>
            {
                if(leverage <= 1)
                {
                    Close(1.0m);
                }    
                else
                {
                    Close(leverage);
                }
            }).DisposeWith(Disposables);

    }
}