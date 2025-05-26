using Binnaculum.Controls;
using Binnaculum.Core;

namespace Binnaculum.Popups;

public partial class OptionBuilderPopup
{
    private Models.Currency _currency;
    private Models.BrokerAccount _broker;
    private Models.Ticker _ticker;
    
    public OptionBuilderPopup(Models.Currency currency, 
        Models.BrokerAccount broker,
        Models.Ticker ticker)
	{
		InitializeComponent();

        ForceFillWidth(Container);

        _currency = currency;
        _broker = broker;
        _ticker = ticker;

        SaveOrDiscard.Events().SaveClicked
            .Subscribe(_ => Close(GetResult()))
            .DisposeWith(Disposables);

        SaveOrDiscard.Events().DiscardClicked
            .Subscribe(_ => Close())
            .DisposeWith(Disposables);        

        SetupOptionsType();
        SetupOptionsCode();
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

    private Models.OptionTrade? GetResult()
    {
        return null;
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