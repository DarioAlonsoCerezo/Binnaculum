namespace Core.Tests.Unit

open Microsoft.VisualStudio.TestTools.UnitTesting
open Binnaculum.Core.Models
open Binnaculum.Core.MovementDisplay
open Binnaculum.Core
open System

[<TestClass>]
type MovementDisplayTests() =
    
    [<TestMethod>]
    member _.``computeFormattedTitle should return correct resource key for Trade`` () =
        let movement = emptyMovement()
        let movement = { movement with Type = AccountMovementType.Trade }
        
        let title = computeFormattedTitle movement
        
        Assert.AreEqual(ResourceKeys.MovementType_Trade, title)
    
    [<TestMethod>]
    member _.``computeFormattedTitle should return correct resource key for OptionTrade`` () =
        let movement = emptyMovement()
        let movement = { movement with Type = AccountMovementType.OptionTrade }
        
        let title = computeFormattedTitle movement
        
        Assert.AreEqual(ResourceKeys.MovementType_OptionTrade, title)
    
    [<TestMethod>]
    member _.``computeFormattedTitle should return correct resource key for Dividend`` () =
        let movement = emptyMovement()
        let movement = { movement with Type = AccountMovementType.Dividend }
        
        let title = computeFormattedTitle movement
        
        Assert.AreEqual(ResourceKeys.MovementType_DividendReceived, title)
    
    [<TestMethod>]
    member _.``computeFormattedTitle should return correct resource key for DividendTax`` () =
        let movement = emptyMovement()
        let movement = { movement with Type = AccountMovementType.DividendTax }
        
        let title = computeFormattedTitle movement
        
        Assert.AreEqual(ResourceKeys.MovementType_DividendTaxWithheld, title)
    
    [<TestMethod>]
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
        
        Assert.AreEqual(ResourceKeys.MovementType_Deposit, title)
    
    [<TestMethod>]
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
        
        Assert.AreEqual(ResourceKeys.MovementType_ACATTransfer, title)
    
    [<TestMethod>]
    member _.``computeFormattedSubtitle should return None for Dividend`` () =
        let movement = emptyMovement()
        let movement = { movement with Type = AccountMovementType.Dividend }
        
        let subtitle = computeFormattedSubtitle movement
        
        Assert.AreEqual(None, subtitle)
    
    [<TestMethod>]
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
        
        Assert.IsTrue(subtitle.IsSome)
        Assert.AreEqual(ResourceKeys.Movement_BuyToOpen, subtitle.Value)
    
    [<TestMethod>]
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
        
        Assert.IsTrue(subtitle.IsSome)
        Assert.AreEqual(ResourceKeys.Movement_SellToClose, subtitle.Value)
    
    [<TestMethod>]
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
        
        Assert.AreNotEqual(None, subtitle)
        Assert.IsTrue(
            subtitle.Value.Contains("12/15/2023") || subtitle.Value.Contains("15/12/2023") || subtitle.Value.Contains("2023"),
            "Subtitle should contain a date representation"
        )
    
    [<TestMethod>]
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
        
        Assert.AreNotEqual(None, quantity)
        StringAssert.StartsWith(quantity.Value, "x")
        StringAssert.Contains(quantity.Value, "1,234")
    
    [<TestMethod>]
    member _.``computeVisibilityFlags should set ShowQuantity true for Trade`` () =
        let movement = emptyMovement()
        let movement = { movement with Type = AccountMovementType.Trade }
        
        let (showQuantity, _, _, _, _) = computeVisibilityFlags movement
        
        Assert.IsTrue(showQuantity)
    
    [<TestMethod>]
    member _.``computeVisibilityFlags should set ShowSubtitle true for Trade`` () =
        let movement = emptyMovement()
        let movement = { movement with Type = AccountMovementType.Trade }
        
        let (_, showSubtitle, _, _, _) = computeVisibilityFlags movement
        
        Assert.IsTrue(showSubtitle)
    
    [<TestMethod>]
    member _.``computeVisibilityFlags should set ShowOptionSubtitle true for OptionTrade`` () =
        let movement = emptyMovement()
        let movement = { movement with Type = AccountMovementType.OptionTrade }
        
        let (_, _, showOptionSubtitle, _, _) = computeVisibilityFlags movement
        
        Assert.IsTrue(showOptionSubtitle)
    
    [<TestMethod>]
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
        
        Assert.IsTrue(showACAT)
        Assert.IsFalse(showAmount)
    
    [<TestMethod>]
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
        Assert.AreEqual(ResourceKeys.MovementType_Trade, movement.FormattedTitle)
        Assert.IsTrue(movement.FormattedSubtitle.IsSome)
        Assert.AreEqual(ResourceKeys.Movement_BuyToOpen, movement.FormattedSubtitle.Value)
        Assert.IsTrue(movement.FormattedDate.Length > 0)
        Assert.AreNotEqual(None, movement.FormattedQuantity)
        Assert.IsTrue(movement.ShowQuantity)
        Assert.IsTrue(movement.ShowSubtitle)
        Assert.IsFalse(movement.ShowOptionSubtitle)
        Assert.IsFalse(movement.ShowACAT)
        Assert.IsTrue(movement.ShowAmount)
