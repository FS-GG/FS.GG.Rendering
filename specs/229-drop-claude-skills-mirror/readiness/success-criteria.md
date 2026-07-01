# Success-criteria evidence index (T021) — Feature 229

| SC | Statement | Evidence | Status |
|----|-----------|----------|--------|
| SC-001 | SDD-orchestrated scaffold returns success, 0 `providerWroteSddTree` | `fixed-scaffold.md` (sdd `.claude/skills/` UI=0); `.claude/skills/` empty under sdd → nothing for the guard to flag. Full `fsgg-sdd scaffold` end-to-end is `environment-limited` (needs the SDD#57 orchestrator half; publish-before-flip) | met (rendering half); e2e env-limited |
| SC-002 | 0 template-authored `fs-gg-*` UI skills under `.claude/skills/` (and `.codex/`) under ANY lifecycle; 100% under `.agents/skills/` | `fixed-scaffold.md`: spec-kit/sdd/none `.claude/skills/` UI-product=0; `.agents/skills/` full set. `.codex/skills/` never written | **met** |
| SC-003 | `.agents/skills/` identical to baseline all lifecycles; `sdd ≡ none` | `agents-tree-intact.md`; live `treeFingerprint none == sdd` | **met** |
| SC-004 | `new-sdd-fullstack` runs to completion past scaffold | transitive on SC-001; e2e requires SDD#57 published (publish-before-flip) | env-limited (documented) |
| SC-005 | Corrected gates fail pre-fix, pass post-fix, all profiles; live 0 under spec-kit/sdd/none | `gate-transcripts.md` (red-before 2 fail / green-after 14 pass); live report `claude-product-skills=0` all lifecycles, `provenance: live` | **met** |
| SC-006 | Coherent set re-released (version bumped, packed) | `rerelease.md`; template `0.1.58-preview.1` → `0.1.59-preview.1`; dev packs proved installable | **met** (bump); org publish is follow-on |

## Scope note

SC-002's "0 under any lifecycle" is achieved by **full confinement** (removing the base
`.agents/skills/`→`.claude/skills/` mirror and the sample/feedback `.claude/skills/` sources in addition to
the 9 per-profile product-skill sources). The base `fs-gg-project` workspace skill (from `template/base/.claude/`)
is the one non-UI-product `.claude/skills/` entry under spec-kit and is exempt from the UI-product count.
