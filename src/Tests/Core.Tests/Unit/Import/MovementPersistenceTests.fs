namespace Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open System
open Binnaculum.Core.Import
open Binnaculum.Core.Import.ImportDomainTypes
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Patterns

/// <summary>
/// Tests for MovementPersistence module's conversion logic.
/// Tests the bridge function that converts old PersistenceInput to new ImportMovementBatch.
/// </summary>
[<TestClass>]
type MovementPersistenceTests() =
    
    [<TestMethod>]
    member _.``convertPersistenceInputToBatch should correctly convert old format``() =
        // Arrange
        let stockTrade = {
            Id = 0
            TimeStamp = DateTimePattern.FromDateTime(DateTime.UtcNow)
            TickerId = 1
            BrokerAccountId = 1
            CurrencyId = 1
            Quantity = 10m
            Price = Money.FromAmount(100m)
            Commissions = Money.FromAmount(1m)
            Fees = Money.FromAmount(0.5m)
            TradeCode = TradeCode.BuyToOpen
            TradeType = TradeType.Long
            Leveraged = 1m
            Notes = Some "Test trade"
            Audit = AuditableEntity.FromDateTime(DateTime.UtcNow)
        }
        
        let brokerMovement = {
            Id = 0
            TimeStamp = DateTimePattern.FromDateTime(DateTime.UtcNow)
            Amount = Money.FromAmount(1000m)
            CurrencyId = 1
            BrokerAccountId = 1
            Commissions = Money.FromAmount(0m)
            Fees = Money.FromAmount(0m)
            MovementType = BrokerMovementType.Deposit
            Notes = Some "Test deposit"
            FromCurrencyId = None
            AmountChanged = None
            TickerId = None
            Quantity = None
            Audit = AuditableEntity.FromDateTime(DateTime.UtcNow)
        }
        
        let persistenceInput = {
            BrokerMovements = [brokerMovement]
            OptionTrades = []
            StockTrades = [stockTrade]
            Dividends = []
            DividendTaxes = []
            SessionId = None
        }
        
        // Act
        let batch = MovementPersistence.convertPersistenceInputToBatch persistenceInput 1 SupportedBroker.Tastytrade
        
        // Assert
        Assert.AreEqual(2, batch.Movements.Length, "Should convert 2 movements")
        Assert.AreEqual(1, batch.BrokerAccountId, "Should preserve broker account ID")
        Assert.AreEqual(SupportedBroker.Tastytrade, batch.SourceBroker, "Should preserve source broker")
        
        // Check that movements are correctly typed
        let stockTradeMovements = batch.Movements |> List.filter (function | StockTradeMovement _ -> true | _ -> false)
        let brokerMovements = batch.Movements |> List.filter (function | BrokerMovement _ -> true | _ -> false)
        
        Assert.AreEqual(1, stockTradeMovements.Length, "Should have 1 stock trade movement")
        Assert.AreEqual(1, brokerMovements.Length, "Should have 1 broker movement")
    
    [<TestMethod>]
    member _.``convertPersistenceInputToBatch should preserve metadata fields``() =
        // Arrange
        let optionTrade = {
            Id = 0
            TimeStamp = DateTimePattern.FromDateTime(DateTime(2024, 1, 15))
            ExpirationDate = DateTimePattern.FromDateTime(DateTime(2024, 2, 15))
            Premium = Money.FromAmount(50m)
            NetPremium = Money.FromAmount(48m)
            TickerId = 1
            BrokerAccountId = 1
            CurrencyId = 1
            OptionType = OptionType.Call
            Code = OptionCode.BuyToOpen
            Strike = Money.FromAmount(100m)
            Commissions = Money.FromAmount(1m)
            Fees = Money.FromAmount(1m)
            IsOpen = true
            ClosedWith = None
            Multiplier = 100m
            Notes = Some "Test option"
            Audit = AuditableEntity.FromDateTime(DateTime.UtcNow)
        }
        
        let dividend = {
            Id = 0
            TimeStamp = DateTimePattern.FromDateTime(DateTime(2024, 1, 10))
            DividendAmount = Money.FromAmount(10m)
            TickerId = 2
            CurrencyId = 1
            BrokerAccountId = 1
            Audit = AuditableEntity.FromDateTime(DateTime.UtcNow)
        }
        
        let persistenceInput = {
            BrokerMovements = []
            OptionTrades = [optionTrade]
            StockTrades = []
            Dividends = [dividend]
            DividendTaxes = []
            SessionId = Some 123
        }
        
        // Act
        let batch = MovementPersistence.convertPersistenceInputToBatch persistenceInput 5 SupportedBroker.IBKR
        
        // Assert
        Assert.AreEqual(2, batch.Movements.Length, "Should convert 2 movements")
        Assert.AreEqual(5, batch.BrokerAccountId, "Should use provided broker account ID")
        Assert.AreEqual(SupportedBroker.IBKR, batch.SourceBroker, "Should use provided source broker")
        Assert.AreEqual(2, batch.Metadata.TotalMovementsImported, "Should track total movements")
        Assert.IsTrue(batch.Metadata.OldestMovementDate.IsSome, "Should have oldest movement date")
        
        // The oldest date should be the dividend (Jan 10) not the option trade (Jan 15)
        let expectedOldest = DateTime(2024, 1, 10)
        Assert.AreEqual(expectedOldest.Date, batch.Metadata.OldestMovementDate.Value.Date, "Should correctly identify oldest movement date")
    
    [<TestMethod>]
    member _.``convertPersistenceInputToBatch should handle empty input correctly``() =
        // Arrange
        let emptyInput = {
            BrokerMovements = []
            OptionTrades = []
            StockTrades = []
            Dividends = []
            DividendTaxes = []
            SessionId = None
        }
        
        // Act
        let batch = MovementPersistence.convertPersistenceInputToBatch emptyInput 1 SupportedBroker.Tastytrade
        
        // Assert
        Assert.AreEqual(0, batch.Movements.Length, "Should have no movements")
        Assert.AreEqual(0, batch.Metadata.TotalMovementsImported, "Should report zero movements")
        Assert.IsTrue(batch.Metadata.OldestMovementDate.IsNone, "Should have no oldest movement date for empty input")
    
    [<TestMethod>]
    member _.``convertPersistenceInputToBatch should handle all movement types``() =
        // Arrange
        let stockTrade = {
            Id = 0
            TimeStamp = DateTimePattern.FromDateTime(DateTime.UtcNow)
            TickerId = 1
            BrokerAccountId = 1
            CurrencyId = 1
            Quantity = 10m
            Price = Money.FromAmount(100m)
            Commissions = Money.FromAmount(1m)
            Fees = Money.FromAmount(0.5m)
            TradeCode = TradeCode.BuyToOpen
            TradeType = TradeType.Long
            Leveraged = 1m
            Notes = Some "Stock"
            Audit = AuditableEntity.FromDateTime(DateTime.UtcNow)
        }
        
        let optionTrade = {
            Id = 0
            TimeStamp = DateTimePattern.FromDateTime(DateTime.UtcNow)
            ExpirationDate = DateTimePattern.FromDateTime(DateTime.UtcNow.AddDays(30))
            Premium = Money.FromAmount(50m)
            NetPremium = Money.FromAmount(48m)
            TickerId = 1
            BrokerAccountId = 1
            CurrencyId = 1
            OptionType = OptionType.Call
            Code = OptionCode.BuyToOpen
            Strike = Money.FromAmount(100m)
            Commissions = Money.FromAmount(1m)
            Fees = Money.FromAmount(1m)
            IsOpen = true
            ClosedWith = None
            Multiplier = 100m
            Notes = Some "Option"
            Audit = AuditableEntity.FromDateTime(DateTime.UtcNow)
        }
        
        let dividend = {
            Id = 0
            TimeStamp = DateTimePattern.FromDateTime(DateTime.UtcNow)
            DividendAmount = Money.FromAmount(10m)
            TickerId = 1
            CurrencyId = 1
            BrokerAccountId = 1
            Audit = AuditableEntity.FromDateTime(DateTime.UtcNow)
        }
        
        let dividendTax = {
            Id = 0
            TimeStamp = DateTimePattern.FromDateTime(DateTime.UtcNow)
            DividendTaxAmount = Money.FromAmount(2m)
            TickerId = 1
            CurrencyId = 1
            BrokerAccountId = 1
            Audit = AuditableEntity.FromDateTime(DateTime.UtcNow)
        }
        
        let brokerMovement = {
            Id = 0
            TimeStamp = DateTimePattern.FromDateTime(DateTime.UtcNow)
            Amount = Money.FromAmount(1000m)
            CurrencyId = 1
            BrokerAccountId = 1
            Commissions = Money.FromAmount(0m)
            Fees = Money.FromAmount(0m)
            MovementType = BrokerMovementType.Deposit
            Notes = Some "Deposit"
            FromCurrencyId = None
            AmountChanged = None
            TickerId = None
            Quantity = None
            Audit = AuditableEntity.FromDateTime(DateTime.UtcNow)
        }
        
        let persistenceInput = {
            BrokerMovements = [brokerMovement]
            OptionTrades = [optionTrade]
            StockTrades = [stockTrade]
            Dividends = [dividend]
            DividendTaxes = [dividendTax]
            SessionId = None
        }
        
        // Act
        let batch = MovementPersistence.convertPersistenceInputToBatch persistenceInput 1 SupportedBroker.Tastytrade
        
        // Assert
        Assert.AreEqual(5, batch.Movements.Length, "Should convert all 5 movement types")
        
        // Count each type
        let stockCount = batch.Movements |> List.filter (function | StockTradeMovement _ -> true | _ -> false) |> List.length
        let optionCount = batch.Movements |> List.filter (function | OptionTradeMovement _ -> true | _ -> false) |> List.length
        let dividendCount = batch.Movements |> List.filter (function | DividendMovement _ -> true | _ -> false) |> List.length
        let taxCount = batch.Movements |> List.filter (function | DividendTaxMovement _ -> true | _ -> false) |> List.length
        let brokerCount = batch.Movements |> List.filter (function | BrokerMovement _ -> true | _ -> false) |> List.length
        
        Assert.AreEqual(1, stockCount, "Should have 1 stock trade")
        Assert.AreEqual(1, optionCount, "Should have 1 option trade")
        Assert.AreEqual(1, dividendCount, "Should have 1 dividend")
        Assert.AreEqual(1, taxCount, "Should have 1 dividend tax")
        Assert.AreEqual(1, brokerCount, "Should have 1 broker movement")
