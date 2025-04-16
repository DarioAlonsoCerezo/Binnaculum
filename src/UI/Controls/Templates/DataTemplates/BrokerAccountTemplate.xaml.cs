namespace Binnaculum.Controls;

public partial class BrokerAccountTemplate
{
	public BrokerAccountTemplate()
	{
		InitializeComponent();
	}

    protected override void StartLoad()
    {
        this.Events().BindingContextChanged
            .Select(x => BindingContext)
            .WhereNotNull()
            .Select(x => x is Core.Models.Account account ? account : null)
            .WhereNotNull()
            .Subscribe(x =>
            {
                Icon.ImagePath = x.Broker.Value.Broker.Image;
            })
            .DisposeWith(Disposables);
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