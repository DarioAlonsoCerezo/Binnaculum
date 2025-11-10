namespace Binnaculum.Core.Import

open System.Reactive.Subjects
open Binnaculum.Core.Logging

/// <summary>
/// Progress information for multi-file imports
/// Consumed by MAUI ViewModels for UI updates
/// </summary>
type MultiFileImportProgress = {
    TotalFiles: int
    CurrentFileIndex: int          // 1-based index (e.g., "file 3 of 245")
    CurrentFileName: string
    FilesRemaining: int            // TotalFiles - CurrentFileIndex
    CurrentFileProgress: float     // 0.0 to 1.0 for current file
    OverallProgress: float         // 0.0 to 1.0 for entire import
}

/// <summary>
/// Module for tracking and broadcasting multi-file import progress
/// </summary>
module ImportProgressTracker =
    
    /// BehaviorSubject for UI subscriptions
    /// None = no import in progress
    /// Some(progress) = import active with current state
    let private progressSubject = new BehaviorSubject<MultiFileImportProgress option>(None)
    
    /// Observable for UI to subscribe to
    /// Usage in C# ViewModel: ImportProgressTracker.progressObservable.Subscribe(...)
    let progressObservable = progressSubject :> System.IObservable<MultiFileImportProgress option>
    
    /// <summary>
    /// Initialize progress tracking for a multi-file import
    /// Call this before starting the import loop
    /// </summary>
    /// <param name="totalFiles">Total number of files to import</param>
    let startTracking (totalFiles: int) =
        let initialProgress = {
            TotalFiles = totalFiles
            CurrentFileIndex = 0
            CurrentFileName = ""
            FilesRemaining = totalFiles
            CurrentFileProgress = 0.0
            OverallProgress = 0.0
        }
        progressSubject.OnNext(Some initialProgress)
        CoreLogger.logInfof "ImportProgress" "Started tracking %d files" totalFiles
    
    /// <summary>
    /// Update progress for current file being processed
    /// Call this as file processing progresses
    /// </summary>
    /// <param name="fileIndex">Current file index (1-based)</param>
    /// <param name="fileName">Name of current file</param>
    /// <param name="fileProgress">Progress for current file (0.0 to 1.0)</param>
    let updateFileProgress (fileIndex: int) (fileName: string) (fileProgress: float) =
        match progressSubject.Value with
        | Some current ->
            let updated = {
                current with
                    CurrentFileIndex = fileIndex
                    CurrentFileName = fileName
                    FilesRemaining = current.TotalFiles - fileIndex
                    CurrentFileProgress = fileProgress
                    // Overall progress: (completed files + current file progress) / total
                    OverallProgress = (float (fileIndex - 1) + fileProgress) / float current.TotalFiles
            }
            progressSubject.OnNext(Some updated)
        | None -> 
            CoreLogger.logWarning "ImportProgress" "updateFileProgress called but tracking not started"
    
    /// <summary>
    /// Mark a file as completed (convenience wrapper)
    /// </summary>
    /// <param name="fileIndex">File index that just completed</param>
    let completeFile (fileIndex: int) =
        match progressSubject.Value with
        | Some current ->
            updateFileProgress fileIndex current.CurrentFileName 1.0
        | None -> ()
    
    /// <summary>
    /// Complete tracking and reset state
    /// Call this after import finishes (success or failure)
    /// </summary>
    let completeTracking () =
        progressSubject.OnNext(None)
        CoreLogger.logInfo "ImportProgress" "Tracking completed"
    
    /// <summary>
    /// Get current progress snapshot (for logging/debugging)
    /// </summary>
    let getCurrentProgress () : MultiFileImportProgress option =
        progressSubject.Value
