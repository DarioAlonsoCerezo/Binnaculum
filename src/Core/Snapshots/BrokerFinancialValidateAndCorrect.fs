namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.SnapshotsModel
open BrokerFinancialSnapshotExtensions

module internal BrokerFinancialValidateAndCorrect =

    /// <summary>
    /// Implements SCENARIO G: Validates and corrects an existing financial snapshot to match a previous snapshot if discrepancies are found.
    /// Used for consistency checks when no movements exist but both previous and existing snapshots are present for a currency and date.
    /// </summary>
    let internal snapshotConsistency (previous: BrokerFinancialSnapshot) (existing: BrokerFinancialSnapshot) =
        task {
            // CoreLogger.logDebug "BrokerFinancialValidateAndCorrect" "Starting snapshotConsistency check"

            // CoreLogger.logDebugf
            //     "BrokerFinancialValidateAndCorrect"
            //     "Previous: Deposited=%A, MovementCounter=%A"
            //     previous.Deposited.Value
            //     previous.MovementCounter

            // CoreLogger.logDebugf
            //     "BrokerFinancialValidateAndCorrect"
            //     "Existing: Deposited=%A, MovementCounter=%A"
            //     existing.Deposited.Value
            //     existing.MovementCounter

            let snapshotsDiffer =
                previous.RealizedGains <> existing.RealizedGains
                || previous.RealizedPercentage <> existing.RealizedPercentage
                || previous.UnrealizedGains <> existing.UnrealizedGains
                || previous.UnrealizedGainsPercentage <> existing.UnrealizedGainsPercentage
                || previous.Invested <> existing.Invested
                || previous.Commissions <> existing.Commissions
                || previous.Fees <> existing.Fees
                || previous.Deposited <> existing.Deposited
                || previous.Withdrawn <> existing.Withdrawn
                || previous.DividendsReceived <> existing.DividendsReceived
                || previous.OptionsIncome <> existing.OptionsIncome
                || previous.OtherIncome <> existing.OtherIncome
                || previous.OpenTrades <> existing.OpenTrades
                || previous.MovementCounter <> existing.MovementCounter
                || previous.NetCashFlow <> existing.NetCashFlow

            if snapshotsDiffer then
                // CoreLogger.logDebug "BrokerFinancialValidateAndCorrect" "Snapshots differ - applying correction"

                let correctedSnapshot =
                    { existing with
                        RealizedGains = previous.RealizedGains
                        RealizedPercentage = previous.RealizedPercentage
                        UnrealizedGains = previous.UnrealizedGains
                        UnrealizedGainsPercentage = previous.UnrealizedGainsPercentage
                        Invested = previous.Invested
                        Commissions = previous.Commissions
                        Fees = previous.Fees
                        Deposited = previous.Deposited
                        Withdrawn = previous.Withdrawn
                        DividendsReceived = previous.DividendsReceived
                        OptionsIncome = previous.OptionsIncome
                        OtherIncome = previous.OtherIncome
                        OpenTrades = previous.OpenTrades
                        MovementCounter = previous.MovementCounter
                        NetCashFlow = previous.NetCashFlow }

                do! correctedSnapshot.save ()

            // CoreLogger.logDebugf
            //     "BrokerFinancialValidateAndCorrect"
            //     "Corrected snapshot saved - Deposited: %A, MovementCounter: %A"
            //     correctedSnapshot.Deposited.Value
            //     correctedSnapshot.MovementCounter
            else
                ()
        // CoreLogger.logDebug
        //     "BrokerFinancialValidateAndCorrect"
        //     "Snapshots are consistent - no correction needed"
        // If no difference, do nothing
        }
