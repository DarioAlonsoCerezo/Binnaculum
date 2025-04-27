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
            .Do(_ => SetUnselectedBroker())
            .Subscribe()
            .DisposeWith(Disposables);

        BrokerExpander.Events().ExpandedChanged
            .Select(x => x as ExpandedChangedEventArgs)
            .Where(x => x!.IsExpanded && SelectedBroker.IsVisible)
            .Do(_ => SetUnselectedBroker())
            .Subscribe()
            .DisposeWith(Disposables);

        SelectedBank.Events().BankSelected
            .Do(_ => SetUnselectedBank())
            .Subscribe()
            .DisposeWith(Disposables);

        BankExpander.Events().ExpandedChanged
            .Select(x => x as ExpandedChangedEventArgs)
            .Where(x => x!.IsExpanded && SelectedBank.IsVisible)
            .Do(_ => SetUnselectedBank())
            .Subscribe()
            .DisposeWith(Disposables);
    }

    private void SelectableBrokerControl_BrokerSelected(object sender, Core.Models.Broker e)
        => SetBrokerSelection(e);

    private void SetUnselectedBroker()
    {
        BrokerExpanderTitle.SetLocalizedText(ResourceKeys.AccountCreator_Select_Broker);
        BrokerAccountEntry.SetLocalizedText(ResourceKeys.AccountCreator_Select_Broker, BorderedEntry.PlaceholderProperty);
        SelectedBroker.IsVisible = false;
        BrokerExpander.IsExpanded = true;
        BrokerAccountEntry.IsEnabled = false;
    }

    private void SetBrokerSelection(Core.Models.Broker broker)
    {
        SelectedBroker.Broker = broker;
        BrokerExpanderTitle.SetLocalizedText(ResourceKeys.AccountCreator_Change_Selection);
        SelectedBroker.IsVisible = true;
        BrokerExpander.IsExpanded = false;
        BrokerAccountEntry.SetLocalizedText(ResourceKeys.AccountCreator_Creating_Account_For_Broker, BorderedEntry.PlaceholderProperty, broker.Name);
        BrokerAccountEntry.IsEnabled = true;
    }

    private void SelectableBankControl_BankSelected(object sender, Core.Models.Bank e)
        => SetBankSelection(e);

    private void SetUnselectedBank()
    {
        BankExpanderTitle.SetLocalizedText(ResourceKeys.AccountCreator_Select_Bank);
        BankAccountEntry.SetLocalizedText(ResourceKeys.AccountCreator_Select_Bank, BorderedEntry.PlaceholderProperty);
        SelectedBank.IsVisible = false;
        BankExpander.IsExpanded = true;
        BankAccountEntry.IsEnabled = false;
    }

    private void SetBankSelection(Core.Models.Bank bank)
    {
        SelectedBank.Bank = bank;
        BankExpanderTitle.SetLocalizedText(ResourceKeys.AccountCreator_Change_Selection);
        SelectedBank.IsVisible = true;
        BankExpander.IsExpanded = false;
        BankAccountEntry.SetLocalizedText(ResourceKeys.AccountCreator_Creating_Account_For_Bank, BorderedEntry.PlaceholderProperty, bank.Name);
        BankAccountEntry.IsEnabled = true;
    }
}