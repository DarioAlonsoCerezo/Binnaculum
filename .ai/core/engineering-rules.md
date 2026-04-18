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
