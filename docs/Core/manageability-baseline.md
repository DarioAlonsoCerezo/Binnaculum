# Manageability Baseline

Date: 2026-04-18

## Scope

- This baseline supports advisory refactor planning only.
- CI enforcement remains intentionally disabled.
- Excluded from threshold decisions: generated files, resource wrappers, binaries, fonts, images, and large fixture/snapshot data.

## Current Advisory Thresholds

| Scope | File max lines | Method/function max lines |
| --- | ---: | ---: |
| Core and UI code (non-generated) | 400 | 40 |
| UI page XAML (`src/UI/Pages/*.xaml`) | 700 | n/a |
| UI control/template XAML (`src/UI/Controls/**/*.xaml`) | 450 | n/a |
| UI style dictionaries (`src/UI/Resources/Styles/*.xaml`) | 900 | n/a |
| UI code-behind (`*.xaml.cs`) | 300 | 40 |
| Tests (`src/Tests/**/*`) | 600 | 60 |

## Top Actionable Hotspots

Measured over tracked files with common generated/resource/data extensions excluded.

1. `src/Tests/Core.Tests/Integration/Import/TsllData/TickerSnapshots.fs` (2265)
2. `src/Tests/Core.Tests/Integration/Import/TsllData/BrokerSnapshots.fs` (1681)
3. `src/Tests/Core.Tests/Integration/Import/MpwData/BrokerSnapshots.fs` (1568)
4. `src/Tests/Core.Tests/Integration/Import/MpwData/TickerSnapshots.fs` (1409)
5. `src/Core/Database/OptionTradeExtensions.fs` (1108)
6. `src/Core/Models/DatabaseToModels.fs` (1049)
7. `src/Tests/Core.Tests/Integration/Framework/TestActions.fs` (976)
8. `src/Core/Import/ImportManager.fs` (946)
9. `src/Core/Import/ImportModels.fs` (942)
10. `src/Core/Snapshots/BrokerAccountSnapshotManager.fs` (939)
11. `src/Core/Snapshots/TickerSnapshotCalculateInMemory.fs` (833)
12. `src/Tests/UI.Tests/AppInstalation.cs` (713)
13. `src/Tests/Core.Tests/Integration/Import/OptionsImportExpectedSnapshots.fs` (662)
14. `src/Tests/Core.Tests/Unit/Import/ImportSessionManagerTests.fs` (657)
15. `src/Tests/Core.Tests/Integration/Framework/TestVerifications.fs` (633)
16. `src/Core/Snapshots/SnapshotManagerUtils.fs` (624)
17. `src/Core/Snapshots/TickerSnapshotManager.fs` (593)
18. `src/UI/Resources/Styles/Styles.xaml` (564)
19. `src/Core/Snapshots/BrokerFinancialCalculateInMemory.fs` (558)
20. `src/Tests/Core.Tests/Unit/Import/TastytradeImportTests.fs` (551)

## Near-Threshold Files

- `src/Core/Database/TradeExtensions.fs` (540)
- `src/Core/Import/Brokers/Tastytrade/TastytradeConverter.fs` (505)
- `src/Core/Snapshots/BrokerFinancialSnapshotManager.fs` (500)
- `src/Core/Import/Brokers/IBKR/IBKRStatementParser.fs` (477)
- `src/Core/Import/CsvDateAnalyzer.fs` (466)
- `src/Core/Database/DatabaseModel.fs` (464)
- `src/Core/Models/Models.fs` (449)

## Prioritized Execution Queue

### Batch 1: Highest Impact, Lowest Risk

- Split large test snapshot data modules into domain-focused files and helper builders.
- Reduce `OptionTradeExtensions.fs` and `DatabaseToModels.fs` by extracting cohesive mapping modules.
- Keep behavioral parity via existing integration and unit tests.

### Batch 2: Core Orchestration and Snapshots

- Split `ImportManager.fs` by orchestration phase (validation, conversion, persistence).
- Split snapshot managers by responsibility (load, calculate, persist, reconcile).

### Batch 3: UI and Remaining Hotspots

- Restructure `Styles.xaml` into smaller dictionaries when section cohesion allows.
- Reduce large test framework files into setup, actions, and assertions modules.

## Success Criteria

- Each refactor PR modifies at most one primary hotspot plus direct support files.
- No functional behavior change in financial calculations.
- Targeted tests pass for touched areas.
- Number of files over advisory threshold trends down release over release.
