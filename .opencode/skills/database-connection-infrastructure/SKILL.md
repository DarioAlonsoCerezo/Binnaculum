---
name: database-connection-infrastructure
description: Define rules and conventions for managing SQLite database connections, connection lifecycle, mode switching, and test-only database utilities in the Core layer.
compatibility: opencode
metadata:
  source: .ai/registry/skills/database-connection-infrastructure.yaml
  version: 1.0.0
  owner: core-team
---

# Database Connection Infrastructure Skill

## Purpose
Define rules and conventions for managing SQLite database connections, connection lifecycle, mode switching, pruning of dead infrastructure code, and test-only database utilities in the Core layer.

## Inputs
- requested_change
- affected_files
- connection_context

## Outputs
- implementation_plan
- changed_files
- infrastructure_compliance_check

## Constraints
- All SqliteConnection creation MUST go through ConnectionProvider.createConnection.
- The singleton mutable connection in Database.fs must not be accessed directly from other modules.
- PRAGMA foreign_keys lifecycle: ON in production connect(), OFF only in wipeAllTablesForTesting.
- setConnectionMode must always close and dispose existing connection before switching.
- No raw new SqliteConnection(...) calls outside ConnectionProvider.fs.
- No raw SqliteCommand usage outside Database.fs helpers (createCommand, executeNonQuery, etc.).
- Test-only utilities (wipeAllTablesForTesting) must be clearly flagged as test-only.
- Converge duplicate ConnectionProvider.fs files to a single source.

## Allowed Tools
- cocoindex_search
- grep
- glob
- read
- apply_patch
- bash

## Workflow
- Locate current Database.fs, ConnectionProvider.fs, and DataResetExtensions.fs content.
- Identify all modules that create connections or use raw SqliteCommand/SqliteConnection.
- Verify connection lifecycle matches connect -> command -> execute -> dispose pattern.
- Verify PRAGMA foreign_keys follows defined lifecycle (ON in connect, OFF only in wipeAllTablesForTesting).
- Flag any raw new SqliteConnection(...) or direct singleton connection access.
- Verify setConnectionMode closes prior connection.
- Produce implementation plan and compliance report.

## Acceptance Criteria
- All connection creation funnels through ConnectionProvider.createConnection.
- PRAGMA foreign_keys ON is enforced in production connect().
- All modules use Database.fs helpers (createCommand, executeNonQuery, etc.).
- setConnectionMode safely closes prior connection.
- No duplicate ConnectionProvider.fs sources.
- Test-only utilities are clearly isolated.

## Examples
- Add a new module that needs to open a read-only connection for diagnostics.
- Fix raw SqliteConnection usage found in a database extension module.
- Remove duplicate ConnectionProvider.fs while preserving tests.
- Refactor setConnectionMode to handle disposal correctly.
