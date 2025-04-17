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
            Icon.ImagePath = account.Broker.Value.Broker.Image;
        }
    }
}