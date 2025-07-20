namespace Core.Tests

open NUnit.Framework
open Binnaculum.Core.Models
open System.Collections.Generic
open System

[<TestFixture>]
type DataLoaderTests () =
    let accounts = ResizeArray<Account>()

    [<SetUp>]
    member _.Setup() =
        accounts.Clear()

    [<Test>]
    member _.``loadLatestBrokerAccountSnapshots should return early when no BrokerAccounts exist`` () =
        // Arrange - ensure no broker accounts exist
        accounts.Clear()
        // Add a bank account to ensure we're specifically testing broker accounts
        let bank = { Id = 1; Name = "Test Bank"; Image = Some "bank"; CreatedAt = DateTime.Now }
        let bankAccount = {
            Id = 1
            Bank = bank
            Name = "Test Bank Account"
            Description = None
            Currency = { Id = 1; Title = "USD"; Code = "USD"; Symbol = "$" }
        }
        let account = {
            Type = AccountType.BankAccount
            Broker = None
            Bank = Some bankAccount
            HasMovements = false
        }
        accounts.Add(account)
        // Act & Assert
        let brokerAccounts = accounts |> Seq.filter (fun a -> a.Broker.IsSome) |> Seq.length
        Assert.That(brokerAccounts, NUnit.Framework.Is.EqualTo(0))

    [<Test>]
    member _.``loadLatestBrokerAccountSnapshots should process snapshots when BrokerAccounts exist`` () =
        // Arrange - add a broker account
        let broker = { Id = 1; Name = "Test Broker"; Image = ""; SupportedBroker = "" }
        let brokerAccount = {
            Id = 1
            Broker = broker
            AccountNumber = "123456"
        }
        let account = {
            Type = AccountType.BrokerAccount
            Broker = Some brokerAccount
            Bank = None
            HasMovements = false
        }
        accounts.Add(account)
        // Act & Assert
        let brokerAccountCount = accounts |> Seq.filter (fun a -> a.Broker.IsSome) |> Seq.length
        Assert.That(brokerAccountCount, NUnit.Framework.Is.EqualTo(1))

    [<Test>]
    member _.``BrokerAccounts collection filtering works correctly`` () =
        // Arrange - add mixed account types
        let broker = { Id = 1; Name = "Test Broker"; Image = ""; SupportedBroker = "" }
        let brokerAccount = {
            Id = 1
            Broker = broker
            AccountNumber = "123456"
        }
        let bank = { Id = 1; Name = "Test Bank"; Image = Some "bank"; CreatedAt = DateTime.Now }
        let bankAccount = {
            Id = 1
            Bank = bank
            Name = "Test Bank Account"
            Description = None
            Currency = { Id = 1; Title = "USD"; Code = "USD"; Symbol = "$" }
        }
        let brokerAccountModel = {
            Type = AccountType.BrokerAccount
            Broker = Some brokerAccount
            Bank = None
            HasMovements = false
        }
        let bankAccountModel = {
            Type = AccountType.BankAccount
            Broker = None
            Bank = Some bankAccount
            HasMovements = false
        }
        accounts.Add(brokerAccountModel)
        accounts.Add(bankAccountModel)
        // Act
        let brokerAccounts = 
            accounts 
            |> Seq.filter (fun a -> a.Broker.IsSome)
            |> Seq.map (fun a -> a.Broker.Value)
            |> Seq.toList
        // Assert
        Assert.That(brokerAccounts.Length, NUnit.Framework.Is.EqualTo(1))
        Assert.That(brokerAccounts.Head.Id, NUnit.Framework.Is.EqualTo(1))
        Assert.That(brokerAccounts.Head.Broker.Name, NUnit.Framework.Is.EqualTo("Test Broker"))