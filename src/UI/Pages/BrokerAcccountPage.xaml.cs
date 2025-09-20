using Binnaculum.Core;
using System.Reactive.Subjects;

namespace Binnaculum.Pages;

public partial class BrokerAcccountPage
{
    private Models.OverviewSnapshot _snapshot;
    private Models.Broker _broker;
    private Models.BrokerAccount _account;

    private ReadOnlyObservableCollection<Models.Movement> _movements;
    public ReadOnlyObservableCollection<Models.Movement> Movements => _movements;
    IObservable<Func<Models.Movement, bool>> _filterPredicate;
    BehaviorSubject<Models.OverviewSnapshot?> _selected = new(null);

    public BrokerAcccountPage(Models.OverviewSnapshot snapshot)
    {
        InitializeComponent();
        _snapshot = snapshot;
        _account = _snapshot.BrokerAccount.Value.BrokerAccount;
        _broker = _account.Broker;

        _filterPredicate = _selected
            .Throttle(TimeSpan.FromMilliseconds(300))
            .Select(BuildFilterPredicate)
            .ObserveOn(UiThread);

        Core.UI.Collections.Movements.Connect()
            .Filter(_filterPredicate)
            .Sort(SortExpressionComparer<Models.Movement>.Descending(m => m.TimeStamp))
            .ObserveOn(UiThread)
            .Bind(out _movements)
            .Subscribe();

        SetupData();
    }

    private void SetupData()
    {
        Icon.ImagePath = _broker.Image;
        AccountName.Text = _account.AccountNumber;
    }

    protected override void StartLoad()
    {

    }

    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        await PopModal();
    }

    private Func<Models.Movement, bool> BuildFilterPredicate(Models.OverviewSnapshot? selected)
    {
        if (selected == null)
            return _ => true;

        if (selected.Type.IsBankAccount)
        {
            return x =>
            {
                if (x.Type.IsBankAccountMovement)
                    return x.BankAccountMovement.Value.BankAccount.Id.Equals(selected.Bank.Value.Bank.Id);

                return false;
            };
        }


        return x =>
        {
            if (x.Type.IsBankAccountMovement)
                return false;

            if (x.Type.IsBrokerMovement)
                return x.BrokerMovement.Value.BrokerAccount.Id.Equals(selected.BrokerAccount.Value.BrokerAccount.Id);

            if (x.Type.IsTrade)
                return x.Trade.Value.BrokerAccount.Id.Equals(selected.BrokerAccount.Value.BrokerAccount.Id);

            if (x.Type.IsDividend)
                return x.Dividend.Value.BrokerAccount.Id.Equals(selected.BrokerAccount.Value.BrokerAccount.Id);

            if (x.Type.IsDividendDate)
                return x.DividendDate.Value.BrokerAccount.Id.Equals(selected.BrokerAccount.Value.BrokerAccount.Id);

            if (x.Type.IsDividendTax)
                return x.DividendTax.Value.BrokerAccount.Id.Equals(selected.BrokerAccount.Value.BrokerAccount.Id);

            if (x.Type.IsOptionTrade)
                return x.OptionTrade.Value.BrokerAccount.Id.Equals(selected.BrokerAccount.Value.BrokerAccount.Id);

            return false;
        };
    }
}