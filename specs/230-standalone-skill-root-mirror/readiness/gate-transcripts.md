# Gate transcripts (T010) — Feature 230

## Red-before (new gates vs the Feature 229 template — no mirror)

```
Failed!  - Failed: 2, Passed: 13, Total: 15
- Feature 219 G-EMIT: expected >=27 framework skill sources (9 x 3 roots), found 9.
- Feature 204 GV-2: expected >=30 lifecycle-workspace sources, found 9.
```

## Green-after (fixed template)

```
Passed!  - Failed: 0, Passed: 15, Skipped: 0, Total: 15
```

Feature 219: `sources.Length>=27`; each product skill emits to `.agents/` (all lifecycles) + `.claude/`+`.codex/`
(spec-kit); each id under all three roots. Feature 204: framework=9, workspace>=30 (actual 33), 0 violations;
GV-4/GV-5 `claude-product-skills=0 codex-product-skills=0` (sdd/none); GV-4b `three-root-mirror=ok` (spec-kit);
GV-3 `diff-vs-today=none` unchanged. Report provenance: live.

## Full baseline

`baseline-after.md`: 21 projects, 21 green, 0 red (no regression).
