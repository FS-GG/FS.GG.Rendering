# Contract: Public-surface invariance under the rebrand

**Guarantee (FR-005, SC-005)**: the rebrand changes the public API surface **only** by its namespace
prefix. No public type or member is added, removed, renamed, or retyped by R8.

## What "prefix-only" means

For every published module `<M>`, comparing the pre-rebrand surface to the post-rebrand surface
after substituting the prefix yields an **empty diff**:

```
normalize(old_surface) == normalize(new_surface)
where normalize(s) = s with "FS.Skia.UI." → "FS.GG.UI."
```

This holds for both representations of the surface:

1. **Signature files** — every `src/<M>/*.fsi`: the only changed line is the leading
   `namespace FS.Skia.UI.<M>` → `namespace FS.GG.UI.<M>`. Member declarations are byte-identical.
2. **Surface baselines** — `tests/surface-baselines/FS.GG.UI.<M>.txt`: every fully-qualified line is
   re-prefixed; the **set of members** (after normalization) is unchanged.

## How it is verified

- Regenerate baselines from the renamed assemblies via `scripts/refresh-surface-baselines.fsx`.
- The CI **surface-drift check** compares regenerated baselines to the committed ones; with the
  committed baselines already re-prefixed, any non-prefix delta fails the check.
- Cross-check: a normalized diff of old vs. new `.fsi`/baselines (substitute prefix, then `diff`)
  must be empty. A non-empty normalized diff means an accidental surface change rode along — a
  defect, not an accepted baseline update.

## Failure modes this guards against (spec Edge Case)

- A member silently added/removed while every line was being re-prefixed.
- A type re-typed (e.g. signature drift) masked by the expected prefix churn.
- A baseline file renamed but its contents left on the old prefix (mixed identity).

## Non-goals

This contract does not assert behavior equality beyond surface (the rebrand is behavior-neutral by
construction — no `.fs` logic changes). Behavioral non-regression is covered by the default-tier
validation set passing (FR-004, `quickstart.md`).
