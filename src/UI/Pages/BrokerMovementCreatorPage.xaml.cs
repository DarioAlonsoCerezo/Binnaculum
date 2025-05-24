using Binnaculum.Controls;
using Binnaculum.Core;
using Binnaculum.Popups;

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

        Deposit.Events().DepositChanged
            .Where(_ => Deposit.IsVisible)
            .Select(x => x.Amount > 0)
            .BindTo(Save, x => x.IsVisible)
            .DisposeWith(Disposables);

        var savingMovement = Save.Events().SaveClicked
            .Where(_ => MovementTypeControl.SelectedItem != null)
            .Where(_ => MovementTypeControl.SelectedItem!.ItemValue is Models.MovementType movement)
            .Select(_ => MovementTypeControl.SelectedItem!.ItemValue as Models.MovementType);

        savingMovement.Where(movement => movement == Models.MovementType.Deposit)
            .Select(_ => GetBrokerMovement())
            .CatchCoreError(Core.UI.Creator.SaveBrokerMovement)
            .Select(async _ => await Navigation.PopModalAsync())
            .Subscribe()
            .DisposeWith(Disposables);
    }

    private Models.BrokerMovement GetBrokerMovement()
    {
        return new Models.BrokerMovement(
                            0,
                            Deposit.DepositData.TimeStamp,
                            Deposit.DepositData.Amount,
                            Core.UI.Collections.GetCurrency(Deposit.DepositData.Currency),
                            _account,
                            Deposit.DepositData.Commissions,
                            Deposit.DepositData.Fees,
                            Models.BrokerMovementType.Deposit);
    }
}