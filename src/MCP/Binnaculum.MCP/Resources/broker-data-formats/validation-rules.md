# Strike Adjustment Validation Rules

## Overview

This document defines the **universal validation rules** that apply to all broker strike adjustment data. These rules ensure data integrity and prevent false positives from corrupting your portfolio records.

---

## Core Validation Rules

### Rule 1: Strike Prices Must Be Positive

**Requirement:** Both original and new strike prices must be > 0

```csharp
if (adjustment.OriginalStrike <= 0 || adjustment.NewStrike <= 0)
    throw new ValidationException("Strike prices must be positive");
```

**Why:** Negative or zero strike prices don't exist in real markets  
**Impact:** Invalid: Reject entire adjustment

**Examples:**
```
✅ Original: $10.73, New: $10.43 (both positive)
✅ Original: $150.00, New: $152.50 (both positive)
❌ Original: -$10.73, New: $10.43 (original negative)
❌ Original: $0.00, New: $10.43 (original zero)
```

---

### Rule 2: Adjustment Must Change the Strike

**Requirement:** Original strike ≠ New strike

```csharp
if (Math.Abs(adjustment.OriginalStrike - adjustment.NewStrike) < 0.01m)
    throw new ValidationException("Strike price did not change");
```

**Why:** Strike adjustments must have a measurable effect  
**Impact:** Invalid: Reject entire adjustment  
**Tolerance:** Allow ±$0.01 for rounding

**Examples:**
```
✅ Original: $10.73, New: $10.43 (Δ = -$0.30)
✅ Original: $50.00, New: $25.00 (Δ = -$25.00)
❌ Original: $10.43, New: $10.43 (Δ = $0.00 - no change)
❌ Original: $10.43, New: $10.431 (Δ = $0.001 - negligible)
```

---

### Rule 3: Premium Must Balance (±$0.01)

**Requirement:** Close Premium + Open Premium ≈ $0

```csharp
var netPremium = Math.Abs(closingPremium + openingPremium);
if (netPremium > 0.01m)
    throw new ValidationException($"Premium imbalance: ${netPremium}");
```

**Why:** Strike adjustments are zero-sum events; premium balances internally  
**Impact:** Invalid: Reject entire pair  
**Tolerance:** ±$0.01 (rounding)

**Examples:**
```
✅ Close: +$2,280.00, Open: -$2,280.00, Net: $0.00
✅ Close: +$96.05, Open: -$96.04, Net: $0.01 (within tolerance)
✅ Close: +$150.00, Open: -$150.00, Net: $0.00
❌ Close: +$2,280.00, Open: -$2,279.50, Net: $0.50 (exceeds tolerance)
❌ Close: +$96.00, Open: -$95.00, Net: $1.00 (exceeds tolerance)
```

---

### Rule 4: Quantities Must Match

**Requirement:** Both transactions affect same number of contracts

```csharp
if (closingTxn.Quantity != openingTxn.Quantity)
    throw new ValidationException("Quantities don't match");
```

**Why:** An adjustment affects all contracts in a position equally  
**Impact:** Invalid: Reject this pair (might be separate adjustments)

**Examples:**
```
✅ Close: 100 contracts, Open: 100 contracts
✅ Close: 50 contracts, Open: 50 contracts
❌ Close: 100 contracts, Open: 50 contracts
❌ Close: 50 contracts, Open: 100 contracts
```

---

### Rule 5: Timestamps Must Be Within 1 Second

**Requirement:** |Time1 - Time2| ≤ 1 second

```csharp
var timeDelta = Math.Abs((txn1.OpenTime - txn2.OpenTime).TotalSeconds);
if (timeDelta > 1)
    throw new ValidationException($"Timestamps too far apart: {timeDelta}s");
```

**Why:** Paired adjustment transactions are processed together, microseconds apart  
**Impact:** Invalid: These are not paired transactions  
**Tolerance:** 1 second (accounting for broker processing delays)

**Examples:**
```
✅ 20:10:29.100 and 20:10:29.050 (Δ = 0.05s)
✅ 20:10:29.000 and 20:10:29.999 (Δ = 0.999s)
✅ 20:10:29.500 and 20:10:30.400 (Δ = 0.9s)
❌ 20:10:29.000 and 20:10:31.000 (Δ = 2s - too far)
❌ 20:10:29.000 and 20:10:35.000 (Δ = 6s - clearly separate)
```

---

### Rule 6: Position Identifiers Must Match

**Requirement:** Same ticker, expiration, option type

```csharp
if (txn1.Ticker != txn2.Ticker ||
    txn1.Expiration != txn2.Expiration ||
    txn1.OptionType != txn2.OptionType)
    throw new ValidationException("Position identifiers don't match");
```

**Why:** An adjustment applies to one specific position  
**Impact:** Invalid: These are separate adjustments, not a pair

