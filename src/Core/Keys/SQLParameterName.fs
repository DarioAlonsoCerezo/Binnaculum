namespace Binnaculum.Core

module internal SQLParameterName =
    
    // Common Fields
    [<Literal>]
    let Id = "@Id"

    [<Literal>]
    let TimeStamp = "@TimeStamp"

    [<Literal>]
    let CurrencyId = "@CurrencyId"

    // Dividends
    [<Literal>]
    let DividendAmount = "@DividendAmount"

    [<Literal>]
    let BrokerAccountId = "@BrokerAccountId"

    // Dividend Taxes
    [<Literal>]
    let Amount = "@Amount"

    // Dividend Dates
    [<Literal>]
    let DividendCode = "@DividendCode"

    // Bank Account Movements
    [<Literal>]
    let BankAccountId = "@BankAccountId"

    [<Literal>]
    let MovementType = "@MovementType"

    // Brokers
    [<Literal>]
    let Name = "@Name"

    [<Literal>]
    let Image = "@Image"

    [<Literal>]
    let SupportedBroker = "@SupportedBroker"

    // Broker Accounts
    [<Literal>]
    let BrokerId = "@BrokerId"

    [<Literal>]
    let AccountNumber = "@AccountNumber"

    // Currencies
    [<Literal>]
    let Code = "@Code"

    [<Literal>]
    let Symbol = "@Symbol"

    // Broker Movements
    [<Literal>]
    let AmountDefault = "@Amount"

    [<Literal>]
    let Commissions = "@Commissions"

    [<Literal>]
    let Fees = "@Fees"

    [<Literal>]
    let CreatedAt = "@CreatedAt"

    [<Literal>]
    let UpdatedAt = "@UpdatedAt"

    // Ticker Prices
    [<Literal>]
    let TickerId = "@TickerId"
    
    [<Literal>]
    let PriceDate = "@PriceDate"

    [<Literal>]
    let Price = "@Price"

    // Ticker Splits
    [<Literal>]
    let SplitDate = "@SplitDate"

    [<Literal>]
    let SplitFactor = "@SplitFactor"

    // Trades
    [<Literal>]
    let Quantity = "@Quantity"

    [<Literal>]
    let TradeCode = "@TradeCode"

    [<Literal>]
    let TradeType = "@TradeType"

    [<Literal>]
    let Notes = "@Notes"

    // Options
    [<Literal>]
    let ExpirationDate = "@ExpirationDate"

    [<Literal>]
    let Premium = "@Premium"

    [<Literal>]
    let NetPremium = "@NetPremium"

    [<Literal>]
    let OptionType = "@OptionType"

    [<Literal>]
    let Strike = "@Strike"

    [<Literal>]
    let IsOpen = "@IsOpen"

    [<Literal>]
    let ClosedWith = "@ClosedWith"

    // Banks
    [<Literal>]
    let BankId = "@BankId"

    [<Literal>]
    let Description = "@Description"