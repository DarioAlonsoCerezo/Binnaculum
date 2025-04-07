using System.Reactive.Concurrency;

namespace Binnaculum.Pages;

public partial class AccountCreatorPage : ContentPage
{
	public AccountCreatorPage()
	{
		InitializeComponent();

		ButtonSaveOrDiscard.Events().SaveClicked
			.Select(async _ => await Navigation.PopModalAsync())
			.Subscribe();

        ButtonSaveOrDiscard.Events().DiscardClicked
            .Select(async _ => await Navigation.PopModalAsync())
            .Subscribe();

        SelectableBroker.Broker = new Core.Models.Broker(0, "Tastytrade", "tastytrade", Core.Models.SupportedBroker.Tastytrade);
    }

    protected override bool OnBackButtonPressed()
    {
        return true;
    }
}