**Examples:**
```
✅ TSLL 16JAN26 CALL paired with TSLL 16JAN26 CALL
✅ AAPL 20DEC24 PUT paired with AAPL 20DEC24 PUT
❌ TSLL 16JAN26 CALL paired with TSLL 17JAN26 CALL (different expiry)
❌ TSLL 16JAN26 CALL paired with TSLL 16JAN26 PUT (different type)
❌ TSLL 16JAN26 CALL paired with AAPL 16JAN26 CALL (different ticker)
```

---

### Rule 7: Actions Must Be Opposite

**Requirement:** One CLOSE, one OPEN (opposite direction)

```csharp
var isOpen1 = txn1.Action.Contains("OPEN");
var isOpen2 = txn2.Action.Contains("OPEN");
if (isOpen1 == isOpen2)  // Both open or both close
    throw new ValidationException("Actions must be opposite");
```

**Why:** An adjustment closes the old contract and opens the new one  
**Impact:** Invalid: Not a legitimate pair

**Examples:**
```
✅ SELL_TO_CLOSE + BUY_TO_OPEN (opposite)
✅ BUY_TO_CLOSE + SELL_TO_OPEN (opposite)
❌ BUY_TO_OPEN + BUY_TO_OPEN (both open)
❌ SELL_TO_CLOSE + SELL_TO_CLOSE (both close)
```

---

## Advanced Validation Rules

### Rule 8: Adjustment Amount Should Correlate with Corporate Action

**Requirement:** Strike delta roughly matches corporate action magnitude

```csharp
// For special dividends, strike delta should match dividend amount
if (adjustment.AdjustmentType == "SpecialDividend")
{
    var expectedDelta = -adjustment.DividendAmount; // Typically negative
    var actualDelta = adjustment.NewStrike - adjustment.OriginalStrike;
    
    if (Math.Abs(expectedDelta - actualDelta) > tolerance)
        logger.LogWarning($"Unexpected delta. Expected ~${expectedDelta}, got ${actualDelta}");
}
```

**Why:** Sanity check that adjustment magnitude makes sense  
**Impact:** Warning level; doesn't reject but alerts for investigation  
**Special Cases:** May not apply for stock splits or mergers

**Examples:**
```
✅ Special dividend of $0.30 → Strike drops $0.30
✅ 2:1 stock split → Strike halves
✅ 5% stock dividend → Strike drops ~5%
⚠️  Dividend $0.30 but strike drops $0.50 (unexpected but possible)
❌ Dividend $0.30 but strike drops $5.00 (highly suspicious)
```

---

### Rule 9: Strike Delta Should Be Consistent Across Related Positions

**Requirement:** All adjustments from same corporate action should have same delta

```csharp
// If multiple positions of same ticker adjusted on same day
var adjustmentsForTicker = adjustments
    .Where(a => a.Ticker == ticker && a.AdjustmentDate == date);

var deltas = adjustmentsForTicker.Select(a => a.StrikeDelta).Distinct().ToList();

if (deltas.Count > 1 && !AreAlmostEqual(deltas))
    logger.LogWarning($"Inconsistent strike deltas for {ticker}: {String.Join(", ", deltas)}");
```

**Why:** Single corporate action affects all options on a ticker uniformly  
**Impact:** Warning level; investigate but don't reject

**Examples:**
```
✅ Jan 2026 Call: -$0.30, Dec 2024 Call: -$0.30 (consistent)
✅ Jan 2026 Call: -$0.30, Jan 2026 Put: -$0.30 (consistent)
⚠️  Jan 2026 Call: -$0.30, Mar 2026 Call: -$0.29 (rounding difference OK)
❌ Jan 2026 Call: -$0.30, Jan 2026 Call: -$0.50 (inconsistent - why?)
```

---

### Rule 10: No Matching Trade Shouldn't Fail (Warning Only)

**Requirement:** After adjustment detected, able to link to OptionTrade

```csharp
var matchingTrade = OptionTrades
    .Where(t => t.Ticker == adjustment.Ticker &&
                t.ExpirationDate == adjustment.ExpirationDate &&
                t.OptionType == adjustment.OptionType &&
                t.Strike == adjustment.OriginalStrike &&
                t.IsOpen)
    .FirstOrDefault();

if (matchingTrade == null)
    logger.LogWarning($"No matching open trade found for adjustment: {adjustment}");
else
    ApplyAdjustment(matchingTrade, adjustment);
```

**Why:** Adjustment may be for already-closed position  
**Impact:** Warning level; doesn't reject but alerts that link wasn't made  
**When This Happens:** Trade was closed before adjustment was detected/imported

**Examples:**
```
✅ Adjustment found & matched → Apply
✅ Adjustment found, no match but trade closed → Skip (expected)
⚠️  Adjustment found, no match but trade still open → Investigate
❌ Adjustment found but data corruption → Reject entire import
```

