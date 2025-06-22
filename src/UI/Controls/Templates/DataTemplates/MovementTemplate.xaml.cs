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
            SubTitle.IsVisible = movement.Type.IsTrade || movement.Type.IsDividendDate;
            OptionSubtitle.IsVisible = movement.Type.IsOptionTrade;

            if (movement.Type.IsBrokerMovement)
                FillBrokerAccountMovement(movement);            
            
            if(movement.Type.IsBankAccountMovement)
                FillBankAccountMovement(movement.BankAccountMovement.Value);

            if (movement.Type.IsTrade)
                FillTradeMovement(movement.Trade.Value);

            if (movement.Type.IsDividend)
                FillDividendReceived(movement.Dividend.Value);

            if(movement.Type.IsDividendDate)
                FillDividendDate(movement.DividendDate.Value);

            if(movement.Type.IsDividendTax)
                FillDividendTax(movement.DividendTax.Value);

            if(movement.Type.IsOptionTrade)
                FillOptionTrade(movement.OptionTrade.Value);
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
        Quantity.Text = $"x{trade.Quantity.Simplifyed()}";
        Title.SetLocalizedText(ResourceKeys.MovementType_Trade);
        SubTitle.SetLocalizedText(GetSubtitleFromTradeCode(trade.TradeCode));
    }

    private void FillDividendReceived(Models.Dividend dividend)
    {
        Icon.ImagePath = dividend.Ticker.Image?.Value ?? string.Empty;
        Icon.PlaceholderText = dividend.Ticker.Symbol;
        TimeStamp.DateTime = dividend.TimeStamp;
        Amount.Amount = dividend.Amount;
        Amount.Money = dividend.Currency;
        Title.SetLocalizedText(ResourceKeys.MovementType_DividendReceived);
    }

    private void FillDividendDate(Models.DividendDate dividend)
    {
        Icon.ImagePath = dividend.Ticker.Image?.Value ?? string.Empty;
        Icon.PlaceholderText = dividend.Ticker.Symbol;
        TimeStamp.DateTime = dividend.TimeStamp;
        Amount.Amount = dividend.Amount;
        Amount.Money = dividend.Currency;
        var text = dividend.DividendCode == Models.DividendCode.ExDividendDate
            ? ResourceKeys.MovementType_DividendExDate
            : ResourceKeys.MovementType_DividendPayDate;

        Title.SetLocalizedText(text);
        SubTitle.Text = dividend.TimeStamp.ToShortDateString();
    }

    private void FillDividendTax(Models.DividendTax dividend)
    {
        Icon.ImagePath = dividend.Ticker.Image?.Value ?? string.Empty;
        Icon.PlaceholderText = dividend.Ticker.Symbol;
        TimeStamp.DateTime = dividend.TimeStamp;
        Amount.Amount = dividend.TaxAmount;
        Amount.Money = dividend.Currency;
        Title.SetLocalizedText(ResourceKeys.MovementType_DividendTaxWithheld);
    }

    private void FillOptionTrade(OptionTrade trade)
    {
        var toShow = trade.Code.ToShow();
        
        if (toShow && trade.ExpirationDate > DateTime.Today)
        {
            ExpirationDateLabel.IsVisible = toShow;
            ExpirationDate.Text = trade.ExpirationDate.ToString("d");
        }

        Icon.ImagePath = trade.Ticker.Image?.Value ?? string.Empty;
        Icon.PlaceholderText = trade.Ticker.Symbol;
        TimeStamp.DateTime = trade.TimeStamp;
        Amount.Amount = trade.NetPremium;
        Amount.Money = trade.Currency;
        Amount.ChangeColor = toShow;
        Amount.IsNegative = trade.Code.IsPaid();
        Title.SetLocalizedText(ResourceKeys.MovementType_OptionTrade);
        OptionType.SetLocalizedText(trade.OptionType.ToLocalized());
        OptionCode.SetLocalizedText(trade.Code.ToLocalized());
        OptionStrikeValue.Text = trade.Strike.ToMoneyString();
        if (trade.Quantity > 1)
            OptionQuantity.Text = $"x{trade.Quantity}";

        OptionStrike.IsVisible = trade.ExpirationDate > DateTime.Today;
    }

    private string GetTitleFromBrokerAccountMovementType(Models.BrokerMovementType movementType)
    {
        return movementType switch
        {
            Models.BrokerMovementType.Deposit => ResourceKeys.MovementType_Deposit,
            Models.BrokerMovementType.Withdrawal => ResourceKeys.MovementType_Withdrawal,
            Models.BrokerMovementType.Fee => ResourceKeys.MovementType_Fee,
            Models.BrokerMovementType.InterestsGained => ResourceKeys.MovementType_InterestsGained,
            Models.BrokerMovementType.Lending => ResourceKeys.MovementType_Lending,
            Models.BrokerMovementType.ACATMoneyTransferSent => ResourceKeys.MovementType_ACATMoneyTransferSent,
            Models.BrokerMovementType.ACATMoneyTransferReceived => ResourceKeys.MovementType_ACATMoneyTransferReceived,
            Models.BrokerMovementType.ACATSecuritiesTransferSent => ResourceKeys.MovementType_ACATSecuritiesTransferSent,
            Models.BrokerMovementType.ACATSecuritiesTransferReceived => ResourceKeys.MovementType_ACATSecuritiesTransferReceived,
            Models.BrokerMovementType.InterestsPaid => ResourceKeys.MovementType_InterestsPaid,
            Models.BrokerMovementType.Conversion => ResourceKeys.MovementType_Conversion,
            _ => throw new ArgumentOutOfRangeException(nameof(movementType), movementType, null)
        };
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

    private string GetSubtitleFromTradeCode(Models.TradeCode tradeCode)
    {
        var resourceKey = ResourceKeys.Movement_BuyToOpen;
        if (tradeCode.IsBuyToClose)
            resourceKey = ResourceKeys.Movement_BuyToClose;
        if (tradeCode.IsSellToOpen)
            resourceKey = ResourceKeys.Movement_SellToOpen;
        if (tradeCode.IsSellToClose)
            resourceKey = ResourceKeys.Movement_SellToClose;

        return resourceKey;
    }
}