module internal CurrencyExtensions

open System.Runtime.CompilerServices
open Binnaculum.Core.Database.DatabaseModel
open Microsoft.Data.Sqlite
open Binnaculum.Core
open Binnaculum.Core.SQL

    [<Extension>]
    type Do() =
        
        [<Extension>]
        static member fill(currency: Currency, command: SqliteCommand) =
            command.Parameters.AddWithValue("@Id", currency.Id) |> ignore
            command.Parameters.AddWithValue("@Name", currency.Name) |> ignore
            command.Parameters.AddWithValue("@Code", currency.Code) |> ignore
            command.Parameters.AddWithValue("@Symbol", currency.Symbol) |> ignore
            command
        
        [<Extension>]
        static member read(reader: SqliteDataReader) =
            { 
                Id = reader.GetInt32(reader.GetOrdinal("Id")) 
                Name = reader.GetString(reader.GetOrdinal("Name")) 
                Code = reader.GetString(reader.GetOrdinal("Code")) 
                Symbol = reader.GetString(reader.GetOrdinal("Symbol")) 
            }

        [<Extension>]
        static member save(currency: Currency) = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- CurrencyQuery.insert
            do! Database.Do.executeNonQuery(currency.fill command) |> Async.AwaitTask |> Async.Ignore
        }

        static member getAll() = task {
            let! command = Database.Do.createCommand()
            command.CommandText <- CurrencyQuery.getAll
            let! currencies = Database.Do.readAll<Currency>(command, Do.read)
            return currencies
        }

        static member currencyList() =
            [
                { Id = 0; Name = "UAE dirham"; Code = "AED"; Symbol = "د.إ;" };
                { Id = 0; Name = "Afghan afghani"; Code = "AFN"; Symbol = "Afs" };
                { Id = 0; Name = "Albanian lek"; Code = "ALL"; Symbol = "L" };
                { Id = 0; Name = "Armenian dram"; Code = "AMD"; Symbol = "AMD" };
                { Id = 0; Name = "Netherlands Antillean gulden"; Code = "ANG"; Symbol = "NAƒ" };
                { Id = 0; Name = "Angolan kwanza"; Code = "AOA"; Symbol = "Kz" };
                { Id = 0; Name = "Argentine peso"; Code = "ARS"; Symbol = "$" };
                { Id = 0; Name = "Australian dollar"; Code = "AUD"; Symbol = "$" };
                { Id = 0; Name = "Aruban florin"; Code = "AWG"; Symbol = "ƒ" };
                { Id = 0; Name = "Azerbaijani manat"; Code = "AZN"; Symbol = "AZN" };
                { Id = 0; Name = "Bosnia and Herzegovina konvertibilna marka"; Code = "BAM"; Symbol = "KM" };
                { Id = 0; Name = "Barbadian dollar"; Code = "BBD"; Symbol = "Bds$" };
                { Id = 0; Name = "Bangladeshi taka"; Code = "BDT"; Symbol = "৳" };
                { Id = 0; Name = "Bulgarian lev"; Code = "BGN"; Symbol = "BGN" };
                { Id = 0; Name = "Bahraini dinar"; Code = "BHD"; Symbol = ".د.ب" };
                { Id = 0; Name = "Burundi franc"; Code = "BIF"; Symbol = "FBu" };
                { Id = 0; Name = "Bermudian dollar"; Code = "BMD"; Symbol = "BD$" };
                { Id = 0; Name = "Brunei dollar"; Code = "BND"; Symbol = "B$" };
                { Id = 0; Name = "Bolivian boliviano"; Code = "BOB"; Symbol = "Bs." };
                { Id = 0; Name = "Brazilian real"; Code = "BRL"; Symbol = "R$" };
                { Id = 0; Name = "Bahamian dollar"; Code = "BSD"; Symbol = "B$" };
                { Id = 0; Name = "Bhutanese ngultrum"; Code = "BTN"; Symbol = "Nu." };
                { Id = 0; Name = "Botswana pula"; Code = "BWP"; Symbol = "P" };
                { Id = 0; Name = "Belarusian ruble"; Code = "BYR"; Symbol = "Br" };
                { Id = 0; Name = "Belize dollar"; Code = "BZD"; Symbol = "BZ$" };
                { Id = 0; Name = "Canadian dollar"; Code = "CAD"; Symbol = "$" };
                { Id = 0; Name = "Congolese franc"; Code = "CDF"; Symbol = "F" };
                { Id = 0; Name = "Swiss franc"; Code = "CHF"; Symbol = "Fr." };
                { Id = 0; Name = "Chilean peso"; Code = "CLP"; Symbol = "$" };
                { Id = 0; Name = "Chinese/Yuan renminbi"; Code = "CNY"; Symbol = "¥" };
                { Id = 0; Name = "Colombian peso"; Code = "COP"; Symbol = "Col$" };
                { Id = 0; Name = "Costa Rican colon"; Code = "CRC"; Symbol = "₡" };
                { Id = 0; Name = "Cuban peso"; Code = "CUC"; Symbol = "$" };
                { Id = 0; Name = "Cape Verdean escudo"; Code = "CVE"; Symbol = "Esc" };
                { Id = 0; Name = "Czech koruna"; Code = "CZK"; Symbol = "Kč" };
                { Id = 0; Name = "Djiboutian franc"; Code = "DJF"; Symbol = "Fdj" };
                { Id = 0; Name = "Danish krone"; Code = "DKK"; Symbol = "Kr" };
                { Id = 0; Name = "Dominican peso"; Code = "DOP"; Symbol = "RD$" };
                { Id = 0; Name = "Algerian dinar"; Code = "DZD"; Symbol = ".د.ج" };
                { Id = 0; Name = "Estonian kroon"; Code = "EEK"; Symbol = "KR" };
                { Id = 0; Name = "Egyptian pound"; Code = "EGP"; Symbol = "£" };
                { Id = 0; Name = "Eritrean nakfa"; Code = "ERN"; Symbol = "Nfa" };
                { Id = 0; Name = "Ethiopian birr"; Code = "ETB"; Symbol = "Br" };
                { Id = 0; Name = "European Euro"; Code = "EUR"; Symbol = "€" };
                { Id = 0; Name = "Fijian dollar"; Code = "FJD"; Symbol = "FJ$" };
                { Id = 0; Name = "Falkland Islands pound"; Code = "FKP"; Symbol = "£" };
                { Id = 0; Name = "British pound"; Code = "GBP"; Symbol = "£" };
                { Id = 0; Name = "Georgian lari"; Code = "GEL"; Symbol = "GEL" };
                { Id = 0; Name = "Ghanaian cedi"; Code = "GHS"; Symbol = "GH₵" };
                { Id = 0; Name = "Gibraltar pound"; Code = "GIP"; Symbol = "£" };
                { Id = 0; Name = "Gambian dalasi"; Code = "GMD"; Symbol = "D" };
                { Id = 0; Name = "Guinean franc"; Code = "GNF"; Symbol = "FG" };
                { Id = 0; Name = "Central African CFA franc"; Code = "GQE"; Symbol = "CFA" };
                { Id = 0; Name = "Guatemalan quetzal"; Code = "GTQ"; Symbol = "Q" };
                { Id = 0; Name = "Guyanese dollar"; Code = "GYD"; Symbol = "GY$" };
                { Id = 0; Name = "Hong Kong dollar"; Code = "HKD"; Symbol = "HK$" };
                { Id = 0; Name = "Honduran lempira"; Code = "HNL"; Symbol = "L" };
                { Id = 0; Name = "Croatian kuna"; Code = "HRK"; Symbol = "kn" };
                { Id = 0; Name = "Haitian gourde"; Code = "HTG"; Symbol = "G" };
                { Id = 0; Name = "Hungarian forint"; Code = "HUF"; Symbol = "Ft" };
                { Id = 0; Name = "Indonesian rupiah"; Code = "IDR"; Symbol = "Rp" };
                { Id = 0; Name = "Israeli new sheqel"; Code = "ILS"; Symbol = "₪" };
                { Id = 0; Name = "Indian rupee"; Code = "INR"; Symbol = "₹" };
                { Id = 0; Name = "Iraqi dinar"; Code = "IQD"; Symbol = "د.ع" };
                { Id = 0; Name = "Iranian rial"; Code = "IRR"; Symbol = "IRR" };
                { Id = 0; Name = "Icelandic króna"; Code = "ISK"; Symbol = "kr" };
                { Id = 0; Name = "Jamaican dollar"; Code = "JMD"; Symbol = "J$" };
                { Id = 0; Name = "Jordanian dinar"; Code = "JOD"; Symbol = "JOD" };
                { Id = 0; Name = "Japanese yen"; Code = "JPY"; Symbol = "¥" };
                { Id = 0; Name = "Kenyan shilling"; Code = "KES"; Symbol = "KSh" };
                { Id = 0; Name = "Kyrgyzstani som"; Code = "KGS"; Symbol = "сом" };
                { Id = 0; Name = "Cambodian riel"; Code = "KHR"; Symbol = "៛" };
                { Id = 0; Name = "Comorian franc"; Code = "KMF"; Symbol = "KMF" };
                { Id = 0; Name = "North Korean won"; Code = "KPW"; Symbol = "W" };
                { Id = 0; Name = "South Korean won"; Code = "KRW"; Symbol = "W" };
                { Id = 0; Name = "Kuwaiti dinar"; Code = "KWD"; Symbol = "KWD" };
                { Id = 0; Name = "Cayman Islands dollar"; Code = "KYD"; Symbol = "KY$" };
                { Id = 0; Name = "Kazakhstani tenge"; Code = "KZT"; Symbol = "T" };
                { Id = 0; Name = "Lao kip"; Code = "LAK"; Symbol = "KN" };
                { Id = 0; Name = "Lebanese lira"; Code = "LBP"; Symbol = "£" };
                { Id = 0; Name = "Sri Lankan rupee"; Code = "LKR"; Symbol = "Rs" };
                { Id = 0; Name = "Liberian dollar"; Code = "LRD"; Symbol = "L$" };
                { Id = 0; Name = "Lesotho loti"; Code = "LSL"; Symbol = "M" };
                { Id = 0; Name = "Lithuanian litas"; Code = "LTL"; Symbol = "Lt" };
                { Id = 0; Name = "Latvian lats"; Code = "LVL"; Symbol = "Ls" };
                { Id = 0; Name = "Libyan dinar"; Code = "LYD"; Symbol = "LD" };
                { Id = 0; Name = "Moroccan dirham"; Code = "MAD"; Symbol = "MAD" };
                { Id = 0; Name = "Moldovan leu"; Code = "MDL"; Symbol = "MDL" };
                { Id = 0; Name = "Malagasy ariary"; Code = "MGA"; Symbol = "FMG" };
                { Id = 0; Name = "Macedonian denar"; Code = "MKD"; Symbol = "MKD" };
                { Id = 0; Name = "Myanma kyat"; Code = "MMK"; Symbol = "K" };
                { Id = 0; Name = "Mongolian tugrik"; Code = "MNT"; Symbol = "₮" };
                { Id = 0; Name = "Macanese pataca"; Code = "MOP"; Symbol = "P" };
                { Id = 0; Name = "Mauritanian ouguiya"; Code = "MRO"; Symbol = "UM" };
                { Id = 0; Name = "Mauritian rupee"; Code = "MUR"; Symbol = "Rs" };
                { Id = 0; Name = "Maldivian rufiyaa"; Code = "MVR"; Symbol = "Rf" };
                { Id = 0; Name = "Malawian kwacha"; Code = "MWK"; Symbol = "MK" };
                { Id = 0; Name = "Mexican peso"; Code = "MXN"; Symbol = "$" };
                { Id = 0; Name = "Malaysian ringgit"; Code = "MYR"; Symbol = "RM" };
                { Id = 0; Name = "Mozambican metical"; Code = "MZM"; Symbol = "MTn" };
                { Id = 0; Name = "Namibian dollar"; Code = "NAD"; Symbol = "N$" };
                { Id = 0; Name = "Nigerian naira"; Code = "NGN"; Symbol = "₦" };
                { Id = 0; Name = "Nicaraguan córdoba"; Code = "NIO"; Symbol = "C$" };
                { Id = 0; Name = "Norwegian krone"; Code = "NOK"; Symbol = "kr" };
                { Id = 0; Name = "Nepalese rupee"; Code = "NPR"; Symbol = "NRs" };
                { Id = 0; Name = "New Zealand dollar"; Code = "NZD"; Symbol = "NZ$" };
                { Id = 0; Name = "Omani rial"; Code = "OMR"; Symbol = "OMR" };
                { Id = 0; Name = "Panamanian balboa"; Code = "PAB"; Symbol = "B/." };
                { Id = 0; Name = "Peruvian nuevo sol"; Code = "PEN"; Symbol = "S/." };
                { Id = 0; Name = "Papua New Guinean kina"; Code = "PGK"; Symbol = "K" };
                { Id = 0; Name = "Philippine peso"; Code = "PHP"; Symbol = "₱" };
                { Id = 0; Name = "Pakistani rupee"; Code = "PKR"; Symbol = "Rs." };
                { Id = 0; Name = "Polish zloty"; Code = "PLN"; Symbol = "zł" };
                { Id = 0; Name = "Paraguayan guarani"; Code = "PYG"; Symbol = "₲" };
                { Id = 0; Name = "Qatari riyal"; Code = "QAR"; Symbol = "QR" };
                { Id = 0; Name = "Romanian leu"; Code = "RON"; Symbol = "L" };
                { Id = 0; Name = "Serbian dinar"; Code = "RSD"; Symbol = "din." };
                { Id = 0; Name = "Russian ruble"; Code = "RUB"; Symbol = "R" };
                { Id = 0; Name = "Saudi riyal"; Code = "SAR"; Symbol = "SR" };
                { Id = 0; Name = "Solomon Islands dollar"; Code = "SBD"; Symbol = "SI$" };
                { Id = 0; Name = "Seychellois rupee"; Code = "SCR"; Symbol = "SR" };
                { Id = 0; Name = "Sudanese pound"; Code = "SDG"; Symbol = "SDG" };
                { Id = 0; Name = "Swedish krona"; Code = "SEK"; Symbol = "kr" };
                { Id = 0; Name = "Singapore dollar"; Code = "SGD"; Symbol = "S$" };
                { Id = 0; Name = "Saint Helena pound"; Code = "SHP"; Symbol = "£" };
                { Id = 0; Name = "Sierra Leonean leone"; Code = "SLL"; Symbol = "Le" };
                { Id = 0; Name = "Somali shilling"; Code = "SOS"; Symbol = "Sh." };
                { Id = 0; Name = "Surinamese dollar"; Code = "SRD"; Symbol = "$" };
                { Id = 0; Name = "Syrian pound"; Code = "SYP"; Symbol = "LS" };
                { Id = 0; Name = "Swazi lilangeni"; Code = "SZL"; Symbol = "E" };
                { Id = 0; Name = "Thai baht"; Code = "THB"; Symbol = "฿" };
                { Id = 0; Name = "Tajikistani somoni"; Code = "TJS"; Symbol = "TJS" };
                { Id = 0; Name = "Turkmen manat"; Code = "TMT"; Symbol = "m" };
                { Id = 0; Name = "Tunisian dinar"; Code = "TND"; Symbol = "DT" };
                { Id = 0; Name = "Turkish new lira"; Code = "TRY"; Symbol = "TRY" };
                { Id = 0; Name = "Trinidad and Tobago dollar"; Code = "TTD"; Symbol = "TT$" };
                { Id = 0; Name = "New Taiwan dollar"; Code = "TWD"; Symbol = "NT$" };
                { Id = 0; Name = "Tanzanian shilling"; Code = "TZS"; Symbol = "TZS" };
                { Id = 0; Name = "Ukrainian hryvnia"; Code = "UAH"; Symbol = "UAH" };
                { Id = 0; Name = "Ugandan shilling"; Code = "UGX"; Symbol = "USh" };
                { Id = 0; Name = "United States dollar"; Code = "USD"; Symbol = "US$" };
                { Id = 0; Name = "Uruguayan peso"; Code = "UYU"; Symbol = "$U" };
                { Id = 0; Name = "Uzbekistani som"; Code = "UZS"; Symbol = "UZS" };
                { Id = 0; Name = "Venezuelan bolivar"; Code = "VEB"; Symbol = "Bs" };
                { Id = 0; Name = "Vietnamese dong"; Code = "VND"; Symbol = "₫" };
                { Id = 0; Name = "Vanuatu vatu"; Code = "VUV"; Symbol = "VT" };
                { Id = 0; Name = "Samoan tala"; Code = "WST"; Symbol = "WS$" };
                { Id = 0; Name = "Central African CFA franc"; Code = "XAF"; Symbol = "CFA" };
                { Id = 0; Name = "East Caribbean dollar"; Code = "XCD"; Symbol = "EC$" };
                { Id = 0; Name = "Special Drawing Rights"; Code = "XDR"; Symbol = "SDR" };
                { Id = 0; Name = "West African CFA franc"; Code = "XOF"; Symbol = "CFA" };
                { Id = 0; Name = "CFP franc"; Code = "XPF"; Symbol = "F" };
                { Id = 0; Name = "Yemeni rial"; Code = "YER"; Symbol = "YER" };
                { Id = 0; Name = "South African rand"; Code = "ZAR"; Symbol = "R" };
                { Id = 0; Name = "Zambian kwacha"; Code = "ZMK"; Symbol = "ZK" };
                { Id = 0; Name = "Zimbabwean dollar"; Code = "ZWR"; Symbol = "Z$" }
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