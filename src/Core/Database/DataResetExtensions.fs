namespace Binnaculum.Core.Database

module DataResetExtensions =

    open System.Runtime.CompilerServices
    open Microsoft.Data.Sqlite
    open Binnaculum.Core.SQL
    open Binnaculum.Core.Database

    [<Extension>]
    type Do() =
        
        /// <summary>
        /// Deletes all operational data from the database while preserving reference tables.
        /// Preserves: Ticker, Currency, TickerSplit, TickerPrice
        /// Deletes: All operations, accounts, brokers, banks, movements, trades, options, dividends, snapshots, import sessions
        /// Executes within a transaction for atomicity.
        /// </summary>
        [<Extension>]
        static member deleteAllOperationalData() =
            Do.executeInTransaction(fun connection transaction ->
                task {
                    // Execute all delete statements in order
                    for deleteQuery in DataResetQuery.deleteAllOperationalData do
                        use command = connection.CreateCommand()
                        command.Transaction <- transaction
                        command.CommandText <- deleteQuery
                        do! command.ExecuteNonQueryAsync() |> Async.AwaitTask |> Async.Ignore
                    
                    return ()
                }
            )
