using Binnaculum.Core;
using Binnaculum.Core.Logging;
using static Binnaculum.Core.Models;

namespace Binnaculum.Controls;

public partial class MovementTemplate
{
    private Color _redColor = (Color)Application.Current!.Resources["RedState"];
    private Color _greenColor = (Color)Application.Current!.Resources["GreenState"];

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

        if (BindingContext is Models.Movement movement)
        {
            // Commented out to reduce log noise
            //CoreLogger.logDebug("MovementTemplate", "BindingContext changed for template");
            Quantity.IsVisible = movement.Type.IsTrade;
            SubTitle.IsVisible = ShowSubtitle(movement);
            OptionSubtitle.IsVisible = movement.Type.IsOptionTrade;
            ACAT.IsVisible = ShowACAT(movement);
            Amount.IsVisible = !ACAT.IsVisible;

            if (movement.Type.IsBrokerMovement)
                FillBrokerAccountMovement(movement);

            if (movement.Type.IsBankAccountMovement)
                FillBankAccountMovement(movement.BankAccountMovement.Value);

            if (movement.Type.IsTrade)
                FillTradeMovement(movement.Trade.Value);

            if (movement.Type.IsDividend)
                FillDividendReceived(movement.Dividend.Value);

            if (movement.Type.IsDividendDate)
                FillDividendDate(movement.DividendDate.Value);

            if (movement.Type.IsDividendTax)
                FillDividendTax(movement.DividendTax.Value);

            if (movement.Type.IsOptionTrade)
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

        if (movement.BrokerMovement.Value.MovementType.IsConversion)
        {
            AmountConverted.Amount = movement.BrokerMovement.Value.AmountChanged.Value;
            AmountConverted.Money = movement.BrokerMovement.Value.FromCurrency.Value;
            AmountConverted.IsVisible = true;
        }

        if (movement.BrokerMovement.Value.MovementType.IsACATSecuritiesTransferReceived
            || movement.BrokerMovement.Value.MovementType.IsACATSecuritiesTransferSent)
        {
            Icon.ImagePath = movement.BrokerMovement.Value.Ticker.Value.Image.Value;
            ACATQuantity.Text = movement.BrokerMovement.Value.Quantity.Value.ToString("N0");

            ACATQuantity.TextColor = movement.BrokerMovement.Value.MovementType.IsACATSecuritiesTransferSent
                ? _redColor
                : _greenColor;
        }

        if (movement.BrokerMovement.Value.MovementType.IsACATMoneyTransferReceived
            || movement.BrokerMovement.Value.MovementType.IsACATMoneyTransferSent
            || movement.BrokerMovement.Value.MovementType.IsACATSecuritiesTransferReceived
            || movement.BrokerMovement.Value.MovementType.IsACATSecuritiesTransferSent)
        {
            SubTitle.SetLocalizedText(GetSubtitleFromACAT(movement.BrokerMovement.Value.MovementType));
        }
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
        var resourceKey = ResourceKeys.MovementType_ACATTransfer;
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

    private string GetSubtitleFromACAT(Models.BrokerMovementType movementType)
    {
        var resourceKey = ResourceKeys.MovementType_ACATMoneyTransferReceived_Subtitle;
        if (movementType.IsACATMoneyTransferSent)
            resourceKey = ResourceKeys.MovementType_ACATMoneyTransferSent_Subtitle;
        if (movementType.IsACATSecuritiesTransferSent)
            resourceKey = ResourceKeys.MovementType_ACATSecuritiesTransferSent_Subtitle;
        if (movementType.IsACATSecuritiesTransferReceived)
            resourceKey = ResourceKeys.MovementType_ACATSecuritiesTransferReceived_Subtitle;
        return resourceKey;
    }

    private bool ShowACAT(Models.Movement movement)
    {
        return movement.Type.IsBrokerMovement
            && (movement.BrokerMovement.Value.MovementType.IsACATSecuritiesTransferReceived
                || movement.BrokerMovement.Value.MovementType.IsACATSecuritiesTransferSent);
    }

    private bool ShowSubtitle(Models.Movement movement)
    {
        if (movement.Type.IsBrokerMovement)
        {
            return movement.BrokerMovement.Value.MovementType.IsACATSecuritiesTransferReceived
                || movement.BrokerMovement.Value.MovementType.IsACATSecuritiesTransferSent
                || movement.BrokerMovement.Value.MovementType.IsACATMoneyTransferReceived
                || movement.BrokerMovement.Value.MovementType.IsACATMoneyTransferSent;
        }

        return movement.Type.IsTrade || movement.Type.IsDividendDate;
    }
}