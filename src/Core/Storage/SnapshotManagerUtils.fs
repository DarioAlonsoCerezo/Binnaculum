namespace Binnaculum.Core.Storage

open Binnaculum.Core.Patterns
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database.SnapshotsModel

/// <summary>
/// Utility functions for snapshot managers.
/// </summary>
module internal SnapshotManagerUtils =
    /// Helper function to get the date part only from a DateTimePattern, set to end of day (23:59:59)
    let getDateOnly (dateTime: DateTimePattern) =
        let date = dateTime.Value.Date.AddDays(1).AddTicks(-1)
        DateTimePattern.FromDateTime(date)

    /// Creates a base snapshot with the given date
    let createBaseSnapshot (date: DateTimePattern) : BaseSnapshot =
        {
            Id = 0
            Date = getDateOnly date
            Audit = AuditableEntity.Default
        }
