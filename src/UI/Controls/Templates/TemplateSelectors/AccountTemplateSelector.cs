using Binnaculum.Core;

namespace Binnaculum.Controls;

internal class AccountTemplateSelector : DataTemplateSelector
{
    private readonly DataTemplate EmptyAccountTemplate;
    private readonly DataTemplate BankAccountTemplate;
    private readonly DataTemplate BrokerAccountTemplate;

    public AccountTemplateSelector()
    {
        BrokerAccountTemplate = new DataTemplate(typeof(BrokerAccountTemplate));
        BankAccountTemplate = new DataTemplate(typeof(BankAccountTemplate));
        EmptyAccountTemplate = new DataTemplate(typeof(EmptyAccountTemplate));
    }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if(item is Models.Account account)
        {
            if (account.Type.IsBrokerAccount)
                return BrokerAccountTemplate;
            if (account.Type.IsBankAccount)
                return BankAccountTemplate;
        }

        return EmptyAccountTemplate;
    }
}
