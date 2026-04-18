# Project Context

## Product

Binnaculum is a cross-platform investment tracking app built with .NET MAUI and F# core logic.

## Architecture

- `src/Core/`: F# business logic, database access, financial calculations.
- `src/UI/`: C# and XAML MAUI application.
- `src/Tests/Core.Tests/`: F# tests for core logic.
- `src/Tests/Core.Platform.Tests/`: platform-dependent tests.

## Platform Targets

- Android: `net10.0-android`
- Windows: `net10.0-windows10.0.19041.0` (Windows only)
- iOS: `net10.0-ios` (macOS only)
- MacCatalyst: `net10.0-maccatalyst` (macOS only)

## Domain Constraints

- Monetary values must use `decimal`.
- Core financial rules belong in F# (`src/Core/`), not in UI or ViewModels.
- Performance must account for mobile CPU and memory constraints.
