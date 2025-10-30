namespace Binnaculum.Core

module internal FieldName =
    // Common Fields
    [<Literal>]
    let Id = "Id"

    [<Literal>]
    let TimeStamp = "TimeStamp"

    [<Literal>]
    let CurrencyId = "CurrencyId"

    // Dividends
    [<Literal>]
    let DividendAmount = "DividendAmount"

    [<Literal>]
    let DividendTaxAmount = "DividendTaxAmount"

    [<Literal>]
    let BrokerAccountId = "BrokerAccountId"

    // Dividend Taxes
    [<Literal>]
    let Amount = "Amount"

    // Dividend Dates
    [<Literal>]
    let DividendCode = "DividendCode"

    // Bank Account Movements
    [<Literal>]
    let BankAccountId = "BankAccountId"

    [<Literal>]
    let MovementType = "MovementType"

    // Brokers
    [<Literal>]
    let Name = "Name"

    [<Literal>]
    let Image = "Image"

    [<Literal>]
    let SupportedBroker = "SupportedBroker"

    // Broker Accounts
    [<Literal>]
    let BrokerId = "BrokerId"

    [<Literal>]
    let AccountNumber = "AccountNumber"

    // Currencies
    [<Literal>]
    let Code = "Code"

    [<Literal>]
    let Symbol = "Symbol"

    // Broker Movements
    [<Literal>]
    let AmountDefault = "Amount"

    [<Literal>]
    let Commissions = "Commissions"

    [<Literal>]
    let Fees = "Fees"

    [<Literal>]
    let FromCurrencyId = "FromCurrencyId"

    [<Literal>]
    let AmountChanged = "AmountChanged"

    [<Literal>]
    let CreatedAt = "CreatedAt"

    [<Literal>]
    let UpdatedAt = "UpdatedAt"

    // Ticker Prices
    [<Literal>]
    let TickerId = "TickerId"

    [<Literal>]
    let PriceDate = "PriceDate"

    [<Literal>]
    let Price = "Price"

    // Ticker Splits
    [<Literal>]
    let SplitDate = "SplitDate"

    [<Literal>]
    let SplitFactor = "SplitFactor"

    // Trades
    [<Literal>]
    let Quantity = "Quantity"

    [<Literal>]
    let TradeCode = "TradeCode"

    [<Literal>]
    let TradeType = "TradeType"

    [<Literal>]
    let Notes = "Notes"

    [<Literal>]
    let Leveraged = "Leveraged"

    // Options
    [<Literal>]
    let ExpirationDate = "ExpirationDate"

    [<Literal>]
    let Premium = "Premium"

    [<Literal>]
    let NetPremium = "NetPremium"

    [<Literal>]
    let OptionType = "OptionType"

    [<Literal>]
    let Strike = "Strike"

    [<Literal>]
    let IsOpen = "IsOpen"

    [<Literal>]
    let ClosedWith = "ClosedWith"

    [<Literal>]
    let Multiplier = "Multiplier"

    // Banks
    [<Literal>]
    let BankId = "BankId"

    [<Literal>]
    let Description = "Description"

    // Ticker Snapshots
    [<Literal>]
    let Date = "Date"

    [<Literal>]
    let TotalShares = "TotalShares"

    [<Literal>]
    let Weight = "Weight"

    [<Literal>]
    let CostBasis = "CostBasis"

    [<Literal>]
    let RealCost = "RealCost"

    [<Literal>]
    let Dividends = "Dividends"

    [<Literal>]
    let DividendTaxes = "DividendTaxes"

    [<Literal>]
    let Options = "Options"

    [<Literal>]
    let TotalIncomes = "TotalIncomes"

    [<Literal>]
    let Realized = "Realized"

    [<Literal>]
    let RealizedToday = "RealizedToday"

    [<Literal>]
    let Performance = "Performance"

    [<Literal>]
    let OpenTrades = "OpenTrades"

    // Broker Account Snapshots
    [<Literal>]
    let RealizedGains = "RealizedGains"

    [<Literal>]
    let RealizedPercentage = "RealizedPercentage"

    [<Literal>]
    let UnrealizedGains = "UnrealizedGains"

    [<Literal>]
    let UnrealizedGainsPercentage = "UnrealizedGainsPercentage"

    [<Literal>]
    let Invested = "Invested"

    [<Literal>]
    let Deposited = "Deposited"

    [<Literal>]
    let Withdrawn = "Withdrawn"

    [<Literal>]
    let DividendsReceived = "DividendsReceived"

    [<Literal>]
    let OptionsIncome = "OptionsIncome"

    [<Literal>]
    let OtherIncome = "OtherIncome"

    [<Literal>]
    let NetCashFlow = "NetCashFlow"

    // Broker Snapshots
    [<Literal>]
    let PortfoliosValue = "PortfoliosValue"

    [<Literal>]
    let AccountCount = "AccountCount"

    // Bank Account Snapshots
    [<Literal>]
    let Balance = "Balance"

    [<Literal>]
    let InterestEarned = "InterestEarned"

    [<Literal>]
    let FeesPaid = "FeesPaid"

    // Bank Snapshots
    [<Literal>]
    let TotalBalance = "TotalBalance"

    // Broker Financial Snapshots
    [<Literal>]
    let MovementCounter = "MovementCounter"

    // Additional field
    [<Literal>]
    let TickerSnapshotId = "TickerSnapshotId"

    [<Literal>]
    let BrokerSnapshotId = "BrokerSnapshotId"

    [<Literal>]
    let BrokerAccountSnapshotId = "BrokerAccountSnapshotId"

    // AutoImportOperation fields
    [<Literal>]
    let CapitalDeployed = "CapitalDeployed"

    [<Literal>]
    let CapitalDeployedToday = "CapitalDeployedToday"

    // AutoImportOperationTrade fields
    [<Literal>]
    let AutoOperationId = "AutoOperationId"

    [<Literal>]
    let ReferenceId = "ReferenceId"
