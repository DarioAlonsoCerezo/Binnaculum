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

## Manual Manageability Audit

Use these local commands for advisory reporting while CI checks are intentionally disabled.

```powershell
# Top files by total line count (tracked files only)
$files = git ls-files
$results = foreach ($rel in $files) {
  $full = Join-Path (Get-Location) $rel
  try {
    $reader = [System.IO.File]::OpenText($full)
    $count = 0
    while ($null -ne $reader.ReadLine()) { $count++ }
    $reader.Close()
    [PSCustomObject]@{ Lines = $count; File = $rel }
  } catch {
    [PSCustomObject]@{ Lines = -1; File = $rel }
  }
}
$results | Sort-Object -Property @{Expression='Lines';Descending=$true}, @{Expression='File';Descending=$false} | Select-Object -First 50
```

```powershell
# Top actionable code files excluding generated/resources/common binary and data extensions
$files = git ls-files
$excludePattern = '(\\.Designer\\.cs$|\\.g\\.cs$|\\.resx$|\\.ttf$|\\.png$|\\.jpg$|\\.jpeg$|\\.svg$|\\.ico$|\\.gif$|\\.pdf$|\\.csv$)'
$results = foreach ($rel in $files) {
  if ($rel -match $excludePattern) { continue }
  $full = Join-Path (Get-Location) $rel
  try {
    $reader = [System.IO.File]::OpenText($full)
    $count = 0
    while ($null -ne $reader.ReadLine()) { $count++ }
    $reader.Close()
    [PSCustomObject]@{ Lines = $count; File = $rel }
  } catch {}
}
$results | Sort-Object -Property @{Expression='Lines';Descending=$true}, @{Expression='File';Descending=$false} | Select-Object -First 50
```

## Notes

- Run only platform-compatible targets for the current OS.
- Never skip tests for core financial logic changes.
- Use manageability audit output to prioritize incremental refactor work, not to block merges.
