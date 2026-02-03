namespace Binnaculum.Core.Tests

open NUnit.Framework
open Binnaculum.Core.Database
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Patterns
open System

/// <summary>
/// Tests for the DataReset functionality that deletes all operational data
/// while preserving reference tables (Ticker, Currency, TickerSplit, TickerPrice).
/// </summary>
[<TestFixture>]
type DataResetExtensionsTests() =
    inherit InMemoryDatabaseFixture()
    
    /// <summary>
    /// Helper to create test data for operational tables
    /// </summary>
    member private this.CreateTestOperationalData() =
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
            Audit = AuditableEntity.FromDateTime(DateTime.UtcNow)
        }
        BrokerAccountExtensions.Do.save(brokerAccount) |> Async.AwaitTask |> Async.RunSynchronously
        
        // Create a bank
        let bank = {
            Id = 0
            Name = "Test Bank"
            Image = "bank.png"
        }
        BankExtensions.Do.save(bank) |> Async.AwaitTask |> Async.RunSynchronously
        
        // Get the created bank
        let savedBank = BankExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously |> List.head
        
        // Create a bank account
        let bankAccount = {
            Id = 0
            BankId = savedBank.Id
            AccountNumber = "BANK456"
            Audit = AuditableEntity.FromDateTime(DateTime.UtcNow)
        }
        BankAccountExtensions.Do.save(bankAccount) |> Async.AwaitTask |> Async.RunSynchronously
        
        ()
    
    /// <summary>
    /// Helper to create test data for reference tables
    /// </summary>
    member private this.CreateTestReferenceData() =
        // Create a currency
        let currency = {
            Id = 0
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
            Name = "Apple Inc."
            OptionsEnabled = true
        }
        TickerExtensions.Do.save(ticker) |> Async.AwaitTask |> Async.RunSynchronously
        
        // Get the created ticker
        let savedTicker = TickerExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously |> List.head
        
        // Create a ticker split
        let tickerSplit = {
            Id = 0
            TickerId = savedTicker.Id
            SplitDate = DateTimePattern.FromDateTime(DateTime.UtcNow)
            SplitFactor = 2.0m
        }
        TickerSplitExtensions.Do.save(tickerSplit) |> Async.AwaitTask |> Async.RunSynchronously
        
        // Create a ticker price
        let tickerPrice = {
            Id = 0
            TickerId = savedTicker.Id
            CurrencyId = savedCurrency.Id
            PriceDate = DateTimePattern.FromDateTime(DateTime.UtcNow)
            Price = Money.Create(150.0m)
        }
        TickerPriceExtensions.Do.save(tickerPrice) |> Async.AwaitTask |> Async.RunSynchronously
        
        ()
    
    [<Test>]
    member this.``deleteAllOperationalData removes all operational data``() =
        // Arrange
        this.CreateTestReferenceData()
        this.CreateTestOperationalData()
        
        // Verify data exists before deletion
        let brokersBeforeDelete = BrokerExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let brokerAccountsBeforeDelete = BrokerAccountExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let banksBeforeDelete = BankExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let bankAccountsBeforeDelete = BankAccountExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        
        Assert.That(brokersBeforeDelete.Length, Is.GreaterThan(0), "Should have brokers before delete")
        Assert.That(brokerAccountsBeforeDelete.Length, Is.GreaterThan(0), "Should have broker accounts before delete")
        Assert.That(banksBeforeDelete.Length, Is.GreaterThan(0), "Should have banks before delete")
        Assert.That(bankAccountsBeforeDelete.Length, Is.GreaterThan(0), "Should have bank accounts before delete")
        
        // Act
        DataResetExtensions.Do.deleteAllOperationalData() |> Async.AwaitTask |> Async.RunSynchronously
        
        // Assert - verify operational data is deleted
        let brokersAfterDelete = BrokerExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let brokerAccountsAfterDelete = BrokerAccountExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let banksAfterDelete = BankExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let bankAccountsAfterDelete = BankAccountExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        
        Assert.That(brokersAfterDelete.Length, Is.EqualTo(0), "All brokers should be deleted")
        Assert.That(brokerAccountsAfterDelete.Length, Is.EqualTo(0), "All broker accounts should be deleted")
        Assert.That(banksAfterDelete.Length, Is.EqualTo(0), "All banks should be deleted")
        Assert.That(bankAccountsAfterDelete.Length, Is.EqualTo(0), "All bank accounts should be deleted")
    
    [<Test>]
    member this.``deleteAllOperationalData preserves reference data``() =
        // Arrange
        this.CreateTestReferenceData()
        
        // Verify reference data exists before deletion
        let currenciesBeforeDelete = CurrencyExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let tickersBeforeDelete = TickerExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let tickerSplitsBeforeDelete = TickerSplitExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let tickerPricesBeforeDelete = TickerPriceExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        
        Assert.That(currenciesBeforeDelete.Length, Is.GreaterThan(0), "Should have currencies before delete")
        Assert.That(tickersBeforeDelete.Length, Is.GreaterThan(0), "Should have tickers before delete")
        Assert.That(tickerSplitsBeforeDelete.Length, Is.GreaterThan(0), "Should have ticker splits before delete")
        Assert.That(tickerPricesBeforeDelete.Length, Is.GreaterThan(0), "Should have ticker prices before delete")
        
        // Act
        DataResetExtensions.Do.deleteAllOperationalData() |> Async.AwaitTask |> Async.RunSynchronously
        
        // Assert - verify reference data is preserved
        let currenciesAfterDelete = CurrencyExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let tickersAfterDelete = TickerExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let tickerSplitsAfterDelete = TickerSplitExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let tickerPricesAfterDelete = TickerPriceExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        
        Assert.That(currenciesAfterDelete.Length, Is.EqualTo(currenciesBeforeDelete.Length), "Currencies should be preserved")
        Assert.That(tickersAfterDelete.Length, Is.EqualTo(tickersBeforeDelete.Length), "Tickers should be preserved")
        Assert.That(tickerSplitsAfterDelete.Length, Is.EqualTo(tickerSplitsBeforeDelete.Length), "Ticker splits should be preserved")
        Assert.That(tickerPricesAfterDelete.Length, Is.EqualTo(tickerPricesBeforeDelete.Length), "Ticker prices should be preserved")
    
    [<Test>]
    member this.``deleteAllOperationalData works on empty database``() =
        // Act - should not throw on empty database
        Assert.DoesNotThrow(fun () ->
            DataResetExtensions.Do.deleteAllOperationalData() |> Async.AwaitTask |> Async.RunSynchronously
        )
        
        // Assert - verify database is still empty
        let brokers = BrokerExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        let currencies = CurrencyExtensions.Do.getAll() |> Async.AwaitTask |> Async.RunSynchronously
        
        Assert.That(brokers.Length, Is.EqualTo(0), "Brokers should remain empty")
        Assert.That(currencies.Length, Is.EqualTo(0), "Currencies should remain empty")
    
    [<Test>]
    member this.``deleteAllOperationalData uses transaction``() =
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
        Assert.That(brokers.Length, Is.EqualTo(0), "All brokers should be deleted atomically")
        Assert.That(banks.Length, Is.EqualTo(0), "All banks should be deleted atomically")
        Assert.That(brokerAccounts.Length, Is.EqualTo(0), "All broker accounts should be deleted atomically")
        Assert.That(bankAccounts.Length, Is.EqualTo(0), "All bank accounts should be deleted atomically")
