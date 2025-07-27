module internal CurrencyExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open Binnaculum.Core.SQL
open DataReaderExtensions
open CommandExtensions

    [<Extension>]
    type Do() =
        
        [<Extension>]
        static member fill(currency: Currency, command: SqliteCommand) =
            command.fillEntity<Currency>(
                [
                    (SQLParameterName.Name, currency.Name);
                    (SQLParameterName.Code, currency.Code);
                    (SQLParameterName.Symbol, currency.Symbol);
                ], currency)
        
        [<Extension>]
        static member read(reader: SqliteDataReader) =
            {
                Id = reader.getInt32 FieldName.Id
                Name = reader.getString FieldName.Name
                Code = reader.getString FieldName.Code
                Symbol = reader.getString FieldName.Symbol
            }

        [<Extension>]
        static member save(currency: Currency) = Database.Do.saveEntity currency (fun c cmd -> c.fill cmd) 

        [<Extension>]
        static member delete(currency: Currency) = Database.Do.deleteEntity currency

        static member getAll() = Database.Do.getAllEntities Do.read CurrencyQuery.getAll

        static member getById(id: int) = Database.Do.getById Do.read id CurrencyQuery.getById

        static member getByCode(code: string) =
            task {
                let! command = Database.Do.createCommand()
                command.CommandText <- CurrencyQuery.getByCode
                command.Parameters.AddWithValue(SQLParameterName.Code, code) |> ignore
                let! currency = Database.Do.read<Currency>(command, Do.read)
                return currency
            }

        static member currencyList() =
            [
                { Id = 0; Name = "Currency_AED"; Code = "AED"; Symbol = "د.إ;" };
                { Id = 0; Name = "Currency_AFN"; Code = "AFN"; Symbol = "Afs" };
                { Id = 0; Name = "Currency_ALL"; Code = "ALL"; Symbol = "L" };
                { Id = 0; Name = "Currency_AMD"; Code = "AMD"; Symbol = "AMD" };
                { Id = 0; Name = "Currency_ANG"; Code = "ANG"; Symbol = "NAƒ" };
                { Id = 0; Name = "Currency_AOA"; Code = "AOA"; Symbol = "Kz" };
                { Id = 0; Name = "Currency_ARS"; Code = "ARS"; Symbol = "$" };
                { Id = 0; Name = "Currency_AUD"; Code = "AUD"; Symbol = "$" };
                { Id = 0; Name = "Currency_AWG"; Code = "AWG"; Symbol = "ƒ" };
                { Id = 0; Name = "Currency_AZN"; Code = "AZN"; Symbol = "AZN" };
                { Id = 0; Name = "Currency_BAM"; Code = "BAM"; Symbol = "KM" };
                { Id = 0; Name = "Currency_BBD"; Code = "BBD"; Symbol = "Bds$" };
                { Id = 0; Name = "Currency_BDT"; Code = "BDT"; Symbol = "৳" };
                { Id = 0; Name = "Currency_BGN"; Code = "BGN"; Symbol = "BGN" };
                { Id = 0; Name = "Currency_BHD"; Code = "BHD"; Symbol = ".د.ب" };
                { Id = 0; Name = "Currency_BIF"; Code = "BIF"; Symbol = "FBu" };
                { Id = 0; Name = "Currency_BMD"; Code = "BMD"; Symbol = "BD$" };
                { Id = 0; Name = "Currency_BND"; Code = "BND"; Symbol = "B$" };
                { Id = 0; Name = "Currency_BOB"; Code = "BOB"; Symbol = "Bs." };
                { Id = 0; Name = "Currency_BRL"; Code = "BRL"; Symbol = "R$" };
                { Id = 0; Name = "Currency_BSD"; Code = "BSD"; Symbol = "B$" };
                { Id = 0; Name = "Currency_BTN"; Code = "BTN"; Symbol = "Nu." };
                { Id = 0; Name = "Currency_BWP"; Code = "BWP"; Symbol = "P" };
                { Id = 0; Name = "Currency_BYR"; Code = "BYR"; Symbol = "Br" };
                { Id = 0; Name = "Currency_BZD"; Code = "BZD"; Symbol = "BZ$" };
                { Id = 0; Name = "Currency_CAD"; Code = "CAD"; Symbol = "$" };
                { Id = 0; Name = "Currency_CDF"; Code = "CDF"; Symbol = "F" };
                { Id = 0; Name = "Currency_CHF"; Code = "CHF"; Symbol = "Fr." };
                { Id = 0; Name = "Currency_CLP"; Code = "CLP"; Symbol = "$" };
                { Id = 0; Name = "Currency_CNY"; Code = "CNY"; Symbol = "¥" };
                { Id = 0; Name = "Currency_COP"; Code = "COP"; Symbol = "Col$" };
                { Id = 0; Name = "Currency_CRC"; Code = "CRC"; Symbol = "₡" };
                { Id = 0; Name = "Currency_CUC"; Code = "CUC"; Symbol = "$" };
                { Id = 0; Name = "Currency_CVE"; Code = "CVE"; Symbol = "Esc" };
                { Id = 0; Name = "Currency_CZK"; Code = "CZK"; Symbol = "Kč" };
                { Id = 0; Name = "Currency_DJF"; Code = "DJF"; Symbol = "Fdj" };
                { Id = 0; Name = "Currency_DKK"; Code = "DKK"; Symbol = "Kr" };
                { Id = 0; Name = "Currency_DOP"; Code = "DOP"; Symbol = "RD$" };
                { Id = 0; Name = "Currency_DZD"; Code = "DZD"; Symbol = ".د.ج" };
                { Id = 0; Name = "Currency_EEK"; Code = "EEK"; Symbol = "KR" };
                { Id = 0; Name = "Currency_EGP"; Code = "EGP"; Symbol = "£" };
                { Id = 0; Name = "Currency_ERN"; Code = "ERN"; Symbol = "Nfa" };
                { Id = 0; Name = "Currency_ETB"; Code = "ETB"; Symbol = "Br" };
                { Id = 0; Name = "Currency_EUR"; Code = "EUR"; Symbol = "€" };
                { Id = 0; Name = "Currency_FJD"; Code = "FJD"; Symbol = "FJ$" };
                { Id = 0; Name = "Currency_FKP"; Code = "FKP"; Symbol = "£" };
                { Id = 0; Name = "Currency_GBP"; Code = "GBP"; Symbol = "£" };
                { Id = 0; Name = "Currency_GEL"; Code = "GEL"; Symbol = "GEL" };
                { Id = 0; Name = "Currency_GHS"; Code = "GHS"; Symbol = "GH₵" };
                { Id = 0; Name = "Currency_GIP"; Code = "GIP"; Symbol = "£" };
                { Id = 0; Name = "Currency_GMD"; Code = "GMD"; Symbol = "D" };
                { Id = 0; Name = "Currency_GNF"; Code = "GNF"; Symbol = "FG" };
                { Id = 0; Name = "Currency_GQE"; Code = "GQE"; Symbol = "CFA" };
                { Id = 0; Name = "Currency_GTQ"; Code = "GTQ"; Symbol = "Q" };
                { Id = 0; Name = "Currency_GYD"; Code = "GYD"; Symbol = "GY$" };
                { Id = 0; Name = "Currency_HKD"; Code = "HKD"; Symbol = "HK$" };
                { Id = 0; Name = "Currency_HNL"; Code = "HNL"; Symbol = "L" };
                { Id = 0; Name = "Currency_HRK"; Code = "HRK"; Symbol = "kn" };
                { Id = 0; Name = "Currency_HTG"; Code = "HTG"; Symbol = "G" };
                { Id = 0; Name = "Currency_HUF"; Code = "HUF"; Symbol = "Ft" };
                { Id = 0; Name = "Currency_IDR"; Code = "IDR"; Symbol = "Rp" };
                { Id = 0; Name = "Currency_ILS"; Code = "ILS"; Symbol = "₪" };
                { Id = 0; Name = "Currency_INR"; Code = "INR"; Symbol = "₹" };
                { Id = 0; Name = "Currency_IQD"; Code = "IQD"; Symbol = "د.ع" };
                { Id = 0; Name = "Currency_IRR"; Code = "IRR"; Symbol = "IRR" };
                { Id = 0; Name = "Currency_ISK"; Code = "ISK"; Symbol = "kr" };
                { Id = 0; Name = "Currency_JMD"; Code = "JMD"; Symbol = "J$" };
                { Id = 0; Name = "Currency_JOD"; Code = "JOD"; Symbol = "JOD" };
                { Id = 0; Name = "Currency_JPY"; Code = "JPY"; Symbol = "¥" };
                { Id = 0; Name = "Currency_KES"; Code = "KES"; Symbol = "KSh" };
                { Id = 0; Name = "Currency_KGS"; Code = "KGS"; Symbol = "сом" };
                { Id = 0; Name = "Currency_KHR"; Code = "KHR"; Symbol = "៛" };
                { Id = 0; Name = "Currency_KMF"; Code = "KMF"; Symbol = "KMF" };
                { Id = 0; Name = "Currency_KPW"; Code = "KPW"; Symbol = "W" };
                { Id = 0; Name = "Currency_KRW"; Code = "KRW"; Symbol = "W" };
                { Id = 0; Name = "Currency_KWD"; Code = "KWD"; Symbol = "KWD" };
                { Id = 0; Name = "Currency_KYD"; Code = "KYD"; Symbol = "KY$" };
                { Id = 0; Name = "Currency_KZT"; Code = "KZT"; Symbol = "T" };
                { Id = 0; Name = "Currency_LAK"; Code = "LAK"; Symbol = "KN" };
                { Id = 0; Name = "Currency_LBP"; Code = "LBP"; Symbol = "£" };
                { Id = 0; Name = "Currency_LKR"; Code = "LKR"; Symbol = "Rs" };
                { Id = 0; Name = "Currency_LRD"; Code = "LRD"; Symbol = "L$" };
                { Id = 0; Name = "Currency_LSL"; Code = "LSL"; Symbol = "M" };
                { Id = 0; Name = "Currency_LTL"; Code = "LTL"; Symbol = "Lt" };
                { Id = 0; Name = "Currency_LVL"; Code = "LVL"; Symbol = "Ls" };
                { Id = 0; Name = "Currency_LYD"; Code = "LYD"; Symbol = "LD" };
                { Id = 0; Name = "Currency_MAD"; Code = "MAD"; Symbol = "MAD" };
                { Id = 0; Name = "Currency_MDL"; Code = "MDL"; Symbol = "MDL" };
                { Id = 0; Name = "Currency_MGA"; Code = "MGA"; Symbol = "FMG" };
                { Id = 0; Name = "Currency_MKD"; Code = "MKD"; Symbol = "MKD" };
                { Id = 0; Name = "Currency_MMK"; Code = "MMK"; Symbol = "K" };
                { Id = 0; Name = "Currency_MNT"; Code = "MNT"; Symbol = "₮" };
                { Id = 0; Name = "Currency_MOP"; Code = "MOP"; Symbol = "P" };
                { Id = 0; Name = "Currency_MRO"; Code = "MRO"; Symbol = "UM" };
                { Id = 0; Name = "Currency_MUR"; Code = "MUR"; Symbol = "Rs" };
                { Id = 0; Name = "Currency_MVR"; Code = "MVR"; Symbol = "Rf" };
                { Id = 0; Name = "Currency_MWK"; Code = "MWK"; Symbol = "MK" };
                { Id = 0; Name = "Currency_MXN"; Code = "MXN"; Symbol = "$" };
                { Id = 0; Name = "Currency_MYR"; Code = "MYR"; Symbol = "RM" };
                { Id = 0; Name = "Currency_MZM"; Code = "MZM"; Symbol = "MTn" };
                { Id = 0; Name = "Currency_NAD"; Code = "NAD"; Symbol = "N$" };
                { Id = 0; Name = "Currency_NGN"; Code = "NGN"; Symbol = "₦" };
                { Id = 0; Name = "Currency_NIO"; Code = "NIO"; Symbol = "C$" };
                { Id = 0; Name = "Currency_NOK"; Code = "NOK"; Symbol = "kr" };
                { Id = 0; Name = "Currency_NPR"; Code = "NPR"; Symbol = "NRs" };
                { Id = 0; Name = "Currency_NZD"; Code = "NZD"; Symbol = "NZ$" };
                { Id = 0; Name = "Currency_OMR"; Code = "OMR"; Symbol = "OMR" };
                { Id = 0; Name = "Currency_PAB"; Code = "PAB"; Symbol = "B/." };
                { Id = 0; Name = "Currency_PEN"; Code = "PEN"; Symbol = "S/." };
                { Id = 0; Name = "Currency_PGK"; Code = "PGK"; Symbol = "K" };
                { Id = 0; Name = "Currency_PHP"; Code = "PHP"; Symbol = "₱" };
                { Id = 0; Name = "Currency_PKR"; Code = "PKR"; Symbol = "Rs." };
                { Id = 0; Name = "Currency_PLN"; Code = "PLN"; Symbol = "zł" };
                { Id = 0; Name = "Currency_PYG"; Code = "PYG"; Symbol = "₲" };
                { Id = 0; Name = "Currency_QAR"; Code = "QAR"; Symbol = "QR" };
                { Id = 0; Name = "Currency_RON"; Code = "RON"; Symbol = "L" };
                { Id = 0; Name = "Currency_RSD"; Code = "RSD"; Symbol = "din." };
                { Id = 0; Name = "Currency_RUB"; Code = "RUB"; Symbol = "R" };
                { Id = 0; Name = "Currency_SAR"; Code = "SAR"; Symbol = "SR" };
                { Id = 0; Name = "Currency_SBD"; Code = "SBD"; Symbol = "SI$" };
                { Id = 0; Name = "Currency_SCR"; Code = "SCR"; Symbol = "SR" };
                { Id = 0; Name = "Currency_SDG"; Code = "SDG"; Symbol = "SDG" };
                { Id = 0; Name = "Currency_SEK"; Code = "SEK"; Symbol = "kr" };
                { Id = 0; Name = "Currency_SGD"; Code = "SGD"; Symbol = "S$" };
                { Id = 0; Name = "Currency_SHP"; Code = "SHP"; Symbol = "£" };
                { Id = 0; Name = "Currency_SLL"; Code = "SLL"; Symbol = "Le" };
                { Id = 0; Name = "Currency_SOS"; Code = "SOS"; Symbol = "Sh." };
                { Id = 0; Name = "Currency_SRD"; Code = "SRD"; Symbol = "$" };
                { Id = 0; Name = "Currency_SYP"; Code = "SYP"; Symbol = "LS" };
                { Id = 0; Name = "Currency_SZL"; Code = "SZL"; Symbol = "E" };
                { Id = 0; Name = "Currency_THB"; Code = "THB"; Symbol = "฿" };
                { Id = 0; Name = "Currency_TJS"; Code = "TJS"; Symbol = "TJS" };
                { Id = 0; Name = "Currency_TMT"; Code = "TMT"; Symbol = "m" };
                { Id = 0; Name = "Currency_TND"; Code = "TND"; Symbol = "DT" };
                { Id = 0; Name = "Currency_TRY"; Code = "TRY"; Symbol = "TRY" };
                { Id = 0; Name = "Currency_TTD"; Code = "TTD"; Symbol = "TT$" };
                { Id = 0; Name = "Currency_TWD"; Code = "TWD"; Symbol = "NT$" };
                { Id = 0; Name = "Currency_TZS"; Code = "TZS"; Symbol = "TZS" };
                { Id = 0; Name = "Currency_UAH"; Code = "UAH"; Symbol = "UAH" };
                { Id = 0; Name = "Currency_UGX"; Code = "UGX"; Symbol = "USh" };
                { Id = 0; Name = "Currency_USD"; Code = "USD"; Symbol = "US$" };
                { Id = 0; Name = "Currency_UYU"; Code = "UYU"; Symbol = "$U" };
                { Id = 0; Name = "Currency_UZS"; Code = "UZS"; Symbol = "UZS" };
                { Id = 0; Name = "Currency_VEB"; Code = "VEB"; Symbol = "Bs" };
                { Id = 0; Name = "Currency_VND"; Code = "VND"; Symbol = "₫" };
                { Id = 0; Name = "Currency_VUV"; Code = "VUV"; Symbol = "VT" };
                { Id = 0; Name = "Currency_WST"; Code = "WST"; Symbol = "WS$" };
                { Id = 0; Name = "Currency_XAF"; Code = "XAF"; Symbol = "CFA" };
                { Id = 0; Name = "Currency_XCD"; Code = "XCD"; Symbol = "EC$" };
                { Id = 0; Name = "Currency_XDR"; Code = "XDR"; Symbol = "SDR" };
                { Id = 0; Name = "Currency_XOF"; Code = "XOF"; Symbol = "CFA" };
                { Id = 0; Name = "Currency_XPF"; Code = "XPF"; Symbol = "F" };
                { Id = 0; Name = "Currency_YER"; Code = "YER"; Symbol = "YER" };
                { Id = 0; Name = "Currency_ZAR"; Code = "ZAR"; Symbol = "R" };
                { Id = 0; Name = "Currency_ZMK"; Code = "ZMK"; Symbol = "ZK" };
                { Id = 0; Name = "Currency_ZWR"; Code = "ZWR"; Symbol = "Z$" }
            ]


        static member isCurrencyTableEmpty() = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- CurrencyQuery.getCounted
            let! result = Database.Do.executeExcalar command |> Async.AwaitTask
            command.Dispose()
            return (result :?> int64) = 0L
        }

        static member insertDefaultValues() = task {
            let! isEmpty = Do.isCurrencyTableEmpty()
            if isEmpty then
                let currencies = Do.currencyList()
                currencies
                |> List.map(fun currency -> async {
                    let task = currency.save()
                    return! task |> Async.AwaitTask |> Async.Ignore
                    })
                |> Async.Sequential
                |> Async.Ignore
                |> Async.RunSynchronously
        }