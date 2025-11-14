namespace Binnaculum.Core

module ResourceKeys =
    [<Literal>]
    let AccountCreator_Select_Broker = "AccountCreator_Select_Broker"

    [<Literal>]
    let AccountCreator_Creating_Account_For_Broker =
        "AccountCreator_Creating_Account_For_Broker"

    [<Literal>]
    let AccountCreator_Change_Selection = "AccountCreator_Change_Selection"

    [<Literal>]
    let AccountCreator_Select_Bank = "AccountCreator_Select_Bank"

    [<Literal>]
    let AccountCreator_Creating_Account_For_Bank =
        "AccountCreator_Creating_Account_For_Bank"

    [<Literal>]
    let FilePicker_Select_Image = "FilePicker_Select_Image"

    [<Literal>]
    let ItemSelector_Select_Option = "ItemSelector_Select_Option"

    [<Literal>]
    let ItemSelector_Change_Selection = "ItemSelector_Change_Selection"

    [<Literal>]
    let MovementType_Deposit = "MovementType_Deposit"

    [<Literal>]
    let MovementType_Withdrawal = "MovementType_Withdrawal"

    [<Literal>]
    let MovementType_Fee = "MovementType_Fee"

    [<Literal>]
    let MovementType_InterestsGained = "MovementType_InterestsGained"

    [<Literal>]
    let MovementType_Lending = "MovementType_Lending"

    [<Literal>]
    let MovementType_ACATTransfer = "MovementType_ACATTransfer"

    [<Literal>]
    let MovementType_ACATMoneyTransferSent = "MovementType_ACATMoneyTransferSent"

    [<Literal>]
    let MovementType_ACATMoneyTransferSent_Subtitle =
        "MovementType_ACATMoneyTransferSent_Subtitle"

    [<Literal>]
    let MovementType_ACATMoneyTransferReceived =
        "MovementType_ACATMoneyTransferReceived"

    [<Literal>]
    let MovementType_ACATMoneyTransferReceived_Subtitle =
        "MovementType_ACATMoneyTransferReceived_Subtitle"

    [<Literal>]
    let MovementType_ACATSecuritiesTransferSent =
        "MovementType_ACATSecuritiesTransferSent"

    [<Literal>]
    let MovementType_ACATSecuritiesTransferSent_Subtitle =
        "MovementType_ACATSecuritiesTransferSent_Subtitle"

    [<Literal>]
    let MovementType_ACATSecuritiesTransferReceived =
        "MovementType_ACATSecuritiesTransferReceived"

    [<Literal>]
    let MovementType_ACATSecuritiesTransferReceived_Subtitle =
        "MovementType_ACATSecuritiesTransferReceived_Subtitle"

    [<Literal>]
    let MovementType_InterestsPaid = "MovementType_InterestsPaid"

    [<Literal>]
    let MovementType_Conversion = "MovementType_Conversion"

    [<Literal>]
    let MovementType_DividendReceived = "MovementType_DividendReceived"

    [<Literal>]
    let MovementType_DividendTaxWithheld = "MovementType_DividendTaxWithheld"

    [<Literal>]
    let MovementType_DividendExDate = "MovementType_DividendExDate"

    [<Literal>]
    let MovementType_DividendPayDate = "MovementType_DividendPayDate"

    [<Literal>]
    let MovementType_Trade = "MovementType_Trade"

    [<Literal>]
    let MovementType_OptionTrade = "MovementType_OptionTrade"

    [<Literal>]
    let MovementType_Bank_Balance = "MovementType_Bank_Balance"

    [<Literal>]
    let MovementType_Bank_Fees = "MovementType_Bank_Fees"

    [<Literal>]
    let MovementType_Bank_Interest = "MovementType_Bank_Interest"

    [<Literal>]
    let Yesterday = "Yesterday"

    [<Literal>]
    let Today = "Today"

    [<Literal>]
    let Movement_BuyToClose = "Movement_BuyToClose"

    [<Literal>]
    let Movement_BuyToOpen = "Movement_BuyToOpen"

    [<Literal>]
    let Movement_SellToClose = "Movement_SellToClose"

    [<Literal>]
    let Movement_SellToOpen = "Movement_SellToOpen"

    [<Literal>]
    let Multiplier_Title = "Multiplier_Title"

    [<Literal>]
    let OptionType_Call = "OptionType_Call"

    [<Literal>]
    let OptionType_Put = "OptionType_Put"

    [<Literal>]
    let OptionCode_BTC = "OptionCode_BTC"

    [<Literal>]
    let OptionCode_BTO = "OptionCode_BTO"

    [<Literal>]
    let OptionCode_STC = "OptionCode_STC"

    [<Literal>]
    let OptionCode_STO = "OptionCode_STO"

    [<Literal>]
    let OptionCode_Assigned = "OptionCode_Assigned"

    [<Literal>]
    let OptionCode_Expired = "OptionCode_Expired"

    [<Literal>]
    let OptionCode_CashSettledAssigned = "OptionCode_CashSettledAssigned"

    [<Literal>]
    let OptionCode_CashSettledExercised = "OptionCode_CashSettledExercised"

    [<Literal>]
    let OptionCode_Exercised = "OptionCode_Exercised"


    [<Literal>]
    let OptionCode_BTC_Extended = "OptionCode_BTC_Extended"

    [<Literal>]
    let OptionCode_BTO_Extended = "OptionCode_BTO_Extended"

    [<Literal>]
    let OptionCode_STC_Extended = "OptionCode_STC_Extended"

    [<Literal>]
    let OptionCode_STO_Extended = "OptionCode_STO_Extended"

    [<Literal>]
    let OptionCode_Assigned_Extended = "OptionCode_Assigned_Extended"

    [<Literal>]
    let OptionCode_Expired_Extended = "OptionCode_Expired_Extended"

    [<Literal>]
    let OptionCode_CashSettledAssigned_Extended =
        "OptionCode_CashSettledAssigned_Extended"

    [<Literal>]
    let OptionCode_CashSettledExercised_Extended =
        "OptionCode_CashSettledExercised_Extended"

    [<Literal>]
    let OptionCode_Exercised_Extended = "OptionCode_Exercised_Extended"

    [<Literal>]
    let Placeholder_FromCurrency = "Placeholder_FromCurrency"

    [<Literal>]
    let Placeholder_Amount = "Placeholder_Amount"

    [<Literal>]
    let Placeholder_Quantity = "Placeholder_Quantity"

    [<Literal>]
    let Broker_Unknown = "Broker_Unknown"

    // Import status resource keys (for UI localization)
    [<Literal>]
    let Import_SavingData = "Import_SavingData"

    [<Literal>]
    let Import_SavingData_Generic = "Import_SavingData_Generic"

    [<Literal>]
    let Import_ProcessingFile = "Import_ProcessingFile"

    [<Literal>]
    let Import_ProcessingFile_Generic = "Import_ProcessingFile_Generic"

    [<Literal>]
    let Import_ProcessingRecords = "Import_ProcessingRecords"

    [<Literal>]
    let Import_ProcessingRecords_Generic = "Import_ProcessingRecords_Generic"

    [<Literal>]
    let Import_Validating = "Import_Validating"

    [<Literal>]
    let Import_Cancelled = "Import_Cancelled"

    [<Literal>]
    let Import_Failed = "Import_Failed"

    [<Literal>]
    let Import_Completed = "Import_Completed"

    [<Literal>]
    let Import_CalculatingSnapshots = "Import_CalculatingSnapshots"

    [<Literal>]
    let Import_CalculatingSnapshots_Generic = "Import_CalculatingSnapshots_Generic"

    [<Literal>]
    let Import_Chunked_ReadingFile = "Import_Chunked_ReadingFile"

    [<Literal>]
    let Import_Chunked_AnalyzingDates = "Import_Chunked_AnalyzingDates"

    [<Literal>]
    let Import_Chunked_ProcessingChunk = "Import_Chunked_ProcessingChunk"

    [<Literal>]
    let Import_Chunked_CalculatingSnapshots = "Import_Chunked_CalculatingSnapshots"

    [<Literal>]
    let Import_Chunked_CompletedSummary = "Import_Chunked_CompletedSummary"

    [<Literal>]
    let Import_Chunked_State_Idle = "Import_Chunked_State_Idle"

    [<Literal>]
    let Import_Chunked_State_ReadingFile = "Import_Chunked_State_ReadingFile"

    [<Literal>]
    let Import_Chunked_State_AnalyzingDates = "Import_Chunked_State_AnalyzingDates"

    [<Literal>]
    let Import_Chunked_State_ProcessingChunk = "Import_Chunked_State_ProcessingChunk"

    [<Literal>]
    let Import_Chunked_State_CalculatingSnapshots =
        "Import_Chunked_State_CalculatingSnapshots"

    [<Literal>]
    let Import_Chunked_State_Completed = "Import_Chunked_State_Completed"

    [<Literal>]
    let Import_Chunked_State_Failed = "Import_Chunked_State_Failed"

    [<Literal>]
    let Import_Chunked_State_Cancelled = "Import_Chunked_State_Cancelled"

    [<Literal>]
    let Time_Format_Hours = "Time_Format_Hours"

    [<Literal>]
    let Time_Format_Minutes = "Time_Format_Minutes"

    [<Literal>]
    let Time_Format_Seconds = "Time_Format_Seconds"

    [<Literal>]
    let Time_Format_LessThanMinute = "Time_Format_LessThanMinute"

    [<Literal>]
    let Import_Phase_LoadingMovements = "Import_Phase_LoadingMovements"

    [<Literal>]
    let Import_Phase_CalculatingBrokerSnapshots =
        "Import_Phase_CalculatingBrokerSnapshots"

    [<Literal>]
    let Import_Phase_CalculatingTickerSnapshots =
        "Import_Phase_CalculatingTickerSnapshots"

    [<Literal>]
    let Import_Phase_CreatingOperations = "Import_Phase_CreatingOperations"

    [<Literal>]
    let Import_Phase_PersistingData = "Import_Phase_PersistingData"

    [<Literal>]
    let Import_Chunked_TimeRemaining = "Import_Chunked_TimeRemaining"

    [<Literal>]
    let Import_Chunked_CompletedDuration = "Import_Chunked_CompletedDuration"

    [<Literal>]
    let Import_Chunked_Validating = "Import_Chunked_Validating"

    [<Literal>]
    let Import_Chunked_ValidatingFile = "Import_Chunked_ValidatingFile"

    [<Literal>]
    let Import_Chunked_ExtractingFile = "Import_Chunked_ExtractingFile"

    [<Literal>]
    let Import_Chunked_ExtractingFileWithProgress =
        "Import_Chunked_ExtractingFileWithProgress"

    [<Literal>]
    let Import_Chunked_ChunkDateRange = "Import_Chunked_ChunkDateRange"
