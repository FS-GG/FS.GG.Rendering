# Validation-set justification records

> Migration Stage R3 deliverable. One justification record per candidate test/check from the
> source repository (FS-Skia-UI), per the constitution's rule that every active check carries
> a justification (contract / when it runs / owner / cost). This is a *decision* record — no
> tests are copied here (that is Stage R4) and the harness is not built (Stage R5).
>
> **Decision** ∈ `import-now` | `defer` | `archive` | `rewrite-smaller`.
> **Frequency** ∈ `local` | `ci` | `release-only` | `manual-advisory` (or `—` when not imported).
> Granularity is per source test project / named check (not per file) — see `research.md`.

| Candidate | Product contract | Failure mode | Owner | Frequency | Cost | Decision | Note |
|---|---|---|---|---|---|---|---|
| `Color.Tests` | Color contrast / palette correctness | wrong contrast ratios, palette regressions | rendering maintainer | local | fast, pure, no GPU | import-now | — |
| `Scene.Tests` | Scene graph + drawing-primitive behavior | scene-tree / render-routing regressions | rendering maintainer | local | fast, pure | import-now | — |
| `Layout.Tests` | Layout engine + graph validation | mis-layout, invalid graph accepted | rendering maintainer | local | fast, pure | import-now | — |
| `Input.Tests` | Pointer/input event model | dropped or misrouted pointer events | rendering maintainer | local | fast, pure | import-now | — |
| `KeyboardInput.Tests` | Keyboard model + key handling | key-routing / focus regressions | rendering maintainer | local | fast, pure | import-now | — |
| `Elmish.Tests` | MVU update/effect behavior | broken update transitions / effects | rendering maintainer | local | fast, pure | import-now | — |
| `Controls.Tests` | Semantic control behavior + accessibility | control-behavior / a11y regressions | rendering maintainer | local | fast, mostly pure | import-now | — |
| `Testing.Tests` | Test-helper library correctness | broken capture / proof seams | rendering maintainer | local | fast | import-now | — |
| `SkiaViewer.Tests` | Viewer/host + frame loop | viewer init / frame regressions | rendering maintainer | local | fast; needs GL context | import-now | local GL available per harness baseline; CI runs it only where a GL surface exists |
| `Smoke.Tests` | GL / startup smoke | startup or GL-context failures | rendering maintainer | local | fast; needs GL | import-now | honors Principle VI — must distinguish a defect from a missing window-system |
| `Lib.Tests` | Cross-cutting runtime helpers | helper regressions affecting runtime | rendering maintainer | local | fast | import-now | R4 triage: import the runtime-protecting tests; drop dead helpers |
| `surface-baselines` (+ `refresh-surface-baselines.fsx`) | Public `.fsi` surface stability (Principle II) | unintended public API drift | rendering maintainer | ci | low; regenerate on intended change | import-now | — |
| docs build (`fsdocs`) | Docs site builds from current sources | broken docs build | rendering maintainer | ci | moderate; `fsdocs build` | import-now | publish step is release-gated later (Stage R6) |
| `Package.Tests` | Package restore + consumption contract | package skew, broken consumer restore | rendering maintainer | release-only | moderate; needs pack | import-now | kept because it protects current package consumers (FR-008 condition met) |
| `Product.Tests` (`template/base`) | Generated product restores / builds / instantiates | broken `dotnet new` template or generated app | rendering maintainer | release-only | heavy; pack + instantiate | import-now | simulates a real generated consumer |
| `Parity.Tests` | Gallery visual parity | unnoticed visual regressions | rendering maintainer | — | heavy; image fixtures; flake risk | rewrite-smaller | fold into Stage R5 harness T1 offscreen-readback checks; do not import as-is |
| `ControlsPreview.Harness` | Controls preview / inspection (a harness, not a test) | n/a | rendering maintainer | — | n/a | defer | prior art for the Stage R5 harness; fold in, do not import as a legacy test |
| `Governance.Tests` | (governance behavior) | — | — | — | n/a | archive | governance machinery removed by the constitution; not a product concern |
| `SkillSupport.Tests` | (skill-support behavior) | — | — | — | n/a | archive | `SkillSupport` module excluded in R2; not owned here |
| historical readiness reports (`readiness/`, `docs/testSpecs`) | (historical readiness state) | — | rendering maintainer | — | n/a | archive | superseded; left in the archive, not an active obligation |
| `Parity` golden-image fixtures | visual baseline fixtures | stale fixtures misrepresent current output | rendering maintainer | — | large fixtures | archive | regenerate under harness T1 when needed; do not import stale fixtures |

## Coverage of required candidate classes (FR-007)

| Class | Covered by |
|---|---|
| Focused runtime unit tests | `Color`/`Scene`/`Layout`/`Input`/`KeyboardInput`/`Elmish`/`Controls`/`Testing`/`SkiaViewer`/`Smoke`/`Lib.Tests` |
| Public API surface-drift | `surface-baselines` (+ refresh script) |
| Package / consumer checks | `Package.Tests` |
| Template pack/install/instantiate | `Product.Tests` (template) |
| Docs build | `fsdocs` docs build |
| Broad historical readiness reports | `readiness/`, `docs/testSpecs` |
| Generated fixtures | `Parity` golden-image fixtures |

## Notes

- Every candidate above has a decision; **no candidate required an open-question deferral**
  (FR-010) — all were settleable from the source surface and the R2 module map. Should a
  future candidate be unsettleable, it is recorded as `defer` with options here and in the
  ledger, never dropped.
- Excluded-module tests (`Governance.Tests`, `SkillSupport.Tests`) are `archive`, consistent
  with the R2 exclusions in `docs/product/module-map.md`.
