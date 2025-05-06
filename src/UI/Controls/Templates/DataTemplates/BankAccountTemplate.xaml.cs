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
            SetupValues(account.Bank.Value!);
        }
    }

    private void SetupValues(Core.Models.BankAccount bankAccount)
    {
        if (bankAccount.Bank.Image != null)
            Icon.ImagePath = bankAccount.Bank.Image.Value;
        else
            Icon.ImagePath = "bank";

        AccountNumber.Text = bankAccount.Name;
    }
}