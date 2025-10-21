# Option Trade Import & Storage Rules

## Critical Principle: One Contract Per Record

**ALL option trades MUST be stored with Quantity = 1 in the database**, regardless of whether the trader is creating 1 contract or 11 contracts in a single action.

### Why?
1. **Individual Contract Closing**: Traders need to close contracts independently (e.g., close 3 of 11)
2. **FIFO Matching Simplicity**: Queue-based matching works cleanly with 1:1 records
3. **Consistency**: Same data model across UI and import enables reliable FIFO logic
4. **Auditability**: Each contract has its own lifecycle, timestamps, and closing relationship

---

## UI-Based Option Trade Creation

**Location**: `src/Core/UI/Creator.fs`, function `SaveOptionsTrade` (lines 286-320)

**Implementation Pattern**: When saving option trades from the UI, ALWAYS expand Quantity > 1 into multiple records:

1. If trade.Quantity > 1:
   - Divide NetPremium equally: `NetPremium_per_contract = Total / Quantity`
   - Create N records with Quantity = 1 each
   - All other fields remain identical

2. If trade.Quantity = 1: Store as-is

**Example**:
```
Input: SellToOpen 5 TSLL Calls with NetPremium=$50
Output: 5 database records:
  - Record 1: Quantity=1, NetPremium=$10
  - Record 2: Quantity=1, NetPremium=$10
  - Record 3: Quantity=1, NetPremium=$10
  - Record 4: Quantity=1, NetPremium=$10
  - Record 5: Quantity=1, NetPremium=$10
```

---

## CSV Import Processing

**Location**: `src/Core/Import/DatabasePersistence.fs`, function `createOptionTradeFromTransaction` (lines 210-270)

**REQUIREMENT**: CSV import MUST expand trades like the UI does.

**Current Behavior (❌ BUG)**:
- Creates 1 record per CSV line
- Preserves Quantity field from CSV
- NetPremium not proportionally divided

**Required Behavior (✅ FIX)**:
- If transaction.Quantity > 1:
  - Calculate NetPremium per contract: `netPremiumPerContract = netPremium / Quantity`
  - Create Quantity number of records, each with Quantity=1
  - Return collection instead of single option

**Affected Code**:
- Return type must change from `OptionTrade option` to `OptionTrade list`
- Calling code (line ~612) must iterate through expanded trades
- Each expanded trade must be persisted and potentially linked separately

---

## FIFO Matching Algorithm

**Location**: `src/Core/Database/OptionTradeExtensions.fs`
- Function `linkClosingTrade` (lines 199-228)
- Function `tryFindOpenTradeForClosing` (lines 162-180)

**Matching Process**:
1. For each closing trade (BuyToClose or SellToClose):
   - Find matching opening trade by: TickerId, CurrencyId, BrokerAccountId, OptionType, Strike, Expiration
   - Use FIFO order: ORDER BY TimeStamp, LIMIT 1
2. Link the records:
   - Set IsOpen = false on opening trade
   - Set ClosedWith = closing_trade_id
3. If no match found: Log error (non-critical), continue processing

**Why Expansion Fixes This**:
- With Quantity=1: FIFO naturally matches 1 opening to 1 closing
- Without expansion: Quantity mismatch prevents proper matching

**Example Fix**:
```
Before Expansion (BROKEN):
  SellToOpen Quantity=5, SellToOpen Quantity=6
  BuyToClose Quantity=11
  Result: Only first SellToOpen (Qty=5) gets linked, second ignored

After Expansion (FIXED):
  SellToOpen Qty=1 (record 1), ... (record 5)
  SellToOpen Qty=1 (record 6), ... (record 11)
  BuyToClose Qty=1 (record 12), ... (record 22)
  Result: All 11 opening records get linked to closing records
```

---

## IsOpen Flag Management

**Truth Table**:

| IsOpen | ClosedWith | Meaning | Valid? |
|--------|-----------|---------|--------|
| `true` | `null` | Position opened, not yet closed | ✅ YES |
| `false` | `closing_trade_id` | Position closed and linked | ✅ YES |
| `true` | not null | Marked open but linked to closing trade | ❌ NO |
| `false` | `null` | Marked closed but not linked | ❌ NO |

**Update Process**:
When a closing trade matches an opening trade:
1. Set `IsOpen = false`
2. Set `ClosedWith = closing_trade_id`
3. Update Audit timestamp
4. Persist to database

---

## Edge Cases & Validation

### 1. Partial Closing
**Scenario**: Trader opens 5 contracts, closes 3

After expansion with Quantity=1:
- 5 opening records created
- 3 closing records created
- 3 of 5 opening records get IsOpen=false
- 2 opening records remain with IsOpen=true

### 2. Same Strike/Expiration, Different Times
**Scenario**: Multiple SellToOpen at same timestamp, then BuyToClose

FIFO matching uses TimeStamp as primary sort, database insertion order as secondary.
Ensures consistent order even with simultaneous trades.

### 3. Closing Without Opening
**Scenario**: BuyToClose without corresponding SellToOpen (data corruption)

Behavior:
- Closing trade is persisted ✓
- ClosedWith remains null (no match found)
- Error logged but import continues (non-critical)

### 4. Quantity Validation
Rule: Reject if transaction.Quantity <= 0

### 5. Expired Options
Rule: Do NOT automatically mark as closed based on expiration date
Only explicit close transactions (BuyToClose, SellToClose) update IsOpen flag

---

## Implementation Locations

| File | Function | Change | Priority |
|------|----------|--------|----------|
| `src/Core/Import/DatabasePersistence.fs` | `createOptionTradeFromTransaction` | Expand Quantity > 1 | **HIGH** |
| `src/Core/Import/DatabasePersistence.fs` | Calling code (line ~612) | Handle list of trades | **HIGH** |
| `src/Core/Database/OptionTradeExtensions.fs` | `linkClosingTrade` | Already works with Qty=1 | Check only |
| `src/Core/UI/Creator.fs` | `SaveOptionsTrade` | Verify expansion logic | Reference |

---

## Test Data & Known Issues

**Known Issue**: TSLL Import with Option Contracts

File: `src/Tests/Core.Platform.MauiTester/Resources/TestData/TsllImportTest.csv`

Lines 182-184: SellToOpen
- Line 182: SellToOpen 5 TSLL 11/15/24 Call 17.00 @ 0.85
- Line 184: SellToOpen 6 TSLL 11/15/24 Call 17.00 @ 0.85

Line 179: BuyToClose
- Line 179: BuyToClose 11 TSLL 11/15/24 Call 17.00 @ 0.95

Current Issue: 5+6 opening records not fully linked to 11 closing record
After Fix: All 11 opening records (1 per contract) will be properly linked

---

## Reference Implementations

**UI Expansion (Correct)**: `src/Core/UI/Creator.fs` lines 286-301
```fsharp
let expandedTrades =
    optionTrades
    |> List.collect (fun trade ->
        if trade.Quantity > 1 then
            let netPremium = trade.NetPremium / decimal trade.Quantity
            [ for _ in 1 .. trade.Quantity ->
                  { trade with
                      Quantity = 1
                      NetPremium = netPremium } ]
        else
            [ trade ])
```

**FIFO Linking (Correct)**: `src/Core/Database/OptionTradeExtensions.fs` lines 184-198
```fsharp
let updatedTrade =
    { openTrade with
        IsOpen = false
        ClosedWith = Some closingTradeId
        Audit = updatedAudit }
do! Do.save (updatedTrade)
```
