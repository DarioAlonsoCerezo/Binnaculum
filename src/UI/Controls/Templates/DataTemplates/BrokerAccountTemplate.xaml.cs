namespace Binnaculum.Controls;

public partial class BrokerAccountTemplate
{
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

        if (BindingContext is Core.Models.Account account)
        { 
            SetupValues(account.Broker.Value);
        }
    }

    private void SetupValues(Core.Models.BrokerAccount brokerAccount)
    {
        Icon.ImagePath = brokerAccount.Broker.Image;
        BrokerName.Text = brokerAccount.AccountNumber;
    }
}