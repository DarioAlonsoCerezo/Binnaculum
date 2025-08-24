using Binnaculum.Pages;

namespace Binnaculum.Controls;

public partial class BrokerAccountTemplate
{
    private Core.Models.OverviewSnapshot? _snapshot;
    private Core.Models.BrokerAccount? _brokerAccount;
    private Core.Models.Broker? _broker;
    private bool _hasMovements = false;

    public BrokerAccountTemplate()
	{
		InitializeComponent();        
    }

    protected override void StartLoad()
    {
        
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (BindingContext is Core.Models.OverviewSnapshot snapshot)
        {
            Disposables?.Clear();
            _snapshot = snapshot;
            _brokerAccount = snapshot.BrokerAccount.Value.BrokerAccount;
            _broker = _brokerAccount.Broker;
            _hasMovements = snapshot.BrokerAccount.Value.Financial.MovementCounter > 0;

            SetupValues();
        }
    }

    private void SetupValues()
    {
        Icon.ImagePath = _broker!.Image;
        BrokerName.Text = _brokerAccount!.AccountNumber;
        
        AddMovementContainer.VerticalOptions = _hasMovements 
            ? LayoutOptions.End 
            : LayoutOptions.Center;

        AddMovementContainer.HorizontalOptions = _hasMovements
            ? LayoutOptions.Start 
            : LayoutOptions.Center;

        Add.Scale = _hasMovements ? 0.6 : 1;
        AddMovementContainer.Spacing = _hasMovements ? 0 : 12;
        Percentage.IsVisible = _hasMovements;

        Percentage.Percentage = _snapshot!.BrokerAccount.Value.Financial.RealizedPercentage;

        Observable
            .Merge(
                Add.Events().AddClicked.Select(_ => Unit.Default),
                AddMovementContainerGesture.Events().Tapped.Select(_ => Unit.Default),
                AddMovementTextGesture.Events().Tapped.Select(_ => Unit.Default))
            .Where(_ => _brokerAccount != null)
            .Select(async _ =>
            {
                await Navigation.PushModalAsync(new BrokerMovementCreatorPage(_brokerAccount!));
            })
            .Subscribe()
            .DisposeWith(Disposables);

        BrokerAccountGesture.Events().Tapped
            .Select(async _ =>
            {
                await Navigation.PushModalAsync(new BrokerAcccountPage());
            })
            .Subscribe()
            .DisposeWith(Disposables);
    }
}