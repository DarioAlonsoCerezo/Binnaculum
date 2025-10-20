# Common Patterns in Broker Strike Adjustment Data

## Overview

This document describes the **universal patterns** that appear across all brokers when reporting option strike adjustments. Understanding these patterns makes it easier to implement support for new brokers.

---

## Pattern 1: The Paired Transaction Model

### Core Concept
All brokers represent strike adjustments as **exactly two linked transactions**:

```
Transaction A: Close the OLD contract
└─ Action: SELL_TO_CLOSE (or equivalent)
└─ Strike: 10.73 (old/higher strike)
└─ Premium: +$2,280 (cash in)

Transaction B: Open the NEW contract  
└─ Action: BUY_TO_OPEN (or equivalent)
└─ Strike: 10.43 (new/lower strike)
└─ Premium: -$2,280 (cash out)

Result: Net cash impact = ~$0 (balanced)
```

### Why This Pattern?

The broker doesn't actually "adjust" the contract in place. Instead, they:
1. Liquidate the old contract at its strike
2. Create a new contract at the adjusted strike
3. Manage the premium difference internally

To the user, it appears as an instantaneous swap at the same price.

### Detection Key: Premium Balance

The **premium balances to zero** (within rounding tolerance):
```
Premium A + Premium B ≈ $0
$2,280 + (-$2,280) = $0 ✅
```

This is the most reliable indicator of a legitimate adjustment pair.

---

## Pattern 2: Transaction Type Codes

### Universal Transaction Types

| Broker | Type Code | Meaning |
|--------|-----------|---------|
| **Tastytrade** | Receive Deliver | Adjustment event |
| **Interactive Brokers** | Adjustment | Strike adjustment |
| **TD Ameritrade** | Corporate Action | Dividend or split |
| **E*TRADE** | Dividend | Special dividend |
| **Fidelity** | Dividend Reinvestment | Reinvested dividend |

### Detection Strategy

Look for transaction type that indicates **corporate action** or **adjustment**:
- NOT "Trade" (regular option purchases/sales)
- NOT "Dividend" (cash dividend only)
- NOT "Commission" (fees)
- YES "Receive Deliver", "Adjustment", "Corporate Action", etc.

### Subtype Codes (When Available)

Some brokers provide additional classification:

| Subtype | Meaning | Expected Strike Delta |
|---------|---------|----------------------|
| Special Dividend | Special dividend paid | Usually -0.30 to -1.00 |
| Stock Split | Stock split occurred | Proportional to split ratio |
| Stock Dividend | Stock dividend issued | Usually -0.50 to -5.00 |
| Rights Offering | Rights issued | Variable |
| Merger | Merger adjustment | Variable |
| Spin-off | Company split | Variable |

---

## Pattern 3: Timestamp Characteristics

### Temporal Markers

Strike adjustments appear with consistent **temporal characteristics**:

### Rule 1: Paired Transactions Have Identical or Near-Identical Timestamps
```
txn1 time: 2024-12-11 20:10:29.100
txn2 time: 2024-12-11 20:10:29.050

Δt = 0.05 seconds ✅ (virtually simultaneous)
```

**Tolerance:** Typically ≤ 1 second  
**Why:** The broker processes both sides of the adjustment together

### Rule 2: Adjustment Date Matches Corporate Action Date
```
Corporate action announced: 2024-12-11
Adjustment appears in CSV: 2024-12-11
Δ = Same day ✅
```

**Why:** Brokers apply adjustments on the effective date of the corporate action

### Rule 3: Adjustments Don't Appear at Random Times
```
Normal trading: Mixed throughout day (9:30 AM - 4:00 PM ET)
Adjustments: Typically after hours (20:00 - 02:00 ET)
```

**Why:** Brokers apply corporate adjustments after market close

---

## Pattern 4: Instrument Grouping

### Grouping By Position Identity

All paired transactions in an adjustment have **identical position identifiers**:

```
Both transactions must have:
├─ Same underlying ticker/symbol ✅
├─ Same expiration date ✅
├─ Same option type (CALL or PUT) ✅
├─ Same quantity ✅
└─ Same date/time (within tolerance) ✅

But:
└─ DIFFERENT strike prices ✅
```

### Valid Pair Example
```
✅ TSLL 16JAN26 10.73 CALL paired with TSLL 16JAN26 10.43 CALL
   (Same ticker, expiration, type, different strikes)

❌ TSLL 16JAN26 10.73 CALL paired with TSLL 16JAN26 10.73 PUT
   (Different option type - not a pair)

❌ TSLL 16JAN26 10.73 CALL paired with TSLL 17JAN26 10.73 CALL
   (Different expiration dates - not a pair)
```

---

## Pattern 5: Strike Price Behavior

### Strike Adjustment Direction

When corporate actions occur, strikes adjust predictably:

### Special Dividend
```
Effect: Company pays special dividend
Strike adjustment: DECREASE (usually)
Example: $10.73 → $10.43 (Δ -$0.30)
Reason: Each share gains $0.30 value from dividend
```

### Stock Split
```
Effect: Company splits stock (e.g., 2:1)
Strike adjustment: INCREASE proportionally
Example: $50.00 → $25.00 (Δ ÷2)
Reason: Each original share becomes 2 shares worth half
```

### Reverse Stock Split
```
Effect: Company reverse-splits stock (e.g., 1:2)
Strike adjustment: DECREASE proportionally
Example: $5.00 → $10.00 (Δ ×2)
Reason: Each original share becomes 0.5 shares worth double
```

### Stock Dividend
```
Effect: Company pays dividend in stock
Strike adjustment: DECREASE
Example: $50.00 → $48.70 (Δ -$1.30 for 2.6% dividend)
Reason: Existing shares diluted by stock issued
```