---

## Data Quality Levels

### Tier 1: Critical Validations (MUST PASS)

These validations **must pass** or the adjustment is rejected entirely:

- ✅ Rule 1: Positive strike prices
- ✅ Rule 2: Strike changes
- ✅ Rule 3: Premium balances
- ✅ Rule 4: Quantities match
- ✅ Rule 5: Timestamp within 1s
- ✅ Rule 6: Position IDs match
- ✅ Rule 7: Actions opposite

**Result if ANY fail:** REJECT adjustment

---

### Tier 2: Consistency Warnings (SHOULD PASS)

These validations check **consistency and reasonableness**. Failures produce warnings:

- ⚠️ Rule 8: Delta correlates with amount
- ⚠️ Rule 9: Consistent across same action
- ⚠️ Rule 10: Link to existing trade

**Result if ANY fail:** WARN and log details, but still apply adjustment

---

### Tier 3: Optional Enrichment (NICE TO HAVE)

Additional checks that enhance data but aren't critical:

- ℹ️ Verify adjustment date matches public announcement
- ℹ️ Check corporate action calendar for confirmation
- ℹ️ Validate against broker's official adjustment list
- ℹ️ Cross-reference with OCC announcements

**Result if any fail:** Log info, provide context, no action needed

---

## Implementation Pattern

```csharp
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = new();
    public List<ValidationWarning> Warnings { get; set; } = new();
}

public async Task<ValidationResult> ValidateAdjustment(AdjustmentDetails adjustment)
{
    var result = new ValidationResult { IsValid = true };
    
    // Tier 1: Critical Validations
    try
    {
        ValidateCritical(adjustment);
    }
    catch (Exception ex)
    {
        result.IsValid = false;
        result.Errors.Add(new ValidationError(ex.Message));
        return result;  // Stop here
    }
    
    // Tier 2: Consistency Warnings
    var warnings = ValidateConsistency(adjustment);
    result.Warnings.AddRange(warnings);
    
    // Tier 3: Optional Enrichment
    var info = ValidateEnrichment(adjustment);
    logger.LogInformation($"Enrichment checks: {info}");
    
    return result;
}

private void ValidateCritical(AdjustmentDetails adj)
{
    if (adj.OriginalStrike <= 0 || adj.NewStrike <= 0)
        throw new ValidationException("Rule 1: Strike prices must be positive");
    
    if (Math.Abs(adj.OriginalStrike - adj.NewStrike) < 0.01m)
        throw new ValidationException("Rule 2: Strike must change");
    
    // ... other critical validations
}
```

---

## Handling Validation Failures

### If Tier 1 (Critical) Fails

```
Action: REJECT the adjustment
Log: Error-level message with details
Effect: Adjustment is NOT applied to portfolio
Status: Import may continue with other adjustments
```

### If Tier 2 (Warning) Fails

```
Action: APPLY the adjustment anyway
Log: Warning-level message with investigation notes
Effect: Adjustment IS applied, but flagged for review
Status: User should investigate discrepancy
```

### If Tier 3 (Info) Fails

```
Action: APPLY the adjustment
Log: Info-level message with context
Effect: Adjustment IS applied normally
Status: No user action needed (informational only)
```

---

## Error Messages & Recovery

### Scenario: Premium Imbalance Detected

```
❌ Validation Failed: Premium imbalance of $0.47

Details:
  Close transaction premium: +$2,280.00
  Open transaction premium:  -$2,279.53
  Net imbalance: $0.47 (exceeds $0.01 tolerance)

Possible causes:
  1. Broker fees/commissions applied
  2. Data transcription error in CSV
  3. FX conversion rounding
  4. System clock/timestamp issue

Recovery options:
  1. Manually verify in broker statement
  2. Adjust tolerance to $1.00 and retry
  3. Contact broker support
  4. Skip this adjustment (manual review needed)
```

### Scenario: No Matching Trade Found

```
⚠️  Warning: Adjustment detected but no matching open trade

Details:
  Adjustment: TSLL 16JAN26 $10.73 CALL → $10.43 CALL
  Expected: Open OptionTrade with Strike = $10.73
  Found: No matching trade

Possible causes:
  1. Trade was already closed before import
  2. Trade imported from different broker account
  3. Strike price manually adjusted previously
  4. Data entry error in strike price

Recovery options:
  1. Verify trade exists in portfolio
  2. Search for similar strikes ($10.70-$10.76)
  3. Check closed trade history
  4. Manual adjustment if strike is incorrect
```

---

## References

- **OCC Strike Adjustment Standards**: Official specifications
- **CME Data Quality Guidelines**: Best practices for validation
- **FINRA Compliance Requirements**: Regulatory validation standards

---

**Last Updated:** October 20, 2025  
**Version:** 1.0
