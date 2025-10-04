namespace Binnaculum.Core.Import

open System.IO
open System.IO.Compression
open Binnaculum.Core.Logging

/// <summary>
/// Processed file information for import operations
/// </summary>
type ProcessedFile = {
    FilePath: string
    IsTemporary: bool
    CsvFiles: string list
}

/// <summary>
/// File processing module for CSV and ZIP file detection, extraction, and cleanup
/// </summary>
module FileProcessor =
    
    /// <summary>
    /// Process a file for import - handles both CSV and ZIP files
    /// </summary>
    /// <param name="filePath">Path to the file to process</param>
    /// <returns>ProcessedFile with CSV file paths</returns>
    let processFile (filePath: string) = 
        if not (File.Exists(filePath)) then
            failwith $"File not found: {filePath}"
        
        let extension = Path.GetExtension(filePath).ToLowerInvariant()
        
        match extension with
        | ".csv" -> 
            { FilePath = filePath; IsTemporary = false; CsvFiles = [filePath] }
        | ".zip" ->
            let tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())
            Directory.CreateDirectory(tempDir) |> ignore
            
            try
                ZipFile.ExtractToDirectory(filePath, tempDir)
            with
            | ex -> 
                // Clean up temp directory on extraction failure
                if Directory.Exists(tempDir) then
                    Directory.Delete(tempDir, true)
                failwith $"Failed to extract ZIP file {filePath}: {ex.Message}"
            
            let csvFiles = 
                Directory.GetFiles(tempDir, "*.csv", SearchOption.AllDirectories)
                |> Array.toList
            
            if csvFiles.IsEmpty then
                // Clean up temp directory if no CSV files found
                Directory.Delete(tempDir, true)
                failwith "No CSV files found in ZIP archive"
                
            { FilePath = tempDir; IsTemporary = true; CsvFiles = csvFiles }
        | _ -> 
            failwith $"Unsupported file format: {extension}. Only CSV and ZIP files are supported."
    
    /// <summary>
    /// Clean up temporary files created during processing
    /// </summary>
    /// <param name="processedFile">The processed file to clean up</param>
    let cleanup (processedFile: ProcessedFile) =
        if processedFile.IsTemporary && Directory.Exists(processedFile.FilePath) then
            try
                Directory.Delete(processedFile.FilePath, true)
            with
            | ex -> 
                CoreLogger.logWarningf "FileProcessor" "Failed to cleanup temporary directory %s: %s" processedFile.FilePath ex.Message
    
    /// <summary>
    /// Validate that all CSV files in a processed file are readable
    /// </summary>
    /// <param name="processedFile">The processed file to validate</param>
    /// <returns>true if all files are readable, false otherwise</returns>
    let validateCsvFiles (processedFile: ProcessedFile) =
        processedFile.CsvFiles
        |> List.forall (fun csvFile ->
            try
                File.Exists(csvFile) && File.ReadAllLines(csvFile).Length > 0
            with
            | _ -> false)
    
    /// <summary>
    /// Get the total size of all CSV files in a processed file
    /// </summary>
    /// <param name="processedFile">The processed file to analyze</param>
    /// <returns>Total size in bytes</returns>
    let getTotalFileSize (processedFile: ProcessedFile) =
        processedFile.CsvFiles
        |> List.sumBy (fun csvFile ->
            try
                let fileInfo = FileInfo(csvFile)
                fileInfo.Length
            with
            | _ -> 0L)