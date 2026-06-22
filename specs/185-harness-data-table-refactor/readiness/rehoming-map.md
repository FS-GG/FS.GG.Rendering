# Re-homing / Classification Map — Harness Data-Table Refactor (185)

Drives US1 `RequiredHeaders` population (T010) and US2 hook discovery (T019/T022). Catalogs the 12
descriptor rows: variants, static report titles, and which bodies genuinely diverge.

## Per-feature variants + static report titles (→ RequiredHeaders)

The static `# Feature <N> …` titles each feature's renderers emit. Interpolated titles
(`Scenario: {…}`, `Excluded …: {…}`) are excluded — they are dynamic, stay inline.

| Id | Static report titles (RequiredHeaders) |
|---|---|
| 148 | Live Preservation Proof; Damage Parity; Content/Placement Reuse; Snapshot Lifecycle; Timing Probe; Validation Summary; Compatibility Ledger |
| 149 | Live Compositor Proof; Damage Parity; Reuse Evidence; Snapshot Lifecycle; Timing Probe; Validation Summary; Compatibility Ledger |
| 152 | Live Proof Run Set; Damage-Scoped Live Parity; Timing Claim Decision; P7 Readiness Summary; Compatibility Ledger |
| 153 | Live Proof Interpreter; Proof-Set Decision; Compositor Proof Interpreter Readiness; Compatibility Ledger |
| 154 | Compositor Proof Acceptance; Same-Profile Damage-Scoped Parity; Timing Decision; Proof-Set Acceptance; P7 Readiness Verdict; Compatibility Ledger |
| 155 | Native Proof Capture; Same-Profile Damage-Scoped Parity; Timing Decision; Native Proof Set; P7 Closeout Verdict; Compatibility Ledger |
| 156 | Readiness Summary; Same-Profile Timing Summary; Unsupported Host Timing; Compatibility Ledger |
| 157 | Readiness Summary; Damage Attempt; Damage Summary; Fallback; Parity; Unsupported Host; Compatibility Ledger |
| 158 | Readiness Summary; Readback-Free Timing Summary; Proof/Probe Evidence; Unsupported Host Timing; Compatibility Ledger |
| 159 | Readiness Summary; Layer Promotion Summary; Promotion Attempt; Counter Evidence; Unsupported Host Promotion; Compatibility Ledger |
| 160 | Readiness Summary; Focused Throughput Summary; Focused Throughput Iteration; Full Validation; Unsupported Host Throughput; Compatibility Ledger |
| 161 | Readiness Summary; Host Performance Lane Ledger; Lane Ledger Entry; Host Facts; Full Validation; Unsupported Host Lane Ledger; Compatibility Ledger |

## Divergence classification (US2 hook discovery)

Every `renderFeature<N><Variant>` is a hand-written markdown template with feature-unique title +
body text (e.g. 148 LiveProof "Live Preservation Proof" + "Sample Regions" vs 149 LiveProof "Live
Compositor Proof" + "Acceptance Gate"). The variant *kind* is shared (`ReportVariant`), but the
*content* diverges per feature far more than the spec's "~20%" framing implies.

**Consequence for US2:** the realistic parametrization is a generic dispatcher over `ReportVariant`
that delegates per-(feature,variant) body to a `FeatureRenderHooks` entry. The SC-001 line-cap is met
by the **Pattern-E module split** (Compositor.Types/Config/FeatureState/Render), not by deduplicating
body text. SC-003 (`grep '^\s*let\s+renderFeature' → 0`) is met because the bodies become hook
lambdas / `Compositor.Render`-internal functions, not top-level `let renderFeature###` bindings.

**Genuinely feature-unique (must be explicit hooks regardless):**
- 159 layer-promotion (promotion attempts/fallbacks/parity/counters tree).
- 160 focused-throughput (iterations + csv + excluded evidence).
- 161 host-lane-ledger (lane-ledger entries + host-facts).
- 156/158 timing-scenario per-scenario report fan-out.
- 157 damage attempt/summary/fallback fan-out.

## Per-feature state machines (US2 T020)

The 6 divergent MVU sequences live in `Cli.runLegacyCompositorReadinessCmd` (148/149/152/153/154/155
each fold a distinct `updateReadiness` transition list) and the 156–161 dedicated handlers. The
parametric `init`/`update`/`status` over a descriptor replaces the dispatch skeleton; the
feature-specific transition lists move to descriptor data / hooks.

## CLI surface reality (US3 — corrects quickstart Step 3)

The observable CLI is `compositor-readiness --feature <N>` (+ `compositor-performance`/`-promotion`
`--feature <N>`), NOT top-level `156`/`feature156` aliases. FR-007 "preserve observable contract"
therefore means preserving the `--feature <N>` dispatch; US3 collapses the internal per-feature
`runFeature###*Cmd` handlers into one `descriptorById`-keyed `runReadiness`, keyed off `--feature`.
