---
name: dataflow-integrity
description: Define rules and conventions for data insertion paths through Creator (manual UI data) and ImportManager (file-based import) while preventing architectural violations by AI agents. Ensures all data...
allowed-tools:
  - cocoindex_search
  - grep
  - glob
  - read
  - apply_patch
  - bash
---

# Dataflow Integrity Skill

## Purpose
Define rules and conventions for data insertion paths through Creator (manual UI data) and ImportManager (file-based import) while preventing architectural violations by AI agents. Ensures all data flows through the two established paths, snapshots are triggered correctly, and no third-path data insertions are introduced.

## Inputs
- requested_change
- affected_modules
- data_type

## Outputs
- implementation_plan
- changed_files
- dataflow_compliance_report

## Constraints
- All data insertion into the database MUST flow exclusively through Creator (manual UI data) or ImportManager (file-based import). No third insertion paths exist and none shall be created.
- Any Core module that is not Creator or ImportManager MUST NOT contain code that inserts, updates, or deletes data in the database. Such modules may only read data through Extensions/Query Modules.
- Creator and ImportManager MUST NOT cross-responsibility boundaries: Creator never knows about ImportManager or its functions, and ImportManager never calls UI-layer or Creator functions.
- Each SaveXxx in Creator MUST trigger the correct synchronization triggers (SnapshotProcessingCoordinator.handleBrokerAccountChange() for broker data, SnapshotManager for bank data, ReactiveSnapshotManager.refresh() where applicable). No trigger omission allowed.
- Saver MUST NOT handle snapshot logic. Snapshots are exclusively managed by Creator for user-initiated actions. Saver only persists data and updates reactive collections.
- Saver responsibility is limited to persistence and reactive collection updates (ReactiveMovementManager, Collections.updateXxx). No business logic or snapshot handling shall be added to Saver.
- Default seed data (brokers, currencies, SPY) remains in startup loaders (src/Core/DataLoader/) and in *Extensions.fs as insertIfNotExists. Seed data MUST NOT be mixed with Creator or ImportManager.
- Each entity MUST use its *Extensions.fs for save(). save() MUST NOT be called directly from Creator or ImportManager without going through the extension.

## Allowed Tools
- cocoindex_search
- grep
- glob
- read
- apply_patch
- bash

## Workflow
- Detect the affected module and determine if it is in a data insertion path.
- If the module is Creator/Saver/ImportManager: verify that existing patterns are followed without mixing responsibilities.
- If the module is NOT Creator/ImportManager: verify that the modification does NOT introduce data insertions. If it introduces insertions, flag as a violation.
- Verify that each SaveXxx in Creator activates the correct synchronization triggers without omissions.
- Verify that there are no cross-responsibility links between Creator and ImportManager.
- Produce a compliance report or a correction plan.

## Acceptance Criteria
- All data insertion in Core is exclusively in Creator or ImportManager.
- No data insertions in third-party modules.
- No cross-responsibility links between Creator and ImportManager.
- Each SaveXxx in Creator triggers consistent snapshots and reactive refresh.
- Saver maintains its exclusive responsibility of persistence + collection updates.
- Seed data remains in startup loaders.

## Examples
- Add a new SaveXxx in Creator with the correct snapshots.
- Add a new movement type from ImportManager without mixing with Creator.
- Detect and correct an accidental data insertion in a Snapshots module that should only read data.
