namespace Core.Tests.Unit.Managers

open Microsoft.VisualStudio.TestTools.UnitTesting
open Binnaculum.Core.Database
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Tests
open Binnaculum.Core.Patterns
open Binnaculum.Core.Models
open Binnaculum.Core.UI
open System

/// <summary>
/// Unit tests for ReactiveMovementManager bounded loading.
/// Tests verify max 50 movements per account and collection behavior.
/// </summary>
[<TestClass>]
type ReactiveMovementManagerTests() =
    inherit InMemoryDatabaseFixture()
    
    [<TestInitialize>]
    member this.Setup() =
        // Clear Collections before each test
        Collections.Accounts.Clear()
        Collections.Movements.Clear()
    
    [<TestMethod>]
    member this.``refreshAsync handles empty accounts without error``() =
        task {
            // Arrange - create account with no movements
            let broker = {
                Id = 0
                Name = "Test Broker"
                Image = ""
                SupportedBroker = SupportedBroker.InteractiveBrokers
                Audit = Do.createAudit()
            }
            let! savedBroker = BrokerExtensions.Do.save broker
            
            let brokerAccount = {
                Id = 0
                BrokerId = savedBroker.Id
                AccountNumber = "ACC001"
                Audit = Do.createAudit()
            }
            let! savedBrokerAccount = BrokerAccountExtensions.Do.save brokerAccount
            
            // Add to Collections.Accounts
            let account = {
                Type = AccountType.BrokerAccount
                Broker = Some savedBrokerAccount
                Bank = None
                HasMovements = false
            }
            Collections.Accounts.Add(account)
            
            // Act - should not throw
            do! ReactiveMovementManager.refreshAsync()
            
            // Assert
            let movementsCount = Collections.Movements.Count
            Assert.AreEqual(0, movementsCount, "Should handle empty accounts without error")
        }
    
    [<TestMethod>]
    member this.``refreshAsync loads bounded number of movements per account``() =
        task {
            // Arrange
            let currency = { Id = 0; Name = "US Dollar"; Code = "USD"; Symbol = "$" }
            let! savedCurrency = CurrencyExtensions.Do.save currency
            
            let broker = {
                Id = 0
                Name = "Test Broker"
                Image = ""
                SupportedBroker = SupportedBroker.InteractiveBrokers
                Audit = Do.createAudit()
            }
            let! savedBroker = BrokerExtensions.Do.save broker
            
            let brokerAccount = {
                Id = 0
                BrokerId = savedBroker.Id
                AccountNumber = "ACC001"
                Audit = Do.createAudit()
            }
            let! savedBrokerAccount = BrokerAccountExtensions.Do.save brokerAccount
            
            // Create 100 broker movements (should only load 50)
            for i in 1 .. 100 do
                let timestamp = DateTimePattern.FromDateTime(DateTime.UtcNow.AddDays(float -i))
                let movement = {
                    Id = 0
                    TimeStamp = timestamp
                    Amount = Money.FromAmount(decimal (i * 100))
                    CurrencyId = savedCurrency.Id
                    BrokerAccountId = savedBrokerAccount.Id
                    Commissions = Money.FromAmount(1m)
                    Fees = Money.FromAmount(1m)
                    MovementType = BrokerMovementType.Deposit
                    Notes = None
                    FromCurrencyId = None
                    AmountChanged = None
                    TickerId = None
                    Quantity = None
                    Audit = Do.createAudit()
                }
                let! _ = BrokerMovementExtensions.Do.save movement
                ()
            
            // Add to Collections.Accounts
            let account = {
                Type = AccountType.BrokerAccount
                Broker = Some savedBrokerAccount
                Bank = None
                HasMovements = false
            }
            Collections.Accounts.Add(account)
            
            // Act
            do! ReactiveMovementManager.refreshAsync()
            
            // Assert
            let movementsCount = Collections.Movements.Count
            Assert.IsTrue(movementsCount <= 50, "Should load max 50 movements per account")
            Assert.IsTrue(movementsCount > 0, "Should load some movements")
        }
    
    [<TestMethod>]
    member this.``refreshAsync loads movements for multiple accounts``() =
        task {
            // Arrange
            let currency = { Id = 0; Name = "US Dollar"; Code = "USD"; Symbol = "$" }
            let! savedCurrency = CurrencyExtensions.Do.save currency
            
            let broker = {
                Id = 0
                Name = "Test Broker"
                Image = ""
                SupportedBroker = SupportedBroker.InteractiveBrokers
                Audit = Do.createAudit()
            }
            let! savedBroker = BrokerExtensions.Do.save broker
            
            // Create 2 broker accounts
            let brokerAccount1 = {
                Id = 0
                BrokerId = savedBroker.Id
                AccountNumber = "ACC001"
                Audit = Do.createAudit()
            }
            let! savedBrokerAccount1 = BrokerAccountExtensions.Do.save brokerAccount1
            
            let brokerAccount2 = {
                Id = 0
                BrokerId = savedBroker.Id
                AccountNumber = "ACC002"
                Audit = Do.createAudit()
            }
            let! savedBrokerAccount2 = BrokerAccountExtensions.Do.save brokerAccount2
            
            // Create 30 movements for each account
            for i in 1 .. 30 do
                let timestamp = DateTimePattern.FromDateTime(DateTime.UtcNow.AddDays(float -i))
                let movement1 = {
                    Id = 0
                    TimeStamp = timestamp
                    Amount = Money.FromAmount(decimal (i * 100))
                    CurrencyId = savedCurrency.Id
                    BrokerAccountId = savedBrokerAccount1.Id
                    Commissions = Money.FromAmount(1m)
                    Fees = Money.FromAmount(1m)
                    MovementType = BrokerMovementType.Deposit
                    Notes = None
                    FromCurrencyId = None
                    AmountChanged = None
                    TickerId = None
                    Quantity = None
                    Audit = Do.createAudit()
                }
                let! _ = BrokerMovementExtensions.Do.save movement1
                
                let movement2 = {
                    Id = 0
                    TimeStamp = timestamp
                    Amount = Money.FromAmount(decimal (i * 100))
                    CurrencyId = savedCurrency.Id
                    BrokerAccountId = savedBrokerAccount2.Id
                    Commissions = Money.FromAmount(1m)
                    Fees = Money.FromAmount(1m)
                    MovementType = BrokerMovementType.Deposit
                    Notes = None
                    FromCurrencyId = None
                    AmountChanged = None
                    TickerId = None
                    Quantity = None
                    Audit = Do.createAudit()
                }
                let! _ = BrokerMovementExtensions.Do.save movement2
                ()
            
            // Add both accounts to Collections.Accounts
            let account1 = {
                Type = AccountType.BrokerAccount
                Broker = Some savedBrokerAccount1
                Bank = None
                HasMovements = false
            }
            Collections.Accounts.Add(account1)
            
            let account2 = {
                Type = AccountType.BrokerAccount
                Broker = Some savedBrokerAccount2
                Bank = None
                HasMovements = false
            }
            Collections.Accounts.Add(account2)
            
            // Act
            do! ReactiveMovementManager.refreshAsync()
            
            // Assert
            let movementsCount = Collections.Movements.Count
            Assert.IsTrue(movementsCount > 0, "Should load movements from both accounts")
            Assert.IsTrue(movementsCount <= 100, "Should load max 50 movements per account (2 accounts = max 100)")
            // With only 30 movements per account, we should get all 60
            Assert.AreEqual(60, movementsCount, "Should load all 60 movements when under the 50 per account limit")
        }
