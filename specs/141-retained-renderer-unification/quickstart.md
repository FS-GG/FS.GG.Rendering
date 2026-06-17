# Quickstart: Retained Renderer Unification Validation

## Prerequisites

- Work from repository root: `/home/developer/projects/FS.GG.Rendering`
- Use branch `141-retained-renderer-unification`
- Ensure `.specify/feature.json` points to `specs/141-retained-renderer-unification`
- Review [contracts/retained-renderer-unification.md](./contracts/retained-renderer-unification.md)

## Focused Feature 141 Validation

After implementation tasks add the focused tests, run:

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature141
```

Expected outcome:

- Direct, cold retained, and warm retained rendering are equivalent for focused fixtures.
- Retained warm frames report reuse evidence without owning scene composition semantics.
- Invalidation evidence covers visual, layout, modifier/layer, text proof, identity, and child-order changes.
- A failed or unsafe reuse path falls back to fresh assembly without exposing stale or partial output.
- Source guards identify exactly one assembly owner and no retained-only composition rule set.

## Existing Assembly and Composition Compatibility

Run the guards for the two prerequisite architecture phases:

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature139
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature140
```

Expected outcome:

- Feature 139 shared assembly extraction remains the current assembly route.
- Feature 140 modifier/layer/portal evidence remains shared with retained invalidation.
- Legacy lowering, local z-order, portal/layer, cache, overlay, and glyph-run proof compatibility stays intact.

## Retained, Cache, and Fingerprint Oracles

Run the retained and cache audit suites:

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature091
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature092
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Audit_Reconcile
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Audit_Fingerprint
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Audit_MemoCache
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Audit_PictureCache
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Audit_TextCache
```

Expected outcome:

- Retained identity, keyed reconciliation, and stale-state discard behavior remain deterministic.
- Structural fingerprints change for render-affecting input changes and remain stable for equivalent inputs.
- Memo, picture, and text cache-disabled variants remain equivalent to cache-enabled output.
- Warm retained idle frames avoid full repaint/remeasure work where existing oracles require it.

## Randomized Equivalence

Run the generated-tree coverage once it is added to Feature 141 tests:

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter RetainedRandomizedEquivalence
```

Expected outcome:

- At least 200 generated control trees or composition chains compare direct, cold retained, and warm retained
  output.
- Zero equivalence failures across scene, bounds, diagnostics, fingerprints, and cache transparency.
- Repeated equivalent retained frames produce stable fingerprints and reuse evidence across at least three runs.

## Layout, Scene, and Viewer Compatibility

Run broad deterministic compatibility suites:

```bash
dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Audit
dotnet test tests/Scene.Tests/Scene.Tests.fsproj --filter Feature140
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature140 -c Release
```

Expected outcome:

- Incremental layout remains equivalent to full layout for retained rendering scenarios.
- Scene glyph-run proof data and fingerprints remain compatible.
- SkiaViewer proof drawing and replay/cache behavior remain compatible, or GL/window-system limitations are
  recorded separately.

## Surface and Broad Preflight

Use direct `dotnet` commands as the current checkout's baseline validation. Historical `./fake.sh` wrapper
targets may be recorded if they exist locally, but this checkout has previously recorded wrapper absence.

```bash
dotnet build FS.GG.Rendering.slnx -c Debug --no-restore
dotnet test tests/Color.Tests/Color.Tests.fsproj --no-build
dotnet test tests/Scene.Tests/Scene.Tests.fsproj --no-build
dotnet test tests/Layout.Tests/Layout.Tests.fsproj --no-build
dotnet test tests/Input.Tests/Input.Tests.fsproj --no-build
dotnet test tests/KeyboardInput.Tests/KeyboardInput.Tests.fsproj --no-build
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --no-build
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --no-build
dotnet test tests/Testing.Tests/Testing.Tests.fsproj --no-build
dotnet test tests/Lib.Tests/Lib.Tests.fsproj --no-build
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-build
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --no-build
dotnet test tests/Smoke.Tests/Smoke.Tests.fsproj --no-build
dotnet fsi scripts/refresh-surface-baselines.fsx
```

Expected outcome:

- Build and deterministic tests complete with zero new failures attributable to Feature 141.
- Public surface check reports zero public contract changes, or every intentional change is documented with
  migration guidance and versioning rationale.
- Surface-baseline refresh has no unexpected diffs.

## Optional Pixel and Harness Evidence

When a suitable GL/window-system environment is available, run:

```bash
dotnet run --project tests/Rendering.Harness -- offscreen --json --out artifacts/feature141-harness
```

Expected outcome:

- Offscreen evidence passes for retained/direct compatibility scenarios.
- Any missing GL/presentation capability is recorded in readiness as an environment limitation with the
  harness output path and status.

## Evidence to Record During Implementation

Record final command results in readiness notes before implementation is considered ready:

- Focused Feature 141 suite result.
- Feature 139 and Feature 140 compatibility results.
- Retained/cache/fingerprint audit results.
- Randomized equivalence count and seed information.
- Public surface result and baseline diff status.
- Build and broad deterministic test status.
- SkiaViewer/Smoke/Rendering.Harness evidence or environment limitation.
- Any intentional pixel, diagnostic, metric, or public surface change with rationale.
- Confirmation that full text shaping, overlay interaction state, portable serialization, compositor work,
  intrinsic layout, and public retained renderer APIs stayed out of scope.

## Current Checkout Notes

The 2026-06-17 Feature 141 implementation recorded `Feature091` as a validation limitation because
`dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature091 --no-build` did not complete
within the local shell window and was interrupted. Focused Feature 141, Feature 092, Feature 139,
Feature 140, public-surface, and `Audit` Controls filters passed, and all non-Controls broad deterministic
test projects listed above passed.
