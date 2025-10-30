namespace Binnaculum.Core.Snapshots

open Binnaculum.Core.Database.DatabaseModel

/// <summary>
/// Shared module for calculating capital deployed across ticker snapshots and operations.
/// Ensures consistent capital deployment logic throughout the application.
/// </summary>
[<AutoOpen>]
module CapitalDeployedCalculator =

    /// <summary>
    /// Calculate capital deployed for a single option trade.
    /// Rules:
    /// - BuyToOpen (Call/Put): Strike × Multiplier (debit position)
    /// - SellToOpen Call: $0 (assume covered with shares or contracts)
    /// - SellToOpen Put: Strike × Multiplier (cash-secured put obligation)
    /// - BuyToClose/SellToClose/Expiration: $0 (closing trades don't deploy new capital)
    /// </summary>
    let internal calculateOptionTradeCapitalDeployed (trade: OptionTrade) : decimal =
        match trade.Code with
        | OptionCode.BuyToOpen ->
            // Debit trade - deploy strike obligation
            trade.Strike.Value * trade.Multiplier
        | OptionCode.SellToOpen ->
            match trade.OptionType with
            | OptionType.Call ->
                // Assume covered call - no capital deployed
                0m
            | OptionType.Put ->
                // Cash-secured put - deploy strike obligation
                trade.Strike.Value * trade.Multiplier
        | _ ->
            // Closing trades (BuyToClose, SellToClose, Expiration) deploy no new capital
            0m

    /// <summary>
    /// Calculate capital deployed for a single stock trade.
    /// Capital deployed = absolute value of (Price × Quantity)
    /// </summary>
    let internal calculateStockTradeCapitalDeployed (trade: Trade) : decimal =
        abs (trade.Price.Value * trade.Quantity)

    /// <summary>
    /// Calculate total capital deployed from a list of option trades.
    /// Only opening trades contribute to capital deployment.
    /// </summary>
    let internal calculateTotalOptionCapitalDeployed (optionTrades: OptionTrade list) : decimal =
        optionTrades |> List.sumBy calculateOptionTradeCapitalDeployed

    /// <summary>
    /// Calculate total capital deployed from a list of stock trades.
    /// </summary>
    let internal calculateTotalStockCapitalDeployed (trades: Trade list) : decimal =
        trades |> List.sumBy calculateStockTradeCapitalDeployed
