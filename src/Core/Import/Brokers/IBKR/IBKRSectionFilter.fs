namespace Binnaculum.Core.Import

open IBKRModels

/// <summary>
/// Privacy-compliant section filtering for IBKR statements
/// Excludes sensitive personal information while preserving transaction data
/// </summary>
module IBKRSectionFilter =
    
    /// <summary>
    /// Sections that contain sensitive personal information and should be skipped
    /// </summary>
    let private sensitiveSection = Set.ofList [
        "Account Information"
        "Account Info"
        "Statement Header"
        "Statement"
        "Notes"
        "Legal Notes"
        "Location of Customer Assets"
        "Custody Information"
        "Net Asset Value"
        "Account Summary"
        "Change in NAV"
    ]
    
    /// <summary>
    /// Sections that we actively parse for financial data
    /// </summary>
    let private parsableSection = Set.ofList [
        "Trades"
        "Deposits & Withdrawals"
        "Open Positions"
        "Financial Instrument Information"
        "Cash Report"
        "Base Currency Exchange Rate"
        "Forex Balances"
        "Collateral for Customer Borrowing"
    ]
    
    /// <summary>
    /// Determine the section type from a section name
    /// Returns appropriate IBKRSection or SkippedSection for privacy compliance
    /// </summary>
    let classifySection (sectionName: string) : IBKRSection =
        let normalizedName = sectionName.Trim()
        
        // Check if this is a sensitive section that should be skipped
        if sensitiveSection |> Set.contains normalizedName then
            SkippedSection $"Privacy: {normalizedName}"
        else
            // Map known parsable sections
            match normalizedName with
            | "Trades" -> Trades
            | "Deposits & Withdrawals" -> DepositsWithdrawals
            | "Open Positions" -> OpenPositions
            | "Financial Instrument Information" -> FinancialInstruments
            | "Cash Report" -> CashReport
            | "Base Currency Exchange Rate" -> ExchangeRates
            | "Forex Balances" -> ForexBalances
            | "Collateral for Customer Borrowing" -> CollateralBorrowing
            | _ -> SkippedSection $"Unknown: {normalizedName}"
    
    /// <summary>
    /// Check if a section should be processed (not skipped for privacy)
    /// </summary>
    let shouldProcessSection (section: IBKRSection) : bool =
        match section with
        | SkippedSection _ -> false
        | _ -> true
    
    /// <summary>
    /// Get the reason why a section was skipped
    /// </summary>
    let getSkipReason (section: IBKRSection) : string option =
        match section with
        | SkippedSection reason -> Some reason
        | _ -> None
    
    /// <summary>
    /// Filter CSV lines to remove sensitive data while preserving structure
    /// Used for logging and debugging without exposing personal information
    /// </summary>
    let sanitizeLineForLogging (line: string) : string =
        let parts = line.Split(',')
        if parts.Length < 2 then line
        else
            let sectionType = parts.[0].Trim()
            let dataType = if parts.Length > 1 then parts.[1].Trim() else ""
            
            // For sensitive sections, only show the section type
            if sensitiveSection |> Set.contains sectionType then
                $"{sectionType},Data,[REDACTED FOR PRIVACY]"
            else
                line
    
    /// <summary>
    /// Validate that no sensitive data is included in parsed results
    /// Returns validation errors if sensitive data is detected
    /// </summary>
    let validatePrivacyCompliance (data: IBKRStatementData) : string list =
        let errors = ResizeArray<string>()
        
        // Check that we don't have any account numbers or personal identifiers
        // This is a safety check in case the filtering logic has gaps
        
        // Check broker name doesn't contain personal info (should be generic broker name)
        match data.BrokerName with
        | Some brokerName when brokerName.Contains("Account") || brokerName.Contains("#") ->
            errors.Add("Potential account information in broker name")
        | _ -> ()
        
        // Check instrument descriptions for potential account info
        for instrument in data.Instruments do
            if instrument.Description.Contains("Account") || instrument.Description.Contains("#") then
                errors.Add($"Potential account information in instrument description: {instrument.Symbol}")
        
        errors |> List.ofSeq