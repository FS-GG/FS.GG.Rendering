# Feature 167 Scheduler And Responsiveness Tests

Status: passed with environment-limited live evidence on 2026-06-19.

Focused commands:

```sh
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release --no-restore --filter Feature167
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj -c Release --no-restore --filter Feature167
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release --filter Feature167
dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --no-restore --filter "Interaction|Responsiveness|Feature167"
dotnet fsi scripts/run-validation-lanes.fsx --lane rendering-harness --out artifacts/validation-lanes
```

Results:

- SkiaViewer Feature167: 11 passed.
- Elmish Feature167: 6 passed.
- Rendering.Harness Feature167: 1 passed.
- AntShowcase interaction/responsiveness/Feature167: 10 passed.
- Rendering-harness validation lane wrote `artifacts/validation-lanes/validation-20260619-115533-7a7b32/summary.md`.

Representative responsiveness output:

- `responsiveness/resp-20260619-120611-0fcd49/records.jsonl`
- `responsiveness/resp-20260619-120611-0fcd49/summary.json`
- `responsiveness/resp-20260619-120611-0fcd49/summary.md`
- `responsiveness/resp-20260619-120611-0fcd49/environment.md`

Readiness is `environment-limited` because this run used deterministic/headless substitute
evidence without a live GL presentation boundary.
