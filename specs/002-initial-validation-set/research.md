# Phase 0 Research: Define the Initial Validation Set (Stage R3)

This stage produces decision artifacts, so "research" means enumerating the source test
surface and resolving how to triage it. No software-stack unknowns exist (no code is
written), so there are no `NEEDS CLARIFICATION` items. Each decision is grounded in the
inspected source.

## Source test surface inspected

`/home/developer/projects/FS-Skia-UI/tests/` — ~17 test projects (~221 test `.fs` files):

| Project | Apparent purpose | Maps to (R2 module map) |
|---|---|---|
| `Color.Tests` | Color primitives | Color (owned) |
| `Scene.Tests` | Scene graph / primitives | Scene (owned) |
| `Layout.Tests` | Layout engine/graph | Layout (owned) |
| `Input.Tests` | Pointer/input model | Input (owned) |
| `KeyboardInput.Tests` | Keyboard input | KeyboardInput (owned) |
| `Elmish.Tests` | MVU integration | Elmish (owned) |
| `SkiaViewer.Tests` | Viewer/host | Viewer (owned) |
| `Controls.Tests` | Semantic controls | Controls (owned) |
| `Testing.Tests` | Test-helper library | Testing (owned) |
| `Lib.Tests` | Cross-cutting/library helpers | (triage by content) |
| `Smoke.Tests` | GL/startup smoke | Viewer / Rendering.Core |
| `Parity.Tests` | Gallery/visual parity | validation surface |
| `Package.Tests` | Package/consumer checks | Tooling/Template |
| `Governance.Tests` | Governance behavior | **excluded** (governance) |
| `SkillSupport.Tests` | Skill-support behavior | **excluded** (SkillSupport excluded in R2) |
| `ControlsPreview.Harness` | Controls preview harness | infra (relate to Stage R5 harness) |

Plus: `readiness/surface-baselines` + `scripts/refresh-surface-baselines.fsx` (API
surface-drift baselines); `template/base/tests/Product.Tests` (generated-product smoke);
`docs/testSpecs` and `readiness/` historical reports.

## Decision 1 — Record granularity

**Decision**: One justification record **per source test project / check** (the ~17
projects + surface-drift check + template smoke + readiness reports), not per individual
test file. Group runtime unit-test projects by the module they cover.

**Rationale**: ~221 files is too granular to justify individually and would recreate the
"heavy" feeling the migration avoids; the project is the natural unit of import and of
ownership. Per-file decisions can happen at Stage R4 within an imported project.

**Alternatives considered**: Per-file records (rejected — disproportionate cost); per-module
only (rejected — loses non-module checks like Package/Parity/surface-drift).

## Decision 2 — Default decisions applied (from the plan)

| Candidate class | Default decision |
|---|---|
| Focused runtime unit tests (Color/Scene/Layout/Input/KeyboardInput/Elmish/SkiaViewer/Controls/Testing) | **import now** (local inner loop) |
| GL/startup smoke (`Smoke.Tests`) | **import now** (local/CI — fast, protects startup; honors Principle VI: distinguish defect vs missing window-system) |
| API surface-drift (`surface-baselines` + refresh script) | **import now** (CI / on baseline change — protects Principle II) |
| Package/consumer checks (`Package.Tests`) | **import now only if they protect current consumers**; otherwise defer |
| Template generated-product smoke (`Product.Tests`) | **import now** (release/CI — simulates a real generated consumer) |
| Visual parity / gallery (`Parity.Tests`) | **defer** or **rewrite smaller** (heavy; on-demand, tie to Stage R5 harness tiers) |
| `Lib.Tests` | triage by content — import the parts protecting current runtime behavior, defer the rest |
| `Governance.Tests`, `SkillSupport.Tests` | **archive/exclude** (governance machinery removed; SkillSupport excluded in R2) |
| Broad historical readiness reports (`readiness/`, `docs/testSpecs`) | **defer/archive** (not active obligations) |
| Stale generated fixtures | **archive** with reason |

**Rationale**: Mirrors the plan's default decisions and the constitution's "checks pay for
themselves" rule; deviations are justified per candidate at implementation.

**Alternatives considered**: Import the whole `tests/` tree (rejected — the bulk-import the
migration explicitly forbids); import nothing now (rejected — leaves R4 with no protected
baseline).

## Decision 3 — Frequency taxonomy

**Decision**: Four labels — **local inner loop** (fast deterministic, default), **CI**
(runs on push/PR), **release-only** (packaging/template/perf), **manual/advisory**
(on-demand). Release-only checks are listed in a separate group from local checks.

**Rationale**: Keeps the default local tier fast (the thing contributors actually run) while
separating heavier/packaging checks — directly satisfies the R3 exit criteria.

## Decision 4 — Harness as deliberate infrastructure

**Decision**: The rendering test harness gets its **own** justification record
(`docs/validation/harness.md`), marked deliberate-infrastructure, decision = "build at
Stage R5; display-agnostic parts (env probe, CLI skeleton, evidence schema) MAY scaffold
earlier." It is explicitly **not** an imported legacy test. `ControlsPreview.Harness` and
`Parity.Tests` are noted as related prior art to fold into harness tiers, not imported
wholesale.

**Rationale**: The plan and project notes treat the harness as a first-class capability,
not a legacy import; recording it separately keeps "deliberately light" from being read as
"skimp on infrastructure."

## Decision 5 — Deferral/archive ledger is non-binding

**Decision**: Deferred, archived, and rewrite-pending candidates live in
`docs/validation/deferral-ledger.md`, each with a reason and an explicit "not an active
obligation — does not block routine work" marker.

**Rationale**: Preserves intent (nothing silently lost) without re-imposing obligations —
matches the R3 exit criterion "deferred checks are not lost, but they are not active
obligations."

## Out of scope (explicit deferrals)

- Copying any selected test or source → Stage R4.
- Building the harness or any tier → Stage R5.
- Wiring checks into a CI system → Stage R6 (stabilize product validation).
- Per-test-file decisions within an imported project → Stage R4.
