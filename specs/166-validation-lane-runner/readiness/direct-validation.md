# Direct Validation Preservation

Direct commands remain runnable outside the lane runner.

```text
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release --no-restore -v minimal
Passed! - Failed: 0, Passed: 181, Skipped: 0, Total: 181
```

```text
dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --no-restore -v minimal
Passed! - Failed: 0, Passed: 80, Skipped: 0, Total: 80
```

```text
dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --no-restore
```

The Controls command started, emitted the typed-controls skipped-test line, then
remained quiet for several minutes. It was manually canceled after reproducing
the same no-progress condition reported by the required lane run. This preserves
the direct workflow while documenting why the lane runner blocks readiness.
