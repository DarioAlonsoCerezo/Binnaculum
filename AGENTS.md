# AGENTS.md

This file is the entry point for AI assistants working in this repository.

## Scope

- Language: technical English for all AI instruction files.
- Spanish is used only in existing translated documentation (for example `README.es.md` and `docs/**/*.es.md`).
- Source of truth for assistant behavior is the `.ai/` directory.

## Canonical Instruction Sources

Read these first, in order:

1. `.ai/core/project-context.md`
2. `.ai/core/engineering-rules.md`
3. `.ai/core/workflows.md`
4. `.ai/core/commands.md`
5. `.ai/core/sql-guidelines.md`

## Tool Adapters

Use the adapter for your environment after reading the core files:

- GitHub Copilot: `.ai/adapters/copilot.md`
- Claude: `.ai/adapters/claude.md`
- OpenCode: `.ai/adapters/opencode.md`

## Search Strategy (Default)

Use semantic search first when available.

1. `cocoindex_search` (default first option)
2. `grep` and `glob` (fallback for exact text or filename patterns)
3. `read` (only after target files are identified)

### Exceptions

- If a file path is already known exactly, read it directly.
- If looking for an exact literal token, use `grep`.
- Use targeted reads for final line-level verification.

## Skills and Agents

Canonical definitions live in:

- Skills: `.ai/registry/skills/`
- Agents: `.ai/registry/agents/`

SQL-specific resources:

- Skill: `.ai/registry/skills/sql-change.yaml`
- Agent: `.ai/registry/agents/sql-impact-analyzer.yaml`

Provider-specific adapter mappings live in:

- OpenCode: `.ai/adapters/opencode/`
- Claude: `.ai/adapters/claude/`
- Copilot: `.ai/adapters/copilot/`

## Legacy Policy

Legacy assistant instruction files under `.github/` are not part of the canonical system.
Do not reintroduce `.github/copilot-instructions.md`, `.github/instructions/*.instructions.md`, or `.github/skills/**`.

## Update Policy

- Update `core` files first.
- Then update adapters only if tool-specific behavior is required.
- Do not duplicate or fork critical engineering rules across adapters.

## Validation

Instruction consistency is validated in CI.
If CI fails, fix `.ai/core/*` and adapter references before merging.
