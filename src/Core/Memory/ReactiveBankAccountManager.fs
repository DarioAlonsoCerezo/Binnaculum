namespace Binnaculum.Core.UI

open System
open System.Collections.Concurrent
open System.Reactive.Linq
open System.Runtime.CompilerServices
open DynamicData
open Binnaculum.Core.Models

/// <summary>
/// Reactive bank account manager that provides O(1) bank account lookups and automatic updates
/// when the underlying Accounts, Banks, or Currencies collections change.
/// </summary>
module ReactiveBankAccountManager =
    
    /// <summary>
    /// Internal reactive bank account cache by ID that maintains O(1) lookups
    /// </summary>
    let private bankAccountCacheById = ConcurrentDictionary<int, BankAccount>()
    
    /// <summary>
    /// Subscription for managing reactive updates from Accounts collection
    /// </summary>
    let mutable private accountsSubscription: System.IDisposable option = None
    
    /// <summary>
    /// Subscription for managing reactive updates from Banks collection
    /// </summary>
    let mutable private banksSubscription: System.IDisposable option = None
    
    /// <summary>
    /// Subscription for managing reactive updates from Currencies collection
    /// </summary>
    let mutable private currenciesSubscription: System.IDisposable option = None
    
    /// <summary>
    /// Initialize the reactive bank account cache by subscribing to Collections changes
    /// </summary>
    let private initializeCache() =
        // Subscribe to account collection changes for bank accounts
        let accountsSub = 
            Collections.Accounts.Connect()
                .Filter(fun account -> account.Type = AccountType.BankAccount && account.Bank.IsSome)
                .Subscribe(fun changeSet ->
                    for change in changeSet do
                        match change.Reason with
                        | ListChangeReason.Add -> 
                            let bankAccount = change.Item.Current.Bank.Value
                            bankAccountCacheById.TryAdd(bankAccount.Id, bankAccount) |> ignore
                        | ListChangeReason.Replace ->
                            let bankAccount = change.Item.Current.Bank.Value
                            bankAccountCacheById.AddOrUpdate(bankAccount.Id, bankAccount, fun _ _ -> bankAccount) |> ignore
                        | ListChangeReason.Remove ->
                            let bankAccount = change.Item.Current.Bank.Value
                            bankAccountCacheById.TryRemove(bankAccount.Id) |> ignore
                        | ListChangeReason.Clear ->
                            bankAccountCacheById.Clear()
                        | _ -> ())
        
        // Subscribe to bank collection changes to update bank account references
        let banksSub = 
            Collections.Banks.Connect()
                .Subscribe(fun changeSet ->
                    for change in changeSet do
                        match change.Reason with
                        | ListChangeReason.Replace ->
                            let updatedBank = change.Item.Current
                            // Update any bank accounts that reference this bank
                            bankAccountCacheById.Values
                            |> Seq.filter (fun ba -> ba.Bank.Id = updatedBank.Id)
                            |> Seq.iter (fun ba ->
                                let updatedBankAccount = { ba with Bank = updatedBank }
                                bankAccountCacheById.[ba.Id] <- updatedBankAccount)
                        | _ -> ())
        
        // Subscribe to currency collection changes to update bank account references
        let currenciesSub = 
            Collections.Currencies.Connect()
                .Subscribe(fun changeSet ->
                    for change in changeSet do
                        match change.Reason with
                        | ListChangeReason.Replace ->
                            let updatedCurrency = change.Item.Current
                            // Update any bank accounts that reference this currency
                            bankAccountCacheById.Values
                            |> Seq.filter (fun ba -> ba.Currency.Id = updatedCurrency.Id)
                            |> Seq.iter (fun ba ->
                                let updatedBankAccount = { ba with Currency = updatedCurrency }
                                bankAccountCacheById.[ba.Id] <- updatedBankAccount)
                        | _ -> ())
        
        accountsSubscription <- Some accountsSub
        banksSubscription <- Some banksSub
        currenciesSubscription <- Some currenciesSub
        
        // Initialize cache with current bank account items
        Collections.Accounts.Items
        |> Seq.filter (fun account -> account.Type = AccountType.BankAccount && account.Bank.IsSome)
        |> Seq.iter (fun account -> 
            let bankAccount = account.Bank.Value
            bankAccountCacheById.TryAdd(bankAccount.Id, bankAccount) |> ignore)
    
    /// <summary>
    /// Initialize the reactive bank account manager (should be called once at application startup)
    /// </summary>
    let initialize() = 
        if accountsSubscription.IsNone then
            initializeCache()
    
    /// <summary>
    /// Get a bank account by ID with O(1) lookup performance.
    /// Falls back to linear search if cache is not populated.
    /// </summary>
    /// <param name="id">Bank Account ID</param>
    /// <returns>Bank Account matching the ID</returns>
    let getBankAccountByIdFast(id: int) : BankAccount =
        match bankAccountCacheById.TryGetValue(id) with
        | true, bankAccount -> bankAccount
        | false, _ ->
            // Fallback to linear search and cache the result
            match Collections.Accounts.Items |> Seq.tryFind(fun account -> account.Bank.IsSome && account.Bank.Value.Id = id) with
            | Some account ->
                let bankAccount = account.Bank.Value
                bankAccountCacheById.TryAdd(id, bankAccount) |> ignore
                bankAccount
            | None ->
                raise (System.Collections.Generic.KeyNotFoundException($"Bank account with ID {id} not found in Collections.Accounts"))
    
    /// <summary>
    /// Get a reactive observable that emits the bank account when it becomes available by ID
    /// </summary>
    /// <param name="id">Bank Account ID</param>
    /// <returns>Observable that emits the bank account</returns>
    let getBankAccountByIdReactive(id: int) : IObservable<BankAccount> =
        Observable.Return(getBankAccountByIdFast(id))
    
    /// <summary>
    /// Dispose all subscriptions (should be called at application shutdown)
    /// </summary>
    let dispose() =
        accountsSubscription |> Option.iter (fun sub -> sub.Dispose())
        banksSubscription |> Option.iter (fun sub -> sub.Dispose())
        currenciesSubscription |> Option.iter (fun sub -> sub.Dispose())
        accountsSubscription <- None
        banksSubscription <- None
        currenciesSubscription <- None

/// <summary>
/// Extension methods for reactive bank account operations
/// </summary>
[<Extension>]
type ReactiveBankAccountExtensions() =
    
    /// <summary>
    /// Extension method to convert bank account ID to reactive bank account observable
    /// </summary>
    /// <param name="id">Bank Account ID</param>
    /// <returns>Observable that emits the bank account</returns>
    [<Extension>]
    static member ToReactiveBankAccount(id: int) : IObservable<BankAccount> =
        ReactiveBankAccountManager.getBankAccountByIdReactive(id)
    
    /// <summary>
    /// Extension method to get bank account by ID using fast O(1) lookup
    /// </summary>
    /// <param name="id">Bank Account ID</param>
    /// <returns>Bank Account matching the ID</returns>
    [<Extension>]
    static member ToFastBankAccountById(id: int) : BankAccount =
        ReactiveBankAccountManager.getBankAccountByIdFast(id)