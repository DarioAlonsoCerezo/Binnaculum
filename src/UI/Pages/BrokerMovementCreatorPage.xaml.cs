using Binnaculum.Core;

namespace Binnaculum.Pages;

public partial class BrokerMovementCreatorPage
{
    private readonly Models.BrokerAccount _account;

	public BrokerMovementCreatorPage(Models.BrokerAccount account)
	{
		InitializeComponent();
        _account = account;
    }

    protected override void StartLoad()
    {

    }
}