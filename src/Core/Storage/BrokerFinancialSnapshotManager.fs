namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open Binnaculum.Core.Storage.SnapshotManagerUtils

module internal BrokerFinancialSnapshotManager =
    /// <summary>
    /// Helper to calculate BrokerFinancialSnapshots for a set of movements and a snapshot date
    /// </summary>
    let private calculateFinancialSnapshots (movements: BrokerMovement list) (snapshotDate: DateTimePattern) (getIds: int -> int * int) =
        async {
            let! currencyOpts =
                movements
                |> List.map (fun m -> CurrencyExtensions.Do.getById m.CurrencyId |> Async.AwaitTask)
                |> Async.Parallel
            let zipped = List.zip movements (currencyOpts |> Array.toList)
            let filtered = zipped |> List.choose (fun (m, cOpt) -> cOpt |> Option.map (fun c -> (m, c)))
            let byCurrency =
                filtered
                |> List.groupBy (fun (_, c) -> c.Id)
            // Refactor: async per currency group
            let! results =
                byCurrency
                |> List.map (fun (currencyId, ms) -> async {
                    let msMovements = ms |> List.map fst
                    let movementCounter = msMovements.Length
                    let invested = msMovements |> List.sumBy (fun m -> match m.MovementType with | BrokerMovementType.Deposit -> m.Amount.Value | BrokerMovementType.Withdrawal -> -m.Amount.Value | _ -> 0m) |> Money.FromAmount
                    let commissions = msMovements |> List.sumBy (fun m -> m.Commissions.Value) |> Money.FromAmount
                    let fees = msMovements |> List.sumBy (fun m -> m.Fees.Value) |> Money.FromAmount
                    let deposited = msMovements |> List.sumBy (fun m -> match m.MovementType with | BrokerMovementType.Deposit -> m.Amount.Value | _ -> 0m) |> Money.FromAmount
                    let withdrawn = msMovements |> List.sumBy (fun m -> match m.MovementType with | BrokerMovementType.Withdrawal -> m.Amount.Value | _ -> 0m) |> Money.FromAmount
                    let dividends = msMovements |> List.sumBy (fun m -> match m.MovementType with | BrokerMovementType.InterestsGained -> m.Amount.Value | _ -> 0m) |> Money.FromAmount
                    let optionsIncome = 0m |> Money.FromAmount // Placeholder
                    let otherIncome = 0m |> Money.FromAmount // Placeholder
                    let brokerId, brokerAccountId = getIds currencyId
                    // Fetch previous snapshot for this group
                    let! prevSnapshot =
                        if brokerAccountId <> -1 then
                            BrokerFinancialSnapshotExtensions.Do.getLatestByBrokerAccountId brokerAccountId |> Async.AwaitTask
                        elif brokerId <> -1 then
                            BrokerFinancialSnapshotExtensions.Do.getLatestByBrokerId brokerId |> Async.AwaitTask
                        else
                            async { return None }
                    // Use prevSnapshot for future calculations (currently placeholders)
                    let realizedGains = 0m |> Money.FromAmount // TODO: Use prevSnapshot
                    let realizedPct = 0m // TODO: Use prevSnapshot
                    let unrealizedGains = 0m |> Money.FromAmount // TODO: Use prevSnapshot
                    let unrealizedPct = 0m // TODO: Use prevSnapshot
                    let openTrades = false // TODO: Use prevSnapshot
                    return {
                        Base = SnapshotManagerUtils.createBaseSnapshot snapshotDate
                        BrokerId = brokerId
                        BrokerAccountId = brokerAccountId
                        CurrencyId = currencyId
                        MovementCounter = movementCounter
                        RealizedGains = realizedGains
                        RealizedPercentage = realizedPct
                        UnrealizedGains = unrealizedGains
                        UnrealizedGainsPercentage = unrealizedPct
                        Invested = invested
                        Commissions = commissions
                        Fees = fees
                        Deposited = deposited
                        Withdrawn = withdrawn
                        DividendsReceived = dividends
                        OptionsIncome = optionsIncome
                        OtherIncome = otherIncome
                        OpenTrades = openTrades
                    }
                })
                |> Async.Parallel
            return results |> Array.toList
        }

    /// <summary>
    /// Calculates all BrokerFinancialSnapshots for a specific broker account and date (grouped by currency)
    /// </summary>
    let calculateForBrokerAccount (brokerAccountId: int) (date: DateTimePattern) =
        async {
            let snapshotDate = getDateOnly date
            let! movements = BrokerMovementExtensions.Do.getByBrokerAccountIdAndDateRange(brokerAccountId, snapshotDate) |> Async.AwaitTask
            return! calculateFinancialSnapshots movements snapshotDate (fun _ -> (-1, brokerAccountId))
        }

    /// <summary>
    /// Calculates all BrokerFinancialSnapshots for a specific broker and date (aggregates all accounts, grouped by currency)
    /// </summary>
    let calculateForBroker (brokerId: int) (date: DateTimePattern) =
        async {
            let snapshotDate = getDateOnly date
            let! movements = BrokerMovementExtensions.Do.getByBrokerAccountIdAndDateRange(brokerId, snapshotDate) |> Async.AwaitTask
            return! calculateFinancialSnapshots movements snapshotDate (fun _ -> (brokerId, -1))
        }

