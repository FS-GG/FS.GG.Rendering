---
phase: implement
date: 2026-06-25
severity: minor
---

## Process friction
The implementation went smoothly because the plan/research/data-model pinned the exact
zero-drift mechanism (bare-sibling `Scene list` append, first line at the spec-196 baseline)
up front, so byte-identity held by construction. The one real friction was a test-design
trap, not a process gap: the first cut of the multi-line channel-presence test compared a
two-word one-line label ("ALPHA BRAVO") against the same text with an embedded `\n`, but
soft-wrap correctly wraps the wide one-line spelling to the *same* two lines — so the bytes
matched and the test failed. Fixed by using a label short enough to fit one line ("A B" vs
"A\nB") so the only difference is the explicit break. What would have helped: a one-line note
in research R2 that "a long whitespace label is indistinguishable from the same text with a
hard break — that is intended; channel-presence tests must use text that fits one line."

## Generalizable code
none. The wrap/cap/ellipsis/stacking logic is symbology-grammar-specific (per-grammar region
+ budget + the existing `fitLabel`) and rightly stays internal to `src/Symbology/Symbology.fs`
with no `.fsi` surface. The `wrapSegment` greedy-whitespace fold is small and local; promoting
it to `FS.GG.UI.SkillSupport` would be premature — no second consumer exists.

## Skill gaps
none. `fs-gg-symbology` already covered the channel grammar and the headless render→eyeball
loop; this feature only widened the existing label channel, and the canonical SKILL.md +
mirrored product skill now document multi-line (wrap → cap → ellipsis, per-grammar budgets).

## Research links
research blocked — offline environment; no external lookups were needed (the change reused the
established `measureTextResolved` / `glyphRunProof` / `fitLabel` seams already in the repo).
