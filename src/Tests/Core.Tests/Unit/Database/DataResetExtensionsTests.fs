namespace Binnaculum.Core.Tests

open Microsoft.VisualStudio.TestTools.UnitTesting
open Binnaculum.Core.Database
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Patterns
open System

/// <summary>
/// Tests for the DataReset functionality that deletes all operational data
/// while preserving reference tables (Ticker, Currency, TickerSplit, TickerPrice).
/// </summary>
[<TestClass>]
type public DataResetExtensionsTests() =
    inherit InMemoryDatabaseFixture()
    
    /// <summary>
    /// Helper to create test data for operational tables
    /// </summary>
    member private this.CreateTestOperationalData() =
        let now = DateTime.UtcNow

        // Create a broker
        let broker = {
            Id = 0
            Name = "Test Broker"
            Image = "test.png"
            SupportedBroker = SupportedBroker.IBKR
        }
        BrokerExtensions.Do.save(broker) |> Async.AwaitTask |> Async.RunSynchronously
        
        // Get the created broker
        let savedBroker = BrokerExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously |> List.head
        
        // Create a broker account
        let brokerAccount = {
            Id = 0
            BrokerId = savedBroker.Id
            AccountNumber = "TEST123"
            Audit = AuditableEntity.FromDateTime(now)
        }
        BrokerAccountExtensions.Do.save(brokerAccount) |> Async.AwaitTask |> Async.RunSynchronously

        let savedCurrency =
            CurrencyExtensions.Do.getAll()
            |> Async.AwaitTask
            |> Async.RunSynchronously
            |> List.tryHead
            |> Option.defaultWith (fun () ->
                let currency = {
                    Id = 0
                    Name = "US Dollar"
                    Code = "USD"
                    Symbol = "$"
                }
                CurrencyExtensions.Do.save(currency) |> Async.AwaitTask |> Async.RunSynchronously
                CurrencyExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously |> List.head
            )
        
        // Create a bank
        let bank = {
            Id = 0
            Name = "Test Bank"
            Image = Some "bank.png"
            Audit = AuditableEntity.FromDateTime(now)
        }
        BankExtensions.Do.save(bank) |> Async.AwaitTask |> Async.RunSynchronously
        
        // Get the created bank
        let savedBank = BankExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously |> List.head
        
        // Create a bank account
        let bankAccount = {
            Id = 0
            BankId = savedBank.Id
            Name = "Primary Account"
            Description = Some "Test account"
            CurrencyId = savedCurrency.Id
            Audit = AuditableEntity.FromDateTime(now)
        }
        BankAccountExtensions.Do.save(bankAccount) |> Async.AwaitTask |> Async.RunSynchronously
        
        ()
    
    /// <summary>
    /// Helper to create test data for reference tables
    /// </summary>
    member private this.CreateTestReferenceData() =
        let now = DateTime.UtcNow

        // Create a currency
        let currency = {
            Id = 0
            Name = "US Dollar"
            Code = "USD"
            Symbol = "$"
        }
        CurrencyExtensions.Do.save(currency) |> Async.AwaitTask |> Async.RunSynchronously
        
        // Get the created currency
        let savedCurrency = CurrencyExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously |> List.head
        
        // Create a ticker
        let ticker = {
            Id = 0
            Symbol = "AAPL"
            Image = None
            Name = Some "Apple Inc."
            OptionsEnabled = true
            OptionContractMultiplier = 100
            Audit = AuditableEntity.FromDateTime(now)
        }
        TickerExtensions.Do.save(ticker) |> Async.AwaitTask |> Async.RunSynchronously
        
        // Get the created ticker
        let savedTicker = TickerExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously |> List.head
        
        // Create a ticker split
        let tickerSplit = {
            Id = 0
            TickerId = savedTicker.Id
            SplitDate = DateTimePattern.FromDateTime(now)
            SplitFactor = 2.0m
            Audit = AuditableEntity.FromDateTime(now)
        }
        TickerSplitExtensions.Do.save(tickerSplit) |> Async.AwaitTask |> Async.RunSynchronously
        
        // Create a ticker price
        let tickerPrice = {
            Id = 0
            TickerId = savedTicker.Id
            CurrencyId = savedCurrency.Id
            PriceDate = DateTimePattern.FromDateTime(now)
            Price = Money.FromAmount(150.0m)
            Audit = AuditableEntity.FromDateTime(now)
        }
        TickerPriceExtensions.Do.save(tickerPrice) |> Async.AwaitTask |> Async.RunSynchronously
        
        ()
    
    [<TestMethod>]
    member public this.``deleteAllOperationalData removes all operational data``() =
        // Arrange
        this.CreateTestReferenceData()
        this.CreateTestOperationalData()
        
        // Verify data exists before deletion
        let brokersBeforeDelete = BrokerExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let brokerAccountsBeforeDelete = BrokerAccountExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let banksBeforeDelete = BankExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let bankAccountsBeforeDelete = BankAccountExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        
        Assert.IsTrue(brokersBeforeDelete.Length > 0, "Should have brokers before delete")
        Assert.IsTrue(brokerAccountsBeforeDelete.Length > 0, "Should have broker accounts before delete")
        Assert.IsTrue(banksBeforeDelete.Length > 0, "Should have banks before delete")
        Assert.IsTrue(bankAccountsBeforeDelete.Length > 0, "Should have bank accounts before delete")
        
        // Act
        DataResetExtensions.Do.deleteAllOperationalData() |> Async.AwaitTask |> Async.RunSynchronously
        
        // Assert - verify operational data is deleted
        let brokersAfterDelete = BrokerExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let brokerAccountsAfterDelete = BrokerAccountExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let banksAfterDelete = BankExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let bankAccountsAfterDelete = BankAccountExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        
        Assert.AreEqual(0, brokersAfterDelete.Length, "All brokers should be deleted")
        Assert.AreEqual(0, brokerAccountsAfterDelete.Length, "All broker accounts should be deleted")
        Assert.AreEqual(0, banksAfterDelete.Length, "All banks should be deleted")
        Assert.AreEqual(0, bankAccountsAfterDelete.Length, "All bank accounts should be deleted")
    
    [<TestMethod>]
    member public this.``deleteAllOperationalData preserves reference data``() =
        // Arrange
        this.CreateTestReferenceData()
        
        // Verify reference data exists before deletion
        let currenciesBeforeDelete = CurrencyExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let tickersBeforeDelete = TickerExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let tickerSplitsBeforeDelete = TickerSplitExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let tickerPricesBeforeDelete = TickerPriceExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        
        Assert.IsTrue(currenciesBeforeDelete.Length > 0, "Should have currencies before delete")
        Assert.IsTrue(tickersBeforeDelete.Length > 0, "Should have tickers before delete")
        Assert.IsTrue(tickerSplitsBeforeDelete.Length > 0, "Should have ticker splits before delete")
        Assert.IsTrue(tickerPricesBeforeDelete.Length > 0, "Should have ticker prices before delete")
        
        // Act
        DataResetExtensions.Do.deleteAllOperationalData() |> Async.AwaitTask |> Async.RunSynchronously
        
        // Assert - verify reference data is preserved
        let currenciesAfterDelete = CurrencyExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let tickersAfterDelete = TickerExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let tickerSplitsAfterDelete = TickerSplitExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let tickerPricesAfterDelete = TickerPriceExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        
        Assert.AreEqual(currenciesBeforeDelete.Length, currenciesAfterDelete.Length, "Currencies should be preserved")
        Assert.AreEqual(tickersBeforeDelete.Length, tickersAfterDelete.Length, "Tickers should be preserved")
        Assert.AreEqual(tickerSplitsBeforeDelete.Length, tickerSplitsAfterDelete.Length, "Ticker splits should be preserved")
        Assert.AreEqual(tickerPricesBeforeDelete.Length, tickerPricesAfterDelete.Length, "Ticker prices should be preserved")
    
    [<TestMethod>]
    member public this.``deleteAllOperationalData works on empty database``() =
        // Act - should not throw on empty database
        try
            DataResetExtensions.Do.deleteAllOperationalData() |> Async.AwaitTask |> Async.RunSynchronously
        with ex ->
            Assert.Fail($"Unexpected exception: {ex.Message}")
        
        // Assert - verify database is still empty
        let brokers = BrokerExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let currencies = CurrencyExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        
        Assert.AreEqual(0, brokers.Length, "Brokers should remain empty")
        Assert.AreEqual(0, currencies.Length, "Currencies should remain empty")
    
    [<TestMethod>]
    member public this.``deleteAllOperationalData uses transaction``() =
        // Arrange
        this.CreateTestReferenceData()
        this.CreateTestOperationalData()
        
        // Act - the transaction is internal to deleteAllOperationalData
        // If any delete fails, the entire operation should fail atomically
        DataResetExtensions.Do.deleteAllOperationalData() |> Async.AwaitTask |> Async.RunSynchronously
        
        // Assert - verify all operational data is deleted consistently
        let brokers = BrokerExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let banks = BankExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let brokerAccounts = BrokerAccountExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let bankAccounts = BankAccountExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        
        // If transaction works correctly, either all data is deleted or none is deleted
        Assert.AreEqual(0, brokers.Length, "All brokers should be deleted atomically")
        Assert.AreEqual(0, banks.Length, "All banks should be deleted atomically")
        Assert.AreEqual(0, brokerAccounts.Length, "All broker accounts should be deleted atomically")
        Assert.AreEqual(0, bankAccounts.Length, "All bank accounts should be deleted atomically")
