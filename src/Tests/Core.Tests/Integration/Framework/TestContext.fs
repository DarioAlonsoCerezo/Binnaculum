namespace Core.Tests.Integration

/// <summary>
/// Test context for maintaining state across test operations.
/// Stores IDs and data created during test setup for use in subsequent operations.
/// </summary>
type TestContext = {
    mutable TastytradeId: int
    mutable IbkrId: int
    mutable BrokerAccountId: int
    mutable BankAccountId: int
    mutable BankId: int
    mutable UsdCurrencyId: int
    mutable EurCurrencyId: int
    mutable SpyTickerId: int
}

module TestContext =
    
    /// <summary>
    /// Create a new test context with default values
    /// </summary>
    let create() : TestContext =
        {
            TastytradeId = 0
            IbkrId = 0
            BrokerAccountId = 0
            BankAccountId = 0
            BankId = 0
            UsdCurrencyId = 0
            EurCurrencyId = 0
            SpyTickerId = 0
        }
    
    /// <summary>
    /// Reset context to default values
    /// </summary>
    let reset(ctx: TestContext) : unit =
        ctx.TastytradeId <- 0
        ctx.IbkrId <- 0
        ctx.BrokerAccountId <- 0
        ctx.BankAccountId <- 0
        ctx.BankId <- 0
        ctx.UsdCurrencyId <- 0
        ctx.EurCurrencyId <- 0
        ctx.SpyTickerId <- 0
