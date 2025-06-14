namespace Binnaculum.Core.SQL

open Binnaculum.Core.TableName
open Binnaculum.Core.FieldName
open Binnaculum.Core // For SQLParameterName access

module internal InvestmentOverviewSnapshotQuery =
    let createTable =
        $"""
        CREATE TABLE IF NOT EXISTS {InvestmentOverviewSnapshots}
        (
            {Id} INTEGER PRIMARY KEY,
            {Date} TEXT NOT NULL,
            {PortfoliosValue} TEXT NOT NULL,
            {RealizedGains} TEXT NOT NULL,
            {RealizedPercentage} TEXT NOT NULL,
            {Invested} TEXT NOT NULL,
            {Commissions} TEXT NOT NULL,
            {Fees} TEXT NOT NULL,
            {CreatedAt} TEXT NOT NULL DEFAULT (DATETIME('now')),
            {UpdatedAt} TEXT
        );

        -- Index to optimize queries filtering by Date
        CREATE INDEX IF NOT EXISTS idx_InvestmentOverviewSnapshots_Date ON {InvestmentOverviewSnapshots}({Date});

        -- Trigger to automatically update the UpdatedAt column on row update
        CREATE TRIGGER IF NOT EXISTS trg_InvestmentOverviewSnapshots_UpdatedAt
        AFTER UPDATE ON {InvestmentOverviewSnapshots}
        FOR EACH ROW
        BEGIN
            UPDATE {InvestmentOverviewSnapshots}
            SET {UpdatedAt} = strftime('%%Y-%%m-%%dT%%H:%%M:%%S', 'now')
            WHERE {Id} = OLD.{Id};
        END;
        """

    let insert =
        $"""
        INSERT INTO {InvestmentOverviewSnapshots}
        (
            {Date},
            {PortfoliosValue},
            {RealizedGains},
            {RealizedPercentage},
            {Invested},
            {Commissions},
            {Fees},
            {CreatedAt},
            {UpdatedAt}
        )
        VALUES
        (
            {SQLParameterName.Date},
            {SQLParameterName.PortfoliosValue},
            {SQLParameterName.RealizedGains},
            {SQLParameterName.RealizedPercentage},
            {SQLParameterName.Invested},
            {SQLParameterName.Commissions},
            {SQLParameterName.Fees},
            {SQLParameterName.CreatedAt},
            {SQLParameterName.UpdatedAt}
        )
        """

    let update =
        $"""
        UPDATE {InvestmentOverviewSnapshots}
        SET
            {Date} = {SQLParameterName.Date},
            {PortfoliosValue} = {SQLParameterName.PortfoliosValue},
            {RealizedGains} = {SQLParameterName.RealizedGains},
            {RealizedPercentage} = {SQLParameterName.RealizedPercentage},
            {Invested} = {SQLParameterName.Invested},
            {Commissions} = {SQLParameterName.Commissions},
            {Fees} = {SQLParameterName.Fees},
            {CreatedAt} = {SQLParameterName.CreatedAt},
            {UpdatedAt} = {SQLParameterName.UpdatedAt}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let delete =
        $"""
        DELETE FROM {InvestmentOverviewSnapshots}
        WHERE
            {Id} = {SQLParameterName.Id}
        """

    let getAll =
        $"""
        SELECT * FROM {InvestmentOverviewSnapshots}
        """

    let getById =
        $"""
        SELECT * FROM {InvestmentOverviewSnapshots}
        WHERE
            {Id} = {SQLParameterName.Id}
        LIMIT 1
        """
        
    let getByDate =
        $"""
        SELECT * FROM {InvestmentOverviewSnapshots}
        WHERE
            {Date} = {SQLParameterName.Date}
        LIMIT 1
        """
        
    let getLatest =
        $"""
        SELECT * FROM {InvestmentOverviewSnapshots}
        ORDER BY {Date} DESC
        LIMIT 1
        """
        
    let getByDateRange =
        $"""
        SELECT * FROM {InvestmentOverviewSnapshots}
        WHERE
            {Date} BETWEEN {SQLParameterName.Date} AND {SQLParameterName.DateEnd}
        ORDER BY {Date} ASC
        """