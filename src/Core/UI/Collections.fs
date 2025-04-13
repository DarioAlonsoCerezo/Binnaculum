namespace Binnaculum.Core.UI

open DynamicData
open Binnaculum.Core.Models

module Collections =
    let Brokers = new SourceList<Broker>()
    let Currencies = new SourceList<Currency>()
