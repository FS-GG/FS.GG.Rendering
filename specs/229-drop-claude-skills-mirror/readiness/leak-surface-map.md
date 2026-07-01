# Leak-surface map (T003) — Feature 229

Enumerated from `.template.config/template.json` on branch `229-drop-claude-skills-mirror`
(post-Feature-228 state). Confirms the exact 9 `.claude/skills/` product-skill sources to delete
and the out-of-scope siblings that stay.

## In scope — DELETE (9 product-skill sources targeting `.claude/skills/`)

All currently gated `(<profile predicate>) && lifecycle == "spec-kit"` (the Feature 228 gating):

| Target | Condition |
|--------|-----------|
| `.claude/skills/fs-gg-scene/` | `(app \| headless-scene \| governed \| sample-pack \| game) && lifecycle == "spec-kit"` |
| `.claude/skills/fs-gg-symbology/` | `(app \| headless-scene \| governed \| sample-pack \| game) && lifecycle == "spec-kit"` |
| `.claude/skills/fs-gg-skiaviewer/` | `(app \| sample-pack \| game) && lifecycle == "spec-kit"` |
| `.claude/skills/fs-gg-elmish/` | `(app \| sample-pack \| game) && lifecycle == "spec-kit"` |
| `.claude/skills/fs-gg-keyboard-input/` | `(app \| game) && lifecycle == "spec-kit"` |
| `.claude/skills/fs-gg-ui-widgets/` | `(app \| game) && lifecycle == "spec-kit"` |
| `.claude/skills/fs-gg-styling/` | `(app \| game) && lifecycle == "spec-kit"` |
| `.claude/skills/fs-gg-layout/` | `(app \| game) && lifecycle == "spec-kit"` |
| `.claude/skills/fs-gg-testing/` | `(governed) && lifecycle == "spec-kit"` |

## Scope REVISED during /implement — full confinement

Live evidence showed deleting only the 9 per-skill sources left `spec-kit`'s `.claude/skills/` with 7/8 UI
skills (via the base mirror) — inconsistent, and not "0 under any lifecycle." The maintainer chose **full
confinement**, so the scope expanded to remove **every** `.claude/skills/…` source:

- ✅ the 9 per-profile `.claude/skills/fs-gg-*/` product-skill rows,
- ✅ the **base mirror** `.agents/skills/` → `.claude/skills/` (this DOES carry UI skills — the repo-root
  `.agents/skills/` contains 7 of the 8 UI skills, so it mirrored them into `.claude/skills/` under spec-kit),
- ✅ the sample `.claude/skills/fs-gg-samples` and feedback `.claude/skills/fs-gg-feedback-capture` rows.

## KEEP unchanged

- The **9 matching `.agents/skills/fs-gg-*/` sources** (provider surface). Confirmed count: 9.
- The base `.claude/` **workspace** row (`template/base/.claude/` → `.claude/`) — settings, hooks, and the
  standalone `fs-gg-project` skill. This is Spec Kit workspace infrastructure, not a UI-skill mirror; the
  `fs-gg-project` skill is the sole `.claude/skills/` entry under `spec-kit` (exempt from the UI-product count).
- `.codex/skills/`: **no source targets it** (verified) — nothing to change.

**Net**: no `sources` row targets `.claude/skills/…`.

## Verified scan output

```
product-skill sources -> .agents/skills/: 9
product-skill sources -> .claude/skills/: 9
product-skill sources -> other: 0
```

After the fix (T007), the `.claude/skills/` count must be **0** and `.agents/skills/` must stay **9**.
