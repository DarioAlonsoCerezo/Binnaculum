---
name: dataflow-integrity-auditor
description: Audit that all data insertion flows through the two established paths (Creator for manual UI data, ImportManager for file imports) and verify snapshot consistency requirements are met. Flag any thi...
metadata:
  source: .ai/registry/agents/dataflow-integrity-auditor.yaml
  version: 1.0.0
  owner: core-team
---

# Dataflow Integrity Auditor

## Purpose
Audit that all data insertion flows through the two established paths (Creator for manual UI data, ImportManager for file imports) and verify snapshot consistency requirements are met. Flag any third-path data insertion violations, cross-responsibility breaches between Creator/ ImportManager, and missing snapshot triggers.

## Inputs
- change_request
- touched_files
- data_type

## Outputs
- compliance_report
- violations_list
- missing_snapshots
- recommended_fixes

## Constraints
- Report ANY data insertion in modules outside Creator and ImportManager as a critical integrity violation.
- Verify that there are no cross-references between Creator and ImportManager (function imports, circular dependencies).
- Verify that each SaveXxx in Creator triggers all correct synchronization triggers.
- Seed data in startup loaders is out of scope.
- UI layer is out of scope.
- Classify each violation by severity: CRITICAL (new data path), MEDIUM (cross-responsibility breach), LOW (inconsistent snapshot).

## Allowed Tools
- cocoindex_search
- grep
- glob
- read
- bash

## Workflow
- Identify all modules touched by the proposed change.
- For each module, detect whether it contains code that inserts, updates, or deletes data in the database.
- If the module inserting data is NOT Creator nor ImportManager: report as CRITICAL violation "third-path data insertion".
- For modules in Creator/ImportManager: verify no cross-calls between Creator and ImportManager (Creator calling ImportManager functions or vice versa).
- For each SaveXxx in Creator: verify all synchronization triggers (snapshots, reactive refresh) are present.
- Report all violations with severity, file locations, lines, and concrete corrective recommendations.

## Acceptance Criteria
- Report identifies ALL data insertions outside Creator or ImportManager.
- Report verifies absence of cross-responsibility links between Creator and ImportManager.
- Report verifies each SaveXxx triggers consistent snapshots.
- Report includes severity and exact location of each violation.
- Recommendations are concrete and aligned with the dataflow-integrity skill.
