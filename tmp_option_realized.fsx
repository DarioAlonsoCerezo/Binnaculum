open System

type OptionCode =
    | SellToOpen
    | BuyToOpen
    | SellToClose
    | BuyToClose

[<Struct>]
type OptionTrade =
    { Id: int
      Code: OptionCode
      NetPremium: decimal
      TimeStamp: DateTime
      Strike: decimal
      Expiration: DateTime }

let calculateRealizedGains (optionTrades: OptionTrade list) =
    let tradesByOption =
        optionTrades
        |> List.sortBy (fun t -> t.TimeStamp)
        |> List.groupBy (fun t -> (t.Strike, t.Expiration))

    let mutable total = 0m

    for ((strike, expiration), trades) in tradesByOption do
        printfn
            "Processing option Strike=%M Expiration=%s Count=%d"
            strike
            (expiration.ToString("yyyy-MM-dd"))
            trades.Length

        let mutable openPositions: (OptionCode * decimal) list = []

        for trade in trades do
            match trade.Code with
            | SellToOpen
            | BuyToOpen ->
                openPositions <- openPositions @ [ (trade.Code, trade.NetPremium) ]
                printfn "  Open %A premium %M (queue=%d)" trade.Code trade.NetPremium openPositions.Length
            | BuyToClose
            | SellToClose ->
                let mutable remaining = 1
                let mutable queue = openPositions

                while remaining > 0 && not queue.IsEmpty do
                    let (code, netPremium) = queue.Head

                    let gain =
                        match code, trade.Code with
                        | SellToOpen, BuyToClose -> netPremium - abs trade.NetPremium
                        | BuyToOpen, SellToClose -> trade.NetPremium - abs netPremium
                        | _ -> 0m

                    total <- total + gain
                    remaining <- remaining - 1
                    queue <- queue.Tail
                    printfn "  Close %A matched %A -> gain %M (running total %M)" trade.Code code gain total

                openPositions <- queue

        ()

    total

let data =
    [ { Id = 12
        Code = SellToOpen
        NetPremium = 33.86m
        TimeStamp = DateTime(2024, 4, 25)
        Strike = 35m
        Expiration = DateTime(2024, 5, 3) }
      { Id = 9
        Code = SellToOpen
        NetPremium = 17.86m
        TimeStamp = DateTime(2024, 4, 26, 20, 22, 28)
        Strike = 11m
        Expiration = DateTime(2024, 5, 3) }
      { Id = 8
        Code = BuyToOpen
        NetPremium = 12.13m
        TimeStamp = DateTime(2024, 4, 26, 20, 22, 28)
        Strike = 11m
        Expiration = DateTime(2024, 5, 3) }
      { Id = 11
        Code = BuyToOpen
        NetPremium = 5.13m
        TimeStamp = DateTime(2024, 4, 26, 19, 8, 13)
        Strike = 19m
        Expiration = DateTime(2024, 5, 3) }
      { Id = 10
        Code = SellToOpen
        NetPremium = 17.86m
        TimeStamp = DateTime(2024, 4, 26, 19, 8, 13)
        Strike = 19m
        Expiration = DateTime(2024, 5, 3) }
      { Id = 3
        Code = BuyToClose
        NetPremium = 9.13m
        TimeStamp = DateTime(2024, 4, 29)
        Strike = 35m
        Expiration = DateTime(2024, 5, 3) }
      { Id = 2
        Code = SellToClose
        NetPremium = 4.86m
        TimeStamp = DateTime(2024, 4, 29)
        Strike = 11m
        Expiration = DateTime(2024, 5, 3) }
      { Id = 4
        Code = SellToOpen
        NetPremium = 15.86m
        TimeStamp = DateTime(2024, 4, 29)
        Strike = 23m
        Expiration = DateTime(2024, 5, 10) }
      { Id = 5
        Code = BuyToClose
        NetPremium = 17.13m
        TimeStamp = DateTime(2024, 4, 29)
        Strike = 19m
        Expiration = DateTime(2024, 5, 3) }
      { Id = 7
        Code = BuyToClose
        NetPremium = 8.13m
        TimeStamp = DateTime(2024, 4, 29)
        Strike = 11m
        Expiration = DateTime(2024, 5, 3) }
      { Id = 6
        Code = SellToClose
        NetPremium = 0.86m
        TimeStamp = DateTime(2024, 4, 29)
        Strike = 19m
        Expiration = DateTime(2024, 5, 3) }
      { Id = 1
        Code = SellToOpen
        NetPremium = 14.86m
        TimeStamp = DateTime(2024, 4, 30)
        Strike = 23m
        Expiration = DateTime(2024, 5, 10) } ]

printfn "Total realized gains: %M" (calculateRealizedGains data)
