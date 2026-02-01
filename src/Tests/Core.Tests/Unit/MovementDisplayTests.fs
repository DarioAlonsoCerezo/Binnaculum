namespace Core.Tests.Unit

open NUnit.Framework
open Binnaculum.Core.Models
open Binnaculum.Core.MovementDisplay
open Binnaculum.Core
open System

[<TestFixture>]
type MovementDisplayTests() =
    
    [<Test>]
    member _.``computeFormattedTitle should return correct resource key for Trade`` () =
        let movement = emptyMovement()
        let movement = { movement with Type = AccountMovementType.Trade }
        
        let title = computeFormattedTitle movement
        
        Assert.That(title, Is.EqualTo(ResourceKeys.MovementType_Trade))
    
    [<Test>]
    member _.``computeFormattedTitle should return correct resource key for OptionTrade`` () =
        let movement = emptyMovement()
        let movement = { movement with Type = AccountMovementType.OptionTrade }
        
        let title = computeFormattedTitle movement
        
        Assert.That(title, Is.EqualTo(ResourceKeys.MovementType_OptionTrade))
    
    [<Test>]
    member _.``computeFormattedTitle should return correct resource key for Dividend`` () =
        let movement = emptyMovement()
        let movement = { movement with Type = AccountMovementType.Dividend }
        
        let title = computeFormattedTitle movement
        
        Assert.That(title, Is.EqualTo(ResourceKeys.MovementType_DividendReceived))
    
    [<Test>]
    member _.``computeFormattedTitle should return correct resource key for DividendTax`` () =
        let movement = emptyMovement()
        let movement = { movement with Type = AccountMovementType.DividendTax }
        
        let title = computeFormattedTitle movement
        
        Assert.That(title, Is.EqualTo(ResourceKeys.MovementType_DividendTaxWithheld))
    
    [<Test>]
    member _.``computeFormattedTitle should return correct resource key for BrokerMovement Deposit`` () =
        let brokerMovement = 
            { Id = 1
              TimeStamp = DateTime.Now
              Amount = 100m
              Currency = { Id = 1; Title = "US Dollar"; Code = "USD"; Symbol = "$" }
              BrokerAccount = { Id = 1; Broker = { Id = 1; Name = "Test"; Image = ""; SupportedBroker = SupportedBroker.Unknown }; AccountNumber = "123" }
              Commissions = 0m
              Fees = 0m
              MovementType = BrokerMovementType.Deposit
              Notes = None
              FromCurrency = None
              AmountChanged = None
              Ticker = None
              Quantity = None }
        
        let movement = emptyMovement()
        let movement = { movement with Type = AccountMovementType.BrokerMovement; BrokerMovement = Some brokerMovement }
        
        let title = computeFormattedTitle movement
        
        Assert.That(title, Is.EqualTo(ResourceKeys.MovementType_Deposit))
    
    [<Test>]
    member _.``computeFormattedTitle should return correct resource key for BrokerMovement ACAT`` () =
        let brokerMovement = 
            { Id = 1
              TimeStamp = DateTime.Now
              Amount = 100m
              Currency = { Id = 1; Title = "US Dollar"; Code = "USD"; Symbol = "$" }
              BrokerAccount = { Id = 1; Broker = { Id = 1; Name = "Test"; Image = ""; SupportedBroker = SupportedBroker.Unknown }; AccountNumber = "123" }
              Commissions = 0m
              Fees = 0m
              MovementType = BrokerMovementType.ACATSecuritiesTransferReceived
              Notes = None
              FromCurrency = None
              AmountChanged = None
              Ticker = None
              Quantity = None }
        
        let movement = emptyMovement()
        let movement = { movement with Type = AccountMovementType.BrokerMovement; BrokerMovement = Some brokerMovement }
        
        let title = computeFormattedTitle movement
        
        Assert.That(title, Is.EqualTo(ResourceKeys.MovementType_ACATTransfer))
    
    [<Test>]
    member _.``computeFormattedSubtitle should return None for Dividend`` () =
        let movement = emptyMovement()
        let movement = { movement with Type = AccountMovementType.Dividend }
        
        let subtitle = computeFormattedSubtitle movement
        
        Assert.That(subtitle, Is.EqualTo(None))
    
    [<Test>]
    member _.``computeFormattedSubtitle should return correct resource key for Trade BuyToOpen`` () =
        let trade = 
            { Id = 1
              TimeStamp = DateTime.Now
              Quantity = 100m
              Price = 10m
              TotalInvestedAmount = 1000m
              Ticker = { Id = 1; Symbol = "AAPL"; Image = None; Name = Some "Apple"; OptionsEnabled = true; OptionContractMultiplier = 100 }
              BrokerAccount = { Id = 1; Broker = { Id = 1; Name = "Test"; Image = ""; SupportedBroker = SupportedBroker.Unknown }; AccountNumber = "123" }
              Currency = { Id = 1; Title = "US Dollar"; Code = "USD"; Symbol = "$" }
              TradeCode = TradeCode.BuyToOpen
              TradeType = TradeType.Long
              Commissions = 0m
              Fees = 0m
              Leveraged = 1m
              Notes = None }
        
        let movement = emptyMovement()
        let movement = { movement with Type = AccountMovementType.Trade; Trade = Some trade }
        
        let subtitle = computeFormattedSubtitle movement
        
        Assert.That(subtitle.IsSome, Is.True)
        Assert.That(subtitle.Value, Is.EqualTo(ResourceKeys.Movement_BuyToOpen))
    
    [<Test>]
    member _.``computeFormattedSubtitle should return correct resource key for Trade SellToClose`` () =
        let trade = 
            { Id = 1
              TimeStamp = DateTime.Now
              Quantity = 100m
              Price = 10m
              TotalInvestedAmount = 1000m
              Ticker = { Id = 1; Symbol = "AAPL"; Image = None; Name = Some "Apple"; OptionsEnabled = true; OptionContractMultiplier = 100 }
              BrokerAccount = { Id = 1; Broker = { Id = 1; Name = "Test"; Image = ""; SupportedBroker = SupportedBroker.Unknown }; AccountNumber = "123" }
              Currency = { Id = 1; Title = "US Dollar"; Code = "USD"; Symbol = "$" }
              TradeCode = TradeCode.SellToClose
              TradeType = TradeType.Long
              Commissions = 0m
              Fees = 0m
              Leveraged = 1m
              Notes = None }
        
        let movement = emptyMovement()
        let movement = { movement with Type = AccountMovementType.Trade; Trade = Some trade }
        
        let subtitle = computeFormattedSubtitle movement
        
        Assert.That(subtitle.IsSome, Is.True)
        Assert.That(subtitle.Value, Is.EqualTo(ResourceKeys.Movement_SellToClose))
    
    [<Test>]
    member _.``computeFormattedSubtitle should return date string for DividendDate`` () =
        let dividendDate = 
            { Id = 1
              TimeStamp = DateTime(2023, 12, 15)
              Amount = 10m
              Ticker = { Id = 1; Symbol = "AAPL"; Image = None; Name = Some "Apple"; OptionsEnabled = true; OptionContractMultiplier = 100 }
              Currency = { Id = 1; Title = "US Dollar"; Code = "USD"; Symbol = "$" }
              BrokerAccount = { Id = 1; Broker = { Id = 1; Name = "Test"; Image = ""; SupportedBroker = SupportedBroker.Unknown }; AccountNumber = "123" }
              DividendCode = DividendCode.ExDividendDate }
        
        let movement = emptyMovement()
        let movement = { movement with Type = AccountMovementType.DividendDate; DividendDate = Some dividendDate }
        
        let subtitle = computeFormattedSubtitle movement
        
        Assert.That(subtitle, Is.Not.EqualTo(None))
        Assert.That(subtitle.Value, Does.Contain("12/15/2023").Or.Contain("15/12/2023").Or.Contain("2023"))
    
    [<Test>]
    member _.``computeFormattedQuantity should format Trade quantity correctly`` () =
        let trade = 
            { Id = 1
              TimeStamp = DateTime.Now
              Quantity = 1234.5m
              Price = 10m
              TotalInvestedAmount = 12345m
              Ticker = { Id = 1; Symbol = "AAPL"; Image = None; Name = Some "Apple"; OptionsEnabled = true; OptionContractMultiplier = 100 }
              BrokerAccount = { Id = 1; Broker = { Id = 1; Name = "Test"; Image = ""; SupportedBroker = SupportedBroker.Unknown }; AccountNumber = "123" }
              Currency = { Id = 1; Title = "US Dollar"; Code = "USD"; Symbol = "$" }
              TradeCode = TradeCode.BuyToOpen
              TradeType = TradeType.Long
              Commissions = 0m
              Fees = 0m
              Leveraged = 1m
              Notes = None }
        
        let movement = emptyMovement()
        let movement = { movement with Type = AccountMovementType.Trade; Trade = Some trade }
        
        let quantity = computeFormattedQuantity movement
        
        Assert.That(quantity, Is.Not.EqualTo(None))
        Assert.That(quantity.Value, Does.StartWith("x"))
        Assert.That(quantity.Value, Does.Contain("1,234"))
    
    [<Test>]
    member _.``computeVisibilityFlags should set ShowQuantity true for Trade`` () =
        let movement = emptyMovement()
        let movement = { movement with Type = AccountMovementType.Trade }
        
        let (showQuantity, _, _, _, _) = computeVisibilityFlags movement
        
        Assert.That(showQuantity, Is.True)
    
    [<Test>]
    member _.``computeVisibilityFlags should set ShowSubtitle true for Trade`` () =
        let movement = emptyMovement()
        let movement = { movement with Type = AccountMovementType.Trade }
        
        let (_, showSubtitle, _, _, _) = computeVisibilityFlags movement
        
        Assert.That(showSubtitle, Is.True)
    
    [<Test>]
    member _.``computeVisibilityFlags should set ShowOptionSubtitle true for OptionTrade`` () =
        let movement = emptyMovement()
        let movement = { movement with Type = AccountMovementType.OptionTrade }
        
        let (_, _, showOptionSubtitle, _, _) = computeVisibilityFlags movement
        
        Assert.That(showOptionSubtitle, Is.True)
    
    [<Test>]
    member _.``computeVisibilityFlags should set ShowACAT true for ACAT securities transfer`` () =
        let brokerMovement = 
            { Id = 1
              TimeStamp = DateTime.Now
              Amount = 100m
              Currency = { Id = 1; Title = "US Dollar"; Code = "USD"; Symbol = "$" }
              BrokerAccount = { Id = 1; Broker = { Id = 1; Name = "Test"; Image = ""; SupportedBroker = SupportedBroker.Unknown }; AccountNumber = "123" }
              Commissions = 0m
              Fees = 0m
              MovementType = BrokerMovementType.ACATSecuritiesTransferReceived
              Notes = None
              FromCurrency = None
              AmountChanged = None
              Ticker = None
              Quantity = None }
        
        let movement = emptyMovement()
        let movement = { movement with Type = AccountMovementType.BrokerMovement; BrokerMovement = Some brokerMovement }
        
        let (_, _, _, showACAT, showAmount) = computeVisibilityFlags movement
        
        Assert.That(showACAT, Is.True)
        Assert.That(showAmount, Is.False)
    
    [<Test>]
    member _.``createMovementWithDisplayProperties should populate all display properties`` () =
        let trade = 
            { Id = 1
              TimeStamp = DateTime(2023, 12, 15)
              Quantity = 100m
              Price = 10m
              TotalInvestedAmount = 1000m
              Ticker = { Id = 1; Symbol = "AAPL"; Image = None; Name = Some "Apple"; OptionsEnabled = true; OptionContractMultiplier = 100 }
              BrokerAccount = { Id = 1; Broker = { Id = 1; Name = "Test"; Image = ""; SupportedBroker = SupportedBroker.Unknown }; AccountNumber = "123" }
              Currency = { Id = 1; Title = "US Dollar"; Code = "USD"; Symbol = "$" }
              TradeCode = TradeCode.BuyToOpen
              TradeType = TradeType.Long
              Commissions = 0m
              Fees = 0m
              Leveraged = 1m
              Notes = None }
        
        let rawMovement = emptyMovement()
        let rawMovement = { rawMovement with Type = AccountMovementType.Trade; Trade = Some trade; TimeStamp = trade.TimeStamp }
        
        let movement = createMovementWithDisplayProperties rawMovement
        
        // Verify all properties are populated
        Assert.That(movement.FormattedTitle, Is.EqualTo(ResourceKeys.MovementType_Trade))
        Assert.That(movement.FormattedSubtitle.IsSome, Is.True)
        Assert.That(movement.FormattedSubtitle.Value, Is.EqualTo(ResourceKeys.Movement_BuyToOpen))
        Assert.That(movement.FormattedDate, Is.Not.Empty)
        Assert.That(movement.FormattedQuantity, Is.Not.EqualTo(None))
        Assert.That(movement.ShowQuantity, Is.True)
        Assert.That(movement.ShowSubtitle, Is.True)
        Assert.That(movement.ShowOptionSubtitle, Is.False)
        Assert.That(movement.ShowACAT, Is.False)
        Assert.That(movement.ShowAmount, Is.True)
