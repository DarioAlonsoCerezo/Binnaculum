using Binnaculum.Core.UI;

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
            .Do(x =>
            {
                ExpanderTitle.IsVisible = true;
                SelectedBroker.IsVisible = false;
                BrokerExpander.IsExpanded = true;
            })
            .Subscribe()
            .DisposeWith(Disposables);
    }

    private void SetSelection(Core.Models.Broker broker)
    {
        SelectedBroker.Broker = broker;
        ExpanderTitle.IsVisible = false;
        SelectedBroker.IsVisible = true;
        BrokerExpander.IsExpanded = false;
    }
}
