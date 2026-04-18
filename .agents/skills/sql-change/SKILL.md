---
name: sql-change
description: Standardize how SQL changes are implemented in the repository while preserving naming conventions, data integrity, and performance characteristics.
---

# SQL Change Skill

## Purpose
Standardize how SQL changes are implemented in the repository while preserving naming conventions, data integrity, and performance characteristics.

## Inputs
- requested_change
- target_tables
- expected_behavior

## Outputs
- implementation_plan
- changed_files
- validation_summary

## Constraints
- Always use parameterized SQL.
- Keep SQL in src/Core/SQL modules.
- Use TableName, FieldName, and SQLParameterName constants.
- Preserve foreign key and cascade correctness.
- Preserve or improve index coverage for filter and ordering paths.

## Allowed Tools
- cocoindex_search
- grep
- glob
- read
- apply_patch
- bash

## Workflow
- Locate relevant SQL modules and dependent database extensions.
- Assess schema, foreign key, trigger, and index impact.
- Implement query or schema updates using existing patterns.
- Update transaction boundaries if write semantics changed.
- Validate with build and test commands from .ai/core/commands.md.

## Acceptance Criteria
- SQL is parameterized and follows naming constants.
- Affected paths compile and tests pass.
- No regression in referential integrity behavior.
- Query behavior matches requested_change.

## Examples
- Add filtered query by date range in an existing *Query module.
- Add index for a newly introduced high-frequency filter column.
- Add foreign key with cascade rules consistent with parent-child ownership.
