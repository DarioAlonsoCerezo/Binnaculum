namespace Binnaculum.Core.Logging

open System
open System.Diagnostics
open Microsoft.Extensions.Logging

/// <summary>
/// Simple logging levels for Core module
/// </summary>
type LogLevel =
    | Debug = 0
    | Info = 1
    | Warning = 2
    | Error = 3

/// <summary>
/// Core logging module for F# components with Microsoft.Extensions.Logging integration
/// </summary>
module CoreLogger =

    /// Current minimum log level (can be configured)
    let mutable private minLogLevel = LogLevel.Debug

    /// Master switch to enable/disable all logging
    let mutable private loggingEnabled = true // Set to false to disable all logging

    /// Optional external logger for advanced features
    let mutable private externalLogger: ILogger option = None

    /// Set the minimum log level for filtering
    let setMinLevel level = minLogLevel <- level

    /// Enable or disable all logging (master switch)
    let setEnabled enabled = loggingEnabled <- enabled

    /// Set an external Microsoft.Extensions.Logging ILogger for advanced features
    /// If not set, falls back to simple Debug/Console output
    let setLogger logger = externalLogger <- Some logger

    /// Internal logging function with Microsoft.Extensions.Logging integration
    let private log level tag message =
        if loggingEnabled && level >= minLogLevel then
            match externalLogger with
            | Some logger ->
                // Use Microsoft.Extensions.Logging for better performance and features
                let msLogLevel =
                    match level with
                    | LogLevel.Debug -> Microsoft.Extensions.Logging.LogLevel.Debug
                    | LogLevel.Info -> Microsoft.Extensions.Logging.LogLevel.Information
                    | LogLevel.Warning -> Microsoft.Extensions.Logging.LogLevel.Warning
                    | LogLevel.Error -> Microsoft.Extensions.Logging.LogLevel.Error
                    | _ -> Microsoft.Extensions.Logging.LogLevel.Information

                // Use structured logging with zero allocations when disabled
                logger.Log(msLogLevel, "[{Tag}] {Message}", tag, message)
            | None ->
                // Fallback to simple implementation when no external logger is set
                let timestamp = DateTime.Now.ToString("HH:mm:ss.fff")

                let levelStr =
                    match level with
                    | LogLevel.Debug -> "DEBUG"
                    | LogLevel.Info -> "INFO "
                    | LogLevel.Warning -> "WARN "
                    | LogLevel.Error -> "ERROR"
                    | _ -> "UNKN "

                let formattedMessage = $"[{timestamp}] {levelStr}: [{tag}] {message}"

                // Smart output selection based on execution environment
                if System.Diagnostics.Debugger.IsAttached then
                    Debug.WriteLine(formattedMessage)
                else
                    Console.WriteLine(formattedMessage)

    /// Log debug message
    let logDebug tag message = log LogLevel.Debug tag message

    /// Log info message
    let logInfo tag message = log LogLevel.Info tag message

    /// Log warning message
    let logWarning tag message = log LogLevel.Warning tag message

    /// Log error message
    let logError tag message = log LogLevel.Error tag message

    /// Convenience function for formatting messages with parameters
    let logDebugf tag format = Printf.ksprintf (logDebug tag) format
    let logInfof tag format = Printf.ksprintf (logInfo tag) format
    let logWarningf tag format = Printf.ksprintf (logWarning tag) format
    let logErrorf tag format = Printf.ksprintf (logError tag) format

    /// High-performance logging functions that use Microsoft.Extensions.Logging directly when available
    /// These provide zero-allocation logging when the log level is disabled
    let logDebugOptimized tag (messageFunc: unit -> string) =
        if loggingEnabled then
            match externalLogger with
            | Some logger when logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug) ->
                logger.LogDebug("[{Tag}] {Message}", tag, messageFunc ())
            | None when LogLevel.Debug >= minLogLevel -> logDebug tag (messageFunc ())
            | _ -> () // No-op when logging is disabled

    let logInfoOptimized tag (messageFunc: unit -> string) =
        if loggingEnabled then
            match externalLogger with
            | Some logger when logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information) ->
                logger.LogInformation("[{Tag}] {Message}", tag, messageFunc ())
            | None when LogLevel.Info >= minLogLevel -> logInfo tag (messageFunc ())
            | _ -> () // No-op when logging is disabled
