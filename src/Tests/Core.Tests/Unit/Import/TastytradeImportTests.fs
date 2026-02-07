namespace Binnaculum.Core.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open System
open System.IO
open Binnaculum.Core.Import
open Binnaculum.Core.Import.TastytradeModels
open Binnaculum.Core.Import.TastytradeStatementParser
open Binnaculum.Core.Import.TastytradeOptionSymbolParser
open Binnaculum.Core.Import.TastytradeStrategyDetector

[<TestClass>]
type TastytradeImportTests() =

    let testDataPath =
        Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "TestData", "Tastytrade_Samples")

    [<TestMethod>]
    member this.``TastytradeOptionSymbolParser should parse complex option symbol correctly``() =
        let result = parseOptionSymbol "PLTR  240531C00022000"

        Assert.AreEqual("PLTR", result.Ticker)
        Assert.AreEqual(DateTime(2024, 5, 31), result.ExpirationDate)
        Assert.AreEqual(22.00m, result.Strike)
        Assert.AreEqual("CALL", result.OptionType)

    [<TestMethod>]
    member this.``TastytradeOptionSymbolParser should parse PUT option correctly``() =
        let result = parseOptionSymbol "AAPL  240614P00185000"

        Assert.AreEqual("AAPL", result.Ticker)
        Assert.AreEqual(DateTime(2024, 6, 14), result.ExpirationDate)
        Assert.AreEqual(185.00m, result.Strike)
        Assert.AreEqual("PUT", result.OptionType)

    [<TestMethod>]
    member this.``TastytradeOptionSymbolParser should handle invalid symbols gracefully``() =
        try
            parseOptionSymbol "INVALID_SYMBOL" |> ignore
            Assert.Fail("Should have thrown exception for invalid symbol")
        with _ ->
            Assert.IsTrue(true, "Exception thrown as expected") // Test passed

        try
            parseOptionSymbol "" |> ignore
            Assert.Fail("Should have thrown exception for empty symbol")
        with _ ->
            Assert.IsTrue(true, "Exception thrown as expected") // Test passed

    [<TestMethod>]
    member this.``TastytradeOptionSymbolParser isValidOptionSymbol should work correctly``() =
        Assert.IsTrue(isValidOptionSymbol "PLTR  240531C00022000")
        Assert.IsTrue(isValidOptionSymbol "AAPL  240614P00185000")
        Assert.IsFalse(isValidOptionSymbol "INVALID")
        Assert.IsFalse(isValidOptionSymbol "")

    [<TestMethod>]
    member this.``TastytradeOptionSymbolParser extractTicker should work correctly``() =
        let ticker1 = extractTicker "PLTR  240531C00022000"
        let ticker2 = extractTicker "AAPL  240614P00185000"

        Assert.AreEqual("PLTR", ticker1)
        Assert.AreEqual("AAPL", ticker2)

    [<TestMethod>]
    member this.``TastytradeOptionSymbolParser formatOptionSymbol round-trip should work``() =
        let original = "AAPL  240614C00180000"
        let parsed = parseOptionSymbol original
        let formatted = formatOptionSymbol parsed

        Assert.AreEqual(original, formatted)

    [<TestMethod>]
    member this.``TastytradeStatementParser should parse options heavy sample file``() =
        let filePath = Path.Combine(testDataPath, "tastytrade_options_heavy_sample.csv")
        let result = parseTransactionHistoryFromFile filePath

        let errorMessages =
            String.Join("; ", result.Errors |> List.map (fun e -> e.ErrorMessage))

        Assert.AreEqual(0, result.Errors.Length, sprintf "Parsing errors: %s" errorMessages)
        Assert.AreEqual(6, result.Transactions.Length)
        Assert.AreEqual(6, result.ProcessedLines)

        // Verify all transactions are option trades
        let optionTransactions =
            result.Transactions
            |> List.filter (fun t -> TransactionTypeDetection.isOptionTransaction t.InstrumentType)

        Assert.AreEqual(6, optionTransactions.Length)

    [<TestMethod>]
    member this.``TastytradeStatementParser should parse mixed trading sample file``() =
        let filePath = Path.Combine(testDataPath, "tastytrade_mixed_trading_sample.csv")
        let result = parseTransactionHistoryFromFile filePath

        let errorMessages =
            String.Join("; ", result.Errors |> List.map (fun e -> e.ErrorMessage))

        Assert.AreEqual(0, result.Errors.Length, sprintf "Parsing errors: %s" errorMessages)
        Assert.AreEqual(11, result.Transactions.Length)

        // Verify transaction type distribution
        let equityTrades =
            result.Transactions
            |> List.filter (fun t -> TransactionTypeDetection.isEquityTrade t.InstrumentType)

        let equityOptionTrades =
            result.Transactions
            |> List.filter (fun t -> TransactionTypeDetection.isEquityOptionTransaction t.InstrumentType)

        let futureOptionTrades =
            result.Transactions
            |> List.filter (fun t -> TransactionTypeDetection.isFutureOptionTransaction t.InstrumentType)

        let moneyMovements =
            result.Transactions
            |> List.filter (fun t ->
                match t.TransactionType with
                | MoneyMovement(_) -> true
                | _ -> false)

        Assert.AreEqual(3, equityTrades.Length, "Should have 3 equity trades (AAPL buy, TSLA buy, TSLA sell)")
        Assert.AreEqual(1, equityOptionTrades.Length, "Should have 1 equity option (AAPL call)")
        Assert.AreEqual(4, futureOptionTrades.Length, "Should have 4 future option trades (/ESZ5 spreads)")
        Assert.AreEqual(3, moneyMovements.Length, "Should have 3 money movements (deposit, fee, interest)")

    [<TestMethod>]
    member this.``TastytradeStatementParser should parse ACAT transfer sample file``() =
        let filePath = Path.Combine(testDataPath, "tastytrade_acat_transfer_sample.csv")
        let result = parseTransactionHistoryFromFile filePath

        let errorMessages =
            String.Join("; ", result.Errors |> List.map (fun e -> e.ErrorMessage))

        Assert.AreEqual(0, result.Errors.Length, sprintf "Parsing errors: %s" errorMessages)

        Assert.AreEqual(3, result.Transactions.Length, "Should parse all 3 transactions: ACH deposit, ACAT equity transfer, and money transfer from Interactive Brokers")

        // Verify the ACH deposit with quoted comma-separated value parses correctly
        let depositWith1700 =
            result.Transactions |> List.tryFind (fun t -> t.Value = 1700.00m)

        Assert.IsTrue(depositWith1700.IsSome, "Should find the $1,700.00 ACH deposit")
        Assert.AreEqual("ACH DEPOSIT", depositWith1700.Value.Description)

        Assert.IsTrue(
            (match depositWith1700.Value.TransactionType with
             | MoneyMovement(Deposit) -> true
             | _ -> false),
            "ACH DEPOSIT should be a Money Movement Deposit"
        )

        // Verify ACAT equity transfer (Receive Deliver)
        let acatEquityTransfer =
            result.Transactions
            |> List.tryFind (fun t ->
                match t.TransactionType with
                | ReceiveDeliver("ACAT") -> true
                | _ -> false)

        Assert.IsTrue(acatEquityTransfer.IsSome, "Should find the ACAT equity transfer")
        Assert.AreEqual(Some "MPW", acatEquityTransfer.Value.Symbol)
        Assert.AreEqual(Some "Equity", acatEquityTransfer.Value.InstrumentType)
        Assert.AreEqual(1000m, acatEquityTransfer.Value.Quantity, "Should receive 1000 shares of MPW")
        Assert.AreEqual(0m, acatEquityTransfer.Value.Value, "ACAT equity transfers have 0 cash value")

        // Verify money transfer from Interactive Brokers
        let moneyTransfer =
            result.Transactions
            |> List.tryFind (fun t ->
                match t.TransactionType with
                | MoneyMovement(Transfer) -> true
                | _ -> false)

        Assert.IsTrue(moneyTransfer.IsSome, "Should find the money transfer")
        Assert.AreEqual(313.46m, moneyTransfer.Value.Value, "Should transfer $313.46")

        Assert.AreEqual("TRANSFER FROM INTERACTIVE B", moneyTransfer.Value.Description, "Should have correct description")

    [<TestMethod>]
    member this.``TastytradeStatementParser should handle dates with timezones correctly``() =
        let csvContent =
            """Date,Type,Sub Type,Action,Symbol,Instrument Type,Description,Value,Quantity,Average Price,Commissions,Fees,Multiplier,Root Symbol,Underlying Symbol,Expiration Date,Strike Price,Call or Put,Order #,Total,Currency
2024-05-31T14:42:13+0100,Money Movement,Deposit,,,,Test deposit,1000.00,0,,0.00,0.00,,,,,,,,1000.00,USD"""

        let result = parseTransactionHistory csvContent

        Assert.AreEqual(0, result.Errors.Length)
        Assert.AreEqual(1, result.Transactions.Length)

        let transaction = result.Transactions.[0]
        Assert.AreEqual(2024, transaction.Date.Year)
        Assert.AreEqual(5, transaction.Date.Month)
        Assert.AreEqual(31, transaction.Date.Day)

    [<TestMethod>]
    member this.``TastytradeStrategyDetector should group transactions by order number``() =
        // Create sample transactions with same order number
        let transactions =
            [ { Date = DateTime.Now
                TransactionType = Trade(BuyToClose, BUY_TO_CLOSE)
                Symbol = Some "AAPL"
                InstrumentType = Some "Equity Option"
                Description = "Test 1"
                Value = -50m
                Quantity = 1m
                AveragePrice = Some -50m
                Commissions = 1m
                Fees = 0.13m
                Multiplier = Some 100m
                RootSymbol = Some "AAPL"
                UnderlyingSymbol = Some "AAPL"
                ExpirationDate = Some(DateTime(2024, 5, 31))
                StrikePrice = Some 180m
                CallOrPut = Some "CALL"
                OrderNumber = Some "123456789"
                Currency = "USD"
                RawCsvLine = ""
                LineNumber = 1 }
              { Date = DateTime.Now
                TransactionType = Trade(SellToOpen, SELL_TO_OPEN)
                Symbol = Some "AAPL"
                InstrumentType = Some "Equity Option"
                Description = "Test 2"
                Value = 75m
                Quantity = 1m
                AveragePrice = Some 75m
                Commissions = 1m
                Fees = 0.14m
                Multiplier = Some 100m
                RootSymbol = Some "AAPL"
                UnderlyingSymbol = Some "AAPL"
                ExpirationDate = Some(DateTime(2024, 6, 14))
                StrikePrice = Some 185m
                CallOrPut = Some "CALL"
                OrderNumber = Some "123456789"
                Currency = "USD"
                RawCsvLine = ""
                LineNumber = 2 } ]

        let strategies = detectStrategies transactions

        Assert.AreEqual(1, strategies.Length)
        Assert.AreEqual(2, strategies.[0].Transactions.Length)
        Assert.AreEqual("123456789", strategies.[0].OrderNumber)

    [<TestMethod>]
    member this.``TastytradeStrategyDetector should detect calendar spread``() =
        // Create transactions that form a calendar spread (same strike, different expirations)
        let transactions =
            [ { Date = DateTime.Now
                TransactionType = Trade(BuyToClose, BUY_TO_CLOSE)
                Symbol = Some "AAPL"
                InstrumentType = Some "Equity Option"
                Description = "Buy to close near expiration"
                Value = -50m
                Quantity = 1m
                AveragePrice = Some -50m
                Commissions = 1m
                Fees = 0.13m
                Multiplier = Some 100m
                RootSymbol = Some "AAPL"
                UnderlyingSymbol = Some "AAPL"
                ExpirationDate = Some(DateTime(2024, 5, 31))
                StrikePrice = Some 180m
                CallOrPut = Some "CALL"
                OrderNumber = Some "123456789"
                Currency = "USD"
                RawCsvLine = ""
                LineNumber = 1 }
              { Date = DateTime.Now
                TransactionType = Trade(SellToOpen, SELL_TO_OPEN)
                Symbol = Some "AAPL"
                InstrumentType = Some "Equity Option"
                Description = "Sell to open far expiration"
                Value = 75m
                Quantity = 1m
                AveragePrice = Some 75m
                Commissions = 1m
                Fees = 0.14m
                Multiplier = Some 100m
                RootSymbol = Some "AAPL"
                UnderlyingSymbol = Some "AAPL"
                ExpirationDate = Some(DateTime(2024, 6, 14))
                StrikePrice = Some 180m
                CallOrPut = Some "CALL"
                OrderNumber = Some "123456789"
                Currency = "USD"
                RawCsvLine = ""
                LineNumber = 2 } ]

        let strategies = detectStrategies transactions

        Assert.AreEqual(1, strategies.Length)
        Assert.AreEqual(Some CalendarSpread, strategies.[0].StrategyType)

    [<TestMethod>]
    member this.``TastytradeStrategyDetector should detect single leg``() =
        let transactions =
            [ { Date = DateTime.Now
                TransactionType = Trade(SellToOpen, SELL_TO_OPEN)
                Symbol = Some "NVDA"
                InstrumentType = Some "Equity Option"
                Description = "Single leg trade"
                Value = 250m
                Quantity = 1m
                AveragePrice = Some 250m
                Commissions = 1m
                Fees = 0.13m
                Multiplier = Some 100m
                RootSymbol = Some "NVDA"
                UnderlyingSymbol = Some "NVDA"
                ExpirationDate = Some(DateTime(2024, 5, 31))
                StrikePrice = Some 400m
                CallOrPut = Some "CALL"
                OrderNumber = Some "111222333"
                Currency = "USD"
                RawCsvLine = ""
                LineNumber = 1 } ]

        let strategies = detectStrategies transactions

        Assert.AreEqual(1, strategies.Length)
        Assert.AreEqual(Some SingleLeg, strategies.[0].StrategyType)

    [<TestMethod>]
    member this.``TransactionTypeDetection should correctly identify option transactions``() =
        Assert.IsTrue(TransactionTypeDetection.isOptionTransaction (Some "Equity Option"))
        Assert.IsFalse(TransactionTypeDetection.isOptionTransaction (Some "Equity"))
        Assert.IsFalse(TransactionTypeDetection.isOptionTransaction None)

    [<TestMethod>]
    member this.``TransactionTypeDetection should correctly identify equity trades``() =
        Assert.IsTrue(TransactionTypeDetection.isEquityTrade (Some "Equity"))
        Assert.IsFalse(TransactionTypeDetection.isEquityTrade (Some "Equity Option"))
        Assert.IsFalse(TransactionTypeDetection.isEquityTrade None)

    [<TestMethod>]
    member this.``TastytradeStatementParser should handle empty CSV gracefully``() =
        let result = parseTransactionHistory ""

        Assert.AreEqual(0, result.Transactions.Length)
        Assert.AreEqual(0, result.Errors.Length)
        Assert.AreEqual(0, result.ProcessedLines)

    [<TestMethod>]
    member this.``TastytradeStatementParser should handle header-only CSV``() =
        let csvContent =
            "Date,Type,Sub Type,Action,Symbol,Instrument Type,Description,Value,Quantity,Average Price,Commissions,Fees,Multiplier,Root Symbol,Underlying Symbol,Expiration Date,Strike Price,Call or Put,Order #,Total,Currency"

        let result = parseTransactionHistory csvContent

        Assert.AreEqual(0, result.Transactions.Length)
        Assert.AreEqual(0, result.Errors.Length)
        Assert.AreEqual(0, result.ProcessedLines)

    [<TestMethod>]
    member this.``TastytradeStatementParser should handle malformed CSV lines``() =
        let csvContent =
            """Date,Type,Sub Type,Action,Symbol,Instrument Type,Description,Value,Quantity,Average Price,Commissions,Fees,Multiplier,Root Symbol,Underlying Symbol,Expiration Date,Strike Price,Call or Put,Order #,Total,Currency
2024-05-31T14:42:13+0100,Trade,Buy to Close,BUY_TO_CLOSE""" // Incomplete line

        let result = parseTransactionHistory csvContent

        Assert.AreEqual(0, result.Transactions.Length)
        Assert.AreEqual(1, result.Errors.Length)
        Assert.AreEqual(MissingRequiredField("All fields"), result.Errors.[0].ErrorType)

    [<TestMethod>]
    member this.``TastytradeStatementParser should handle invalid dates``() =
        let csvContent =
            """Date,Type,Sub Type,Action,Symbol,Instrument Type,Description,Value,Quantity,Average Price,Commissions,Fees,Multiplier,Root Symbol,Underlying Symbol,Expiration Date,Strike Price,Call or Put,Order #,Total,Currency
INVALID_DATE,Trade,Buy to Close,BUY_TO_CLOSE,AAPL,Equity,Test,-100,1,-100,0,0,,,,,,,123,-100,USD"""

        let result = parseTransactionHistory csvContent

        Assert.AreEqual(0, result.Transactions.Length)
        Assert.AreEqual(1, result.Errors.Length)
        Assert.AreEqual(InvalidDateFormat, result.Errors.[0].ErrorType)

    [<TestMethod>]
    member this.``TastytradeStatementParser should parse decimal values correctly``() =
        let csvContent =
            """Date,Type,Sub Type,Action,Symbol,Instrument Type,Description,Value,Quantity,Average Price,Commissions,Fees,Multiplier,Root Symbol,Underlying Symbol,Expiration Date,Strike Price,Call or Put,Order #,Total,Currency
2024-05-31T14:42:13+0100,Money Movement,Deposit,,,,Test deposit,"1,234.56",0,,0.00,0.00,,,,,,,,"1,234.56",USD"""

        let result = parseTransactionHistory csvContent

        Assert.AreEqual(0, result.Errors.Length)
        Assert.AreEqual(1, result.Transactions.Length)
        Assert.AreEqual(1234.56m, result.Transactions.[0].Value)

    [<TestMethod>]
    member this.``TastytradeStatementParser should parse legacy format lending rebate file``() =
        let filePath = Path.Combine(testDataPath, "LendingRebate.csv")
        let result = parseTransactionHistoryFromFile filePath

        let errorMessages =
            String.Join("; ", result.Errors |> List.map (fun e -> e.ErrorMessage))

        Assert.AreEqual(0, result.Errors.Length, sprintf "Parsing errors: %s" errorMessages)

        Assert.AreEqual(2, result.Transactions.Length, "Should parse 2 transactions (debit interest + lending rebate)"
        )

        Assert.AreEqual(2, result.ProcessedLines)

        // Transactions are in CSV file order (not sorted by date)
        // First transaction in CSV: June 17 - Debit Interest
        let debitInterestTransaction = result.Transactions.[0]
        Assert.AreEqual("FROM 05/16 THRU 06/15 @11    %", debitInterestTransaction.Description)
        Assert.AreEqual(-0.01m, debitInterestTransaction.Value)
        Assert.AreEqual(DateTime(2024, 6, 17, 22, 0, 0), debitInterestTransaction.Date)

        // Verify it's a Money Movement with DebitInterest subtype
        match debitInterestTransaction.TransactionType with
        | MoneyMovement(subType) ->
            Assert.IsTrue((subType = DebitInterest), "Should be identified as DebitInterest subtype")
        | _ -> Assert.Fail("Transaction should be a Money Movement")

        // Second transaction in CSV: June 14 - Lending Rebate
        let lendingTransaction = result.Transactions.[1]
        Assert.AreEqual("FULLYPAID LENDING REBATE", lendingTransaction.Description)
        Assert.AreEqual(0.30m, lendingTransaction.Value)
        Assert.AreEqual(DateTime(2024, 6, 14, 22, 0, 0), lendingTransaction.Date)

        // Verify it's a Money Movement with Lending subtype
        match lendingTransaction.TransactionType with
        | MoneyMovement(subType) -> Assert.IsTrue((subType = Lending), "Should be identified as Lending subtype")
        | _ -> Assert.Fail("Transaction should be a Money Movement")

    [<TestMethod>]
    member this.``TastytradeStatementParser should parse and verify Future Option transactions correctly``() =
        let filePath = Path.Combine(testDataPath, "tastytrade_mixed_trading_sample.csv")
        let result = parseTransactionHistoryFromFile filePath

        let errorMessages =
            String.Join("; ", result.Errors |> List.map (fun e -> e.ErrorMessage))

        Assert.AreEqual(0, result.Errors.Length, sprintf "Parsing errors: %s" errorMessages)
        Assert.AreEqual(11, result.Transactions.Length, "Should have 11 total transactions")

        // Extract all Future Option transactions
        let futureOptions =
            result.Transactions
            |> List.filter (fun t -> TransactionTypeDetection.isFutureOptionTransaction t.InstrumentType)

        Assert.AreEqual(4, futureOptions.Length, "Should have 4 Future Option transactions")

        // Verify Transaction 1: Buy to Close /ESZ5 E2BX5 251111C6880 @ 1.1
        let buyToClose6880 =
            futureOptions
            |> List.tryFind (fun t ->
                match t.TransactionType with
                | Trade(BuyToClose, BUY_TO_CLOSE) -> t.Symbol = Some "/ESZ5" && t.Value = -55.00m
                | _ -> false)

        Assert.IsTrue(buyToClose6880.IsSome, "Should find Buy to Close 6880 call")
        let tx1 = buyToClose6880.Value
        Assert.AreEqual(DateTime(2025, 11, 11, 17, 50, 40), tx1.Date)
        Assert.AreEqual(Some "/ESZ5", tx1.Symbol, "Symbol should use UnderlyingSymbol (/ESZ5)")
        Assert.AreEqual(Some "Future Option", tx1.InstrumentType)
        Assert.AreEqual("Bought 1 /ESZ5 E2BX5 11/11/25 Call 6880.00 @ 1.1", tx1.Description)
        Assert.AreEqual(-55.00m, tx1.Value, "Value should be -55.00 (cost to buy to close)")
        Assert.AreEqual(1m, tx1.Quantity, "Quantity should be 1 contract")
        Assert.AreEqual(Some -55.00m, tx1.AveragePrice)
        Assert.AreEqual(-1.25m, tx1.Commissions, "Commissions should be -1.25")
        Assert.AreEqual(-0.87m, tx1.Fees, "Fees should be -0.87")
        Assert.AreEqual(Some 1m, tx1.Multiplier, "Future Option multiplier should be 1 (not 100)")
        Assert.AreEqual(Some "./ESZ5 E2BX5", tx1.RootSymbol)
        Assert.AreEqual(Some "/ESZ5", tx1.UnderlyingSymbol)
        Assert.AreEqual(Some(DateTime(2025, 11, 11)), tx1.ExpirationDate)
        Assert.AreEqual(Some 6880m, tx1.StrikePrice)
        Assert.AreEqual(Some "CALL", tx1.CallOrPut)
        Assert.AreEqual(Some "420000990", tx1.OrderNumber)

        // Verify Transaction 2: Sell to Close /ESZ5 E2BX5 251111C6865 @ 4.9
        let sellToClose6865 =
            futureOptions
            |> List.tryFind (fun t ->
                match t.TransactionType with
                | Trade(SellToClose, SELL_TO_CLOSE) -> t.Symbol = Some "/ESZ5" && t.Value = 245.00m
                | _ -> false)

        Assert.IsTrue(sellToClose6865.IsSome, "Should find Sell to Close 6865 call")
        let tx2 = sellToClose6865.Value
        Assert.AreEqual(DateTime(2025, 11, 11, 17, 50, 40), tx2.Date)
        Assert.AreEqual(Some "/ESZ5", tx2.Symbol, "Symbol should use UnderlyingSymbol (/ESZ5)")
        Assert.AreEqual(Some "Future Option", tx2.InstrumentType)
        Assert.AreEqual("Sold 1 /ESZ5 E2BX5 11/11/25 Call 6865.00 @ 4.9", tx2.Description)
        Assert.AreEqual(245.00m, tx2.Value, "Value should be 245.00 (proceeds from sell to close)")
        Assert.AreEqual(1m, tx2.Quantity, "Quantity should be 1 contract")
        Assert.AreEqual(Some 245.00m, tx2.AveragePrice)
        Assert.AreEqual(Some 1m, tx2.Multiplier, "Future Option multiplier should be 1")
        Assert.AreEqual(Some 6865m, tx2.StrikePrice)
        Assert.AreEqual(Some "CALL", tx2.CallOrPut)
        Assert.AreEqual(Some "420000990", tx2.OrderNumber, "Same order number as first transaction")

        // Verify Transaction 3: Sell to Open /ESZ5 E2BX5 251111C6880 @ 1.75
        let sellToOpen6880 =
            futureOptions
            |> List.tryFind (fun t ->
                match t.TransactionType with
                | Trade(SellToOpen, SELL_TO_OPEN) -> t.Symbol = Some "/ESZ5" && t.Value = 87.50m
                | _ -> false)

        Assert.IsTrue(sellToOpen6880.IsSome, "Should find Sell to Open 6880 call")
        let tx3 = sellToOpen6880.Value
        Assert.AreEqual(DateTime(2025, 11, 11, 14, 56, 57), tx3.Date)
        Assert.AreEqual(Some "/ESZ5", tx3.Symbol, "Symbol should use UnderlyingSymbol (/ESZ5)")
        Assert.AreEqual(Some "Future Option", tx3.InstrumentType)
        Assert.AreEqual("Sold 1 /ESZ5 E2BX5 11/11/25 Call 6880.00 @ 1.75", tx3.Description)
        Assert.AreEqual(87.50m, tx3.Value, "Value should be 87.50")
        Assert.AreEqual(1m, tx3.Quantity)
        Assert.AreEqual(Some 1m, tx3.Multiplier)
        Assert.AreEqual(Some 6880m, tx3.StrikePrice)
        Assert.AreEqual(Some "CALL", tx3.CallOrPut)
        Assert.AreEqual(Some "419888474", tx3.OrderNumber)

        // Verify Transaction 4: Buy to Open /ESZ5 E2BX5 251111C6865 @ 4.5
        let buyToOpen6865 =
            futureOptions
            |> List.tryFind (fun t ->
                match t.TransactionType with
                | Trade(BuyToOpen, BUY_TO_OPEN) -> t.Symbol = Some "/ESZ5" && t.Value = -225.00m
                | _ -> false)

        Assert.IsTrue(buyToOpen6865.IsSome, "Should find Buy to Open 6865 call")
        let tx4 = buyToOpen6865.Value
        Assert.AreEqual(DateTime(2025, 11, 11, 14, 56, 57), tx4.Date)
        Assert.AreEqual(Some "/ESZ5", tx4.Symbol, "Symbol should use UnderlyingSymbol (/ESZ5)")
        Assert.AreEqual(Some "Future Option", tx4.InstrumentType)
        Assert.AreEqual("Bought 1 /ESZ5 E2BX5 11/11/25 Call 6865.00 @ 4.5", tx4.Description)
        Assert.AreEqual(-225.00m, tx4.Value, "Value should be -225.00 (cost)")
        Assert.AreEqual(1m, tx4.Quantity)
        Assert.AreEqual(Some -225.00m, tx4.AveragePrice)
        Assert.AreEqual(Some 1m, tx4.Multiplier)
        Assert.AreEqual(Some 6865m, tx4.StrikePrice)
        Assert.AreEqual(Some "CALL", tx4.CallOrPut)
        Assert.AreEqual(Some "419888474", tx4.OrderNumber, "Same order number as third transaction")

        // Verify all Future Options have correct common attributes
        for futureOption in futureOptions do
            Assert.AreEqual("USD", futureOption.Currency)
            Assert.AreEqual(-1.25m, futureOption.Commissions, "All should have -1.25 commissions")
            Assert.AreEqual(-0.87m, futureOption.Fees, "All should have -0.87 fees")
            Assert.IsNotNull(futureOption.RootSymbol, "Root symbol should be set")
            Assert.AreEqual(Some "/ESZ5", futureOption.UnderlyingSymbol, "All ES futures")
            Assert.AreEqual(Some(DateTime(2025, 11, 11)), futureOption.ExpirationDate, "All expire 11/11/25")
            Assert.AreEqual(Some "CALL", futureOption.CallOrPut, "All are call options")
