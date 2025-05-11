using Binnaculum.Controls;
using Binnaculum.Core;
using Binnaculum.Core.UI;
using Binnaculum.Popups;
using CommunityToolkit.Maui.Core;

namespace Binnaculum.Pages;

public partial class AccountCreatorPage
{
    private readonly ReadOnlyObservableCollection<Core.Models.Broker> _brokers;
    public ReadOnlyObservableCollection<Core.Models.Broker> Brokers => _brokers;

    private readonly ReadOnlyObservableCollection<Core.Models.Bank> _banks;
    public ReadOnlyObservableCollection<Core.Models.Bank> Banks => _banks;

    public AccountCreatorPage()
	{
		InitializeComponent();

        Collections.Brokers.Connect()
            .Sort(SortExpressionComparer<Core.Models.Broker>
                .Descending(b => b.Id < 0)
                .ThenByAscending(b => b.Name))
            .ObserveOn(UiThread)
            .Bind(out _brokers)
            .Subscribe()
            .DisposeWith(Disposables);

        Collections.Banks.Connect()
            .Sort(SortExpressionComparer<Core.Models.Bank>
                .Descending(b => b.Id < 0)
                .ThenByAscending(b => b.Name))
            .ObserveOn(UiThread)
            .Bind(out _banks)
            .Subscribe()
            .DisposeWith(Disposables);

        BindableLayout.SetItemsSource(BrokersLayout, Brokers);
        BindableLayout.SetItemsSource(BanksLayout, Banks);
    }    

    protected override void StartLoad()
    {
        ButtonSaveOrDiscard.Events().SaveClicked
            .Select(async _ =>
            {
                await SaveAccounts();
                await Navigation.PopModalAsync();
            })
            .Subscribe()
            .DisposeWith(Disposables);

        ButtonSaveOrDiscard.Events().DiscardClicked
            .Select(async _ => await Navigation.PopModalAsync())
            .Subscribe()
            .DisposeWith(Disposables);

        CloseGesture.Events().Tapped
            .Select(async _ => await Navigation.PopModalAsync())
            .Subscribe()
            .DisposeWith(Disposables);

        SelectedBroker.Events().BrokerSelected
            .Select(async _ => await UnfocusEntries())
            .Do(_ => SetUnselectedBroker())
            .Subscribe()
            .DisposeWith(Disposables);

        BrokerExpander.Events().ExpandedChanged
            .Select(x => x as ExpandedChangedEventArgs)
            .Where(x => x!.IsExpanded && SelectedBroker.IsVisible)
            .Select(async _ => await UnfocusEntries())
            .Do(_ => SetUnselectedBroker())
            .Subscribe()
            .DisposeWith(Disposables);

        SelectedBank.Events().BankSelected
            .Select(async _ => await UnfocusEntries())
            .Do(_ => SetUnselectedBank())
            .Subscribe()
            .DisposeWith(Disposables);

        BankExpander.Events().ExpandedChanged
            .Select(x => x as ExpandedChangedEventArgs)
            .Where(x => x!.IsExpanded && SelectedBank.IsVisible)
            .Select(async _ => await UnfocusEntries())
            .Do(_ => SetUnselectedBank())
            .Subscribe()
            .DisposeWith(Disposables);

        BrokerAccountEntry.Events().TextChanged
            .CombineLatest(BrokerAccountEntry.Events().TextChanged)
            .Where(_ => SelectedBroker.IsVisible || SelectedBank.IsVisible)
            .Select(_ => (SelectedBroker.Broker, SelectedBank.Bank, BrokerAccountEntry.Text, BankAccountEntry.Text))
            .ObserveOn(UiThread)
            .Select(CheckActiveButton)
            .BindTo(ButtonSaveOrDiscard, x => x.IsButtonSaveEnabled)
            .DisposeWith(Disposables);
    }

    private bool CheckActiveButton((Models.Broker? broker, Models.Bank? bank, string? brokerAccountName, string? bankAccountName) selection)
    {
        if (selection.broker != null || selection.bank != null)
        {
            if (!string.IsNullOrWhiteSpace(selection.brokerAccountName) || !string.IsNullOrWhiteSpace(selection.bankAccountName))
            {
                if (selection.brokerAccountName?.Length > 2 || selection.bankAccountName?.Length > 2)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private async void SelectableBrokerControl_BrokerSelected(object _, Core.Models.Broker broker)
    {
        await UnfocusEntries();
        SetBrokerSelection(broker);
    }

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
        if(broker.Id < 0)
        {
            new BrokerCreatorPopup().Show();
            return;
        }
        SelectedBroker.Broker = broker;
        BrokerExpanderTitle.SetLocalizedText(ResourceKeys.AccountCreator_Change_Selection);
        SelectedBroker.IsVisible = true;
        BrokerExpander.IsExpanded = false;
        BrokerAccountEntry.SetLocalizedText(ResourceKeys.AccountCreator_Creating_Account_For_Broker, BorderedEntry.PlaceholderProperty, broker.Name);
        BrokerAccountEntry.IsEnabled = true;
    }

    private async void SelectableBankControl_BankSelected(object _, Core.Models.Bank bank)
    {
        await UnfocusEntries();
        SetBankSelection(bank);
    }

    private void SetUnselectedBank()
    {
        BankExpanderTitle.SetLocalizedText(ResourceKeys.AccountCreator_Select_Bank);
        BankAccountEntry.SetLocalizedText(ResourceKeys.AccountCreator_Select_Bank, BorderedEntry.PlaceholderProperty);
        SelectedBank.IsVisible = false;
        BankExpander.IsExpanded = true;
        BankAccountEntry.IsEnabled = false;
        BankAccountEntry.IsCurrencyVisible = false;
    }

    private void SetBankSelection(Core.Models.Bank bank)
    {
        if (bank.Id < 0)
        {
            new BankCreatorPopup().Show();
            return; 
        }
        SelectedBank.Bank = bank;
        BankExpanderTitle.SetLocalizedText(ResourceKeys.AccountCreator_Change_Selection);
        SelectedBank.IsVisible = true;
        BankExpander.IsExpanded = false;
        BankAccountEntry.SetLocalizedText(ResourceKeys.AccountCreator_Creating_Account_For_Bank, BorderedEntry.PlaceholderProperty, bank.Name);
        BankAccountEntry.IsCurrencyVisible = true;
        BankAccountEntry.IsEnabled = true;
    }

    private async Task UnfocusEntries()
    {
        await BankAccountEntry.Unfocus(hideKeyboard: true);
        await BrokerAccountEntry.Unfocus(hideKeyboard: true);
    }

    private async Task SaveAccounts()
    {
        if(SelectedBroker.IsVisible && BrokerAccountEntry.Text?.Length > 2)
        {
            await Creator.SaveBrokerAccount(SelectedBroker.Broker.Id, BrokerAccountEntry.Text);
        }

        if(SelectedBank.IsVisible && BankAccountEntry.Text?.Length > 2)
        {
            var currency = Collections.Currencies.Items.Single(x => x.Code.Equals(BankAccountEntry.SelectedCurrency));
            await Creator.SaveBankAccount(SelectedBank.Bank.Id, BankAccountEntry.Text, currency.Id);
        }
    }
}