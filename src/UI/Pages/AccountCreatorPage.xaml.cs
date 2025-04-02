using System.Reactive.Concurrency;

namespace Binnaculum.Pages;

public partial class AccountCreatorPage : ContentPage
{
	public AccountCreatorPage()
	{
		InitializeComponent();

		SaveButton.Events().Clicked
			.Select(async _ => await Navigation.PopModalAsync())
			.Subscribe();

		DiscardLabel.Events().Tapped
            .Select(async _ => await Navigation.PopModalAsync())
            .Subscribe();
    }

    protected override bool OnBackButtonPressed()
    {
        return true;
    }
}
