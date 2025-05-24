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
            .BindTo(BrokerMovement, x => x.IsVisible)
            .DisposeWith(Disposables);

        BrokerMovement.Events().DepositChanged
            .Where(_ => BrokerMovement.IsVisible)
            .Select(x => x.Amount > 0)
            .BindTo(Save, x => x.IsVisible)
            .DisposeWith(Disposables);

        var savingMovement = Save.Events().SaveClicked
            .Where(_ => MovementTypeControl.SelectedItem != null)
            .Where(_ => MovementTypeControl.SelectedItem!.ItemValue is Models.MovementType movement)
            .Select(_ => MovementTypeControl.SelectedItem!.ItemValue as Models.MovementType);

        savingMovement
            .Select(GetBrokerMovement)
            .WhereNotNull()
            .CatchCoreError(Core.UI.Creator.SaveBrokerMovement)
            .Select(async _ => await Navigation.PopModalAsync())
            .Subscribe()
            .DisposeWith(Disposables);
    }

    private Models.BrokerMovement? GetBrokerMovement(Models.MovementType? movementType)
    {
        var brokerMovementType = Core.UI.Creator.GetBrokerMovementType(movementType);
        if (brokerMovementType == null)
            return null;

        return new Models.BrokerMovement(
                            0,
                            BrokerMovement.DepositData.TimeStamp,
                            BrokerMovement.DepositData.Amount,
                            Core.UI.Collections.GetCurrency(BrokerMovement.DepositData.Currency),
                            _account,
                            BrokerMovement.DepositData.Commissions,
                            BrokerMovement.DepositData.Fees,
                            brokerMovementType.Value,
                            null); // Notes parameter is null by default
    }
}