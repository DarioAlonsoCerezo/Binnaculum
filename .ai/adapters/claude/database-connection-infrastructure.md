# Claude Database Connection Infrastructure Skill Mapping

Canonical source:

- `.ai/registry/skills/database-connection-infrastructure.yaml`

Claude execution notes:

- Apply repository database connection rules to any `Database.fs`, `ConnectionProvider.fs`, or `DataResetExtensions.fs` changes.
- Use semantic search first when available to locate connection infrastructure code.
- Follow `.ai/core/sql-guidelines.md` and this SKILL for constraints and validation.

Canonical agent mapping:

- `.ai/registry/agents/database-connection-infrastructure-auditor.yaml`
