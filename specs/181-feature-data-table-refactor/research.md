# Phase 0 Research — Per-Feature Data-Table Refactor

**Feature**: 181 | **Date**: 2026-06-21 | **Status**: complete (resolves all NEEDS CLARIFICATION)

Method: a thorough read of the harness at HEAD (`tools/Rendering.Harness/`), `tests/Package.Tests/`,
the shared abstractions from prior phases, and the project's baseline/golden mechanism, cross-checked
against the Phase-4 entry in the code-health plan and the Phase-3 (180) outcome.

---

## R1 — Where does the harness live, and what is its current shape?

**Decision**: The unit of change is `tools/Rendering.Harness/` (relocated out of `tests/` in Phase 2).

- `Rendering.Harness.fsproj`: `OutputType=Exe`, references **6** `src/` projects (SkiaViewer,
  Controls.Elmish, Testing, Diagnostics, Scene, Controls). Include order is `Domain.fs … Compositor.(fsi/fs)
  … Cli.fs` (Cli last). It is itself tested by `tests/Rendering.Harness.Tests/`.
- `Compositor.fs` ≈ 5,667 lines: **97 `let renderFeature…` functions**, per-feature payload type defs,
  and **262 per-feature constants**. The constant pattern per feature is a quintet+ of
  `Path.Combine`-derived directory/path strings, e.g. `feature148ReadinessDirectory`,
  `feature148ParityDirectory`, `feature148TimingDirectory`, `feature148CompatibilityLedgerPath`,
  `feature148ValidationSummaryPath`, plus `feature148Id` and feature-specific `PolicyId`/`AcceptedProfileId`.
- `Cli.fs` ≈ 4,004 lines: **12 `isFeature###` predicates** (each matching `"148"`/`"feature148"`/the id
  string) and `if/elif` dispatch chains (e.g. `runCompositorReadinessCmd` chains `isFeature161 → … →
  isFeature156 → runLegacyCompositorReadinessCmd`); per-feature handlers up to ~400 lines.

**Rationale**: This grounds FR-001…FR-006 in the real tree and confirms the original plan's "97
renderers / 262 constants" counts are still current at HEAD (the spec's authoritative-scope assumption).

---

## R2 — Which features exist, and is variant coverage uniform?

**Decision**: The catalog has **12 entries: 148, 149, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161**
— **non-contiguous** (150/151 absent). Variant coverage is **non-uniform**, so the descriptor MUST carry
a per-feature *set* of supported variants, not a fixed shape.

