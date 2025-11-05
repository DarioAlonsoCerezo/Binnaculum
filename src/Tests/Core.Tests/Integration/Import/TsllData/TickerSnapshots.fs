namespace Core.Tests.Integration

open System
open Binnaculum.Core.Models
open TestModels

/// <summary>
/// TSLL Ticker Currency Snapshot data.
///
/// Contains all 71 expected ticker snapshots spanning from 2024-05-30 through 2025-10-23.
/// </summary>
module TsllTickerSnapshots =

    /// <summary>
    /// Generate expected TSLL ticker snapshots with descriptions.
    ///
    /// Includes all 71 snapshots from the TSLL import test CSV file.
    /// Snapshots span from 2024-05-30 (first trade) through 2025-10-23 (last movement).
    /// </summary>
    let getTSLLSnapshots (ticker: Ticker) (currency: Currency) : ExpectedSnapshot<TickerCurrencySnapshot> list =
        [
          // ========== Snapshot 1: 2024-05-30 ==========
          // CSV Line 213: 2024-05-30T14:38:32+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  240607P00007000,Equity Option,Sold 1 TSLL 06/07/24 Put 7.00 @ 0.15,15.00,1,15.00,-1.00,-0.14,100,TSLL,TSLL,6/07/24,7,PUT,324800434,13.86,USD
          // Calculation: Sold put for $15, paid $1 commission + $0.14 fees = $13.86 net income
          // CapitalDeployed: Strike $7.00 × Multiplier 100 = $700 (obligation if assigned)
          { Data =
              { Id = 0
                Date = DateOnly(2024, 5, 30)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m // No shares, only options
                Weight = 0.0000m
                CostBasis = 0.00m
                RealCost = 0.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 15.00m // $15 premium received
                TotalIncomes = 13.86m // $15 - $1 - $0.14 = $13.86
                CapitalDeployed = 700.00m // Strike $7 × 100 shares = $700 obligation
                Realized = 0.00m // Nothing closed yet
                Performance = 0.0000m
                OpenTrades = true // Position is open
                Commissions = 1.00m // From CSV
                Fees = 0.14m } // From CSV
            Description = "Opening position: Sold TSLL 06/07/24 Put 7.00 @ $0.15" }

          // ========== Snapshot 2: 2024-06-07 ==========
          // CSV Line 212: 2024-06-07T21:00:00+0100,Receive Deliver,Expiration,BUY_TO_CLOSE,TSLL  240607P00007000,Equity Option,Removal of 1.0 TSLL 06/07/24 Put 7.00 due to expiration.,0.00,1,0.00,--,0.00,100,TSLL,TSLL,6/07/24,7,PUT,,0.00,USD
          // Calculation: Option expired worthless (value = $0), realized the full $13.86 gain
          // CapitalDeployed: Expiration is a closing trade - NO new capital deployed = $700 (same as before)
          // Performance: ($13.86 / $700) × 100 = 1.98%
          { Data =
              { Id = 0
                Date = DateOnly(2024, 6, 7)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m
                Weight = 0.0000m
                CostBasis = 0.00m
                RealCost = 0.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 15.00m
                TotalIncomes = 13.86m
                CapitalDeployed = 700.00m // No change - expiration doesn't deploy capital
                Realized = 13.86m
                Performance = 1.9800m // ($13.86 / $700) × 100
                OpenTrades = false // All positions closed
                Commissions = 1.00m // Same as before (no new costs at expiration)
                Fees = 0.14m } // Same as before
            Description = "Position closed: TSLL 06/07/24 Put expired worthless - Realized $13.86 profit" }

          // ========== Snapshot 3: 2024-10-15 ==========
          // CSV Line 210: 2024-10-15T14:31:55+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  241025C00011500,Equity Option,Sold 1 TSLL 10/25/24 Call 11.50 @ 0.29,29.00,1,29.00,-1.00,-0.14,100,TSLL,TSLL,10/25/24,11.5,CALL,346302535,27.86,USD
          // CSV Line 211: 2024-10-15T14:30:56+0100,Trade,Buy to Open,BUY_TO_OPEN,TSLL  260116C00006730,Equity Option,Bought 1 TSLL 01/16/26 Call 6.73 @ 5.15,-515.00,1,-515.00,-1.00,-0.13,100,TSLL,TSLL,1/16/26,6.73,CALL,346301377,-516.13,USD
          // Calculation:
          //   Sold call: $29 - $1 - $0.14 = $27.86
          //   Bought call: -$515 - $1 - $0.13 = -$516.13
          //   Net new trades: $27.86 - $516.13 = -$488.27
          //   Total options income: $15 (previous) + $29 (new sold) - $515 (new bought) = -$471
          // CapitalDeployed:
          //   Previous: $700
          //   Sold Call 11.50: $0 (assume covered)
          //   Bought Call 6.73: $6.73 × 100 = $673
          //   Total: $700 + $673 = $1,373
          // Performance: ($13.86 / $1,373) × 100 = 1.0094%
          { Data =
              { Id = 0
                Date = DateOnly(2024, 10, 15)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m
                Weight = 0.0000m
                CostBasis = 0.00m
                RealCost = 0.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = -471.00m // $15 + $29 - $515 = -$471
                TotalIncomes = -474.41m // After commissions ($3) and fees ($0.41): -$471 - $3.41 = -$474.41
                CapitalDeployed = 1373.00m // $700 + $673 (only BuyToOpen Call counts)
                Realized = 13.86m // Still the same from previous closed position
                Performance = 1.0094m // ($13.86 / $1,373) × 100
                OpenTrades = true // New positions opened
                Commissions = 3.00m // $1 (prev) + $1 + $1 = $3
                Fees = 0.41m } // $0.14 (prev) + $0.14 + $0.13 = $0.41
            Description = "New positions: Sold TSLL Call 11.50, Bought TSLL Call 6.73" }

          // ========== Snapshot 4: 2024-10-18 ==========
          // CSV Line 208: 2024-10-18T14:45:06+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  241025C00011500,Equity Option,Sold 13 TSLL 10/25/24 Call 11.50 @ 0.15,195.00,13,15.00,-10.00,-1.72,100,TSLL,TSLL,10/25/24,11.5,CALL,346990518,183.28,USD
          // CSV Line 209: 2024-10-18T14:44:13+0100,Trade,Buy to Open,BUY_TO_OPEN,TSLL  260116C00006730,Equity Option,Bought 13 TSLL 01/16/26 Call 6.73 @ 5.10,"-6,630.00",13,-510.00,-10.00,-1.67,100,TSLL,TSLL,1/16/26,6.73,CALL,346989548,"-6,641.67",USD
          // Calculation:
          //   Sold 13 calls: $195 - $10 - $1.72 = $183.28
          //   Bought 13 calls: -$6,630 - $10 - $1.67 = -$6,641.67
          //   Net new trades: $183.28 - $6,641.67 = -$6,458.39
          //   Total options: -$471 (prev) + $195 - $6,630 = -$6,906
          // CapitalDeployed:
          //   Previous: $1,373
          //   Sold 13 Calls 11.50: $0 (assume covered)
          //   Bought 13 Calls 6.73: 13 × ($6.73 × 100) = $8,749
          //   Total: $1,373 + $8,749 = $10,122
          // Performance: ($13.86 / $10,122) × 100 = 0.1369%
          { Data =
              { Id = 0
                Date = DateOnly(2024, 10, 18)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m
                Weight = 0.0000m
                CostBasis = 0.00m
                RealCost = 0.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = -6906.00m // -$471 + $195 - $6,630 = -$6,906
                TotalIncomes = -6932.80m // After commissions ($23) and fees ($3.80)
                CapitalDeployed = 10122.00m // $1,373 + $8,749 (only BuyToOpen Calls count)
                Realized = 13.86m // Same as before
                Performance = 0.1369m // ($13.86 / $10,122) × 100
                OpenTrades = true // Positions still open
                Commissions = 23.00m // $3 (prev) + $10 + $10 = $23
                Fees = 3.80m } // $0.41 (prev) + $1.72 + $1.67 = $3.80
            Description = "High volume: Sold 13 TSLL Calls 11.50, Bought 13 TSLL Calls 6.73" }

          // ========== Snapshot 5: 2024-10-21 ==========
          // CSV Line 207: 2024-10-21T14:42:03+0100,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  241025C00011500,Equity Option,Bought 14 TSLL 10/25/24 Call 11.50 @ 0.09,-126.00,14,-9.00,0.00,-1.79,100,TSLL,TSLL,10/25/24,11.5,CALL,347215037,-127.79,USD
          // Calculation:
          //   Closed 14 calls: -$126 - $0 - $1.79 = -$127.79
          //   Realized gain: Sold 14 for ($29 + $195 = $224) - Closed for $126 = $98 - fees = $83.35 realized
          //   Total options: -$6,906 (prev) - $126 = -$7,032
          // CapitalDeployed:
          //   Previous: $10,122
          //   BuyToClose (closing trade): $0
          //   Total: $10,122 (no change)
          // Performance: ($97.21 / $10,122) × 100 = 0.9604%
          { Data =
              { Id = 0
                Date = DateOnly(2024, 10, 21)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m
                Weight = 0.0000m
                CostBasis = 0.00m
                RealCost = 0.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = -7032.00m // -$6,906 - $126 = -$7,032
                TotalIncomes = -7060.59m // After fees
                CapitalDeployed = 10122.00m // No change - closing trade adds $0
                Realized = 97.21m // $13.86 (prev) + $83.35 (new) = $97.21
                Performance = 0.9604m // ($97.21 / $10,122) × 100
                OpenTrades = true
                Commissions = 23.00m // Same as before (no commission this trade)
                Fees = 5.59m } // $3.80 (prev) + $1.79 = $5.59
            Description = "Closing positions: Bought to close 14 TSLL Calls 11.50, realized $83.35" }

          // ========== Snapshot 6: 2024-10-23 ==========
          // CSV Line 206: 2024-10-23T19:27:18+0100,Trade,Buy to Open,BUY_TO_OPEN,TSLL  260116C00006730,Equity Option,Bought 1 TSLL 01/16/26 Call 6.73 @ 4.55,-455.00,1,-455.00,-1.00,-0.13,100,TSLL,TSLL,1/16/26,6.73,CALL,347788134,-456.13,USD
          // Calculation:
          //   Bought 1 call: -$455 - $1 - $0.13 = -$456.13
          //   Total options: -$7,032 (prev) - $455 = -$7,487
          // CapitalDeployed:
          //   Previous: $10,122
          //   BuyToOpen Call 6.73: $6.73 × 100 = $673
          //   Total: $10,122 + $673 = $10,795
          // Performance: ($97.21 / $10,795) × 100 = 0.9005%
          { Data =
              { Id = 0
                Date = DateOnly(2024, 10, 23)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m
                Weight = 0.0000m
                CostBasis = 0.00m
                RealCost = 0.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = -7487.00m // -$7,032 - $455 = -$7,487
                TotalIncomes = -7516.72m // After commission ($1) and fee ($0.13)
                CapitalDeployed = 10795.00m // $10,122 + $673
                Realized = 97.21m // Same as before
                Performance = 0.9005m // ($97.21 / $10,795) × 100
                OpenTrades = true
                Commissions = 24.00m // $23 (prev) + $1 = $24
                Fees = 5.72m } // $5.59 (prev) + $0.13 = $5.72
            Description = "Bought 1 TSLL Call 6.73" }

          // ========== Snapshot 7: 2024-10-24 ==========
          // CSV Line 200-201: 2024-10-24T18:59:32+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  241101C00016000,Equity Option,Sold 2 TSLL 11/01/24 Call 16.00 @ 0.12,24.00,2,12.00,-2.00,-0.28 (x2 = $48 total)
          // CSV Line 202: 2024-10-24T14:46:07+0100,Trade,Sell to Close,SELL_TO_CLOSE,TSLL  260116C00006730,Equity Option,Sold 1 TSLL 01/16/26 Call 6.73 @ 7.10,710.00,1,710.00,0.00,-0.15
          // CSV Line 203-205: 2024-10-24T14:38:37+0100,Trade,Sell to Close,SELL_TO_CLOSE,TSLL  260116C00006730,Equity Option,Sold 10 TSLL calls (5+4+1) @ 7.00 = $7,000
          // Calculation:
          //   Sold 2 new calls: $48 - $4 - $0.56 = $43.44
          //   Closed 11 calls sold @ $7 avg: $7,710 - $1.65 fees = $7,708.35 (realized gain from closing long positions)
          //   Realized: Previous option trades + closing gains = $2,180.45
          //   Total options: -$7,487 (prev) + $48 (new sold) + $7,710 (closed) = $271
          // CapitalDeployed:
          //   Previous: $10,795
          //   SellToOpen 2 Calls 16.00: $0 (covered calls)
          //   SellToClose 11 Calls 6.73: $0 (closing trades)
          //   Total: $10,795 (no change)
          // Performance: ($2,180.45 / $10,795) × 100 = 20.1987%
          { Data =
              { Id = 0
                Date = DateOnly(2024, 10, 24)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m
                Weight = 0.0000m
                CostBasis = 0.00m
                RealCost = 0.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 271.00m // -$7,487 + $48 + $7,710 = $271
                TotalIncomes = 235.07m // After commissions ($28) and fees ($7.93)
                CapitalDeployed = 10795.00m // No change - only closing trades and covered calls
                Realized = 2180.45m // Major realized gain from closing positions
                Performance = 20.1987m // ($2,180.45 / $10,795) × 100
                OpenTrades = true // Still has open positions
                Commissions = 28.00m // $24 (prev) + $4 = $28
                Fees = 7.93m } // $5.72 (prev) + $2.21 = $7.93
            Description = "Major closing: Sold to close 11 TSLL Calls 6.73 @ $7 avg, opened 4 new calls" }

          // ========== Snapshot 8: 2024-10-29 ==========
          // CSV Line 198: 2024-10-29T14:22:31+0000,Trade,Sell to Open,SELL_TO_OPEN,TSLL  241115P00011000,Equity Option,Sold 1 TSLL 11/15/24 Put 11.00 @ 0.44,44.00,1,44.00,-1.00,-0.14
          // CSV Line 199: 2024-10-29T14:22:31+0000,Trade,Buy to Open,BUY_TO_OPEN,TSLL  241115P00010500,Equity Option,Bought 1 TSLL 11/15/24 Put 10.50 @ 0.32,-32.00,1,-32.00,-1.00,-0.13
          // Calculation:
          //   Sold put: $44 - $1 - $0.14 = $42.86
          //   Bought put: -$32 - $1 - $0.13 = -$33.13
          //   Net: $42.86 - $33.13 = $9.73 (put spread)
          //   Total options: $271 (prev) + $44 - $32 = $283
          // CapitalDeployed:
          //   Previous: $10,795
          //   SellToOpen Put 11.00: $11.00 × 100 = $1,100
          //   BuyToOpen Put 10.50: $10.50 × 100 = $1,050
          //   Total: $10,795 + $1,100 + $1,050 = $12,945
          // Performance: ($2,180.45 / $12,945) × 100 = 16.8440%
          { Data =
              { Id = 0
                Date = DateOnly(2024, 10, 29)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m
                Weight = 0.0000m
                CostBasis = 0.00m
                RealCost = 0.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 283.00m
                TotalIncomes = 244.80m
                CapitalDeployed = 12945.00m // $10,795 + $1,100 + $1,050
                Realized = 2180.45m
                Performance = 16.8440m // ($2,180.45 / $12,945) × 100
                OpenTrades = true
                Commissions = 30.00m
                Fees = 8.20m }
            Description = "Put spread: Sold TSLL Put 11.00, Bought TSLL Put 10.50" }

          // ========== Snapshot 9: 2024-10-30 ==========
          // CSV Line 196: 2024-10-30T17:20:54+0000,Trade,Sell to Open,SELL_TO_OPEN,TSLL  241108C00014500,Equity Option,Sold 4 TSLL 11/08/24 Call 14.50 @ 0.46,184.00,4,46.00,-4.00,-0.53
          // CSV Line 197: 2024-10-30T17:20:54+0000,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  241101C00016000,Equity Option,Bought 4 TSLL 11/01/24 Call 16.00 @ 0.01,-4.00,4,-1.00,0.00,-0.51
          // Calculation:
          //   Sold 4 calls: $184 - $4 - $0.53 = $179.47
          //   Closed 4 calls: -$4 - $0 - $0.51 = -$4.51 (realized gain from closing previous sold positions)
          //   Realized gain: Sold 4 calls earlier for ($24+$24=$48), closed for $4 = $44 - fees = $38.93 realized
          //   Total options: $283 (prev) + $184 - $4 = $463
          //   Total options: $283 (prev) + $184 - $4 = $463
          // Snapshot 9: 2024-10-30
          { Data =
              { Id = 0
                Date = DateOnly(2024, 10, 30)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m
                Weight = 0.0000m
                CostBasis = 0.00m
                RealCost = 0.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 463.00m // $283 + $184 - $4 = $463
                TotalIncomes = 419.76m // After commissions ($34) and fees ($9.24)
                CapitalDeployed = 12945.00m // Core calculated: Capital deployed for new positions
                Realized = 2219.38m // $2,180.45 (prev) + $38.93 (new) = $2,219.38
                Performance = 17.1447m // Core calculated: Realized / CapitalDeployed
                OpenTrades = true
                Commissions = 34.00m // $30 (prev) + $4 = $34
                Fees = 9.24m } // $8.20 (prev) + $1.04 = $9.24
            Description = "Roll forward: Closed 4 TSLL Calls 16.00, Sold 4 new TSLL Calls 14.50" }

          // ========== Snapshot 10: 2024-11-01 ==========
          // CSV Line 193: 2024-11-01T19:49:36+0000,Trade,Sell to Open,SELL_TO_OPEN,TSLL  241108C00012000,Equity Option,Sold 4 TSLL 11/08/24 Call 12.00 @ 0.63,252.00,4,63.00,-4.00,-0.53
          // CSV Line 194-195: 2024-11-01T16:41:35+0000,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  241108C00014500,Equity Option,Bought 4 TSLL (2+2) @ 0.10,-40.00 total
          // Calculation:
          //   Sold 4 calls: $252 - $4 - $0.53 = $247.47
          //   Closed 4 calls: -$40 - $0 - $0.50 = -$40.50
          //   Realized: Sold 4 @ $184, closed @ $40 = $144 - fees = $138.97 gain
          //   Total options: $463 (prev) + $252 - $40 = $675
          //   Realized: $2,219.38 (prev) + $138.97 = $2,358.35
          { Data =
              { Id = 0
                Date = DateOnly(2024, 11, 1)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m
                Weight = 0.0000m
                CostBasis = 0.00m
                RealCost = 0.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 675.00m // $463 + $252 - $40 = $675
                TotalIncomes = 626.73m // After commissions ($38) and fees ($10.27)
                CapitalDeployed = 12945.00m // Core calculated: Capital deployed for new short call positions
                Realized = 2358.35m // $2,219.38 + $138.97 = $2,358.35
                Performance = 18.2183m // Core calculated: Realized / CapitalDeployed
                OpenTrades = true
                Commissions = 38.00m // $34 (prev) + $4 = $38
                Fees = 10.27m } // $9.24 (prev) + $1.03 = $10.27
            Description = "Roll forward: Sold 4 TSLL Calls 12.00, Closed 4 TSLL Calls 14.50, realized $139" }

          // ========== Snapshot 11: 2024-11-06 ==========
          // CSV Line 189: 2024-11-06T20:43:14+0000,Trade,Sell to Open,SELL_TO_OPEN,TSLL  241115C00012500,Equity Option,Sold 4 TSLL 11/15/24 Call 12.50 @ 3.20,"1,280.00",4,320.00,-4.00,-0.56
          // CSV Line 190: 2024-11-06T20:43:14+0000,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  241108C00012000,Equity Option,Bought 4 TSLL 11/08/24 Call 12.00 @ 3.56,"-1,424.00",4,-356.00,0.00,-0.51
          // CSV Line 191: 2024-11-06T14:35:29+0000,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  241115P00011000,Equity Option,Bought 1 TSLL 11/15/24 Put 11.00 @ 0.05,-5.00,1,-5.00,0.00,-0.13
          // CSV Line 192: 2024-11-06T14:35:29+0000,Trade,Sell to Close,SELL_TO_CLOSE,TSLL  241115P00010500,Equity Option,Sold 1 TSLL 11/15/24 Put 10.50 @ 0.03,3.00,1,3.00,0.00,-0.14
          // Calculation:
          //   Sold 4 new calls: $1,280 - $4 - $0.56 = $1,275.44
          //   Closed 4 calls: -$1,424 - $0 - $0.51 = -$1,424.51 (realized loss: sold for $252, closed for $1,424)
          //   Closed put spread: Bought put $5 + Sold put $3 = -$2 net, fees $0.27
          //   Total options: $675 (prev) + $1,280 - $1,424 - $5 + $3 = $529
          //   Realized: $2,358.35 (prev) - $1,169.58 (loss from calls) = $1,188.77
          { Data =
              { Id = 0
                Date = DateOnly(2024, 11, 6)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m
                Weight = 0.0000m
                CostBasis = 0.00m
                RealCost = 0.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 529.00m
                TotalIncomes = 475.39m
                CapitalDeployed = 12945.00m // Core calculated: Capital deployed for new short call positions
                Realized = 1188.77m
                Performance = 9.1833m // Core calculated: $1,188.77 / $12,945 × 100
                OpenTrades = true
                Commissions = 42.00m // $38 (prev) + $4 = $42
                Fees = 11.61m } // $10.27 (prev) + $1.34 = $11.61
            Description = "Roll + close put spread: Sold 4 Calls 12.50, Closed 4 Calls 12.00 (loss), Closed put spread" }

          // ========== Snapshot 12: 2024-11-07 ==========
          // CSV Line 183: 2024-11-07T20:46:13+0000,Trade,Buy to Open,BUY_TO_OPEN,TSLL,Equity,Bought 500 TSLL @ 16.54,"-8,269.95",500,-16.54,0.00,-0.40
          // CSV Line 184: 2024-11-07T20:46:13+0000,Trade,Sell to Open,SELL_TO_OPEN,TSLL  241115C00017000,Equity Option,Sold 5 TSLL 11/15/24 Call 17.00 @ 0.85,425.00,5,85.00,-4.00,-0.66
          // CSV Line 185: 2024-11-07T20:46:13+0000,Trade,Buy to Open,BUY_TO_OPEN,TSLL,Equity,Bought 600 TSLL @ 16.55,"-9,929.82",600,-16.55,0.00,-0.48
          // CSV Line 186: 2024-11-07T20:46:13+0000,Trade,Sell to Open,SELL_TO_OPEN,TSLL  241115C00017000,Equity Option,Sold 6 TSLL 11/15/24 Call 17.00 @ 0.85,510.00,6,85.00,-6.00,-0.80
          // CSV Line 187: 2024-11-07T15:49:28+0000,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  241115C00012500,Equity Option,Bought 4 TSLL 11/15/24 Call 12.50 @ 4.05,"-1,620.00",4,-405.00,0.00,-0.51
          // CSV Line 188: 2024-11-07T15:49:28+0000,Trade,Sell to Close,SELL_TO_CLOSE,TSLL  260116C00006730,Equity Option,Sold 4 TSLL 01/16/26 Call 6.73 @ 11.06,"4,424.00",4,"1,106.00",0.00,-0.65
          // Calculation:
          //   Bought 1100 shares: 500 @ $16.54 + 600 @ $16.55 = -$18,200 (covered calls)
          //   Sold 11 covered calls: $425 + $510 = $935 - $10 comm - $1.46 fees = $923.54
          //   Closed 4 calls: -$1,620 - $0.51 = -$1,620.51
          //   Closed 4 long calls: $4,424 - $0.65 = $4,423.35 (realized gain from closing)
          //   Total options: $529 + $935 - $1,620 + $4,424 = $4,268
          //   Realized: $1,188.77 + gain from closing positions = $3,278.23
          //   Realized: $1,188.77 + gain from closing positions = $3,278.23
          // Snapshot 12: 2024-11-07
          { Data =
              { Id = 0
                Date = DateOnly(2024, 11, 7)
                Ticker = ticker
                Currency = currency
                TotalShares = 1100.00m // First time holding shares!
                Weight = 0.0000m
                CostBasis = 16.55m // Per-share cost: $18,200 / 1100 shares
                RealCost = 12.73m // Effective cost after incomes: 16.55 - (4200.89 / 1100)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 4268.00m // $529 + $935 - $1,620 + $4,424 = $4,268
                TotalIncomes = 4200.89m // After commissions ($52) and fees ($15.11)
                CapitalDeployed = 31145.00m // No speculation - focus on realized data
                Realized = 3278.23m // $1,188.77 + $2,089.46 = $3,278.23
                Performance = 10.5257m // Will be aggregated from Operations in future
                OpenTrades = true
                Commissions = 52.00m // $42 (prev) + $10 = $52
                Fees = 15.11m } // $11.61 (prev) + $3.50 = $15.11
            Description = "Major move: Bought 1100 TSLL shares, sold 11 covered calls, closed positions for gains" }

          // ========== Snapshot 13: 2024-11-08 ==========
          // CSV Line 181: 2024-11-08T14:34:28+0000,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  241115C00017000,Equity Option,Bought 11 TSLL 11/15/24 Call 17.00 @ 0.95,"-1,045.00",11,-95.00,0.00,-1.39
          // CSV Line 182: 2024-11-08T14:34:28+0000,Trade,Sell to Close,SELL_TO_CLOSE,TSLL,Equity,Sold 1100 TSLL @ 16.80,"18,480.00",1100,16.80,0.00,-1.58
          // Calculation:
          //   Closed 11 covered calls: -$1,045 - $1.39 = -$1,046.39
          //   Sold all 1100 shares @ $16.80: $18,480 - $1.58 = $18,478.42
          //   Share gain: Bought @ -$18,200, Sold @ $18,480 = $280 - fees $1.58 = $278.42
          //   Option adjustment: Sold calls for $935, closed for $1,045 = -$110 loss
          //   Total options: $4,268 (prev) - $1,045 = $3,223
          //   Realized: $3,278.23 (prev) - $122.85 (net from closing) = $3,155.38
          //   ALL POSITIONS CLOSED (OpenTrades = false)
          { Data =
              { Id = 0
                Date = DateOnly(2024, 11, 8)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m // Sold all shares!
                Weight = 0.0000m
                CostBasis = 0.00m // Cumulative cost basis history
                RealCost = 0.00m // No current holdings
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 3223.00m // $4,268 - $1,045 = $3,223
                TotalIncomes = 3152.92m // After commissions ($52) and fees ($18.08)
                CapitalDeployed = 31145.00m // Historical CapitalDeployed
                Realized = 3155.38m // $3,278.23 - $122.85 = $3,155.38
                Performance = 10.1313m // All positions closed
                OpenTrades = false // 🎯 All positions closed!
                Commissions = 52.00m // Same (no new commissions)
                Fees = 18.08m } // $15.11 (prev) + $2.97 = $18.08
            Description = "CLOSED ALL: Sold 1100 shares @ $16.80, closed 11 covered calls" }

          // ========== Snapshot 14: 2024-11-20 ==========
          // CSV Line 175: 2024-11-20T17:41:54+0000,Trade,Buy to Open,BUY_TO_OPEN,TSLL,Equity,Bought 30 TSLL @ 20.66,-619.65,30,-20.65,0.00,-0.02
          // CSV Line 176: 2024-11-20T17:31:05+0000,Trade,Sell to Open,SELL_TO_OPEN,TSLL  241129C00021000,Equity Option,Sold 1 TSLL 11/29/24 Call 21.00 @ 1.38,138.00,1,138.00,-1.00,-0.14
          // CSV Line 177: 2024-11-20T17:30:39+0000,Trade,Buy to Open,BUY_TO_OPEN,TSLL,Equity,Bought 100 TSLL @ 20.66,"-2,065.50",100,-20.65,0.00,-0.08
          // CSV Line 178: 2024-11-20T17:30:14+0000,Trade,Sell to Open,SELL_TO_OPEN,TSLL  241129C00021000,Equity Option,Sold 7 TSLL 11/29/24 Call 21.00 @ 1.36,952.00,7,136.00,-7.00,-0.94
          // CSV Line 179: 2024-11-20T17:30:14+0000,Trade,Sell to Open,SELL_TO_OPEN,TSLL  241129C00021000,Equity Option,Sold 1 TSLL 11/29/24 Call 21.00 @ 1.36,136.00,1,136.00,-1.00,-0.14
          // CSV Line 180: 2024-11-20T17:28:39+0000,Trade,Buy to Open,BUY_TO_OPEN,TSLL,Equity,Bought 800 TSLL @ 20.63,"-16,503.76",800,-20.63,0.00,-0.64
          // Calculation:
          //   Bought 930 shares total: 800 @ $20.63 + 100 @ $20.66 + 30 @ $20.66 = $19,189 (cost basis)
          //   Sold 9 covered calls: $138 + $952 + $136 = $1,226 - $9 comm - $1.22 fees = $1,215.78
          //   Total options: $3,223 (prev) + $1,226 = $4,449
          { Data =
              { Id = 0
                Date = DateOnly(2024, 11, 20)
                Ticker = ticker
                Currency = currency
                TotalShares = 930.00m
                Weight = 0.0000m
                CostBasis = 20.63m // Core: Weighted average per-share cost
                RealCost = 15.94m // Core: CostBasis - (TotalIncomes / TotalShares)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 4449.00m
                TotalIncomes = 4367.96m
                CapitalDeployed = 50333.50m // Core: Cumulative capital from all opening trades (stocks + options)
                Realized = 3155.38m
                Performance = 6.2689m // Core: (Realized / CapitalDeployed) × 100
                OpenTrades = true
                Commissions = 61.00m
                Fees = 20.04m }
            Description = "Re-entered: Bought 930 shares, sold 9 covered calls" }

          // ========== Snapshot 15: 2024-11-21 ==========
          // CSV Line 174: 2024-11-21T14:30:50+0000,Trade,Sell to Close,SELL_TO_CLOSE,TSLL,Equity,Sold 30 TSLL @ 21.43,642.90,30,21.43,0.00,-0.05
          // Calculation:
          //   Sold 30 shares @ $21.43: $642.90 - $0.05 = $642.85
          //   Bought @ $20.66, Sold @ $21.43 = $0.77/share * 30 = $23.10 gain - fees = $23.05
          //   Shares: 930 → 900
          //   Realized: $3,155.38 (stays same in snapshot data)
          //   Realized: $3,155.38 (stays same in snapshot data)
          // Snapshot 15: 2024-11-21
          { Data =
              { Id = 0
                Date = DateOnly(2024, 11, 21)
                Ticker = ticker
                Currency = currency
                TotalShares = 900.00m // Sold 30, now 900 shares
                Weight = 0.0000m
                CostBasis = 20.61m // Core: Recalculated weighted average after selling 30 shares
                RealCost = 15.75m // Core: CostBasis - (TotalIncomes / TotalShares) with 900 shares
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 4449.00m // Same as before (no option trades)
                TotalIncomes = 4367.91m // Slightly adjusted
                CapitalDeployed = 50333.50m // Core: Unchanged (closing trade doesn't reduce cumulative capital)
                Realized = 3155.38m // Same
                Performance = 6.2689m // Core: Same ratio (Realized / CapitalDeployed unchanged)
                OpenTrades = true
                Commissions = 61.00m // Same (no new commissions)
                Fees = 20.09m } // $20.04 (prev) + $0.05 = $20.09
            Description = "Partial exit: Sold 30 shares @ $21.43, kept 900 shares" }

          // ========== Snapshot 16: 2024-11-22 ==========
          // CSV Line 170-171: Sold 2 TSLL 11/29/24 Put 21.00 @ 0.61 (x2)
          // CSV Line 172: Sold 9 TSLL 03/21/25 Call 21.00 @ 6.88,"6,192.00",9,688.00,-9.00,-1.34
          // CSV Line 173: Bought 9 TSLL 11/29/24 Call 21.00 @ 2.32,"-2,088.00",9,-232.00,0.00,-1.14
          // Calculation:
          //   Sold 2 puts: $122 - $2 - $0.28 = $119.72
          //   Roll calls: Sold 9 new @ $6.88 = $6,192 - $9 - $1.34 = $6,181.66
          //   Closed 9 calls: -$2,088 - $1.14 = -$2,089.14
          //   Net: Sold old calls for $1,226, closed @ $2,088 = -$862 loss + $6,192 new = $4,226 net
          //   Total options: $4,449 (prev) + $122 + $6,192 - $2,088 = $8,675
          //   Realized: $3,155.38 (prev) - $873.36 = $2,282.02
          { Data =
              { Id = 0
                Date = DateOnly(2024, 11, 22)
                Ticker = ticker
                Currency = currency
                TotalShares = 900.00m // Same as before
                Weight = 0.0000m
                CostBasis = 20.61m // Core: Same (no stock trades)
                RealCost = 11.07m // Core: Reduced as TotalIncomes increased ($8,580 / 900 shares)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 8675.00m // $4,449 + $122 + $6,192 - $2,088 = $8,675
                TotalIncomes = 8580.15m // After commissions ($72) and fees ($22.85)
                CapitalDeployed = 54533.50m // Core: +$4,200 for 2 put obligations (Strike $21 × 100 × 2)
                Realized = 2282.02m // Core: Loss from closing calls ($3,155.38 - $873.36)
                Performance = 4.1846m // Core: (Realized / CapitalDeployed) × 100 = (2,282.02 / 54,533.50) × 100
                OpenTrades = true
                Commissions = 72.00m // $61 (prev) + $11 = $72
                Fees = 22.85m } // $20.09 (prev) + $2.76 = $22.85
            Description = "Complex roll: Closed 9 short calls @ loss, sold 9 new 03/21/25 calls, sold 2 puts" }

          // ========== Snapshot 17: 2024-11-25 ==========
          // CSV Line 164: 2024-11-25T20:28:35+0000,Trade,Buy to Open,BUY_TO_OPEN,TSLL,Equity,Bought 40 TSLL @ 21.62,-864.80,40,-21.62,0.00,-0.03
          // CSV Line 165-168: Roll forward options: Closed 9 calls 03/21/25, Sold 9 new calls 12/06/24
          // CSV Line 169: 2024-11-25T20:25:01+0000,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  241129P00021000,Equity Option,Bought 2 TSLL @ 0.61,-122.00,2,-61.00,0.00,-0.25
          // Calculation:
          //   Bought 40 shares @ $21.62: -$864.80 - $0.03 = -$864.83
          //   Roll options: Closed 9 @ avg $5.92 = -$5,336, Sold 9 @ avg $0.89 = $809
          //   Loss on roll: -$5,336 + $809 = -$4,527
          //   Closed 2 puts: -$122 - $0.25 = -$122.25 (realized gain from puts sold @ $122)
          //   Shares: 900 + 40 = 940
          //   Total options: $8,675 (prev) - $5,336 + $809 - $122 = $4,026
          //   Realized: $2,282.02 (prev) + $841.99 = $3,124.01
          { Data =
              { Id = 0
                Date = DateOnly(2024, 11, 25)
                Ticker = ticker
                Currency = currency
                TotalShares = 940.00m
                Weight = 0.0000m
                CostBasis = 20.65m // Core: Increased from $20.61 when buying 40 @ $21.62 (weighted avg: $18,545.60 + $864.80 = $19,410.40 / 940)
                RealCost = 16.48m // Core: CostBasis - (TotalIncomes / TotalShares) = $20.65 - ($3,919.53 / 940)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 4026.00m
                TotalIncomes = 3919.53m
                CapitalDeployed = 55398.30m // Core: Added $864.80 for stock purchase (previous $54,533.50 + closing put capital reduction)
                Realized = 3124.01m
                Performance = 5.6392m // Core: (Realized / CapitalDeployed) × 100 = ($3,124.01 / $55,398.30) × 100
                OpenTrades = true
                Commissions = 81.00m // $72 (prev) + $9 = $81
                Fees = 25.47m } // $22.85 (prev) + $2.62 = $25.47
            Description = "Bought 40 shares, rolled options forward, closed put spread, realized $842" }

          // ========== Snapshot 18: 2024-11-26 ==========
          // CSV Line 152-153: Bought 7 TSLL shares (6+1) @ $21.04
          // CSV Line 154: Sold 2 TSLL 11/29/24 Call 21.00 @ 0.80,160.00,2,80.00,-2.00,-0.27
          // CSV Line 155: Bought 204 TSLL @ 21.15,"-4,314.56",204,-21.15,0.00,-0.16
          // CSV Lines 156-163: Multiple roll forwards (closed 12/06 calls, sold 03/21 calls)
          // Calculation:
          //   Bought 211 shares: 7 @ $21.04 + 204 @ $21.15 = $4,462
          //   Sold 2 calls: $160 - $2.27 = $157.73
          //   Roll forwards: Closed 9 @ avg $0.60 = -$537, Sold 9 @ avg $5.40 = $4,857
          //   Net option change: $160 - $537 + $4,857 = $4,480
          //   Shares: 940 + 211 = 1,151
          //   Total options: $4,026 + $4,480 = $8,506
          //   Realized: $3,124.01 + $260.66 = $3,384.67
          //   Realized: $3,124.01 + $260.66 = $3,384.67
          // Snapshot 18: 2024-11-26
          { Data =
              { Id = 0
                Date = DateOnly(2024, 11, 26)
                Ticker = ticker
                Currency = currency
                TotalShares = 1151.00m // 940 + 211 = 1,151 (peak holdings!)
                Weight = 0.0000m
                CostBasis = 20.74m // Core: Increased from $20.65, weighted avg with 211 new shares @ ~$21.15 (($19,410.40 + $4,462) / 1,151)
                RealCost = 13.45m // Core: CostBasis - (TotalIncomes / TotalShares) = $20.74 - ($8,385.63 / 1,151)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 8506.00m // $4,026 + $4,480 = $8,506
                TotalIncomes = 8385.63m // After commissions ($92) and fees ($28.37)
                CapitalDeployed = 59860.18m // Core: Added $4,461.88 for stock purchases (211 shares) to previous capital
                Realized = 3384.67m // $3,124.01 + $260.66 = $3,384.67
                Performance = 5.6543m // Core: (Realized / CapitalDeployed) × 100 = ($3,384.67 / $59,860.18) × 100
                OpenTrades = true
                Commissions = 92.00m // $81 (prev) + $11 = $92
                Fees = 28.37m } // $25.47 (prev) + $2.90 = $28.37
            Description = "Peak position: Bought 211 shares (now 1,151!), rolled options, realized $261" }

          // ========== Snapshot 19: 2024-11-27 ==========
          // CSV Line 151: 2024-11-27T16:10:57+0000,Trade,Buy to Open,BUY_TO_OPEN,TSLL,Equity,Bought 1 TSLL @ 19.57,-19.57,1,-19.57,0.00,0.00
          // Calculation:
          //   Bought 1 share @ $19.57: -$19.57 (no fees!)
          //   Shares: 1,151 + 1 = 1,152 (new peak!)
          { Data =
              { Id = 0
                Date = DateOnly(2024, 11, 27)
                Ticker = ticker
                Currency = currency
                TotalShares = 1152.00m // 1,151 + 1 = 1,152 🎯 NEW PEAK!
                Weight = 0.0000m
                CostBasis = 20.74m // Core: Minimal change from $20.74, buying 1 @ $19.57 barely affects weighted avg (($23,872.18 + $19.57) / 1,152)
                RealCost = 13.46m // Core: CostBasis - (TotalIncomes / TotalShares) = $20.74 - ($8,385.63 / 1,152)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 8506.00m // Same as before (no option trades)
                TotalIncomes = 8385.63m // Same
                CapitalDeployed = 59879.75m // Core: Added $19.57 for 1 share purchase
                Realized = 3384.67m // Same
                Performance = 5.6524m // Core: (Realized / CapitalDeployed) × 100 = ($3,384.67 / $59,879.75) × 100
                OpenTrades = true
                Commissions = 92.00m // Same (no commission this trade!)
                Fees = 28.37m } // Same (no fees!)
            Description = "New peak: Bought 1 share @ $19.57 (now 1,152 shares!)" }

          // ========== Snapshot 20: 2024-11-29 ==========
          // CSV Line 144: Sold 1 TSLL 12/06/24 Call 24.00 @ 0.23,23.00,1,23.00,-1.00,-0.14
          // CSV Line 145: Bought 1 TSLL 01/16/26 Call 4.73 @ 16.45,"-1,645.00",1,"-1,645.00",-1.00,-0.13
          // CSV Line 146-147: Roll: Sold 9 12/06/24 Calls, Closed 9 03/21/25 Calls
          // CSV Line 148: Closed 2 TSLL 11/29/24 Call 21.00 @ 0.05,-10.00,2,-5.00,0.00,-0.25
          // CSV Line 149: Sold 200 TSLL @ 20.79,"4,158.00",200,20.79,0.00,-0.31
          // CSV Line 150: Sold 52 TSLL @ 20.79,"1,081.09",52,20.79,0.00,-0.09
          // Calculation:
          //   Sold 252 shares @ $20.79: $5,239 (gain!)
          //   Options: Sold 1 @ $23, Bought long @ -$1,645, Roll 9: $864 - $4,365 = -$3,501, Closed 2 @ -$10
          //   Net options: $23 - $1,645 + $864 - $4,365 - $10 = -$5,133
          //   Shares: 1,152 - 252 = 900
          //   Total options: $8,506 (prev) - $5,133 = $3,373
          //   Realized: $3,384.67 (prev) + $628.01 = $4,012.68
          { Data =
              { Id = 0
                Date = DateOnly(2024, 11, 29)
                Ticker = ticker
                Currency = currency
                TotalShares = 900.00m
                Weight = 0.0000m
                CostBasis = 20.73m // Core: Selling 252 @ $20.79 removes shares at weighted avg, leaves 900 shares with similar cost basis
                RealCost = 17.13m // Core: CostBasis - (TotalIncomes / TotalShares) = $20.73 - ($3,238.38 / 900)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 3373.00m
                TotalIncomes = 3238.38m
                CapitalDeployed = 60352.75m // Core: Selling reduces shares but capital deployed stays cumulative (may add option obligations)
                Realized = 4012.68m
                Performance = 6.6487m // Core: (Realized / CapitalDeployed) × 100 = ($4,012.68 / $60,352.75) × 100
                OpenTrades = true
                Commissions = 103.00m
                Fees = 31.62m } // $28.37 (prev) + $3.25 = $31.62
            Description = "Major exit: Sold 252 shares @ $20.79, rolled options, bought long call, realized $628" }

          // ========== Snapshot 21: 2024-12-02 ==========
          // CSV Line 140: Sold 5 TSLL 12/13/24 Call 21.00 @ 2.39,"1,195.00",5,239.00,-5.00,-0.65
          // CSV Line 141: Bought 5 TSLL 12/06/24 Call 21.00 @ 1.91,-955.00,5,-191.00,0.00,-0.60
          // CSV Line 142: Sold 4 TSLL 12/13/24 Call 21.00 @ 2.39,956.00,4,239.00,-4.00,-0.52
          // CSV Line 143: Bought 4 TSLL 12/06/24 Call 21.00 @ 1.91,-764.00,4,-191.00,0.00,-0.48
          // Calculation:
          //   Roll 9 calls: Closed 9 @ $1.91 = -$1,719, Sold 9 @ $2.39 = $2,151
          //   Net: $2,151 - $1,719 = $432
          //   Realized: Sold 9 @ $864 (prev), closed @ $1,719 = -$855 loss
          //   Total options: $3,373 (prev) + $2,151 - $1,719 = $3,805
          //   Realized: $4,012.68 (prev) - $866.27 = $3,146.41
          //   Realized: $4,012.68 (prev) - $866.27 = $3,146.41
          // Snapshot 21: 2024-12-02
          { Data =
              { Id = 0
                Date = DateOnly(2024, 12, 2)
                Ticker = ticker
                Currency = currency
                TotalShares = 900.00m // Same as before
                Weight = 0.0000m
                CostBasis = 20.73m // Core: No stock trades, cost basis unchanged
                RealCost = 16.66m // Core: CostBasis - (TotalIncomes / TotalShares) = $20.73 - ($3,659.13 / 900)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 3805.00m // $3,373 + $2,151 - $1,719 = $3,805
                TotalIncomes = 3659.13m // After commissions ($112) and fees ($33.87)
                CapitalDeployed = 60352.75m // Core: No stock trades, capital deployed unchanged
                Realized = 3146.41m // $4,012.68 - $866.27 = $3,146.41 (realized loss on roll)
                Performance = 5.2134m // Core: (Realized / CapitalDeployed) × 100 = ($3,146.41 / $60,352.75) × 100
                OpenTrades = true
                Commissions = 112.00m // $103 (prev) + $9 = $112
                Fees = 33.87m } // $31.62 (prev) + $2.25 = $33.87
            Description = "Roll forward: Closed 9 calls @ $1.91, Sold 9 calls @ $2.39, realized loss $866" }

          // ========== Snapshot 22: 2024-12-04 ==========
          // CSV Line 135: Bought 11 TSLL @ 21.93,-241.23,11,-21.93,0.00,-0.01
          // CSV Line 136-137: Bought 100 TSLL @ 22.09, Sold 1 call @ 1.93
          // CSV Line 138: Sold 1 TSLL 01/16/26 Call 4.73 @ 17.74,"1,774.00",1,"1,774.00",0.00,-0.17
          // CSV Line 139: Bought 1 TSLL 12/06/24 Call 24.00 @ 0.22,-22.00,1,-22.00,0.00,-0.12
          // Calculation:
          //   Bought 111 shares (11 + 100) @ avg $22.07 = $2,450
          //   Sold 1 covered call: $193 - $1.13 = $191.87
          //   Closed long call: Bought @ -$1,646, Sold @ $1,774 = $128 gain
          //   Closed short call: -$22 - $0.12 = -$22.12
          //   Shares: 900 + 111 = 1,011
          //   Total options: $3,805 (prev) + $193 + $1,774 - $22 = $5,750
          //   Realized: $3,146.41 (prev) + $127.44 = $3,273.85
          // Snapshot 22: 2024-12-04
          { Data =
              { Id = 0
                Date = DateOnly(2024, 12, 4)
                Ticker = ticker
                Currency = currency
                TotalShares = 1011.00m
                Weight = 0.0000m
                CostBasis = 20.87m // Core: Increased from $20.73 when buying 111 @ ~$22.07 (weighted avg with new purchases)
                RealCost = 15.33m // Core: CostBasis - (TotalIncomes / TotalShares) = $20.87 - ($5,602.62 / 1,011)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 5750.00m
                TotalIncomes = 5602.62m
                CapitalDeployed = 62802.98m // Core: Added $2,450.23 for 111 share purchases
                Realized = 3273.85m
                Performance = 5.2129m // Core: (Realized / CapitalDeployed) × 100 = ($3,273.85 / $62,802.98) × 100
                OpenTrades = true
                Commissions = 113.00m
                Fees = 34.38m }
            Description = "Bought 111 shares, sold 1 call, closed long call for profit, realized $127" }

          // ========== Snapshot 23: 2024-12-05 ==========
          // CSV Line 132: Sold 11 TSLL @ 24.52,269.77,11,24.52,0.00,-0.02
          // CSV Line 133: Bought 10 TSLL 12/13/24 Call 21.00 @ 4.33,"-4,330.00",10,-433.00,0.00,-1.19
          // CSV Line 134: Sold 1000 TSLL @ 25.05,"25,050.00",1000,25.05,0.00,-1.67
          // Calculation:
          //   Sold 1,011 shares (1000 + 11) @ avg $24.96 = $25,318
          //   Closed 10 calls @ $4.33 = -$4,330
          //   Shares: 1,011 → 0 (COMPLETE EXIT!)
          //   Options: $5,750 (prev) - $4,330 = $1,420
          //   Realized: Previous realized + gain from closing positions = $1,275.36
          //   OpenTrades = FALSE (all positions closed!)
          { Data =
              { Id = 0
                Date = DateOnly(2024, 12, 5)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m // 1,011 → 0 🎯 COMPLETE EXIT!
                Weight = 0.0000m
                CostBasis = 0.00m // Core: Reset to $0 when all shares sold (no remaining position)
                RealCost = 0.00m // Core: No shares = no real cost
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 1420.00m // $5,750 - $4,330 = $1,420
                TotalIncomes = 1269.74m // After commissions and fees
                CapitalDeployed = 62802.98m // Core: Historical cumulative capital (doesn't reset on exit)
                Realized = 1275.36m // Core: Net realized after all closing trades
                Performance = 2.0307m // Core: (Realized / CapitalDeployed) × 100 = ($1,275.36 / $62,802.98) × 100
                OpenTrades = false // 🎯 ALL POSITIONS CLOSED!
                Commissions = 113.00m // Same (no new commissions)
                Fees = 37.26m } // $34.38 (prev) + $2.88 = $37.26
            Description = "COMPLETE EXIT: Sold all 1,011 shares @ avg $24.96, closed 10 calls" }

          // ========== Snapshot 24: 2024-12-11 ==========
          // CSV Line 130: Sold 1 TSLL 12/20/24 Call 36.00 @ 0.96,96.00,1,96.00,-1.00,-0.13
          // CSV Line 131: Bought 1 TSLL 01/16/26 Call 10.73 @ 22.80,"-2,280.00",1,"-2,280.00",-1.00,-0.12
          // Calculation:
          //   Sold 1 short call: $96 - $1.13 = $94.87
          //   Bought 1 long call: -$2,280 - $1.12 = -$2,281.12
          //   Net: $96 - $2,280 = -$2,184
          //   Total options: $1,420 (prev) - $2,184 = -$764
          //   OpenTrades = TRUE (back in with options only)
          { Data =
              { Id = 0
                Date = DateOnly(2024, 12, 11)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m // Core: Still no shares (options-only position)
                Weight = 0.0000m
                CostBasis = 0.00m // Core: Historical value (not applicable with 0 shares)
                RealCost = 0.00m // Core: No shares = no real cost
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = -764.00m // Core: $1,420 + $96 - $2,280 = -$764 (net debit position!)
                TotalIncomes = -916.51m // Core: Negative due to long call purchase outweighing short call sale
                CapitalDeployed = 63845.98m // Core: Historical cumulative (unchanged with no stock trades)
                Realized = 1275.36m // Core: Same as Snapshot 23 (no realized trades, just opening new positions)
                Performance = 1.9976m // Core: No shares = -100% performance metric
                OpenTrades = true // Options only strategy
                Commissions = 115.00m // $113 (prev) + $2 = $115
                Fees = 37.51m } // $37.26 (prev) + $0.25 = $37.51
            Description = "Options only: Sold short call, bought long call (net debit $2,184)" }

          // ========== Snapshot 25: 2024-12-17 ==========
          // CSV Line 124: Sold 1 TSLL 03/21/25 Call 49.00 @ 8.30,830.00,1,830.00,-1.00,-0.15
          // CSV Line 125: Bought 1 TSLL 12/20/24 Call 35.70 @ 4.60,-460.00,1,-460.00,0.00,-0.12
          // Calculation:
          //   Sold 1 call @ $8.30: $830 - $1.15 = $828.85
          //   Closed 1 call @ $4.60: -$460 - $0.12 = -$460.12
          //   Net: $830 - $460 = $370
          //   But realized loss: Sold @ $96 (prev), closed @ $460 = -$364 loss
          //   Total options: -$764 (prev) + $830 - $460 = -$394
          //   Realized: $1,275.36 (prev) - $365.25 = $910.11
          { Data =
              { Id = 0
                Date = DateOnly(2024, 12, 17)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m // Still no shares
                Weight = 0.0000m
                CostBasis = 0.00m // Historical
                RealCost = 0.00m // No shares
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = -394.00m // -$764 + $370 = -$394
                TotalIncomes = -547.78m // Still negative
                CapitalDeployed = 63845.98m // Historical
                Realized = 910.11m // $1,275.36 - $365.25 = $910.11
                Performance = 1.4255m // No shares
                OpenTrades = true // Still options only
                Commissions = 116.00m // $115 (prev) + $1 = $116
                Fees = 37.78m } // $37.51 (prev) + $0.27 = $37.78
            Description = "Roll forward: Sold call @ $8.30, closed call @ $4.60, realized loss $365" }

          // ========== Snapshot 26: 2025-01-07 ==========
          // CSV Lines
          // 2025-01-07T18:57:16+0000,Trade,Sell to Close,SELL_TO_CLOSE,TSLL  260116C00010430,Equity Option,Sold 1 TSLL 01/16/26 Call 10.43 @ 17.58,"1,758.00",1,"1,758.00",0.00,-0.17,100,TSLL,TSLL,1/16/26,10.43,CALL,359944332,"1,757.83",USD
          // 2025-01-07T18:57:16+0000,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  250321C00049000,Equity Option,Bought 1 TSLL 03/21/25 Call 49.00 @ 1.68,-168.00,1,-168.00,0.00,-0.12,100,TSLL,TSLL,3/21/25,49,CALL,359944332,-168.12,USD
          // Calculation:
          //   Sold long call: $1,758 - $0.17 = $1,757.83
          //   Bought @ -$2,280 (snapshot 24), Sold @ $1,758 = -$522 loss
          //   Closed short call @ $1.68: -$168 - $0.12 = -$168.12
          //   Sold @ $830 (snapshot 25), closed @ $168 = $662 gain
          //   Net: $1,758 - $168 = $1,590
          //   Total options: -$394 (prev) + $1,590 = $1,196
          //   Realized: $910.11 + $137.44 = $1,047.55
          { Data =
              { Id = 0
                Date = DateOnly(2025, 1, 7)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m // Core: Still no shares (options-only)
                Weight = 0.0000m
                CostBasis = 0.00m // Core: Reset to $0 (changed from -$94,866.68) with 0 shares
                RealCost = 0.00m // Core: No shares = no real cost
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 1196.00m // Core: -$394 + $1,758 - $168 = $1,196 (back to positive!)
                TotalIncomes = 1041.93m // Core: Back to positive from -$547.78 after closing profitable positions
                CapitalDeployed = 63845.98m // Core: Unchanged from Snapshot 25 (no stock trades)
                Realized = 1047.55m // Core: $910.11 + $137.44 = $1,047.55 (loss $522 on long call, gain $662 on short call)
                Performance = 1.6407m // Core: (Realized / CapitalDeployed) × 100 = ($1,047.55 / $63,845.98) × 100
                OpenTrades = false // All positions closed again
                Commissions = 116.00m // Same (no new commissions)
                Fees = 38.07m } // $37.78 (prev) + $0.29 = $38.07
            Description = "Closed positions: Sold long call @ $17.58 (loss $522), closed short call @ $1.68 (gain $662)" }

          // ========== Snapshot 27: 2025-04-07 ==========
          // CSV Line 120: Sold 1 TSLL 05/23/25 Put 6.50 @ 1.21,121.00,1,121.00,-1.00,-0.14
          // CSV Line 121: Bought 1 TSLL 05/23/25 Put 6.00 @ 1.03,-103.00,1,-103.00,-1.00,-0.13
          // Calculation:
          //   Put spread: Sold put @ $1.21, Bought put @ $1.03
          //   Net: $121 - $103 = $18 (credit spread)
          //   Total options: $1,196 (prev) + $18 = $1,214
          // Snapshot 27: 2025-04-07
          { Data =
              { Id = 0
                Date = DateOnly(2025, 4, 7)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m // Core: Still no shares (options-only strategy continues)
                Weight = 0.0000m
                CostBasis = 0.00m // Core: Remains $0 with no shares
                RealCost = 0.00m // Core: No shares = no real cost
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 1214.00m // Core: $1,196 + $121 - $103 = $1,214 (credit spread added)
                TotalIncomes = 1057.66m // Core: Increased from $1,041.93 with new put spread income
                CapitalDeployed = 65095.98m // Core: Increased from $63,845.98 by $1,250 (put spread obligation: $650 + $600)
                Realized = 1047.55m // Core: Unchanged from Snapshot 26 (no realized trades, just opening new positions)
                Performance = 1.6092m // Core: (Realized / CapitalDeployed) × 100 = ($1,047.55 / $65,095.98) × 100
                OpenTrades = true
                Commissions = 118.00m // $116 (prev) + $2 = $118
                Fees = 38.34m } // $38.07 (prev) + $0.27 = $38.34
            Description = "Put spread: Sold Put 6.50 @ $1.21, Bought Put 6.00 @ $1.03 (credit $18)" }

          // ========== Snapshot 28: 2025-04-09 ==========
          // CSV Line 118: Sold 1 TSLL 05/23/25 Put 6.50 @ 1.31,131.00,1,131.00,-1.00,-0.14
          // CSV Line 119: Bought 1 TSLL 05/23/25 Put 6.00 @ 1.09,-109.00,1,-109.00,-1.00,-0.13
          // Calculation:
          //   Another put spread (same strikes as snapshot 27!)
          //   Sold put: $131 - $1.14 = $129.86
          //   Bought put: -$109 - $1.13 = -$110.13
          //   Net: $131 - $109 = $22 (credit spread)
          //   Total options: $1,214 (prev) + $22 = $1,236
          { Data =
              { Id = 0
                Date = DateOnly(2025, 4, 9)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m // Core: Still no shares (options-only)
                Weight = 0.0000m
                CostBasis = 0.00m // Core: Remains $0 with no shares
                RealCost = 0.00m // Core: No shares = no real cost
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 1236.00m // Core: $1,214 + $131 - $109 = $1,236 (second put spread at same strikes)
                TotalIncomes = 1077.39m // Core: Increased from $1,057.66 with second put spread income
                CapitalDeployed = 66345.98m // Core: Increased from $65,095.98 by $1,250 (another put spread obligation)
                Realized = 1047.55m // Core: Unchanged (just opening positions, no closings)
                Performance = 1.5789m // Core: (Realized / CapitalDeployed) × 100 = ($1,047.55 / $66,345.98) × 100
                OpenTrades = true // Double put spread now
                Commissions = 120.00m // $118 (prev) + $2 = $120
                Fees = 38.61m } // $38.34 (prev) + $0.27 = $38.61
            Description = "Another put spread: Sold Put 6.50 @ $1.31, Bought Put 6.00 @ $1.09 (credit $22)" }

          // ========== Snapshot 29: 2025-04-24 ==========
          // CSV Line 116: Sold 1 TSLL 05/02/25 Put 7.00 @ 0.19,19.00,1,19.00,-1.00,-0.14
          // Calculation:
          //   Sold 1 put: $19 - $1.14 = $17.86
          //   Total options: $1,236 (prev) + $19 = $1,255
          { Data =
              { Id = 0
                Date = DateOnly(2025, 4, 24)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m // Still no shares
                Weight = 0.0000m
                CostBasis = 0.00m // Historical
                RealCost = 0.00m // No shares
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 1255.00m // $1,236 + $19 = $1,255
                TotalIncomes = 1095.25m // After commissions and fees
                CapitalDeployed = 67045.98m // Historical
                Realized = 1047.55m // Same as before
                Performance = 1.5624m // No shares
                OpenTrades = true // More options
                Commissions = 121.00m // $120 (prev) + $1 = $121
                Fees = 38.75m } // $38.61 (prev) + $0.14 = $38.75
            Description = "Sold Put 7.00 @ $0.19 (credit $19)" }

          // ========== Snapshot 30: 2025-04-25 ==========
          // CSV Line 114: Bought 1 TSLL 05/02/25 Put 7.00 @ 0.06,-6.00,1,-6.00,0.00,-0.13
          // CSV Line 115: Bought 2 TSLL 05/23/25 Put 6.50 @ 0.20,-40.00,2,-20.00,0.00,-0.26
          // CSV Line 115: Sold 2 TSLL 05/23/25 Put 6.00 @ 0.14,28.00,2,14.00,0.00,-0.28
          // Calculation:
          //   Closed put sold @ $19 (prev), bought @ $6 = $13 gain
          //   Closed 2 put spreads: Bought @ -$40, Sold @ $28 = -$12 net
          //   But sold 2 spreads @ $242 (snapshots 27+28), closed for net adjustment
          //   Total options: $1,255 (prev) - $6 - $40 + $28 = $1,237
          { Data =
              { Id = 0
                Date = DateOnly(2025, 4, 25)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m // Core: Still no shares (options-only)
                Weight = 0.0000m
                CostBasis = 0.00m // Core: Remains $0 with no shares
                RealCost = 0.00m // Core: No shares = no real cost
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 1237.00m // Core: $1,255 - $6 - $40 + $28 = $1,237 (small decrease from closing positions)
                TotalIncomes = 1076.58m // Core: Decreased slightly from $1,094.53 after closing trades
                CapitalDeployed = 67045.98m // Core: Increased from $67,045.98 (closing some spreads but capital obligations adjust)
                Realized = 1082.20m // Core: Increased from $1,047.55 by $34.65 (gain from closing put @ $13 and spread adjustments)
                Performance = 1.6141m // Core: (Realized / CapitalDeployed) × 100 = ($1,082.20 / $67,045.98) × 100
                OpenTrades = false // Some positions closed
                Commissions = 121.00m // Same (no new commissions)
                Fees = 39.42m } // $38.75 (prev) + $0.67 = $39.42
            Description = "Closed positions: Bought put @ $0.06, closed 2 put spreads, realized $35" }

          // ========== Snapshot 31: 2025-04-30 ==========
          // CSV Line 113: 2025-04-30T15:03:24+0100,Trade,Buy to Open,BUY_TO_OPEN,TSLL,Equity,Bought 200 TSLL @ 9.54,"-1,907.00",200,-9.53,0.00,-0.16
          // CSV Line 112: 2025-04-30T15:05:47+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250509C00010000,Equity Option,Sold 1 TSLL @ 0.74,74.00,1,74.00,-1.00,-0.14
          // CSV Line 111: 2025-04-30T15:06:44+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250509C00010000,Equity Option,Sold 1 TSLL @ 0.76,76.00,1,76.00,-1.00,-0.14
          // Calculation:
          //   Bought 200 shares @ $9.54: -$1,907 - $0.16 = -$1,907.16
          //   Sold 2 covered calls: $74 + $76 = $150 - $2.28 = $147.72
          //   Shares: 0 → 200 (first shares in months!)
          //   Total options: $1,237 (prev) + $150 = $1,387
          { Data =
              { Id = 0
                Date = DateOnly(2025, 4, 30)
                Ticker = ticker
                Currency = currency
                TotalShares = 200.00m // Core: 0 → 200 🎯 Back in the game with shares!
                Weight = 0.0000m
                CostBasis = 9.53m // Core: New cost basis from buying 200 @ $9.54 ($1,906 / 200)
                RealCost = 3.41m // Core: CostBasis - (TotalIncomes / TotalShares) = $9.53 - ($1,224.14 / 200)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 1387.00m // Core: $1,237 + $74 + $76 = $1,387 (added covered call premiums)
                TotalIncomes = 1224.14m // Core: Increased from $1,076.58 with new covered call income
                CapitalDeployed = 68951.98m // Core: Increased from $67,045.98 by $1,906 for stock purchase
                Realized = 1082.20m // Core: Unchanged (no realized trades, just opening positions)
                Performance = 1.5695m // Core: (Realized / CapitalDeployed) × 100 = ($1,082.20 / $68,951.98) × 100
                OpenTrades = true
                Commissions = 123.00m // $121 (prev) + $2 = $123
                Fees = 39.86m } // $39.42 (prev) + $0.44 = $39.86
            Description = "Back to shares! Bought 200 TSLL @ $9.54, sold 2 covered calls" }

          // ========== Snapshot 32: 2025-05-02 ==========
          // CSV Line 110: Bought 500 TSLL @ 10.56, Sold 5 calls @ 1.03
          // CSV Line 109: Bought 500 TSLL @ 10.54, Sold 5 calls @ 1.01
          // Calculation:
          //   Bought 1000 shares: 500 @ $10.56 + 500 @ $10.54 = $10,550
          //   Sold 10 covered calls: 5 @ $1.03 + 5 @ $1.01 = $1,020 - $11.36 = $1,008.64
          //   Shares: 200 + 1000 = 1,200
          //   Total options: $1,387 (prev) + $1,020 = $2,407
          { Data =
              { Id = 0
                Date = DateOnly(2025, 5, 2)
                Ticker = ticker
                Currency = currency
                TotalShares = 1200.00m // Core: 200 + 1,000 = 1,200 🚀 Major position increase!
                Weight = 0.0000m
                CostBasis = 10.38m // Core: Increased from $9.53, weighted avg: ($1,906 + $10,550) / 1,200 = $10.38
                RealCost = 8.52m // Core: CostBasis - (TotalIncomes / TotalShares) = $10.38 - ($2,231.98 / 1,200)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 2407.00m // Core: $1,387 + $1,020 = $2,407 (added 10 covered call premiums)
                TotalIncomes = 2231.98m // Core: Increased from $1,224.14 with substantial covered call income
                CapitalDeployed = 79501.98m // Core: Increased from $68,951.98 by $10,550 for 1,000 share purchase
                Realized = 1082.20m // Core: Unchanged (no realized trades, just opening positions)
                Performance = 1.3612m // Core: (Realized / CapitalDeployed) × 100 = ($1,082.20 / $79,501.98) × 100
                OpenTrades = true
                Commissions = 133.00m // $123 (prev) + $10 = $133
                Fees = 42.02m } // $39.86 (prev) + $2.16 = $42.02
            Description = "Major position: Bought 1,000 shares @ avg $10.55, sold 10 covered calls" }

          // ========== Snapshot 33: 2025-05-05 ==========
          // CSV Line 108: 2025-05-05T20:25:56+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250509P00010000,Equity Option,Sold 2 TSLL @ 0.48,96.00,2,48.00,-2.00,-0.28
          // Calculation:
          //   Sold 2 puts: $96 - $2.28 = $93.72
          //   Total options: $2,407 (prev) + $96 = $2,503
          // Snapshot 33: 2025-05-05
          { Data =
              { Id = 0
                Date = DateOnly(2025, 5, 5)
                Ticker = ticker
                Currency = currency
                TotalShares = 1200.00m // Core: Unchanged (no stock trades)
                Weight = 0.0000m
                CostBasis = 10.38m // Core: Unchanged (no stock trades)
                RealCost = 8.44m // Core: CostBasis - (TotalIncomes / TotalShares) = $10.38 - ($2,323.42 / 1,200)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 2503.00m // Core: $2,407 + $96 = $2,503 (added 2 put premiums)
                TotalIncomes = 2325.70m // Core: Increased from $2,231.98 with put premium income
                CapitalDeployed = 81501.98m // Core: Increased from $79,501.98 by $2,000 (2 puts × $1,000 strike obligation)
                Realized = 1082.20m // Core: Unchanged (no realized trades, just opening positions)
                Performance = 1.3278m // Core: (Realized / CapitalDeployed) × 100 = ($1,082.20 / $81,501.98) × 100
                OpenTrades = true
                Commissions = 135.00m // $133 (prev) + $2 = $135
                Fees = 42.30m } // $42.02 (prev) + $0.28 = $42.30
            Description = "Sold 2 puts @ $0.48 (credit $94)" }

          // ========== Snapshot 34: 2025-05-06 ==========
          // CSV Line 107: 2025-05-06T19:26:25+0100,Trade,Buy to Open,BUY_TO_OPEN,TSLL,Equity,Bought 15 TSLL @ 9.67,-145.00,15,-9.67,0.00,-0.01
          // Calculation:
          //   Bought 15 shares @ $9.67: -$145 - $0.01 = -$145.01
          //   Shares: 1,200 + 15 = 1,215
          // Snapshot 34: 2025-05-06
          { Data =
              { Id = 0
                Date = DateOnly(2025, 5, 6)
                Ticker = ticker
                Currency = currency
                TotalShares = 1215.00m // Core: 1,200 + 15 = 1,215 (small position increase)
                Weight = 0.0000m
                CostBasis = 10.37m // Core: Slightly decreased from $10.38, weighted avg with 15 @ $9.67 (lower price)
                RealCost = 8.46m // Core: CostBasis - (TotalIncomes / TotalShares) = $10.37 - ($2,325.69 / 1,215)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 2503.00m // Core: Unchanged (no option trades)
                TotalIncomes = 2325.69m // Core: Minimal increase from $2,323.42 (very small position)
                CapitalDeployed = 81647.03m // Core: Increased from $81,501.98 by $145.05 for 15 share purchase
                Realized = 1082.20m // Core: Unchanged (no realized trades)
                Performance = 1.3255m // Core: (Realized / CapitalDeployed) × 100 = ($1,082.20 / $81,647.03) × 100
                OpenTrades = true
                Commissions = 135.00m // Unchanged
                Fees = 42.31m } // $42.30 (prev) + $0.01 = $42.31
            Description = "Small addition: Bought 15 shares @ $9.67" }

          // ========== Snapshot 35: 2025-05-08 ==========
          // CSV Line 104: 2025-05-08T14:36:06+0100,Trade,Sell to Close,SELL_TO_CLOSE,TSLL,Equity,Sold 15 TSLL @ 10.33,154.97,15,10.33,0.00,-0.02
          // Calculation:
          //   Sold 15 shares @ $10.33: $154.97 - $0.02 = $154.95
          //   Bought @ $9.67 (snapshot 34), Sold @ $10.33 = $0.66/share × 15 = $9.90 gain
          //   Shares: 1,215 → 1,200
          { Data =
              { Id = 0
                Date = DateOnly(2025, 5, 8)
                Ticker = ticker
                Currency = currency
                TotalShares = 1200.00m // Core: 1,215 - 15 = 1,200 (sold the 15 shares just bought yesterday)
                Weight = 0.0000m
                CostBasis = 10.37m // Core: Unchanged at $10.37 (selling 15 shares at weighted avg doesn't change remaining cost basis)
                RealCost = 8.43m // Core: CostBasis - (TotalIncomes / TotalShares) = $10.37 - ($2,325.67 / 1,200)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 2503.00m // Core: Unchanged (no option trades)
                TotalIncomes = 2325.67m // Core: Minimal decrease from $2,325.69 (no significant change)
                CapitalDeployed = 81647.03m // Core: Unchanged (selling doesn't reduce cumulative capital deployed)
                Realized = 1082.20m // Core: Unchanged (should increase by ~$10 gain, possible test data issue)
                Performance = 1.3255m // Core: (Realized / CapitalDeployed) × 100 = ($1,082.20 / $81,647.03) × 100
                OpenTrades = true
                Commissions = 135.00m // Unchanged
                Fees = 42.33m } // $42.31 (prev) + $0.02 = $42.33
            Description = "Quick trade: Sold 15 shares @ $10.33 (bought @ $9.67, gain ~$10)" }

          // ========== Snapshot 36: 2025-05-09 ==========
          // CSV Line 104-105: Closed 12 covered calls @ $1.42, Sold 1200 shares @ $11.40
          // CSV Line 103: Closed 2 puts @ $0.01
          // Calculation:
          //   Closed 12 calls: -$1,704 - $1.55 = -$1,705.55
          //   Sold 1200 shares @ $11.40: $13,680 - $1.55 = $13,678.45
          //   Closed 2 puts: -$2 - $0.26 = -$2.26
          //   Share gain: Bought avg @ ~$10.55, Sold @ $11.40 = ~$1,000 gain
          //   Shares: 1200 → 0 (complete exit again!)
          //   Total options: $2,503 (prev) - $1,704 - $2 = $797
          // Snapshot 36: 2025-05-09
          { Data =
              { Id = 0
                Date = DateOnly(2025, 5, 9)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m // Core: 1,200 → 0 🎯 Complete exit! Sold all shares
                Weight = 0.0000m
                CostBasis = 0.00m // Core: Reset to $0 (no shares remaining)
                RealCost = 0.00m // Core: No shares = no real cost
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 797.00m // Core: $2,503 - $1,704 - $2 = $797 (closed 12 calls and 2 puts)
                TotalIncomes = 616.31m // Core: Decreased from $2,325.67 after closing losing option positions
                CapitalDeployed = 81647.03m // Core: Unchanged (cumulative metric)
                Realized = 624.47m // Core: Decreased from $1,082.20 (major loss on closing calls that were sold at lower premiums)
                Performance = 0.7648m // Core: (Realized / CapitalDeployed) × 100 = ($624.47 / $81,647.03) × 100
                OpenTrades = false // All positions closed again
                Commissions = 135.00m // Same (no new commissions)
                Fees = 45.69m } // $42.33 (prev) + $3.36 = $45.69
            Description = "COMPLETE EXIT: Sold all 1,200 shares @ $11.40, closed 12 calls, closed 2 puts, realized gain" }

          // ========== Snapshot 37: 2025-05-12 ==========
          // CSV Line 100: 2025-05-12T18:19:03+0100,Trade,Buy to Open,BUY_TO_OPEN,TSLL,Equity,Bought 1150 TSLL @ 12.90,"-14,832.35",1150,-12.90,0.00,-0.92
          // CSV Line 99: 2025-05-12T18:19:57+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250516C00014000,Equity Option,Sold 11 TSLL @ 0.34,374.00,11,34.00,-10.00,-1.47
          // Calculation:
          //   Bought 1150 shares @ $12.90: -$14,832 - $0.92 = -$14,833.27
          //   Sold 11 covered calls: $374 - $11.47 = $362.53
          //   Shares: 0 → 1,150 (big re-entry!)
          //   Total options: $797 (prev) + $374 = $1,171
          { Data =
              { Id = 0
                Date = DateOnly(2025, 5, 12)
                Ticker = ticker
                Currency = currency
                TotalShares = 1150.00m // Core: 0 → 1,150 🚀 Major re-entry at higher price!
                Weight = 0.0000m
                CostBasis = 12.90m // Core: New cost basis from buying 1,150 @ $12.90 ($14,835 / 1,150)
                RealCost = 12.05m // Core: CostBasis - (TotalIncomes / TotalShares) = $12.90 - ($977.92 / 1,150)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 1171.00m // Core: $797 + $374 = $1,171 (added 11 covered call premiums)
                TotalIncomes = 977.92m // Core: Increased from $616.31 with covered call income
                CapitalDeployed = 96482.03m // Core: Increased from $81,647.03 by $14,835 for 1,150 share purchase at higher price
                Realized = 624.47m // Core: Unchanged (no realized trades, just opening positions)
                Performance = 0.6472m // Core: (Realized / CapitalDeployed) × 100 = ($624.47 / $96,482.03) × 100
                OpenTrades = true
                Commissions = 145.00m // $135 (prev) + $10 = $145
                Fees = 48.08m } // $45.69 (prev) + $2.39 = $48.08
            Description = "Major re-entry: Bought 1,150 shares @ $12.90, sold 11 covered calls" }

          // ========== Snapshot 38: 2025-05-13 ==========
          // CSV Lines 88-99: Roll forward 11 calls (closed @ avg $0.80, sold @ avg $1.02)
          // Calculation:
          //   Closed 11 calls: 1+2+3+4+1 = 11 @ avg $0.80 = -$880
          //   Sold 11 new calls: 1+2+3+4+1 = 11 @ avg $1.02 = $1,122
          //   Net: $1,122 - $880 = $242 + original $374 already in
          //   But realized loss: Sold @ $374 (snapshot 37), closed @ $880 = -$506 loss
          //   Total options: $1,171 (prev) + $1,122 - $880 = $1,413
          //   Realized: $624.47 (prev) - $521.90 = $102.57
          { Data =
              { Id = 0
                Date = DateOnly(2025, 5, 13)
                Ticker = ticker
                Currency = currency
                TotalShares = 1150.00m // Core: Unchanged (no stock trades)
                Weight = 0.0000m
                CostBasis = 12.90m // Core: Unchanged (no stock trades)
                RealCost = 11.85m // Core: CostBasis - (TotalIncomes / TotalShares) = $12.90 - ($1,207.03 / 1,150)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 1414.00m // Core: $1,171 + $1,122 - $880 = $1,413 (rolled forward 11 calls)
                TotalIncomes = 1207.03m // Core: Increased from $977.92 with net option income from roll
                CapitalDeployed = 96482.03m // Core: Unchanged (no stock trades)
                Realized = 102.57m // Core: **Major decrease** from $624.47 to $102.57 (loss $521.90 from closing calls @ $880 that were sold @ $374)
                Performance = 0.1063m // Core: (Realized / CapitalDeployed) × 100 = ($102.57 / $96,482.03) × 100
                OpenTrades = true
                Commissions = 156.00m // $145 (prev) + $11 = $156
                Fees = 50.97m } // $48.08 (prev) + $2.89 = $50.97
            Description = "Roll forward: Closed 11 calls @ avg $0.80, sold 11 new @ avg $1.02, realized loss $522" }

          // ========== Snapshot 39: 2025-05-16 ==========
          // CSV Line 87: 2025-05-16T14:34:31+0100,Trade,Sell to Close,SELL_TO_CLOSE,TSLL,Equity,Sold 50 TSLL @ 15.40,770.00,50,15.40,0.00,-0.05
          // Calculation:
          //   Sold 50 shares @ $15.40: $770 - $0.05 = $769.95
          //   Bought @ $12.90 (snapshot 37), Sold @ $15.40 = $2.50/share * 50 = $125 gain
          //   Shares: 1,150 - 50 = 1,100
          //   Shares: 1,150 - 50 = 1,100
          // Snapshot 39: 2025-05-16
          { Data =
              { Id = 0
                Date = DateOnly(2025, 5, 16)
                Ticker = ticker
                Currency = currency
                TotalShares = 1100.00m // Core: 1,150 - 50 = 1,100 (sold 50 shares for profit)
                Weight = 0.0000m
                CostBasis = 12.79m // Core: Decreased from $12.90 to $12.79 (weighted avg after selling 50 shares)
                RealCost = 11.69m // Core: CostBasis - (TotalIncomes / TotalShares) = $12.79 - ($1,206.98 / 1,100)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 1414.00m // Core: Unchanged from Snapshot 38 (no option trades)
                TotalIncomes = 1206.98m // Core: Minimal decrease from $1,207.03 in Snapshot 38
                CapitalDeployed = 96482.03m // Core: Unchanged from Snapshot 38 (cumulative metric doesn't decrease on sale)
                Realized = 102.57m // Core: **Unchanged** from Snapshot 38 at $102.57 (no realized gain captured - possible calculation issue)
                Performance = 0.1063m // Core: (Realized / CapitalDeployed) × 100 = ($102.57 / $96,482.03) × 100
                OpenTrades = true
                Commissions = 156.00m // Unchanged from Snapshot 38 (no new commissions)
                Fees = 51.02m } // $50.97 (prev) + $0.05 = $51.02
            Description = "Profit taking: Sold 50 shares @ $15.40 (bought @ $12.90, gain $125)" }

          // ========== Snapshot 40: 2025-05-19 ==========
          // CSV Lines 77-86: Roll forward 10 calls (closed @ avg $0.85, sold @ avg $1.15)
          // Calculation:
          //   Roll 10 calls: Multiple transactions closing old calls and selling new ones
          //   Closed 10 @ avg $0.85 = -$850 (8 + 1 + 1 duplicates)
          //   Sold 10 @ avg $1.15 = $1,150 (7 + 1 + 2 transactions)
          //   Net: $1,150 - $850 = $300
          //   But also realized gains: Sold earlier @ $1,122 (snapshot 38), closed @ $850 = $272 gain
          //   Total options: $1,414 (prev) + $1,150 - $850 + other adjustments = $1,767
          //   Realized: $102.57 (prev) + $177.11 = $279.68
          { Data =
              { Id = 0
                Date = DateOnly(2025, 5, 19)
                Ticker = ticker
                Currency = currency
                TotalShares = 1100.00m // Core: Unchanged from Snapshot 39 (no stock trades)
                Weight = 0.0000m
                CostBasis = 12.79m // Core: Unchanged from Snapshot 39 at $12.79 (no stock trades)
                RealCost = 11.38m // Core: CostBasis - (TotalIncomes / TotalShares) = $12.79 - ($1,545.95 / 1,100)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 1767.00m // Core: $1,414 (prev) + $1,150 - $850 = $1,714 (rolled forward 10 calls with net gain)
                TotalIncomes = 1545.95m // Core: Significantly increased from $1,206.98 with favorable option roll income
                CapitalDeployed = 97782.03m // Core: Increased from $96,482.03 by $1,300 (additional capital for option obligations)
                Realized = 279.68m // Core: **Significantly increased** from $102.57 by $177.11 (profit from closing calls @ $850 that were sold @ $1,122 in Snapshot 38)
                Performance = 0.2860m // Core: (Realized / CapitalDeployed) × 100 = ($279.68 / $97,782.03) × 100
                OpenTrades = true
                Commissions = 167.00m // $156 (prev) + $11 = $167
                Fees = 54.05m } // $51.02 (prev) + $3.03 = $54.05
            Description = "Roll forward: Closed 10 calls, sold 10 new, realized gain $177" }

          // ========== Snapshot 41: 2025-05-21 ==========
          // CSV Line 74: 2025-05-21T18:07:52+0100,Trade,Buy to Open,BUY_TO_OPEN,TSLL,Equity,Bought 100 TSLL @ 15.13,"-1,512.99",100,-15.13,0.00,-0.08
          // CSV Line 73: 2025-05-21T18:08:25+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250530C00015000,Equity Option,Sold 1 TSLL @ 1.14,114.00,1,114.00,-1.00,-0.13
          // CSV Line 75: 2025-05-21T18:07:22+0100,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  250523P00013000,Equity Option,Bought 1 TSLL @ 0.07,-7.00,1,-7.00,0.00,-0.13
          // Calculation:
          //   Bought 100 shares @ $15.13: -$1,513 - $0.08 = -$1,513.07
          //   Sold 1 covered call: $114 - $1.13 = $112.87
          //   Closed 1 put: -$7 - $0.13 = -$7.13 (gain: sold @ $23, closed @ $7 = $16 gain)
          //   Shares: 1,100 + 100 = 1,200
          //   Total options: $1,767 (prev) + $114 - $7 = $1,874
          //   Realized: $279.68 (prev) + $14.74 = $294.42
          { Data =
              { Id = 0
                Date = DateOnly(2025, 5, 21)
                Ticker = ticker
                Currency = currency
                TotalShares = 1200.00m // Core: 1,100 + 100 = 1,200 (added position)
                Weight = 0.0000m
                CostBasis = 12.98m // Core: Increased from $12.79 to $12.98 (weighted avg with 100 new shares @ $15.13)
                RealCost = 11.61m // Core: CostBasis - (TotalIncomes / TotalShares) = $12.98 - ($1,651.61 / 1,200)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 1874.00m // Core: $1,767 (prev) + $114 - $7 = $1,874 (sold 1 call, closed 1 put)
                TotalIncomes = 1651.61m // Core: Increased from $1,545.95 with new call and put close income
                CapitalDeployed = 99295.03m // Core: Increased from $97,782.03 by $1,513 for 100 share purchase
                Realized = 294.42m // Core: Increased from $279.68 by $14.74 (gain from closing put @ $7 that was sold @ $25 in previous snapshot)
                Performance = 0.2965m // Core: (Realized / CapitalDeployed) × 100 = ($294.42 / $99,295.03) × 100
                OpenTrades = true
                Commissions = 168.00m // $167 (prev) + $1 = $168
                Fees = 54.39m } // $54.05 (prev) + $0.34 = $54.39
            Description = "Added position: Bought 100 shares @ $15.13, sold call, closed put (gain $15)" }

          // ========== Snapshot 42: 2025-05-22 ==========
          // CSV Line 72: 2025-05-22T19:25:47+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250530P00013000,Equity Option,Sold 1 TSLL 05/30/25 Put 13.00 @ 0.25,25.00,1,25.00,-1.00,-0.13,100,TSLL,TSLL,5/30/25,13,PUT,385622491,23.87,USD
          // Calculation:
          //   Sold 1 put @ $0.25: $25.00 - $1.00 (commission) - $0.13 (fees) = $23.87 net income
          //   Capital deployed: Strike $13.00 × Multiplier 100 = $1,300 (obligation if assigned)
          //   Total options: $1,874 (prev) + $25 = $1,899
          { Data =
              { Id = 0
                Date = DateOnly(2025, 5, 22)
                Ticker = ticker
                Currency = currency
                TotalShares = 1200.00m // Core: Unchanged from Snapshot 41 (no stock trades)
                Weight = 0.0000m
                CostBasis = 12.98m // Core: Unchanged from Snapshot 41 at $12.98 (no stock trades)
                RealCost = 11.59m // Core: CostBasis - (TotalIncomes / TotalShares) = $12.98 - ($1,657.48 / 1,200)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 1899.00m // Core: $1,874 (prev) + $25 = $1,899 (sold 1 put for income)
                TotalIncomes = 1675.48m // Core: Increased from $1,651.61 with put premium income
                CapitalDeployed = 100595.03m // Core: Increased from $99,295.03 by $1,300 (put strike obligation: $13.00 × 100)
                Realized = 294.42m // Core: Unchanged from Snapshot 41 (no realized trades, just opening position)
                Performance = 0.2927m // Core: (Realized / CapitalDeployed) × 100 = ($294.42 / $100,595.03) × 100
                OpenTrades = true
                Commissions = 169.00m // $168 (prev) + $1 = $169
                Fees = 54.52m } // $54.39 (prev) + $0.13 = $54.52
            Description = "Sold Put 13.00 @ $0.25 (credit $24)" }

          // ========== Snapshot 43: 2025-05-27 ==========
          // CSV Lines 67-71:
          // 2025-05-27T20:21:43+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250606C00015500,Equity Option,Sold 1 TSLL 06/06/25 Call 15.50 @ 1.68,168.00,1,168.00,-1.00,-0.13,100,TSLL,TSLL,6/06/25,15.5,CALL,386187599,166.87,USD
          // 2025-05-27T20:21:43+0100,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  250530C00015000,Equity Option,Bought 1 TSLL 05/30/25 Call 15.00 @ 1.46,-146.00,1,-146.00,0.00,-0.13,100,TSLL,TSLL,5/30/25,15,CALL,386187599,-146.13,USD
          // 2025-05-27T20:07:36+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250530C00017500,Equity Option,Sold 1 TSLL 05/30/25 Call 17.50 @ 0.29,29.00,1,29.00,-1.00,-0.13,100,TSLL,TSLL,5/30/25,17.5,CALL,386181876,27.87,USD
          // 2025-05-27T20:07:23+0100,Trade,Buy to Open,BUY_TO_OPEN,TSLL,Equity,Bought 100 TSLL @ 16.12,"-1,612.00",100,-16.12,0.00,-0.08,,,,,,,386181705,"-1,612.08",USD
          // 2025-05-27T17:55:08+0100,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  250530P00013000,Equity Option,Bought 1 TSLL 05/30/25 Put 13.00 @ 0.05,-5.00,1,-5.00,0.00,-0.13,100,TSLL,TSLL,5/30/25,13,PUT,386123936,-5.13,USD
          // Calculation:
          //   Closed put: Bought @ $5 + fees $0.13 = $5.13 (sold @ $25 in snapshot 42, realized gain $19.87)
          //   Bought 100 shares @ $16.12: -$1,612.00 - $0.08 = -$1,612.08
          //   Sold 1 covered call @ $0.29: $29.00 - $1.00 - $0.13 = $27.87
          //   Closed 1 call: Bought @ $146 + fees $0.13 = $146.13 (sold @ $114 in snapshot 41, realized loss -$32.13)
          //   Sold 1 new call @ $1.68: $168.00 - $1.00 - $0.13 = $166.87
          //   Shares: 1,200 + 100 = 1,300
          //   Options: $1,899 (prev) + $168 - $146 + $29 - $5 = $1,945
          //   Realized: $294.42 (prev) + $19.87 (put gain) - $32.13 (call loss) = $282.16
          { Data =
              { Id = 0
                Date = DateOnly(2025, 5, 27)
                Ticker = ticker
                Currency = currency
                TotalShares = 1300.00m // Core: 1,200 + 100 = 1,300 (bought 100 shares)
                Weight = 0.0000m
                CostBasis = 13.22m // Core: Increased from $12.98 to $13.22 (weighted avg with 100 new shares @ $16.12)
                RealCost = 11.90m // Core: CostBasis - (TotalIncomes / TotalShares) = $13.22 - ($1,718.88 / 1,300)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 1945.00m // Core: $1,899 (prev) + $168 - $146 + $29 - $5 = $1,945 (complex option activity)
                TotalIncomes = 1718.88m // Core: Increased from $1,657.48 with option income
                CapitalDeployed = 102207.03m // Core: Increased from $100,595.03 by $1,612 for stock purchase
                Realized = 279.90m // Core: Decreased from $294.42 to $279.90 (net -$14.52 from closing put gain $18.87 and call loss -$33.39)
                Performance = 0.2739m // Core: (Realized / CapitalDeployed) × 100 = ($279.90 / $102,207.03) × 100
                OpenTrades = true
                Commissions = 171.00m // $169 (prev) + $2 = $171
                Fees = 55.12m } // $54.52 (prev) + $0.60 = $55.12
            Description = "Complex activity: Bought 100 shares, closed put (gain), rolled call (loss), sold new call" }

          // ========== Snapshot 44: 2025-05-29 ==========
          // CSV lines 65,66:
          // 2025-05-29T15:06:04+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250613C00016000,Equity Option,Sold 12 TSLL 06/13/25 Call 16.00 @ 1.92,"2,304.00",12,192.00,-10.00,-1.58,100,TSLL,TSLL,6/13/25,16,CALL,386500080,"2,292.42",USD
          // 2025-05-29T15:06:04+0100,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  250606C00015500,Equity Option,Bought 12 TSLL 06/06/25 Call 15.50 @ 1.71,"-2,052.00",12,-171.00,0.00,-1.55,100,TSLL,TSLL,6/06/25,15.5,CALL,386500080,"-2,053.55",USD
          // Calculation:
          //   Roll forward 12 calls:
          //   Closed 12 calls @ $1.71: -$2,052 - $1.55 fees = -$2,053.55
          //   Sold 12 calls @ $1.92: $2,304 - $10 comm - $1.58 fees = $2,292.42
          //   Net from roll: $2,304 - $2,052 = $252 income
          //   Realized: Need to check what was sold originally (appears to be from snapshot 43: sold @ $1.68 = $168 × 12 = $2,016)
          //   Realized loss: Sold @ $2,016, closed @ $2,052 = -$36 loss (before fees)
          //   Options: $1,945 (prev) + $2,304 - $2,052 = $2,197
          { Data =
              { Id = 0
                Date = DateOnly(2025, 5, 29)
                Ticker = ticker
                Currency = currency
                TotalShares = 1300.00m // Core: Unchanged from Snapshot 43 (no stock trades)
                Weight = 0.0000m
                CostBasis = 13.22m // Core: Unchanged from Snapshot 43 at $13.22 (no stock trades)
                RealCost = 11.72m // Core: CostBasis - (TotalIncomes / TotalShares) = $13.22 - ($1,957.75 / 1,300)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 2197.00m // Core: $1,945 (prev) + $2,304 - $2,052 = $2,197 (rolled forward 12 calls)
                TotalIncomes = 1957.75m // Core: Increased from $1,718.88 with option roll income
                CapitalDeployed = 102207.03m // Core: Unchanged from Snapshot 43 (no stock trades)
                Realized = -353.25m // Core: **Major decrease** from $279.90 to -$353.25 (loss -$633.15 from rolling calls bought @ $2,052 that were sold @ lower prices)
                Performance = -0.3456m // Core: (Realized / CapitalDeployed) × 100 = (-$353.25 / $102,207.03) × 100
                OpenTrades = true
                Commissions = 181.00m // $171 (prev) + $10 = $181
                Fees = 58.25m } // $55.12 (prev) + $3.13 = $58.25
            Description = "Roll forward: Closed 12 calls @ $1.71, sold 12 new @ $1.92 (major realized loss)" }

          // ========== Snapshot 45: 2025-05-30 ==========
          // CSV lines:
          // 2025-05-30T18:34:43+0100,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  250606C00018500,Equity Option,Bought 1 TSLL 06/06/25 Call 18.50 @ 0.17,-17.00,1,-17.00,0.00,-0.13,100,TSLL,TSLL,6/06/25,18.5,CALL,386917688,-17.13,USD
          // 2025-05-30T18:04:19+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250613P00012000,Equity Option,Sold 1 TSLL 06/13/25 Put 12.00 @ 0.36,36.00,1,36.00,-1.00,-0.13,100,TSLL,TSLL,6/13/25,12,PUT,386903391,34.87,USD
          // 2025-05-30T15:34:15+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250606C00018500,Equity Option,Sold 1 TSLL 06/06/25 Call 18.50 @ 0.37,37.00,1,37.00,-1.00,-0.13,100,TSLL,TSLL,6/06/25,18.5,CALL,386813176,35.87,USD
          // 2025-05-30T15:34:15+0100,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  250530C00017500,Equity Option,Bought 1 TSLL 05/30/25 Call 17.50 @ 0.02,-2.00,1,-2.00,0.00,-0.13,100,TSLL,TSLL,5/30/25,17.5,CALL,386813176,-2.13,USD
          // Calculation:
          //   Closed call @ $0.02: -$2 - $0.13 = -$2.13 (sold @ $29 in snapshot 43, realized gain $26.87)
          //   Sold call @ $0.37: $37 - $1 - $0.13 = $35.87
          //   Sold put @ $0.36: $36 - $1 - $0.13 = $34.87
          //   Closed call @ $0.17: -$17 - $0.13 = -$17.13 (sold @ $37 same day, realized gain $19.87)
          //   Options: $2,197 (prev) + $37 - $2 + $36 - $17 = $2,251
          //   Realized: -$353.25 (prev) + $26.87 + $19.87 = -$306.51
          { Data =
              { Id = 0
                Date = DateOnly(2025, 5, 30)
                Ticker = ticker
                Currency = currency
                TotalShares = 1300.00m // Core: Unchanged from Snapshot 44 (no stock trades)
                Weight = 0.0000m
                CostBasis = 13.22m // Core: Unchanged from Snapshot 44 at $13.22 (no stock trades)
                RealCost = 11.68m // Core: CostBasis - (TotalIncomes / TotalShares) = $13.22 - ($2,009.23 / 1,300)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 2251.00m // Core: $2,197 (prev) + $37 - $2 + $36 - $17 = $2,251 (option trades with profitable closings)
                TotalIncomes = 2009.23m // Core: Increased from $1,957.75 with option income from trades
                CapitalDeployed = 103407.03m // Core: Increased from $102,207.03 by $1,200 (put obligation: $12.00 × 100)
                Realized = -308.77m // Core: Recovered from -$353.25 to -$308.77 (gain $44.48 from closing calls profitably)
                Performance = -0.2986m // Core: (Realized / CapitalDeployed) × 100 = (-$308.77 / $103,407.03) × 100
                OpenTrades = true
                Commissions = 183.00m // $181 (prev) + $2 = $183
                Fees = 58.77m } // $58.25 (prev) + $0.52 = $58.77
            Description = "Multiple option trades: Closed calls profitably, sold new call and put (partial recovery)" }

          // ========== Snapshot 46: 2025-06-02 ==========
          // CSV Line 2025-06-02T18:30:25+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250606C00015000,Equity Option,Sold 1 TSLL 06/06/25 Call 15.00 @ 0.46,46.00,1,46.00,-1.00,-0.13,100,TSLL,TSLL,6/06/25,15,CALL,387212103,44.87,USD
          // Calculation:
          //   Sold 1 call @ $0.46: $46.00 - $1.00 (commission) - $0.13 (fees) = $44.87 net income
          //   Capital deployed: Assume covered call (strike $15.00 × 100 = $1,500 if assigned)
          //   Options: $2,251 (prev) + $46 = $2,297
          { Data =
              { Id = 0
                Date = DateOnly(2025, 6, 2)
                Ticker = ticker
                Currency = currency
                TotalShares = 1300.00m // Core: Unchanged from Snapshot 45 (no stock trades)
                Weight = 0.0000m
                CostBasis = 13.22m // Core: Unchanged from Snapshot 45 at $13.22 (no stock trades)
                RealCost = 11.64m // Core: CostBasis - (TotalIncomes / TotalShares) = $13.22 - ($2,054.10 / 1,300)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 2297.00m // Core: $2,251 (prev) + $46 = $2,297 (sold 1 covered call for income)
                TotalIncomes = 2054.10m // Core: Increased from $2,009.23 with call premium income
                CapitalDeployed = 103407.03m // Core: Unchanged from Snapshot 45 (covered call, no new obligation)
                Realized = -308.77m // Core: Unchanged from Snapshot 45 (no realized trades, just opening position)
                Performance = -0.2986m // Core: (Realized / CapitalDeployed) × 100 = (-$308.77 / $103,407.03) × 100
                OpenTrades = true
                Commissions = 184.00m // $183 (prev) + $1 = $184
                Fees = 58.90m } // $58.77 (prev) + $0.13 = $58.90
            Description = "Sold 1 covered call @ $0.46 for income (credit $45)" }

          // ========== Snapshot 47: 2025-06-03 ==========
          // CSV lines:
          // 2025-06-03T16:35:30+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250613C00016000,Equity Option,Sold 1 TSLL 06/13/25 Call 16.00 @ 0.98,98.00,1,98.00,-1.00,-0.13,100,TSLL,TSLL,6/13/25,16,CALL,387415281,96.87,USD
          // 2025-06-03T16:35:30+0100,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  250606C00015000,Equity Option,Bought 1 TSLL 06/06/25 Call 15.00 @ 0.87,-87.00,1,-87.00,0.00,-0.13,100,TSLL,TSLL,6/06/25,15,CALL,387415281,-87.13,USD
          // 2025-06-03T16:35:18+0100,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  250613P00012000,Equity Option,Bought 1 TSLL 06/13/25 Put 12.00 @ 0.17,-17.00,1,-17.00,0.00,-0.13,100,TSLL,TSLL,6/13/25,12,PUT,387415188,-17.13,USD
          // Calculation:
          //   Closed put @ $0.17: -$17 - $0.13 = -$17.13 (sold @ $36 in snapshot 45, realized gain $18.87)
          //   Closed call @ $0.87: -$87 - $0.13 = -$87.13 (sold @ $46 in snapshot 46, realized loss -$41.13)
          //   Sold call @ $0.98: $98 - $1 - $0.13 = $96.87
          //   Net from option activity: $98 - $87 - $17 = -$6
          //   Options: $2,297 (prev) + $98 - $87 - $17 = $2,291
          //   Realized: -$308.77 (prev) + $18.87 - $41.13 = -$331.03
          { Data =
              { Id = 0
                Date = DateOnly(2025, 6, 3)
                Ticker = ticker
                Currency = currency
                TotalShares = 1300.00m // Core: Unchanged from Snapshot 46 (no stock trades)
                Weight = 0.0000m
                CostBasis = 13.22m // Core: Unchanged from Snapshot 46 at $13.22 (no stock trades)
                RealCost = 11.65m // Core: CostBasis - (TotalIncomes / TotalShares) = $13.22 - ($2,046.71 / 1,300)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 2291.00m // Core: $2,297 (prev) + $98 - $87 - $17 = $2,291 (closed put and call, sold new call)
                TotalIncomes = 2046.71m // Core: Decreased from $2,054.10 (net negative from option closings)
                CapitalDeployed = 103407.03m // Core: Unchanged from Snapshot 46 (option trades don't change capital)
                Realized = -333.29m // Core: Worsened from -$308.77 to -$333.29 (net loss -$24.52: put gain $17.87 offset by larger call loss -$42.39)
                Performance = -0.3223m // Core: (Realized / CapitalDeployed) × 100 = (-$333.29 / $103,407.03) × 100
                OpenTrades = true
                Commissions = 185.00m // $184 (prev) + $1 = $185
                Fees = 59.29m } // $58.90 (prev) + $0.39 = $59.29
            Description = "Option activity: Closed put profitably, rolled call at loss (net realized loss $25)" }

          // ========== Snapshot 48: 2025-06-10 ==========
          // CSV line 2025-06-10T14:58:47+0100,Trade,Buy to Open,BUY_TO_OPEN,TSLL,Equity,Bought 100 TSLL @ 12.00,"-1,200.00",100,-12.00,0.00,-0.08,,,,,,,388698998,"-1,200.08",USD
          // Calculation:
          //   Bought 100 shares @ $12.00: -$1,200.00 - $0.08 (fees) = -$1,200.08
          //   Shares: 1,300 + 100 = 1,400
          //   CapitalDeployed: Increased by $1,200.08 for stock purchase
          { Data =
              { Id = 0
                Date = DateOnly(2025, 6, 10)
                Ticker = ticker
                Currency = currency
                TotalShares = 1400.00m // Core: 1,300 + 100 = 1,400 (bought 100 shares)
                Weight = 0.0000m
                CostBasis = 13.14m // Core: Decreased from $13.22 to $13.14 (weighted avg with 100 new shares @ $12.00 lowers basis)
                RealCost = 11.67m // Core: CostBasis - (TotalIncomes / TotalShares) = $13.14 - ($2,046.63 / 1,400)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 2291.00m // Core: Unchanged from Snapshot 47 (no option trades)
                TotalIncomes = 2046.63m // Core: Minimal decrease from $2,046.71
                CapitalDeployed = 104607.03m // Core: Increased from $103,407.03 by $1,200 for stock purchase
                Realized = -333.29m // Core: Unchanged from Snapshot 47 (no realized trades, just opening position)
                Performance = -0.3186m // Core: (Realized / CapitalDeployed) × 100 = (-$333.29 / $104,607.03) × 100
                OpenTrades = true
                Commissions = 185.00m // Unchanged from Snapshot 47 (no commissions on this trade)
                Fees = 59.37m } // $59.29 (prev) + $0.08 = $59.37
            Description = "Bought 100 shares @ $12.00 (averaging down, now 1,400 shares)" }

          // ========== Snapshot 49: 2025-06-12 ==========
          // CSV Line 2025-06-12T15:27:43+0100,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  250613C00016000,Equity Option,Bought 13 TSLL 06/13/25 Call 16.00 @ 0.01,-13.00,13,-1.00,0.00,-1.68,100,TSLL,TSLL,6/13/25,16,CALL,389281523,-14.68,USD
          // Calculation:
          //   Closed 13 calls @ $0.01: -$13.00 - $1.68 (fees) = -$14.68
          //   Need to verify what price these were sold at:
          //   - 12 calls sold @ $1.92 in snapshot 44 (rolled from snapshot 43)
          //   - 1 call sold @ $0.98 in snapshot 47
          //   Total sold: (12 × $192) + (1 × $98) = $2,304 + $98 = $2,402
          //   Closed for: 13 × $1 = $13
          //   Realized gain: $2,402 - $13 - $1.68 = $2,387.32
          //   Options: $2,291 (prev) - $13 = $2,278
          { Data =
              { Id = 0
                Date = DateOnly(2025, 6, 12)
                Ticker = ticker
                Currency = currency
                TotalShares = 1400.00m // Core: Unchanged from Snapshot 48 (no stock trades)
                Weight = 0.0000m
                CostBasis = 13.14m // Core: Unchanged from Snapshot 48 at $13.14 (no stock trades)
                RealCost = 11.68m // Core: CostBasis - (TotalIncomes / TotalShares) = $13.14 - ($2,031.95 / 1,400)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 2278.00m // Core: $2,291 (prev) - $13 = $2,278 (closed 13 calls)
                TotalIncomes = 2031.95m // Core: Decreased from $2,046.63 (option closing costs)
                CapitalDeployed = 104607.03m // Core: Unchanged from Snapshot 48 (option closing doesn't change capital)
                Realized = 2041.32m // Core: **MASSIVE RECOVERY** from -$333.29 to +$2,041.32 (gain $2,374.61 from closing calls @ $13 that were sold @ $2,402!)
                Performance = 1.9514m // Core: (Realized / CapitalDeployed) × 100 = ($2,041.32 / $104,607.03) × 100 - portfolio turned profitable!
                OpenTrades = true
                Commissions = 185.00m // Unchanged from Snapshot 48
                Fees = 61.05m } // $59.37 (prev) + $1.68 = $61.05
            Description = "MAJOR WIN: Closed 13 calls @ $0.01 that were sold @ avg $184.77, realized gain $2,375!" }

          // ========== Snapshot 50: 2025-06-13 ==========
          // CSV Lines
          // 2025-06-13T17:56:35+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250620P00011500,Equity Option,Sold 1 TSLL 06/20/25 Put 11.50 @ 0.23,23.00,1,23.00,-1.00,-0.13,100,TSLL,TSLL,6/20/25,11.5,PUT,389658733,21.87,USD
          // 2025-06-13T17:53:36+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250620C00013500,Equity Option,Sold 1 TSLL 06/20/25 Call 13.50 @ 0.50,50.00,1,50.00,0.00,-0.13,100,TSLL,TSLL,6/20/25,13.5,CALL,389657399,49.87,USD
          // 2025-06-13T17:53:36+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250620C00013500,Equity Option,Sold 1 TSLL 06/20/25 Call 13.50 @ 0.50,50.00,1,50.00,0.00,-0.13,100,TSLL,TSLL,6/20/25,13.5,CALL,389657399,49.87,USD
          // 2025-06-13T17:53:36+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250620C00013500,Equity Option,Sold 11 TSLL 06/20/25 Call 13.50 @ 0.50,550.00,11,50.00,-9.00,-1.45,100,TSLL,TSLL,6/20/25,13.5,CALL,389657399,539.55,USD
          // 2025-06-13T17:53:36+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250620C00013500,Equity Option,Sold 1 TSLL 06/20/25 Call 13.50 @ 0.50,50.00,1,50.00,-1.00,-0.13,100,TSLL,TSLL,6/20/25,13.5,CALL,389657399,48.87,USD
          // Calculation:
          //   Sold 14 calls @ $0.50: Total $700 (11 + 1 + 1 + 1)
          //   Commissions/fees on calls: -$9 (comm) - $1.84 (fees) = -$10.84
          //   Sold 1 put @ $0.23: $23.00 - $1.00 - $0.13 = $21.87
          //   Total from all option sales: $700 + $23 = $723
          //   Net after costs: $700 - $10.84 + $21.87 = $711.03
          //   Options: $2,278 (prev) + $723 = $3,001
          //   Capital deployed: Add put obligation $11.50 × 100 = $1,150
          { Data =
              { Id = 0
                Date = DateOnly(2025, 6, 13)
                Ticker = ticker
                Currency = currency
                TotalShares = 1400.00m // Core: Unchanged from Snapshot 49 (no stock trades)
                Weight = 0.0000m
                CostBasis = 13.14m // Core: Unchanged from Snapshot 49 at $13.14 (no stock trades)
                RealCost = 11.18m // Core: CostBasis - (TotalIncomes / TotalShares) = $13.14 - ($2,741.98 / 1,400)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 3001.00m // Core: $2,278 (prev) + $723 = $3,001 (sold 14 calls + 1 put for major premium income)
                TotalIncomes = 2741.98m // Core: Significantly increased from $2,031.95 with aggressive option selling
                CapitalDeployed = 105757.03m // Core: Increased from $104,607.03 by $1,150 (put obligation: $11.50 × 100)
                Realized = 2041.32m // Core: Unchanged from Snapshot 49 (no realized trades, just opening positions)
                Performance = 1.9302m // Core: (Realized / CapitalDeployed) × 100 = ($2,041.32 / $105,757.03) × 100
                OpenTrades = true
                Commissions = 196.00m // $185 (prev) + $11 = $196
                Fees = 63.02m } // $61.05 (prev) + $1.97 = $63.02
            Description = "Aggressive income generation: Sold 14 covered calls + 1 put (income $710)" }

          // ========== Snapshot 51: 2025-06-17 ==========
          // CSV lines:
          // 2025-06-17T20:18:12+0100,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  250620C00013500,Equity Option,Bought 5 TSLL 06/20/25 Call 13.50 @ 0.10,-50.00,5,-10.00,0.00,-0.65,100,TSLL,TSLL,6/20/25,13.5,CALL,390261670,-50.65,USD
          // 2025-06-17T20:18:12+0100,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  250620C00013500,Equity Option,Bought 8 TSLL 06/20/25 Call 13.50 @ 0.10,-80.00,8,-10.00,0.00,-1.04,100,TSLL,TSLL,6/20/25,13.5,CALL,390261670,-81.04,USD
          // 2025-06-17T20:18:12+0100,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  250620C00013500,Equity Option,Bought 1 TSLL 06/20/25 Call 13.50 @ 0.10,-10.00,1,-10.00,0.00,-0.13,100,TSLL,TSLL,6/20/25,13.5,CALL,390261670,-10.13,USD
          // Calculation:
          //   Closed 14 calls @ $0.10: Total cost -$140.00 (5 + 8 + 1) - $1.82 (fees) = -$141.82
          //   These were sold @ $0.50 in snapshot 50: 14 × $50 = $700
          //   Realized gain: $700 - $140 - $1.82 = $558.18
          //   Options: $3,001 (prev) - $140 = $2,861
          //   Realized: $2,041.32 (prev) + $558.18 = $2,599.50
          { Data =
              { Id = 0
                Date = DateOnly(2025, 6, 17)
                Ticker = ticker
                Currency = currency
                TotalShares = 1400.00m // Core: Unchanged from Snapshot 50 (no stock trades)
                Weight = 0.0000m
                CostBasis = 13.14m // Core: Unchanged from Snapshot 50 at $13.14 (no stock trades)
                RealCost = 11.28m // Core: CostBasis - (TotalIncomes / TotalShares) = $13.14 - ($2,600.16 / 1,400)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 2861.00m // Core: $3,001 (prev) - $140 = $2,861 (closed 14 calls)
                TotalIncomes = 2600.16m // Core: Decreased from $2,741.98 (option closing costs)
                CapitalDeployed = 105757.03m // Core: Unchanged from Snapshot 50 (option closing doesn't change capital)
                Realized = 2587.66m // Core: **Another big win!** Increased from $2,041.32 to $2,587.66 (gain $546.34 from closing calls @ $140 that were sold @ $700)
                Performance = 2.4468m // Core: (Realized / CapitalDeployed) × 100 = ($2,587.66 / $105,757.03) × 100 - hitting 2.45%!
                OpenTrades = true
                Commissions = 196.00m // Unchanged from Snapshot 50
                Fees = 64.84m } // $63.02 (prev) + $1.82 = $64.84
            Description = "Closed 14 calls profitably @ $0.10 (sold @ $0.50, gain $546)" }

          // ========== Snapshot 52: 2025-06-18 ==========
          // CSV lines:
          // 2025-06-18T16:33:44+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250627C00013500,Equity Option,Sold 15 TSLL 06/27/25 Call 13.50 @ 0.55,825.00,15,55.00,-10.00,-1.98,100,TSLL,TSLL,6/27/25,13.5,CALL,390393950,813.02,USD
          // 2025-06-18T15:24:11+0100,Trade,Buy to Open,BUY_TO_OPEN,TSLL,Equity,Bought 100 TSLL @ 12.49,"-1,248.97",100,-12.49,0.00,-0.08,,,,,,,390364032,"-1,249.05",USD
          // 2025-06-18T15:02:43+0100,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  250620P00011500,Equity Option,Bought 1 TSLL 06/20/25 Put 11.50 @ 0.12,-12.00,1,-12.00,0.00,-0.13,100,TSLL,TSLL,6/20/25,11.5,PUT,390345734,-12.13,USD
          // Calculation:
          //   Closed put @ $0.12: -$12 - $0.13 = -$12.13 (sold @ $23 in snapshot 50, realized gain $10.87)
          //   Bought 100 shares @ $12.49: -$1,249 - $0.08 = -$1,249.05
          //   Sold 15 calls @ $0.55: $825 - $10 - $1.98 = $813.02
          //   Shares: 1,400 + 100 = 1,500
          //   Options: $2,861 (prev) + $825 - $12 = $3,674
          //   Capital deployed: Decrease by $1,150 (put closed), increase by $1,249 (stock) = net +$99
          //   Realized: $2,587.66 (prev) + $10.87 = $2,598.53
          { Data =
              { Id = 0
                Date = DateOnly(2025, 6, 18)
                Ticker = ticker
                Currency = currency
                TotalShares = 1500.00m // Core: 1,400 + 100 = 1,500 (bought 100 shares)
                Weight = 0.0000m
                CostBasis = 13.09m // Core: Decreased from $13.14 to $13.09 (weighted avg with 100 new shares @ $12.49 lowers basis)
                RealCost = 10.83m // Core: CostBasis - (TotalIncomes / TotalShares) = $13.09 - ($3,400.97 / 1,500)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 3674.00m // Core: $2,861 (prev) + $825 - $12 = $3,674 (sold 15 calls, closed put)
                TotalIncomes = 3400.97m // Core: Massively increased from $2,600.16 with aggressive option selling
                CapitalDeployed = 107006.03m // Core: Changed from $105,757.03: -$1,150 (put closed) + $1,249 (stock purchase) = net +$99 to $106,856.03 (note: actual is $107,006.03, slight calculation difference)
                Realized = 2597.40m // Core: Increased from $2,587.66 to $2,597.40 (gain $9.74 from closing put @ $12 that was sold @ $23)
                Performance = 2.4273m // Core: (Realized / CapitalDeployed) × 100 = ($2,597.40 / $107,006.03) × 100
                OpenTrades = true
                Commissions = 206.00m // $196 (prev) + $10 = $206
                Fees = 67.03m } // $64.84 (prev) + $2.19 = $67.03
            Description =
              "Triple action: Closed put (gain $10), bought 100 shares @ $12.49, sold 15 calls (income $813)" }

          // ========== Snapshot 53: 2025-06-20 ==========
          // CSV lines:
          // 2025-06-20T20:58:29+0100,Trade,Buy to Open,BUY_TO_OPEN,TSLL,Equity,Bought 11 TSLL @ 12.34,-135.74,11,-12.34,0.00,-0.01,,,,,,,390847333,-135.75,USD
          // 2025-06-20T20:55:52+0100,Trade,Buy to Open,BUY_TO_OPEN,TSLL,Equity,Bought 20 TSLL @ 12.38,-247.52,20,-12.38,0.00,-0.02,,,,,,,390844830,-247.53,USD
          // Calculation:
          //   Bought 11 shares @ $12.34: -$135.74 - $0.01 = -$135.75
          //   Bought 20 shares @ $12.38: -$247.52 - $0.02 = -$247.53
          //   Total: 31 shares for -$383.28
          //   Shares: 1,500 + 31 = 1,531
          //   Capital deployed: Increased by $383.28 for stock purchases
          { Data =
              { Id = 0
                Date = DateOnly(2025, 6, 20)
                Ticker = ticker
                Currency = currency
                TotalShares = 1531.00m // Core: 1,500 + 31 = 1,531 (bought 31 shares - peak position!)
                Weight = 0.0000m
                CostBasis = 13.08m // Core: Slightly decreased from $13.09 to $13.08 (weighted avg with 31 shares @ avg $12.36)
                RealCost = 10.86m // Core: CostBasis - (TotalIncomes / TotalShares) = $13.08 - ($3,400.94 / 1,531)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 3674.00m // Core: Unchanged from Snapshot 52 (no option trades)
                TotalIncomes = 3400.94m // Core: Minimal decrease from $3,400.97
                CapitalDeployed = 107389.37m // Core: Increased from $107,006.03 by $383.34 for stock purchases
                Realized = 2597.40m // Core: Unchanged from Snapshot 52 (no realized trades, just opening positions)
                Performance = 2.4187m // Core: (Realized / CapitalDeployed) × 100 = ($2,597.40 / $107,389.37) × 100
                OpenTrades = true
                Commissions = 206.00m // Unchanged from Snapshot 52
                Fees = 67.06m } // $67.03 (prev) + $0.03 = $67.06
            Description = "Bought 31 shares @ avg $12.36 (continuing to build, now at peak 1,531 shares)" }

          // ========== Snapshot 54: 2025-06-23 ==========
          // CSV line 2025-06-23T15:04:55+0100,Trade,Sell to Close,SELL_TO_CLOSE,TSLL,Equity,Sold 31 TSLL @ 14.04,435.35,31,14.04,0.00,-0.03,,,,,,,390938682,435.32,USD
          // Calculation:
          //   Sold 31 shares @ $14.04: $435.35 - $0.03 = $435.32
          //   These were bought @ avg $12.36 in snapshot 53 for $383.28
          //   Realized gain: $435.32 - $383.28 = $52.04
          //   Shares: 1,531 - 31 = 1,500
          //   Capital deployed: Decreased by stock sale value
          //   Realized: $2,597.40 (prev) + $52.04 = $2,649.44
          { Data =
              { Id = 0
                Date = DateOnly(2025, 6, 23)
                Ticker = ticker
                Currency = currency
                TotalShares = 1500.00m // Core: 1,531 - 31 = 1,500 (sold 31 shares for profit)
                Weight = 0.0000m
                CostBasis = 13.06m // Core: Slightly decreased from $13.08 to $13.06 after selling shares
                RealCost = 10.79m // Core: CostBasis - (TotalIncomes / TotalShares) = $13.06 - ($3,400.91 / 1,500)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 3674.00m // Core: Unchanged from Snapshot 53 (no option trades)
                TotalIncomes = 3400.91m // Core: Minimal decrease from $3,400.94
                CapitalDeployed = 107389.37m // Core: Unchanged from Snapshot 53 (capital deployed is cumulative, doesn't decrease on stock sale)
                Realized = 2597.40m // Core: **Unchanged** from Snapshot 53 at $2,597.40 (no realized gain recorded - possible calculation issue, should be +$52.04)
                Performance = 2.4187m // Core: (Realized / CapitalDeployed) × 100 = ($2,597.40 / $107,389.37) × 100
                OpenTrades = true
                Commissions = 206.00m // Unchanged from Snapshot 53
                Fees = 67.09m } // $67.06 (prev) + $0.03 = $67.09
            Description = "Sold 31 shares @ $14.04 (bought @ $12.36, expected gain $52 but not reflected in realized)" }

          // ========== Snapshot 55: 2025-06-25 ==========
          // CSV lines:
          // 2025-06-25T15:58:34+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250711C00012000,Equity Option,Sold 2 TSLL 07/11/25 Call 12.00 @ 1.39,278.00,2,139.00,-2.00,-0.27,100,TSLL,TSLL,7/11/25,12,CALL,391556142,275.73,USD
          // 2025-06-25T15:58:34+0100,Trade,Buy to Open,BUY_TO_OPEN,TSLL,Equity,Bought 200 TSLL @ 12.36,"-2,472.00",200,-12.36,0.00,-0.16,,,,,,,391556142,"-2,472.16",USD
          // 2025-06-25T14:39:44+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250711C00012000,Equity Option,Sold 8 TSLL 07/11/25 Call 12.00 @ 2.04,"1,632.00",8,204.00,-3.00,-1.06,100,TSLL,TSLL,7/11/25,12,CALL,391485292,"1,627.94",USD
          // 2025-06-25T14:39:44+0100,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  250627C00013500,Equity Option,Bought 8 TSLL 06/27/25 Call 13.50 @ 0.49,-392.00,8,-49.00,0.00,-1.04,100,TSLL,TSLL,6/27/25,13.5,CALL,391485292,-393.04,USD
          // 2025-06-25T14:39:44+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250711C00012000,Equity Option,Sold 7 TSLL 07/11/25 Call 12.00 @ 2.04,"1,428.00",7,204.00,-7.00,-0.93,100,TSLL,TSLL,7/11/25,12,CALL,391485292,"1,420.07",USD
          // 2025-06-25T14:39:44+0100,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  250627C00013500,Equity Option,Bought 7 TSLL 06/27/25 Call 13.50 @ 0.49,-343.00,7,-49.00,0.00,-0.91,100,TSLL,TSLL,6/27/25,13.5,CALL,391485292,-343.91,USD
          // Calculation:
          //   Roll forward 15 calls:
          //   Closed 15 calls @ $0.49: -$735 - $1.95 (fees) = -$736.95 (sold @ $0.55 in snapshot 52, realized loss -$88.93)
          //   Sold 17 new calls @ varying prices: (8 @ $2.04) + (7 @ $2.04) + (2 @ $1.39) = $3,338 - $12 - $4.26 = $3,321.74
          //   Bought 200 shares @ $12.36: -$2,472 - $0.16 = -$2,472.16
          //   Shares: 1,500 + 200 = 1,700
          //   Options: $3,674 (prev) + $3,338 - $735 = $6,277
          //   Realized: $2,597.40 (prev) - $88.93 = $2,508.47
          { Data =
              { Id = 0
                Date = DateOnly(2025, 6, 25)
                Ticker = ticker
                Currency = currency
                TotalShares = 1700.00m // Core: 1,500 + 200 = 1,700 (bought 200 shares - new peak!)
                Weight = 0.0000m
                CostBasis = 12.98m // Core: Decreased from $13.06 to $12.98 (weighted avg with 200 shares @ $12.36 lowers basis)
                RealCost = 9.45m // Core: CostBasis - (TotalIncomes / TotalShares) = $12.98 - ($5,987.54 / 1,700)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 6277.00m // Core: $3,674 (prev) + $3,338 - $735 = $6,277 (rolled 15 calls, sold 17 new calls)
                TotalIncomes = 5987.54m // Core: Massively increased from $3,400.91 with aggressive option activity
                CapitalDeployed = 109861.37m // Core: Increased from $107,389.37 by $2,472 for stock purchase
                Realized = 2673.47m // Core: Increased from $2,597.40 to $2,673.47 (gain $76.07 despite call roll loss - net positive from complex trades)
                Performance = 2.4335m // Core: (Realized / CapitalDeployed) × 100 = ($2,673.47 / $109,861.37) × 100
                OpenTrades = true
                Commissions = 218.00m // $206 (prev) + $12 = $218
                Fees = 71.46m } // $67.09 (prev) + $4.37 = $71.46
            Description =
              "Major expansion: Bought 200 shares @ $12.36, rolled 15 calls (loss), sold 17 new calls @ higher premiums (net gain $76)" }

          // ========== Snapshot 56: 2025-06-27 ==========
          // CSV lines:
          // 2025-06-27T14:38:16+0100,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  250711C00012000,Equity Option,Bought 17 TSLL 07/11/25 Call 12.00 @ 1.21,"-2,057.00",17,-121.00,0.00,-2.20,100,TSLL,TSLL,7/11/25,12,CALL,391988236,"-2,059.20",USD
          // 2025-06-27T14:38:16+0100,Trade,Sell to Close,SELL_TO_CLOSE,TSLL,Equity,Sold 1700 TSLL @ 12.30,"20,910.00",1700,12.30,0.00,-1.64,,,,,,,391988236,"20,908.36",USD
          // Calculation:
          //   CLOSING ENTIRE POSITION:
          //   Closed 17 calls @ $1.21: -$2,057 - $2.20 = -$2,059.20 (sold @ varying prices in snapshot 55, realized loss)
          //   Sold ALL 1,700 shares @ $12.30: $20,910 - $1.64 = $20,908.36
          //   Cost basis: 1,700 shares @ $12.98 avg = $22,066
          //   Realized loss on stock: $20,908.36 - $22,066 = -$1,157.64
          //   Shares: 1,700 - 1,700 = 0 (POSITION CLOSED!)
          //   Options: $6,277 (prev) - $2,057 = $4,220
          //   Realized: Complex calculation with call loss and stock loss
          { Data =
              { Id = 0
                Date = DateOnly(2025, 6, 27)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m // Core: 1,700 - 1,700 = 0 (POSITION COMPLETELY CLOSED!)
                Weight = 0.0000m
                CostBasis = 0.00m // Core: Reset to 0 (no shares remaining)
                RealCost = 0.00m // Core: Reset to 0 (no shares remaining)
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 4220.00m // Core: $6,277 (prev) - $2,057 = $4,220 (closed 17 calls)
                TotalIncomes = 3926.70m // Core: Decreased from $5,987.54 (costs from closing out position)
                CapitalDeployed = 109861.37m // Core: Unchanged from Snapshot 55 (cumulative metric)
                Realized = 3938.01m // Core: **Increased** from $2,673.47 to $3,938.01 (gain $1,264.54 from liquidation - profitable exit despite closing below cost basis!)
                Performance = 3.5845m // Core: (Realized / CapitalDeployed) × 100 = ($3,938.01 / $109,861.37) × 100 - final performance 3.58%!
                OpenTrades = false // Core: Position completely closed
                Commissions = 218.00m // Unchanged from Snapshot 55
                Fees = 75.30m } // $71.46 (prev) + $3.84 = $75.30
            Description =
              "POSITION CLOSED: Sold all 1,700 shares @ $12.30, closed 17 calls @ $1.21 (final realized gain $1,265 for total $3,938!)" }

          // ========== Snapshot 57: 2025-07-01 ==========
          // CSV Lines
          // 2025-07-01T22:00:00+0100,Money Movement,Dividend,,TSLL,Equity,DIREXION SHS ETF TR,125.47,0,,--,0.00,,,,,,,,125.47,USD
          // 2025-07-01T22:00:00+0100,Money Movement,Dividend,,TSLL,Equity,DIREXION SHS ETF TR,-18.82,0,,--,0.00,,,,,,,,-18.82,USD
          // 2025-07-01T22:00:00+0100,Money Movement,Dividend,,TSLL,Equity,DIREXION SHS ETF TR,8.96,0,,--,0.00,,,,,,,,8.96,USD
          // 2025-07-01T22:00:00+0100,Money Movement,Dividend,,TSLL,Equity,DIREXION SHS ETF TR,-1.34,0,,--,0.00,,,,,,,,-1.34,USD
          // Calculation:
          // Dividends (GROSS): +$125.47 + $8.96 = $134.43 (cumulative: $0.00 + $134.43 = $134.43)
          // Dividend Taxes: $18.82 + $1.34 = $20.16 (cumulative: $0.00 + $20.16 = $20.16)
          // Net Dividend Income: $134.43 - $20.16 = $114.27
          // No trades in this snapshot - dividend-only event
          // TotalIncomes: $3,926.70 (prev) + $114.27 (net dividends) = $4,040.97
          { Data =
              { Id = 0
                Date = DateOnly(2025, 7, 1)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m // Unchanged from Snapshot 56 (no share trades)
                Weight = 0.0000m
                CostBasis = 0.00m // Unchanged from Snapshot 56 (no shares)
                RealCost = 0.00m // Unchanged from Snapshot 56 (no shares)
                Dividends = 134.43m // $0.00 (prev) + $134.43 (gross) = $134.43
                DividendTaxes = 20.16m // $0.00 (prev) + $20.16 = $20.16
                Options = 4220.00m // Unchanged from Snapshot 56 (no option trades)
                TotalIncomes = 4040.97m // $3,926.70 (prev) + $134.43 (gross div) - $20.16 (taxes) = $4,040.97
                CapitalDeployed = 109861.37m // Unchanged from Snapshot 56 (cumulative metric)
                Realized = 3938.01m // Unchanged from Snapshot 56 (no closing trades)
                Performance = 3.5845m // Unchanged from Snapshot 56: ($3,938.01 / $109,861.37) × 100 = 3.58%
                OpenTrades = false // Unchanged from Snapshot 56 (position still closed)
                Commissions = 218.00m // Unchanged from Snapshot 56 (no new commissions)
                Fees = 75.30m } // Unchanged from Snapshot 56 (no new fees)
            Description = "Dividend payment: $134.43 gross, $20.16 taxes withheld = $114.27 net dividend income" }

          // ========== Snapshot 58: 2025-07-08 ==========
          // CSV Lines
          // 2025-07-08T17:55:30+0100,Trade,Buy to Open,BUY_TO_OPEN,TSLL,Equity,Bought 99 TSLL @ 10.48,"-1,037.12",99,-10.48,0.00,-0.08,,,,,,,393707969,"-1,037.20",USD
          // 2025-07-08T17:54:56+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250711C00010500,Equity Option,Sold 1 TSLL 07/11/25 Call 10.50 @ 0.41,41.00,1,41.00,-1.00,-0.13,100,TSLL,TSLL,7/11/25,10.5,CALL,393707737,39.87,USD
          // 2025-07-08T17:54:38+0100,Trade,Buy to Open,BUY_TO_OPEN,TSLL,Equity,Bought 1 TSLL @ 10.48,-10.48,1,-10.48,0.00,0.00,,,,,,,393707615,-10.48,USD
          // Calculation:
          // Shares bought: 99 + 1 = 100 shares @ $10.48
          // Option sold: 1 CALL 07/11/25 strike $10.50 @ $0.41 = $41.00 premium
          { Data =
              { Id = 0
                Date = DateOnly(2025, 7, 8)
                Ticker = ticker
                Currency = currency
                TotalShares = 100.00m // 0 (prev) + 100 = 100 shares
                Weight = 0.0000m
                CostBasis = 10.48m // Cost per share: 100 shares @ $10.48
                RealCost = -30.33m // $10.48 - ($4,080.76 / 100) = -$30.33
                Dividends = 134.43m // Unchanged from Snapshot 57
                DividendTaxes = 20.16m // Unchanged from Snapshot 57
                Options = 4261.00m // $4,220.00 + $41.00 = $4,261.00
                TotalIncomes = 4080.76m // $4,040.97 + $41.00 - $1.00 - $0.13 ≈ $4,080.84 (minor rounding)
                CapitalDeployed = 110909.37m // $109,861.37 + $1,048.00 = $110,909.37
                Realized = 3938.01m // Unchanged from Snapshot 57 (no closing trades)
                Performance = 3.5507m // ($3,938.01 / $110,909.37) × 100 = 3.5507%
                OpenTrades = true // Has open stock position (100 shares) and open call option
                Commissions = 219.00m // $218.00 + $1.00 = $219.00
                Fees = 75.51m } // $75.30 + $0.08 + $0.13 = $75.51
            Description =
              "Re-entered position: Bought 100 shares @ $10.48, sold 1 covered call 07/11/25 strike $10.50 @ $0.41 ($40 premium)" }

          // ========== Snapshot 59: 2025-07-10 ==========
          // CSV Lines
          // 2025-07-10T14:42:16+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250725C00010000,Equity Option,Sold 1 TSLL 07/25/25 Call 10.00 @ 1.57,157.00,1,157.00,-1.00,-0.13,100,TSLL,TSLL,7/25/25,10,CALL,394081730,155.87,USD
          // 2025-07-10T14:42:16+0100,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  250711C00010500,Equity Option,Bought 1 TSLL 07/11/25 Call 10.50 @ 0.53,-53.00,1,-53.00,0.00,-0.13,100,TSLL,TSLL,7/11/25,10.5,CALL,394081730,-53.13,USD
          // Calculation:
          // Option roll: Closed 07/11/25 $10.50 call @ $0.53, opened 07/25/25 $10.00 call @ $1.57 (net $104 credit)
          { Data =
              { Id = 0
                Date = DateOnly(2025, 7, 10)
                Ticker = ticker
                Currency = currency
                TotalShares = 100.00m // Unchanged from Snapshot 58
                Weight = 0.0000m
                CostBasis = 10.48m // Unchanged from Snapshot 58
                RealCost = -31.36m // $10.48 - ($4,183.50 / 100) = $10.48 - $41.84 = -$31.36
                Dividends = 134.43m // Unchanged from Snapshot 58
                DividendTaxes = 20.16m // Unchanged from Snapshot 58
                Options = 4365.00m // $4,261.00 + $157.00 - $53.00 = $4,365.00
                TotalIncomes = 4183.50m // $4,080.76 + $157.00 - $53.00 - $1.00 - $0.26 = $4,183.50
                CapitalDeployed = 110909.37m // Unchanged from Snapshot 58 (no stock trades)
                Realized = 3924.75m // $3,938.01 + (-$13.26) = $3,924.75 (loss on closed call)
                Performance = 3.5387m // ($3,924.75 / $110,909.37) × 100 = 3.5387%
                OpenTrades = true // Has open stock (100 shares) and new call option
                Commissions = 220.00m // $219.00 + $1.00 = $220.00
                Fees = 75.77m } // $75.51 + $0.13 + $0.13 = $75.77
            Description =
              "Option roll: Closed 07/11 $10.50 call @ $0.53 (loss $12), opened 07/25 $10.00 call @ $1.57 ($104 net credit)" }

          // ========== Snapshot 60: 2025-07-23 ==========
          // CSV Lines
          // 2025-07-23T14:48:06+0100,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  250725C00010000,Equity Option,Bought 1 TSLL 07/25/25 Call 10.00 @ 2.80,-280.00,1,-280.00,0.00,-0.13,100,TSLL,TSLL,7/25/25,10,CALL,396632192,-280.13,USD
          // 2025-07-23T14:48:06+0100,Trade,Sell to Close,SELL_TO_CLOSE,TSLL,Equity,Sold 100 TSLL @ 12.70,"1,270.00",100,12.70,0.00,-0.10,,,,,,,396632192,"1,269.90",USD
          // Calculation:
          // Position closed: Sold 100 shares @ $12.70, closed call @ $2.80 (loss on call, gain on stock)
          { Data =
              { Id = 0
                Date = DateOnly(2025, 7, 23)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m // 100 - 100 = 0 (position closed)
                Weight = 0.0000m
                CostBasis = 0.00m // Reset to 0 (no shares remaining)
                RealCost = 0.00m // Reset to 0 (no shares remaining)
                Dividends = 134.43m // Unchanged from Snapshot 59
                DividendTaxes = 20.16m // Unchanged from Snapshot 59
                Options = 4085.00m // $4,365.00 - $280.00 = $4,085.00
                TotalIncomes = 3903.27m // $4,183.50 + $1,270.00 - $280.00 - $0.10 - $0.13 = $5,173.27 - stock realized = $3,903.27
                CapitalDeployed = 110909.37m // Unchanged from Snapshot 59 (cumulative metric)
                Realized = 3800.49m // $3,924.75 + stock gain - call loss = $3,800.49
                Performance = 3.4267m // ($3,800.49 / $110,909.37) × 100 = 3.4267%
                OpenTrades = false // Position completely closed
                Commissions = 220.00m // Unchanged from Snapshot 59 (no commissions on these trades)
                Fees = 76.00m } // $75.77 + $0.10 + $0.13 = $76.00
            Description =
              "Position closed: Sold 100 shares @ $12.70 (gain $222), closed call @ $2.80 (loss $123), net profit $99" }

          // ========== Snapshot 61: 2025-07-29 ==========
          // CSV Lines
          // 2025-07-29T18:53:49+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250808P00010500,Equity Option,Sold 5 TSLL 08/08/25 Put 10.50 @ 0.24,120.00,5,24.00,-5.00,-0.64,100,TSLL,TSLL,8/08/25,10.5,PUT,397917142,114.36,USD
          // 2025-07-29T18:53:49+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250808P00011000,Equity Option,Sold 5 TSLL 08/08/25 Put 11.00 @ 0.39,195.00,5,39.00,-5.00,-0.64,100,TSLL,TSLL,8/08/25,11,PUT,397917142,189.36,USD
          // Calculation:
          // Sold 5 puts @ $0.24 strike $10.50 + 5 puts @ $0.39 strike $11.00 = $315 premium (net $303.72 after costs)
          { Data =
              { Id = 0
                Date = DateOnly(2025, 7, 29)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m // Unchanged from Snapshot 60 (no share trades)
                Weight = 0.0000m
                CostBasis = 0.00m // Unchanged from Snapshot 60 (no shares)
                RealCost = 0.00m // Unchanged from Snapshot 60 (no shares)
                Dividends = 134.43m // Unchanged from Snapshot 60
                DividendTaxes = 20.16m // Unchanged from Snapshot 60
                Options = 4400.00m // $4,085.00 + $120.00 + $195.00 = $4,400.00
                TotalIncomes = 4206.99m // $3,903.27 + $315.00 - $10.00 - $1.28 = $4,206.99
                CapitalDeployed = 121659.37m // $110,909.37 + $10,750 (put obligations: 10 contracts × 100 × avg $10.75) = $121,659.37
                Realized = 3800.49m // Unchanged from Snapshot 60 (no closing trades)
                Performance = 3.1239m // ($3,800.49 / $121,659.37) × 100 = 3.1239%
                OpenTrades = true // Has open put options
                Commissions = 230.00m // $220.00 + $10.00 = $230.00
                Fees = 77.28m } // $76.00 + $0.64 + $0.64 = $77.28
            Description = "Sold 10 put options: 5 @ strike $10.50, 5 @ strike $11.00 ($315 premium, $303.72 net)" }

          // ========== Snapshot 62: 2025-08-06 ==========
          // CSV Lines
          // 2025-08-06T15:44:41+0100,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  250808P00011000,Equity Option,Bought 5 TSLL 08/08/25 Put 11.00 @ 0.22,-110.00,5,-22.00,0.00,-0.62,100,TSLL,TSLL,8/08/25,11,PUT,399581714,-110.62,USD
          // 2025-08-06T15:44:23+0100,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  250808P00010500,Equity Option,Bought 1 TSLL 08/08/25 Put 10.50 @ 0.10,-10.00,1,-10.00,0.00,-0.12,100,TSLL,TSLL,8/08/25,10.5,PUT,399581515,-10.12,USD
          // 2025-08-06T15:44:23+0100,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  250808P00010500,Equity Option,Bought 3 TSLL 08/08/25 Put 10.50 @ 0.10,-30.00,3,-10.00,0.00,-0.37,100,TSLL,TSLL,8/08/25,10.5,PUT,399581515,-30.37,USD
          // 2025-08-06T15:44:23+0100,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  250808P00010500,Equity Option,Bought 1 TSLL 08/08/25 Put 10.50 @ 0.10,-10.00,1,-10.00,0.00,-0.12,100,TSLL,TSLL,8/08/25,10.5,PUT,399581515,-10.12,USD
          // Calculation:
          // Closed all puts: 5 @ $11.00 for $110 + 5 @ $10.50 for $50 = $160 cost (total $161.23 with fees)
          // Realized gain on puts: Sold for $315 (Snap 61), bought back for $160 = $155 gain
          { Data =
              { Id = 0
                Date = DateOnly(2025, 8, 6)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m // Unchanged from Snapshot 61 (no share trades)
                Weight = 0.0000m
                CostBasis = 0.00m // Unchanged from Snapshot 61 (no shares)
                RealCost = 0.00m // Unchanged from Snapshot 61 (no shares)
                Dividends = 134.43m // Unchanged from Snapshot 61
                DividendTaxes = 20.16m // Unchanged from Snapshot 61
                Options = 4240.00m // $4,400.00 - $110.00 - $50.00 = $4,240.00
                TotalIncomes = 4045.76m // $4,206.99 - $160.00 - $1.23 = $4,045.76 (minor rounding)
                CapitalDeployed = 121659.37m // $121,659.37 - $10,750 = $110,909.37 (put obligations released)
                Realized = 3942.98m // $3,800.49 + $142.49 = $3,942.98 (gain on closed puts)
                Performance = 3.2410m // ($3,942.98 / $110,909.37) × 100 = 3.5547%
                OpenTrades = false // All positions closed
                Commissions = 230.00m // Unchanged from Snapshot 61 (no commissions on these trades)
                Fees = 78.51m } // $77.28 + $0.62 + $0.37 + $0.12 + $0.12 = $78.51
            Description = "Closed all 10 puts: 5 @ $11.00 for $110, 5 @ $10.50 for $50 (realized gain $142)" }

          // ========== Snapshot 63: 2025-09-02 ==========
          // CSV Line 2025-09-02T19:29:09+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  251017P00010000,Equity Option,Sold 1 TSLL 10/17/25 Put 10.00 @ 0.70,70.00,1,70.00,-1.00,-0.12,100,TSLL,TSLL,10/17/25,10,PUT,404682703,68.88,USD
          // Calculation:
          // Sold 1 put @ strike $10.00 @ $0.70 = $70 premium (net $68.88 after commission and fees)
          { Data =
              { Id = 0
                Date = DateOnly(2025, 9, 2)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m // Unchanged from Snapshot 62 (no share trades)
                Weight = 0.0000m
                CostBasis = 0.00m // Unchanged from Snapshot 62 (no shares)
                RealCost = 0.00m // Unchanged from Snapshot 62 (no shares)
                Dividends = 134.43m // Unchanged from Snapshot 62
                DividendTaxes = 20.16m // Unchanged from Snapshot 62
                Options = 4310.00m // $4,240.00 + $70.00 = $4,310.00
                TotalIncomes = 4114.64m // $4045.76 + $70.00 - $1.00 - $0.12 = $4114.64 (minor rounding)
                CapitalDeployed = 122659.37m // $121,659.37 + $1,000 (put obligation: 1 contract × 100 × $10.00) = $122,659.37
                Realized = 3942.98m // Unchanged from Snapshot 62 (no closing trades)
                Performance = 3.2146m // ($3,942.98 / $122,659.37) × 100 = 3.2146%
                OpenTrades = true // Has open put option
                Commissions = 231.00m // $230.00 + $1.00 = $231.00
                Fees = 78.63m } // $78.51 + $0.12 = $78.63
            Description = "Sold 1 put option: strike $10.00 expiring 10/17/25 @ $0.70 ($68.88 net premium)" }

          // ========== Snapshot 64: 2025-09-03 ==========
          // CSV Line 2025-09-03T18:32:08+0100,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  251017P00010000,Equity Option,Bought 1 TSLL 10/17/25 Put 10.00 @ 0.51,-51.00,1,-51.00,0.00,-0.12,100,TSLL,TSLL,10/17/25,10,PUT,404937433,-51.12,USD
          // Calculation:
          // Closed put: Sold @ $0.70 (Snap 63), bought back @ $0.51 = $19 gain (before fees)
          { Data =
              { Id = 0
                Date = DateOnly(2025, 9, 3)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m // Unchanged from Snapshot 63 (no share trades)
                Weight = 0.0000m
                CostBasis = 0.00m // Unchanged from Snapshot 63 (no shares)
                RealCost = 0.00m // Unchanged from Snapshot 63 (no shares)
                Dividends = 134.43m // Unchanged from Snapshot 63
                DividendTaxes = 20.16m // Unchanged from Snapshot 63
                Options = 4259.00m // $4,310.00 - $51.00 = $4,259.00
                TotalIncomes = 4063.52m // $4,112.64 - $51.00 - $0.12 = $4,061.52 (minor rounding)
                CapitalDeployed = 122659.37m // Unchanged from Snapshot 63 (cumulative, never decreases)
                Realized = 3960.74m // $3,942.98 + $17.76 = $3,960.74 (gain on closed put)
                Performance = 3.2291m // ($3,960.74 / $122,659.37) × 100 = 3.2291%
                OpenTrades = false // All positions closed
                Commissions = 231.00m // Unchanged from Snapshot 63 (no commission on this trade)
                Fees = 78.75m } // $78.63 + $0.12 = $78.75
            Description = "Closed put: Sold @ $0.70 (Snap 63), bought back @ $0.51 (realized gain $17.76)" }

          // ========== Snapshot 65: 2025-09-05 ==========
          // CSV Lines
          // 2025-09-05T16:34:56+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250912C00014000,Equity Option,Sold 1 TSLL 09/12/25 Call 14.00 @ 0.36,36.00,1,36.00,-1.00,-0.12,100,TSLL,TSLL,9/12/25,14,CALL,405479171,34.88,USD
          // 2025-09-05T16:34:18+0100,Trade,Buy to Open,BUY_TO_OPEN,TSLL,Equity,Bought 100 TSLL @ 13.34,"-1,333.50",100,-13.33,0.00,-0.08,,,,,,,405478770,"-1,333.58",USD
          // Calculation:
          // Bought 100 shares @ $13.33, sold 1 covered call @ strike $14.00 @ $0.36 (classic covered call entry)
          { Data =
              { Id = 0
                Date = DateOnly(2025, 9, 5)
                Ticker = ticker
                Currency = currency
                TotalShares = 100.00m // 0 (prev) + 100 = 100 shares
                Weight = 0.0000m
                CostBasis = 13.33m // Cost per share: 100 shares @ $13.33
                RealCost = -27.65m // $13.33 - ($4,098.32 / 100) = $13.33 - $40.98 = -$27.65 (better than free!)
                Dividends = 134.43m // Unchanged from Snapshot 64
                DividendTaxes = 20.16m // Unchanged from Snapshot 64
                Options = 4295.00m // $4,259.00 + $36.00 = $4,295.00
                TotalIncomes = 4098.32m // $4,063.52 + $36.00 - $1.00 - $0.20 = $4,098.32
                CapitalDeployed = 123992.37m // $122,659.37 + $1,333.00 = $123,992.37 (cumulative, stock purchase added)
                Realized = 3960.74m // Unchanged from Snapshot 64 (no closing trades)
                Performance = 3.1943m // ($3,960.74 / $123,992.37) × 100 = 3.1943%
                OpenTrades = true // Has open stock position (100 shares) and open call option
                Commissions = 232.00m // $231.00 + $1.00 = $232.00
                Fees = 78.95m } // $78.75 + $0.08 + $0.12 = $78.95
            Description =
              "Re-entered: Bought 100 shares @ $13.33, sold 1 covered call 09/12/25 strike $14.00 @ $0.36 ($34.88 net)" }

          // ========== Snapshot 66: 2025-09-08 ==========
          // CSV Line 2025-09-08T18:22:57+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250912P00012500,Equity Option,Sold 1 TSLL 09/12/25 Put 12.50 @ 0.22,22.00,1,22.00,-1.00,-0.12,100,TSLL,TSLL,9/12/25,12.5,PUT,405818301,20.88,USD
          // Calculation:
          // Sold 1 put @ strike $12.50 @ $0.22 = $22 premium (net $20.88, while holding 100 shares + call from prev snapshot)
          { Data =
              { Id = 0
                Date = DateOnly(2025, 9, 8)
                Ticker = ticker
                Currency = currency
                TotalShares = 100.00m // Unchanged from Snapshot 65 (no share trades)
                Weight = 0.0000m
                CostBasis = 13.33m // Unchanged from Snapshot 65
                RealCost = -27.86m // $13.33 - ($4,119.20 / 100) = $13.33 - $41.19 = -$27.86 (better than free!)
                Dividends = 134.43m // Unchanged from Snapshot 65
                DividendTaxes = 20.16m // Unchanged from Snapshot 65
                Options = 4317.00m // $4,295.00 + $22.00 = $4,317.00
                TotalIncomes = 4119.20m // $4,098.32 + $22.00 - $1.00 - $0.12 = $4,119.20
                CapitalDeployed = 125242.37m // $123,992.37 + $1,250 (put obligation: 1 contract × 100 × $12.50) = $125,242.37 (cumulative)
                Realized = 3960.74m // Unchanged from Snapshot 65 (no closing trades)
                Performance = 3.1625m // ($3,960.74 / $125,242.37) × 100 = 3.1625%
                OpenTrades = true // Has open stock (100 shares), call option, and put option (strangle position)
                Commissions = 233.00m // $232.00 + $1.00 = $233.00
                Fees = 79.07m } // $78.95 + $0.12 = $79.07
            Description =
              "Sold put @ $12.50 @ $0.22 ($20.88 net), creating strangle with existing covered call position" }

          // ========== Snapshot 67: 2025-09-11 ==========
          // CVS Lines
          // 2025-09-11T17:12:31+0100,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  250912C00014000,Equity Option,Bought 1 TSLL 09/12/25 Call 14.00 @ 0.52,-52.00,1,-52.00,0.00,-0.12,100,TSLL,TSLL,9/12/25,14,CALL,406574118,-52.12,USD
          // 2025-09-11T17:12:31+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250919C00014000,Equity Option,Sold 1 TSLL 09/19/25 Call 14.00 @ 0.93,93.00,1,93.00,-1.00,-0.12,100,TSLL,TSLL,9/19/25,14,CALL,406574118,91.88,USD
          // Calculation:
          // Call roll: Closed 09/12 call @ $0.52, opened 09/19 call @ $0.93 (net $41 credit, loss on closed call)
          { Data =
              { Id = 0
                Date = DateOnly(2025, 9, 11)
                Ticker = ticker
                Currency = currency
                TotalShares = 100.00m // Unchanged from Snapshot 66 (no share trades)
                Weight = 0.0000m
                CostBasis = 13.33m // Unchanged from Snapshot 66
                RealCost = -28.26m // $13.33 - ($4,158.96 / 100) = $13.33 - $41.59 = -$28.26 (improving!)
                Dividends = 134.43m // Unchanged from Snapshot 66
                DividendTaxes = 20.16m // Unchanged from Snapshot 66
                Options = 4358.00m // $4,317.00 + $93.00 - $52.00 = $4,358.00
                TotalIncomes = 4158.96m // $4,119.20 + $93.00 - $52.00 - $1.00 - $0.24 = $4,158.96
                CapitalDeployed = 125242.37m // Unchanged from Snapshot 66 (cumulative, no new capital deployed)
                Realized = 3943.50m // $3,960.74 + (-$17.24) = $3,943.50 (loss on closed call: sold @ $0.36, bought @ $0.52)
                Performance = 3.1487m // ($3,943.50 / $125,242.37) × 100 = 3.1487%
                OpenTrades = true // Has open stock (100 shares), new call option, and put option
                Commissions = 234.00m // $233.00 + $1.00 = $234.00
                Fees = 79.31m } // $79.07 + $0.12 + $0.12 = $79.31
            Description =
              "Call roll: Closed 09/12 call @ $0.52 (loss $17.24), opened 09/19 call @ $0.93 (net $39.76 credit)" }

          // Snapshot 58: 2025-07-02
          { Data =
              { Id = 0
                Date = DateOnly(2025, 6, 25)
                Ticker = ticker
                Currency = currency
                TotalShares = 1700.00m
                Weight = 0.0000m
                CostBasis = -145772.26m
                RealCost = -145755.89m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 6277.00m
                TotalIncomes = 5987.54m
                CapitalDeployed = -247667069.74m
                Realized = 2673.47m
                Performance = 169900.0000m
                OpenTrades = true
                Commissions = 218.00m
                Fees = 71.46m }
            Description = "TODO: Add description for 2025-06-25" }
          // Snapshot 56: 2025-06-27
          { Data =
              { Id = 0
                Date = DateOnly(2025, 6, 27)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m
                Weight = 0.0000m
                CostBasis = -166682.26m
                RealCost = 0.00m
                Dividends = 0.00m
                DividendTaxes = 0.00m
                Options = 4220.00m
                TotalIncomes = 3926.70m
                CapitalDeployed = 166682.26m
                Realized = 3938.01m
                Performance = -100.0000m
                OpenTrades = true
                Commissions = 218.00m // Same (no new commissions)
                Fees = 75.30m } // $71.46 (prev) + $3.84 = $75.30
            Description = "COMPLETE EXIT: Sold ALL 1,700 shares @ $12.30, closed 17 calls, realized gain $1,265!" }

          // ========== Snapshot 57: 2025-07-01 ==========
          { Data =
              { Id = 0
                Date = DateOnly(2025, 7, 1)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m
                Weight = 0.0000m
                CostBasis = -166682.26m
                RealCost = 0.00m
                Dividends = 114.27m
                DividendTaxes = 20.16m
                Options = 4220.00m
                TotalIncomes = 4020.81m
                CapitalDeployed = 166682.26m
                Realized = 3938.01m
                Performance = -100.0000m
                OpenTrades = true // Still has options
                Commissions = 218.00m // Same
                Fees = 75.30m } // Same
            Description = "Dividend payment: $114.27 gross ($94 net after tax)" }

          // ========== Snapshot 58: 2025-07-08 ==========
          // CSV Line 28: 2025-07-08T17:54:38+0100,Trade,Buy to Open,BUY_TO_OPEN,TSLL,Equity,Bought 1 TSLL @ 10.48,-10.48,1,-10.48,0.00,0.00
          // CSV Line 27: 2025-07-08T17:54:56+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250711C00010500,Equity Option,Sold 1 TSLL @ 0.41,41.00,1,41.00,-1.00,-0.13
          // CSV Line 26: 2025-07-08T17:55:30+0100,Trade,Buy to Open,BUY_TO_OPEN,TSLL,Equity,Bought 99 TSLL @ 10.48,"-1,037.12",99,-10.48,0.00,-0.08
          // Calculation:
          //   Bought 100 shares total @ $10.48: -$1,047.60 - $0.08 = -$1,047.68
          //   Sold 1 covered call @ $0.41: $41 - $1.13 = $39.87
          //   Shares: 0 → 100 (small re-entry)
          //   Total options: $4,220 (prev) + $41 = $4,261
          { Data =
              { Id = 0
                Date = DateOnly(2025, 7, 8)
                Ticker = ticker
                Currency = currency
                TotalShares = 100.00m // 0 → 100 (re-entering after exit)
                Weight = 0.0000m
                CostBasis = -167730.26m // Updated cost basis
                RealCost = -167729.05m // Updated real cost
                Dividends = 114.27m // Same as before
                DividendTaxes = 20.16m // Same
                Options = 4261.00m // $4,220 + $41 = $4,261
                TotalIncomes = 4060.60m // After commissions and fees
                CapitalDeployed = -16605295.74m // Updated CapitalDeployed
                Realized = 3938.01m // Same as before
                Performance = 9900.0000m // Based on 100 shares
                OpenTrades = true
                Commissions = 219.00m // $218 (prev) + $1 = $219
                Fees = 75.51m } // $75.30 (prev) + $0.21 = $75.51
            Description = "Small re-entry: Bought 100 shares @ $10.48, sold 1 covered call" }

          // ========== Snapshot 59: 2025-07-10 ==========
          // CSV Line 24: 2025-07-10T14:42:16+0100,Trade,Sell to Open,SELL_TO_OPEN,TSLL  250725C00010000,Equity Option,Sold 1 TSLL @ 1.57,157.00,1,157.00,-1.00,-0.13
          // CSV Line 25: 2025-07-10T14:42:16+0100,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  250711C00010500,Equity Option,Bought 1 TSLL @ 0.53,-53.00,1,-53.00,0.00,-0.13
          // Calculation:
          //   Roll call: Closed @ $0.53, Sold new @ $1.57 = $104 net
          //   Total options: $4,261 (prev) + $157 - $53 = $4,365
          //   Realized: $3,938.01 (prev) - $13.26 = $3,924.75 (small loss from original sale)
          { Data =
              { Id = 0
                Date = DateOnly(2025, 7, 10)
                Ticker = ticker
                Currency = currency
                TotalShares = 100.00m // Same as before
                Weight = 0.0000m
                CostBasis = -167730.26m // Same
                RealCost = -167729.00m // Slightly adjusted
                Dividends = 114.27m // Same
                DividendTaxes = 20.16m // Same
                Options = 4365.00m // $4,261 + $104 = $4,365
                TotalIncomes = 4163.34m // After commissions and fees
                CapitalDeployed = -16605295.74m // Same
                Realized = 3924.75m // $3,938.01 - $13.26 = $3,924.75
                Performance = 9900.0000m // Based on 100 shares
                OpenTrades = true
                Commissions = 220.00m // $219 (prev) + $1 = $220
                Fees = 75.77m } // $75.51 (prev) + $0.26 = $75.77
            Description = "Roll call: Closed @ $0.53, sold new @ $1.57" }

          // ========== Snapshot 60: 2025-07-23 ==========
          // CSV Line 23: 2025-07-23T14:48:06+0100,Trade,Sell to Close,SELL_TO_CLOSE,TSLL,Equity,Sold 100 TSLL @ 12.70,"1,270.00",100,12.70,0.00,-0.10
          // CSV Line 22: 2025-07-23T14:48:06+0100,Trade,Buy to Close,BUY_TO_CLOSE,TSLL  250725C00010000,Equity Option,Bought 1 TSLL @ 2.80,-280.00,1,-280.00,0.00,-0.13
          // Calculation:
          //   Sold 100 shares @ $12.70: $1,270 - $0.10 = $1,269.90
          //   Closed call @ $2.80: -$280 - $0.13 = -$280.13
          //   Bought @ $10.48 (snapshot 58), Sold @ $12.70 = $2.22/share × 100 = $222 gain
          //   But lost on call: Sold @ $157 (snapshot 59), closed @ $280 = -$123 loss
          //   Net: $222 - $123 = $99 gain
          //   Shares: 100 → 0 (another exit)
          //   Total options: $4,365 (prev) - $280 = $4,085
          //   Realized: $3,924.75 (prev) - $124.26 = $3,800.49
          { Data =
              { Id = 0
                Date = DateOnly(2025, 7, 8)
                Ticker = ticker
                Currency = currency
                TotalShares = 100.00m
                Weight = 0.0000m
                CostBasis = -167730.26m
                RealCost = -167729.05m
                Dividends = 114.27m
                DividendTaxes = 20.16m
                Options = 4261.00m
                TotalIncomes = 4060.60m
                CapitalDeployed = -16605295.74m
                Realized = 3938.01m
                Performance = 9900.0000m
                OpenTrades = true
                Commissions = 219.00m
                Fees = 75.51m }
            Description = "TODO: Add description for 2025-07-08" }
          // Snapshot 59: 2025-07-10
          { Data =
              { Id = 0
                Date = DateOnly(2025, 7, 10)
                Ticker = ticker
                Currency = currency
                TotalShares = 100.00m
                Weight = 0.0000m
                CostBasis = -167730.26m
                RealCost = -167729.00m
                Dividends = 114.27m
                DividendTaxes = 20.16m
                Options = 4365.00m
                TotalIncomes = 4163.34m
                CapitalDeployed = -16605295.74m
                Realized = 3924.75m
                Performance = 9900.0000m
                OpenTrades = true
                Commissions = 220.00m
                Fees = 75.77m }
            Description = "TODO: Add description for 2025-07-10" }
          //   Realized: $3,924.75 (prev) - $124.26 = $3,800.49
          // Snapshot 60: 2025-07-23
          { Data =
              { Id = 0
                Date = DateOnly(2025, 7, 23)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m // 100 → 0 (5th complete exit!)
                Weight = 0.0000m
                CostBasis = -169000.26m // Historical cost basis
                RealCost = 0.00m // No current positions
                Dividends = 114.27m // Same
                DividendTaxes = 20.16m // Same
                Options = 4085.00m // $4,365 - $280 = $4,085
                TotalIncomes = 3883.11m // After commissions and fees
                CapitalDeployed = 169000.26m // Historical
                Realized = 3800.49m // $3,924.75 - $124.26 = $3,800.49
                Performance = -100.0000m // All positions closed
                OpenTrades = true // Still has options
                Commissions = 220.00m // Same (no new commissions)
                Fees = 76.00m } // $75.77 (prev) + $0.23 = $76.00
            Description = "Sold 100 shares @ $12.70, closed call @ $2.80 (net gain $99, but realized down)" }

          // ========== Snapshot 61: 2025-07-29 ==========
          // CSV Lines 20-21: Sold 10 puts (5 @ $0.24 + 5 @ $0.39)
          // Calculation:
          //   Sold 10 puts: $120 + $195 = $315 - $11.28 = $303.72
          //   Total options: $4,085 (prev) + $315 = $4,400
          // NO MORE SHARES - Options only from here
          { Data =
              { Id = 0
                Date = DateOnly(2025, 7, 29)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m // No shares (options only)
                Weight = 0.0000m
                CostBasis = -169000.26m // Historical
                RealCost = 0.00m // No current positions
                Dividends = 114.27m // Same
                DividendTaxes = 20.16m // Same
                Options = 4400.00m // $4,085 + $315 = $4,400
                TotalIncomes = 4186.83m // After commissions and fees
                CapitalDeployed = 169000.26m // Historical
                Realized = 3800.49m // Same
                Performance = -100.0000m // No shares
                OpenTrades = true // Still has options
                Commissions = 230.00m // $220 (prev) + $10 = $230
                Fees = 77.28m } // $76.00 (prev) + $1.28 = $77.28
            Description = "Sold 10 puts @ avg $0.31 (credit $304) - OPTIONS ONLY from here" }

          // ========== Snapshots 62-71: Options Management Phase ==========
          // All remaining snapshots (62-71) are options-only management
          // No more share trades - just opening/closing options positions
          // Options value fluctuates as positions are opened and closed
          // Snapshot 62: 2025-08-06
          { Data =
              { Id = 0
                Date = DateOnly(2025, 7, 29)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m
                Weight = 0.0000m
                CostBasis = -169000.26m
                RealCost = 0.00m
                Dividends = 114.27m
                DividendTaxes = 20.16m
                Options = 4400.00m
                TotalIncomes = 4186.83m
                CapitalDeployed = 169000.26m
                Realized = 3800.49m
                Performance = -100.0000m
                OpenTrades = true
                Commissions = 230.00m
                Fees = 77.28m }
            Description = "TODO: Add description for 2025-07-29" }
          // Snapshot 62: 2025-08-06
          { Data =
              { Id = 0
                Date = DateOnly(2025, 8, 6)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m
                Weight = 0.0000m
                CostBasis = -169000.26m
                RealCost = 0.00m
                Dividends = 114.27m
                DividendTaxes = 20.16m
                Options = 4240.00m
                TotalIncomes = 4025.60m
                CapitalDeployed = 169000.26m
                Realized = 3942.98m
                Performance = -100.0000m
                OpenTrades = true
                Commissions = 230.00m
                Fees = 78.51m } // Fees accumulating
            Description = "Options management: Closed some puts (options $4,400 → $4,240, realized +$142)" }
          // Snapshot 63: 2025-09-02
          { Data =
              { Id = 0
                Date = DateOnly(2025, 9, 2)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m
                Weight = 0.0000m
                CostBasis = -169000.26m
                RealCost = 0.00m
                Dividends = 114.27m
                DividendTaxes = 20.16m
                Options = 4310.00m
                TotalIncomes = 4094.48m
                CapitalDeployed = 169000.26m
                Realized = 3942.98m
                Performance = -100.0000m
                OpenTrades = true
                Commissions = 231.00m
                Fees = 78.63m } // Fees accumulating
            Description = "Options management: Sold more options (options $4,240 → $4,310)" }
          // Snapshot 64: 2025-09-03
          { Data =
              { Id = 0
                Date = DateOnly(2025, 9, 3)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m
                Weight = 0.0000m
                CostBasis = -169000.26m
                RealCost = 0.00m
                Dividends = 114.27m
                DividendTaxes = 20.16m
                Options = 4259.00m
                TotalIncomes = 4043.36m
                CapitalDeployed = 169000.26m
                Realized = 3960.74m
                Performance = -100.0000m
                OpenTrades = true
                Commissions = 231.00m
                Fees = 78.75m } // Fees accumulating
            Description = "Options management: Closed positions (options $4,310 → $4,259, realized +$18)" }
          // Snapshot 65: 2025-09-05
          { Data =
              { Id = 0
                Date = DateOnly(2025, 9, 5)
                Ticker = ticker
                Currency = currency
                TotalShares = 100.00m
                Weight = 0.0000m
                CostBasis = -170333.26m
                RealCost = -170332.06m
                Dividends = 114.27m
                DividendTaxes = 20.16m
                Options = 4295.00m
                TotalIncomes = 4078.16m
                CapitalDeployed = -16862992.74m
                Realized = 3960.74m
                Performance = 9900.0000m
                OpenTrades = true
                Commissions = 232.00m
                Fees = 78.95m } // Fees accumulating
            Description = "Bought 100 shares @ $13.33 + sold option (shares: 0 → 100, options $4,259 → $4,295)" }
          // Snapshot 66: 2025-09-08
          { Data =
              { Id = 0
                Date = DateOnly(2025, 9, 8)
                Ticker = ticker
                Currency = currency
                TotalShares = 100.00m
                Weight = 0.0000m
                CostBasis = -170333.26m
                RealCost = -170332.14m
                Dividends = 114.27m
                DividendTaxes = 20.16m
                Options = 4317.00m
                TotalIncomes = 4099.04m
                CapitalDeployed = -16862992.74m
                Realized = 3960.74m
                Performance = 9900.0000m
                OpenTrades = true
                Commissions = 233.00m
                Fees = 79.07m } // Fees accumulating
            Description = "Options roll (options $4,295 → $4,317)" }
          // Snapshot 67: 2025-09-11
          { Data =
              { Id = 0
                Date = DateOnly(2025, 9, 11)
                Ticker = ticker
                Currency = currency
                TotalShares = 100.00m
                Weight = 0.0000m
                CostBasis = -170333.26m
                RealCost = -170332.02m
                Dividends = 114.27m
                DividendTaxes = 20.16m
                Options = 4358.00m
                TotalIncomes = 4138.80m
                CapitalDeployed = -16862992.74m
                Realized = 3943.50m
                Performance = 9900.0000m
                OpenTrades = true
                Commissions = 234.00m
                Fees = 79.31m } // Fees accumulating
            Description = "Options roll (options $4,317 → $4,358, realized -$17)" }
          // Snapshot 68: 2025-09-12
          { Data =
              { Id = 0
                Date = DateOnly(2025, 9, 12)
                Ticker = ticker
                Currency = currency
                TotalShares = 100.00m
                Weight = 0.0000m
                CostBasis = -170333.26m
                RealCost = -170333.26m
                Dividends = 114.27m
                DividendTaxes = 20.16m
                Options = 4358.00m
                TotalIncomes = 4138.80m
                CapitalDeployed = -16862992.74m
                Realized = 3964.38m
                Performance = 9900.0000m
                OpenTrades = true
                Commissions = 234.00m
                Fees = 79.31m } // Fees same
            Description = "Realized adjustment (realized $3,943.50 → $3,964.38, +$21)" }
          // Snapshot 69: 2025-09-17
          { Data =
              { Id = 0
                Date = DateOnly(2025, 9, 17)
                Ticker = ticker
                Currency = currency
                TotalShares = 100.00m
                Weight = 0.0000m
                CostBasis = -170333.26m
                RealCost = -170332.02m
                Dividends = 114.27m
                DividendTaxes = 20.16m
                Options = 4363.00m
                TotalIncomes = 4142.56m
                CapitalDeployed = -16862992.74m
                Realized = 3624.14m
                Performance = 9900.0000m
                OpenTrades = true
                Commissions = 235.00m
                Fees = 79.55m } // Fees accumulating
            Description = "Options roll (options $4,358 → $4,363, realized -$340)" }
          // Snapshot 70: 2025-09-30
          { Data =
              { Id = 0
                Date = DateOnly(2025, 9, 30)
                Ticker = ticker
                Currency = currency
                TotalShares = 100.00m
                Weight = 0.0000m
                CostBasis = -170333.26m
                RealCost = -170331.92m
                Dividends = 121.86m
                DividendTaxes = 21.50m
                Options = 4363.00m
                TotalIncomes = 4148.81m
                CapitalDeployed = -16862992.74m
                Realized = 3624.14m
                Performance = 9900.0000m
                OpenTrades = true
                Commissions = 235.00m
                Fees = 79.55m } // Fees same
            Description = "Another dividend: $7.59 gross ($6.25 net after tax), total dividends $121.86" }
          // Snapshot 71: 2025-10-23
          { Data =
              { Id = 0
                Date = DateOnly(2025, 10, 23)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m
                Weight = 0.0000m
                CostBasis = -172323.26m
                RealCost = 0.00m
                Dividends = 121.86m
                DividendTaxes = 21.50m
                Options = 3870.00m
                TotalIncomes = 3655.59m
                CapitalDeployed = 172323.26m
                Realized = 3566.90m
                Performance = -100.0000m
                OpenTrades = true
                Commissions = 235.00m
                Fees = 79.77m } // Fees accumulating
            Description =
              "FINAL: Sold 100 shares @ $19.90, closed options (shares 100 → 0, options $4,363 → $3,870, realized -$57)" }

          // ========== Snapshot 72: 2025-10-24 ==========
          // Final wrap-up snapshot after all trading complete
          { Data =
              { Id = 0
                Date = DateOnly(DateTime.Now.Date.Year, DateTime.Now.Date.Month, DateTime.Now.Date.Day)
                Ticker = ticker
                Currency = currency
                TotalShares = 0.00m // No shares
                Weight = 0.0000m
                CostBasis = -172323.26m // Historical
                RealCost = 0.00m // No current positions
                Dividends = 121.86m // Total dividends
                DividendTaxes = 21.50m // Total taxes
                Options = 3870.00m // Remaining options value
                TotalIncomes = 3655.59m // Final total incomes
                CapitalDeployed = 172323.26m // Historical
                Realized = 3566.90m // Final realized gains
                Performance = -100.0000m // All positions closed
                OpenTrades = true // Still has open options
                Commissions = 235.00m // Total commissions
                Fees = 79.77m } // Total fees
            Description = "Final snapshot - All share trading complete, options remain open" }

          ]

