# Canonical Commands

## Restore

```bash
dotnet restore
```

## Core Build

```bash
dotnet build src/Core/Core.fsproj
dotnet build src/Tests/Core.Tests/Core.Tests.fsproj
```

## Core Tests

```bash
dotnet test src/Tests/Core.Tests/Core.Tests.fsproj
```

## UI Builds

```bash
dotnet build src/UI/Binnaculum.csproj -f net10.0-android
dotnet build src/UI/Binnaculum.csproj -f net10.0-windows10.0.19041.0
dotnet build src/UI/Binnaculum.csproj -f net10.0-ios
dotnet build src/UI/Binnaculum.csproj -f net10.0-maccatalyst
```

## Targeted Performance Tests

```bash
dotnet test --filter "BrokerFinancialSnapshotManager"
```

## Notes

- Run only platform-compatible targets for the current OS.
- Never skip tests for core financial logic changes.
