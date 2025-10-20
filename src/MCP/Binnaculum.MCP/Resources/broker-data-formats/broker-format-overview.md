# Broker Data Formats & Strike Adjustment Parsing

## Overview

This document series provides comprehensive guidance on how different brokers structure their exportable data, specifically focusing on **Option Strike Price Adjustments**. Strike adjustments occur when corporate actions (especially special dividends) affect option contract specifications.

---

## Why Strike Adjustments Matter

When a company takes corporate actions like special dividends, stock splits, or rights offerings, the option contracts must be adjusted to maintain fair valuation. Understanding how your broker reports these adjustments is critical for:

- **Accurate Portfolio Valuation**: Knowing the true strike price after adjustments
- **Tax Reporting**: Understanding cost basis adjustments
- **Performance Analysis**: Tracking which trades were affected by corporate actions
- **Audit Trail**: Documenting why your option strikes changed
- **Automated Processing**: Parsing broker data to update your records

---

## Supported Brokers

### 1. **Tastytrade** ✅
- **Coverage Level**: Comprehensive
- **Data Format**: CSV export with transaction details
- **Adjustment Type**: Special Dividend pairs
- **Documentation**: [Tastytrade Strike Adjustment Guide](./tastytrade-strike-adjustment-guide.md)

*More brokers coming soon (Interactive Brokers, TD Ameritrade, E*TRADE, etc.)*

---

## Common Patterns Across Brokers

All brokers use similar core concepts for reporting strike adjustments:

### Pattern 1: Paired Transactions
Strike adjustments typically appear as **two paired transactions**:
- Transaction A: Close position at OLD strike
- Transaction B: Open position at NEW strike
- Same timestamp or very close timing
- Premium balances to zero or near-zero

**Why this pattern?**
The broker effectively "closes" the old contract and "opens" a new contract with the adjusted strike. To the user, this looks like:
1. Sell-to-close the old contract (e.g., sell 10.73 call)
2. Buy-to-open the new contract (e.g., buy 10.43 call)
3. Net premium difference reflects the strike adjustment

### Pattern 2: Transaction Type/Description
Brokers use specific transaction types to identify adjustments:
- **Tastytrade**: "Receive Deliver" type with "Special Dividend" subtype
- **Interactive Brokers**: Usually coded as "Adjustment" or similar
- **TD Ameritrade**: May appear as "Corporate Action" or similar

### Pattern 3: Timestamp Matching
Paired adjustment transactions occur:
- On the SAME date as the corporate action announcement
- Within seconds of each other (not hours or days apart)
- After the adjustment is publicly announced but before market impact

---

## Data Structure Commonalities

Most brokers export adjustment data in CSV format with columns like:

| Field | Purpose | Example |
|-------|---------|---------|
| Date | Adjustment date | 2024-12-11 |
| Time | Adjustment time | 20:10:29 |
| Type | Transaction type | Receive Deliver |
| SubType | Specific event | Special Dividend |
| Action | BUY_TO_OPEN, SELL_TO_CLOSE, etc. | BUY_TO_OPEN |
| Ticker | Underlying symbol | TSLL |
| ExpirationDate | Option expiration | 2025-01-17 |
| OptionType | CALL or PUT | CALL |
| Strike | Strike price for this transaction | 10.43 |
| Quantity | Number of contracts | 100 |
| Premium | Total premium for transaction | 2280.00 |

---

## Parsing Algorithm (General Approach)

All broker data follows this basic algorithm:

```
1. Extract all transactions of type "Adjustment" (varies by broker)
2. Group by (Ticker, ExpirationDate, OptionType, Timestamp)
3. Find pairs where:
   - Same ticker
   - Same expiration
   - Same option type (both CALL or both PUT)
   - Same or very close timestamp (≤1 second tolerance)
   - Opposite actions (BUY paired with SELL)
   - Premium amounts balance (sum to zero ±$0.01)
4. Extract adjustment details:
   - Original Strike = Strike from SELL/CLOSE transaction
   - New Strike = Strike from BUY/OPEN transaction
   - Strike Delta = New - Original
   - Dividend Impact = Premium amount
5. Link to your OptionTrade records:
   - Find option trades with original strike price
   - Update strike to new value
   - Record adjustment reason in notes/history
```

---

## Validation Rules (Applies to All Brokers)

Before accepting an adjustment as valid:

✅ **All of these must be true:**
- Strike prices are positive numbers
- Adjustment changes the strike (delta ≠ 0)
- Dividend/corporate action is documented
- Affected trades are open (not already closed)
- Timestamp is recent (within 1-2 business days of announcement)

❌ **Reject if:**
- Strike prices are invalid (≤ 0)
- Premium doesn't balance (>$0.01 tolerance)
- Mismatched expiration dates in pair
- Mismatched option types (CALL ≠ PUT)
- Unrelated ticker pairs

---

## Implementation Steps

For each broker you want to support:

1. **Document the Data Format** (see Tastytrade example)
   - Sample CSV excerpt
   - Column explanations
   - Real-world example

2. **Define Detection Criteria**
   - Transaction type/subtype to look for
   - Pairing algorithm specifics
   - Edge cases unique to broker

3. **Implement Parser**
   - Create broker-specific parser module
   - Extract adjustment pairs
   - Validate according to rules above

4. **Create Unit Tests**
   - Test real broker CSV data
   - Verify detection accuracy
   - Test edge cases

5. **Document via MCP Tool**
   - Expose parser documentation
   - Provide code examples
   - Show expected outputs

---

## Quick Links

- [Tastytrade Strike Adjustment Guide](./tastytrade-strike-adjustment-guide.md) - Detailed walkthrough with real data
- [Common Patterns](./common-patterns.md) - Reusable concepts across brokers
- [Validation Rules](./validation-rules.md) - Data quality standards

---

## Questions & Examples

**Q: Why do strikes change?**
A: Corporate actions like special dividends, stock splits, or rights offerings adjust contract specifications. The OCC (Options Clearing Corporation) mandates these adjustments to maintain fair value.

**Q: How do I know if an adjustment is real?**
A: Real adjustments will appear in pairs with matching timestamps, balanced premiums, and the same corporate action reason. Verify against your broker's corporate action calendar.

**Q: Can I automate this?**
A: Yes! Use the parser for your broker to automatically detect and apply adjustments during import. See each broker guide for implementation examples.

**Q: What if my broker isn't listed?**
A: Review the "Common Patterns" document - most brokers follow similar structures. You can often adapt the Tastytrade parser as a template. Let us know about your broker for official support!

---

**Last Updated:** October 20, 2025
**Version:** 1.0
