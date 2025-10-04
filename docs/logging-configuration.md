# Logging Configuration Example

To configure the logging level in your application, use the `CoreLogger.setMinLevel` function:

## F# Example
```fsharp
// In your app initialization
open Binnaculum.Core.Logging

// Set to Info level (hide Debug messages)
CoreLogger.setMinLevel LogLevel.Info

// Or set to Debug level (show all messages)
CoreLogger.setMinLevel LogLevel.Debug

// For production, set to Warning or Error
CoreLogger.setMinLevel LogLevel.Warning
```

## C# Example
```csharp
// In your MAUI app startup (MauiProgram.cs or similar)
using Binnaculum.Core.Logging;

// Configure logging level
CoreLogger.setMinLevel(LogLevel.Info);
```

## Log Levels
- **Debug**: Detailed information for debugging (default)
- **Info**: General information about application flow
- **Warning**: Potentially harmful situations
- **Error**: Error events that might still allow the application to continue

## Output Format
All logs are output in the format:
```
[HH:mm:ss.fff] LEVEL: [Tag] Message
```

Example:
```
[14:32:45.123] INFO : [ImportManager] Starting import for broker 1, account 2, file=test.csv
[14:32:45.456] DEBUG: [ImportManager] Validating input parameters
[14:32:45.789] ERROR: [ImportManager] File not found: missing.csv
```