# AI Instructions Architecture

This directory centralizes AI assistant guidance for Binnaculum.

## Goals

- Keep one canonical source of truth.
- Support multiple assistant environments with minimal duplication.
- Prevent instruction drift through validation.

## Structure

- `core/`: canonical, tool-agnostic project instructions.
- `registry/`: canonical skills and agents definitions.
- `adapters/`: tool-specific wrappers that reference `core/`.
- `validation/`: validation specs for CI checks.

## Authoring Rules

- Use technical English.
- Keep rules precise, testable, and actionable.
- Prefer references to existing repository paths over broad statements.

## Change Workflow

1. Update `core/*`.
2. Update `adapters/*` only for tool-specific syntax or constraints.
3. Run validation.
4. Update transition notes if legacy files still remain.

## Cross-Environment Skill Standard

Use this process when creating a new skill that must work with GitHub Copilot, OpenCode, and Claude.

### 1) Keep one canonical definition

- Define the skill once in `.ai/registry/skills/`.
- Keep behavior, workflow, and acceptance criteria canonical there.
- Avoid provider-only logic in the canonical definition.

### 2) Project to provider-native `SKILL.md`

For runtime compatibility, each environment expects a folder-based skill with `SKILL.md` and YAML frontmatter.

- Copilot-compatible locations: `.github/skills/<name>/SKILL.md`, `.claude/skills/<name>/SKILL.md`, `.agents/skills/<name>/SKILL.md`
- OpenCode-compatible locations: `.opencode/skills/<name>/SKILL.md` (also supports `.claude/skills` and `.agents/skills` discovery)
- Claude-compatible locations: `.claude/skills/<name>/SKILL.md`

In this repository, projections are generated automatically from canonical files by:

- `./scripts/generate-skill-projections.ps1`

This script deletes and recreates:

- `.github/skills/*`
- `.opencode/skills/*`
- `.claude/skills/*`
- `.agents/skills/*`

Generated frontmatter is provider-specific:

- GitHub (`.github/skills/*/SKILL.md`): `name`, `description`, and `allowed-tools` when present.
- OpenCode (`.opencode/skills/*/SKILL.md`): `name`, `description`, `compatibility`, `metadata`.
- Claude (`.claude/skills/*/SKILL.md`): minimal portable fields (`name`, `description`).
- Agent-spec projection (`.agents/skills/*/SKILL.md`): minimal portable fields (`name`, `description`).

### 3) Enforce portable frontmatter

For maximum compatibility across the three environments, always include:

- `name` (required)
- `description` (required)

Optional, portable fields:

- `license`
- `compatibility`
- `metadata`

If using provider-specific fields (for example `allowed-tools` or `dependencies`), isolate them in provider adapters and do not treat them as canonical.

### 4) Follow strict naming and length constraints

- `name` must match the skill directory name.
- `name` format: lowercase alphanumeric plus single hyphen separators.
- Regex: `^[a-z0-9]+(-[a-z0-9]+)*$`
- Length: 1-64 chars.
- Use `description` that states both capability and trigger conditions.
- To remain portable, keep `description` <= 200 characters (Claude limit), even though other implementations may allow up to 1024.

### 5) Keep skill content focused

- One repeatable workflow per skill.
- Include clear activation cues (when to use).
- Keep steps deterministic and testable.
- Put large references/scripts in the skill directory and reference them from `SKILL.md`.

### 6) Validate before merge

- Confirm canonical entry exists in `.ai/registry/skills/`.
- Confirm provider projections exist in adapter-managed locations.
- Regenerate projections using `./scripts/generate-skill-projections.ps1`.
- Run `./scripts/validate-ai-instructions.ps1`.
- Ensure no legacy-only skill locations are used as canonical source.
