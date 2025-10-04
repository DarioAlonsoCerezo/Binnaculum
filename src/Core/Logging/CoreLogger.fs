namespace Binnaculum.Core.Logging

open System
open System.Diagnostics

/// <summary>
/// Simple logging levels for Core module
/// </summary>
type LogLevel =
    | Debug = 0
    | Info = 1
    | Warning = 2
    | Error = 3

/// <summary>
/// Core logging module for F# components
/// </summary>
module CoreLogger =

    /// Current minimum log level (can be configured)
    let mutable private minLogLevel = LogLevel.Debug

    /// Set the minimum log level for filtering
    let setMinLevel level = minLogLevel <- level

    /// Internal logging function
    let private log level tag message =
        if level >= minLogLevel then
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
                // IDE debugging - use Debug output for better integration
                Debug.WriteLine(formattedMessage)
            else
                // Console/Terminal execution - use Console output
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
