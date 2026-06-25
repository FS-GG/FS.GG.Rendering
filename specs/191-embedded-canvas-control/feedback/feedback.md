# Feature 191 — fs-gg / Spec Kit feedback (T044)

Captured during implementation of the embedded canvas control. Severity: **info** unless noted.

## Process friction

- **`DISPLAY=:1` framing was over-specified (low).** The tasks repeatedly gate US1/US2 suites behind
  `DISPLAY=:1`, but `Controls.Tests`/`Elmish.Tests` are fully headless (`Control.renderTree` /
  `RetainedRender.step` / pure routing). All US1/US2 assertions ran green headless. The GL gate is only
  needed for live screenshot/perf evidence (SC-004), which remained environment-limited.

- **Reuse byte-identity vs `CachedSubtree` wrapping (medium).** Comparing a retained-step scene to a
  fresh `renderTree` scene fails on raw equality because reuse-stable frames add `CachedSubtree`/`Group`
  layers. The fix (normalize/strip those layers, as `Feature116PictureCacheTests` does) is non-obvious
  and worth a shared helper in `TestSupport` rather than re-deriving it per feature.

- **`MissingAccessibilityMetadata` is an Error for interactive kinds (low).** Hand-built `Control`
  records with `Accessibility = None` trip a fail-closed Error; tests must build through the
  constructors (`Canvas.create`/`Stack.create`) so metadata is inferred. Easy to hit, easy to fix once known.

## Generalizable-code candidates

- **`Reconcile.attrValueEqual` is closed-by-omission (medium).** A new `AttrValue` case silently hits
  the `| _ -> false` wildcard, meaning the attribute is treated as *always changed* (the canvas repainted
  every frame until `SceneValue` equality was added). A compile-time exhaustiveness nudge (or a documented
  checklist "add the new case to `attrValueEqual` and `mapAttrValue`") would prevent the same trap next time.

- **Volatile/always-dirty needs ancestor propagation to be fully general (low).** A `volatile'` node only
  repaints when its node is individually visited; when *nothing* in the subtree changes, the parent is
  reused wholesale and the volatile child is never reached. For per-frame canvases this is moot (the scene
  changes every frame), but a truly "always repaint regardless of siblings" guarantee would need a
  "contains-volatile" bit propagated up the reuse decision. Documented; deferred as out of scope.

## What worked well

- The `Scene`-only dependency for `FS.GG.UI.Canvas` kept `Elements`/`Loop` trivially headless-testable.
- The fingerprint-keyed picture cache delivered US2 cache isolation (0 chrome repaints) with **no** new
  cache machinery — the volatile flag is a guarantee/optimization layered on top, not a prerequisite.