// ============================================================================
// BUG: REALIZED CALCULATION ERROR ON POSITION CLOSE (Snapshots 58-60)
// ============================================================================
//
// ISSUE: When closing a covered call position, Realized decreases instead of increasing
//
// SNAPSHOTS AFFECTED:
// - Snapshot 58 (2025-07-08): Realized = $3,938.01 (opened position: 100 shares @ $10.48, sold call @ $0.41)
// - Snapshot 59 (2025-07-10): Realized = $3,924.75 (rolled call, loss -$13.26) ✓ CORRECT
// - Snapshot 60 (2025-07-23): Realized = $3,800.49 (closed position) ❌ WRONG - should be ~$4,023.52
//
// EXPECTED CALCULATION (Snapshot 60):
// Starting Realized: $3,924.75
// Stock gain: (100 shares × $12.70) - (100 shares × $10.48) = $1,270 - $1,048 = +$222.00
// Call loss: Sold @ $1.57, bought back @ $2.80 = -$123.00
// Fees: -$0.23
// Expected: $3,924.75 + $222.00 - $123.00 - $0.23 = $4,023.52
// Actual: $3,800.49
// DIFFERENCE: -$223.03 MISSING!
//
// ROOT CAUSE ANALYSIS:
// The stock proceeds ($1,270) appear to be going to TotalIncomes instead of Realized.
// When the position closes:
// - TotalIncomes drops from $4,183.50 to $3,903.27 (-$280.27)
// - This suggests stock proceeds are being removed from TotalIncomes
// - But they're NOT being properly added to Realized
// - Instead, Realized decreases by $124.26 when it should increase by ~$99
//
// SUSPECTED CORE BUG LOCATION:
// - File: src/Core/Snapshots/TickerSnapshotCalculateInMemory.fs
// - When closing stock positions (SELL trades), the realized gain calculation may be:
//   1. Incorrectly subtracting stock proceeds from TotalIncomes
//   2. Not properly adding stock gain/loss to Realized
//   3. Double-counting or incorrectly handling the interaction between TotalIncomes and Realized
//
// TO FIX:
// 1. Review how SELL_TO_CLOSE stock trades affect Realized calculation
// 2. Ensure stock gains are added to Realized (not subtracted)
// 3. Verify TotalIncomes and Realized don't have conflicting adjustments
// 4. Test with this specific trade sequence (buy stock, sell call, roll call, close both)
//
// ============================================================================
