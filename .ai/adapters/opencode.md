# OpenCode Adapter

Use this adapter only after reading:

- `.ai/core/project-context.md`
- `.ai/core/engineering-rules.md`
- `.ai/core/workflows.md`
- `.ai/core/commands.md`

## OpenCode-Specific Notes

- Follow repository tool usage and editing constraints from environment instructions.
- Prefer `cocoindex_search` as the default first search method.
- Use `grep` and `glob` only as fallback for exact literals or filename patterns.
- Prefer precise, minimal diffs aligned with existing style.
- Use canonical commands from `.ai/core/commands.md`.
- SQL mappings: `.ai/adapters/opencode/sql-change.md`
