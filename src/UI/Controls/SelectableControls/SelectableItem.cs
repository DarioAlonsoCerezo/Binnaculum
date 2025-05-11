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
        }
    ];
}