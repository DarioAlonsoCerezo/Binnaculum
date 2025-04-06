namespace Binnaculum.Core

open Binnaculum.Core.Models
open Microsoft.Maui.Storage
open System.Text.Json

//We will use this to save or load lightweight data serialized from preferences.
//This is not a database, but a simple key-value storage.
module internal Storage =
    
    let defaulHomeData = {
        Accounts = []
        Transactions = []
    }

    let save dataKey data =        
        Preferences.Set(dataKey, JsonSerializer.Serialize data)

    let load<'T> (dataKey: string) (defaultValue: 'T) =
        match Preferences.Get(dataKey, "") with
        | "" -> defaultValue
        | json -> JsonSerializer.Deserialize<'T>(json)

    let remove dataKey =
        Preferences.Remove(dataKey)

    let clear () =
        Preferences.Clear()