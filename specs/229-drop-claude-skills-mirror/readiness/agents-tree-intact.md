# Provider tree intact + spec-kit reversal (T012) — Feature 229

## `.agents/skills/` never shrinks (SC-003, FR-002)

Live scaffold (`profile=game`, fully-confined template) — `.agents/skills/` UI product set is byte-identical
to the pre-fix baseline under every lifecycle:

```
spec-kit: .agents/skills/ = {fs-gg-project, product-*, 8 UI, samples, feedback, ...} (25 total; 8 UI)
sdd:      .agents/skills/ = 8 UI (+ base)   none: .agents/skills/ = 8 UI (+ base)
```

The 9 `.agents/skills/fs-gg-*/` product-skill sources were untouched; the base `.agents/` re-emit and the
repo-root `.agents/skills/` mirror are untouched. No reduction of the provider tree in any lifecycle.

## `sdd ≡ none` (SC-003)

Live validation asserts `treeFingerprint none == treeFingerprint sdd` per profile (`result: pass`).

## spec-kit reversal disclosed (FR-003)

Under `spec-kit`, UI product skills are now emitted to `.agents/skills/` ONLY (the `.claude/skills/` UI
mirror — 7 via the base mirror + 1 via the deleted per-skill source — is gone). This deliberately reverses
Feature 228's spec-kit-keeps-the-mirror invariant. `.claude/skills/` under spec-kit now holds only the base
`fs-gg-project` workspace skill (from `template/base/.claude/`). Explicit `spec-kit` still equals the no-flag
default (Feature 204 GV-3 `diff-vs-today=none`, green).
