using Binnaculum.Controls;
using Binnaculum.Core;
using Binnaculum.Core.UI;
using Binnaculum.Core.Import;
using Binnaculum.Core.Utilities;
using Microsoft.Maui.Storage;
using static Binnaculum.Core.Models;

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
        OptionTradeMovement.BrokerAccount = account;
        //By default, it should be created manually
        ManualRadioButton.IsChecked = true;
        // Broker support detection - Enable FromFileRadioButton for supported brokers only
        var isSupportedBroker = _account.Broker.SupportedBroker == SupportedBroker.IBKR ||
                               _account.Broker.SupportedBroker == SupportedBroker.Tastytrade;
        FromFileRadioButton.IsEnabled = isSupportedBroker;
    }

    protected override void StartLoad()
    {
        CloseGesture.Events().Tapped
            .Select(async _ => await Navigation.PopModalAsync())
            .Subscribe()
            .DisposeWith(Disposables);

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
                BrokerMovement.ShowCurrency = true;
                BrokerMovement.ShowTicker = false;
                BrokerMovement.HideAmount = x == Models.MovementType.Conversion;
                if (x == Models.MovementType.ACATSecuritiesTransferReceived
                    || x == Models.MovementType.ACATSecuritiesTransferSent)
                {
                    BrokerMovement.ShowTicker = true;
                    BrokerMovement.ShowCurrency = false;
                }
                if (x == Models.MovementType.Deposit
                    || x == Models.MovementType.Withdrawal
                    || x == Models.MovementType.ACATMoneyTransferSent
                    || x == Models.MovementType.ACATMoneyTransferReceived
                    || x == Models.MovementType.ACATSecuritiesTransferSent
                    || x == Models.MovementType.ACATSecuritiesTransferReceived
                    || x == Models.MovementType.Conversion
                    || hiderFeesAndCommissions)
                    return true;

                return false;
            })
            .ObserveOn(UiThread)
            .BindTo(BrokerMovement, x => x.IsVisible)
            .DisposeWith(Disposables);

        selection.Select(x => x == Models.MovementType.Trade)
            .ObserveOn(UiThread)
            .BindTo(TradeMovement, x => x.IsVisible)
            .DisposeWith(Disposables);

        selection.Select(x =>
            {
                if (x == Models.MovementType.DividendReceived
                    || x == Models.MovementType.DividendExDate
                    || x == Models.MovementType.DividendPayDate
                    || x == Models.MovementType.DividendTaxWithheld)
                {
                    if (x == Models.MovementType.DividendReceived)
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
            .ObserveOn(UiThread)
            .BindTo(DividendMovement, x => x.IsVisible)
            .DisposeWith(Disposables);

        selection.Select(x => x == Models.MovementType.OptionTrade)
            .ObserveOn(UiThread)
            .BindTo(OptionTradeMovement, x => x.IsVisible)
            .DisposeWith(Disposables);

        BrokerMovement.Events().DepositChanged
            .Where(_ => BrokerMovement.IsVisible)
            .Select(_ => BrokerMovement.DepositData.Amount > 0)
            .ObserveOn(UiThread)
            .BindTo(Save, x => x.IsVisible)
            .DisposeWith(Disposables);

        BrokerMovement.Events().ConversionChanged
            .Where(_ => BrokerMovement.IsVisible)
            .Select(_ => BrokerMovement.ConversionData.AmountTo > 0 && BrokerMovement.ConversionData.AmountFrom > 0)
            .ObserveOn(UiThread)
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
            .DoAsync(Navigation.PopModalAsync)
            .Subscribe()
            .DisposeWith(Disposables);

        TradeMovement.Events().TradeChanged
            .Where(_ => TradeMovement.IsVisible)
            .Select(x => x != null)
            .ObserveOn(UiThread)
            .BindTo(Save, x => x.IsVisible)
            .DisposeWith(Disposables);

        Save.Events().SaveClicked
            .Where(_ => TradeMovement.IsVisible)
            .Select(_ => TradeMovement.Trade)
            .WhereNotNull()
            .CatchCoreError(Core.UI.Creator.SaveTrade)
            .DoAsync(Navigation.PopModalAsync)
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
            .ObserveOn(UiThread)
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

        OptionTradeMovement.Events().OptionTradesChanged
            .Where(_ => OptionTradeMovement.IsVisible)
            .Select(x => x != null && x.Count > 0)
            .ObserveOn(UiThread)
            .BindTo(Save, x => x.IsVisible)
            .DisposeWith(Disposables);

        Save.Events().SaveClicked
            .Where(_ => OptionTradeMovement.IsVisible)
            .Select(_ => OptionTradeMovement.Trades)
            .WhereNotNull()
            .CatchCoreError(Core.UI.Creator.SaveOptionsTrade)
            .Select(async _ => await Navigation.PopModalAsync())
            .Subscribe()
            .DisposeWith(Disposables);

        // Handle radio button selection
        FromFileRadioButton.Events().CheckedChanged
            .Where(isChecked => isChecked)
            .ObserveOn(UiThread)
            .Do(_ => ManualRadioButton.IsChecked = false)
            .Subscribe(_ =>
            {
                FileImportSection.IsVisible = true;
                // Hide manual movement controls
                MovementTypeControl.IsVisible = false;
                BrokerMovement.IsVisible = false;
                TradeMovement.IsVisible = false;
                DividendMovement.IsVisible = false;
                OptionTradeMovement.IsVisible = false;
                Save.IsVisible = false;
            })
            .DisposeWith(Disposables);

        // Handle manual selection
        ManualRadioButton.Events().CheckedChanged
            .Where(isChecked => isChecked)
            .ObserveOn(UiThread)
            .Do(_ => FromFileRadioButton.IsChecked = false)
            .Subscribe(_ =>
            {
                FileImportSection.IsVisible = false;
                MovementTypeControl.IsVisible = true;
                // Reset import results
                ImportResults.IsVisible = false;
                ImportProgress.IsVisible = false;
            })
            .DisposeWith(Disposables);

        // Handle file selection and import
        SelectFileButton.Events().Clicked
            .ObserveOn(UiThread)
            .SelectMany(_ => FilePickerService.pickDataFileAsync("Testing"))
            .Where(x => x != null)
            .ObserveOn(UiThread)
            .Do(_ => ShowImportProgress())
            .Delay(TimeSpan.FromMilliseconds(300)) // Small delay to ensure progress UI is visible
            .ObserveOn(BackgroundScheduler)
            .CatchCoreError(file => ImportManager.importFile(_account.Broker.Id, _account.Id, file!.FilePath))
            .ObserveOn(UiThread)
            .Do(HandleImportResult)
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


        if (movementType == Models.MovementType.Conversion)
            return GetConversion(movementType, brokerMovementType, notes);

        if (movementType == Models.MovementType.ACATSecuritiesTransferReceived
            || movementType == Models.MovementType.ACATSecuritiesTransferSent)
            return GetACAT(movementType, brokerMovementType, notes);

        return new Models.BrokerMovement(
                            0,
                            BrokerMovement.DepositData.TimeStamp,
                            BrokerMovement.DepositData.Amount,
                            BrokerMovement.DepositData.Currency.ToFastCurrency(),
                            _account,
                            BrokerMovement.DepositData.Commissions,
                            BrokerMovement.DepositData.Fees,
                            brokerMovementType.Value,
                            notes,
                            Microsoft.FSharp.Core.FSharpOption<Models.Currency>.None,
                            Microsoft.FSharp.Core.FSharpOption<decimal>.None,
                            Microsoft.FSharp.Core.FSharpOption<Models.Ticker>.None,
                            Microsoft.FSharp.Core.FSharpOption<decimal>.None);
    }

    private Models.BrokerMovement? GetConversion(Models.MovementType? movementType,
        Microsoft.FSharp.Core.FSharpOption<BrokerMovementType> brokerMovementType,
        Microsoft.FSharp.Core.FSharpOption<string> notes)
    {
        return new Models.BrokerMovement(
                            0,
                            BrokerMovement.DepositData.TimeStamp,
                            BrokerMovement.ConversionData.AmountTo,
                            BrokerMovement.ConversionData.CurrencyTo.ToFastCurrency(),
                            _account,
                            BrokerMovement.ConversionData.Commissions,
                            BrokerMovement.ConversionData.Fees,
                            brokerMovementType.Value,
                            notes,
                            BrokerMovement.ConversionData.CurrencyFrom.ToFastCurrency(),
                            BrokerMovement.ConversionData.AmountFrom,
                            Microsoft.FSharp.Core.FSharpOption<Models.Ticker>.None,
                            Microsoft.FSharp.Core.FSharpOption<decimal>.None);
    }

    private Models.BrokerMovement? GetACAT(Models.MovementType? movementType,
        Microsoft.FSharp.Core.FSharpOption<BrokerMovementType> brokerMovementType,
        Microsoft.FSharp.Core.FSharpOption<string> notes)
    {
        return new Models.BrokerMovement(
                            0,
                            BrokerMovement.DepositData.TimeStamp,
                            0m,
                            SavedPrefereces.UserPreferences.Value.Currency.ToFastCurrency(),
                            _account,
                            BrokerMovement.ACATData.Commissions,
                            BrokerMovement.ACATData.Fees,
                            brokerMovementType.Value,
                            notes,
                            Microsoft.FSharp.Core.FSharpOption<Models.Currency>.None,
                            Microsoft.FSharp.Core.FSharpOption<decimal>.None,
                            BrokerMovement.ACATData.Ticker,
                            BrokerMovement.ACATData.Quantity);
    }

    private bool HideFeesAndCommissions(Models.MovementType movementType)
    {
        if (movementType.IsFee)
            return true;

        if (movementType.IsInterestsGained)
            return true;

        if (movementType.IsLending)
            return true;

        if (movementType.IsInterestsGained)
            return true;

        if (movementType.IsInterestsPaid)
            return true;

        return false;
    }

    private void ShowImportProgress()
    {
        ImportProgress.IsVisible = true;
        ImportProgress.IsRunning = true;
        ImportResults.IsVisible = false;
        SelectFileButton.IsEnabled = false;
    }

    private void HandleImportResult(ImportResult result)
    {
        ImportProgress.IsVisible = false;
        ImportProgress.IsRunning = false;
        ImportResults.IsVisible = true;
        SelectFileButton.IsEnabled = true;

        if (result.Success)
        {
            ImportStatusLabel.Text = $"Import completed successfully";
            ImportDetailsLabel.Text = $"Imported {result.ProcessedRecords} transactions";
        }
        else
        {
            ImportStatusLabel.Text = "Import failed";
            var errorMessages = result.Errors.Select(e => e.ErrorMessage).Take(3);
            ImportDetailsLabel.Text = string.Join(", ", errorMessages);
        }
    }

    private void HandleImportError(Exception error)
    {
        ImportProgress.IsVisible = false;
        ImportProgress.IsRunning = false;
        ImportResults.IsVisible = true;
        SelectFileButton.IsEnabled = true;

        ImportStatusLabel.Text = "Import failed";
        ImportDetailsLabel.Text = error.Message;
    }
}