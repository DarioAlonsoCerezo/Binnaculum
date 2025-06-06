﻿namespace Binnaculum.Core.Utilities

open Microsoft.Maui.Storage
open Microsoft.Maui.ApplicationModel
open Microsoft.Maui.Devices
open System.IO

type FilePickerResult = {
    FileName: string
    FilePath: string
    ContentType: string
    Success: bool
}

module FilePickerService =
    /// <summary>
    /// Opens a file picker to select an image file.
    /// </summary>
    /// <returns>A Task containing file information or null on cancel/failure</returns>
    let pickImageAsync(pickerTitle) = task {
        try
            // Check if we have permission
            let! status = Permissions.RequestAsync<Permissions.StorageRead>()
            
            if status <> PermissionStatus.Granted then
                return { FileName = ""; FilePath = ""; ContentType = ""; Success = false }
            else
                // Define options for the file picker
                let options = PickOptions()
                options.PickerTitle <- pickerTitle
                options.FileTypes <- FilePickerFileType.Images
                
                // Pick a file
                let! result = FilePicker.Default.PickAsync(options)
                
                if result <> null then
                    // Define the target path in the AppDataDirectory
                    let targetDirectory = FileSystem.AppDataDirectory
                    let targetPath = Path.Combine(targetDirectory, result.FileName)

                    // Copy the file to the target directory, overwriting if it already exists
                    File.Copy(result.FullPath, targetPath, true)

                    // Return the updated file path
                    return { 
                        FileName = result.FileName
                        FilePath = targetPath
                        ContentType = result.ContentType 
                        Success = true
                    }
                else
                    // User cancelled
                    return { FileName = ""; FilePath = ""; ContentType = ""; Success = false }
        with
        | ex ->
            // Handle exceptions
            System.Diagnostics.Debug.WriteLine($"Error picking file: {ex.Message}")
            return { FileName = ""; FilePath = ""; ContentType = ""; Success = false }

    }

    /// <summary>
    /// Opens a file picker to select CSV or ZIP files.
    /// </summary>
    /// <returns>A Task containing file information or default values on cancel/failure</returns>
    let pickDataFileAsync(pickerTitle) = task {
        try
            // Check if we have permission
            let! status = Permissions.RequestAsync<Permissions.StorageRead>()
        
            if status <> PermissionStatus.Granted then
                return { FileName = ""; FilePath = ""; ContentType = ""; Success = false }
            else
                // Define custom file types for CSV and ZIP
                let customFileType = new FilePickerFileType(
                    dict [
                        // Windows file extensions
                        DevicePlatform.WinUI, ["csv"; "zip"]
                        // macOS UTIs (Uniform Type Identifiers)
                        DevicePlatform.macOS, ["public.comma-separated-values-text"; "public.zip-archive"] 
                        // iOS UTIs
                        DevicePlatform.iOS, ["public.comma-separated-values-text"; "public.zip-archive"]
                        // Android MIME types
                        DevicePlatform.Android, ["text/csv"; "application/zip"; "application/x-zip-compressed"]
                    ]
                )
            
                // Define options for the file picker
                let options = PickOptions()
                options.PickerTitle <- pickerTitle
                options.FileTypes <- customFileType
            
                // Pick a file
                let! result = FilePicker.Default.PickAsync(options)
            
                if result <> null then
                    return { 
                        FileName = result.FileName
                        FilePath = result.FullPath
                        ContentType = result.ContentType 
                        Success = true
                    }
                else
                    // User cancelled
                    return { FileName = ""; FilePath = ""; ContentType = ""; Success = false }
        with
        | ex ->
            // Handle exceptions
            System.Diagnostics.Debug.WriteLine($"Error picking file: {ex.Message}")
            return { FileName = ""; FilePath = ""; ContentType = ""; Success = false }
    }