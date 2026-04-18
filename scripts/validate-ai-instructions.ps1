$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

$requiredFiles = @(
  "AGENTS.md",
  ".ai/core/project-context.md",
  ".ai/core/engineering-rules.md",
  ".ai/core/workflows.md",
  ".ai/core/commands.md",
  ".ai/core/sql-guidelines.md",
  ".ai/adapters/copilot.md",
  ".ai/adapters/claude.md",
  ".ai/adapters/opencode.md",
  ".ai/validation/spec.md",
  ".ai/registry/skills/sql-change.yaml",
  ".ai/registry/agents/sql-impact-analyzer.yaml",
  ".ai/adapters/copilot/sql-change.md",
  ".ai/adapters/claude/sql-change.md",
  ".ai/adapters/opencode/sql-change.md"
)

$missing = @()
foreach ($file in $requiredFiles) {
  $path = Join-Path $repoRoot $file
  if (-not (Test-Path $path)) { $missing += $file }
}

if ($missing.Count -gt 0) {
  Write-Error ("Missing required files: " + ($missing -join ", "))
}

$coreRefs = @(
  ".ai/core/project-context.md",
  ".ai/core/engineering-rules.md",
  ".ai/core/workflows.md",
  ".ai/core/commands.md"
)

$adapters = @(
  ".ai/adapters/copilot.md",
  ".ai/adapters/claude.md",
  ".ai/adapters/opencode.md"
)

foreach ($adapter in $adapters) {
  $content = Get-Content (Join-Path $repoRoot $adapter) -Raw
  foreach ($coreRef in $coreRefs) {
    if ($content -notmatch [regex]::Escape($coreRef)) {
      Write-Error "$adapter does not reference $coreRef"
    }
  }
}

$agentsContent = Get-Content (Join-Path $repoRoot "AGENTS.md") -Raw
$requiredAgentRefs = @(
  ".ai/core/project-context.md",
  ".ai/core/engineering-rules.md",
  ".ai/core/workflows.md",
  ".ai/core/commands.md",
  ".ai/core/sql-guidelines.md",
  ".ai/adapters/copilot.md",
  ".ai/adapters/claude.md",
  ".ai/adapters/opencode.md",
  ".ai/registry/skills/sql-change.yaml",
  ".ai/registry/agents/sql-impact-analyzer.yaml"
)

foreach ($ref in $requiredAgentRefs) {
  if ($agentsContent -notmatch [regex]::Escape($ref)) {
    Write-Error "AGENTS.md does not reference $ref"
  }
}

$requiredSearchRefs = @(
  "cocoindex_search",
  "grep",
  "glob",
  "read"
)

foreach ($searchRef in $requiredSearchRefs) {
  if ($agentsContent -notmatch [regex]::Escape($searchRef)) {
    Write-Error "AGENTS.md does not include search policy reference: $searchRef"
  }
}

$forbiddenLegacy = @(
  ".github/copilot-instructions.md"
)

foreach ($legacyFile in $forbiddenLegacy) {
  if (Test-Path (Join-Path $repoRoot $legacyFile)) {
    Write-Error "Forbidden legacy file exists: $legacyFile"
  }
}

$forbiddenLegacyGlobs = @(
  ".github/instructions/*.instructions.md",
  ".github/skills/**"
)

foreach ($pattern in $forbiddenLegacyGlobs) {
  $matches = Get-ChildItem -Path (Join-Path $repoRoot $pattern) -File -Recurse -ErrorAction SilentlyContinue
  if ($matches) {
    $paths = $matches | ForEach-Object { $_.FullName.Replace($repoRoot.Path + [System.IO.Path]::DirectorySeparatorChar, "") }
    Write-Error ("Forbidden legacy paths exist for pattern " + $pattern + ": " + ($paths -join ", "))
  }
}

Write-Host "AI instruction validation passed."
