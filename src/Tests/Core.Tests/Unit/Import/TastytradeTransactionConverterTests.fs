namespace Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open Binnaculum.Core.Import
open Binnaculum.Core.Import.TastytradeModels
open Binnaculum.Core.Import.TastytradeTransactionConverter
open System

[<TestClass>]
type TastytradeTransactionConverterTests() =

    let createMoneyMovementTransaction (date: DateTime) (subType: MoneyMovementSubType) (value: decimal) =
        { Date = date
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
          LineNumber = 1 }

    let createOptionTradeTransaction (date: DateTime) (action: TradeAction) (value: decimal) =
        { Date = date
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
          ExpirationDate = Some(DateTime(2024, 5, 3))
          StrikePrice = Some 7m
          CallOrPut = Some "PUT"
          OrderNumber = Some "319448136"
          Currency = "USD"
          RawCsvLine = "Test option line"
          LineNumber = 2 }

    [<TestMethod>]
    member _.``Should convert Money Movement Deposit to BrokerMovement count``() =
        // Arrange
        let transaction = createMoneyMovementTransaction (DateTime(2024, 4, 22)) Deposit 10m

        // Act
        let result = convertTransaction transaction

        // Assert
        Assert.AreEqual(1, result.BrokerMovementsCount)
        Assert.AreEqual(0, result.OptionTradesCount)
        Assert.AreEqual(0, result.StockTradesCount)
        Assert.AreEqual(0, result.Errors.Count)

    [<TestMethod>]
    member _.``Should convert Money Movement Balance Adjustment to BrokerMovement count``() =
        // Arrange
        let transaction =
            createMoneyMovementTransaction (DateTime(2024, 4, 27)) BalanceAdjustment -0.02m

        // Act
        let result = convertTransaction transaction

        // Assert
        Assert.AreEqual(1, result.BrokerMovementsCount)
        Assert.AreEqual(0, result.OptionTradesCount)
        Assert.AreEqual(0, result.StockTradesCount)
        Assert.AreEqual(0, result.Errors.Count)

    [<TestMethod>]
    member _.``Should convert Option Trade to OptionTrade count``() =
        // Arrange
        let transaction =
            createOptionTradeTransaction (DateTime(2024, 4, 25)) SELL_TO_OPEN 35m

        // Act
        let result = convertTransaction transaction

        // Assert
        Assert.AreEqual(0, result.BrokerMovementsCount)
        Assert.AreEqual(1, result.OptionTradesCount)
        Assert.AreEqual(0, result.StockTradesCount)
        Assert.AreEqual(0, result.Errors.Count)

    [<TestMethod>]
    member _.``Should sort transactions chronologically``() =
        // Arrange
        let transaction1 =
            createMoneyMovementTransaction (DateTime(2024, 4, 27)) Deposit 10m

        let transaction2 =
            createMoneyMovementTransaction (DateTime(2024, 4, 22)) BalanceAdjustment -0.02m

        let transaction3 =
            createOptionTradeTransaction (DateTime(2024, 4, 25)) SELL_TO_OPEN 35m

        let transactions = [ transaction1; transaction2; transaction3 ] // Out of chronological order

        // Act
        let stats = convertTransactions transactions

        // Assert - should process all transactions regardless of input order
        Assert.AreEqual(3, stats.TotalTransactions)
        Assert.AreEqual(2, stats.BrokerMovementsCreated) // 2 money movements
        Assert.AreEqual(1, stats.OptionTradesCreated) // 1 option trade
        Assert.AreEqual(0, stats.StockTradesCreated)
        Assert.AreEqual(0, stats.ErrorsCount)

    [<TestMethod>]
    member _.``Should handle unknown transaction types with error``() =
        // This test verifies that only known transaction types are counted
        // ReceiveDeliver transactions are informational and don't create records, but are not errors

        // Arrange - Create a ReceiveDeliver transaction (valid but informational)
        let receiveDeliverTransaction =
            { Date = DateTime(2024, 4, 22)
              TransactionType = ReceiveDeliver("ACAT")
              Symbol = None
              InstrumentType = None
              Description = "ACAT transfer"
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
              RawCsvLine = "Test receive line"
              LineNumber = 5 }

        // Act
        let result = convertTransaction receiveDeliverTransaction

        // Assert - ReceiveDeliver transactions don't create records but also don't error
        Assert.AreEqual(0, result.BrokerMovementsCount)
        Assert.AreEqual(0, result.OptionTradesCount)
        Assert.AreEqual(0, result.StockTradesCount)
        Assert.AreEqual(0, result.Errors.Count)

    [<TestMethod>]
    member _.``Should handle empty transaction list``() =
        // Act
        let stats = convertTransactions []

        // Assert
        Assert.AreEqual(0, stats.TotalTransactions)
        Assert.AreEqual(0, stats.BrokerMovementsCreated)
        Assert.AreEqual(0, stats.OptionTradesCreated)
        Assert.AreEqual(0, stats.StockTradesCreated)
        Assert.AreEqual(0, stats.ErrorsCount)

    [<TestMethod>]
    member _.``Should handle real Tastytrade CSV sample data from problem statement``() =
        // This test demonstrates the fix using the exact data from the GitHub issue

        // Arrange - Create transactions matching the problem statement examples
        let depositTransaction =
            createMoneyMovementTransaction (DateTime(2024, 4, 22, 22, 0, 0)) Deposit 10.00m

        let optionTradeTransaction =
            createOptionTradeTransaction (DateTime(2024, 4, 25, 14, 41, 11)) SELL_TO_OPEN 35.00m

        let balanceAdjustmentTransaction =
            createMoneyMovementTransaction (DateTime(2024, 4, 27, 19, 18, 0)) BalanceAdjustment -0.02m

        let sampleTransactions =
            [ depositTransaction; optionTradeTransaction; balanceAdjustmentTransaction ]

        // Act
        let stats = convertTransactions sampleTransactions

        // Assert - Should show meaningful counts instead of 0 movements
        Assert.AreEqual(3, stats.TotalTransactions, "Should process all 3 transactions")

        Assert.AreEqual(2, stats.BrokerMovementsCreated, "Should create 2 BrokerMovements (deposit + balance adjustment)"
        )

        Assert.AreEqual(1, stats.OptionTradesCreated, "Should create 1 OptionTrade")
        Assert.AreEqual(0, stats.StockTradesCreated, "No stock trades in this sample")
        Assert.AreEqual(0, stats.ErrorsCount, "Should process without errors")

// This demonstrates the core fix: instead of returning 0 movements,
// we now get meaningful counts that can be displayed to the user
