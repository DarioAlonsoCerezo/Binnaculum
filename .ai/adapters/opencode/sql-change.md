# OpenCode SQL Skill Mapping

Canonical source:

- `.ai/registry/skills/sql-change.yaml`

OpenCode execution notes:

- Prefer `cocoindex_search` first for locating SQL and dependency touchpoints.
- Fall back to `grep` and `glob` for exact tokens and file patterns.
- Keep SQL updates in `src/Core/SQL/*.fs` and use constants from `TableName`, `FieldName`, and `SQLParameterName`.

Canonical agent mapping:

- `.ai/registry/agents/sql-impact-analyzer.yaml`
