# Research: Complete P8 Layout Acceptance

## Decision: Treat Feature151 as final P8 acceptance, not a second protocol rewrite

**Rationale**: Feature150 already landed the first P8/R3b slice: explicit layout constraints,
intrinsic queries/results, content extent, cache-entry records, ScrollViewer extent readback,
Controls.Elmish layout metrics, Testing layout-readiness helpers, surface baselines, package
validation, and focused readiness artifacts. The radical rendering report states the remaining
P8 work is representative corpus breadth, evaluator-internal measured/intrinsic reuse, broad
regression evidence, and full solution/package validation. Feature151 should close those gaps
without replacing the accepted public protocol.

**Alternatives considered**:

- Replace Yoga or add a general solver: rejected by the spec scope and the roadmap; solver work is a
  separate specialized follow-up.
- Re-open Feature150's public protocol before evidence: rejected because the current gap is breadth
  and acceptance evidence, not a known missing API.
- Fold in P7 live compositor acceptance: rejected because P7 remains environment-limited and this
  feature must not claim new compositor partial-redraw behavior.

## Decision: Use Feature150 public surfaces as the acceptance substrate

**Rationale**: `FS.GG.UI.Layout` already exposes `Layout.constraints`,
`Layout.constraintsFromAvailable`, `Layout.layoutInputKey`, `Layout.intrinsicQuery`,
`Layout.evaluateIntrinsic`, `Layout.measureProtocol`, `Layout.cacheEntry`, `Layout.contentExtent`,
`Layout.evaluate`, and `Layout.evaluateIncremental`. `FS.GG.UI.Controls.Control.scrollViewport`
exposes viewport/content extent and offset readback. `FS.GG.UI.Testing.LayoutReadiness` exposes
readiness statuses and validation. These surfaces are sufficient for corpus and readiness
acceptance unless implementation proves an observable gap.

**Alternatives considered**:

- Add a private-only acceptance harness: rejected because Tier 1 readiness must prove the public
  package-facing contract.
- Add a new acceptance package: rejected because it would split evidence away from the packages
  consumers actually use.
- Change public types up front: rejected until a corpus case demonstrates that existing public
  records cannot express required evidence.

## Decision: Represent the corpus as both fixtures and a reviewable ledger

**Rationale**: Tests prove behavior, while readiness reviewers need a compact map from each required
case to expected bounds, placements, scroll extents, diagnostics, command, and verdict. A typed
fixture set in `tests/Layout.Tests` and `tests/Controls.Tests` gives executable coverage; readiness
files under `specs/151-complete-p8-layout/readiness/` make acceptance auditable in under 10 minutes.

**Alternatives considered**:

- Rely only on test names: rejected because test output alone does not expose expected geometry or
  diagnostic rationale.
- Rely only on Markdown: rejected because acceptance must be backed by automated evidence.
- Generate one large golden file: rejected because it is harder to classify failures and unrelated
  environment limits case by case.

## Decision: Accept reuse only with full dependency-key evidence

**Rationale**: The main behavioral risk after Feature150 is accepting stale measured or intrinsic
results. Reuse must require matching participant id, entry kind, normalized constraints or query
identity, content/layout input key, visibility, child order, child dependency keys, intrinsic
dependency keys, and evaluator/cache revision. Warm hits are accepted only when full and incremental
outputs remain equivalent; stale or partial matches are misses or diagnostics before output can be
accepted.

**Alternatives considered**:

- Reuse by node id only: rejected because constraints, content, child order, visibility, and
  intrinsic dependencies can change under a stable id.
- Disable all caching to pass parity: rejected because measured and intrinsic reuse is explicitly in
  the remaining P8 acceptance bar.
- Count cache misses as failures: rejected because valid input changes should miss; stale accepted
  hits are the failure.

## Decision: Keep regression evidence explicit and classified

**Rationale**: Broad validation spans retained rendering, default layout, disabled-cache parity,
overlay behavior, render-anywhere, text shaping, compositor readiness, package/public surface
compatibility, and full solution/package validation. A failed, skipped, synthetic-only, or
environment-limited result must be classified before final readiness is accepted, so the readiness
summary cannot turn incomplete validation into accepted P8 status by omission.

**Alternatives considered**:

- Run only `Feature151` filters: rejected because the spec requires broad regression coverage.
- Treat all failing broad tests as P8 failures: rejected because unrelated pre-existing failures must
  be classified rather than hidden.
- Treat environment-limited compositor checks as accepted: rejected because P7 live proof remains
  explicitly environment-limited.

## Decision: Publish final P8 readiness under the feature directory

**Rationale**: Feature150 already established local readiness artifacts. Feature151 should publish
the final P8 entry point under `specs/151-complete-p8-layout/readiness/validation-summary.md`, with
links to corpus, ScrollViewer, reuse, parity, regression, compatibility, package, and limitation
evidence. The radical rendering report and public validation docs can then link to that summary
instead of duplicating the evidence.

**Alternatives considered**:

- Update only the roadmap report: rejected because reviewers need feature-scoped command and verdict
  records.
- Scatter evidence across test output and package logs: rejected because the spec requires one
  reviewable readiness package.
- Reuse Feature150 readiness files as final P8 evidence: rejected because Feature150 explicitly
  records remaining open gaps.

## Decision: Use `dotnet` validation paths in this checkout

**Rationale**: The layout skill names FAKE targets (`CapabilityCheck`, `DependencyReport`,
`PackageSurfaceCheck`, `GeneratedProductCheck`), but this checkout currently has no root `fake.sh`.
The plan therefore uses existing runnable `dotnet restore`, `dotnet build`, `dotnet test`,
`dotnet fsi scripts/refresh-surface-baselines.fsx`, and `dotnet pack` commands. If the FAKE wrapper
is restored later, its equivalent target output can be added to readiness without changing the P8
contract.

**Alternatives considered**:

- Mark planning blocked until `fake.sh` exists: rejected because equivalent repository commands are
  available.
- Invent replacement build scripts in this feature: rejected because P8 acceptance should validate
  layout behavior, not introduce unrelated build infrastructure.
- Omit package/surface checks: rejected by Tier 1 and package-readiness requirements.
