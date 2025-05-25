using Binnaculum.Core;
using static Binnaculum.Core.Models;

namespace Binnaculum.Controls;

public partial class MovementTemplate
{
	public MovementTemplate()
	{
		InitializeComponent();
	}

    protected override void StartLoad()
    {
        
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if(BindingContext is Models.Movement movement)
        {            
            Quantity.IsVisible = movement.Type.IsTrade;

            if (movement.Type.IsBrokerMovement)
                FillBrokerAccountMovement(movement);            
            
            if(movement.Type.IsBankAccountMovement)
                FillBankAccountMovement(movement.BankAccountMovement.Value);

            if (movement.Type.IsTrade)
                FillTradeMovement(movement.Trade.Value);
        }
    }

    private void FillBrokerAccountMovement(Models.Movement movement)
    {
        Icon.ImagePath = movement.BrokerMovement.Value.BrokerAccount.Broker.Image;
        Amount.Amount = movement.BrokerMovement.Value.Amount;
        Amount.Money = movement.BrokerMovement.Value.Currency;
        TimeStamp.DateTime = movement.BrokerMovement.Value.TimeStamp;
        Title.SetLocalizedText(GetTitleFromBrokerAccountMovementType(movement.BrokerMovement.Value.MovementType));
    }

    private void FillBankAccountMovement(Models.BankAccountMovement movement)
    {
        Icon.ImagePath = movement.BankAccount.Bank.Image.Value;
        Amount.Amount = movement.Amount;
        Amount.Money = movement.Currency;
        TimeStamp.DateTime = movement.TimeStamp;
        Title.SetLocalizedText(GetTitleFromBankAccountMovementType(movement.MovementType));
    }

    private void FillTradeMovement(Models.Trade trade)
    {
        Icon.ImagePath = trade.Ticker.Image?.Value ?? string.Empty;
        Icon.PlaceholderText = trade.Ticker.Symbol;
        TimeStamp.DateTime = trade.TimeStamp;
        Amount.Amount = trade.TotalInvestedAmount;
        Amount.Money = trade.Currency;
        Quantity.Text = $"x{trade.Quantity}";
        Title.SetLocalizedText(ResourceKeys.MovementType_Trade);
    }

    private string GetTitleFromBrokerAccountMovementType(Models.BrokerMovementType movementType)
    {
        var resourceKey = ResourceKeys.MovementType_ACATMoneyTransfer;
        if (movementType.IsACATSecuritiesTransfer)
            resourceKey = ResourceKeys.MovementType_ACATSecuritiesTransfer;
        if (movementType.IsConversion)
            resourceKey = ResourceKeys.MovementType_Conversion;
        if (movementType.IsDeposit)
            resourceKey = ResourceKeys.MovementType_Deposit;
        if (movementType.IsFee)
            resourceKey = ResourceKeys.MovementType_Fee;
        if (movementType.IsInterestsGained)
            resourceKey = ResourceKeys.MovementType_InterestsGained;       
        if (movementType.IsInterestsPaid)
            resourceKey = ResourceKeys.MovementType_InterestsPaid;
        if (movementType.IsLending)
            resourceKey = ResourceKeys.MovementType_Lending;
        if (movementType.IsWithdrawal)
            resourceKey = ResourceKeys.MovementType_Withdrawal;
        
        return resourceKey;
    }

    private string GetTitleFromBankAccountMovementType(Models.BankAccountMovementType movementType)
    {
        var resourceKey = ResourceKeys.MovementType_Bank_Fees;
        if (movementType.IsBalance)
            resourceKey = ResourceKeys.MovementType_Bank_Balance;
        if (movementType.IsInterest)
            resourceKey = ResourceKeys.MovementType_Bank_Interest;

        return resourceKey;
    }
}