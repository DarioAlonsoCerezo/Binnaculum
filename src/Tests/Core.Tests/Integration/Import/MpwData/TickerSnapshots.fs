namespace Core.Tests.Integration

open System
open Binnaculum.Core.Models
open TestModels

/// <summary>
/// MPW Ticker Currency Snapshot data.
///
/// Contains all 66 expected ticker snapshots spanning from 2024-04-26 through 2025-11-07.
/// Generated from actual core calculation results.
/// </summary>
module MpwTickerSnapshots =

    /// <summary>
    /// Generate expected MPW ticker snapshots with descriptions.
    ///
    /// Includes all 66 snapshots from the MPW import test CSV file.
    /// Snapshots span from 2024-04-26 (first trade) through 2025-11-07 (today).
    /// Includes both equity shares and option contracts (calls and puts).
    /// </summary>
    let getMPWSnapshots (ticker: Ticker) (currency: Currency) : ExpectedSnapshot<TickerCurrencySnapshot> list =
        [
          // Snapshot 0: 2024-04-26
          { Data =
              { Id = 0; Date = DateOnly(2024, 4, 26)
                Ticker = ticker; Currency = currency
                TotalShares = 0.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = 0.00m
                Dividends = 0.00m; DividendTaxes = 0.00m
                Options = 15.00m; TotalIncomes = 12.73m
                CapitalDeployed = 850.00m; Realized = 0.00m
                Performance = 0.0000m; OpenTrades = true
                Commissions = 2.00m; Fees = 0.27m }
            Description = "Snapshot 0: 2024-04-26" }
          // Snapshot 1: 2024-04-29
          { Data =
              { Id = 0; Date = DateOnly(2024, 4, 29)
                Ticker = ticker; Currency = currency
                TotalShares = 0.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = 0.00m
                Dividends = 0.00m; DividendTaxes = 0.00m
                Options = 8.00m; TotalIncomes = 5.46m
                CapitalDeployed = 850.00m; Realized = 5.46m
                Performance = 0.6424m; OpenTrades = false
                Commissions = 2.00m; Fees = 0.54m }
            Description = "Snapshot 1: 2024-04-29" }
          // Snapshot 2: 2024-05-03
          { Data =
              { Id = 0; Date = DateOnly(2024, 5, 3)
                Ticker = ticker; Currency = currency
                TotalShares = 0.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = 0.00m
                Dividends = 0.00m; DividendTaxes = 0.00m
                Options = 17.00m; TotalIncomes = 12.19m
                CapitalDeployed = 1700.00m; Realized = 5.46m
                Performance = 0.3212m; OpenTrades = true
                Commissions = 4.00m; Fees = 0.81m }
            Description = "Snapshot 2: 2024-05-03" }
          // Snapshot 3: 2024-05-06
          { Data =
              { Id = 0; Date = DateOnly(2024, 5, 6)
                Ticker = ticker; Currency = currency
                TotalShares = 0.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = 0.00m
                Dividends = 0.00m; DividendTaxes = 0.00m
                Options = 68.00m; TotalIncomes = 56.41m
                CapitalDeployed = 3800.00m; Realized = 5.46m
                Performance = 0.1437m; OpenTrades = true
                Commissions = 10.00m; Fees = 1.59m }
            Description = "Snapshot 3: 2024-05-06" }
          // Snapshot 4: 2024-05-09
          { Data =
              { Id = 0; Date = DateOnly(2024, 5, 9)
                Ticker = ticker; Currency = currency
                TotalShares = 0.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = 0.00m
                Dividends = 0.00m; DividendTaxes = 0.00m
                Options = 27.00m; TotalIncomes = 10.94m
                CapitalDeployed = 4900.00m; Realized = -9.65m
                Performance = -0.1969m; OpenTrades = true
                Commissions = 13.00m; Fees = 3.06m }
            Description = "Snapshot 4: 2024-05-09" }
          // Snapshot 5: 2024-05-10
          { Data =
              { Id = 0; Date = DateOnly(2024, 5, 10)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.19m
                Dividends = 0.00m; DividendTaxes = 0.00m
                Options = 211.00m; TotalIncomes = 192.67m
                CapitalDeployed = 5900.00m; Realized = -9.65m
                Performance = -0.1636m; OpenTrades = true
                Commissions = 15.00m; Fees = 3.33m }
            Description = "Snapshot 5: 2024-05-10" }
          // Snapshot 6: 2024-05-13
          { Data =
              { Id = 0; Date = DateOnly(2024, 5, 13)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.05m
                Dividends = 0.00m; DividendTaxes = 0.00m
                Options = 65.00m; TotalIncomes = 46.03m
                CapitalDeployed = 5900.00m; Realized = 46.03m
                Performance = 0.7802m; OpenTrades = true
                Commissions = 15.00m; Fees = 3.97m }
            Description = "Snapshot 6: 2024-05-13" }
          // Snapshot 7: 2024-05-15
          { Data =
              { Id = 0; Date = DateOnly(2024, 5, 15)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.10m
                Dividends = 0.00m; DividendTaxes = 0.00m
                Options = 132.00m; TotalIncomes = 103.94m
                CapitalDeployed = 5900.00m; Realized = 46.03m
                Performance = 0.7802m; OpenTrades = true
                Commissions = 23.00m; Fees = 5.06m }
            Description = "Snapshot 7: 2024-05-15" }
          // Snapshot 8: 2024-05-16
          { Data =
              { Id = 0; Date = DateOnly(2024, 5, 16)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.17m
                Dividends = 0.00m; DividendTaxes = 0.00m
                Options = 200.00m; TotalIncomes = 169.66m
                CapitalDeployed = 5900.00m; Realized = 46.03m
                Performance = 0.7802m; OpenTrades = true
                Commissions = 25.00m; Fees = 5.34m }
            Description = "Snapshot 8: 2024-05-16" }
          // Snapshot 9: 2024-05-20
          { Data =
              { Id = 0; Date = DateOnly(2024, 5, 20)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.18m
                Dividends = 0.00m; DividendTaxes = 0.00m
                Options = 223.00m; TotalIncomes = 182.57m
                CapitalDeployed = 5900.00m; Realized = 85.93m
                Performance = 1.4564m; OpenTrades = true
                Commissions = 33.00m; Fees = 7.43m }
            Description = "Snapshot 9: 2024-05-20" }
          // Snapshot 10: 2024-05-23
          { Data =
              { Id = 0; Date = DateOnly(2024, 5, 23)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.28m
                Dividends = 0.00m; DividendTaxes = 0.00m
                Options = 335.00m; TotalIncomes = 282.00m
                CapitalDeployed = 5900.00m; Realized = 165.31m
                Performance = 2.8019m; OpenTrades = true
                Commissions = 43.00m; Fees = 10.00m }
            Description = "Snapshot 10: 2024-05-23" }
          // Snapshot 11: 2024-05-31
          { Data =
              { Id = 0; Date = DateOnly(2024, 5, 31)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.44m
                Dividends = 0.00m; DividendTaxes = 0.00m
                Options = 505.00m; TotalIncomes = 439.42m
                CapitalDeployed = 5900.00m; Realized = 140.73m
                Performance = 2.3853m; OpenTrades = true
                Commissions = 53.00m; Fees = 12.58m }
            Description = "Snapshot 11: 2024-05-31" }
          // Snapshot 12: 2024-06-03
          { Data =
              { Id = 0; Date = DateOnly(2024, 6, 3)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.63m
                Dividends = 0.00m; DividendTaxes = 0.00m
                Options = 705.00m; TotalIncomes = 626.82m
                CapitalDeployed = 5900.00m; Realized = -141.85m
                Performance = -2.4042m; OpenTrades = true
                Commissions = 63.00m; Fees = 15.18m }
            Description = "Snapshot 12: 2024-06-03" }
          // Snapshot 13: 2024-06-14
          { Data =
              { Id = 0; Date = DateOnly(2024, 6, 14)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.86m
                Dividends = 0.00m; DividendTaxes = 0.00m
                Options = 965.00m; TotalIncomes = 861.65m
                CapitalDeployed = 18900.00m; Realized = -141.85m
                Performance = -0.7505m; OpenTrades = true
                Commissions = 83.00m; Fees = 20.35m }
            Description = "Snapshot 13: 2024-06-14" }
          // Snapshot 14: 2024-07-01
          { Data =
              { Id = 0; Date = DateOnly(2024, 7, 1)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -1.08m
                Dividends = 0.00m; DividendTaxes = 0.00m
                Options = 1195.00m; TotalIncomes = 1079.06m
                CapitalDeployed = 18900.00m; Realized = 345.55m
                Performance = 1.8283m; OpenTrades = true
                Commissions = 93.00m; Fees = 22.94m }
            Description = "Snapshot 14: 2024-07-01" }
          // Snapshot 15: 2024-07-02
          { Data =
              { Id = 0; Date = DateOnly(2024, 7, 2)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -1.25m
                Dividends = 0.00m; DividendTaxes = 0.00m
                Options = 1375.00m; TotalIncomes = 1246.47m
                CapitalDeployed = 18900.00m; Realized = 352.96m
                Performance = 1.8675m; OpenTrades = true
                Commissions = 103.00m; Fees = 25.53m }
            Description = "Snapshot 15: 2024-07-02" }
          // Snapshot 16: 2024-07-09
          { Data =
              { Id = 0; Date = DateOnly(2024, 7, 9)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -1.37m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 1375.00m; TotalIncomes = 1373.97m
                CapitalDeployed = 18900.00m; Realized = 352.96m
                Performance = 1.8675m; OpenTrades = true
                Commissions = 103.00m; Fees = 25.53m }
            Description = "Snapshot 16: 2024-07-09" }
          // Snapshot 17: 2024-07-16
          { Data =
              { Id = 0; Date = DateOnly(2024, 7, 16)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -1.08m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 1096.00m; TotalIncomes = 1078.96m
                CapitalDeployed = 20050.00m; Realized = -119.63m
                Performance = -0.5967m; OpenTrades = true
                Commissions = 116.00m; Fees = 28.54m }
            Description = "Snapshot 17: 2024-07-16" }
          // Snapshot 18: 2024-07-24
          { Data =
              { Id = 0; Date = DateOnly(2024, 7, 24)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.71m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 744.00m; TotalIncomes = 714.10m
                CapitalDeployed = 20050.00m; Realized = -386.76m
                Performance = -1.9290m; OpenTrades = true
                Commissions = 126.00m; Fees = 31.40m }
            Description = "Snapshot 18: 2024-07-24" }
          // Snapshot 19: 2024-07-31
          { Data =
              { Id = 0; Date = DateOnly(2024, 7, 31)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.75m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 784.00m; TotalIncomes = 750.69m
                CapitalDeployed = 21400.00m; Realized = -386.76m
                Performance = -1.8073m; OpenTrades = true
                Commissions = 129.00m; Fees = 31.81m }
            Description = "Snapshot 19: 2024-07-31" }
          // Snapshot 20: 2024-08-01
          { Data =
              { Id = 0; Date = DateOnly(2024, 8, 1)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.77m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 805.00m; TotalIncomes = 770.42m
                CapitalDeployed = 21850.00m; Realized = -387.03m
                Performance = -1.7713m; OpenTrades = true
                Commissions = 130.00m; Fees = 32.08m }
            Description = "Snapshot 20: 2024-08-01" }
          // Snapshot 21: 2024-08-02
          { Data =
              { Id = 0; Date = DateOnly(2024, 8, 2)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.79m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 828.00m; TotalIncomes = 792.15m
                CapitalDeployed = 22300.00m; Realized = -387.29m
                Performance = -1.7367m; OpenTrades = true
                Commissions = 131.00m; Fees = 32.35m }
            Description = "Snapshot 21: 2024-08-02" }
          // Snapshot 22: 2024-08-06
          { Data =
              { Id = 0; Date = DateOnly(2024, 8, 6)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.75m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 786.00m; TotalIncomes = 749.77m
                CapitalDeployed = 22300.00m; Realized = -351.09m
                Performance = -1.5744m; OpenTrades = true
                Commissions = 131.00m; Fees = 32.73m }
            Description = "Snapshot 22: 2024-08-06" }
          // Snapshot 23: 2024-08-15
          { Data =
              { Id = 0; Date = DateOnly(2024, 8, 15)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -1.08m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 1144.00m; TotalIncomes = 1080.60m
                CapitalDeployed = 22300.00m; Realized = -150.16m
                Performance = -0.6733m; OpenTrades = true
                Commissions = 151.00m; Fees = 39.90m }
            Description = "Snapshot 23: 2024-08-15" }
          // Snapshot 24: 2024-08-16
          { Data =
              { Id = 0; Date = DateOnly(2024, 8, 16)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.88m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 949.00m; TotalIncomes = 884.34m
                CapitalDeployed = 22300.00m; Realized = -122.74m
                Performance = -0.5504m; OpenTrades = true
                Commissions = 151.00m; Fees = 41.16m }
            Description = "Snapshot 24: 2024-08-16" }
          // Snapshot 25: 2024-08-19
          { Data =
              { Id = 0; Date = DateOnly(2024, 8, 19)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.84m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 910.00m; TotalIncomes = 840.68m
                CapitalDeployed = 23650.00m; Realized = -45.32m
                Performance = -0.1916m; OpenTrades = true
                Commissions = 154.00m; Fees = 42.82m }
            Description = "Snapshot 25: 2024-08-19" }
          // Snapshot 26: 2024-08-20
          { Data =
              { Id = 0; Date = DateOnly(2024, 8, 20)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.91m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 986.00m; TotalIncomes = 905.18m
                CapitalDeployed = 23650.00m; Realized = -42.08m
                Performance = -0.1779m; OpenTrades = true
                Commissions = 164.00m; Fees = 44.32m }
            Description = "Snapshot 26: 2024-08-20" }
          // Snapshot 27: 2024-08-22
          { Data =
              { Id = 0; Date = DateOnly(2024, 8, 22)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.90m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 980.00m; TotalIncomes = 898.93m
                CapitalDeployed = 23650.00m; Realized = -38.61m
                Performance = -0.1632m; OpenTrades = true
                Commissions = 164.00m; Fees = 44.57m }
            Description = "Snapshot 27: 2024-08-22" }
          // Snapshot 28: 2024-08-23
          { Data =
              { Id = 0; Date = DateOnly(2024, 8, 23)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.91m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 994.00m; TotalIncomes = 911.79m
                CapitalDeployed = 23650.00m; Realized = -38.61m
                Performance = -0.1632m; OpenTrades = true
                Commissions = 165.00m; Fees = 44.71m }
            Description = "Snapshot 28: 2024-08-23" }
          // Snapshot 29: 2024-08-26
          { Data =
              { Id = 0; Date = DateOnly(2024, 8, 26)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.87m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 952.00m; TotalIncomes = 869.41m
                CapitalDeployed = 23650.00m; Realized = -48.39m
                Performance = -0.2046m; OpenTrades = true
                Commissions = 165.00m; Fees = 45.09m }
            Description = "Snapshot 29: 2024-08-26" }
          // Snapshot 30: 2024-08-29
          { Data =
              { Id = 0; Date = DateOnly(2024, 8, 29)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.84m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 926.00m; TotalIncomes = 842.41m
                CapitalDeployed = 23650.00m; Realized = 21.52m
                Performance = 0.0910m; OpenTrades = true
                Commissions = 165.00m; Fees = 46.09m }
            Description = "Snapshot 30: 2024-08-29" }
          // Snapshot 31: 2024-08-30
          { Data =
              { Id = 0; Date = DateOnly(2024, 8, 30)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.83m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 917.00m; TotalIncomes = 833.28m
                CapitalDeployed = 23650.00m; Realized = 25.25m
                Performance = 0.1068m; OpenTrades = true
                Commissions = 165.00m; Fees = 46.22m }
            Description = "Snapshot 31: 2024-08-30" }
          // Snapshot 32: 2024-09-03
          { Data =
              { Id = 0; Date = DateOnly(2024, 9, 3)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.86m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 953.00m; TotalIncomes = 862.50m
                CapitalDeployed = 26200.00m; Realized = 25.25m
                Performance = 0.0964m; OpenTrades = true
                Commissions = 171.00m; Fees = 47.00m }
            Description = "Snapshot 32: 2024-09-03" }
          // Snapshot 33: 2024-09-04
          { Data =
              { Id = 0; Date = DateOnly(2024, 9, 4)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.68m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 773.00m; TotalIncomes = 681.86m
                CapitalDeployed = 26200.00m; Realized = 71.46m
                Performance = 0.2727m; OpenTrades = true
                Commissions = 171.00m; Fees = 47.64m }
            Description = "Snapshot 33: 2024-09-04" }
          // Snapshot 34: 2024-09-06
          { Data =
              { Id = 0; Date = DateOnly(2024, 9, 6)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.71m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 803.00m; TotalIncomes = 707.30m
                CapitalDeployed = 26200.00m; Realized = 71.46m
                Performance = 0.2727m; OpenTrades = true
                Commissions = 175.00m; Fees = 48.20m }
            Description = "Snapshot 34: 2024-09-06" }
          // Snapshot 35: 2024-09-10
          { Data =
              { Id = 0; Date = DateOnly(2024, 9, 10)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.36m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 453.00m; TotalIncomes = 356.01m
                CapitalDeployed = 26200.00m; Realized = 173.85m
                Performance = 0.6635m; OpenTrades = true
                Commissions = 175.00m; Fees = 49.49m }
            Description = "Snapshot 35: 2024-09-10" }
          // Snapshot 36: 2024-09-12
          { Data =
              { Id = 0; Date = DateOnly(2024, 9, 12)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.37m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 473.00m; TotalIncomes = 372.33m
                CapitalDeployed = 26200.00m; Realized = 43.31m
                Performance = 0.1653m; OpenTrades = true
                Commissions = 178.00m; Fees = 50.17m }
            Description = "Snapshot 36: 2024-09-12" }
          // Snapshot 37: 2024-09-13
          { Data =
              { Id = 0; Date = DateOnly(2024, 9, 13)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.37m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 473.00m; TotalIncomes = 372.33m
                CapitalDeployed = 26200.00m; Realized = 87.91m
                Performance = 0.3355m; OpenTrades = true
                Commissions = 178.00m; Fees = 50.17m }
            Description = "Snapshot 37: 2024-09-13" }
          // Snapshot 38: 2024-09-16
          { Data =
              { Id = 0; Date = DateOnly(2024, 9, 16)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.41m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 512.00m; TotalIncomes = 409.05m
                CapitalDeployed = 26750.00m; Realized = 87.91m
                Performance = 0.3286m; OpenTrades = true
                Commissions = 180.00m; Fees = 50.45m }
            Description = "Snapshot 38: 2024-09-16" }
          // Snapshot 39: 2024-09-17
          { Data =
              { Id = 0; Date = DateOnly(2024, 9, 17)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.45m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 556.00m; TotalIncomes = 447.97m
                CapitalDeployed = 26750.00m; Realized = -235.17m
                Performance = -0.8791m; OpenTrades = true
                Commissions = 184.00m; Fees = 51.53m }
            Description = "Snapshot 39: 2024-09-17" }
          // Snapshot 40: 2024-09-24
          { Data =
              { Id = 0; Date = DateOnly(2024, 9, 24)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.48m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 596.00m; TotalIncomes = 483.41m
                CapitalDeployed = 26750.00m; Realized = -235.17m
                Performance = -0.8791m; OpenTrades = true
                Commissions = 188.00m; Fees = 52.09m }
            Description = "Snapshot 40: 2024-09-24" }
          // Snapshot 41: 2024-09-25
          { Data =
              { Id = 0; Date = DateOnly(2024, 9, 25)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.44m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 560.00m; TotalIncomes = 442.33m
                CapitalDeployed = 26750.00m; Realized = -64.25m
                Performance = -0.2402m; OpenTrades = true
                Commissions = 192.00m; Fees = 53.17m }
            Description = "Snapshot 41: 2024-09-25" }
          // Snapshot 42: 2024-10-02
          { Data =
              { Id = 0; Date = DateOnly(2024, 10, 2)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.41m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 528.00m; TotalIncomes = 410.07m
                CapitalDeployed = 26750.00m; Realized = -59.79m
                Performance = -0.2235m; OpenTrades = true
                Commissions = 192.00m; Fees = 53.43m }
            Description = "Snapshot 42: 2024-10-02" }
          // Snapshot 43: 2024-10-04
          { Data =
              { Id = 0; Date = DateOnly(2024, 10, 4)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.41m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 528.00m; TotalIncomes = 410.07m
                CapitalDeployed = 26750.00m; Realized = -24.35m
                Performance = -0.0910m; OpenTrades = true
                Commissions = 192.00m; Fees = 53.43m }
            Description = "Snapshot 43: 2024-10-04" }
          // Snapshot 44: 2024-10-07
          { Data =
              { Id = 0; Date = DateOnly(2024, 10, 7)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.34m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 462.00m; TotalIncomes = 343.68m
                CapitalDeployed = 26750.00m; Realized = 47.84m
                Performance = 0.1788m; OpenTrades = true
                Commissions = 192.00m; Fees = 53.82m }
            Description = "Snapshot 44: 2024-10-07" }
          // Snapshot 45: 2024-10-08
          { Data =
              { Id = 0; Date = DateOnly(2024, 10, 8)
                Ticker = ticker; Currency = currency
                TotalShares = 1000.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = -0.24m
                Dividends = 150.00m; DividendTaxes = 22.50m
                Options = 360.00m; TotalIncomes = 241.42m
                CapitalDeployed = 26750.00m; Realized = 129.30m
                Performance = 0.4834m; OpenTrades = true
                Commissions = 192.00m; Fees = 54.08m }
            Description = "Snapshot 45: 2024-10-08" }
          // Snapshot 46: 2024-10-10
          { Data =
              { Id = 0; Date = DateOnly(2024, 10, 10)
                Ticker = ticker; Currency = currency
                TotalShares = 0.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = 0.00m
                Dividends = 230.00m; DividendTaxes = 34.50m
                Options = 360.00m; TotalIncomes = 308.31m
                CapitalDeployed = 26750.00m; Realized = 5078.19m
                Performance = 18.9839m; OpenTrades = false
                Commissions = 192.00m; Fees = 55.19m }
            Description = "Snapshot 46: 2024-10-10" }
          // Snapshot 47: 2024-10-24
          { Data =
              { Id = 0; Date = DateOnly(2024, 10, 24)
                Ticker = ticker; Currency = currency
                TotalShares = 0.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = 0.00m
                Dividends = 230.00m; DividendTaxes = 34.50m
                Options = -740.00m; TotalIncomes = -797.33m
                CapitalDeployed = 28250.00m; Realized = 5078.19m
                Performance = 17.9759m; OpenTrades = true
                Commissions = 197.00m; Fees = 55.83m }
            Description = "Snapshot 47: 2024-10-24" }
          // Snapshot 48: 2024-11-01
          { Data =
              { Id = 0; Date = DateOnly(2024, 11, 1)
                Ticker = ticker; Currency = currency
                TotalShares = 0.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = 0.00m
                Dividends = 230.00m; DividendTaxes = 34.50m
                Options = -719.00m; TotalIncomes = -777.47m
                CapitalDeployed = 28700.00m; Realized = 5078.19m
                Performance = 17.6940m; OpenTrades = true
                Commissions = 198.00m; Fees = 55.97m }
            Description = "Snapshot 48: 2024-11-01" }
          // Snapshot 49: 2024-11-05
          { Data =
              { Id = 0; Date = DateOnly(2024, 11, 5)
                Ticker = ticker; Currency = currency
                TotalShares = 0.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = 0.00m
                Dividends = 230.00m; DividendTaxes = 34.50m
                Options = -736.00m; TotalIncomes = -794.60m
                CapitalDeployed = 28700.00m; Realized = 5080.92m
                Performance = 17.7036m; OpenTrades = true
                Commissions = 198.00m; Fees = 56.10m }
            Description = "Snapshot 49: 2024-11-05" }
          // Snapshot 50: 2024-11-07
          { Data =
              { Id = 0; Date = DateOnly(2024, 11, 7)
                Ticker = ticker; Currency = currency
                TotalShares = 0.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = 0.00m
                Dividends = 230.00m; DividendTaxes = 34.50m
                Options = 144.00m; TotalIncomes = 84.73m
                CapitalDeployed = 28700.00m; Realized = 4854.61m
                Performance = 16.9150m; OpenTrades = false
                Commissions = 198.00m; Fees = 56.77m }
            Description = "Snapshot 50: 2024-11-07" }
          // Snapshot 51: 2024-12-13
          { Data =
              { Id = 0; Date = DateOnly(2024, 12, 13)
                Ticker = ticker; Currency = currency
                TotalShares = 0.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = 0.00m
                Dividends = 230.00m; DividendTaxes = 34.50m
                Options = 14.00m; TotalIncomes = -47.52m
                CapitalDeployed = 29000.00m; Realized = 4854.61m
                Performance = 16.7400m; OpenTrades = true
                Commissions = 200.00m; Fees = 57.02m }
            Description = "Snapshot 51: 2024-12-13" }
          // Snapshot 52: 2024-12-17
          { Data =
              { Id = 0; Date = DateOnly(2024, 12, 17)
                Ticker = ticker; Currency = currency
                TotalShares = 0.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = 0.00m
                Dividends = 230.00m; DividendTaxes = 34.50m
                Options = 40.00m; TotalIncomes = -26.02m
                CapitalDeployed = 30100.00m; Realized = 4854.61m
                Performance = 16.1283m; OpenTrades = true
                Commissions = 204.00m; Fees = 57.52m }
            Description = "Snapshot 52: 2024-12-17" }
          // Snapshot 53: 2025-01-17
          { Data =
              { Id = 0; Date = DateOnly(2025, 1, 17)
                Ticker = ticker; Currency = currency
                TotalShares = 0.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = 0.00m
                Dividends = 230.00m; DividendTaxes = 34.50m
                Options = 62.00m; TotalIncomes = -5.29m
                CapitalDeployed = 30100.00m; Realized = 4878.35m
                Performance = 16.2071m; OpenTrades = true
                Commissions = 205.00m; Fees = 57.79m }
            Description = "Snapshot 53: 2025-01-17" }
          // Snapshot 54: 2025-01-21
          { Data =
              { Id = 0; Date = DateOnly(2025, 1, 21)
                Ticker = ticker; Currency = currency
                TotalShares = 0.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = 0.00m
                Dividends = 230.00m; DividendTaxes = 34.50m
                Options = 172.00m; TotalIncomes = 103.90m
                CapitalDeployed = 30100.00m; Realized = 4873.78m
                Performance = 16.1920m; OpenTrades = false
                Commissions = 205.00m; Fees = 58.60m }
            Description = "Snapshot 54: 2025-01-21" }
          // Snapshot 55: 2025-02-18
          { Data =
              { Id = 0; Date = DateOnly(2025, 2, 18)
                Ticker = ticker; Currency = currency
                TotalShares = 0.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = 0.00m
                Dividends = 230.00m; DividendTaxes = 34.50m
                Options = -138.00m; TotalIncomes = -207.23m
                CapitalDeployed = 30300.00m; Realized = 4873.78m
                Performance = 16.0851m; OpenTrades = true
                Commissions = 206.00m; Fees = 58.73m }
            Description = "Snapshot 55: 2025-02-18" }
          // Snapshot 56: 2025-02-20
          { Data =
              { Id = 0; Date = DateOnly(2025, 2, 20)
                Ticker = ticker; Currency = currency
                TotalShares = 0.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = 0.00m
                Dividends = 230.00m; DividendTaxes = 34.50m
                Options = -111.00m; TotalIncomes = -181.37m
                CapitalDeployed = 30300.00m; Realized = 4873.78m
                Performance = 16.0851m; OpenTrades = true
                Commissions = 207.00m; Fees = 58.87m }
            Description = "Snapshot 56: 2025-02-20" }
          // Snapshot 57: 2025-02-28
          { Data =
              { Id = 0; Date = DateOnly(2025, 2, 28)
                Ticker = ticker; Currency = currency
                TotalShares = 0.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = 0.00m
                Dividends = 230.00m; DividendTaxes = 34.50m
                Options = -198.00m; TotalIncomes = -268.50m
                CapitalDeployed = 30300.00m; Realized = 4812.51m
                Performance = 15.8829m; OpenTrades = true
                Commissions = 207.00m; Fees = 59.00m }
            Description = "Snapshot 57: 2025-02-28" }
          // Snapshot 58: 2025-03-07
          { Data =
              { Id = 0; Date = DateOnly(2025, 3, 7)
                Ticker = ticker; Currency = currency
                TotalShares = 0.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = 0.00m
                Dividends = 230.00m; DividendTaxes = 34.50m
                Options = -168.00m; TotalIncomes = -239.64m
                CapitalDeployed = 30300.00m; Realized = 4812.51m
                Performance = 15.8829m; OpenTrades = true
                Commissions = 208.00m; Fees = 59.14m }
            Description = "Snapshot 58: 2025-03-07" }
          // Snapshot 59: 2025-03-27
          { Data =
              { Id = 0; Date = DateOnly(2025, 3, 27)
                Ticker = ticker; Currency = currency
                TotalShares = 0.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = 0.00m
                Dividends = 230.00m; DividendTaxes = 34.50m
                Options = 217.00m; TotalIncomes = 145.08m
                CapitalDeployed = 30300.00m; Realized = 4914.96m
                Performance = 16.2210m; OpenTrades = false
                Commissions = 208.00m; Fees = 59.42m }
            Description = "Snapshot 59: 2025-03-27" }
          // Snapshot 60: 2025-08-26
          { Data =
              { Id = 0; Date = DateOnly(2025, 8, 26)
                Ticker = ticker; Currency = currency
                TotalShares = 0.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = 0.00m
                Dividends = 230.00m; DividendTaxes = 34.50m
                Options = -251.00m; TotalIncomes = -326.29m
                CapitalDeployed = 31200.00m; Realized = 4914.96m
                Performance = 15.7531m; OpenTrades = true
                Commissions = 211.00m; Fees = 59.79m }
            Description = "Snapshot 60: 2025-08-26" }
          // Snapshot 61: 2025-09-18
          { Data =
              { Id = 0; Date = DateOnly(2025, 9, 18)
                Ticker = ticker; Currency = currency
                TotalShares = 0.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = 0.00m
                Dividends = 230.00m; DividendTaxes = 34.50m
                Options = -203.00m; TotalIncomes = -281.65m
                CapitalDeployed = 31200.00m; Realized = 4914.96m
                Performance = 15.7531m; OpenTrades = true
                Commissions = 214.00m; Fees = 60.15m }
            Description = "Snapshot 61: 2025-09-18" }
          // Snapshot 62: 2025-09-26
          { Data =
              { Id = 0; Date = DateOnly(2025, 9, 26)
                Ticker = ticker; Currency = currency
                TotalShares = 0.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = 0.00m
                Dividends = 230.00m; DividendTaxes = 34.50m
                Options = -203.00m; TotalIncomes = -281.65m
                CapitalDeployed = 31200.00m; Realized = 4959.60m
                Performance = 15.8962m; OpenTrades = true
                Commissions = 214.00m; Fees = 60.15m }
            Description = "Snapshot 62: 2025-09-26" }
          // Snapshot 63: 2025-10-01
          { Data =
              { Id = 0; Date = DateOnly(2025, 10, 1)
                Ticker = ticker; Currency = currency
                TotalShares = 0.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = 0.00m
                Dividends = 230.00m; DividendTaxes = 34.50m
                Options = -173.00m; TotalIncomes = -255.02m
                CapitalDeployed = 31200.00m; Realized = 4959.60m
                Performance = 15.8962m; OpenTrades = true
                Commissions = 217.00m; Fees = 60.52m }
            Description = "Snapshot 63: 2025-10-01" }
          // Snapshot 64: 2025-10-17
          { Data =
              { Id = 0; Date = DateOnly(2025, 10, 17)
                Ticker = ticker; Currency = currency
                TotalShares = 0.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = 0.00m
                Dividends = 230.00m; DividendTaxes = 34.50m
                Options = -173.00m; TotalIncomes = -255.02m
                CapitalDeployed = 31200.00m; Realized = 4986.23m
                Performance = 15.9815m; OpenTrades = true
                Commissions = 217.00m; Fees = 60.52m }
            Description = "Snapshot 64: 2025-10-17" }
          // Snapshot 65: 2025-11-07
          { Data =
              { Id = 0; Date = DateOnly(2025, 11, 7)
                Ticker = ticker; Currency = currency
                TotalShares = 0.00m; Weight = 0.0000m
                CostBasis = 0.00m; RealCost = 0.00m
                Dividends = 230.00m; DividendTaxes = 34.50m
                Options = -173.00m; TotalIncomes = -255.02m
                CapitalDeployed = 31200.00m; Realized = 4986.23m
                Performance = 15.9815m; OpenTrades = true
                Commissions = 217.00m; Fees = 60.52m }
            Description = "Snapshot 65: 2025-11-07" }
        ]
