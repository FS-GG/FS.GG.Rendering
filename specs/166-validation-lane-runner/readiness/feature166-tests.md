# Feature 166 Tests

Command:

```sh
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release --no-restore --filter Feature166 -v minimal
```

Result:

```text
Passed! - Failed: 0, Passed: 18, Skipped: 0, Total: 18, Duration: 555 ms
```

Full harness regression:

```sh
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release --no-restore -v minimal
```

Result:

```text
Passed! - Failed: 0, Passed: 181, Skipped: 0, Total: 181, Duration: 570 ms
```
