# T024 — FR-007: non-game profiles are provably unchanged (quickstart Scenario E; SC-006)

The FR-007 byte-diff baseline ([fr007-baseline/](./fr007-baseline/)) was snapshotted from the
**pre-220** template (T006). After all US1–US3 edits, `headless-scene` / `governed` / `sample-pack`
were re-instantiated and their generated **source + tests** diffed against that baseline.

```
headless-scene: ✅ EMPTY DIFF (byte-identical src+tests)
governed:       ✅ EMPTY DIFF (byte-identical src+tests)
sample-pack:    ✅ EMPTY DIFF (byte-identical src+tests)
```

## Regression caught and fixed during this gate

The first diff pass flagged `sample-pack/src/Product/Program.fs`: the initial re-export
conditionalization had **reordered** the shared `let` bindings (and added a comment line inside the
controls block), which changed the resolved text for the controls family. Fixed by wrapping the
controls-only re-exports **in place** (preserving the exact original binding order) with **no**
added comment lines inside the conditional. Re-diff → all three EMPTY. This is exactly the silent
sample-pack regression the T024 gate exists to catch (Decision 2 / tasks.md Notes).

## Scope note (FR-005 vs FR-007)

The diff target is the generated **product source + tests + project** (the FR-007 byte-identical
contract). `docs/scaffold-map.md` is a shared authoring doc that **intentionally** updates for every
profile per **FR-005** (T021) to describe the new game starter; it is not a product-code regression.

**SC-006 (non-interactive half): 0 regressions.**
</content>
