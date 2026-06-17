# Feature 146 Browser Feasibility

Browser feasibility is implemented in `Rendering.Harness.RenderAnywhere` as an MVU-style evidence
workflow.

The current candidate backend is `canvaskit-command-stream/proof`. The report evaluates the
Feature 146 corpus against available reference evidence and records an environment-limited
comparison when browser execution is not configured in the current host. The final decision is a
documented fallback path: continue with a generated CanvasKit command-stream proof and do not claim
a production browser backend yet.

Acceptance criteria for a future accepted browser candidate:

- at least three representative scenes compared against passed reference evidence
- explicit tolerance and diff metric for each scene
- unsupported capability/resource summaries by scene
- final decision set to `AcceptedCandidatePath`

Validation commands:

```bash
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature146
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- render-anywhere-browser-feasibility --out specs/146-render-anywhere-protocol/readiness/browser
```
