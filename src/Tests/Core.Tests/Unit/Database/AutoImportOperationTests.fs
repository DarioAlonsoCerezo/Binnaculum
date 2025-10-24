namespace Binnaculum.Core.Tests

open NUnit.Framework
open Binnaculum.Core.Database
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Patterns

/// <summary>
/// Unit tests for AutoImportOperation database operations.
/// Tests basic CRUD functionality using in-memory database.
/// </summary>
[<TestFixture>]
type AutoImportOperationTests() =
    inherit InMemoryDatabaseFixture()
    
    [<Test>]
    member this.``Can create AutoImportOperation with default values``() =
        let operation = AutoImportOperationExtensions.Do.createOperation(1, 1, 1)
        
        Assert.That(operation.BrokerAccountId, Is.EqualTo(1))
        Assert.That(operation.TickerId, Is.EqualTo(1))
        Assert.That(operation.CurrencyId, Is.EqualTo(1))
        Assert.That(operation.IsOpen, Is.True)
        Assert.That(operation.Realized.Value, Is.EqualTo(0m))
        Assert.That(operation.Commissions.Value, Is.EqualTo(0m))
        Assert.That(operation.Fees.Value, Is.EqualTo(0m))
        Assert.That(operation.Premium.Value, Is.EqualTo(0m))
        Assert.That(operation.Dividends.Value, Is.EqualTo(0m))
        Assert.That(operation.DividendTaxes.Value, Is.EqualTo(0m))
        Assert.That(operation.CapitalDeployed.Value, Is.EqualTo(0m))
        Assert.That(operation.Performance, Is.EqualTo(0m))
    
    [<Test>]
    member this.``Can update operation metrics``() =
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
        
        Assert.That(updatedOperation.Realized.Value, Is.EqualTo(100m))
        Assert.That(updatedOperation.Commissions.Value, Is.EqualTo(5m))
        Assert.That(updatedOperation.Fees.Value, Is.EqualTo(2m))
        Assert.That(updatedOperation.Premium.Value, Is.EqualTo(50m))
        Assert.That(updatedOperation.Dividends.Value, Is.EqualTo(10m))
        Assert.That(updatedOperation.DividendTaxes.Value, Is.EqualTo(3m))
        Assert.That(updatedOperation.CapitalDeployed.Value, Is.EqualTo(1000m))
        Assert.That(updatedOperation.Performance, Is.EqualTo(10m)) // 100 / 1000 * 100 = 10%
    
    [<Test>]
    member this.``Performance calculation handles zero capital deployed``() =
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
        
        Assert.That(updatedOperation.Performance, Is.EqualTo(0m), "Performance should be 0% when capital is 0")
    
    [<Test>]
    member this.``Can close an operation``() =
        let operation = AutoImportOperationExtensions.Do.createOperation(1, 1, 1)
        Assert.That(operation.IsOpen, Is.True)
        
        let closedOperation = AutoImportOperationExtensions.Do.closeOperation(operation)
        Assert.That(closedOperation.IsOpen, Is.False)
    
    [<Test>]
    member this.``OperationTradeType discriminated union has all expected cases``() =
        // Test that all operation trade types are defined
        let stockTrade = OperationTradeType.StockTrade
        let optionTrade = OperationTradeType.OptionTrade
        let dividend = OperationTradeType.Dividend
        let dividendTax = OperationTradeType.DividendTax
        
        Assert.That(stockTrade, Is.Not.Null)
        Assert.That(optionTrade, Is.Not.Null)
        Assert.That(dividend, Is.Not.Null)
        Assert.That(dividendTax, Is.Not.Null)
    
    [<Test>]
    member this.``AutoImportOperation implements IEntity interface``() =
        let operation = AutoImportOperationExtensions.Do.createOperation(1, 1, 1)
        let entity = operation :> Do.IEntity
        
        Assert.That(entity, Is.Not.Null)
        Assert.That(entity.Id, Is.EqualTo(0))
    
    [<Test>]
    member this.``AutoImportOperation implements IAuditEntity interface``() =
        let operation = AutoImportOperationExtensions.Do.createOperation(1, 1, 1)
        let auditEntity = operation :> Do.IAuditEntity
        
        Assert.That(auditEntity, Is.Not.Null)
        Assert.That(auditEntity.CreatedAt, Is.EqualTo(None))
        Assert.That(auditEntity.UpdatedAt, Is.EqualTo(None))
