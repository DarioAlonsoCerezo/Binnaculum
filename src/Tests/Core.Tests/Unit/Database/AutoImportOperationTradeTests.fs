namespace Binnaculum.Core.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open Binnaculum.Core.Database
open Binnaculum.Core.Database.DatabaseModel

/// <summary>
/// Unit tests for AutoImportOperationTrade database operations.
/// Tests the junction table that links operations to trades, options, dividends, and dividend taxes.
/// </summary>
[<TestClass>]
type public AutoImportOperationTradeTests() =
    inherit InMemoryDatabaseFixture()
    
    [<TestMethod>]
    member public this.``Can create stock trade link``() =
        let link =
            { Id = 0
              AutoOperationId = 1
              TradeType = OperationTradeType.StockTrade
              ReferenceId = 123
              Audit = AuditableEntity.Default }
        
        Assert.AreEqual(1, link.AutoOperationId)
        Assert.AreEqual(OperationTradeType.StockTrade, link.TradeType)
        Assert.AreEqual(123, link.ReferenceId)
    
    [<TestMethod>]
    member public this.``Can create option trade link``() =
        let link =
            { Id = 0
              AutoOperationId = 1
              TradeType = OperationTradeType.OptionTrade
              ReferenceId = 456
              Audit = AuditableEntity.Default }
        
        Assert.AreEqual(OperationTradeType.OptionTrade, link.TradeType)
    
    [<TestMethod>]
    member public this.``Can create dividend link``() =
        let link =
            { Id = 0
              AutoOperationId = 1
              TradeType = OperationTradeType.Dividend
              ReferenceId = 789
              Audit = AuditableEntity.Default }
        
        Assert.AreEqual(OperationTradeType.Dividend, link.TradeType)
    
    [<TestMethod>]
    member public this.``Can create dividend tax link``() =
        let link =
            { Id = 0
              AutoOperationId = 1
              TradeType = OperationTradeType.DividendTax
              ReferenceId = 999
              Audit = AuditableEntity.Default }
        
        Assert.AreEqual(OperationTradeType.DividendTax, link.TradeType)
    
    [<TestMethod>]
    member public this.``AutoImportOperationTrade implements IEntity interface``() =
        let link =
            { Id = 5
              AutoOperationId = 1
              TradeType = OperationTradeType.StockTrade
              ReferenceId = 123
              Audit = AuditableEntity.Default }
        
        let entity = link :> Do.IEntity
        Assert.AreEqual(5, entity.Id)
    
    [<TestMethod>]
    member public this.``AutoImportOperationTrade implements IAuditEntity interface``() =
        let link =
            { Id = 0
              AutoOperationId = 1
              TradeType = OperationTradeType.StockTrade
              ReferenceId = 123
              Audit = AuditableEntity.Default }
        
        let auditEntity = link :> Do.IAuditEntity
        Assert.AreEqual(None, auditEntity.CreatedAt)
        Assert.AreEqual(None, auditEntity.UpdatedAt)
    
    [<TestMethod>]
    member public this.``OperationTradeType can be converted to and from database string``() =
        // Test that type parsers work correctly
        let stockTradeStr = TypeParser.fromOperationTradeTypeToDatabase OperationTradeType.StockTrade
        let optionTradeStr = TypeParser.fromOperationTradeTypeToDatabase OperationTradeType.OptionTrade
        let dividendStr = TypeParser.fromOperationTradeTypeToDatabase OperationTradeType.Dividend
        let dividendTaxStr = TypeParser.fromOperationTradeTypeToDatabase OperationTradeType.DividendTax
        
        Assert.AreEqual("STOCK_TRADE", stockTradeStr)
        Assert.AreEqual("OPTION_TRADE", optionTradeStr)
        Assert.AreEqual("DIVIDEND", dividendStr)
        Assert.AreEqual("DIVIDEND_TAX", dividendTaxStr)
        
        // Test round-trip conversion
        let stockTradeBack = TypeParser.fromDatabaseToOperationTradeType stockTradeStr
        let optionTradeBack = TypeParser.fromDatabaseToOperationTradeType optionTradeStr
        let dividendBack = TypeParser.fromDatabaseToOperationTradeType dividendStr
        let dividendTaxBack = TypeParser.fromDatabaseToOperationTradeType dividendTaxStr
        
        Assert.AreEqual(OperationTradeType.StockTrade, stockTradeBack)
        Assert.AreEqual(OperationTradeType.OptionTrade, optionTradeBack)
        Assert.AreEqual(OperationTradeType.Dividend, dividendBack)
        Assert.AreEqual(OperationTradeType.DividendTax, dividendTaxBack)
