# Fixed-scaffold live evidence (T009 / T011) — Feature 229 (full confinement)

Packed the fully-confined working tree as `FS.GG.UI.Template 0.1.58-dev229post.2`, installed it, and
scaffolded `profile=game` per lifecycle. UI-product = fs-gg-{scene,symbology,skiaviewer,elmish,
keyboard-input,ui-widgets,styling,layout}; `all-fs-gg` includes the base `fs-gg-project` skill.

```
spec-kit: .claude/skills UI-product = 0 | .claude/skills all-fs-gg = 1 (fs-gg-project only) | .agents/skills = 25
sdd:      .claude/skills UI-product = 0 | .claude/skills all-fs-gg = 0 (empty)               | .agents/skills = 8
none:     .claude/skills UI-product = 0 | .claude/skills all-fs-gg = 0 (empty)               | .agents/skills = 8
```

`.claude/skills/` contents:
- spec-kit → `fs-gg-project` (base `.claude/` workspace skill only)
- sdd / none → empty (the base `.claude/` workspace tree is spec-kit-gated)

**Result**: UI product skills in `.claude/skills/` = **0 under every lifecycle** (SC-002). `.agents/skills/`
carries the full set under every lifecycle (SC-003). `sdd ≡ none` (identical skill trees).

## Live lifecycle-validation report (T016, provenance: live)

`FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx` → `result: pass`.
Report (`specs/204-template-lifecycle-symbol/readiness/lifecycle-template-validation.md`) records, per covered profile:

```
spec-kit/<p>: claude-product-skills=0
sdd/<p>:      framework-skills-present=ok (N SKILL.md)   sdd/<p>: claude-product-skills=0
none/<p>:     framework-skills-present=ok (N SKILL.md)   none/<p>: claude-product-skills=0
provenance: live
result: pass
```

(framework-skills-present counts: app=8, headless-scene=2, governed=3, sample-pack=4.)
