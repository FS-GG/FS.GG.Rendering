# Feature 190 — per-phase feedback (fs-gg-feedback-capture)

Phase 6 (final) of the god-module decomposition campaign: `RetainedRender.step` pipeline decomposition.

## Process friction

- **Severity: low.** The `.fsi`-first relocation (T006) required moving namespace-level types
  (`WorkReductionRecord` + the `Compositor*`/`Feature159*` family) *and* their module-local input
  records (`DamageSetInputs`/`PromotionInputs`) out of `RetainedRender.fsi`, plus requalifying ~80
  internal test call sites `RetainedRender.* → CompositorPolicy.*`. Mechanical but broad; a single sed
  over the moved function names handled it cleanly with zero assertion changes. The compiler + the
  unchanged 932-test corpus made it safe.
- **Severity: low.** `ControlRenderResult.Scene` is a single `Scene`, not a `Scene list` — easy to
  trip on when writing byte-identity assertions (the existing `Audit_Reconcile` pattern of
  `Expect.equal a.Render.Scene b.Render.Scene` is the right idiom; `RetainedRender.hashScene` takes a
  list).
- **Severity: low.** `RetainedRenderStep`/`RetainedNode` are NOT structurally equality-comparable
  (they carry `Control<'msg>` with function-typed event handlers), so composition tests must compare
  projections (`.Render.Scene`, `.WorkReduction`, `.Diagnostics`), never the whole record.

## Generalizable-code candidates

- **The `ctx`-rebind extraction idiom** (rebind `let theme = ctx.Theme` etc. at each stage's top so the
  lifted closure bodies stay verbatim) is the reusable technique that made a 615-line hot-path
  decomposition byte-identical by construction. Worth codifying as the standard Pattern-B move for
  future closure-nest extractions.
- The dual-mode trace-parity test (capture `Console.Error`; assert full span set under
  `FS_GG_RENDER_LAG_TRACE=1`, else assert silence) is a reusable shape for any module whose trace
  `enabled` flag is read once at load.

## What went well

- Byte-identity held on the first green build of the extraction — the rebind idiom + verbatim bodies
  worked exactly as the plan predicted (FR-002 "byte-identical by construction").
- The regression gate (Feature190GateTests) is genuinely discriminating: the injected damage-set
  perturbation flips the golden, proving SC-008 is not vacuous.

## Deviations (disclosed)

- `RetainedRender.fs` lands at 1620 lines (≈1500 soft target) and `assemblyStage` at ~330 (≈250 soft
  target) — recorded in `readiness/line-counts.md` with rationale. Both are within the "≈" intent;
  further splitting was judged counter to FR-016.
- US2 (`init` convergence) dropped-and-recorded (`readiness/us2-decision.md`) — no net reduction.
