# Contract: Post-split Module Topology, Ordering & Surface

This feature exposes its contract as (a) the F# compile-order graph, (b) the public-surface baseline,
and (c) the inspection/evidence artifact schema. All three are machine-verified.

## C1 — `Scene.fsproj` compile order (load-bearing)

F# file order *is* the dependency order. The `<ItemGroup>` MUST become:

```xml
<ItemGroup>
  <Compile Include="Types.fsi" />
  <Compile Include="Types.fs" />
  <Compile Include="TextShaping.fsi" />   <!-- if Scene shims call into Text.Shaping, it precedes Scene -->
  <Compile Include="TextShaping.fs" />
  <Compile Include="Scene.fsi" />
  <Compile Include="Scene.fs" />
  <Compile Include="Inspection.fsi" />
  <Compile Include="Inspection.fs" />
  <Compile Include="Evidence.fsi" />
  <Compile Include="Evidence.fs" />
  <Compile Include="SceneWire.fs" />
  <Compile Include="SceneCodec.fsi" />
  <Compile Include="SceneCodec.fs" />
  <Compile Include="Animation.fsi" />
  <Compile Include="Animation.fs" />
</ItemGroup>
```

- **Contract**: `Types.*` precede all. The exact relative order of `TextShaping.*` vs `Scene.*` is
  fixed empirically by the delegation direction (shown above assumes `module Scene` delegates *into*
  `Text.Shaping`, so shaping compiles first). `Inspection.*`/`Evidence.*` follow `Scene.*`.
- **Verification**: `dotnet build FS.GG.Rendering.slnx -c Release` succeeds with no FS0039
  (undefined) / back-edge / ordering error. (Edge Case "F# root-position back-edge".)

## C2 — Public surface baseline (drift gate)

- **Artifact**: `readiness/surface-baselines/FS.GG.UI.Scene.txt` (one CLR `FullName` per line).
- **Contract**: after the split, regenerate via `dotnet fsi scripts/refresh-surface-baselines.fsx`
  and diff against the committed baseline.
  - US1 (namespace-level `Types.fs`): **empty diff**.
  - US3 (module names preserved): **empty diff**.
  - US2: diff is whatever the shaping/measurer relocation produces — MUST be only the intended,
    reviewed shaping-name changes and **no incidental drift** (FR-007).
- **Version-bump gate**: bump `<Version>` in `Scene.fsproj` (currently `0.1.37-preview.1`) *iff* the
  reviewed diff is non-empty. Empty diff ⇒ no bump (FR-007/SC-006 satisfied by zero drift).
- **Verification**: `SurfaceAreaTests` (live gate) is green against the committed baseline.

## C3 — Inspection/evidence artifact schema (semantic-diff gate)

- **Contract**: emitted `VisualInspectionArtifact` / `RetainedInspectionArtifact` / `SceneEvidence` /
  `LayoutEvidence` values are **semantically equivalent** (parsed status / counts / headers /
  finding sets) to the captured baseline, EXCEPT the FR-006 dedup delta: findings sharing a
  `FindingId` (the field populated by `stableFindingId` — Scene.fs:1825) are collapsed to one within
  each inspection path. (`FindingId` and "`stableFindingId`" name the same key: the field holds the
  function's output; each path uses its own identity scope — the retained path's key includes
  `transitionId`, the visual path's does not.)
- **Dedup invariants** (FR-005/FR-009, US3 acceptance #2/#3):
  - Duplicate findings (equal `FindingId`) collapse to a single finding.
  - Every **unique** finding is preserved; ordering is not changed in a meaning-altering way.
  - No fail-loud diagnostic is weakened or silenced; a degenerate scene still surfaces its finding.
  - The dedup *collapse* rule is identical on the visual and retained paths — dedupe by `FindingId`,
    keep first, preserve unique findings (SC-003). The identity-*key* function may differ per path
    (the retained key includes `transitionId`); each path collapses within its own identity scope.
- **Verification**: visual/retained-inspection + scene/layout-evidence suites; semantic-artifact diff
  vs baseline corpus; the dedup delta matches an explicitly reviewed-and-approved expected-output
  record (SC-007).

## C4 — Byte-equivalence of shaping output

- **Contract**: for the existing text corpus, `buildGlyphRun` / `buildFallbackShapedText` /
  `glyphRunDataFromShapedText` results, their fingerprints, and `measureText`/`measureTextResolved`/
  `setRealTextMeasurer` lifecycle are **byte-identical** before vs after US2 (the unification is a
  refactor of shared logic, not a behavior change).
- **Verification**: `Feature140GlyphRunTests`, `Feature142*` (shaped-text determinism / itemization /
  pure-fallback compatibility), `Feature136MeasurementSeamTests` — all green, byte-identical.
