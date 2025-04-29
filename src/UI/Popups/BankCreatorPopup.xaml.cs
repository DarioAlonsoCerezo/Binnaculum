
namespace Binnaculum.Popups;

public partial class BankCreatorPopup
{
	public BankCreatorPopup()
	{
		InitializeComponent();

        SaveOrDiscard.Events().DiscardClicked
            .Subscribe(_ => Close())
            .DisposeWith(Disposables);

        SaveOrDiscard.Events().SaveClicked
            .Subscribe(_ => Close())
            .DisposeWith(Disposables);

        Container.WidthRequest = DeviceDisplay.MainDisplayInfo.Width;
    }
}