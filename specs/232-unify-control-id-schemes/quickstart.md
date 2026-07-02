# Quickstart / Validation: unify control-id schemes onto `Key ?? path`

**Feature**: 232 · How to prove the fix end-to-end. Behavior-level; exercises the real seams.

## Prerequisites

- .NET SDK per `global.json`; repo restored (`dotnet restore`).
- Test projects: `tests/Controls.Tests`, `tests/Controls.Elmish.Tests`.

## Early behavioral smoke (run FIRST, on `main`, before any fix)

Confirms the diagnosed root cause is real (plan standing assumption). Reproduce ≥1 symptom through the
real seams:

1. **Unkeyed keyboard dispatch drop** — build a tree with a single **unkeyed** focusable `Button` with
   an activation binding; establish focus on it; route an activation key through the focus/dispatch
   seam (`routeFocusedKey`). **Expected on `main`**: zero messages produced (the bug). Capture this as
   the failing baseline.
2. **Unkeyed hover not stamped** — build two **unkeyed** same-kind controls; set the runtime
   `HoveredControl` to the *path* of the second; run `applyRuntimeVisualState`. **Expected on `main`**:
   neither (or the wrong) node carries Hover.

Record the observed failure in the feature notes; if a symptom does NOT reproduce, revise research.md
before implementing.

## Validation commands (after the fix)

```sh
# Focus / id-parity + runtime bridge + widgets + diagnostics
dotnet test tests/Controls.Tests/Controls.Tests.fsproj

# routeFocusedKey dispatch, hover/press stamp, focus ring, at-rest identity
dotnet test tests/Controls.Elmish.Tests/Controls.Elmish.Tests.fsproj

# Full solution build + public-surface / ApiCompat gate
dotnet build FS.GG.Rendering.slnx   # or the repo's build entrypoint
dotnet test                          # whole suite
```

## Expected outcomes (maps to Success Criteria)

- **SC-001**: the unkeyed focused-control keyboard smoke now produces the activation message (was 0).
- **SC-002**: hover/press-derived state stamps exactly the pointer-resolved node; no same-kind sibling
  is wrongly stamped; the first sibling is byte-identical to its un-bridged form.
- **SC-003**: `Focus.order` gives two unkeyed same-kind siblings distinct stop ids (0 collisions);
  `Focus.traverse` steps between them.
- **SC-004**: DatePicker / SplitButton produce 0 `MissingOverlayAnchor`; the declared `triggerId`
  resolves to a real control; `focusScope` stops reference real ids.
- **SC-005**: all previously-green keyed-control tests stay green; at-rest bridged tree byte-identical;
  targeted-walk touched-node count unchanged for a keyed tree.
- **SC-006**: full build + suite pass; public-surface / ApiCompat reports no unaccounted drift.

## Notes

- Keyed controls are the regression guard: their ids are unchanged in every seam, so their existing
  tests must not need edits. Any test that DID need editing was asserting the unkeyed `Key ?? Kind` bug.
- This ships via the normal `fs-gg-ui` coherent-set release when merged (see cross-repo-coordination
  worked example); no registry flip is part of THIS feature's branch — it is a `src/` change.
