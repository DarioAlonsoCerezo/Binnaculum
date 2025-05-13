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

        MovementTypeControl.ItemsSource = SelectableItem.BrokerMovementTypeList();
    }

    protected override void StartLoad()
    {
        CloseGesture.Events().Tapped
            .Select(async _ => await Navigation.PopModalAsync())
            .Subscribe()
            .DisposeWith(Disposables);

        //By default, it should be created manually
        ManualRadioButton.IsChecked = true;

        var selection = MovementTypeControl.Events()
            .ItemSelected
            .Select(x =>
            {
                if (x.ItemValue is Models.MovementType movement)
                    return movement;
                return null;
            }).WhereNotNull();

        selection.Select(x => x == Models.MovementType.Deposit || x == Models.MovementType.Withdrawal)
            .BindTo(Deposit, x => x.IsVisible)
            .DisposeWith(Disposables);
    }
}