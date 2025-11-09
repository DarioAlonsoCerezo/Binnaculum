using Binnaculum.Core;

namespace Binnaculum.Controls;

internal class MovementTemplateSelector : DataTemplateSelector
{
    private readonly DataTemplate EmptyMovementTemplate;
    private readonly DataTemplate TradeMovementTemplate;
    private readonly DataTemplate OptionTradeMovementTemplate;
    private readonly DataTemplate DividendMovementTemplate;
    private readonly DataTemplate DividendTaxMovementTemplate;
    private readonly DataTemplate DividendDateMovementTemplate;
    private readonly DataTemplate BrokerMovementTemplate;
    private readonly DataTemplate BankMovementTemplate;

    public MovementTemplateSelector()
    {
        EmptyMovementTemplate = new DataTemplate(typeof(EmptyMovementTemplate));
        TradeMovementTemplate = new DataTemplate(typeof(TradeMovementTemplate));
        OptionTradeMovementTemplate = new DataTemplate(typeof(OptionTradeMovementTemplate));
        DividendMovementTemplate = new DataTemplate(typeof(DividendMovementTemplate));
        DividendTaxMovementTemplate = new DataTemplate(typeof(DividendTaxMovementTemplate));
        DividendDateMovementTemplate = new DataTemplate(typeof(DividendDateMovementTemplate));
        BrokerMovementTemplate = new DataTemplate(typeof(BrokerMovementTemplate));
        BankMovementTemplate = new DataTemplate(typeof(BankMovementTemplate));
    }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is not Models.Movement movement)
            return EmptyMovementTemplate;

        return movement.Type switch
        {
            Models.AccountMovementType.Trade => TradeMovementTemplate,
            Models.AccountMovementType.OptionTrade => OptionTradeMovementTemplate,
            Models.AccountMovementType.Dividend => DividendMovementTemplate,
            Models.AccountMovementType.DividendTax => DividendTaxMovementTemplate,
            Models.AccountMovementType.DividendDate => DividendDateMovementTemplate,
            Models.AccountMovementType.BrokerMovement => BrokerMovementTemplate,
            Models.AccountMovementType.BankAccountMovement => BankMovementTemplate,
            _ => EmptyMovementTemplate
        };
    }
}