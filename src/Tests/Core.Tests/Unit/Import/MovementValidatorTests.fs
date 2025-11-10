namespace Tests

open NUnit.Framework
open System
open Binnaculum.Core.Import
open Binnaculum.Core.Import.ImportDomainTypes
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Patterns

[<TestFixture>]
type MovementValidatorTests() =
    
    /// <summary>
    /// Helper to create a valid Trade for testing
    /// </summary>
    let createValidTrade() =
        { Id = 0
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
          Audit = AuditableEntity.FromDateTime(DateTime.UtcNow) }
    
    /// <summary>
    /// Helper to create a valid OptionTrade for testing
    /// </summary>
    let createValidOptionTrade() =
        { Id = 0
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
          Notes = Some "Test option"
          Audit = AuditableEntity.FromDateTime(DateTime.UtcNow) }
    
    /// <summary>
    /// Helper to create a valid Dividend for testing
    /// </summary>
    let createValidDividend() =
        { Id = 0
          TimeStamp = DateTimePattern.FromDateTime(DateTime.UtcNow)
          DividendAmount = Money.FromAmount(10m)
          TickerId = 1
          CurrencyId = 1
          BrokerAccountId = 1
          Audit = AuditableEntity.FromDateTime(DateTime.UtcNow) }
    
    /// <summary>
    /// Helper to create a valid BrokerMovement for testing
    /// </summary>
    let createValidBrokerMovement() =
        { Id = 0
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
          Audit = AuditableEntity.FromDateTime(DateTime.UtcNow) }
    
    [<Test>]
    member _.``Valid stock trade should pass validation``() =
        // Arrange
        let trade = createValidTrade()
        let movement = StockTradeMovement(trade)
        let batch = {
            Movements = [movement]
            BrokerAccountId = 1
            SourceBroker = SupportedBroker.Tastytrade
            ImportDate = DateTime.UtcNow
            Metadata = ImportMetadata.createEmpty()
        }
        
        // Act
        let result = MovementValidator.validateBatch batch
        
        // Assert
        Assert.That(result.Valid.Length, Is.EqualTo(1))
        Assert.That(result.Invalid.Length, Is.EqualTo(0))
    
    [<Test>]
    member _.``Stock trade with negative quantity should fail validation``() =
        // Arrange
        let trade = { createValidTrade() with Quantity = -10m }
        let movement = StockTradeMovement(trade)
        let batch = {
            Movements = [movement]
            BrokerAccountId = 1
            SourceBroker = SupportedBroker.Tastytrade
            ImportDate = DateTime.UtcNow
            Metadata = ImportMetadata.createEmpty()
        }
        
        // Act
        let result = MovementValidator.validateBatch batch
        
        // Assert
        Assert.That(result.Valid.Length, Is.EqualTo(0))
        Assert.That(result.Invalid.Length, Is.EqualTo(1))
        let (_, errorMsg) = result.Invalid.[0]
        Assert.That(errorMsg, Does.Contain("quantity must be positive"))
    
    [<Test>]
    member _.``Stock trade with negative price should fail validation``() =
        // Arrange
        let trade = { createValidTrade() with Price = Money.FromAmount(-100m) }
        let movement = StockTradeMovement(trade)
        let batch = {
            Movements = [movement]
            BrokerAccountId = 1
            SourceBroker = SupportedBroker.Tastytrade
            ImportDate = DateTime.UtcNow
            Metadata = ImportMetadata.createEmpty()
        }
        
        // Act
        let result = MovementValidator.validateBatch batch
        
        // Assert
        Assert.That(result.Valid.Length, Is.EqualTo(0))
        Assert.That(result.Invalid.Length, Is.EqualTo(1))
        let (_, errorMsg) = result.Invalid.[0]
        Assert.That(errorMsg, Does.Contain("price cannot be negative"))
    
    [<Test>]
    member _.``Valid option trade should pass validation``() =
        // Arrange
        let option = createValidOptionTrade()
        let movement = OptionTradeMovement(option)
        let batch = {
            Movements = [movement]
            BrokerAccountId = 1
            SourceBroker = SupportedBroker.Tastytrade
            ImportDate = DateTime.UtcNow
            Metadata = ImportMetadata.createEmpty()
        }
        
        // Act
        let result = MovementValidator.validateBatch batch
        
        // Assert
        Assert.That(result.Valid.Length, Is.EqualTo(1))
        Assert.That(result.Invalid.Length, Is.EqualTo(0))
    
    [<Test>]
    member _.``Option trade with negative strike should fail validation``() =
        // Arrange
        let option = { createValidOptionTrade() with Strike = Money.FromAmount(-100m) }
        let movement = OptionTradeMovement(option)
        let batch = {
            Movements = [movement]
            BrokerAccountId = 1
            SourceBroker = SupportedBroker.Tastytrade
            ImportDate = DateTime.UtcNow
            Metadata = ImportMetadata.createEmpty()
        }
        
        // Act
        let result = MovementValidator.validateBatch batch
        
        // Assert
        Assert.That(result.Valid.Length, Is.EqualTo(0))
        Assert.That(result.Invalid.Length, Is.EqualTo(1))
        let (_, errorMsg) = result.Invalid.[0]
        Assert.That(errorMsg, Does.Contain("strike cannot be negative"))
    
    [<Test>]
    member _.``Valid dividend should pass validation``() =
        // Arrange
        let dividend = createValidDividend()
        let movement = DividendMovement(dividend)
        let batch = {
            Movements = [movement]
            BrokerAccountId = 1
            SourceBroker = SupportedBroker.Tastytrade
            ImportDate = DateTime.UtcNow
            Metadata = ImportMetadata.createEmpty()
        }
        
        // Act
        let result = MovementValidator.validateBatch batch
        
        // Assert
        Assert.That(result.Valid.Length, Is.EqualTo(1))
        Assert.That(result.Invalid.Length, Is.EqualTo(0))
    
    [<Test>]
    member _.``Dividend with zero amount should fail validation``() =
        // Arrange
        let dividend = { createValidDividend() with DividendAmount = Money.FromAmount(0m) }
        let movement = DividendMovement(dividend)
        let batch = {
            Movements = [movement]
            BrokerAccountId = 1
            SourceBroker = SupportedBroker.Tastytrade
            ImportDate = DateTime.UtcNow
            Metadata = ImportMetadata.createEmpty()
        }
        
        // Act
        let result = MovementValidator.validateBatch batch
        
        // Assert
        Assert.That(result.Valid.Length, Is.EqualTo(0))
        Assert.That(result.Invalid.Length, Is.EqualTo(1))
        let (_, errorMsg) = result.Invalid.[0]
        Assert.That(errorMsg, Does.Contain("amount must be positive"))
    
    [<Test>]
    member _.``Valid broker movement should pass validation``() =
        // Arrange
        let brokerMovement = createValidBrokerMovement()
        let movement = BrokerMovement(brokerMovement)
        let batch = {
            Movements = [movement]
            BrokerAccountId = 1
            SourceBroker = SupportedBroker.Tastytrade
            ImportDate = DateTime.UtcNow
            Metadata = ImportMetadata.createEmpty()
        }
        
        // Act
        let result = MovementValidator.validateBatch batch
        
        // Assert
        Assert.That(result.Valid.Length, Is.EqualTo(1))
        Assert.That(result.Invalid.Length, Is.EqualTo(0))
    
    [<Test>]
    member _.``Broker movement with negative amount should fail validation``() =
        // Arrange
        let brokerMovement = { createValidBrokerMovement() with Amount = Money.FromAmount(-1000m) }
        let movement = BrokerMovement(brokerMovement)
        let batch = {
            Movements = [movement]
            BrokerAccountId = 1
            SourceBroker = SupportedBroker.Tastytrade
            ImportDate = DateTime.UtcNow
            Metadata = ImportMetadata.createEmpty()
        }
        
        // Act
        let result = MovementValidator.validateBatch batch
        
        // Assert
        Assert.That(result.Valid.Length, Is.EqualTo(0))
        Assert.That(result.Invalid.Length, Is.EqualTo(1))
        let (_, errorMsg) = result.Invalid.[0]
        Assert.That(errorMsg, Does.Contain("amount cannot be negative"))
    
    [<Test>]
    member _.``Batch with mixed valid and invalid movements should separate them correctly``() =
        // Arrange
        let validTrade = createValidTrade()
        let invalidTrade = { createValidTrade() with Quantity = -10m }
        let validDividend = createValidDividend()
        let invalidDividend = { createValidDividend() with DividendAmount = Money.FromAmount(0m) }
        
        let batch = {
            Movements = [
                StockTradeMovement(validTrade)
                StockTradeMovement(invalidTrade)
                DividendMovement(validDividend)
                DividendMovement(invalidDividend)
            ]
            BrokerAccountId = 1
            SourceBroker = SupportedBroker.Tastytrade
            ImportDate = DateTime.UtcNow
            Metadata = ImportMetadata.createEmpty()
        }
        
        // Act
        let result = MovementValidator.validateBatch batch
        
        // Assert
        Assert.That(result.Valid.Length, Is.EqualTo(2))
        Assert.That(result.Invalid.Length, Is.EqualTo(2))
