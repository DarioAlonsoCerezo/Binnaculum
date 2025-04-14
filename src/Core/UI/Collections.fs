namespace Binnaculum.Core.UI

open DynamicData
open Binnaculum.Core.Models

module Collections =
    /// <summary>
    /// The Brokers list is loaded every time to ensure it reflects the most accurate and up-to-date information.
    /// This is necessary as the list must be accessible for creating new broker accounts and 
    /// should always be available for selection from the UI.
    /// </summary>
    let Brokers = new SourceList<Broker>()

    /// <summary>
    /// The Currencies list is loaded every time to ensure it reflects the most accurate and up-to-date information.
    /// This is necessary as the list must be accessible for creating new accounts and 
    /// should always be available for selection from the UI.
    /// </summary>
    let Currencies = new SourceList<Currency>()

    /// <summary>
    /// The Banks list is loaded every time to ensure it reflects the most accurate and up-to-date information.
    /// This is necessary as the list must be accessible for creating new bank accounts and 
    /// should always be available for selection from the UI.
    /// </summary>
    let Banks = new SourceList<Bank>()
