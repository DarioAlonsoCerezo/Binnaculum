param(
  [ValidateSet("all", "skills", "agents")]
  [string]$Only = "all"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

function Get-YamlScalar {
  param(
    [string[]]$Lines,
    [string]$Key
  )

  $pattern = "^(?<indent>\s*)" + [regex]::Escape($Key) + ":\s*(?<value>.*)$"
  for ($i = 0; $i -lt $Lines.Count; $i++) {
    $m = [regex]::Match($Lines[$i], $pattern)
    if (-not $m.Success) { continue }

    $value = $m.Groups["value"].Value.Trim()
    if ($value -eq "") { return "" }

    if ($value -in @(">-", ">", "|-", "|")) {
      $block = @()
      $baseIndent = $m.Groups["indent"].Length
      for ($j = $i + 1; $j -lt $Lines.Count; $j++) {
        $line = $Lines[$j]
        if ($line.Trim() -eq "") {
          $block += ""
          continue
        }

        $currentIndent = ($line.Length - $line.TrimStart().Length)
        if ($currentIndent -le $baseIndent) { break }

        $trimmed = $line.Substring([Math]::Min($baseIndent + 2, $line.Length))
        $block += $trimmed
      }

      return (($block -join " ") -replace "\s+", " ").Trim()
    }

    if ($value -match '^[''\"](.*)[''\"]$') { return $matches[1] }
    return $value
  }

  return ""
}

function Get-YamlList {
  param(
    [string[]]$Lines,
    [string]$Key
  )

  $startPattern = "^" + [regex]::Escape($Key) + ":\s*$"
  for ($i = 0; $i -lt $Lines.Count; $i++) {
    if (-not [regex]::IsMatch($Lines[$i], $startPattern)) { continue }

    $result = @()
    for ($j = $i + 1; $j -lt $Lines.Count; $j++) {
      $line = $Lines[$j]
      if ($line.Trim() -eq "") { continue }
      if ([regex]::IsMatch($line, "^\S")) { break }

      $itemMatch = [regex]::Match($line, "^\s*-\s*(.+)$")
      if ($itemMatch.Success) {
        $value = $itemMatch.Groups[1].Value.Trim()
        if ($value -match '^[''\"](.*)[''\"]$') { $value = $matches[1] }
        $result += $value
      }
    }

    return $result
  }

  return @()
}

function Convert-TextToDescription {
  param(
    [string]$Text,
    [string]$Fallback
  )

  $normalized = ($Text -replace "\s+", " ").Trim()
  if ([string]::IsNullOrWhiteSpace($normalized)) { $normalized = $Fallback }
  if ($normalized.Length -gt 200) { return $normalized.Substring(0, 197) + "..." }
  return $normalized
}

function Assert-CanonicalId {
  param(
    [string]$Id,
    [string]$Path,
    [string]$TypeName
  )

  if ([string]::IsNullOrWhiteSpace($Id)) {
    Write-Error "Missing required 'id' in $Path"
  }

  if ($Id -notmatch '^[a-z0-9]+(-[a-z0-9]+)*$') {
    Write-Error "$TypeName id has invalid format in ${Path}: $Id"
  }
}

function Build-CanonicalBody {
  param(
    [string]$Title,
    [string]$Purpose,
    [string[]]$ScopeInclude,
    [string[]]$ScopeExclude,
    [string[]]$Inputs,
    [string[]]$Outputs,
    [string[]]$Constraints,
    [string[]]$AllowedTools,
    [string[]]$WorkflowSteps,
    [string[]]$AcceptanceCriteria,
    [string[]]$Examples
  )

  $bodyLines = @("# $Title", "")

  if (-not [string]::IsNullOrWhiteSpace($Purpose)) {
    $bodyLines += @("## Purpose", $Purpose, "")
  }

  if ($ScopeInclude.Count -gt 0 -or $ScopeExclude.Count -gt 0) {
    $bodyLines += @("## Scope")
    if ($ScopeInclude.Count -gt 0) {
      $bodyLines += ""
      $bodyLines += "### Include"
      foreach ($item in $ScopeInclude) { $bodyLines += "- $item" }
    }
    if ($ScopeExclude.Count -gt 0) {
      $bodyLines += ""
      $bodyLines += "### Exclude"
      foreach ($item in $ScopeExclude) { $bodyLines += "- $item" }
    }
    $bodyLines += ""
  }

  if ($Inputs.Count -gt 0) {
    $bodyLines += @("## Inputs")
    foreach ($item in $Inputs) { $bodyLines += "- $item" }
    $bodyLines += ""
  }

  if ($Outputs.Count -gt 0) {
    $bodyLines += @("## Outputs")
    foreach ($item in $Outputs) { $bodyLines += "- $item" }
    $bodyLines += ""
  }

  if ($Constraints.Count -gt 0) {
    $bodyLines += @("## Constraints")
    foreach ($item in $Constraints) { $bodyLines += "- $item" }
    $bodyLines += ""
  }

  if ($AllowedTools.Count -gt 0) {
    $bodyLines += @("## Allowed Tools")
    foreach ($item in $AllowedTools) { $bodyLines += "- $item" }
    $bodyLines += ""
  }

  if ($WorkflowSteps.Count -gt 0) {
    $bodyLines += @("## Workflow")
    foreach ($item in $WorkflowSteps) { $bodyLines += "- $item" }
    $bodyLines += ""
  }

  if ($AcceptanceCriteria.Count -gt 0) {
    $bodyLines += @("## Acceptance Criteria")
    foreach ($item in $AcceptanceCriteria) { $bodyLines += "- $item" }
    $bodyLines += ""
  }

  if ($Examples.Count -gt 0) {
    $bodyLines += @("## Examples")
    foreach ($item in $Examples) { $bodyLines += "- $item" }
    $bodyLines += ""
  }

  return $bodyLines
}

function Convert-ToCopilotTools {
  param([string[]]$AllowedTools)

  $mapping = @{
    "bash" = "execute"
    "read" = "read"
    "grep" = "search"
    "glob" = "search"
    "cocoindex_search" = "search"
    "apply_patch" = "edit"
    "task" = "agent"
    "webfetch" = "web"
    "todowrite" = "todo"
  }

  $result = @()
  foreach ($tool in $AllowedTools) {
    $normalized = $tool.Trim().ToLowerInvariant()
    if ($mapping.ContainsKey($normalized)) {
      $mapped = $mapping[$normalized]
      if ($mapped -notin $result) { $result += $mapped }
    }
  }

  return $result
}

function New-SkillFrontmatter {
  param(
    [string]$Provider,
    [string]$Id,
    [string]$Description,
    [string]$Source,
    [string]$Version,
    [string]$Owner,
    [string[]]$AllowedTools
  )

  switch ($Provider) {
    "github" {
      $lines = @("---", "name: $Id", "description: $Description")
      if ($AllowedTools.Count -gt 0) {
        $lines += "allowed-tools:"
        foreach ($tool in $AllowedTools) { $lines += "  - $tool" }
      }
      return $lines + @("---", "")
    }
    "opencode" {
      $lines = @(
        "---",
        "name: $Id",
        "description: $Description",
        "compatibility: opencode",
        "metadata:",
        "  source: $Source"
      )
      if (-not [string]::IsNullOrWhiteSpace($Version)) { $lines += "  version: $Version" }
      if (-not [string]::IsNullOrWhiteSpace($Owner)) { $lines += "  owner: $Owner" }
      return $lines + @("---", "")
    }
    "claude" {
      return @("---", "name: $Id", "description: $Description", "---", "")
    }
    "agents" {
      return @("---", "name: $Id", "description: $Description", "---", "")
    }
    default { Write-Error "Unsupported skill provider: $Provider" }
  }
}

function New-AgentFrontmatter {
  param(
    [string]$Provider,
    [string]$Id,
    [string]$Description,
    [string]$Source,
    [string]$Version,
    [string]$Owner,
    [string[]]$AllowedTools
  )

  switch ($Provider) {
    "github" {
      $lines = @("---", "name: $Id", "description: $Description")
      $copilotTools = Convert-ToCopilotTools -AllowedTools $AllowedTools
      if ($copilotTools.Count -gt 0) {
        $lines += "tools:"
        foreach ($tool in $copilotTools) { $lines += "  - $tool" }
      }
      return $lines + @("---", "")
    }
    "opencode" {
      $lines = @(
        "---",
        "name: $Id",
        "description: $Description",
        "mode: subagent",
        "metadata:",
        "  source: $Source"
      )
      if (-not [string]::IsNullOrWhiteSpace($Version)) { $lines += "  version: $Version" }
      if (-not [string]::IsNullOrWhiteSpace($Owner)) { $lines += "  owner: $Owner" }
      return $lines + @("---", "")
    }
    "agents" {
      $lines = @(
        "---",
        "name: $Id",
        "description: $Description",
        "metadata:",
        "  source: $Source"
      )
      if (-not [string]::IsNullOrWhiteSpace($Version)) { $lines += "  version: $Version" }
      if (-not [string]::IsNullOrWhiteSpace($Owner)) { $lines += "  owner: $Owner" }
      return $lines + @("---", "")
    }
    default { Write-Error "Unsupported agent provider: $Provider" }
  }
}

function Generate-SkillProjections {
  $registryPath = Join-Path $repoRoot ".ai/registry/skills"
  if (-not (Test-Path $registryPath)) {
    Write-Error "Canonical skills directory not found: .ai/registry/skills"
  }

  $targets = @(
    @{ Provider = "github"; Path = ".github/skills" },
    @{ Provider = "opencode"; Path = ".opencode/skills" },
    @{ Provider = "claude"; Path = ".claude/skills" },
    @{ Provider = "agents"; Path = ".agents/skills" }
  )

  foreach ($target in $targets) {
    $fullPath = Join-Path $repoRoot $target.Path
    if (Test-Path $fullPath) { Remove-Item -Recurse -Force $fullPath }
    New-Item -ItemType Directory -Path $fullPath | Out-Null
  }

  $canonicalFiles = Get-ChildItem -Path $registryPath -Filter "*.yaml" -File
  if (-not $canonicalFiles -or $canonicalFiles.Count -eq 0) {
    Write-Error "No canonical skills found in .ai/registry/skills"
  }

  foreach ($file in $canonicalFiles) {
    $lines = Get-Content $file.FullName
    $id = Get-YamlScalar -Lines $lines -Key "id"
    Assert-CanonicalId -Id $id -Path $file.FullName -TypeName "Skill"

    $title = Get-YamlScalar -Lines $lines -Key "title"
    if ([string]::IsNullOrWhiteSpace($title)) { $title = $id }
    $version = Get-YamlScalar -Lines $lines -Key "version"
    $owner = Get-YamlScalar -Lines $lines -Key "owner"
    $purpose = Get-YamlScalar -Lines $lines -Key "purpose"
    $description = Convert-TextToDescription -Text $purpose -Fallback "Reusable project skill."

    $scopeInclude = Get-YamlList -Lines $lines -Key "include"
    $scopeExclude = Get-YamlList -Lines $lines -Key "exclude"
    $inputs = Get-YamlList -Lines $lines -Key "inputs"
    $outputs = Get-YamlList -Lines $lines -Key "outputs"
    $constraints = Get-YamlList -Lines $lines -Key "constraints"
    $allowedTools = Get-YamlList -Lines $lines -Key "allowed_tools"
    $workflowSteps = Get-YamlList -Lines $lines -Key "workflow_steps"
    $acceptanceCriteria = Get-YamlList -Lines $lines -Key "acceptance_criteria"
    $examples = Get-YamlList -Lines $lines -Key "examples"

    $bodyLines = Build-CanonicalBody `
      -Title $title `
      -Purpose $purpose `
      -ScopeInclude $scopeInclude `
      -ScopeExclude $scopeExclude `
      -Inputs $inputs `
      -Outputs $outputs `
      -Constraints $constraints `
      -AllowedTools $allowedTools `
      -WorkflowSteps $workflowSteps `
      -AcceptanceCriteria $acceptanceCriteria `
      -Examples $examples

    foreach ($target in $targets) {
      $frontmatter = New-SkillFrontmatter `
        -Provider $target.Provider `
        -Id $id `
        -Description $description `
        -Source ".ai/registry/skills/$($file.Name)" `
        -Version $version `
        -Owner $owner `
        -AllowedTools $allowedTools

      $skillText = ($frontmatter + $bodyLines) -join "`n"
      $skillDir = Join-Path $repoRoot (Join-Path $target.Path $id)
      New-Item -ItemType Directory -Path $skillDir -Force | Out-Null
      Set-Content -Path (Join-Path $skillDir "SKILL.md") -Value $skillText -NoNewline
    }
  }

  Write-Host "Skill projections generated successfully."
}

