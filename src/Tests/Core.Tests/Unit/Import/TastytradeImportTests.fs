namespace Binnaculum.Core.Tests

open NUnit.Framework
open System
open System.IO
open Binnaculum.Core.Import
open Binnaculum.Core.Import.TastytradeModels
open Binnaculum.Core.Import.TastytradeStatementParser
open Binnaculum.Core.Import.TastytradeOptionSymbolParser
open Binnaculum.Core.Import.TastytradeStrategyDetector

[<TestFixture>]
type TastytradeImportTests() =

    let testDataPath =
        Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "TestData", "Tastytrade_Samples")

    [<Test>]
    member this.``TastytradeOptionSymbolParser should parse complex option symbol correctly``() =
        let result = parseOptionSymbol "PLTR  240531C00022000"

        Assert.That(result.Ticker, Is.EqualTo("PLTR"))
        Assert.That(result.ExpirationDate, Is.EqualTo(DateTime(2024, 5, 31)))
        Assert.That(result.Strike, Is.EqualTo(22.00m))
        Assert.That(result.OptionType, Is.EqualTo("CALL"))

    [<Test>]
    member this.``TastytradeOptionSymbolParser should parse PUT option correctly``() =
        let result = parseOptionSymbol "AAPL  240614P00185000"

        Assert.That(result.Ticker, Is.EqualTo("AAPL"))
        Assert.That(result.ExpirationDate, Is.EqualTo(DateTime(2024, 6, 14)))
        Assert.That(result.Strike, Is.EqualTo(185.00m))
        Assert.That(result.OptionType, Is.EqualTo("PUT"))

    [<Test>]
    member this.``TastytradeOptionSymbolParser should handle invalid symbols gracefully``() =
        try
            parseOptionSymbol "INVALID_SYMBOL" |> ignore
            Assert.Fail("Should have thrown exception for invalid symbol")
        with _ ->
            Assert.Pass("Exception thrown as expected")

        try
            parseOptionSymbol "" |> ignore
            Assert.Fail("Should have thrown exception for empty symbol")
        with _ ->
            Assert.Pass("Exception thrown as expected")

    [<Test>]
    member this.``TastytradeOptionSymbolParser isValidOptionSymbol should work correctly``() =
        Assert.That(isValidOptionSymbol "PLTR  240531C00022000", Is.True)
        Assert.That(isValidOptionSymbol "AAPL  240614P00185000", Is.True)
        Assert.That(isValidOptionSymbol "INVALID", Is.False)
        Assert.That(isValidOptionSymbol "", Is.False)

    [<Test>]
    member this.``TastytradeOptionSymbolParser extractTicker should work correctly``() =
        let ticker1 = extractTicker "PLTR  240531C00022000"
        let ticker2 = extractTicker "AAPL  240614P00185000"

        Assert.That(ticker1, Is.EqualTo("PLTR"))
        Assert.That(ticker2, Is.EqualTo("AAPL"))

    [<Test>]
    member this.``TastytradeOptionSymbolParser formatOptionSymbol round-trip should work``() =
        let original = "AAPL  240614C00180000"
        let parsed = parseOptionSymbol original
        let formatted = formatOptionSymbol parsed

        Assert.That(formatted, Is.EqualTo(original))

    [<Test>]
    member this.``TastytradeStatementParser should parse options heavy sample file``() =
        let filePath = Path.Combine(testDataPath, "tastytrade_options_heavy_sample.csv")
        let result = parseTransactionHistoryFromFile filePath

        let errorMessages =
            String.Join("; ", result.Errors |> List.map (fun e -> e.ErrorMessage))

        Assert.That(result.Errors, Is.Empty, sprintf "Parsing errors: %s" errorMessages)
        Assert.That(result.Transactions.Length, Is.EqualTo(6))
        Assert.That(result.ProcessedLines, Is.EqualTo(6))

        // Verify all transactions are option trades
        let optionTransactions =
            result.Transactions
            |> List.filter (fun t -> TransactionTypeDetection.isOptionTransaction t.InstrumentType)

        Assert.That(optionTransactions.Length, Is.EqualTo(6))

    [<Test>]
    member this.``TastytradeStatementParser should parse mixed trading sample file``() =
        let filePath = Path.Combine(testDataPath, "tastytrade_mixed_trading_sample.csv")
        let result = parseTransactionHistoryFromFile filePath

        let errorMessages =
            String.Join("; ", result.Errors |> List.map (fun e -> e.ErrorMessage))

        Assert.That(result.Errors, Is.Empty, sprintf "Parsing errors: %s" errorMessages)
        Assert.That(result.Transactions.Length, Is.EqualTo(11))

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

        Assert.That(equityTrades.Length, Is.EqualTo(3), "Should have 3 equity trades (AAPL buy, TSLA buy, TSLA sell)")
        Assert.That(equityOptionTrades.Length, Is.EqualTo(1), "Should have 1 equity option (AAPL call)")
        Assert.That(futureOptionTrades.Length, Is.EqualTo(4), "Should have 4 future option trades (/ESZ5 spreads)")
        Assert.That(moneyMovements.Length, Is.EqualTo(3), "Should have 3 money movements (deposit, fee, interest)")

    [<Test>]
    member this.``TastytradeStatementParser should parse ACAT transfer sample file``() =
        let filePath = Path.Combine(testDataPath, "tastytrade_acat_transfer_sample.csv")
        let result = parseTransactionHistoryFromFile filePath

        let errorMessages =
            String.Join("; ", result.Errors |> List.map (fun e -> e.ErrorMessage))

        Assert.That(result.Errors, Is.Empty, sprintf "Parsing errors: %s" errorMessages)

        Assert.That(
            result.Transactions.Length,
            Is.EqualTo(3),
            "Should parse all 3 transactions: ACH deposit, ACAT equity transfer, and money transfer from Interactive Brokers"
        )

        // Verify the ACH deposit with quoted comma-separated value parses correctly
        let depositWith1700 =
            result.Transactions |> List.tryFind (fun t -> t.Value = 1700.00m)

        Assert.That(depositWith1700.IsSome, Is.True, "Should find the $1,700.00 ACH deposit")
        Assert.That(depositWith1700.Value.Description, Is.EqualTo("ACH DEPOSIT"))

        Assert.That(
            (match depositWith1700.Value.TransactionType with
             | MoneyMovement(Deposit) -> true
             | _ -> false),
            Is.True,
            "ACH DEPOSIT should be a Money Movement Deposit"
        )

        // Verify ACAT equity transfer (Receive Deliver)
        let acatEquityTransfer =
            result.Transactions
            |> List.tryFind (fun t ->
                match t.TransactionType with
                | ReceiveDeliver("ACAT") -> true
                | _ -> false)

        Assert.That(acatEquityTransfer.IsSome, Is.True, "Should find the ACAT equity transfer")
        Assert.That(acatEquityTransfer.Value.Symbol, Is.EqualTo(Some "MPW"))
        Assert.That(acatEquityTransfer.Value.InstrumentType, Is.EqualTo(Some "Equity"))
        Assert.That(acatEquityTransfer.Value.Quantity, Is.EqualTo(1000m), "Should receive 1000 shares of MPW")
        Assert.That(acatEquityTransfer.Value.Value, Is.EqualTo(0m), "ACAT equity transfers have 0 cash value")

        // Verify money transfer from Interactive Brokers
        let moneyTransfer =
            result.Transactions
            |> List.tryFind (fun t ->
                match t.TransactionType with
                | MoneyMovement(Transfer) -> true
                | _ -> false)

        Assert.That(moneyTransfer.IsSome, Is.True, "Should find the money transfer")
        Assert.That(moneyTransfer.Value.Value, Is.EqualTo(313.46m), "Should transfer $313.46")

        Assert.That(
            moneyTransfer.Value.Description,
            Is.EqualTo("TRANSFER FROM INTERACTIVE B"),
            "Should have correct description"
        )

    [<Test>]
    member this.``TastytradeStatementParser should handle dates with timezones correctly``() =
        let csvContent =
            """Date,Type,Sub Type,Action,Symbol,Instrument Type,Description,Value,Quantity,Average Price,Commissions,Fees,Multiplier,Root Symbol,Underlying Symbol,Expiration Date,Strike Price,Call or Put,Order #,Total,Currency
2024-05-31T14:42:13+0100,Money Movement,Deposit,,,,Test deposit,1000.00,0,,0.00,0.00,,,,,,,,1000.00,USD"""

        let result = parseTransactionHistory csvContent

        Assert.That(result.Errors, Is.Empty)
        Assert.That(result.Transactions.Length, Is.EqualTo(1))

        let transaction = result.Transactions.[0]
        Assert.That(transaction.Date.Year, Is.EqualTo(2024))
        Assert.That(transaction.Date.Month, Is.EqualTo(5))
        Assert.That(transaction.Date.Day, Is.EqualTo(31))

    [<Test>]
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

        Assert.That(strategies.Length, Is.EqualTo(1))
        Assert.That(strategies.[0].Transactions.Length, Is.EqualTo(2))
        Assert.That(strategies.[0].OrderNumber, Is.EqualTo("123456789"))

    [<Test>]
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

        Assert.That(strategies.Length, Is.EqualTo(1))
        Assert.That(strategies.[0].StrategyType, Is.EqualTo(Some CalendarSpread))

    [<Test>]
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

        Assert.That(strategies.Length, Is.EqualTo(1))
        Assert.That(strategies.[0].StrategyType, Is.EqualTo(Some SingleLeg))

    [<Test>]
    member this.``TransactionTypeDetection should correctly identify option transactions``() =
        Assert.That(TransactionTypeDetection.isOptionTransaction (Some "Equity Option"), Is.True)
        Assert.That(TransactionTypeDetection.isOptionTransaction (Some "Equity"), Is.False)
        Assert.That(TransactionTypeDetection.isOptionTransaction None, Is.False)

    [<Test>]
    member this.``TransactionTypeDetection should correctly identify equity trades``() =
        Assert.That(TransactionTypeDetection.isEquityTrade (Some "Equity"), Is.True)
        Assert.That(TransactionTypeDetection.isEquityTrade (Some "Equity Option"), Is.False)
        Assert.That(TransactionTypeDetection.isEquityTrade None, Is.False)

    [<Test>]
    member this.``TastytradeStatementParser should handle empty CSV gracefully``() =
        let result = parseTransactionHistory ""

        Assert.That(result.Transactions, Is.Empty)
        Assert.That(result.Errors, Is.Empty)
        Assert.That(result.ProcessedLines, Is.EqualTo(0))

    [<Test>]
    member this.``TastytradeStatementParser should handle header-only CSV``() =
        let csvContent =
            "Date,Type,Sub Type,Action,Symbol,Instrument Type,Description,Value,Quantity,Average Price,Commissions,Fees,Multiplier,Root Symbol,Underlying Symbol,Expiration Date,Strike Price,Call or Put,Order #,Total,Currency"

        let result = parseTransactionHistory csvContent

        Assert.That(result.Transactions, Is.Empty)
        Assert.That(result.Errors, Is.Empty)
        Assert.That(result.ProcessedLines, Is.EqualTo(0))

    [<Test>]
    member this.``TastytradeStatementParser should handle malformed CSV lines``() =
        let csvContent =
            """Date,Type,Sub Type,Action,Symbol,Instrument Type,Description,Value,Quantity,Average Price,Commissions,Fees,Multiplier,Root Symbol,Underlying Symbol,Expiration Date,Strike Price,Call or Put,Order #,Total,Currency
2024-05-31T14:42:13+0100,Trade,Buy to Close,BUY_TO_CLOSE""" // Incomplete line

        let result = parseTransactionHistory csvContent

        Assert.That(result.Transactions, Is.Empty)
        Assert.That(result.Errors.Length, Is.EqualTo(1))
        Assert.That(result.Errors.[0].ErrorType, Is.EqualTo(MissingRequiredField("All fields")))

    [<Test>]
    member this.``TastytradeStatementParser should handle invalid dates``() =
        let csvContent =
            """Date,Type,Sub Type,Action,Symbol,Instrument Type,Description,Value,Quantity,Average Price,Commissions,Fees,Multiplier,Root Symbol,Underlying Symbol,Expiration Date,Strike Price,Call or Put,Order #,Total,Currency
INVALID_DATE,Trade,Buy to Close,BUY_TO_CLOSE,AAPL,Equity,Test,-100,1,-100,0,0,,,,,,,123,-100,USD"""

        let result = parseTransactionHistory csvContent

        Assert.That(result.Transactions, Is.Empty)
        Assert.That(result.Errors.Length, Is.EqualTo(1))
        Assert.That(result.Errors.[0].ErrorType, Is.EqualTo(InvalidDateFormat))

    [<Test>]
    member this.``TastytradeStatementParser should parse decimal values correctly``() =
        let csvContent =
            """Date,Type,Sub Type,Action,Symbol,Instrument Type,Description,Value,Quantity,Average Price,Commissions,Fees,Multiplier,Root Symbol,Underlying Symbol,Expiration Date,Strike Price,Call or Put,Order #,Total,Currency
2024-05-31T14:42:13+0100,Money Movement,Deposit,,,,Test deposit,"1,234.56",0,,0.00,0.00,,,,,,,,"1,234.56",USD"""

        let result = parseTransactionHistory csvContent

        Assert.That(result.Errors, Is.Empty)
        Assert.That(result.Transactions.Length, Is.EqualTo(1))
        Assert.That(result.Transactions.[0].Value, Is.EqualTo(1234.56m))

    [<Test>]
    member this.``TastytradeStatementParser should parse legacy format lending rebate file``() =
        let filePath = Path.Combine(testDataPath, "LendingRebate.csv")
        let result = parseTransactionHistoryFromFile filePath

        let errorMessages =
            String.Join("; ", result.Errors |> List.map (fun e -> e.ErrorMessage))

        Assert.That(result.Errors, Is.Empty, sprintf "Parsing errors: %s" errorMessages)

        Assert.That(
            result.Transactions.Length,
            Is.EqualTo(2),
            "Should parse 2 transactions (debit interest + lending rebate)"
        )

        Assert.That(result.ProcessedLines, Is.EqualTo(2))

        // Transactions are in CSV file order (not sorted by date)
        // First transaction in CSV: June 17 - Debit Interest
        let debitInterestTransaction = result.Transactions.[0]
        Assert.That(debitInterestTransaction.Description, Is.EqualTo("FROM 05/16 THRU 06/15 @11    %"))
        Assert.That(debitInterestTransaction.Value, Is.EqualTo(-0.01m))
        Assert.That(debitInterestTransaction.Date, Is.EqualTo(DateTime(2024, 6, 17, 22, 0, 0)))

        // Verify it's a Money Movement with DebitInterest subtype
        match debitInterestTransaction.TransactionType with
        | MoneyMovement(subType) ->
            Assert.That((subType = DebitInterest), Is.True, "Should be identified as DebitInterest subtype")
        | _ -> Assert.Fail("Transaction should be a Money Movement")

        // Second transaction in CSV: June 14 - Lending Rebate
        let lendingTransaction = result.Transactions.[1]
        Assert.That(lendingTransaction.Description, Is.EqualTo("FULLYPAID LENDING REBATE"))
        Assert.That(lendingTransaction.Value, Is.EqualTo(0.30m))
        Assert.That(lendingTransaction.Date, Is.EqualTo(DateTime(2024, 6, 14, 22, 0, 0)))

        // Verify it's a Money Movement with Lending subtype
        match lendingTransaction.TransactionType with
        | MoneyMovement(subType) -> Assert.That((subType = Lending), Is.True, "Should be identified as Lending subtype")
        | _ -> Assert.Fail("Transaction should be a Money Movement")

    [<Test>]
    member this.``TastytradeStatementParser should parse and verify Future Option transactions correctly``() =
        let filePath = Path.Combine(testDataPath, "tastytrade_mixed_trading_sample.csv")
        let result = parseTransactionHistoryFromFile filePath

        let errorMessages =
            String.Join("; ", result.Errors |> List.map (fun e -> e.ErrorMessage))

        Assert.That(result.Errors, Is.Empty, sprintf "Parsing errors: %s" errorMessages)
        Assert.That(result.Transactions.Length, Is.EqualTo(11), "Should have 11 total transactions")

        // Extract all Future Option transactions
        let futureOptions =
            result.Transactions
            |> List.filter (fun t -> TransactionTypeDetection.isFutureOptionTransaction t.InstrumentType)

        Assert.That(futureOptions.Length, Is.EqualTo(4), "Should have 4 Future Option transactions")

        // Verify Transaction 1: Buy to Close /ESZ5 E2BX5 251111C6880 @ 1.1
        let buyToClose6880 =
            futureOptions
            |> List.tryFind (fun t ->
                match t.TransactionType with
                | Trade(BuyToClose, BUY_TO_CLOSE) -> t.Symbol = Some "/ESZ5" && t.Value = -55.00m
                | _ -> false)

        Assert.That(buyToClose6880.IsSome, Is.True, "Should find Buy to Close 6880 call")
        let tx1 = buyToClose6880.Value
        Assert.That(tx1.Date, Is.EqualTo(DateTime(2025, 11, 11, 17, 50, 40)))
        Assert.That(tx1.Symbol, Is.EqualTo(Some "/ESZ5"), "Symbol should use UnderlyingSymbol (/ESZ5)")
        Assert.That(tx1.InstrumentType, Is.EqualTo(Some "Future Option"))
        Assert.That(tx1.Description, Is.EqualTo("Bought 1 /ESZ5 E2BX5 11/11/25 Call 6880.00 @ 1.1"))
        Assert.That(tx1.Value, Is.EqualTo(-55.00m), "Value should be -55.00 (cost to buy to close)")
        Assert.That(tx1.Quantity, Is.EqualTo(1m), "Quantity should be 1 contract")
        Assert.That(tx1.AveragePrice, Is.EqualTo(Some -55.00m))
        Assert.That(tx1.Commissions, Is.EqualTo(-1.25m), "Commissions should be -1.25")
        Assert.That(tx1.Fees, Is.EqualTo(-0.87m), "Fees should be -0.87")
        Assert.That(tx1.Multiplier, Is.EqualTo(Some 1m), "Future Option multiplier should be 1 (not 100)")
        Assert.That(tx1.RootSymbol, Is.EqualTo(Some "./ESZ5 E2BX5"))
        Assert.That(tx1.UnderlyingSymbol, Is.EqualTo(Some "/ESZ5"))
        Assert.That(tx1.ExpirationDate, Is.EqualTo(Some(DateTime(2025, 11, 11))))
        Assert.That(tx1.StrikePrice, Is.EqualTo(Some 6880m))
        Assert.That(tx1.CallOrPut, Is.EqualTo(Some "CALL"))
        Assert.That(tx1.OrderNumber, Is.EqualTo(Some "420000990"))

        // Verify Transaction 2: Sell to Close /ESZ5 E2BX5 251111C6865 @ 4.9
        let sellToClose6865 =
            futureOptions
            |> List.tryFind (fun t ->
                match t.TransactionType with
                | Trade(SellToClose, SELL_TO_CLOSE) -> t.Symbol = Some "/ESZ5" && t.Value = 245.00m
                | _ -> false)

        Assert.That(sellToClose6865.IsSome, Is.True, "Should find Sell to Close 6865 call")
        let tx2 = sellToClose6865.Value
        Assert.That(tx2.Date, Is.EqualTo(DateTime(2025, 11, 11, 17, 50, 40)))
        Assert.That(tx2.Symbol, Is.EqualTo(Some "/ESZ5"), "Symbol should use UnderlyingSymbol (/ESZ5)")
        Assert.That(tx2.InstrumentType, Is.EqualTo(Some "Future Option"))
        Assert.That(tx2.Description, Is.EqualTo("Sold 1 /ESZ5 E2BX5 11/11/25 Call 6865.00 @ 4.9"))
        Assert.That(tx2.Value, Is.EqualTo(245.00m), "Value should be 245.00 (proceeds from sell to close)")
        Assert.That(tx2.Quantity, Is.EqualTo(1m), "Quantity should be 1 contract")
        Assert.That(tx2.AveragePrice, Is.EqualTo(Some 245.00m))
        Assert.That(tx2.Multiplier, Is.EqualTo(Some 1m), "Future Option multiplier should be 1")
        Assert.That(tx2.StrikePrice, Is.EqualTo(Some 6865m))
        Assert.That(tx2.CallOrPut, Is.EqualTo(Some "CALL"))
        Assert.That(tx2.OrderNumber, Is.EqualTo(Some "420000990"), "Same order number as first transaction")

        // Verify Transaction 3: Sell to Open /ESZ5 E2BX5 251111C6880 @ 1.75
        let sellToOpen6880 =
            futureOptions
            |> List.tryFind (fun t ->
                match t.TransactionType with
                | Trade(SellToOpen, SELL_TO_OPEN) -> t.Symbol = Some "/ESZ5" && t.Value = 87.50m
                | _ -> false)

        Assert.That(sellToOpen6880.IsSome, Is.True, "Should find Sell to Open 6880 call")
        let tx3 = sellToOpen6880.Value
        Assert.That(tx3.Date, Is.EqualTo(DateTime(2025, 11, 11, 14, 56, 57)))
        Assert.That(tx3.Symbol, Is.EqualTo(Some "/ESZ5"), "Symbol should use UnderlyingSymbol (/ESZ5)")
        Assert.That(tx3.InstrumentType, Is.EqualTo(Some "Future Option"))
        Assert.That(tx3.Description, Is.EqualTo("Sold 1 /ESZ5 E2BX5 11/11/25 Call 6880.00 @ 1.75"))
        Assert.That(tx3.Value, Is.EqualTo(87.50m), "Value should be 87.50")
        Assert.That(tx3.Quantity, Is.EqualTo(1m))
        Assert.That(tx3.Multiplier, Is.EqualTo(Some 1m))
        Assert.That(tx3.StrikePrice, Is.EqualTo(Some 6880m))
        Assert.That(tx3.CallOrPut, Is.EqualTo(Some "CALL"))
        Assert.That(tx3.OrderNumber, Is.EqualTo(Some "419888474"))

        // Verify Transaction 4: Buy to Open /ESZ5 E2BX5 251111C6865 @ 4.5
        let buyToOpen6865 =
            futureOptions
            |> List.tryFind (fun t ->
                match t.TransactionType with
                | Trade(BuyToOpen, BUY_TO_OPEN) -> t.Symbol = Some "/ESZ5" && t.Value = -225.00m
                | _ -> false)

        Assert.That(buyToOpen6865.IsSome, Is.True, "Should find Buy to Open 6865 call")
        let tx4 = buyToOpen6865.Value
        Assert.That(tx4.Date, Is.EqualTo(DateTime(2025, 11, 11, 14, 56, 57)))
        Assert.That(tx4.Symbol, Is.EqualTo(Some "/ESZ5"), "Symbol should use UnderlyingSymbol (/ESZ5)")
        Assert.That(tx4.InstrumentType, Is.EqualTo(Some "Future Option"))
        Assert.That(tx4.Description, Is.EqualTo("Bought 1 /ESZ5 E2BX5 11/11/25 Call 6865.00 @ 4.5"))
        Assert.That(tx4.Value, Is.EqualTo(-225.00m), "Value should be -225.00 (cost)")
        Assert.That(tx4.Quantity, Is.EqualTo(1m))
        Assert.That(tx4.AveragePrice, Is.EqualTo(Some -225.00m))
        Assert.That(tx4.Multiplier, Is.EqualTo(Some 1m))
        Assert.That(tx4.StrikePrice, Is.EqualTo(Some 6865m))
        Assert.That(tx4.CallOrPut, Is.EqualTo(Some "CALL"))
        Assert.That(tx4.OrderNumber, Is.EqualTo(Some "419888474"), "Same order number as third transaction")

        // Verify all Future Options have correct common attributes
        for futureOption in futureOptions do
            Assert.That(futureOption.Currency, Is.EqualTo("USD"))
            Assert.That(futureOption.Commissions, Is.EqualTo(-1.25m), "All should have -1.25 commissions")
            Assert.That(futureOption.Fees, Is.EqualTo(-0.87m), "All should have -0.87 fees")
            Assert.That(futureOption.RootSymbol, Is.Not.Null, "Root symbol should be set")
            Assert.That(futureOption.UnderlyingSymbol, Is.EqualTo(Some "/ESZ5"), "All ES futures")
            Assert.That(futureOption.ExpirationDate, Is.EqualTo(Some(DateTime(2025, 11, 11))), "All expire 11/11/25")
            Assert.That(futureOption.CallOrPut, Is.EqualTo(Some "CALL"), "All are call options")
