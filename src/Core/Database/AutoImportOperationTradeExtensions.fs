module internal AutoImportOperationTradeExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open Binnaculum.Core.Database.TypeParser
open DataReaderExtensions
open CommandExtensions
open Binnaculum.Core.SQL
open OptionExtensions
open Binnaculum.Core.Patterns

[<Extension>]
type Do() =

    [<Extension>]
    static member fill(trade: AutoImportOperationTrade, command: SqliteCommand) =
        command.fillEntityAuditable<AutoImportOperationTrade> (
            [ (SQLParameterName.AutoOperationId, trade.AutoOperationId)
              (SQLParameterName.TradeType, fromOperationTradeTypeToDatabase trade.TradeType)
              (SQLParameterName.ReferenceId, trade.ReferenceId) ],
            trade
        )

    [<Extension>]
    static member read(reader: SqliteDataReader) =
        { Id = reader.getInt32 FieldName.Id
          AutoOperationId = reader.getInt32 FieldName.AutoOperationId
          TradeType =
            reader.GetString(reader.GetOrdinal(FieldName.TradeType))
            |> fromDatabaseToOperationTradeType
          ReferenceId = reader.getInt32 FieldName.ReferenceId
          Audit = reader.getAudit () }

    [<Extension>]
    static member save(trade: AutoImportOperationTrade) =
        Database.Do.saveEntity trade (fun t c -> t.fill c)

    [<Extension>]
    static member delete(trade: AutoImportOperationTrade) = 
        Database.Do.deleteEntity trade

    static member getAll() =
        Database.Do.getAllEntities Do.read AutoImportOperationTradeQuery.getAll

    static member getById(id: int) =
        Database.Do.getById Do.read id AutoImportOperationTradeQuery.getById

    static member getByOperation(autoOperationId: int) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- AutoImportOperationTradeQuery.selectByOperation
            command.Parameters.AddWithValue(SQLParameterName.AutoOperationId, autoOperationId) |> ignore
            let! trades = Database.Do.readAll<AutoImportOperationTrade> (command, Do.read)
            return trades
        }

    static member linkStockTrade(autoOperationId: int, tradeId: int) =
        task {
            let operationTrade =
                { Id = 0
                  AutoOperationId = autoOperationId
                  TradeType = OperationTradeType.StockTrade
                  ReferenceId = tradeId
                  Audit = AuditableEntity.Default }
            return! operationTrade.save()
        }

    static member linkOptionTrade(autoOperationId: int, optionTradeId: int) =
        task {
            let operationTrade =
                { Id = 0
                  AutoOperationId = autoOperationId
                  TradeType = OperationTradeType.OptionTrade
                  ReferenceId = optionTradeId
                  Audit = AuditableEntity.Default }
            return! operationTrade.save()
        }

    static member linkDividend(autoOperationId: int, dividendId: int) =
        task {
            let operationTrade =
                { Id = 0
                  AutoOperationId = autoOperationId
                  TradeType = OperationTradeType.Dividend
                  ReferenceId = dividendId
                  Audit = AuditableEntity.Default }
            return! operationTrade.save()
        }

    static member linkDividendTax(autoOperationId: int, dividendTaxId: int) =
        task {
            let operationTrade =
                { Id = 0
                  AutoOperationId = autoOperationId
                  TradeType = OperationTradeType.DividendTax
                  ReferenceId = dividendTaxId
                  Audit = AuditableEntity.Default }
            return! operationTrade.save()
        }