function Generate-AgentProjections {
  $registryPath = Join-Path $repoRoot ".ai/registry/agents"
  if (-not (Test-Path $registryPath)) {
    Write-Error "Canonical agents directory not found: .ai/registry/agents"
  }

  $targets = @(
    @{ Provider = "github"; Path = ".github/agents"; Extension = ".agent.md" },
    @{ Provider = "opencode"; Path = ".opencode/agents"; Extension = ".md" },
    @{ Provider = "agents"; Path = ".agents/agents"; Extension = ".md" }
  )

  foreach ($target in $targets) {
    $fullPath = Join-Path $repoRoot $target.Path
    if (Test-Path $fullPath) { Remove-Item -Recurse -Force $fullPath }
    New-Item -ItemType Directory -Path $fullPath | Out-Null
  }

  $canonicalFiles = Get-ChildItem -Path $registryPath -Filter "*.yaml" -File
  if (-not $canonicalFiles -or $canonicalFiles.Count -eq 0) {
    Write-Error "No canonical agents found in .ai/registry/agents"
  }

  foreach ($file in $canonicalFiles) {
    $lines = Get-Content $file.FullName
    $id = Get-YamlScalar -Lines $lines -Key "id"
    Assert-CanonicalId -Id $id -Path $file.FullName -TypeName "Agent"

    $title = Get-YamlScalar -Lines $lines -Key "title"
    if ([string]::IsNullOrWhiteSpace($title)) { $title = $id }
    $version = Get-YamlScalar -Lines $lines -Key "version"
    $owner = Get-YamlScalar -Lines $lines -Key "owner"
    $purpose = Get-YamlScalar -Lines $lines -Key "purpose"
    $description = Convert-TextToDescription -Text $purpose -Fallback "Reusable project agent."

    $scopeInclude = Get-YamlList -Lines $lines -Key "include"
    $scopeExclude = Get-YamlList -Lines $lines -Key "exclude"
    $inputs = Get-YamlList -Lines $lines -Key "inputs"
    $outputs = Get-YamlList -Lines $lines -Key "outputs"
    $constraints = Get-YamlList -Lines $lines -Key "constraints"
    $allowedTools = Get-YamlList -Lines $lines -Key "allowed_tools"
    $workflowSteps = Get-YamlList -Lines $lines -Key "workflow_steps"
    $acceptanceCriteria = Get-YamlList -Lines $lines -Key "acceptance_criteria"

    $bodyLines = Build-CanonicalBody `
      -Title $title `
      -Purpose $purpose `
      -ScopeInclude $scopeInclude `
      -ScopeExclude $scopeExclude `
      -Inputs $inputs `
      -Outputs $outputs `
      -Constraints $constraints `
      -AllowedTools $allowedTools `
      -WorkflowSteps $workflowSteps `
      -AcceptanceCriteria $acceptanceCriteria `
      -Examples @()

    foreach ($target in $targets) {
      $frontmatter = New-AgentFrontmatter `
        -Provider $target.Provider `
        -Id $id `
        -Description $description `
        -Source ".ai/registry/agents/$($file.Name)" `
        -Version $version `
        -Owner $owner `
        -AllowedTools $allowedTools

      $agentText = ($frontmatter + $bodyLines) -join "`n"
      $targetPath = Join-Path (Join-Path $repoRoot $target.Path) ($id + $target.Extension)
      Set-Content -Path $targetPath -Value $agentText -NoNewline
    }
  }

  Write-Host "Agent projections generated successfully."
}

switch ($Only) {
  "skills" { Generate-SkillProjections }
  "agents" { Generate-AgentProjections }
  default {
    Generate-SkillProjections
    Generate-AgentProjections
  }
}
