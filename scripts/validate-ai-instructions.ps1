$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

$projectionScript = Join-Path $PSScriptRoot "generate-ai-projections.ps1"
if (-not (Test-Path $projectionScript)) {
  Write-Error "Missing required script: scripts/generate-ai-projections.ps1"
}

& $projectionScript

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
  ".ai/adapters/opencode/sql-change.md",
  "scripts/generate-ai-projections.ps1"
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
  ".github/instructions/*.instructions.md"
)

foreach ($pattern in $forbiddenLegacyGlobs) {
  $matches = Get-ChildItem -Path (Join-Path $repoRoot $pattern) -File -Recurse -ErrorAction SilentlyContinue
  if ($matches) {
    $paths = $matches | ForEach-Object { $_.FullName.Replace($repoRoot.Path + [System.IO.Path]::DirectorySeparatorChar, "") }
    Write-Error ("Forbidden legacy paths exist for pattern " + $pattern + ": " + ($paths -join ", "))
  }
}

$canonicalSkillFiles = Get-ChildItem -Path (Join-Path $repoRoot ".ai/registry/skills") -Filter "*.yaml" -File -ErrorAction SilentlyContinue
if (-not $canonicalSkillFiles -or $canonicalSkillFiles.Count -eq 0) {
  Write-Error "No canonical skill definitions found under .ai/registry/skills"
}

foreach ($canonicalSkill in $canonicalSkillFiles) {
  $canonicalContent = Get-Content $canonicalSkill.FullName -Raw
  $idMatch = [regex]::Match($canonicalContent, '(?m)^\s*id\s*:\s*(.+?)\s*$')
  if (-not $idMatch.Success) {
    Write-Error "Canonical skill is missing required id: $($canonicalSkill.FullName)"
  }

  $skillId = $idMatch.Groups[1].Value.Trim()
  if ($skillId -match '^[''\"](.*)[''\"]$') {
    $skillId = $matches[1]
  }

  $expectedProjected = @(
    ".github/skills/$skillId/SKILL.md",
    ".opencode/skills/$skillId/SKILL.md",
    ".claude/skills/$skillId/SKILL.md",
    ".agents/skills/$skillId/SKILL.md"
  )

  foreach ($projected in $expectedProjected) {
    if (-not (Test-Path (Join-Path $repoRoot $projected))) {
      Write-Error "Missing projected SKILL.md for canonical skill '$skillId': $projected"
    }
  }
}

$canonicalAgentFiles = Get-ChildItem -Path (Join-Path $repoRoot ".ai/registry/agents") -Filter "*.yaml" -File -ErrorAction SilentlyContinue
if (-not $canonicalAgentFiles -or $canonicalAgentFiles.Count -eq 0) {
  Write-Error "No canonical agent definitions found under .ai/registry/agents"
}

foreach ($canonicalAgent in $canonicalAgentFiles) {
  $canonicalContent = Get-Content $canonicalAgent.FullName -Raw
  $idMatch = [regex]::Match($canonicalContent, '(?m)^\s*id\s*:\s*(.+?)\s*$')
  if (-not $idMatch.Success) {
    Write-Error "Canonical agent is missing required id: $($canonicalAgent.FullName)"
  }

  $agentId = $idMatch.Groups[1].Value.Trim()
  if ($agentId -match '^[''\"](.*)[''\"]$') {
    $agentId = $matches[1]
  }

  $expectedProjected = @(
    ".github/agents/$agentId.agent.md",
    ".opencode/agents/$agentId.md",
    ".agents/agents/$agentId.md"
  )

  foreach ($projected in $expectedProjected) {
    if (-not (Test-Path (Join-Path $repoRoot $projected))) {
      Write-Error "Missing projected agent for canonical agent '$agentId': $projected"
    }
  }
}

$skillPathPatterns = @(
  ".github/skills/*/SKILL.md",
  ".opencode/skills/*/SKILL.md",
  ".claude/skills/*/SKILL.md",
  ".agents/skills/*/SKILL.md"
)

$skillFiles = @()
foreach ($pattern in $skillPathPatterns) {
  $matches = Get-ChildItem -Path (Join-Path $repoRoot $pattern) -File -ErrorAction SilentlyContinue
  if ($matches) {
    $skillFiles += $matches
  }
}

$skillSeenByPath = @{}

