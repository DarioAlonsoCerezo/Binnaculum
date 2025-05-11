using Binnaculum.Controls;
using Binnaculum.Core;

namespace Binnaculum.Pages;

public partial class BrokerMovementCreatorPage
{
    private readonly Models.BrokerAccount _account;

	public BrokerMovementCreatorPage(Models.BrokerAccount account)
	{
		InitializeComponent();
        _account = account;

        Icon.ImagePath = _account.Broker.Image;
        AccountName.Text = _account.AccountNumber;

        MovementTypeControl.ItemsSource = new List<SelectableItem>
        {
            new SelectableItem
            {
                Title = "Deposit",
                ItemValue = "Deposit"
            },
            new SelectableItem
            {
                Title = "Withdraw",
                ItemValue = "Withdraw"
            },
            new SelectableItem
            {
                Title = "Transfer",
                ItemValue = "Transfer"
            }
        };
    }

    protected override void StartLoad()
    {
        CloseGesture.Events().Tapped
            .Select(async _ => await Navigation.PopModalAsync())
            .Subscribe()
            .DisposeWith(Disposables);

        //By default, it should be created manually
        ManualRadioButton.IsChecked = true;
    }
}