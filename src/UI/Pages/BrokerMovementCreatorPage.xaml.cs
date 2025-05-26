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
        TradeMovement.BrokerAccount = account;
        DividendMovement.BrokerAccount = account;
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

        selection.Select(x =>
            {
                var hiderFeesAndCommissions = HideFeesAndCommissions(x);
                BrokerMovement.HideFeesAndCommissions = hiderFeesAndCommissions;
                if (x == Models.MovementType.Deposit
                    || x == Models.MovementType.Withdrawal
                    || x == Models.MovementType.ACATMoneyTransfer
                    || hiderFeesAndCommissions)                
                    return true;
                
                return false;
            })
            .BindTo(BrokerMovement, x => x.IsVisible)
            .DisposeWith(Disposables);

        selection.Select(x => x == Models.MovementType.Trade)
            .BindTo(TradeMovement, x => x.IsVisible)
            .DisposeWith(Disposables);

        selection.Select(x =>
            {
                if(x == Models.MovementType.DividendReceived
                    || x == Models.MovementType.DividendExDate
                    || x == Models.MovementType.DividendPayDate
                    || x == Models.MovementType.DividendTaxWithheld)
                {
                    if(x == Models.MovementType.DividendReceived)
                        DividendMovement.DividendEditor = DividenEditor.Received;
                    else if (x == Models.MovementType.DividendExDate)
                        DividendMovement.DividendEditor = DividenEditor.ExDividendDate;
                    else if (x == Models.MovementType.DividendPayDate)
                        DividendMovement.DividendEditor = DividenEditor.PayDate;
                    else if (x == Models.MovementType.DividendTaxWithheld)
                        DividendMovement.DividendEditor = DividenEditor.Taxes;
                    return true;
                }
                return false;
            })
            .BindTo(DividendMovement, x => x.IsVisible)
            .DisposeWith(Disposables);

        selection.Select(x => x == Models.MovementType.OptionTrade)
            .BindTo(OptionTradeMovement, x => x.IsVisible)
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

        TradeMovement.Events().TradeChanged
            .Where(_ => TradeMovement.IsVisible)
            .Select(x => x != null)
            .BindTo(Save, x => x.IsVisible)
            .DisposeWith(Disposables);

        Save.Events().SaveClicked
            .Where(_ => TradeMovement.IsVisible)
            .Select(_ => TradeMovement.Trade)
            .WhereNotNull()
            .CatchCoreError(Core.UI.Creator.SaveTrade)
            .Select(async _ => await Navigation.PopModalAsync())
            .Subscribe()
            .DisposeWith(Disposables);

        Observable.Merge(DividendMovement.Events().DividendChanged.Select(_ => Unit.Default),
                DividendMovement.Events().DividendDateChanged.Select(_ => Unit.Default),
                DividendMovement.Events().DividendTaxChanged.Select(_ => Unit.Default))
            .Where(_ => DividendMovement.IsVisible)
            .Select(_ =>
            {
                return DividendMovement.Dividend != null
                       || DividendMovement.DividendDate != null
                       || DividendMovement.DividendTax != null;
            })
            .BindTo(Save, x => x.IsVisible)
            .DisposeWith(Disposables);

        var saveDividend = Save.Events().SaveClicked
            .Where(_ => DividendMovement.IsVisible);

        saveDividend
            .Where(_ => DividendMovement.DividendEditor == DividenEditor.Received)
            .Select(_ => DividendMovement.Dividend)
            .WhereNotNull()
            .CatchCoreError(Core.UI.Creator.SaveDividend)
            .Select(async _ => await Navigation.PopModalAsync())
            .Subscribe()
            .DisposeWith(Disposables);

        saveDividend
            .Where(_ => DividendMovement.DividendEditor == DividenEditor.ExDividendDate
                        || DividendMovement.DividendEditor == DividenEditor.PayDate)
            .Select(_ => DividendMovement.DividendDate)
            .WhereNotNull()
            .CatchCoreError(Core.UI.Creator.SaveDividendDate)
            .Select(async _ => await Navigation.PopModalAsync())
            .Subscribe()
            .DisposeWith(Disposables);

        saveDividend
            .Where(_ => DividendMovement.DividendEditor == DividenEditor.Taxes)
            .Select(_ => DividendMovement.DividendTax)
            .WhereNotNull()
            .CatchCoreError(Core.UI.Creator.SaveDividendTax)
            .Select(async _ => await Navigation.PopModalAsync())
            .Subscribe()
            .DisposeWith(Disposables);
    }

    private Models.BrokerMovement? GetBrokerMovement(Models.MovementType? movementType)
    {
        var brokerMovementType = Core.UI.Creator.GetBrokerMovementType(movementType);
        if (brokerMovementType == null)
            return null;

        var notes = string.IsNullOrWhiteSpace(BrokerMovement.DepositData.Note)
            ? Microsoft.FSharp.Core.FSharpOption<string>.None
            : Microsoft.FSharp.Core.FSharpOption<string>.Some(BrokerMovement.DepositData.Note!);

        return new Models.BrokerMovement(
                            0,
                            BrokerMovement.DepositData.TimeStamp,
                            BrokerMovement.DepositData.Amount,
                            Core.UI.Collections.GetCurrency(BrokerMovement.DepositData.Currency),
                            _account,
                            BrokerMovement.DepositData.Commissions,
                            BrokerMovement.DepositData.Fees,
                            brokerMovementType.Value,
                            notes);
    }

    private bool HideFeesAndCommissions(Models.MovementType movementType)
    {
        if (movementType.IsFee)
            return true;

        if (movementType.IsInterestsGained)
            return true;

        if (movementType.IsLending)
            return true;

        if(movementType.IsInterestsGained)
            return true;

        if(movementType.IsInterestsPaid)
            return true;

        return false;
    }
}