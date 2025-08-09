namespace Binnaculum.Core.Storage

open Binnaculum.Core.Patterns
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Database.SnapshotsModel
open Microsoft.Maui.Storage
open Binnaculum.Core.Keys
open System

/// <summary>
/// Utility functions for snapshot managers.
/// </summary>
module internal SnapshotManagerUtils =
    /// Helper function to get the date part only from a DateTimePattern, set to end of day (23:59:59)
    let getDateOnly (dateTime: DateTimePattern) =
        let date = dateTime.Value.Date.AddDays(1).AddTicks(-1)
        DateTimePattern.FromDateTime(date)

    /// Helper function to get the date part only from a DateTimePattern, set to start of day (00:00:01)
    /// This is used when retrieving movements FROM a specific date to ensure we capture all movements
    /// throughout the entire day, starting from the very beginning of that day.
    let getDateOnlyStartOfDay (dateTime: DateTimePattern) =
        let date = dateTime.Value.Date.AddSeconds(1.0) // 00:00:01
        DateTimePattern.FromDateTime(date)

    let getDateOnlyFromDateTime (dateTime: DateTime) =
        let pattern = DateTimePattern.FromDateTime(dateTime)
        getDateOnly pattern

    /// Helper function to get start of day from a DateTime, set to 00:00:01
    /// This is used when retrieving movements FROM a specific date to ensure we capture all movements
    /// throughout the entire day, starting from the very beginning of that day.
    let getDateOnlyStartOfDayFromDateTime (dateTime: DateTime) =
        let pattern = DateTimePattern.FromDateTime(dateTime)
        getDateOnlyStartOfDay pattern

    /// Creates a base snapshot with the given date
    let createBaseSnapshot (date: DateTimePattern) : BaseSnapshot =
        {
            Id = 0
            Date = getDateOnly date
            Audit = AuditableEntity.Default
        }

    let getDefaultCurrency() = task {
        let preferenceCurrency = Preferences.Get(CurrencyKey, DefaultCurrency)
        let! defaultCurrency = CurrencyExtensions.Do.getByCode(preferenceCurrency)
        match defaultCurrency with
        | Some currency -> return currency.Id
        | None -> 
            failwithf "Default currency %s not found and no fallback currency available" preferenceCurrency
            return -1
    }
