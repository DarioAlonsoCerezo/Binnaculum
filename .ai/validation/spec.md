# Validation Spec

## Required Files

- `AGENTS.md`
- `.ai/core/project-context.md`
- `.ai/core/engineering-rules.md`
- `.ai/core/workflows.md`
- `.ai/core/commands.md`
- `.ai/core/sql-guidelines.md`
- `.ai/adapters/copilot.md`
- `.ai/adapters/claude.md`
- `.ai/adapters/opencode.md`
- `.ai/registry/skills/sql-change.yaml`
- `.ai/registry/agents/sql-impact-analyzer.yaml`
- `.ai/adapters/copilot/sql-change.md`
- `.ai/adapters/claude/sql-change.md`
- `.ai/adapters/opencode/sql-change.md`

## Required Adapter References

Each adapter must reference all four core files.

## Required Search Policy

`AGENTS.md` must define semantic search as default-first strategy using:

1. `cocoindex_search`
2. `grep` and `glob` fallback
3. `read` after file identification

## Required AGENTS References

`AGENTS.md` must reference:

- `.ai/core/*`
- `.ai/adapters/copilot.md`
- `.ai/adapters/claude.md`
- `.ai/adapters/opencode.md`
- `.ai/registry/skills/sql-change.yaml`
- `.ai/registry/agents/sql-impact-analyzer.yaml`

## Policy

- Core is canonical.
- Adapters contain only tool-specific guidance.
- Critical rules must not diverge from core.
- Skills and agents are canonical under `.ai/registry/` and mapped per provider adapter.

## Forbidden Legacy Paths

The following legacy paths must not exist:

- `.github/copilot-instructions.md`
- `.github/instructions/*.instructions.md`
- `.github/skills/**`
