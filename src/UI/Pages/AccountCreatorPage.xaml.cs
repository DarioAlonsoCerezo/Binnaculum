namespace Binnaculum.Pages;

public partial class AccountCreatorPage
{
	public AccountCreatorPage()
	{
		InitializeComponent();
    }    

    protected override void StartLoad()
    {
        ButtonSaveOrDiscard.Events().SaveClicked
            .Select(async _ => await Navigation.PopModalAsync())
            .Subscribe();

        ButtonSaveOrDiscard.Events().DiscardClicked
            .Select(async _ => await Navigation.PopModalAsync())
            .Subscribe();

        SelectableBroker.Broker = new Core.Models.Broker(0, "Tastytrade", "tastytrade", Core.Models.SupportedBroker.Tastytrade);

    }
}
