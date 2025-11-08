namespace Binnaculum.Core

open System
open Binnaculum.Core.Models
open Binnaculum.Core.Keys

module MovementDisplay =
    
    /// <summary>
    /// Computes the formatted title resource key for a movement.
    /// This replaces the logic from MovementTemplate.xaml.cs GetTitleFromBrokerAccountMovementType/GetTitleFromBankAccountMovementType
    /// </summary>
    let computeFormattedTitle (movement: Movement) : string =
        match movement.Type with
        | AccountMovementType.Trade -> 
            ResourceKeys.MovementType_Trade
        | AccountMovementType.OptionTrade -> 
            ResourceKeys.MovementType_OptionTrade
        | AccountMovementType.Dividend -> 
            ResourceKeys.MovementType_DividendReceived
        | AccountMovementType.DividendTax -> 
            ResourceKeys.MovementType_DividendTaxWithheld
        | AccountMovementType.DividendDate ->
            match movement.DividendDate with
            | Some dd when dd.DividendCode = DividendCode.ExDividendDate -> 
                ResourceKeys.MovementType_DividendExDate
            | Some dd when dd.DividendCode = DividendCode.PayDividendDate -> 
                ResourceKeys.MovementType_DividendPayDate
            | _ -> ResourceKeys.MovementType_DividendExDate
        | AccountMovementType.BrokerMovement ->
            match movement.BrokerMovement with
            | Some bm ->
                match bm.MovementType with
                | BrokerMovementType.Conversion -> ResourceKeys.MovementType_Conversion
                | BrokerMovementType.Deposit -> ResourceKeys.MovementType_Deposit
                | BrokerMovementType.Fee -> ResourceKeys.MovementType_Fee
                | BrokerMovementType.InterestsGained -> ResourceKeys.MovementType_InterestsGained
                | BrokerMovementType.InterestsPaid -> ResourceKeys.MovementType_InterestsPaid
                | BrokerMovementType.Lending -> ResourceKeys.MovementType_Lending
                | BrokerMovementType.Withdrawal -> ResourceKeys.MovementType_Withdrawal
                | BrokerMovementType.ACATMoneyTransferSent 
                | BrokerMovementType.ACATMoneyTransferReceived 
                | BrokerMovementType.ACATSecuritiesTransferSent 
                | BrokerMovementType.ACATSecuritiesTransferReceived -> 
                    ResourceKeys.MovementType_ACATTransfer
                | BrokerMovementType.DividendReceived -> ResourceKeys.MovementType_DividendReceived
                | BrokerMovementType.DividendTaxWithheld -> ResourceKeys.MovementType_DividendTaxWithheld
            | None -> ""
        | AccountMovementType.BankAccountMovement ->
            match movement.BankAccountMovement with
            | Some bam ->
                match bam.MovementType with
                | BankAccountMovementType.Balance -> ResourceKeys.MovementType_Bank_Balance
                | BankAccountMovementType.Interest -> ResourceKeys.MovementType_Bank_Interest
                | BankAccountMovementType.Fee -> ResourceKeys.MovementType_Bank_Fees
            | None -> ""
        | AccountMovementType.TickerSplit -> ""
        | AccountMovementType.EmptyMovement -> ""
    
    /// <summary>
    /// Computes the formatted subtitle resource key for a movement.
    /// This replaces the logic from MovementTemplate.xaml.cs GetSubtitleFromTradeCode/GetSubtitleFromACAT
    /// </summary>
    let computeFormattedSubtitle (movement: Movement) : string option =
        match movement.Type with
        | AccountMovementType.Trade ->
            match movement.Trade with
            | Some t ->
                let resourceKey = 
                    match t.TradeCode with
                    | TradeCode.BuyToClose -> ResourceKeys.Movement_BuyToClose
                    | TradeCode.BuyToOpen -> ResourceKeys.Movement_BuyToOpen
                    | TradeCode.SellToClose -> ResourceKeys.Movement_SellToClose
                    | TradeCode.SellToOpen -> ResourceKeys.Movement_SellToOpen
                Some resourceKey
            | None -> None
        | AccountMovementType.DividendDate ->
            // Subtitle shows the short date string for dividend dates
            match movement.DividendDate with
            | Some dd -> Some (dd.TimeStamp.ToShortDateString())
            | None -> None
        | AccountMovementType.BrokerMovement ->
            match movement.BrokerMovement with
            | Some bm ->
                match bm.MovementType with
                | BrokerMovementType.ACATMoneyTransferReceived -> 
                    Some ResourceKeys.MovementType_ACATMoneyTransferReceived_Subtitle
                | BrokerMovementType.ACATMoneyTransferSent -> 
                    Some ResourceKeys.MovementType_ACATMoneyTransferSent_Subtitle
                | BrokerMovementType.ACATSecuritiesTransferSent -> 
                    Some ResourceKeys.MovementType_ACATSecuritiesTransferSent_Subtitle
                | BrokerMovementType.ACATSecuritiesTransferReceived -> 
                    Some ResourceKeys.MovementType_ACATSecuritiesTransferReceived_Subtitle
                | _ -> None
            | None -> None
        | _ -> None
    
    /// <summary>
    /// Computes the formatted date string for a movement.
    /// Uses standard formatting for most movements
    /// </summary>
    let computeFormattedDate (timestamp: DateTime) : string =
        timestamp.ToShortDateString()
    
    /// <summary>
    /// Simplifyed extension for decimal formatting (from UI extensions)
    /// </summary>
    let private simplifyDecimal (value: decimal) : string =
        if value % 1m = 0m then
            value.ToString("N0")
        else
            value.ToString("N2")
    
    /// <summary>
    /// Computes the formatted quantity string for a movement.
    /// This replaces the logic from MovementTemplate.xaml.cs FillTradeMovement/FillBrokerAccountMovement/FillOptionTrade
    /// </summary>
    let computeFormattedQuantity (movement: Movement) : string option =
        match movement.Type with
        | AccountMovementType.Trade ->
            match movement.Trade with
            | Some t -> Some (sprintf "x%s" (simplifyDecimal t.Quantity))
            | None -> None
        | AccountMovementType.OptionTrade ->
            match movement.OptionTrade with
            | Some ot when ot.Quantity > 1 -> Some (sprintf "x%d" ot.Quantity)
            | _ -> None
        | AccountMovementType.BrokerMovement ->
            match movement.BrokerMovement with
            | Some bm ->
                match bm.MovementType with
                | BrokerMovementType.ACATSecuritiesTransferReceived 
                | BrokerMovementType.ACATSecuritiesTransferSent ->
                    match bm.Quantity with
                    | Some qty -> Some (qty.ToString("N0"))
                    | None -> None
                | _ -> None
            | None -> None
        | _ -> None
    
    /// <summary>
    /// Computes visibility flags for UI elements.
    /// This replaces the logic from MovementTemplate.xaml.cs ShowSubtitle/ShowACAT/OnBindingContextChanged visibility assignments
    /// </summary>
    let computeVisibilityFlags (movement: Movement) =
        let showACAT = 
            match movement.Type with
            | AccountMovementType.BrokerMovement ->
                match movement.BrokerMovement with
                | Some bm ->
                    bm.MovementType = BrokerMovementType.ACATSecuritiesTransferReceived ||
                    bm.MovementType = BrokerMovementType.ACATSecuritiesTransferSent
                | None -> false
            | _ -> false
        
        let showSubtitle = 
            match movement.Type with
            | AccountMovementType.Trade -> true
            | AccountMovementType.DividendDate -> true
            | AccountMovementType.BrokerMovement ->
                match movement.BrokerMovement with
                | Some bm ->
                    bm.MovementType = BrokerMovementType.ACATSecuritiesTransferReceived ||
                    bm.MovementType = BrokerMovementType.ACATSecuritiesTransferSent ||
                    bm.MovementType = BrokerMovementType.ACATMoneyTransferReceived ||
                    bm.MovementType = BrokerMovementType.ACATMoneyTransferSent
                | None -> false
            | _ -> false
        
        let showQuantity = 
            movement.Type = AccountMovementType.Trade
        
        let showOptionSubtitle = 
            movement.Type = AccountMovementType.OptionTrade
        
        let showAmount = not showACAT
        
        (showQuantity, showSubtitle, showOptionSubtitle, showACAT, showAmount)
    
    /// <summary>
    /// Factory function that creates a Movement with all display properties pre-computed.
    /// This should be called whenever a Movement record is created from database data.
    /// </summary>
    let createMovementWithDisplayProperties (rawMovement: Movement) : Movement =
        let title = computeFormattedTitle rawMovement
        let subtitle = computeFormattedSubtitle rawMovement
        let date = computeFormattedDate rawMovement.TimeStamp
        let quantity = computeFormattedQuantity rawMovement
        let (showQuantity, showSubtitle, showOptionSubtitle, showACAT, showAmount) = 
            computeVisibilityFlags rawMovement
        
        { rawMovement with
            FormattedTitle = title
            FormattedSubtitle = subtitle
            FormattedDate = date
            FormattedQuantity = quantity
            ShowQuantity = showQuantity
            ShowSubtitle = showSubtitle
            ShowOptionSubtitle = showOptionSubtitle
            ShowACAT = showACAT
            ShowAmount = showAmount }
