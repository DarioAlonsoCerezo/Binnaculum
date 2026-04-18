# Workflows

## Feature Workflow

1. Locate impacted domain and platform files.
2. Implement changes in the correct layer.
3. Add or update tests.
4. Build and test affected targets.
5. Verify no style or performance regressions.

## Bugfix Workflow

1. Reproduce the issue with a focused test or scenario.
2. Implement minimal fix in the correct layer.
3. Add regression coverage.
4. Run targeted and safety tests.
5. Document behavior change in pull request description.

## Database Change Workflow

1. Update models and queries in core database paths.
2. Validate migrations or compatibility behavior.
3. Add integration-oriented test coverage.
4. Validate with realistic dataset assumptions.

## Performance-Sensitive Workflow

1. Identify hot paths and allocation-heavy logic.
2. Apply chunking or async patterns as needed.
3. Run relevant performance tests.
4. Confirm no regressions before merge.

## Manageability Refactor Workflow

1. Identify oversized or over-complex files in the target scope.
2. Classify findings as generated/resource (excluded) or actionable code.
3. Create an incremental refactor plan grouped into small, reviewable units.
4. Refactor only the highest-priority unit first and keep behavior unchanged.
5. Rebuild and run targeted tests for touched areas.
6. Record remaining debt with clear next steps when full reduction is not completed.

## Manageability Planning Workflow

1. Start with changed files, then schedule legacy hotspots in ranked order.
2. Prefer extraction by responsibility (parsing, validation, orchestration, mapping, UI composition).
3. Split large methods before splitting files when both are over threshold.
4. For XAML, extract repeated UI blocks into reusable controls or templates.
5. Keep refactors cross-layer safe; avoid mixing Core domain logic into UI code-behind.

## Pull Request Checklist

- Build success.
- Tests pass.
- Rules in `engineering-rules.md` are satisfied.
- Scope is contained and reviewed.
- Manageability impact is documented for touched files that exceed advisory thresholds.
