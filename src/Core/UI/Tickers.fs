namespace Binnaculum.Core.UI

open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.ModelsToDatabase
open Binnaculum.Core.Storage
open TickerPriceExtensions

/// <summary>
/// This module provides the public API for all Ticker-related operations accessible from the UI layer.
/// It follows the established patterns of model validation, database persistence via conversion,
/// and follows the project's error handling conventions (let exceptions bubble up to the UI layer).
/// </summary>
module Tickers =
    
    /// <summary>
    /// Saves a TickerPrice to the database.
    /// Takes a Models.TickerPrice record, converts it to the database model, and persists it.
    /// Follows project conventions for error handling - exceptions bubble up to UI layer.
    /// </summary>
    /// <param name="tickerPrice">The TickerPrice model to save</param>
    let SaveTickerPrice(tickerPrice: Binnaculum.Core.Models.TickerPrice) = task {
        let databaseModel = tickerPrice.tickerPriceToDatabase()
        do! databaseModel.save() |> Async.AwaitTask |> Async.Ignore
    }