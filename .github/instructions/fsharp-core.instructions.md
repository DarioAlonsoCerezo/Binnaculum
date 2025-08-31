---
applyTo: "src/Core/**/*.fs"
---

# F# Core Business Logic Guidelines

When working on F# core logic, follow these domain-specific patterns:

## Investment Domain Modeling
- Use discriminated unions for investment types, broker types, currency types
- Model financial calculations with decimal precision (never use float for money)
- Use Option types for nullable financial data (`Some`/`None`)
- Implement proper domain validation with Result types

## Financial Calculation Patterns
- Follow existing patterns in `BrokerFinancialSnapshotManager`
- Use chunked processing for large portfolios (mobile memory constraints)
- Implement async workflows for concurrent calculations
- Always validate percentage calculations against expected ranges

## Database Layer Standards
- Use parameterized SQL queries in `src/Core/SQL/`
- Follow existing SQLite patterns with proper connection management
- Implement proper transaction handling for financial operations
- Use F# records for database models in `src/Core/Models/Models.fs`

## Performance Requirements
- Target mobile device constraints (limited CPU/memory)
- Use sequence operations for memory efficiency with large datasets
- Implement progress reporting for long-running calculations
- Follow existing performance test patterns in `BrokerFinancialSnapshotManagerPerformanceTests.fs`

## Error Handling Philosophy
- Use `failwith` for exceptional business rule violations
- Allow exceptions to bubble up to C# UI layer
- Never catch exceptions in core business logic
- Use descriptive error messages that help UI error display

## Testing Integration
- Every business logic function should have corresponding tests
- Use property-based testing for financial calculations where appropriate
- Test edge cases: zero values, negative values, very large portfolios
- Include performance benchmarks for calculation-intensive functions