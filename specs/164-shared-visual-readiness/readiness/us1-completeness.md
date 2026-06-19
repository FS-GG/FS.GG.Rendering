# US1 Completeness Validation

Command:

```sh
dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature164
```

Result: passed, 8 tests.

Covered:

- 3 pages x 2 themes x 2 sizes expands to 12 deterministic targets.
- Duplicate page ids, duplicate relative paths, and escaping paths are rejected.
- Synthetic PNG fixtures classify complete, missing, wrong-size, corrupt, and zero-byte artifacts.
- Degraded captures require non-empty reasons.
- Stale artifacts outside the target matrix are diagnosed.
