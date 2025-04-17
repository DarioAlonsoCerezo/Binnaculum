using Microsoft.FSharp.Core;

namespace Binnaculum.Controls;

public partial class BankAccountTemplate
{
	public BankAccountTemplate()
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
            if(account.Bank.Value!.Bank.Image != null)
                Icon.ImagePath = account.Bank.Value.Bank.Image.Value;
            else
                Icon.ImagePath = "bank";
        }
    }
}