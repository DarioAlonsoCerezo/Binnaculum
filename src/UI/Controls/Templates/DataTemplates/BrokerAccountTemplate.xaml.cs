using Binnaculum.Pages;

namespace Binnaculum.Controls;

public partial class BrokerAccountTemplate
{
    private Core.Models.Account? _account;
    private Core.Models.BrokerAccount? _brokerAccount;
    private Core.Models.Broker? _broker;

    public BrokerAccountTemplate()
	{
		InitializeComponent();        
    }

    protected override void StartLoad()
    {
        AddMovementButton.Events().AddClicked
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

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (BindingContext is Core.Models.Account account)
        {
            _account = account;
            _brokerAccount = account.Broker.Value;
            _broker = account.Broker.Value?.Broker;

            SetupValues();            
        }
    }

    private void SetupValues()
    {
        Icon.ImagePath = _broker!.Image;
        BrokerName.Text = _brokerAccount!.AccountNumber;
        
        AddMovementContainer.VerticalOptions = _account!.HasMovements 
            ? LayoutOptions.End 
            : LayoutOptions.Center;

        AddMovementContainer.HorizontalOptions = _account!.HasMovements
            ? LayoutOptions.Start 
            : LayoutOptions.Center;

        AddMovementButton.Scale = _account!.HasMovements ? 0.6 : 1;
        AddMovementContainer.Spacing = _account!.HasMovements ? 0 : 12;
    }
}