Observed coverage (the descriptor's `Variants` set encodes this):

| Variant | Features that have it |
|---|---|
| `ValidationSummary` | **all 12** (universal) |
| `CompatibilityLedger` | **all 12** (universal) |
| `PackageValidation`, `RegressionValidation` | 153–161 |
| `LiveProof` | 148, 149, 152, 153, 154, 155 |
| `Parity` | 148, 149, 152, 154, 155, 157 |
| `Reuse`, `Snapshot` | 148, 149 |
| `ProofSet` | 153, 154, 155 |
| `Timing` | 148, 149, 152, 154, 155, 156, 157, 158, 160 |
| `UnsupportedHost` | 156–161 |
| feature-unique (e.g. `CounterReport`/`PromotionSummary` (159), `IterationReport`/`Throughput` (160), `LaneLedger`/`LedgerEntry` (161), `ProofProbe`/`ExcludedSamples` (158)) | single feature each |

**Rationale**: Directly satisfies the spec's Edge Case "some features have report variants others lack…
the descriptor must express which variants a feature has." A fixed record shape would be wrong; a
`Set<ReportVariant>` (or variant→payload map) is required. Feature-unique variants are the prime FR-007
retention candidates (collapsing a one-instance body into a generic path saves nothing).

---

## R3 — How much do same-named variant bodies actually diverge? (the SC-005 question)

**Decision**: Same-named variants across features share a **markdown skeleton** but diverge heavily in
**content and payload type**. Therefore: collapse the *skeleton/scaffolding* and the *constant
derivation* (real, safe line wins); treat the *divergent content* (evidence-link sets, reviewer-checklist
lines, decision text) as **descriptor data**, and **measure** whether moving it into data actually
reduces lines per family before committing to the collapse.

Evidence (`renderFeature158ValidationSummary` vs `renderFeature159ValidationSummary`): identical section
ordering (`# … Readiness Summary` → status line → `## Evidence Links` → `## Reviewer Checklist` →
`## Decision`) but **different evidence-link lists** (`timing/…`, `proof-probes/…` vs `promotion/…`,
`counters/…`), **different checklist bullets**, **different decision sentences**, and **different payload
types** (`Feature158TimingSummary` vs `Feature159Summary`). The `CompatibilityLedger` bodies are similar:
shared four-heading skeleton, fully feature-specific prose.

**Rationale**: This is the exact Phase-3 (180) SC-005 trap — generic config records there *added* ~170
net lines because the bodies diverged in detail. Two outcomes follow:
- The **universal skeleton** (section headers, the status-line format, the `Path.Combine` directory
  derivation, the "exists + write" plumbing) is genuinely shared and collapses with a real net win.
- The **per-feature prose** (link lists, checklist, decision) is data. Whether expressing it as
  descriptor data is shorter than the inline `String.concat [...]` list is a **per-family measurement**:
  if a family's link/checklist/decision data is as long as the body it replaces, leave the body explicit
  (FR-007) and record it. SC-005 is gated on the measured net delta, not assumed.

**Alternatives considered**:
- *Force every variant through one generic renderer with a giant data payload.* Rejected — reproduces the
  180 outcome (more lines, more indirection) for the divergent families, and risks output drift.
- *Template-string engine with placeholder substitution.* Rejected — heavier machinery than `String.concat`,
  harder to keep byte-identical, and Constitution III disfavors clever abstractions without payoff.

---

## R4 — What shared abstractions already exist (don't rebuild them)?

**Decision**: Reuse, don't recreate:
- **`FS.GG.TestSupport.RepositoryRoot`** (`tests/TestSupport/`, Phase 1) — `value`/`find`. The new
  data-driven `Package.Tests` list uses it for repo-relative paths (the per-feature files already do).
- **`Diagnostics.ReadinessStatus`** (`src/Diagnostics/`, Phase 3) — shared 12-case status DU +
  `statusToken`/`blocksAcceptance`/`tryParse`. Status tokens emitted in readiness summaries route through
  it where already wired; this feature does not re-encode status.
- **No `FeatureDescriptor`-like record exists yet** — this feature introduces the first one, harness-local.

**Rationale**: Prevents re-introducing duplication this refactoring already removed, and confirms the
descriptor is genuinely new work rather than a rename of an existing type.

---

## R5 — What is the byte-stability oracle, and how is the baseline captured?

**Decision**: The acceptance gate is **regenerate-and-diff**:
1. Capture, *before any edit*, every per-feature harness command's generated `specs/###-*/readiness/**`
   artifacts and its stdout/stderr/exit code into `specs/181-…/readiness/baseline/`.
2. Capture the full test sweep via `dotnet fsi scripts/baseline-tests.fsx --config Release --out
   specs/181-…/readiness/baseline/tests.md` (globs every `*.Tests.fsproj`, including release-only/sample
   lanes outside the solution).
3. After each story, regenerate the same artifacts into `post-change/` and `diff -r` them; rerun the
   sweep and confirm the **same** pass/fail set (no new reds vs. baseline).

The known pre-existing reds (`tests/Package.Tests`, `samples/ControlsGallery` stale-feed) are recorded as
baseline-not-regression, consistent with the feature-180 evidence (`14 green, 2 pre-existing reds`).

**Rationale**: The harness's generated readiness Markdown/JSON is the golden output the existing suites
assert against; a byte diff of regenerated artifacts + an unchanged red/green set is the strongest
"behavior unchanged" evidence available without standing up new infrastructure (SC-002/SC-004). Embedded
fonts (PROVENANCE.md, feature 136) make the rendered/measured output deterministic across hosts, so the
diff is meaningful.

**Alternatives considered**: *Trust the existing semantic token assertions alone.* Rejected — they check
*presence* of stable tokens, not byte-identity; a refactor could reorder or reword surrounding text and
still pass. The explicit regenerate-and-diff is the spec's named acceptance gate.

---

## R6 — Where should the descriptor live to avoid a cycle / surface change?

**Decision**: New module **`FeatureCatalog.(fsi/fs)` inside `tools/Rendering.Harness/`**, included in the
`.fsproj` *before* `Compositor.fs` so both `Compositor` and `Cli` consume it. No new project; no shipped
`FS.GG.UI.*` `.fsi` change (FR-008).

**Rationale**: The duplication is harness-local; the consumers (`Compositor`, `Cli`, and the
`Package.Tests` list which can reference the harness or a tiny shared descriptor) all live at or above the
harness. Putting the catalog in a shipped `src/` project would be both unnecessary and a Tier-1 surface
change. Include-order-before-`Compositor` keeps the dependency direction forward (Constitution: acyclic,
upward-fanning). The `Package.Tests` list needs the catalog's *data*; the simplest reachable form (a
direct reference to the harness, or a minimal descriptor mirror) is decided in US3 against whatever keeps
the include graph clean — noted as the one open execution choice, not a blocker.

---

## Summary of decisions

| # | Decision |
|---|---|
| R1 | Change unit = `tools/Rendering.Harness/`; counts (97 renderers / 262 constants / 12 isFeature checks) confirmed at HEAD. |
| R2 | 12 non-contiguous features; variant coverage non-uniform → descriptor carries a `Set<ReportVariant>`. |
| R3 | Collapse skeleton + constant derivation (real win); treat divergent prose as data and **measure** net lines per family before collapsing (SC-005 gated). |
| R4 | Reuse `RepositoryRoot` (P1) and `ReadinessStatus` (P3); descriptor is new. |
| R5 | Oracle = regenerate-and-diff `readiness/**` + stdout/stderr/exit + unchanged red/green sweep. |
| R6 | Descriptor lives in new harness-local `FeatureCatalog` module; no shipped surface change. |
