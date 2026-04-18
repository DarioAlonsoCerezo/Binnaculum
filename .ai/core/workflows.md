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

## Pull Request Checklist

- Build success.
- Tests pass.
- Rules in `engineering-rules.md` are satisfied.
- Scope is contained and reviewed.
