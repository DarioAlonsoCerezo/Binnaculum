using Binnaculum.Controls;
using Binnaculum.Core.UI;
using CommunityToolkit.Maui.Core;

namespace Binnaculum.Pages;

public partial class AccountCreatorPage
{
    private ReadOnlyObservableCollection<Core.Models.Broker> _brokers;
    public ReadOnlyObservableCollection<Core.Models.Broker> Brokers => _brokers;
    
    public AccountCreatorPage()
	{
		InitializeComponent();

        Collections.Brokers.Connect()
            .ObserveOn(UiThread)
            .Bind(out _brokers)
            .Subscribe()
            .DisposeWith(Disposables);

        BindableLayout.SetItemsSource(BrokersLayout, Brokers);
    }    

    protected override void StartLoad()
    {
        ButtonSaveOrDiscard.Events().SaveClicked
            .Select(async _ => await Navigation.PopModalAsync())
            .Subscribe();

        ButtonSaveOrDiscard.Events().DiscardClicked
            .Select(async _ => await Navigation.PopModalAsync())
            .Subscribe();

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

    private void SelectableBrokerControl_BrokerSelected(object sender, Core.Models.Broker e)
        => SetSelection(e);

    private void SetUnselected()
    {
        ExpanderTitle.SetLocalizedText(ResourceKeys.AccountCreator_Select_Broker);
        BrokerAccountEntry.SetLocalizedText(ResourceKeys.AccountCreator_Select_Broker, BorderedEntry.PlaceholderProperty);
        SelectedBroker.IsVisible = false;
        SelectedBroker.IsVisible = false;
        BrokerExpander.IsExpanded = true;
        BrokerAccountEntry.IsEnabled = false;
    }

    private void SetSelection(Core.Models.Broker broker)
    {
        SelectedBroker.Broker = broker;
        ExpanderTitle.SetLocalizedText(ResourceKeys.AccountCreator_Change_Selection);
        SelectedBroker.IsVisible = true;
        BrokerExpander.IsExpanded = false;
        BrokerAccountEntry.SetLocalizedText(ResourceKeys.AccountCreator_Creating_Account_For_Broker, BorderedEntry.PlaceholderProperty, broker.Name);
        BrokerAccountEntry.IsEnabled = true;
    }
}
