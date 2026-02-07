namespace Tests

open System
open Microsoft.VisualStudio.TestTools.UnitTesting
open Binnaculum.Core.Import
open SpecialDividendAdjustmentDetector
open AdjustmentValidation
open TastytradeModels

[<TestClass>]
type SpecialDividendAdjustmentTests() =

    // ============================================
    // Test Fixtures and Helpers
    // ============================================

    /// Create a sample special dividend transaction
    let createSpecialDividendTransaction
        (date: DateTime)
        (rootSymbol: string)
        (expirationDate: DateTime)
        (strikePrice: decimal)
        (callOrPut: string)
        (action: TradeAction)
        (value: decimal)
        (quantity: decimal)
        (lineNumber: int)
        : TastytradeTransaction =
        let symbolStr =
            sprintf "%s%s%s%010.2f" rootSymbol (expirationDate.ToString("yyMMdd")) callOrPut strikePrice

        { Date = date
          TransactionType = ReceiveDeliver("Special Dividend")
          Symbol = Some symbolStr
          InstrumentType = Some "Equity Option"
          Description = $"Special Dividend adjustment for {rootSymbol}"
          Value = value
          Quantity = quantity
          AveragePrice = None
          Commissions = 0m
          Fees = 0m
          Multiplier = Some 100m
          RootSymbol = Some rootSymbol
          UnderlyingSymbol = Some rootSymbol
          ExpirationDate = Some expirationDate
          StrikePrice = Some strikePrice
          CallOrPut = Some callOrPut
          OrderNumber = None
          Currency = "USD"
          RawCsvLine = ""
          LineNumber = lineNumber }

    /// Create a regular trade transaction (for comparison)
    let createTradeTransaction
        (date: DateTime)
        (rootSymbol: string)
        (expirationDate: DateTime)
        (strikePrice: decimal)
        (callOrPut: string)
        (tradeCode: TradeSubType)
        (value: decimal)
        (quantity: decimal)
        (lineNumber: int)
        : TastytradeTransaction =
        let action =
            match tradeCode with
            | BuyToOpen -> BUY_TO_OPEN
            | SellToOpen -> SELL_TO_OPEN
            | BuyToClose -> BUY_TO_CLOSE
            | SellToClose -> SELL_TO_CLOSE

        let symbolStr =
            sprintf "%s%s%s%010.2f" rootSymbol (expirationDate.ToString("yyMMdd")) callOrPut strikePrice

        { Date = date
          TransactionType = Trade(tradeCode, action)
          Symbol = Some symbolStr
          InstrumentType = Some "Equity Option"
          Description = $"Regular option trade"
          Value = value
          Quantity = quantity
          AveragePrice = Some(Math.Abs(value) / quantity)
          Commissions = 0m
          Fees = 0m
          Multiplier = Some 100m
          RootSymbol = Some rootSymbol
          UnderlyingSymbol = Some rootSymbol
          ExpirationDate = Some expirationDate
          StrikePrice = Some strikePrice
          CallOrPut = Some callOrPut
          OrderNumber = None
          Currency = "USD"
          RawCsvLine = ""
          LineNumber = lineNumber }

    // ============================================
    // Phase 1: Detection Tests
    // ============================================

    [<TestMethod>]
    member this.``Detect paired adjustment transactions with matching timestamps``() =
        // Arrange: Create a valid adjustment pair (SELL_TO_OPEN + BUY_TO_CLOSE)
        let date = DateTime(2024, 12, 12, 12, 1, 30)
        let expirationDate = DateTime(2024, 12, 20)
        let rootSymbol = "TSLL"

        // Close old position: SELL_TO_OPEN at 36.00 strike -> +$96.00 (credit)
        let closingOldTxn =
            createSpecialDividendTransaction date rootSymbol expirationDate 36.00m "CALL" SELL_TO_OPEN 96.00m 1m 1

        // Open new position: BUY_TO_CLOSE at 35.70 strike -> -$96.00 (debit)
        let openingNewTxn =
            createSpecialDividendTransaction date rootSymbol expirationDate 35.70m "CALL" BUY_TO_CLOSE -96.00m 1m 2

        let transactions = [ closingOldTxn; openingNewTxn ]

        // Act
        let adjustments = detectAdjustments transactions

        // Assert
        Assert.AreEqual(1, adjustments.Length)

        let adjustment = adjustments.[0]
        Assert.AreEqual(36.00m, adjustment.OriginalStrike)
        Assert.AreEqual(35.70m, adjustment.NewStrike)
        Assert.AreEqual(-0.30m, adjustment.StrikeDelta)
        Assert.AreEqual(96.00m, adjustment.DividendAmount)
        Assert.AreEqual("CALL", adjustment.OptionType)

    [<TestMethod>]
    member this.``Ignore unpaired transactions``() =
        // Arrange: Create a single special dividend transaction without a pair
        let date = DateTime(2024, 12, 12, 12, 1, 30)
        let expirationDate = DateTime(2024, 12, 20)
        let rootSymbol = "TSLL"

        let transaction =
            createSpecialDividendTransaction date rootSymbol expirationDate 36.00m "CALL" BUY_TO_CLOSE 96.00m 1m 1

        let transactions = [ transaction ]

        // Act
        let adjustments = detectAdjustments transactions

        // Assert
        Assert.AreEqual(0, adjustments.Length)

    [<TestMethod>]
    member this.``Handle multiple adjustments on same date``() =
        // Arrange: Create two adjustment pairs on same date
        let date1 = DateTime(2024, 12, 12, 12, 1, 30)
        let date2 = DateTime(2024, 12, 12, 12, 2, 0)
        let expirationDate = DateTime(2024, 12, 20)
        let rootSymbol = "TSLL"

        // First pair
        let closing1 =
            createSpecialDividendTransaction date1 rootSymbol expirationDate 36.00m "CALL" BUY_TO_CLOSE 96.00m 1m 1

        let opening1 =
            createSpecialDividendTransaction date1 rootSymbol expirationDate 35.70m "CALL" SELL_TO_OPEN -96.00m 1m 2

        // Second pair (different expiration)
        let expirationDate2 = DateTime(2026, 1, 16)

        let closing2 =
            createSpecialDividendTransaction date2 rootSymbol expirationDate2 10.73m "CALL" BUY_TO_CLOSE 2280.00m 1m 3

        let opening2 =
            createSpecialDividendTransaction date2 rootSymbol expirationDate2 10.43m "CALL" SELL_TO_OPEN -2280.00m 1m 4

        let transactions = [ closing1; opening1; closing2; opening2 ]

        // Act
        let adjustments = detectAdjustments transactions

        // Assert
        Assert.AreEqual(2, adjustments.Length)

    [<TestMethod>]
    member this.``Match by opposite actions correctly``() =
        // Arrange
        let date = DateTime(2024, 12, 12, 12, 1, 30)
        let expirationDate = DateTime(2024, 12, 20)
        let rootSymbol = "TSLL"

        // Test SELL_TO_CLOSE + BUY_TO_OPEN pair
        let closing =
            createSpecialDividendTransaction date rootSymbol expirationDate 36.00m "CALL" SELL_TO_CLOSE 96.00m 1m 1

        let opening =
            createSpecialDividendTransaction date rootSymbol expirationDate 35.70m "CALL" BUY_TO_OPEN -96.00m 1m 2

        let transactions = [ closing; opening ]

        // Act
        let adjustments = detectAdjustments transactions

        // Assert
        Assert.AreEqual(1, adjustments.Length)

    [<TestMethod>]
    member this.``Detect zero premium balance correctly``() =
        // Arrange: Exact $0.00 net premium
        let date = DateTime(2024, 12, 12, 12, 1, 30)
        let expirationDate = DateTime(2024, 12, 20)
        let rootSymbol = "TSLL"

        let closing =
            createSpecialDividendTransaction date rootSymbol expirationDate 36.00m "CALL" BUY_TO_CLOSE 100.00m 1m 1

        let opening =
            createSpecialDividendTransaction date rootSymbol expirationDate 35.70m "CALL" SELL_TO_OPEN -100.00m 1m 2

        let transactions = [ closing; opening ]

        // Act
        let adjustments = detectAdjustments transactions

        // Assert
        Assert.AreEqual(1, adjustments.Length)
        Assert.AreEqual(100.00m, adjustments.[0].DividendAmount)

    [<TestMethod>]
    member this.``Reject mismatched option types``() =
        // Arrange: One CALL and one PUT
        let date = DateTime(2024, 12, 12, 12, 1, 30)
        let expirationDate = DateTime(2024, 12, 20)
        let rootSymbol = "TSLL"

        let closing =
            createSpecialDividendTransaction date rootSymbol expirationDate 36.00m "CALL" BUY_TO_CLOSE 96.00m 1m 1

        let opening =
            createSpecialDividendTransaction date rootSymbol expirationDate 35.70m "PUT" SELL_TO_OPEN -96.00m 1m 2

        let transactions = [ closing; opening ]

        // Act
        let adjustments = detectAdjustments transactions

        // Assert
        Assert.AreEqual(0, adjustments.Length)

    [<TestMethod>]
    member this.``Reject different expiration dates``() =
        // Arrange
        let date = DateTime(2024, 12, 12, 12, 1, 30)
        let expirationDate1 = DateTime(2024, 12, 20)
        let expirationDate2 = DateTime(2024, 12, 27)
        let rootSymbol = "TSLL"

        let closing =
            createSpecialDividendTransaction date rootSymbol expirationDate1 36.00m "CALL" BUY_TO_CLOSE 96.00m 1m 1

        let opening =
            createSpecialDividendTransaction date rootSymbol expirationDate2 35.70m "CALL" SELL_TO_OPEN -96.00m 1m 2

        let transactions = [ closing; opening ]

        // Act
        let adjustments = detectAdjustments transactions

        // Assert
        Assert.AreEqual(0, adjustments.Length)

    // ============================================
    // Phase 6: Validation Tests
    // ============================================

    [<TestMethod>]
    member this.``Validate adjustment with positive strikes``() =
        // Arrange
        let date = DateTime(2024, 12, 12, 12, 1, 30)
        let expirationDate = DateTime(2024, 12, 20)
        let rootSymbol = "TSLL"

        let closing =
            createSpecialDividendTransaction date rootSymbol expirationDate 36.00m "CALL" BUY_TO_CLOSE 96.00m 1m 1

        let opening =
            createSpecialDividendTransaction date rootSymbol expirationDate 35.70m "CALL" SELL_TO_OPEN -96.00m 1m 2

        let adjustments = detectAdjustments [ closing; opening ]

        // Act
        let result = validateAdjustment adjustments.[0]

        // Assert
        Assert.IsTrue(result.IsValid)
        Assert.AreEqual(0, result.Errors.Length)

    [<TestMethod>]
    member this.``Reject adjustment with zero strike``() =
        // Arrange
        let date = DateTime(2024, 12, 12, 12, 1, 30)
        let expirationDate = DateTime(2024, 12, 20)
        let rootSymbol = "TSLL"

        let closing =
            createSpecialDividendTransaction date rootSymbol expirationDate 0m "CALL" BUY_TO_CLOSE 96.00m 1m 1

        let opening =
            createSpecialDividendTransaction date rootSymbol expirationDate 0m "CALL" SELL_TO_OPEN -96.00m 1m 2

        let adjustments = detectAdjustments [ closing; opening ]

        // Act
        let result = validateAndFilterAdjustments adjustments

        // Assert
        Assert.AreEqual(0, result.Length)

    [<TestMethod>]
    member this.``Validate strike delta calculation``() =
        // Arrange
        let date = DateTime(2024, 12, 12, 12, 1, 30)
        let expirationDate = DateTime(2024, 12, 20)
        let rootSymbol = "TSLL"

        let closing =
            createSpecialDividendTransaction date rootSymbol expirationDate 36.00m "CALL" BUY_TO_CLOSE 96.00m 1m 1

        let opening =
            createSpecialDividendTransaction date rootSymbol expirationDate 35.70m "CALL" SELL_TO_OPEN -96.00m 1m 2

        let adjustments = detectAdjustments [ closing; opening ]
        let adjustment = adjustments.[0]

        // Act
        let expectedDelta = 35.70m - 36.00m
        let actualDelta = adjustment.NewStrike - adjustment.OriginalStrike

        // Assert
        Assert.AreEqual(actualDelta, expectedDelta)
        Assert.AreEqual(actualDelta, adjustment.StrikeDelta)

    // ============================================
    // Phase 8: Format Tests
    // ============================================

    [<TestMethod>]
    member this.``Format adjustment note correctly``() =
        // Arrange
        let originalStrike = 36.00m
        let newStrike = 35.70m
        let dividendAmount = 96.00m

        // Act
        let note = formatAdjustmentNote originalStrike newStrike dividendAmount

        // Assert
        StringAssert.Contains(note, "36.00")
        StringAssert.Contains(note, "35.70")
        StringAssert.Contains(note, "-0.30")
        StringAssert.Contains(note, "96.00")
        StringAssert.Contains(note, "special dividend")

    [<TestMethod>]
    member this.``Format adjustment note with large dividend``() =
        // Arrange
        let originalStrike = 10.73m
        let newStrike = 10.43m
        let dividendAmount = 2280.00m

        // Act
        let note = formatAdjustmentNote originalStrike newStrike dividendAmount

        // Assert
        StringAssert.Contains(note, "10.73")
        StringAssert.Contains(note, "10.43")
        StringAssert.Contains(note, "2280.00")

    // ============================================
    // Edge Cases and Integration
    // ============================================

    [<TestMethod>]
    member this.``Reject premium with rounding error > tolerance``() =
        // Arrange: Net premium is $0.02 instead of $0.00
        let date = DateTime(2024, 12, 12, 12, 1, 30)
        let expirationDate = DateTime(2024, 12, 20)
        let rootSymbol = "TSLL"

        let closing =
            createSpecialDividendTransaction date rootSymbol expirationDate 36.00m "CALL" BUY_TO_CLOSE 96.00m 1m 1

        let opening =
            createSpecialDividendTransaction date rootSymbol expirationDate 35.70m "CALL" SELL_TO_OPEN -95.98m 1m 2

        let transactions = [ closing; opening ]

        // Act
        let adjustments = detectAdjustments transactions

        // Assert
        Assert.AreEqual(0, adjustments.Length)

    [<TestMethod>]
    member this.``Accept premium with rounding error < tolerance``() =
        // Arrange: Net premium is $0.005 (within tolerance)
        let date = DateTime(2024, 12, 12, 12, 1, 30)
        let expirationDate = DateTime(2024, 12, 20)
        let rootSymbol = "TSLL"

        let closing =
            createSpecialDividendTransaction date rootSymbol expirationDate 36.00m "CALL" BUY_TO_CLOSE 96.00m 1m 1

        let opening =
            createSpecialDividendTransaction date rootSymbol expirationDate 35.70m "CALL" SELL_TO_OPEN -96.005m 1m 2

        let transactions = [ closing; opening ]

        // Act
        let adjustments = detectAdjustments transactions

        // Assert
        Assert.AreEqual(1, adjustments.Length)

    [<TestMethod>]
    member this.``Handle different quantity contracts``() =
        // Arrange: 2 contracts
        let date = DateTime(2024, 12, 12, 12, 1, 30)
        let expirationDate = DateTime(2024, 12, 20)
        let rootSymbol = "TSLL"

        let closing =
            createSpecialDividendTransaction date rootSymbol expirationDate 36.00m "CALL" BUY_TO_CLOSE 192.00m 2m 1

        let opening =
            createSpecialDividendTransaction date rootSymbol expirationDate 35.70m "CALL" SELL_TO_OPEN -192.00m 2m 2

        let transactions = [ closing; opening ]

        // Act
        let adjustments = detectAdjustments transactions

        // Assert
        Assert.AreEqual(1, adjustments.Length)
        Assert.AreEqual(2m, adjustments.[0].ClosingTransaction.Quantity)
        Assert.AreEqual(2m, adjustments.[0].OpeningTransaction.Quantity)

    [<TestMethod>]
    member this.``Real TSLL test case - Jan 2026 Call``() =
        // Arrange: Real data from TSLL import (lines 124-127 from plan)
        let date = DateTime(2024, 12, 12, 12, 1, 7)
        let expirationDate = DateTime(2026, 1, 16)
        let rootSymbol = "TSLL"

        // Close: SELL_TO_CLOSE TSLL 260116C00010730 -> +$2,280.00
        let closing =
            createSpecialDividendTransaction date rootSymbol expirationDate 10.73m "CALL" SELL_TO_CLOSE 2280.00m 1m 124

        // Open: BUY_TO_OPEN TSLL 260116C00010430 -> -$2,280.00
        let opening =
            createSpecialDividendTransaction date rootSymbol expirationDate 10.43m "CALL" BUY_TO_OPEN -2280.00m 1m 125

        let transactions = [ closing; opening ]

        // Act
        let adjustments = detectAdjustments transactions

        // Assert
        Assert.AreEqual(1, adjustments.Length)

        let adjustment = adjustments.[0]
        Assert.AreEqual(10.73m, adjustment.OriginalStrike)
        Assert.AreEqual(10.43m, adjustment.NewStrike)
        Assert.AreEqual(-0.30m, adjustment.StrikeDelta)
        Assert.AreEqual(2280.00m, adjustment.DividendAmount)
        Assert.AreEqual("TSLL", adjustment.TickerSymbol)

    [<TestMethod>]
    member this.``Real TSLL test case - Dec 2024 Call``() =
        // Arrange: Real data from TSLL import (lines 126-127 from plan)
        let date = DateTime(2024, 12, 12, 12, 1, 31)
        let expirationDate = DateTime(2024, 12, 20)
        let rootSymbol = "TSLL"

        // Open: SELL_TO_OPEN TSLL 241220C00035700 -> +$96.00
        let opening =
            createSpecialDividendTransaction date rootSymbol expirationDate 35.70m "CALL" SELL_TO_OPEN 96.00m 1m 126

        // Close: BUY_TO_CLOSE TSLL 241220C00036000 -> -$96.00
        let closing =
            createSpecialDividendTransaction date rootSymbol expirationDate 36.00m "CALL" BUY_TO_CLOSE -96.00m 1m 127

        let transactions = [ opening; closing ]

        // Act
        let adjustments = detectAdjustments transactions

        // Assert
        Assert.AreEqual(1, adjustments.Length)

        let adjustment = adjustments.[0]
        Assert.AreEqual(36.00m, adjustment.OriginalStrike)
        Assert.AreEqual(35.70m, adjustment.NewStrike)
        Assert.AreEqual(-0.30m, adjustment.StrikeDelta)
        Assert.AreEqual(96.00m, adjustment.DividendAmount)
