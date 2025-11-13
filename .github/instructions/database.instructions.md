---
applyTo: "src/Core/Database/**/*.fs"
---

# Database Guidelines for Binnaculum

When working on SQLite database operations, follow these patterns:

## SQLite Patterns
- Use parameterized SQL queries (never string concatenation)
- Follow existing connection management patterns
- Implement proper transaction handling for financial operations
- Use F# records for database models in `src/Core/Database/DatabaseModel.fs` and `src/Core/Database/SnapshotsModel.fs`

## Database Model Standards
**Locations**: 
- `src/Core/Database/DatabaseModel.fs` - Core database entities (Broker, Trade, Dividend, etc.)
- `src/Core/Database/SnapshotsModel.fs` - Snapshot entities (BrokerFinancialSnapshot, TickerCurrencySnapshot, etc.)
- `src/Core/Models/Models.fs` - UI models that parse from database entities

When updating database models:
- Use F# records with proper field types in `DatabaseModel.fs` or `SnapshotsModel.fs`
- Ensure decimal precision for monetary fields (never float)
- Use Option types for nullable fields
- Follow existing naming conventions (PascalCase for properties)
- Implement `IEntity` and `IAuditEntity` interfaces where appropriate
- Update corresponding UI models in `Models.fs` if needed for UI consumption

## Database Extensions
**Location**: `src/Core/Database/`

When adding database operations:
- Create extension methods in appropriate `*Extensions.fs` files
- Follow async patterns for all database operations
- Implement proper error handling (let exceptions bubble)
- Add corresponding tests to `Core.Tests`

## SQL Query Management
**Location**: `src/Core/SQL/`

When adding or modifying SQL:
- Store queries as module-level constants
- Use parameterized queries exclusively
- Test queries with realistic data volumes
- Consider query performance on mobile devices

## Transaction Handling
For financial operations that modify multiple tables:
- Use explicit transaction scope
- Follow existing transaction patterns
- Ensure proper rollback on errors
- Test transaction isolation

## Database Schema Changes
When schema changes are needed:
- Update database models in `DatabaseModel.fs` or `SnapshotsModel.fs`
- Update corresponding UI models in `Models.fs` for UI consumption
- Create migration logic if needed
- Update all affected queries in `SQL/` directory
- Test with existing database files
- **Always coordinate with maintainer for schema changes**

## Testing Database Operations
All database operations must have tests:
- Test with in-memory SQLite database
- Include edge cases (null values, large datasets)
- Verify transaction behavior
- Test concurrent access patterns
- Reference: Existing tests in `Core.Tests/Database/`

## Common Database Anti-Patterns to Avoid
- String concatenation for SQL queries
- Using float/double for monetary values
- Forgetting to dispose database connections
- Synchronous database operations
- Not testing with realistic data volumes
