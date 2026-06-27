# Phase 1 Data Model: Fix the generated build.fsx governance-engine resolution

This feature is build-wiring + a new engine library, not a data-storage feature. The "entities" below
are the conceptual artifacts the engine and the generated build manipulate. Concrete `.fsi` types are
finalized during implementation against the contracts in `contracts/`.

## Entities

### 1. Governance engine (`FS.GG.UI.Build`)
- **What**: The new in-repo packable library that runs the EvidenceGraph/EvidenceAudit gates in-process.
- **Identity**: `PackageId = FS.GG.UI.Build`; `AssemblyName = FS.GG.UI.Build`; assembly
  `FS.GG.UI.Build.dll`; net10.0 lib.
- **Versioning**: `<Version>` inherited/overridden by the coherent `-p:Version=<V>` pack; resolves at
  the same value as every other `FS.GG.UI.*` package (no independent version).
- **Public surface**: namespace `FS.GG.UI.Build.Evidence`, type `GeneratedRunner` with static
  `run : target:string -> dir:string -> int`. Curated `.fsi` + surface-area baseline
  (`readiness/surface-baselines/FS.GG.UI.Build.txt`).
- **Rules**: in-process only; no external process; dependency-minimal; no dependency on any external
  governance platform.

### 2. Engine invocation contract (consumer ↔ engine)
- **What**: The reflected boundary `build.fsx` uses to call the engine. See
  `contracts/engine-invocation-contract.md`.
- **Fields**: target ∈ { `EvidenceGraph`, `EvidenceAudit` }; working directory (the generated product
  root); return = process-style exit code (0 = pass, non-0 = fail).
- **Invariant**: shape is fixed by `build.fsx` reflection + `GovernanceTests.fs` string scans; changing
  it requires editing both in lock-step.

### 3. Evidence node / EvidenceGraph
- **What**: One sensed `readiness/**` artifact plus its derived state, and the graph over them the
  EvidenceGraph target emits to `readiness/evidence-graph.md`.
- **Fields (per node)**: artifact path; evidence kind (layout / scene / launch / image / screenshot /
  pixel-readback / window-diagnostics / window-options / bounded-smoke); presence; token-contract
  satisfaction (per `template/base/docs/evidence-formats.md`); derived state (present-valid /
  present-invalid / absent / not-required-for-profile).
- **Profile sensitivity**: the required node set differs by profile (headless vs interactive); the
  engine grdaphs what exists and judges against what the run's profile is expected to produce.

### 4. Audit verdict / EvidenceAudit
- **What**: The pass/fail judgement the EvidenceAudit target emits to `readiness/evidence-audit.md`.
- **Fields**: `verdict` (required token — e.g. `verdict=PASS` / `verdict=FAIL`); the failing-class
  reason(s) when not pass; reference to the graph it audited.
- **Mapping to exit code**: pass → `run` returns 0; fail → non-0 with a diagnostic; unresolved engine
  (restore/load failure) → loud failure naming engine identity + feed/path searched (FR-005), never 0.

### 5. Single version pin (`FsSkiaUiVersion`)
- **What**: The one FS.GG.UI version value in `template/base/Directory.Packages.props` that governs
  every `FS.GG.UI.*` package **and** the engine.
- **Invariant (FR-004)**: exactly one literal; one edit + restore moves libraries and engine together.
  The engine introduces no second version value (it is `PackageVersion Include="FS.GG.UI.Build"
  Version="$(FsSkiaUiVersion)"`, already present).

### 6. Verification gate (`Verify`)
- **What**: The composite generated target: Dev + GeneratedGuidanceCheck + TemplateDrift +
  EvidenceGraph + EvidenceAudit + Test.
- **Behavioral change**: EvidenceGraph/EvidenceAudit move from "abort on restore failure" to "resolve
  the engine and run green" for profiles that include them; profiles without the gates keep passing
  without the engine (FR-008 / acceptance US1 #3).

## State transitions (engine `run`)

```
restore engine (build.fsx) ──fail──▶ loud failure: name engine id + feed/path (FR-005), exit≠0
        │ success
        ▼
load assembly + resolve closure (build.fsx AssemblyResolve)
        │
        ▼
GeneratedRunner.run target dir
        ├─ target=EvidenceGraph: sense readiness/** → build graph → write evidence-graph.md ─▶ exit 0/≠0
        └─ target=EvidenceAudit: read graph/sense → derive verdict → write evidence-audit.md ─▶ exit 0/≠0
                                                   (verdict=PASS ⇒ 0; verdict=FAIL ⇒ ≠0)
```

## Validation rules

- Engine MUST exist on the configured (coherent) feed at `$(FsSkiaUiVersion)` (FR-003).
- No `fs.skia.ui.build` / `FS.Skia.UI` package name or cache path remains in `build.fsx` (FR-002).
- Engine output files satisfy `evidence-formats.md` (notably `evidence-audit.md` ⇒ `verdict` token).
- Engine never reports success when unresolved or when a required, expected artifact is invalid (FR-005).
- Existing `GovernanceTests.fs` scans stay green (FR-007).
