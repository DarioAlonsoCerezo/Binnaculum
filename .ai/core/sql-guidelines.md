# SQL Guidelines

## Scope

Use these rules for SQL changes in `src/Core/SQL/*.fs` and related database access code in `src/Core/Database/`.

## SQL Source of Truth

- SQL statements are defined as F# string constants in `src/Core/SQL/*.fs`.
- There are no standalone `.sql` files in this repository.
- New SQL must follow existing module structure: `createTable`, `insert`, `update`, `delete`, and `get*` queries.

## Naming and Constants

- Use table constants from `src/Core/Keys/TableName.fs`.
- Use field constants from `src/Core/Keys/FieldName.fs`.
- Use SQL parameter constants from `src/Core/Keys/SQLParameterName.fs`.
- Avoid hardcoded table names, field names, and parameter names in new SQL.

## Schema and Integrity Rules

- Prefer `CREATE TABLE IF NOT EXISTS` for table creation.
- Define required foreign keys and keep cascade behavior consistent with current domain rules.
- Add or update indexes for common filter/sort columns in new or modified tables.
- Keep `UpdatedAt` behavior aligned with existing `AFTER UPDATE` triggers where applicable.
- Assume foreign keys are enabled by database bootstrap (`PRAGMA foreign_keys = ON`).

## Query Construction Rules

- Always use parameterized SQL.
- Keep query text in the corresponding module under `src/Core/SQL/`.
- Keep selection and ordering deterministic for time-series and snapshot data.
- Avoid string concatenation for SQL values.

## Transaction and Write Rules

- Use `executeInTransaction` for multi-step writes that must be atomic.
- Follow existing insert-id patterns (`last_insert_rowid`) when creating parent-child records.

## Validation Checklist for SQL Changes

- Core build succeeds.
- Core tests pass.
- Database and snapshot-related tests are updated when behavior changes.
- Index and foreign key impact is reviewed for performance and correctness.
