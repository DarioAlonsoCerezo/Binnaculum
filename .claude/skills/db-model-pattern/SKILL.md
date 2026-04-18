---
name: db-model-pattern
description: Implement and update database entities using the repository DB pattern (DatabaseModel + SQL Query + Extensions + mapping) with consistent conventions.
---

# DB Model Pattern Skill

## Purpose
Implement and update database entities using the repository DB pattern (DatabaseModel + SQL Query + Extensions + mapping) with consistent conventions.

## Inputs
- requested_change
- target_entity
- expected_behavior

## Outputs
- implementation_plan
- changed_files
- validation_summary

## Constraints
- Keep SQL in src/Core/SQL and persistence logic in src/Core/Database/*Extensions.fs.
- Use IEntity for all DB entities and IAuditEntity when audit columns are present.
- Use fillEntityAuditable and getAudit for auditable entities.
- Use DataReaderExtensions and TypeParser for conversions.
- Keep SQL parameterized and use constants for names.
- Add and maintain getAll, getById, and domain-specific queries as needed.

## Allowed Tools
- cocoindex_search
- grep
- glob
- read
- apply_patch
- bash

## Workflow
- Locate analogous entity implementations as references.
- Implement or update DatabaseModel record and interfaces.
- Implement or update SQL Query module with required CRUD and indexes.
- Implement or update entity Extensions with fill, read, save, delete, and getters.
- Update model mappings and run build/tests from .ai/core/commands.md.

## Acceptance Criteria
- Entity follows canonical pattern used by existing Core database modules.
- SQL and extensions compile and tests pass.
- Audit and data conversion behavior is consistent with existing entities.
- No layer boundary violations are introduced.

## Examples
- Add a new auditable entity with full Query, Extensions, and mapping support.
- Refactor a non-compliant entity to align with fill/read/save/delete conventions.
- Add a filtered query while preserving naming and parameterization conventions.
