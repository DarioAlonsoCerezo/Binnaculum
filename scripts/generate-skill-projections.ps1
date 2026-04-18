$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$registryPath = Join-Path $repoRoot ".ai/registry/skills"

if (-not (Test-Path $registryPath)) {
  Write-Error "Canonical skills directory not found: .ai/registry/skills"
}

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
    if ($value -eq "" ) { return "" }

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
        if ($currentIndent -le $baseIndent) {
          break
        }

        $trimmed = $line.Substring([Math]::Min($baseIndent + 2, $line.Length))
        $block += $trimmed
      }

      return (($block -join " ") -replace "\s+", " ").Trim()
    }

    if ($value -match '^[''\"](.*)[''\"]$') {
      return $matches[1]
    }

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
        if ($value -match '^[''\"](.*)[''\"]$') {
          $value = $matches[1]
        }
        $result += $value
      }
    }

    return $result
  }

  return @()
}

function Convert-TextToDescription {
  param(
    [string]$Text
  )

  $normalized = ($Text -replace "\s+", " ").Trim()
  if ([string]::IsNullOrWhiteSpace($normalized)) {
    $normalized = "Reusable project skill."
  }

  if ($normalized.Length -gt 200) {
    return $normalized.Substring(0, 197) + "..."
  }

  return $normalized
}

function New-FrontmatterForProvider {
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
      # Copilot-compatible frontmatter.
      $lines = @(
        "---",
        "name: $Id",
        "description: $Description"
      )

      if ($AllowedTools.Count -gt 0) {
        $lines += "allowed-tools:"
        foreach ($tool in $AllowedTools) {
          $lines += "  - $tool"
        }
      }

      return $lines + @("---", "")
    }

    "opencode" {
      # OpenCode recognizes: name, description, license, compatibility, metadata.
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
      # Keep Claude frontmatter minimal for maximum compatibility.
      return @(
        "---",
        "name: $Id",
        "description: $Description",
        "---",
        ""
      )
    }

    "agents" {
      # Generic Agent Skills projection with portable fields only.
      return @(
        "---",
        "name: $Id",
        "description: $Description",
        "---",
        ""
      )
    }

    default {
      Write-Error "Unsupported provider projection: $Provider"
    }
  }
}

$projectionRoots = @(
  @{ Provider = "github"; Path = ".github/skills" },
  @{ Provider = "opencode"; Path = ".opencode/skills" },
  @{ Provider = "claude"; Path = ".claude/skills" },
  @{ Provider = "agents"; Path = ".agents/skills" }
)

foreach ($projection in $projectionRoots) {
  $fullPath = Join-Path $repoRoot $projection.Path
  if (Test-Path $fullPath) {
    Remove-Item -Recurse -Force $fullPath
  }
  New-Item -ItemType Directory -Path $fullPath | Out-Null
}

$canonicalSkillFiles = Get-ChildItem -Path $registryPath -Filter "*.yaml" -File
if (-not $canonicalSkillFiles -or $canonicalSkillFiles.Count -eq 0) {
  Write-Error "No canonical skills found in .ai/registry/skills"
}

foreach ($skillFile in $canonicalSkillFiles) {
  $lines = Get-Content $skillFile.FullName

  $id = Get-YamlScalar -Lines $lines -Key "id"
  if ([string]::IsNullOrWhiteSpace($id)) {
    Write-Error "Missing required 'id' in $($skillFile.FullName)"
  }

  $title = Get-YamlScalar -Lines $lines -Key "title"
  if ([string]::IsNullOrWhiteSpace($title)) { $title = $id }

  $version = Get-YamlScalar -Lines $lines -Key "version"
  $owner = Get-YamlScalar -Lines $lines -Key "owner"
  $purpose = Get-YamlScalar -Lines $lines -Key "purpose"
  $description = Convert-TextToDescription -Text $purpose

  $scopeInclude = Get-YamlList -Lines $lines -Key "include"
  $scopeExclude = Get-YamlList -Lines $lines -Key "exclude"
  $inputs = Get-YamlList -Lines $lines -Key "inputs"
  $outputs = Get-YamlList -Lines $lines -Key "outputs"
  $constraints = Get-YamlList -Lines $lines -Key "constraints"
  $allowedTools = Get-YamlList -Lines $lines -Key "allowed_tools"
  $workflowSteps = Get-YamlList -Lines $lines -Key "workflow_steps"
  $acceptanceCriteria = Get-YamlList -Lines $lines -Key "acceptance_criteria"
  $examples = Get-YamlList -Lines $lines -Key "examples"

  $bodyLines = @("# $title", "")

  if (-not [string]::IsNullOrWhiteSpace($purpose)) {
    $bodyLines += @("## Purpose", $purpose, "")
  }

  if ($scopeInclude.Count -gt 0 -or $scopeExclude.Count -gt 0) {
    $bodyLines += @("## Scope")
    if ($scopeInclude.Count -gt 0) {
      $bodyLines += ""
      $bodyLines += "### Include"
      foreach ($item in $scopeInclude) { $bodyLines += "- $item" }
    }
    if ($scopeExclude.Count -gt 0) {
      $bodyLines += ""
      $bodyLines += "### Exclude"
      foreach ($item in $scopeExclude) { $bodyLines += "- $item" }
    }
    $bodyLines += ""
  }

  if ($inputs.Count -gt 0) {
    $bodyLines += @("## Inputs")
    foreach ($item in $inputs) { $bodyLines += "- $item" }
    $bodyLines += ""
  }

  if ($outputs.Count -gt 0) {
    $bodyLines += @("## Outputs")
    foreach ($item in $outputs) { $bodyLines += "- $item" }
    $bodyLines += ""
  }

  if ($constraints.Count -gt 0) {
    $bodyLines += @("## Constraints")
    foreach ($item in $constraints) { $bodyLines += "- $item" }
    $bodyLines += ""
  }

  if ($allowedTools.Count -gt 0) {
    $bodyLines += @("## Allowed Tools")
    foreach ($item in $allowedTools) { $bodyLines += "- $item" }
    $bodyLines += ""
  }

  if ($workflowSteps.Count -gt 0) {
    $bodyLines += @("## Workflow")
    foreach ($item in $workflowSteps) { $bodyLines += "- $item" }
    $bodyLines += ""
  }

  if ($acceptanceCriteria.Count -gt 0) {
    $bodyLines += @("## Acceptance Criteria")
    foreach ($item in $acceptanceCriteria) { $bodyLines += "- $item" }
    $bodyLines += ""
  }

  if ($examples.Count -gt 0) {
    $bodyLines += @("## Examples")
    foreach ($item in $examples) { $bodyLines += "- $item" }
    $bodyLines += ""
  }

  foreach ($projection in $projectionRoots) {
    $frontmatterLines = New-FrontmatterForProvider `
      -Provider $projection.Provider `
      -Id $id `
      -Description $description `
      -Source ".ai/registry/skills/$($skillFile.Name)" `
      -Version $version `
      -Owner $owner `
      -AllowedTools $allowedTools

    $skillText = ($frontmatterLines + $bodyLines) -join "`n"

    $skillDir = Join-Path $repoRoot (Join-Path $projection.Path $id)
    New-Item -ItemType Directory -Path $skillDir -Force | Out-Null
    $target = Join-Path $skillDir "SKILL.md"
    Set-Content -Path $target -Value $skillText -NoNewline
  }
}

Write-Host "Skill projections generated successfully."
