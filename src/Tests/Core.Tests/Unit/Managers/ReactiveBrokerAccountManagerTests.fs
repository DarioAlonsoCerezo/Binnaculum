namespace Core.Tests

open NUnit.Framework
open System.Collections.Generic
open Binnaculum.Core.Models
open Binnaculum.Core.UI

[<TestFixture>]
type ReactiveBrokerAccountManagerTests() =

    [<SetUp>]
    member this.Setup() =
        // Clear collections before each test using Edit method
        Collections.Accounts.Edit(fun list -> list.Clear())
        Collections.Brokers.Edit(fun list -> list.Clear())
        ReactiveBrokerAccountManager.dispose()

    [<Test>]
    member this.``ReactiveBrokerAccountManager should provide fast lookup by ID``() =
        // Arrange
        let broker = {
            Id = 1
            Name = "Test Broker"
            Image = "test.png"
            SupportedBroker = SupportedBroker.Unknown
        }
        
        let brokerAccount = {
            Id = 1
            AccountNumber = "ACC001"
            Broker = broker
        }
        
        let account = {
            Type = AccountType.BrokerAccount
            Broker = Some brokerAccount
            Bank = None
            HasMovements = false
        }
        
        // Add to collections using Edit method
        Collections.Brokers.Edit(fun list -> list.Add(broker))
        Collections.Accounts.Edit(fun list -> list.Add(account))
        
        // Initialize reactive manager
        ReactiveBrokerAccountManager.initialize()
        
        // Act
        let brokerAccountId = 1
        let result = brokerAccountId.ToFastBrokerAccountById()
        
        // Assert
        Assert.That(result.Id, Is.EqualTo(1))
        Assert.That(result.AccountNumber, Is.EqualTo("ACC001"))
        Assert.That(result.Broker.Name, Is.EqualTo("Test Broker"))

    [<Test>]
    member this.``ReactiveBrokerAccountManager should provide fast lookup by AccountNumber``() =
        // Arrange
        let broker = {
            Id = 1
            Name = "Test Broker"
            Image = "test.png"
            SupportedBroker = SupportedBroker.Unknown
        }
        
        let brokerAccount = {
            Id = 1
            AccountNumber = "ACC001"
            Broker = broker
        }
        
        let account = {
            Type = AccountType.BrokerAccount
            Broker = Some brokerAccount
            Bank = None
            HasMovements = false
        }
        
        // Add to collections using Edit method
        Collections.Brokers.Edit(fun list -> list.Add(broker))
        Collections.Accounts.Edit(fun list -> list.Add(account))
        
        // Initialize reactive manager
        ReactiveBrokerAccountManager.initialize()
        
        // Act
        let accountNumber = "ACC001"
        let result = accountNumber.ToFastBrokerAccountByAccountNumber()
        
        // Assert
        Assert.That(result.IsSome, Is.True)
        Assert.That(result.Value.Id, Is.EqualTo(1))
        Assert.That(result.Value.AccountNumber, Is.EqualTo("ACC001"))

    [<Test>]
    member this.``ReactiveBrokerAccountManager should handle missing broker account gracefully``() =
        // Arrange
        ReactiveBrokerAccountManager.initialize()
        
        // Act & Assert - should throw because broker account doesn't exist
        let nonExistentId = 999
        Assert.Throws<System.Collections.Generic.KeyNotFoundException>(fun () -> 
            nonExistentId.ToFastBrokerAccountById() |> ignore) |> ignore
        
        // Act for AccountNumber lookup - should return None
        let nonExistentAccountNumber = "NONEXISTENT"
        let result = nonExistentAccountNumber.ToFastBrokerAccountByAccountNumber()
        Assert.That(result.IsNone, Is.True)

    [<Test>]
    member this.``ReactiveBrokerAccountManager should update cache when accounts change``() =
        // Arrange
        let broker = {
            Id = 1
            Name = "Test Broker"
            Image = "test.png"
            SupportedBroker = SupportedBroker.Unknown
        }
        
        let brokerAccount = {
            Id = 1
            AccountNumber = "ACC001"
            Broker = broker
        }
        
        let account = {
            Type = AccountType.BrokerAccount
            Broker = Some brokerAccount
            Bank = None
            HasMovements = false
        }
        
        // Initialize reactive manager first
        ReactiveBrokerAccountManager.initialize()
        
        // Add to collections after initialization to test reactive updates
        Collections.Brokers.Edit(fun list -> list.Add(broker))
        Collections.Accounts.Edit(fun list -> list.Add(account))
        
        // Act
        let brokerAccountId = 1
        let result = brokerAccountId.ToFastBrokerAccountById()
        
        // Assert
        Assert.That(result.Id, Is.EqualTo(1))
        Assert.That(result.AccountNumber, Is.EqualTo("ACC001"))

    [<Test>]
    member this.``DatabaseToModels extensions should work with new fast lookups``() =
        // Arrange
        let broker = {
            Id = 1
            Name = "Test Broker"
            Image = "test.png"
            SupportedBroker = SupportedBroker.Unknown
        }
        
        let brokerAccount = {
            Id = 1
            AccountNumber = "ACC001"
            Broker = broker
        }
        
        let account = {
            Type = AccountType.BrokerAccount
            Broker = Some brokerAccount
            Bank = None
            HasMovements = false
        }
        
        Collections.Brokers.Edit(fun list -> list.Add(broker))
        Collections.Accounts.Edit(fun list -> list.Add(account))
        ReactiveBrokerAccountManager.initialize()
        
        // Act - test the extension method directly as used in DatabaseToModels
        let brokerAccountId = 1
        let result = brokerAccountId.ToFastBrokerAccountById()
        
        // Assert
        Assert.That(result.Id, Is.EqualTo(1))
        Assert.That(result.AccountNumber, Is.EqualTo("ACC001"))
        Assert.That(result.Broker.Id, Is.EqualTo(1))
        Assert.That(result.Broker.Name, Is.EqualTo("Test Broker"))

    [<TearDown>]
    member this.TearDown() =
        // Clean up after each test using Edit method
        Collections.Accounts.Edit(fun list -> list.Clear())
        Collections.Brokers.Edit(fun list -> list.Clear())
        ReactiveBrokerAccountManager.dispose()