namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName

module internal DataResetQuery =
    /// <summary>
    /// Deletes all operational data while preserving reference tables:
    /// - Preserves: Ticker, Currency, TickerSplit, TickerPrice
    /// - Deletes: All operational data (trades, accounts, movements, snapshots, etc.)
    /// Must be executed in correct FK order within a transaction.
    /// </summary>
    let deleteAllOperationalData =
        [
            // Delete import session data first (no FK dependencies)
            $"DELETE FROM {ImportSessionChunks};"
            $"DELETE FROM {ImportSessions};"
            
            // Delete auto import operations and trades (references operational data)
            $"DELETE FROM {AutoImportOperationTrades};"
            $"DELETE FROM {AutoImportOperations};"
            
            // Delete snapshots (reference operational data)
            $"DELETE FROM {InvestmentOverviewSnapshots};"
            $"DELETE FROM {BankSnapshots};"
            $"DELETE FROM {BankAccountSnapshots};"
            $"DELETE FROM {BrokerFinancialSnapshots};"
            $"DELETE FROM {BrokerSnapshots};"
            $"DELETE FROM {BrokerAccountSnapshots};"
            $"DELETE FROM {TickerCurrencySnapshots};"
            $"DELETE FROM {TickerSnapshots};"
            
            // Delete transactions (trades, options, dividends, movements)
            $"DELETE FROM {Options};"
            $"DELETE FROM {DividendDates};"
            $"DELETE FROM {DividendTaxes};"
            $"DELETE FROM {Dividends};"
            $"DELETE FROM {Trades};"
            
            // Delete movements
            $"DELETE FROM {BankAccountMovements};"
            $"DELETE FROM {BrokerMovements};"
            
            // Delete accounts (reference brokers/banks)
            $"DELETE FROM {BankAccounts};"
            $"DELETE FROM {BrokerAccounts};"
            
            // Delete banks and brokers (top-level operational entities)
            $"DELETE FROM {Banks};"
            $"DELETE FROM {Brokers};"
        ]
