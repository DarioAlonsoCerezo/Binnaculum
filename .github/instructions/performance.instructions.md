---
applyTo: "src/Core/Snapshots/**/*.fs"
---

# Performance Guidelines for Binnaculum

When working on financial calculations and snapshot management, follow these performance patterns:

## Mobile Targets Considerations
- **Memory Constraints**: Mobile devices have limited RAM; use sequence operations for large datasets
- **CPU Constraints**: Avoid blocking operations; use F# async workflows
- **GC Pressure**: Monitor garbage collection impact in performance tests
- **Battery Impact**: Efficient calculations preserve battery life

## Chunked Processing Patterns
Follow established patterns for large dataset handling:
- Process portfolios in chunks (see `BrokerFinancialSnapshotManager`)
- Use sequence operations (`Seq.map`, `Seq.filter`) instead of lists for memory efficiency
- Implement progress reporting for long-running calculations
- Test with realistic portfolio sizes (1000+ holdings)

## Concurrent Operations
- Use F# async workflows for concurrent calculations
- Avoid blocking operations in financial calculations
- Implement proper cancellation token support
- Follow existing async patterns in snapshot managers

## Performance Testing Requirements
All performance-sensitive code must include benchmarks:
- Target: Mobile device constraints (limited CPU/memory)
- Include tests for memory pressure scenarios
- Test concurrent processing patterns
- Validate GC behavior under load
- Reference: `BrokerFinancialSnapshotManagerPerformanceTests.fs`

## Performance Validation
Before submitting changes:
1. Run BrokerFinancialSnapshotManager performance tests
2. Verify no new performance regressions
3. Check memory allocation patterns
4. Validate async/await patterns are correct
5. Test with large portfolios (mobile simulation)

## Common Performance Anti-Patterns to Avoid
- Using `List.map` on large sequences (prefer `Seq.map`)
- Blocking operations without async/await
- Creating unnecessary intermediate collections
- Forgetting to dispose resources in long-running operations
- Ignoring GC pressure in tight loops