---

## Pattern 6: Premium Balancing

### The Zero-Sum Nature

Strike adjustments are **zero-sum events** from a broker's perspective:

```
Premium paid by broker: +$2,280
Premium paid by trader: -$2,280
Net broker impact: $0
```

### Validation Tolerance

Due to rounding, premiums rarely balance to exact $0.00:

```
Tolerance table:
├─ Ideal: Balance within $0.01 ✅
├─ Acceptable: Balance within $0.05 ✅
├─ Questionable: Balance within $0.50 ⚠️
└─ Reject: Balance > $0.50 ❌
```

### Why Imbalances Occur

1. **Rounding**: Premium = Shares × Price/100; rounding errors accumulate
2. **Fees**: Some brokers deduct small adjustment fees
3. **Conversion**: FX conversions for international options

---

## Pattern 7: Action Code Pairing

### Valid Pairings

The "Action" field determines which transaction is CLOSE and which is OPEN:

| Scenario | Action Pair | Meaning |
|----------|------------|---------|
| **Reducing position** | SELL_TO_CLOSE + BUY_TO_OPEN | Close old, open new (typical) |
| **Increasing position** | BUY_TO_CLOSE + SELL_TO_OPEN | Close old, open new (less common) |

### The Opposite Action Rule

Valid pairs MUST have opposite direction actions:

```
✅ BUY_TO_OPEN ↔ SELL_TO_CLOSE (most common)
✅ SELL_TO_OPEN ↔ BUY_TO_CLOSE (less common)
❌ BUY_TO_OPEN ↔ BUY_TO_OPEN (invalid - same direction)
❌ SELL_TO_CLOSE ↔ SELL_TO_CLOSE (invalid - same direction)
```

---

## Pattern 8: Quantity Consistency

### Quantity Must Be Identical

Both transactions in a pair must affect the **same number of contracts**:

```
✅ SELL_TO_CLOSE 100 contracts @ $10.73
✅ BUY_TO_OPEN 100 contracts @ $10.43
   (Quantities match)

❌ SELL_TO_CLOSE 100 contracts @ $10.73
❌ BUY_TO_OPEN 50 contracts @ $10.43
   (Quantities don't match - not a pair)
```

### Partial Adjustments

If only some contracts are adjusted:
```
Original position: 100 contracts @ $10.73
After adjustment: 50 @ $10.43 (adjusted), 50 @ $10.73 (unchanged)

Appears as TWO separate pairs:
Pair 1: SELL_TO_CLOSE 50 @ $10.73 + BUY_TO_OPEN 50 @ $10.43
Pair 2: (No pair for the 50 remaining @ $10.73)
```

Process each pair independently.

---

## Pattern 9: Data Completeness

### Required Fields for Detection

To detect a valid adjustment pair, you need:

```
Per Transaction:
├─ Date/Time (precise to second) ✅
├─ Instrument identifier (ticker/expiration/strike/type) ✅
├─ Action code (BUY/SELL, OPEN/CLOSE) ✅
├─ Strike price (numeric) ✅
├─ Premium (numeric, can be negative) ✅
├─ Quantity (numeric, must be positive) ✅
└─ Transaction type (Adjustment/Receive Deliver/Corporate Action) ✅
```

### What's NOT Required (But Helpful)

- Execution price per share
- Commission amounts
- Net cash amount
- P&L values
- Settlement dates

---

## Pattern 10: Frequency and Timing

### When Adjustments Occur

```
Most Common:
├─ After special dividends: Very common
├─ After stock splits: Very common
├─ After mergers: Common
├─ After rights offerings: Less common

Timing:
├─ After-hours: 20:00 - 02:00 ET (typical)
├─ After market open: Rare
├─ Before market open: Rare
```

### Detection Windows

When scanning CSV files:
```
Recent imports: Look in last 30 days
Historical scan: Look for pattern even in old data
```

---

## Anti-Patterns: What's NOT an Adjustment

### False Positive 1: Regular Covered Calls
```
SELL_TO_OPEN 100 CALLs @ $50
BUY_TO_CLOSE 100 CALLs @ $50

❌ Not an adjustment because:
  - Strike prices are identical
  - Premium wouldn't balance (lost to transaction)
  - No corporate action mentioned
```

### False Positive 2: Regular Rolls
```
SELL_TO_CLOSE 100 CALLs @ $50 May expiry
BUY_TO_OPEN 100 CALLs @ $50 June expiry

❌ Not an adjustment because:
  - Expiration dates are different
  - Different strike behavior
  - Manual trader decision, not corporate action
```

### False Positive 3: Multiple Strikes (Vertical Spread)
```
BUY_TO_OPEN 100 CALLs @ $50
SELL_TO_OPEN 100 CALLs @ $52

❌ Not an adjustment because:
  - This is a spread position
  - Strike difference is intentional
  - No time pairing
```

---

## Implementation Checklist

When implementing for a new broker:

- [ ] Identify their transaction type code for adjustments
- [ ] Determine instrument identifier format (how strikes are encoded)
- [ ] Verify timestamp precision available
- [ ] Identify action code conventions (BUY/SELL naming)
- [ ] Test with real corporate action data if available
- [ ] Validate premium balance tolerance
- [ ] Document any broker-specific quirks
- [ ] Create unit tests with real CSV samples

---

## References for Further Learning

- **OCC Corporate Actions Guide**: Strike adjustment specifications
- **CME Globex Documentation**: Adjustment implementation details
- **Trader Forums**: Real-world examples of adjustments seen in the wild

---

**Last Updated:** October 20, 2025  
**Version:** 1.0
