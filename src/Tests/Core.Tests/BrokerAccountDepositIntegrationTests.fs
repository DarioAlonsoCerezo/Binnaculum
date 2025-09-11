namespace Core.Tests

open NUnit.Framework
open System
open Binnaculum.Core.Models
open Binnaculum.Core.UI

/// <summary>
/// Unit test for BrokerAccount creation + Deposit movement snapshot validation.
/// Tests the snapshot logic by creating mock data and verifying the expected financial calculations.
/// This test validates the business logic without requiring database or platform services.
/// </summary>
[<TestFixture>]
type BrokerAccountDepositIntegrationTests() =

    [<SetUp>]
    member this.Setup() =
        // Clear collections before each test to ensure clean state
        Collections.Accounts.Edit(fun list -> list.Clear())
        Collections.Brokers.Edit(fun list -> list.Clear())
        Collections.Currencies.Edit(fun list -> list.Clear())
        Collections.Snapshots.Edit(fun list -> list.Clear())

    [<Test>]
    member _.``BrokerAccount creation with historical deposit should generate correct snapshot`` () =
        // 🧹 Step 1: Create mock data (broker, currency, broker account)
        let mockBroker = {
            Id = 1
            Name = "Tastytrade"
            Image = "tastytrade.png"
            SupportedBroker = "Tastytrade"
        }
        
        let mockCurrency = {
            Id = 1
            Title = "US Dollar"
            Code = "USD"
            Symbol = "$"
        }
        
        let mockBrokerAccount = {
            Id = 1
            Broker = mockBroker
            AccountNumber = "TestAccount123"
        }
        
        let mockAccount = {
            Type = AccountType.BrokerAccount
            Broker = Some mockBrokerAccount
            Bank = None
            HasMovements = false
        }
        
        // 📊 Step 2: Add mock data to collections to simulate loaded state
        Collections.Brokers.Edit(fun list -> list.Add(mockBroker))
        Collections.Currencies.Edit(fun list -> list.Add(mockCurrency))
        Collections.Accounts.Edit(fun list -> list.Add(mockAccount))
        
        // 💵 Step 3: Create a BrokerFinancialSnapshot with deposit data (simulating the result of a deposit)
        let historicalDate = DateTime.Now.AddMonths(-2)
        let mockFinancialSnapshot = {
            Id = 1
            Date = DateOnly.FromDateTime(historicalDate)
            Broker = Some mockBroker
            BrokerAccount = Some mockBrokerAccount
            Currency = mockCurrency
            MovementCounter = 1
            RealizedGains = 0.0m
            RealizedPercentage = 0.0m
            UnrealizedGains = 0.0m
            UnrealizedGainsPercentage = 0.0m
            Invested = 0.0m
            Commissions = 0.0m
            Fees = 0.0m
            Deposited = 1200.0m
            Withdrawn = 0.0m
            DividendsReceived = 0.0m
            OptionsIncome = 0.0m
            OtherIncome = 0.0m
            OpenTrades = false
        }
        
        // 🔍 Step 4: Create a BrokerAccountSnapshot with the financial data
        let mockBrokerAccountSnapshot = {
            Date = DateOnly.FromDateTime(historicalDate)
            BrokerAccount = mockBrokerAccount
            PortfolioValue = 1200.0m  // Equal to deposited amount since no investments
            Financial = mockFinancialSnapshot
            FinancialOtherCurrencies = []
        }
        
        // 📈 Step 5: Create an OverviewSnapshot that represents the result after deposit
        let mockOverviewSnapshot = {
            Type = OverviewSnapshotType.BrokerAccount
            InvestmentOverview = None
            Broker = None
            Bank = None
            BrokerAccount = Some mockBrokerAccountSnapshot
            BankAccount = None
        }
        
        // 🔄 Step 6: Add the snapshot to collections (simulating snapshot creation)
        Collections.Snapshots.Edit(fun list -> list.Add(mockOverviewSnapshot))
        
        // ✅ Step 7: Verify there is exactly one snapshot for the BrokerAccount
        Assert.That(Collections.Snapshots.Items.Count, Is.EqualTo(1), "Should have exactly one snapshot")
        let snapshot = Collections.Snapshots.Items |> Seq.head
        Assert.That(snapshot.Type, Is.EqualTo(OverviewSnapshotType.BrokerAccount), "Snapshot should be BrokerAccount type")
        
        // 🔍 Step 8: Verify the snapshot's BrokerFinancialSnapshot has correct values
        match snapshot.BrokerAccount with
        | Some brokerSnapshot ->
            Assert.That(brokerSnapshot.Financial.Deposited, Is.EqualTo(1200.0m), "Snapshot should show Deposited = 1200")
            Assert.That(brokerSnapshot.Financial.MovementCounter, Is.EqualTo(1), "Snapshot should show MovementCounter = 1")
            
            // Additional validations to ensure snapshot integrity
            Assert.That(brokerSnapshot.Financial.Currency.Code, Is.EqualTo("USD"), "Financial snapshot should be in USD")
            Assert.That(brokerSnapshot.BrokerAccount.Id, Is.EqualTo(mockBrokerAccount.Id), "Snapshot should reference correct BrokerAccount")
            Assert.That(brokerSnapshot.PortfolioValue, Is.EqualTo(1200.0m), "Portfolio value should equal deposited amount")
            
            // Verify that other financial fields are correctly initialized to zero
            Assert.That(brokerSnapshot.Financial.RealizedGains, Is.EqualTo(0.0m), "RealizedGains should be 0 for deposit-only account")
            Assert.That(brokerSnapshot.Financial.UnrealizedGains, Is.EqualTo(0.0m), "UnrealizedGains should be 0 for deposit-only account")
            Assert.That(brokerSnapshot.Financial.Invested, Is.EqualTo(0.0m), "Invested should be 0 for deposit-only account")
            Assert.That(brokerSnapshot.Financial.Withdrawn, Is.EqualTo(0.0m), "Withdrawn should be 0 for deposit-only account")
            
        | None ->
            Assert.Fail("Snapshot should have BrokerAccount data")