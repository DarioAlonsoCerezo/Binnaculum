namespace Tests

open NUnit.Framework
open Binnaculum.Core.Import
open Binnaculum.Core.Import.TastytradeModels
open Binnaculum.Core.Import.TastytradeTransactionConverter
open System

[<TestFixture>]
type TastytradeTransactionConverterTests() =

    let createMoneyMovementTransaction (date: DateTime) (subType: MoneyMovementSubType) (value: decimal) =
        {
            Date = date
            TransactionType = MoneyMovement(subType)
            Symbol = None
            InstrumentType = None
            Description = "Test transaction"
            Value = value
            Quantity = 0m
            AveragePrice = None
            Commissions = 1m
            Fees = 0.5m
            Multiplier = None
            RootSymbol = None
            UnderlyingSymbol = None
            ExpirationDate = None
            StrikePrice = None
            CallOrPut = None
            OrderNumber = None
            Currency = "USD"
            RawCsvLine = "Test line"
            LineNumber = 1
        }

    let createOptionTradeTransaction (date: DateTime) (action: TradeAction) (value: decimal) =
        {
            Date = date
            TransactionType = Trade(SellToOpen, action)
            Symbol = Some "SOFI  240503P00007000"
            InstrumentType = Some "Equity Option"
            Description = "Sold option"
            Value = value
            Quantity = 1m
            AveragePrice = Some 35m
            Commissions = 1m
            Fees = 0.14m
            Multiplier = Some 100m
            RootSymbol = Some "SOFI"
            UnderlyingSymbol = Some "SOFI"
            ExpirationDate = Some (DateTime(2024, 5, 3))
            StrikePrice = Some 7m
            CallOrPut = Some "PUT"
            OrderNumber = Some "319448136"
            Currency = "USD"
            RawCsvLine = "Test option line"
            LineNumber = 2
        }

    [<Test>]
    member _.``Should convert Money Movement Deposit to BrokerMovement count`` () =
        // Arrange
        let transaction = createMoneyMovementTransaction (DateTime(2024, 4, 22)) Deposit 10m
        
        // Act
        let result = convertTransaction transaction
        
        // Assert
        Assert.That(result.BrokerMovementsCount, Is.EqualTo(1))
        Assert.That(result.OptionTradesCount, Is.EqualTo(0))
        Assert.That(result.StockTradesCount, Is.EqualTo(0))
        Assert.That(result.Errors, Is.Empty)

    [<Test>]
    member _.``Should convert Money Movement Balance Adjustment to BrokerMovement count`` () =
        // Arrange
        let transaction = createMoneyMovementTransaction (DateTime(2024, 4, 27)) BalanceAdjustment -0.02m
        
        // Act
        let result = convertTransaction transaction
        
        // Assert
        Assert.That(result.BrokerMovementsCount, Is.EqualTo(1))
        Assert.That(result.OptionTradesCount, Is.EqualTo(0))
        Assert.That(result.StockTradesCount, Is.EqualTo(0))
        Assert.That(result.Errors, Is.Empty)

    [<Test>]
    member _.``Should convert Option Trade to OptionTrade count`` () =
        // Arrange
        let transaction = createOptionTradeTransaction (DateTime(2024, 4, 25)) SELL_TO_OPEN 35m
        
        // Act
        let result = convertTransaction transaction
        
        // Assert
        Assert.That(result.BrokerMovementsCount, Is.EqualTo(0))
        Assert.That(result.OptionTradesCount, Is.EqualTo(1))
        Assert.That(result.StockTradesCount, Is.EqualTo(0))
        Assert.That(result.Errors, Is.Empty)

    [<Test>]
    member _.``Should sort transactions chronologically`` () =
        // Arrange
        let transaction1 = createMoneyMovementTransaction (DateTime(2024, 4, 27)) Deposit 10m
        let transaction2 = createMoneyMovementTransaction (DateTime(2024, 4, 22)) BalanceAdjustment -0.02m
        let transaction3 = createOptionTradeTransaction (DateTime(2024, 4, 25)) SELL_TO_OPEN 35m
        
        let transactions = [transaction1; transaction2; transaction3] // Out of chronological order
        
        // Act
        let stats = convertTransactions transactions
        
        // Assert - should process all transactions regardless of input order
        Assert.That(stats.TotalTransactions, Is.EqualTo(3))
        Assert.That(stats.BrokerMovementsCreated, Is.EqualTo(2)) // 2 money movements
        Assert.That(stats.OptionTradesCreated, Is.EqualTo(1)) // 1 option trade
        Assert.That(stats.StockTradesCreated, Is.EqualTo(0))
        Assert.That(stats.ErrorsCount, Is.EqualTo(0))

    [<Test>]
    member _.``Should handle unknown transaction types with error`` () =
        // Arrange
        let transaction = {
            Date = DateTime(2024, 4, 22)
            TransactionType = ReceiveDeliver("ACAT") // Unknown type
            Symbol = None
            InstrumentType = None
            Description = "Unknown transaction"
            Value = 100m
            Quantity = 0m
            AveragePrice = None
            Commissions = 0m
            Fees = 0m
            Multiplier = None
            RootSymbol = None
            UnderlyingSymbol = None
            ExpirationDate = None
            StrikePrice = None
            CallOrPut = None
            OrderNumber = None
            Currency = "USD"
            RawCsvLine = "Test unknown line"
            LineNumber = 5
        }
        
        // Act
        let result = convertTransaction transaction
        
        // Assert
        Assert.That(result.BrokerMovementsCount, Is.EqualTo(0))
        Assert.That(result.OptionTradesCount, Is.EqualTo(0))
        Assert.That(result.StockTradesCount, Is.EqualTo(0))
        Assert.That(result.Errors, Is.Not.Empty)
        Assert.That(result.Errors.[0], Does.Contain("Unsupported transaction type"))

    [<Test>]
    member _.``Should handle empty transaction list`` () =
        // Act
        let stats = convertTransactions []
        
        // Assert
        Assert.That(stats.TotalTransactions, Is.EqualTo(0))
        Assert.That(stats.BrokerMovementsCreated, Is.EqualTo(0))
        Assert.That(stats.OptionTradesCreated, Is.EqualTo(0))
        Assert.That(stats.StockTradesCreated, Is.EqualTo(0))
        Assert.That(stats.ErrorsCount, Is.EqualTo(0))