namespace Core.Tests.Unit.Database

open NUnit.Framework
open Binnaculum.Core.Database
open Binnaculum.Core.Database.DatabaseModel
open Binnaculum.Core.Tests
open Binnaculum.Core.Patterns
open System

/// <summary>
/// Unit tests for pagination methods across all movement types.
/// Tests verify LIMIT/OFFSET behavior and proper ordering.
/// </summary>
[<TestFixture>]
type PaginationMethodsTests() =
    inherit InMemoryDatabaseFixture()
    
    [<Test>]
    member this.``loadMovementsPaged handles empty results correctly``() =
        task {
            // Arrange - create currency, broker and account but no movements
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
            
            let account = {
                Id = 0
                BrokerId = savedBroker.Id
                AccountNumber = "TEST123"
                Audit = Do.createAudit()
            }
            let! savedAccount = BrokerAccountExtensions.Do.save account
            
            // Act - try to load page when no movements exist
            let! page = BrokerMovementExtensions.Do.loadMovementsPaged(savedAccount.Id, 0, 50)
            
            // Assert
            Assert.That(page.Length, Is.EqualTo(0), "Empty result should return empty list")
        }
    
    [<Test>]
    member this.``loadMovementsPaged respects page size limit``() =
        task {
            // Arrange - create test data
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
            
            let account = {
                Id = 0
                BrokerId = savedBroker.Id
                AccountNumber = "TEST123"
                Audit = Do.createAudit()
            }
            let! savedAccount = BrokerAccountExtensions.Do.save account
            
            // Create 75 movements
            for i in 1 .. 75 do
                let timestamp = DateTimePattern.FromDateTime(DateTime.UtcNow.AddDays(float -i))
                let movement = {
                    Id = 0
                    TimeStamp = timestamp
                    Amount = Money.FromAmount(decimal (i * 100))
                    CurrencyId = savedCurrency.Id
                    BrokerAccountId = savedAccount.Id
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
            
            // Act - load first page with limit 50
            let! firstPage = BrokerMovementExtensions.Do.loadMovementsPaged(savedAccount.Id, 0, 50)
            
            // Assert
            Assert.That(firstPage.Length, Is.EqualTo(50), "First page should contain exactly 50 movements")
            
            // Act - load second page
            let! secondPage = BrokerMovementExtensions.Do.loadMovementsPaged(savedAccount.Id, 1, 50)
            
            // Assert  
            Assert.That(secondPage.Length, Is.EqualTo(25), "Second page should contain remaining 25 movements")
        }
    
    [<Test>]
    member this.``getMovementCount returns correct total``() =
        task {
            // Arrange
            let currency = { Id = 0; Name = "US Dollar"; Code = "USD"; Symbol = "$" }
            let! savedCurrency = CurrencyExtensions.Do.save currency
            
            let broker = {
                Id = 0
                Name = "Test Broker"
                Image = ""
                SupportedBroker = SupportedBrokers.InteractiveBrokers
                Audit = Do.createAudit()
            }
            let! savedBroker = BrokerExtensions.Do.save broker
            
            let account = {
                Id = 0
                BrokerId = savedBroker.Id
                AccountNumber = "TEST123"
                Audit = Do.createAudit()
            }
            let! savedAccount = BrokerAccountExtensions.Do.save account
            
            // Create 42 movements
            for i in 1 .. 42 do
                let timestamp = DateTimePattern.FromDateTime(DateTime.UtcNow.AddDays(float -i))
                let movement = {
                    Id = 0
                    TimeStamp = timestamp
                    Amount = Money.FromAmount(decimal (i * 100))
                    CurrencyId = savedCurrency.Id
                    BrokerAccountId = savedAccount.Id
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
            
            // Act
            let! count = BrokerMovementExtensions.Do.getMovementCount(savedAccount.Id)
            
            // Assert
            Assert.That(count, Is.EqualTo(42), "Count should match total movements created")
        }
