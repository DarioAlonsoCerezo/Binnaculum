namespace Binnaculum.Controls;

public partial class BorderedFeeAndCommissionControl
{
    public decimal Commission { get; private set; }
    public decimal Fee { get; private set; }
    
    public BorderedFeeAndCommissionControl()
	{
		InitializeComponent();
        Commission = 0m;
        Fee = 0m;
    }

    protected override void StartLoad()
    {
        Commissions.Events().TextChanged
            .Subscribe(_ =>
            {
                Commission = 0m;
                if (decimal.TryParse(Commissions.Text, out var commission))
                    Commission = commission;
            })
            .DisposeWith(Disposables);

        Fees.Events().TextChanged
            .Subscribe(_ =>
            {
                Fee = 0m;
                if (decimal.TryParse(Fees.Text, out var fee))
                    Fee = fee;                
            })
            .DisposeWith(Disposables);
    }
}