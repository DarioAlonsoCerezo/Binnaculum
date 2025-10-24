module internal AutoImportOperationExtensions

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
    static member fill(operation: AutoImportOperation, command: SqliteCommand) =
        command.fillEntityAuditable<AutoImportOperation> (
            [ (SQLParameterName.BrokerAccountId, operation.BrokerAccountId)
              (SQLParameterName.TickerId, operation.TickerId)
              (SQLParameterName.CurrencyId, operation.CurrencyId)
              (SQLParameterName.IsOpen, if operation.IsOpen then 1 else 0)
              (SQLParameterName.Realized, operation.Realized.Value)
              (SQLParameterName.Commissions, operation.Commissions.Value)
              (SQLParameterName.Fees, operation.Fees.Value)
              (SQLParameterName.Premium, operation.Premium.Value)
              (SQLParameterName.Dividends, operation.Dividends.Value)
              (SQLParameterName.DividendTaxes, operation.DividendTaxes.Value)
              (SQLParameterName.CapitalDeployed, operation.CapitalDeployed.Value)
              (SQLParameterName.Performance, operation.Performance) ],
            operation
        )

    [<Extension>]
    static member read(reader: SqliteDataReader) =
        { Id = reader.getInt32 FieldName.Id
          BrokerAccountId = reader.getInt32 FieldName.BrokerAccountId
          TickerId = reader.getInt32 FieldName.TickerId
          CurrencyId = reader.getInt32 FieldName.CurrencyId
          IsOpen = reader.getInt32 FieldName.IsOpen = 1
          Realized = reader.getMoney FieldName.Realized
          Commissions = reader.getMoney FieldName.Commissions
          Fees = reader.getMoney FieldName.Fees
          Premium = reader.getMoney FieldName.Premium
          Dividends = reader.getMoney FieldName.Dividends
          DividendTaxes = reader.getMoney FieldName.DividendTaxes
          CapitalDeployed = reader.getMoney FieldName.CapitalDeployed
          Performance = reader.getDecimal FieldName.Performance
          Audit = reader.getAudit () }

    [<Extension>]
    static member save(operation: AutoImportOperation) =
        Database.Do.saveEntity operation (fun o c -> o.fill c)

    [<Extension>]
    static member delete(operation: AutoImportOperation) = Database.Do.deleteEntity operation

    static member getAll() =
        Database.Do.getAllEntities Do.read AutoImportOperationQuery.getAll

    static member getById(id: int) =
        Database.Do.getById Do.read id AutoImportOperationQuery.getById

    static member getByTicker(tickerId: int) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- AutoImportOperationQuery.selectByTicker
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore
            let! operations = Database.Do.readAll<AutoImportOperation> (command, Do.read)
            return operations
        }

    static member getByBrokerAccount(brokerAccountId: int) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- AutoImportOperationQuery.selectByBrokerAccount

            command.Parameters.AddWithValue(SQLParameterName.BrokerAccountId, brokerAccountId)
            |> ignore

            let! operations = Database.Do.readAll<AutoImportOperation> (command, Do.read)
            return operations
        }

    static member getOpenOperation(tickerId: int, brokerAccountId: int) =
        task {
            let! command = Database.Do.createCommand ()
            command.CommandText <- AutoImportOperationQuery.selectOpenOperation
            command.Parameters.AddWithValue(SQLParameterName.TickerId, tickerId) |> ignore

            command.Parameters.AddWithValue(SQLParameterName.BrokerAccountId, brokerAccountId)
            |> ignore

            let! operations = Database.Do.readAll<AutoImportOperation> (command, Do.read)
            return operations |> Seq.tryHead
        }

    static member createOperation(brokerAccountId: int, tickerId: int, currencyId: int) =
        { Id = 0
          BrokerAccountId = brokerAccountId
          TickerId = tickerId
          CurrencyId = currencyId
          IsOpen = true
          Realized = Money.FromAmount(0m)
          Commissions = Money.FromAmount(0m)
          Fees = Money.FromAmount(0m)
          Premium = Money.FromAmount(0m)
          Dividends = Money.FromAmount(0m)
          DividendTaxes = Money.FromAmount(0m)
          CapitalDeployed = Money.FromAmount(0m)
          Performance = 0m
          Audit = AuditableEntity.Default }

    static member closeOperation(operation: AutoImportOperation) = { operation with IsOpen = false }

    static member updateOperationMetrics
        (
            operation: AutoImportOperation,
            realized: Money,
            commissions: Money,
            fees: Money,
            premium: Money,
            dividends: Money,
            dividendTaxes: Money,
            capitalDeployed: Money
        ) =
        let totalRealized = realized.Value
        let totalCapital = capitalDeployed.Value

        let performance =
            if totalCapital <> 0m then
                (totalRealized / totalCapital) * 100m
            else
                0m

        { operation with
            Realized = realized
            Commissions = commissions
            Fees = fees
            Premium = premium
            Dividends = dividends
            DividendTaxes = dividendTaxes
            CapitalDeployed = capitalDeployed
            Performance = performance }
