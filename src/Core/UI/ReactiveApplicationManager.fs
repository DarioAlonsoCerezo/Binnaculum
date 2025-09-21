namespace Binnaculum.Core.UI

open Binnaculum.Core.Storage
open Binnaculum.Core.DataLoader

/// <summary>
/// Reactive application manager that coordinates all reactive managers and provides unified initialization.
/// This module acts as the main entry point for transitioning from manual loading to reactive patterns.
/// </summary>
module ReactiveApplicationManager =
    
    /// <summary>
    /// Initialize all reactive managers in the correct order
    /// This replaces the need for individual manager initialization calls
    /// </summary>
    let initializeReactiveManagers() =
        // Initialize base collection reactive managers first
        ReactiveCurrencyManager.initialize()
        ReactiveTickerManager.initialize()
        ReactiveBrokerManager.initialize()
        ReactiveBankManager.initialize()
        ReactiveBrokerAccountManager.initialize()
        ReactiveBankAccountManager.initialize()
        
        // Initialize movement manager (depends on base collections)
        ReactiveMovementManager.initialize()
        
        // Initialize snapshot manager (depends on all above)
        ReactiveSnapshotManager.initialize()
    
    /// <summary>
    /// Initialize the reactive application with automatic snapshot updates
    /// This replaces the traditional DataLoader.initialization() approach
    /// </summary>
    let initializeReactiveApplication() = task {
        // Initialize all reactive managers
        initializeReactiveManagers()
        
        // Load initial data using traditional methods (this will trigger reactive updates)
        do! DataLoader.loadBasicData() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.getOrRefreshAllAccounts() |> Async.AwaitTask |> Async.Ignore
        
        // Trigger initial snapshot loading (will become automatic after this)
        ReactiveSnapshotManager.refresh()
        
        // Load ticker snapshots (not yet converted to reactive)
        do! TickerSnapshotLoader.load() |> Async.AwaitTask |> Async.Ignore
    }
    
    /// <summary>
    /// Initialize using traditional approach for backward compatibility
    /// This method maintains the existing behavior while reactive is being adopted
    /// </summary>
    let initializeTraditional() = task {
        do! DataLoader.loadBasicData() |> Async.AwaitTask |> Async.Ignore
        do! DataLoader.initialization() |> Async.AwaitTask |> Async.Ignore
    }
    
    /// <summary>
    /// Dispose all reactive managers properly
    /// </summary>
    let disposeReactiveManagers() =
        ReactiveSnapshotManager.dispose()
        ReactiveMovementManager.dispose()
        // Note: Other reactive managers don't currently have dispose methods