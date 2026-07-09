# Feature 146 Browser Capability

Browser capability is implemented in `Rendering.Harness.RenderAnywhere` as an MVU-style evidence
workflow.

**This is a capability report, not a perceptual diff.** No candidate backend executes in this
harness, so no candidate image is produced and no image is ever compared to another image. The
report records, per corpus scene, whether passing reference evidence exists (`candidate-not-executed`)
or does not (`missing-reference`), and it carries the reference image identity it cites. The final
decision is a documented fallback path: continue with a generated CanvasKit command-stream proof and
do not claim a production browser backend yet.

Nothing in this directory is evidence of cross-backend visual fidelity. The types enforce that:
`CandidateCapabilityStatus` has no `passed` case and `BrowserFinalDecision` has no `accepted` case,
so a green report cannot be misread as backend agreement.

A future accepted browser candidate must add — as code, not as fields nothing writes:

- a candidate backend that actually renders each corpus package to an image
- a real perceptual diff between candidate and reference images, gated on an explicit tolerance
- per-scene unsupported capability/resource summaries derived from that run
- an accepted-candidate decision case, reintroduced alongside the comparison that justifies it

Validation commands:

```bash
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature146
dotnet run --project tools/Rendering.Harness/Rendering.Harness.fsproj -- render-anywhere-reference --out specs/146-render-anywhere-protocol/readiness/reference
dotnet run --project tools/Rendering.Harness/Rendering.Harness.fsproj -- render-anywhere-browser-feasibility --out specs/146-render-anywhere-protocol/readiness/browser
```

The reference command must run first: the capability report reads `readiness/reference/summary.md`
to learn which scenes have passing reference evidence.
