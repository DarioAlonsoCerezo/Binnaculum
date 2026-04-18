# Claude SQL Skill Mapping

Canonical source:

- `.ai/registry/skills/sql-change.yaml`

Claude execution notes:

- Use semantic search first when available to locate SQL modules and related extensions.
- Keep implementation scoped to SQL and database core layers unless contracts require broader changes.
- Preserve repository SQL conventions and validation flow from `.ai/core/sql-guidelines.md`.

Canonical agent mapping:

- `.ai/registry/agents/sql-impact-analyzer.yaml`
