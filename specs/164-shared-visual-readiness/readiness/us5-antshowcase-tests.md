# US5 AntShowcase Tests

Command:

```sh
dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --no-restore --filter "Visual"
```

Result: passed, 25 tests.

Covered:

- Preferred visual readiness matrix exposes 38 shared targets.
- Minimum visual readiness matrix exposes 12 shared targets.
- AntShowcase workflow keeps shared-compatible target ids and relative paths.
- Reviewer gate remains blocking until classifications are complete.
- Generated summary JSON embeds the shared visual-readiness report.
