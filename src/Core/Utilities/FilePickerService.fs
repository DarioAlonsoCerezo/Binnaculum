namespace Binnaculum.Core.Utilities

open Microsoft.Maui.Storage
open Microsoft.Maui.ApplicationModel
open System.IO
open System.Diagnostics
open Binnaculum.Core.Logging

type FilePickerResult =
    { FileName: string
      FilePath: string
      ContentType: string
      Success: bool }

module FilePickerService =
    /// <summary>
    /// Opens a file picker to select an image file.
    /// </summary>
    /// <returns>A Task containing file information or null on cancel/failure</returns>
    let pickImageAsync (pickerTitle) =
        task {
            Debug.WriteLine("[FilePicker] Requesting image file access")
            // Check if we have permission
            let! status = Permissions.RequestAsync<Permissions.StorageRead>()

            if status <> PermissionStatus.Granted then
                Debug.WriteLine("[FilePicker] Storage permission denied for image picker")

                return
                    { FileName = ""
                      FilePath = ""
                      ContentType = ""
                      Success = false }
            else
                Debug.WriteLine("[FilePicker] Storage permission granted for image picker")
                // Define options for the file picker
                let options = PickOptions()
                options.PickerTitle <- pickerTitle
                options.FileTypes <- FilePickerFileType.Images

                // Pick a file
                let! result = FilePicker.Default.PickAsync(options)

                if result <> null then
                    // CoreLogger.logDebugf "FilePicker" "Image selected: %A" result.FullPath
                    // Define the target path in the AppDataDirectory
                    let targetDirectory = FileSystem.AppDataDirectory
                    let targetPath = Path.Combine(targetDirectory, result.FileName)

                    // Copy the file to the target directory, overwriting if it already exists
                    File.Copy(result.FullPath, targetPath, true)

                    // Return the updated file path
                    // CoreLogger.logDebugf "FilePicker" "Image copied to sandbox: %A" targetPath

                    return
                        { FileName = result.FileName
                          FilePath = targetPath
                          ContentType = result.ContentType
                          Success = true }
                else
                    // User cancelled
                    Debug.WriteLine("[FilePicker] Image pick cancelled by user")

                    return
                        { FileName = ""
                          FilePath = ""
                          ContentType = ""
                          Success = false }
        }

    /// <summary>
    /// Opens a file picker to select any type of file.
    /// </summary>
    /// <returns>A Task containing file information or default values on cancel/failure</returns>
    let pickDataFileAsync (pickerTitle) =
        task {
            Debug.WriteLine("[FilePicker] Requesting data file access")
            // Check if we have permission
            let! status = Permissions.RequestAsync<Permissions.StorageRead>()

            if status <> PermissionStatus.Granted then
                Debug.WriteLine("[FilePicker] Storage permission denied for data picker")

                return
                    { FileName = ""
                      FilePath = ""
                      ContentType = ""
                      Success = false }
            else
                Debug.WriteLine("[FilePicker] Storage permission granted for data picker")
                // Define options for the file picker
                let options = PickOptions()
                options.PickerTitle <- pickerTitle
                // Allow any file type by not specifying FileTypes

                // Pick a file
                let! result = FilePicker.Default.PickAsync(options)

                if result <> null then
                    // CoreLogger.logDebugf "FilePicker" "Data file selected: %A" result.FullPath

                    return
                        { FileName = result.FileName
                          FilePath = result.FullPath
                          ContentType = result.ContentType
                          Success = true }
                else
                    // User cancelled
                    Debug.WriteLine("[FilePicker] Data file pick cancelled by user")

                    return
                        { FileName = ""
                          FilePath = ""
                          ContentType = ""
                          Success = false }
        }
