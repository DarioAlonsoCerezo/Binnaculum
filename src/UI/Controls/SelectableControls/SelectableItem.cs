using Binnaculum.Core;

namespace Binnaculum.Controls;

public class SelectableItem
{
    public string Title { get; set; }
    public object ItemValue { get; set; }

    public static List<SelectableItem> BrokerMovementTypeList() =>
    [
        new SelectableItem
        {
            Title = ResourceKeys.MovementType_Deposit,
            ItemValue = Models.MovementType.Deposit
        },
        new SelectableItem
        {
            Title = ResourceKeys.MovementType_Withdrawal,
            ItemValue = Models.MovementType.Withdrawal
        },
        new SelectableItem
        {
            Title = ResourceKeys.MovementType_Trade,
            ItemValue = Models.MovementType.Trade
        },
        new SelectableItem
        {
            Title = ResourceKeys.MovementType_OptionTrade,
            ItemValue = Models.MovementType.OptionTrade
        },
        new SelectableItem
        {
            Title = ResourceKeys.MovementType_DividendReceived,
            ItemValue = Models.MovementType.DividendReceived
        },
        new SelectableItem
        {
            Title = ResourceKeys.MovementType_DividendTaxWithheld,
            ItemValue = Models.MovementType.DividendTaxWithheld
        },
        new SelectableItem
        {
            Title = ResourceKeys.MovementType_DividendExDate,
            ItemValue = Models.MovementType.DividendExDate
        },
        new SelectableItem
        {
            Title = ResourceKeys.MovementType_DividendPayDate,
            ItemValue = Models.MovementType.DividendPayDate
        },
        new SelectableItem
        {
            Title = ResourceKeys.MovementType_Fee,
            ItemValue = Models.MovementType.Fee
        },
        new SelectableItem
        {
            Title = ResourceKeys.MovementType_InterestsGained,
            ItemValue = Models.MovementType.InterestsGained
        },
        new SelectableItem
        {
            Title = ResourceKeys.MovementType_Lending,
            ItemValue = Models.MovementType.Lending
        },
        new SelectableItem
        {
            Title = ResourceKeys.MovementType_ACATMoneyTransfer,
            ItemValue = Models.MovementType.ACATMoneyTransfer
        },
        new SelectableItem
        {
            Title = ResourceKeys.MovementType_ACATSecuritiesTransfer,
            ItemValue = Models.MovementType.ACATSecuritiesTransfer
        },
        new SelectableItem
        {
            Title = ResourceKeys.MovementType_InterestsPaid,
            ItemValue = Models.MovementType.InterestsPaid
        },
        new SelectableItem{
            Title = ResourceKeys.MovementType_Conversion,
            ItemValue = Models.MovementType.Conversion
        }
    ];

    public static List<SelectableItem> OptionTypeList()
    {
        return new List<SelectableItem>
        {
            new SelectableItem
            {
                Title = ResourceKeys.OptionType_Call,
                ItemValue = Models.OptionType.Call
            },
            new SelectableItem
            {
                Title = ResourceKeys.OptionType_Put,
                ItemValue = Models.OptionType.Put
            }
        };
    }

    public static List<SelectableItem> OptionCodeList()
    {
        return new List<SelectableItem>
        {
            new SelectableItem
            {
                Title = ResourceKeys.OptionCode_STO,
                ItemValue = Models.OptionCode.SellToOpen
            },
            new SelectableItem
            {
                Title = ResourceKeys.OptionCode_STC,
                ItemValue = Models.OptionCode.SellToClose
            },
            new SelectableItem
            {
                Title = ResourceKeys.OptionCode_BTO,
                ItemValue = Models.OptionCode.BuyToOpen
            },
            new SelectableItem
            {
                Title = ResourceKeys.OptionCode_BTC,
                ItemValue = Models.OptionCode.BuyToClose
            },
            new SelectableItem
            {
                Title = ResourceKeys.OptionCode_Assigned,
                ItemValue = Models.OptionCode.Assigned
            },
            new SelectableItem
            {
                Title = ResourceKeys.OptionCode_Expired,
                ItemValue = Models.OptionCode.Expired
            }
        };
    }
}