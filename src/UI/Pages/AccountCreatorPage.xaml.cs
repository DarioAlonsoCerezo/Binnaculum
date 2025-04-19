using Binnaculum.Controls;
using Binnaculum.Core.UI;
using CommunityToolkit.Maui.Core;

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

        var ibkr = Collections.Brokers.Items.Single(x => x.SupportedBroker == "Interactive Brokers");
        var tasty = Collections.Brokers.Items.Single(x => x.SupportedBroker == "Tastytrade");
        if (ibkr != null)
            IBKR.Broker = ibkr;
        if (tasty != null)
            Tastytrade.Broker = tasty;

        Tastytrade.Events().BrokerSelected
            .Do(SetSelection)
            .Subscribe()
            .DisposeWith(Disposables);

        IBKR.Events().BrokerSelected
            .Do(SetSelection)
            .Subscribe()
            .DisposeWith(Disposables);

        SelectedBroker.Events().BrokerSelected
            .Do(_ => SetUnselected())
            .Subscribe()
            .DisposeWith(Disposables);

        BrokerExpander.Events().ExpandedChanged
            .Select(x => x as ExpandedChangedEventArgs)
            .Where(x => x!.IsExpanded && SelectedBroker.IsVisible)
            .Do(_ => SetUnselected())
            .Subscribe()
            .DisposeWith(Disposables);
    }

    private void SetUnselected()
    {
        ExpanderTitle.SetLocalizedText(ResourceKeys.SelectBroker);
        BrokerAccountEntry.SetLocalizedText(ResourceKeys.SelectBroker, BorderedEntry.PlaceholderProperty);
        SelectedBroker.IsVisible = false;
        SelectedBroker.IsVisible = false;
        BrokerExpander.IsExpanded = true;
        BrokerAccountEntry.IsEnabled = false;
    }

    private void SetSelection(Core.Models.Broker broker)
    {
        SelectedBroker.Broker = broker;
        ExpanderTitle.SetLocalizedText(ResourceKeys.SelectBrokerChange);
        SelectedBroker.IsVisible = true;
        BrokerExpander.IsExpanded = false;
        BrokerAccountEntry.SetLocalizedText(ResourceKeys.CreatingAccountForBroker, BorderedEntry.PlaceholderProperty, broker.Name);
        BrokerAccountEntry.IsEnabled = true;
    }
}
