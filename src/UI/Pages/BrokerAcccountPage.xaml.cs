using Binnaculum.Core;

namespace Binnaculum.Pages;

public partial class BrokerAcccountPage
{
	private Models.OverviewSnapshot _snapshot;
    private Models.Broker _broker;
    private Models.BrokerAccount _account;

	public BrokerAcccountPage(Models.OverviewSnapshot snapshot)
	{
		InitializeComponent();
		_snapshot = snapshot;
        _account = snapshot.BrokerAccount.Value.BrokerAccount;
        _broker = _account.Broker;

        SetupData();
	}

    private void SetupData()
    {
        Icon.ImagePath = _broker.Image;
        AccountName.Text = _account.AccountNumber;
    }

    protected override void StartLoad()
	{

	}

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        Navigation.PopModalAsync();
    }
}