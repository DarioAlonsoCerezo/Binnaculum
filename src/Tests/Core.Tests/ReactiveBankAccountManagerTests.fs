namespace Core.Tests

open NUnit.Framework
open System
open System.Diagnostics
open Binnaculum.Core.UI
open Binnaculum.Core.Models

[<TestFixture>]
type ReactiveBankAccountManagerTests() =

    let createSampleBank(id: int, name: string) = 
        { 
            Id = id
            Name = name
            Image = Some "bank"
            CreatedAt = DateTime.Now 
        }

    let createSampleCurrency(id: int, code: string) = 
        { 
            Id = id
            Title = $"Currency {code}"
            Code = code
            Symbol = "$" 
        }

    let createSampleBankAccount(id: int, bankId: int, currencyId: int, name: string) = 
        {
            Id = id
            Bank = createSampleBank(bankId, $"Bank {bankId}")
            Currency = createSampleCurrency(currencyId, $"CUR{currencyId}")
            Name = name
            Description = Some $"Bank Account {name}"
        }

    let createSampleAccount(bankAccount: BankAccount) = 
        {
            Type = AccountType.BankAccount
            Broker = None
            Bank = Some bankAccount
            HasMovements = false
        }

    [<SetUp>]
    member this.Setup() =
        // Clear and populate collections
        Collections.Accounts.Edit(fun list -> list.Clear())
        Collections.Banks.Edit(fun list -> list.Clear())
        Collections.Currencies.Edit(fun list -> list.Clear())
        
        // Add sample data
        let banks = [
            createSampleBank(1, "Bank of America")
            createSampleBank(2, "Wells Fargo")
            createSampleBank(3, "Chase Bank")
        ]
        
        let currencies = [
            createSampleCurrency(1, "USD")
            createSampleCurrency(2, "EUR")
            createSampleCurrency(3, "GBP")
        ]
        
        let bankAccounts = [
            { Id = 1; Bank = banks.[0]; Currency = currencies.[0]; Name = "Checking"; Description = Some "Checking Account" }
            { Id = 2; Bank = banks.[1]; Currency = currencies.[0]; Name = "Savings"; Description = Some "Savings Account" }
            { Id = 3; Bank = banks.[0]; Currency = currencies.[1]; Name = "EUR Account"; Description = Some "EUR Account" }
        ]
        
        Collections.Banks.Edit(fun list -> list.AddRange(banks))
        Collections.Currencies.Edit(fun list -> list.AddRange(currencies))
        
        let accounts = bankAccounts |> List.map createSampleAccount
        Collections.Accounts.Edit(fun list -> list.AddRange(accounts))
        
        // Initialize the reactive manager
        ReactiveBankAccountManager.initialize()

    [<TearDown>]
    member this.TearDown() =
        ReactiveBankAccountManager.dispose()

    [<Test>]
    member this.``ReactiveBankAccountManager should initialize successfully``() =
        // Test passes if no exceptions are thrown during setup
        Assert.Pass("ReactiveBankAccountManager initialized successfully")

    [<Test>]
    member this.``Fast lookup should return correct bank account by ID``() =
        let bankAccount = ReactiveBankAccountManager.getBankAccountByIdFast(1)
        
        Assert.That(bankAccount.Id, Is.EqualTo(1))
        Assert.That(bankAccount.Name, Is.EqualTo("Checking"))
        Assert.That(bankAccount.Bank.Name, Is.EqualTo("Bank of America"))
        Assert.That(bankAccount.Currency.Code, Is.EqualTo("USD"))

    [<Test>]
    member this.``Fast lookup should work for multiple bank accounts``() =
        let bankAccount1 = ReactiveBankAccountManager.getBankAccountByIdFast(1)
        let bankAccount2 = ReactiveBankAccountManager.getBankAccountByIdFast(2)
        let bankAccount3 = ReactiveBankAccountManager.getBankAccountByIdFast(3)
        
        Assert.That(bankAccount1.Name, Is.EqualTo("Checking"))
        Assert.That(bankAccount2.Name, Is.EqualTo("Savings"))
        Assert.That(bankAccount3.Name, Is.EqualTo("EUR Account"))
        Assert.That(bankAccount3.Currency.Code, Is.EqualTo("EUR"))

    [<Test>]
    member this.``Reactive observable should emit bank account``() =
        let observable = ReactiveBankAccountManager.getBankAccountByIdReactive(1)
        
        let mutable resultBankAccount = { Id = 0; Bank = createSampleBank(0, ""); Currency = createSampleCurrency(0, ""); Name = ""; Description = None }
        let mutable testCompleted = false
        
        observable.Subscribe(fun bankAccount ->
            resultBankAccount <- bankAccount
            testCompleted <- true) |> ignore
        
        // Wait briefly for observable to emit
        System.Threading.Thread.Sleep(10)
        
        Assert.That(testCompleted, Is.True)
        Assert.That(resultBankAccount.Name, Is.EqualTo("Checking"))
        Assert.That(resultBankAccount.Id, Is.EqualTo(1))

    [<Test>]
    member this.``Reactive cache should update when bank account is added``() =
        // Add a new bank account
        let newBank = createSampleBank(4, "New Bank")
        let newCurrency = createSampleCurrency(4, "CAD")
        let newBankAccount = { Id = 4; Bank = newBank; Currency = newCurrency; Name = "New Account"; Description = Some "New Account" }
        let newAccount = createSampleAccount(newBankAccount)
        
        Collections.Banks.Edit(fun list -> list.Add(newBank))
        Collections.Currencies.Edit(fun list -> list.Add(newCurrency))
        Collections.Accounts.Edit(fun list -> list.Add(newAccount))
        
        // Allow time for reactive updates
        System.Threading.Thread.Sleep(10)
        
        // The reactive cache should include the new bank account
        let retrievedBankAccount = ReactiveBankAccountManager.getBankAccountByIdFast(4)
        Assert.That(retrievedBankAccount.Name, Is.EqualTo("New Account"))
        Assert.That(retrievedBankAccount.Bank.Name, Is.EqualTo("New Bank"))

    [<Test>]
    member this.``Extension methods should work correctly``() =
        // Test ToFastBankAccountById extension method
        let bankAccountId = 1
        let bankAccount = bankAccountId.ToFastBankAccountById()
        Assert.That(bankAccount.Id, Is.EqualTo(1))
        Assert.That(bankAccount.Name, Is.EqualTo("Checking"))
        
        // Test ToReactiveBankAccount extension method
        let bankAccountId2 = 2
        let observable = bankAccountId2.ToReactiveBankAccount()
        let mutable resultBankAccount = { Id = 0; Bank = createSampleBank(0, ""); Currency = createSampleCurrency(0, ""); Name = ""; Description = None }
        
        observable.Subscribe(fun ba -> resultBankAccount <- ba) |> ignore
        System.Threading.Thread.Sleep(10)
        
        Assert.That(resultBankAccount.Id, Is.EqualTo(2))
        Assert.That(resultBankAccount.Name, Is.EqualTo("Savings"))

    [<Test>]
    member this.``Performance comparison should show improvement over linear search``() =
        // Create larger dataset for performance testing
        let largeBanksList = [
            for i in 1..1000 do
                yield createSampleBank(i, $"Bank {i}")
        ]
        
        let largeCurrencyList = [
            for i in 1..10 do
                yield createSampleCurrency(i, $"CUR{i}")
        ]
        
        let largeBankAccountsList = [
            for i in 1..1000 do
                let bankId = ((i-1) % 1000) + 1
                let currencyId = ((i-1) % 10) + 1
                yield { 
                    Id = i
                    Bank = largeBanksList.[bankId - 1]
                    Currency = largeCurrencyList.[currencyId - 1]
                    Name = $"Account {i}"
                    Description = Some $"Account {i}"
                }
        ]
        
        Collections.Banks.Edit(fun list ->
            list.Clear()
            list.AddRange(largeBanksList))
        
        Collections.Currencies.Edit(fun list ->
            list.Clear()
            list.AddRange(largeCurrencyList))
        
        Collections.Accounts.Edit(fun list ->
            list.Clear()
            let accounts = largeBankAccountsList |> List.map createSampleAccount
            list.AddRange(accounts))
        
        // Re-initialize to pick up the larger dataset
        ReactiveBankAccountManager.dispose()
        ReactiveBankAccountManager.initialize()
        
        let lookupCount = 1000
        
        // Time the linear search approach
        let linearSearchStopwatch = Stopwatch.StartNew()
        for i in 1..lookupCount do
            let id = ((i - 1) % 1000) + 1
            let _ = 
                Collections.Accounts.Items 
                |> Seq.find(fun account -> account.Bank.IsSome && account.Bank.Value.Id = id)
                |> fun account -> account.Bank.Value
            ()
        linearSearchStopwatch.Stop()
        let linearTime = linearSearchStopwatch.ElapsedMilliseconds
        
        // Time the fast lookup approach
        let fastLookupStopwatch = Stopwatch.StartNew()
        for i in 1..lookupCount do
            let id = ((i - 1) % 1000) + 1
            let _ = ReactiveBankAccountManager.getBankAccountByIdFast(id)
            ()
        fastLookupStopwatch.Stop()
        let fastTime = fastLookupStopwatch.ElapsedMilliseconds
        
        printfn "=== Reactive BankAccount Performance Results ==="
        printfn "Dataset: 1,000 bank accounts, %d lookups" lookupCount
        printfn "Linear search time: %dms" linearTime
        printfn "Fast O(1) lookup time: %dms" fastTime
        printfn "Performance improvement: %dx" (if fastTime > 0L then linearTime / fastTime else 999L)
        printfn "========================================"
        
        // Fast lookup should be at least as fast as linear search (allowing some tolerance for variance)
        Assert.That(fastTime, Is.LessThanOrEqualTo(linearTime + 10L))
        
        // Restore original test data to avoid affecting other tests
        Collections.Banks.Edit(fun list ->
            list.Clear()
            list.AddRange([
                createSampleBank(1, "Bank of America")
                createSampleBank(2, "Wells Fargo")
                createSampleBank(3, "Chase Bank")
            ]))
        
        Collections.Currencies.Edit(fun list ->
            list.Clear()
            list.AddRange([
                createSampleCurrency(1, "USD")
                createSampleCurrency(2, "EUR")
                createSampleCurrency(3, "GBP")
            ]))
        
        let originalBankAccounts = [
            { Id = 1; Bank = createSampleBank(1, "Bank of America"); Currency = createSampleCurrency(1, "USD"); Name = "Checking"; Description = Some "Checking Account" }
            { Id = 2; Bank = createSampleBank(2, "Wells Fargo"); Currency = createSampleCurrency(1, "USD"); Name = "Savings"; Description = Some "Savings Account" }
            { Id = 3; Bank = createSampleBank(1, "Bank of America"); Currency = createSampleCurrency(2, "EUR"); Name = "EUR Account"; Description = Some "EUR Account" }
        ]
        
        Collections.Accounts.Edit(fun list ->
            list.Clear()
            let accounts = originalBankAccounts |> List.map createSampleAccount
            list.AddRange(accounts))
        
        // Re-initialize with original data
        ReactiveBankAccountManager.dispose()
        ReactiveBankAccountManager.initialize()

    [<Test>]
    member this.``Backward compatibility - Collections approach should still work``() =
        // Test that bank account lookup still works via existing direct access patterns
        let account = Collections.Accounts.Items |> Seq.find(fun a -> a.Bank.IsSome && a.Bank.Value.Id = 1)
        let bankAccount = account.Bank.Value
        
        Assert.That(bankAccount.Id, Is.EqualTo(1))
        Assert.That(bankAccount.Name, Is.EqualTo("Checking"))
        
        // Compare with reactive manager result
        let reactiveBankAccount = ReactiveBankAccountManager.getBankAccountByIdFast(1)
        Assert.That(reactiveBankAccount.Id, Is.EqualTo(bankAccount.Id))
        Assert.That(reactiveBankAccount.Name, Is.EqualTo(bankAccount.Name))