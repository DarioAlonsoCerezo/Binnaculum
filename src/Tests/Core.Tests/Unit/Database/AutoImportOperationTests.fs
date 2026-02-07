namespace Binnaculum.Core.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open Binnaculum.Core.Database
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Patterns

/// <summary>
/// Unit tests for AutoImportOperation database operations.
/// Tests basic CRUD functionality using in-memory database.
/// </summary>
[<TestClass>]
type public AutoImportOperationTests() =
    inherit InMemoryDatabaseFixture()
    
    [<TestMethod>]
    member public this.``Can create AutoImportOperation with default values``() =
        let operation = AutoImportOperationExtensions.Do.createOperation(1, 1, 1)
        
        Assert.AreEqual(1, operation.BrokerAccountId)
        Assert.AreEqual(1, operation.TickerId)
        Assert.AreEqual(1, operation.CurrencyId)
        Assert.IsTrue(operation.IsOpen)
        Assert.AreEqual(0m, operation.Realized.Value)
        Assert.AreEqual(0m, operation.Commissions.Value)
        Assert.AreEqual(0m, operation.Fees.Value)
        Assert.AreEqual(0m, operation.Premium.Value)
        Assert.AreEqual(0m, operation.Dividends.Value)
        Assert.AreEqual(0m, operation.DividendTaxes.Value)
        Assert.AreEqual(0m, operation.CapitalDeployed.Value)
        Assert.AreEqual(0m, operation.Performance)
    
    [<TestMethod>]
    member public this.``Can update operation metrics``() =
        let operation = AutoImportOperationExtensions.Do.createOperation(1, 1, 1)
        
        let updatedOperation =
            AutoImportOperationExtensions.Do.updateOperationMetrics(
                operation,
                Money.FromAmount(100m),
                Money.FromAmount(5m),
                Money.FromAmount(2m),
                Money.FromAmount(50m),
                Money.FromAmount(10m),
                Money.FromAmount(3m),
                Money.FromAmount(1000m)
            )
        
        Assert.AreEqual(100m, updatedOperation.Realized.Value)
        Assert.AreEqual(5m, updatedOperation.Commissions.Value)
        Assert.AreEqual(2m, updatedOperation.Fees.Value)
        Assert.AreEqual(50m, updatedOperation.Premium.Value)
        Assert.AreEqual(10m, updatedOperation.Dividends.Value)
        Assert.AreEqual(3m, updatedOperation.DividendTaxes.Value)
        Assert.AreEqual(1000m, updatedOperation.CapitalDeployed.Value)
        Assert.AreEqual(10m, updatedOperation.Performance) // 100 / 1000 * 100 = 10%
    
    [<TestMethod>]
    member public this.``Performance calculation handles zero capital deployed``() =
        let operation = AutoImportOperationExtensions.Do.createOperation(1, 1, 1)
        
        let updatedOperation =
            AutoImportOperationExtensions.Do.updateOperationMetrics(
                operation,
                Money.FromAmount(100m),
                Money.FromAmount(0m),
                Money.FromAmount(0m),
                Money.FromAmount(0m),
                Money.FromAmount(0m),
                Money.FromAmount(0m),
                Money.FromAmount(0m) // Zero capital deployed
            )
        
        Assert.AreEqual(0m, updatedOperation.Performance, "Performance should be 0% when capital is 0")
    
    [<TestMethod>]
    member public this.``Can close an operation``() =
        let operation = AutoImportOperationExtensions.Do.createOperation(1, 1, 1)
        Assert.IsTrue(operation.IsOpen)
        
        let closedOperation = AutoImportOperationExtensions.Do.closeOperation(operation)
        Assert.IsFalse(closedOperation.IsOpen)
    
    [<TestMethod>]
    member public this.``OperationTradeType discriminated union has all expected cases``() =
        // Test that all operation trade types are defined
        let stockTrade = OperationTradeType.StockTrade
        let optionTrade = OperationTradeType.OptionTrade
        let dividend = OperationTradeType.Dividend
        let dividendTax = OperationTradeType.DividendTax
        
        Assert.IsNotNull(stockTrade)
        Assert.IsNotNull(optionTrade)
        Assert.IsNotNull(dividend)
        Assert.IsNotNull(dividendTax)
    
    [<TestMethod>]
    member public this.``AutoImportOperation implements IEntity interface``() =
        let operation = AutoImportOperationExtensions.Do.createOperation(1, 1, 1)
        let entity = operation :> Do.IEntity
        
        Assert.IsNotNull(entity)
        Assert.AreEqual(0, entity.Id)
    
    [<TestMethod>]
    member public this.``AutoImportOperation implements IAuditEntity interface``() =
        let operation = AutoImportOperationExtensions.Do.createOperation(1, 1, 1)
        let auditEntity = operation :> Do.IAuditEntity
        
        Assert.IsNotNull(auditEntity)
        Assert.AreEqual(None, auditEntity.CreatedAt)
        Assert.AreEqual(None, auditEntity.UpdatedAt)
