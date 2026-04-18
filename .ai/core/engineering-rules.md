# Engineering Rules

## Quality Gates (Mandatory)

- Core project builds successfully.
- Core tests pass.
- No style regressions in C# and XAML formatting.
- No untested changes in financial business logic.

## Language and Layering

- Use F# for financial logic, domain rules, and database operations.
- Use C# and XAML for UI and platform integration.
- Keep separation of concerns between UI and Core.

## Financial Safety Rules

- Never use `float` or `double` for monetary amounts.
- Validate financial calculations with tests for edge cases.
- Preserve deterministic behavior in snapshot calculations.

## Error Handling

- Core logic should fail fast and surface meaningful errors.
- Do not hide core failures with broad exception swallowing.

## Database Rules

- Use parameterized SQL only.
- Follow existing database model and extension patterns.
- Treat schema changes as high-impact and test with existing data.

## Performance Rules

- Prefer async and non-blocking flows for heavy operations.
- Use memory-efficient processing for large portfolios.
- Include or update performance tests when touching sensitive paths.

## Manageability Rules for AI Agents

- These rules are advisory until explicit CI enforcement is introduced.
- Apply rules to changed files first, then reduce legacy debt incrementally.

### File and Method Length Thresholds

| Scope | File max lines | Method/function max lines |
| --- | ---: | ---: |
| Core and UI code (non-generated) | 400 | 40 |
| UI page XAML (`src/UI/Pages/*.xaml`) | 700 | n/a |
| UI control/template XAML (`src/UI/Controls/**/*.xaml`) | 450 | n/a |
| UI style dictionaries (`src/UI/Resources/Styles/*.xaml`) | 900 | n/a |
| UI code-behind (`*.xaml.cs`) | 300 | 40 |
| Tests (`src/Tests/**/*`) | 600 | 60 |

### Complexity and Structure Guidance

- Keep cyclomatic complexity at or below 12 for new or changed methods.
- Keep nesting depth at or below 3 for new or changed methods.
- Keep parameter count at or below 5 for new or changed methods.
- In XAML pages, keep major visual sections between 7 and 9 and extract repeated blocks to reusable controls.

### Exclusions

- Exclude generated files and heavy resources from file/method thresholds.
- Typical exclusions include `*.Designer.cs`, `*.g.cs`, `*.resx`, binary assets, fonts, and large snapshot or fixture data files.
- For generated wrappers under resource paths (for example icon/font mapping classes), treat them as generated and exclude from threshold checks.

### Exception Policy

- Allow exceptions only with a clear technical reason and follow-up refactor task.
- Exceptions must include a target scope and a due milestone to avoid permanent drift.
