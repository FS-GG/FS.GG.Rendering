# T003 — Leak-surface map (`.template.config/template.json`)

Captured pre-fix on branch `228-fix-scaffold-skill-leak`, 2026-07-01. Cross-checks `data-model.md`.

## Product-skill sources (`source` under `template/product-skills/`)

Total: **18** sources = 9 skill ids × 2 surfaces (surface pairs).

| Skill id | `.agents/skills/` (provider surface) | `.claude/skills/` (workspace mirror) | Profile predicate |
|---|---|---|---|
| fs-gg-scene | profile-gated, **no** spec-kit clause ✓ | profile-gated, **missing** spec-kit clause ✗ (LEAK) | app \| headless-scene \| governed \| sample-pack \| game |
| fs-gg-symbology | ✓ | ✗ (LEAK) | app \| headless-scene \| governed \| sample-pack \| game |
| fs-gg-skiaviewer | ✓ | ✗ (LEAK) | app \| sample-pack \| game |
| fs-gg-elmish | ✓ | ✗ (LEAK) | app \| sample-pack \| game |
| fs-gg-keyboard-input | ✓ | ✗ (LEAK) | app \| game |
| fs-gg-ui-widgets | ✓ | ✗ (LEAK) | app \| game |
| fs-gg-styling | ✓ | ✗ (LEAK) | app \| game |
| fs-gg-layout | ✓ | ✗ (LEAK) | app \| game |
| fs-gg-testing | ✓ | ✗ (LEAK) | governed |

## Confirmed invariants

- **9** `.claude/skills/fs-gg-*/` product sources are `profile`-gated but **missing** `lifecycle == "spec-kit"` — the exact leak surface (R2).
- **9** matching `.agents/skills/fs-gg-*/` sibling sources are profile-gated with **no** spec-kit clause — correct, provider surface, out of scope.
- The already-gated base/sample/feedback `.claude/` sources (`fs-gg-samples`, `fs-gg-feedback-capture`, skillist re-emit) already carry `&& lifecycle == "spec-kit"` — the pattern the fix mirrors.
- `.codex/skills/` — no source exists (never written by the template).

**Fix target: exactly the 9 `.claude/skills/fs-gg-*/` product sources** — append `&& lifecycle == "spec-kit"`. All 9 are fixed; the `game` profile triggers only 8 of them (excludes `fs-gg-testing`, which is `governed`-only) — the counts differ because the sources are profile-gated, not because one is missed.
