using Binnaculum.Controls;
using Binnaculum.Core;
using Binnaculum.Popups;

namespace Binnaculum.Pages;

public partial class BrokerMovementCreatorPage
{
    private readonly Models.BrokerAccount _account;

	public BrokerMovementCreatorPage(Models.BrokerAccount account)
	{
		InitializeComponent();
        _account = account;

        Icon.ImagePath = _account.Broker.Image;
        AccountName.Text = _account.AccountNumber;

        MovementTypeControl.ItemsSource = SelectableItem.BrokerMovementTypeList();
    }

    protected override void StartLoad()
    {
        CloseGesture.Events().Tapped
            .Select(async _ => await Navigation.PopModalAsync())
            .Subscribe()
            .DisposeWith(Disposables);

        //By default, it should be created manually
        ManualRadioButton.IsChecked = true;

        var selection = MovementTypeControl.Events()
            .ItemSelected
            .Select(x =>
            {
                if (x.ItemValue is Models.MovementType movement)
                    return movement;
                return null;
            }).WhereNotNull();

        selection.Select(x => x == Models.MovementType.Deposit || x == Models.MovementType.Withdrawal)
            .BindTo(Deposit, x => x.IsVisible)
            .DisposeWith(Disposables);

        Deposit.Events().DepositChanged
            .Where(_ => Deposit.IsVisible)
            .Select(x => x.Amount > 0)
            .BindTo(Save, x => x.IsVisible)
            .DisposeWith(Disposables);

        Save.Events().SaveClicked
            .Select(async _ => await SaveMovement())
            .Select(async _ => await Navigation.PopModalAsync())
            .Subscribe()
            .DisposeWith(Disposables);
    }

    private Task SaveMovement()
    {
        return Task.Run(async () =>
        {
            try
            {
                if (MovementTypeControl.SelectedItem == null)
                    return;

                if (MovementTypeControl.SelectedItem.ItemValue is Models.MovementType movement)
                {
                    if (movement.IsDeposit)
                    {
                        var currencyCode = Deposit.DepositData.Currency;
                        var currency = Core.UI.Collections.GetCurrency(currencyCode);
                        var uiDeposit = new Models.UiDeposit(
                            _account.Id,
                            currency.Id,
                            Deposit.DepositData.Amount,
                            Deposit.DepositData.TimeStamp,
                            Deposit.DepositData.Commissions,
                            Deposit.DepositData.Fees);
                        await Core.UI.Creator.SaveDeposit(uiDeposit);
                    }
                    else if (movement == Models.MovementType.Withdrawal)
                    {
                        
                    }
                    else
                    {
                        
                    }
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                // Show error message to user on the UI thread
                await MainThread.InvokeOnMainThreadAsync(() => {
                    var errorMessage = LocalizationResourceManager.Instance["Error_Saving_Movement"];
                    var popup = new MarkdownMessagePopup
                    {
                        Text = $"{errorMessage}\n\n```\n{ex.Message}\n```"
                    };
                    popup.Show();
                });
#endif
            }
        });
    }
}