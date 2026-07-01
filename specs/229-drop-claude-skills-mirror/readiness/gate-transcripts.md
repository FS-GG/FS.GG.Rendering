# Gate transcripts (T006 / T017) — Feature 229

## Red-before (corrected gates vs the pre-fix template)

Restored the pre-fix `.template.config/template.json` (9 `.claude/skills/` product-skill sources present)
and ran the corrected Feature 204/219 gates:

```
Failed!  - Failed: 2, Passed: 12, Total: 14
- Feature 204 GV-2: gating violations — product-skill template/product-skills/fs-gg-{scene,symbology,
  skiaviewer,elmish,keyboard-input,ui-widgets,styling,layout,testing} -> .claude/skills/... must target
  .agents/skills/ only (ADR-0011); AND: no template source may target .claude/skills/ (full confinement).
- Feature 219 G-EMIT: fs-gg-scene -> .claude/skills/fs-gg-scene/: product-skill sources must target
  .agents/skills/ only (ADR-0011). Expected subject string to start with 'a', got 'c'.
```

Confirms the corrected gates FAIL on the pre-fix template (Constitution V).

## Green-after (corrected gates vs the fixed template)

```
Passed!  - Failed: 0, Passed: 14, Skipped: 0, Total: 14
```

Feature 204: `framework=9`, `workspace>=6`, 0 violations, spec-kit/sdd/none `claude-product-skills=0`,
sdd/none `framework-skills-present=ok`, spec-kit `diff-vs-today=none` (GV-3 unchanged), new `gated-condition`
string. Feature 219: `sources.Length>=9`, all product-skill sources target `.agents/skills/`, none target
`.claude/skills/`. Report provenance: live.
