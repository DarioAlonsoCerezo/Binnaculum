namespace Binnaculum.Core.Storage

open Binnaculum.Core.Database.SnapshotsModel
open Binnaculum.Core.Patterns
open BrokerFinancialSnapshotExtensions

module internal BrokerFinancialCarryForward =

    /// <summary>
    /// Implements SCENARIO E: Carries forward the previous financial snapshot to a new date when no movements exist and no existing snapshot is present.
    /// Creates a new snapshot with the same values as the previous snapshot, updating the date and BrokerAccountSnapshotId.
    /// </summary>
    let internal previousSnapshot
        (targetDate: DateTimePattern)
        (brokerAccountSnapshotId: int)
        (previous: BrokerFinancialSnapshot)
        =
        task {
            let carriedSnapshot = {
                previous with
                    Base = SnapshotManagerUtils.createBaseSnapshot targetDate
                    BrokerAccountSnapshotId = brokerAccountSnapshotId
            }
            do! carriedSnapshot.save()
        }

