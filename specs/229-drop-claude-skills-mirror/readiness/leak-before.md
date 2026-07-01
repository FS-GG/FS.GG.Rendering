# Static before/after scan (T004 / T008) — Feature 229

`template.json` product-skill source scan. "Before" = post-Feature-228 template; "After" = post-T007.

## Before (T004)

```
product-skill sources -> .agents/skills/: 9
product-skill sources -> .claude/skills/: 9   ← the mirror ADR-0011 §3 requires the provider to stop writing
product-skill sources -> other: 0
```

All 9 `.claude/skills/` rows are gated `… && lifecycle == "spec-kit"` (Feature 228). Residual leak is
therefore **spec-kit-only**; `sdd`/`none` are already clean (Feature 228 removed them there).

### Live pre-fix repro (T005)

Packed the post-228 working tree as `FS.GG.UI.Template 0.1.58-dev229pre.1`, `dotnet new install`ed it,
and scaffolded `profile=game` per lifecycle (UI skills = fs-gg-{scene,symbology,skiaviewer,elmish,keyboard-input,ui-widgets,styling,layout}):

```
spec-kit: .claude/skills UI = 8   |  .agents/skills UI = 8   ← residual leak (ADR-0011 §3 violation)
sdd:      .claude/skills UI = 0   |  .agents/skills UI = 8   ← already clean (Feature 228)
none:     .claude/skills UI = 0   |  .agents/skills UI = 8   ← already clean (Feature 228)
```

Confirms the residual is **spec-kit-only** and the `.agents/skills/` provider tree carries the full set.

## After (T008)

After deleting the 9 `.claude/skills/fs-gg-*/` product-skill rows (`git diff`: 45 deletions, one file):

```
product-skill sources -> .agents/skills/: 9   ← unchanged
product-skill sources -> .claude/skills/: 0   ← the mirror is gone (all lifecycles)
product-skill sources -> other: 0
```

Out-of-scope `.claude/skills/` sources intact: the base `.agents/skills/`→`.claude/skills/` mirror
(target `.claude/skills/`), `fs-gg-samples`, and `fs-gg-feedback-capture`. `.codex/skills/` still
never written.
