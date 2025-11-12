using Binnaculum.Controls;
using Binnaculum.Core;

namespace Binnaculum.Popups;

public partial class OptionBuilderPopup
{
    private Models.Currency _currency;
    private Models.BrokerAccount _broker;
    private Models.Ticker _ticker;
    private decimal _multiplier;
    private DateTime _expiration = DateTime.Now;
    private decimal _strikePrice, _premium, _commissions, _fees = 0.0m;
    private int _quantity = 1;

    public OptionBuilderPopup(Models.Currency currency, 
        Models.BrokerAccount broker,
        Models.Ticker ticker,
        decimal multiplier,
        bool feesPerOperation)
	{
		InitializeComponent();

        FeeAndCommission.IsVisible = !feesPerOperation;
        FeeAndCommissionTitles.IsVisible = !feesPerOperation;
        FeesPerOperation.IsOn = feesPerOperation;

        ForceFillWidth();

        _currency = currency;
        _broker = broker;
        _ticker = ticker;
        _multiplier = multiplier;

        ExpirationDate.Events().DateSelected
            .Select(x => x.NewDate ?? DateTime.Now)
            .Subscribe(date =>
            {
                var newDate = new DateTime(date.Year, date.Month, date.Day, 23, 59, 59);
                _expiration = newDate;
            }).DisposeWith(Disposables);

        StrikePriceEntry.Events().TextChanged
            .Subscribe(changed =>
            {
                _strikePrice = changed.NewTextValue.ToMoney();
            }).DisposeWith(Disposables);

        QuantityEntry.Events().TextChanged
            .Subscribe(changed =>
            {
                if (int.TryParse(changed.NewTextValue, out var quantity) && quantity > 0)
                {
                    _quantity = quantity;
                }
            }).DisposeWith(Disposables);

        PremiumEntry.Events().TextChanged
            .Subscribe(changed =>
            {
                _premium = changed.NewTextValue.ToMoney();
            }).DisposeWith(Disposables);

        FeeAndCommission.Events().FeeAndCommissionChanged
            .Subscribe(feeAndCommission =>
            {
                _commissions = feeAndCommission.Commission;
                _fees = feeAndCommission.Fee;
            }).DisposeWith(Disposables);

        SaveOrDiscard.Events().SaveClicked
            .Subscribe(_ => Close(GetResult()))
            .DisposeWith(Disposables);

        SaveOrDiscard.Events().DiscardClicked
            .Subscribe(_ => Close())
            .DisposeWith(Disposables);        

        SetupOptionsType();
        SetupOptionsCode();
        SetupSaveActivation();
    }    

    private void SetupOptionsType()
    {
        var optionTypes = SelectableItem.OptionTypeList();
        var defaultType = optionTypes.Single(x => x.ItemValue == Models.OptionType.Call);
        SelectedOptionType.SelectableItem = defaultType;
        BindableLayout.SetItemsSource(OptionsTypeLayout, optionTypes);

        OptionTypeExpander.Events().ExpandedChanged
            .Select(_ => !OptionTypeExpander.IsExpanded)
            .BindTo(SelectedOptionType, x => x.IsVisible)
            .DisposeWith(Disposables);
    }

    private void SetupOptionsCode()
    {
        var optionCodes = SelectableItem.OptionCodeList();
        var defaultCode = optionCodes.Single(x => x.ItemValue == Models.OptionCode.SellToOpen);
        SelectedOptionCode.SelectableItem = defaultCode;
        BindableLayout.SetItemsSource(OptionsCodeLayout, optionCodes);

        OptionCodeExpander.Events().ExpandedChanged
            .Select(_ => !OptionCodeExpander.IsExpanded)
            .BindTo(SelectedOptionCode, x => x.IsVisible)
            .DisposeWith(Disposables);
    }

    private void SetupSaveActivation()
    {
        Observable.Merge(
            ExpirationDate.Events().DateSelected.Select(_ => Unit.Default),
            StrikePriceEntry.Events().TextChanged.Select(_ => Unit.Default),
            QuantityEntry.Events().TextChanged.Select(_ => Unit.Default),
            PremiumEntry.Events().TextChanged.Select(_ => Unit.Default),
            FeeAndCommission.Events().FeeAndCommissionChanged.Select(_ => Unit.Default))
        .Select(_ => GetResult() != null)
        .BindTo(SaveOrDiscard, x => x.IsButtonSaveEnabled)
        .DisposeWith(Disposables);
    }

    private Models.OptionTrade? GetResult()
    {
        if (_quantity < 1 || _strikePrice <= 0)
            return null;
        var optionCode = (Models.OptionCode)SelectedOptionCode.SelectableItem.ItemValue;

        if(optionCode.IsSellToClose 
            || optionCode.IsSellToOpen
            || optionCode.IsBuyToOpen
            || optionCode.IsBuyToClose)
        {
            if(_premium <= 0)
                return null;
        }

        var quantity = Convert.ToInt32(_quantity);
        var netPremium = _premium * _multiplier * _quantity;
        var totalCommissions = _commissions * _quantity;
        var totalFees = _fees * _quantity;
        var optionType = (Models.OptionType)SelectedOptionType.SelectableItem.ItemValue;
        
        if (optionCode.IsSellToClose || optionCode.IsSellToOpen)
            netPremium = netPremium - totalFees - totalCommissions;

        if (optionCode.IsBuyToClose || optionCode.IsBuyToOpen)
            netPremium = netPremium + totalFees + totalCommissions;

        var isOpen = optionCode == Models.OptionCode.SellToOpen || optionCode == Models.OptionCode.BuyToOpen;
        var notes = Microsoft.FSharp.Core.FSharpOption<string>.None;
        return new Models.OptionTrade(
            0,
            DateTime.Now,
            _expiration,
            _premium,
            netPremium,
            _ticker,
            _broker,
            _currency,
            optionType,
            optionCode,
            _strikePrice,
            totalCommissions,
            totalFees,
            isOpen,
            0,
            _multiplier,
            quantity,
            notes,
            FeesPerOperation.IsOn);
    }

    private void TypeSelected(object sender, SelectableItem selected)
    {
        SelectedOptionType.SelectableItem = selected;
        OptionTypeExpander.IsExpanded = false;
    }

    private void CodeSelected(object sender, SelectableItem selected)
    {
        SelectedOptionCode.SelectableItem = selected;
        OptionCodeExpander.IsExpanded = false;
    }
}