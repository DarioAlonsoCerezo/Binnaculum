namespace Binnaculum.Core.Tests

open NUnit.Framework
open Binnaculum.Core.Database
open Binnaculum.Core.Database.DatabaseModel

/// <summary>
/// Unit tests for AutoImportOperationTrade database operations.
/// Tests the junction table that links operations to trades, options, dividends, and dividend taxes.
/// </summary>
[<TestFixture>]
type AutoImportOperationTradeTests() =
    inherit InMemoryDatabaseFixture()
    
    [<Test>]
    member this.``Can create stock trade link``() =
        let link =
            { Id = 0
              AutoOperationId = 1
              TradeType = OperationTradeType.StockTrade
              ReferenceId = 123
              Audit = AuditableEntity.Default }
        
        Assert.That(link.AutoOperationId, Is.EqualTo(1))
        Assert.That(link.TradeType, Is.EqualTo(OperationTradeType.StockTrade))
        Assert.That(link.ReferenceId, Is.EqualTo(123))
    
    [<Test>]
    member this.``Can create option trade link``() =
        let link =
            { Id = 0
              AutoOperationId = 1
              TradeType = OperationTradeType.OptionTrade
              ReferenceId = 456
              Audit = AuditableEntity.Default }
        
        Assert.That(link.TradeType, Is.EqualTo(OperationTradeType.OptionTrade))
    
    [<Test>]
    member this.``Can create dividend link``() =
        let link =
            { Id = 0
              AutoOperationId = 1
              TradeType = OperationTradeType.Dividend
              ReferenceId = 789
              Audit = AuditableEntity.Default }
        
        Assert.That(link.TradeType, Is.EqualTo(OperationTradeType.Dividend))
    
    [<Test>]
    member this.``Can create dividend tax link``() =
        let link =
            { Id = 0
              AutoOperationId = 1
              TradeType = OperationTradeType.DividendTax
              ReferenceId = 999
              Audit = AuditableEntity.Default }
        
        Assert.That(link.TradeType, Is.EqualTo(OperationTradeType.DividendTax))
    
    [<Test>]
    member this.``AutoImportOperationTrade implements IEntity interface``() =
        let link =
            { Id = 5
              AutoOperationId = 1
              TradeType = OperationTradeType.StockTrade
              ReferenceId = 123
              Audit = AuditableEntity.Default }
        
        let entity = link :> Do.IEntity
        Assert.That(entity.Id, Is.EqualTo(5))
    
    [<Test>]
    member this.``AutoImportOperationTrade implements IAuditEntity interface``() =
        let link =
            { Id = 0
              AutoOperationId = 1
              TradeType = OperationTradeType.StockTrade
              ReferenceId = 123
              Audit = AuditableEntity.Default }
        
        let auditEntity = link :> Do.IAuditEntity
        Assert.That(auditEntity.CreatedAt, Is.EqualTo(None))
        Assert.That(auditEntity.UpdatedAt, Is.EqualTo(None))
    
    [<Test>]
    member this.``OperationTradeType can be converted to and from database string``() =
        // Test that type parsers work correctly
        let stockTradeStr = TypeParser.fromOperationTradeTypeToDatabase OperationTradeType.StockTrade
        let optionTradeStr = TypeParser.fromOperationTradeTypeToDatabase OperationTradeType.OptionTrade
        let dividendStr = TypeParser.fromOperationTradeTypeToDatabase OperationTradeType.Dividend
        let dividendTaxStr = TypeParser.fromOperationTradeTypeToDatabase OperationTradeType.DividendTax
        
        Assert.That(stockTradeStr, Is.EqualTo("STOCK_TRADE"))
        Assert.That(optionTradeStr, Is.EqualTo("OPTION_TRADE"))
        Assert.That(dividendStr, Is.EqualTo("DIVIDEND"))
        Assert.That(dividendTaxStr, Is.EqualTo("DIVIDEND_TAX"))
        
        // Test round-trip conversion
        let stockTradeBack = TypeParser.fromDatabaseToOperationTradeType stockTradeStr
        let optionTradeBack = TypeParser.fromDatabaseToOperationTradeType optionTradeStr
        let dividendBack = TypeParser.fromDatabaseToOperationTradeType dividendStr
        let dividendTaxBack = TypeParser.fromDatabaseToOperationTradeType dividendTaxStr
        
        Assert.That(stockTradeBack, Is.EqualTo(OperationTradeType.StockTrade))
        Assert.That(optionTradeBack, Is.EqualTo(OperationTradeType.OptionTrade))
        Assert.That(dividendBack, Is.EqualTo(OperationTradeType.Dividend))
        Assert.That(dividendTaxBack, Is.EqualTo(OperationTradeType.DividendTax))