foreach ($skillFile in $skillFiles) {
  $relativePath = $skillFile.FullName.Replace($repoRoot.Path + [System.IO.Path]::DirectorySeparatorChar, "")
  $skillDirName = Split-Path -Leaf (Split-Path -Parent $skillFile.FullName)
  $content = Get-Content $skillFile.FullName -Raw

  $frontmatterMatch = [regex]::Match($content, '(?s)\A---\r?\n(.*?)\r?\n---')
  if (-not $frontmatterMatch.Success) {
    Write-Error "Skill file missing YAML frontmatter: $relativePath"
  }

  $frontmatter = $frontmatterMatch.Groups[1].Value
  $nameMatch = [regex]::Match($frontmatter, '(?m)^\s*name\s*:\s*(.+?)\s*$')
  $descriptionMatch = [regex]::Match($frontmatter, '(?m)^\s*description\s*:\s*(.+?)\s*$')

  if (-not $nameMatch.Success) {
    Write-Error "Skill file missing required 'name' field: $relativePath"
  }

  if (-not $descriptionMatch.Success) {
    Write-Error "Skill file missing required 'description' field: $relativePath"
  }

  $name = $nameMatch.Groups[1].Value.Trim()
  $description = $descriptionMatch.Groups[1].Value.Trim()

  if ($name -match '^[''\"](.*)[''\"]$') {
    $name = $matches[1]
  }

  if ($description -match '^[''\"](.*)[''\"]$') {
    $description = $matches[1]
  }

  if ($name.Length -lt 1 -or $name.Length -gt 64) {
    Write-Error "Skill name length must be 1-64 characters: $relativePath"
  }

  if ($name -notmatch '^[a-z0-9]+(-[a-z0-9]+)*$') {
    Write-Error "Skill name has invalid format: $relativePath ($name)"
  }

  if ($name -ne $skillDirName) {
    Write-Error "Skill name must match directory name: $relativePath (name=$name, dir=$skillDirName)"
  }

  if ($description.Length -lt 1 -or $description.Length -gt 200) {
    Write-Error "Skill description length must be 1-200 characters for cross-environment compatibility: $relativePath"
  }

  if ($skillSeenByPath.ContainsKey($relativePath)) {
    Write-Error "Duplicate skill file path detected: $relativePath"
  }

  $skillSeenByPath[$relativePath] = $name
}

$agentPathPatterns = @(
  ".github/agents/*.agent.md",
  ".opencode/agents/*.md",
  ".agents/agents/*.md"
)

$agentFiles = @()
foreach ($pattern in $agentPathPatterns) {
  $matches = Get-ChildItem -Path (Join-Path $repoRoot $pattern) -File -ErrorAction SilentlyContinue
  if ($matches) {
    $agentFiles += $matches
  }
}

$agentSeenByPath = @{}

foreach ($agentFile in $agentFiles) {
  $relativePath = $agentFile.FullName.Replace($repoRoot.Path + [System.IO.Path]::DirectorySeparatorChar, "")
  $baseName = [System.IO.Path]::GetFileNameWithoutExtension($agentFile.Name)
  if ($agentFile.Name.EndsWith(".agent.md")) {
    $baseName = [System.IO.Path]::GetFileNameWithoutExtension($baseName)
  }
  $content = Get-Content $agentFile.FullName -Raw

  $frontmatterMatch = [regex]::Match($content, '(?s)\A---\r?\n(.*?)\r?\n---')
  if (-not $frontmatterMatch.Success) {
    Write-Error "Agent file missing YAML frontmatter: $relativePath"
  }

  $frontmatter = $frontmatterMatch.Groups[1].Value
  $nameMatch = [regex]::Match($frontmatter, '(?m)^\s*name\s*:\s*(.+?)\s*$')
  $descriptionMatch = [regex]::Match($frontmatter, '(?m)^\s*description\s*:\s*(.+?)\s*$')

  if (-not $nameMatch.Success) {
    Write-Error "Agent file missing required 'name' field: $relativePath"
  }

  if (-not $descriptionMatch.Success) {
    Write-Error "Agent file missing required 'description' field: $relativePath"
  }

  $name = $nameMatch.Groups[1].Value.Trim()
  $description = $descriptionMatch.Groups[1].Value.Trim()

  if ($name -match '^[''\"](.*)[''\"]$') {
    $name = $matches[1]
  }

  if ($description -match '^[''\"](.*)[''\"]$') {
    $description = $matches[1]
  }

  if ($name.Length -lt 1 -or $name.Length -gt 64) {
    Write-Error "Agent name length must be 1-64 characters: $relativePath"
  }

  if ($name -notmatch '^[a-z0-9]+(-[a-z0-9]+)*$') {
    Write-Error "Agent name has invalid format: $relativePath ($name)"
  }

  if ($name -ne $baseName) {
    Write-Error "Agent name must match file name: $relativePath (name=$name, file=$baseName)"
  }

  if ($description.Length -lt 1 -or $description.Length -gt 200) {
    Write-Error "Agent description length must be 1-200 characters for cross-environment compatibility: $relativePath"
  }

  if ($agentSeenByPath.ContainsKey($relativePath)) {
    Write-Error "Duplicate agent file path detected: $relativePath"
  }

  $agentSeenByPath[$relativePath] = $name
}

Write-Host "AI instruction validation passed."
