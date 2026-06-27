# Quickstart / Verification Result (T020/T021) — feed V = 0.1.48-preview.1

End-to-end generate → restore → build → evidence → governance, per profile, against the freshly
packed coherent local feed. Full per-profile logs under `readiness/<profile>/` (`smoke.log`,
`governance.log`).

## Per-profile matrix (all green)

| Profile | Generate | Restore | Build (-c Release) | scene-evidence | layout-evidence | launch-evidence | image-evidence | GovernanceTests |
|---|---|---|---|---|---|---|---|---|
| headless-scene | ✅ | ✅ | ✅ 0 warn / 0 err | ✅ ok (deterministic) | ✅ ok, ReadableLayout, NoLayoutOverlap | n/a | n/a | ✅ 3 passed |
| governed       | ✅ | ✅ | ✅ 0 / 0 | ✅ ok | ✅ ok, ReadableLayout | n/a | n/a | ✅ 4 passed |
| app            | ✅ | ✅ | ✅ 0 / 0 | ✅ ok | ✅ ok, accepted=true | ✅ ok (persistent-evidence, first-frame-presented=true) | ✅ ok, image-decodable=true | ✅ 29 passed |
| sample-pack    | ✅ | ✅ | ✅ 0 / 0 | ✅ ok | ✅ ok, accepted=true | ✅ ok (persistent-evidence) | ✅ ok, image-decodable=true | ✅ 28 passed |

SC-001 ✅ (all 4 generate+restore+build) · SC-006 ✅ (each emits expected scene/evidence) ·
SC-005 ✅ (GovernanceTests green for every profile).

## Version invariants (Step 5)

- `<FsSkiaUiVersion>` = `0.1.48-preview.1` (== produced feed version V) — **C3 ✅ / FR-003 ✅**
- exactly **1** `<FsSkiaUiVersion>` literal; **0** literal-version FS.GG.UI pins — **C2 ✅ / FR-004 ✅**
- no superseded `0.1.0-preview.1` literal anywhere in `template/base/` — **C4 ✅ / FR-006 ✅ / SC-003 ✅**

## Bundled Scene reference (Step 6)

- Every type AND member in `template/base/docs/api-surface/Scene/Scene.fsi` resolves in the live
  `src/Scene/*.fsi` (type-name + record-field + DU-case + val comparison). The bundled doc is a curated,
  non-contradicting subset of the live surface (it omits newer peripheral modules — VisualInspection /
  RetainedInspection / SceneCodec / Animation — which C5 permits). No construct is presented as current
  that is absent from live — **C5 ✅ / FR-005 ✅ / SC-004 ✅**. Verified no-op: no edit required.

## build.fsx target Verify — ENVIRONMENT-LIMITED (visible caveat, NOT reported green)

`dotnet fsi build.fsx target Verify` completes `Dev` + `GeneratedGuidanceCheck` + `TemplateDrift`, then
FAILS on the `EvidenceGraph`/`EvidenceAudit` targets with:

    System.Exception: FS.GG.UI.Build 0.1.48-preview.1 could not be restored ... Ensure the version
    exists on a configured feed.

Cause: the governance engine package `FS.GG.UI.Build` has **no producer project** in this repository and
is absent from the local feed and the global NuGet cache (so it cannot be packed at any version). This is
a **pre-existing infrastructure limitation**, independent of this feature's changes (the re-pin/seed/doc
edits do not touch the engine). The `Test` target (GovernanceTests) — the other half of the gate — passes
independently for all profiles (see matrix). Per evidence-honesty rules this is recorded as
environment-limited, not green. (Aside: generated `build.fsx:126` also still probes the pre-rebrand
`fs.skia.ui.build` cache path; out of scope for this feature — build.fsx is not a seed file, the pin, or
the bundled Scene doc, and FR-009 forbids unrelated edits.)
