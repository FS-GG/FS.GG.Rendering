# Three-root mirror live evidence (T006) — Feature 230

Packed the working tree, installed it (dev version sorting above the feed-published `0.1.59-preview.1`,
which otherwise shadows `dotnet new`), scaffolded per lifecycle.

## `profile=game`

```
spec-kit: .agents=25 .claude=25 .codex=25   agents==claude set: YES   agents==codex set: YES
          styling byte-identical across agents/claude: YES
sdd:      .agents=8  .claude=0  .codex=0
none:     .agents=8  .claude=0  .codex=0
```

## Live lifecycle-validation report (T010, provenance: live)

`FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx` → `result: pass`:

```
spec-kit/app|headless-scene|governed|sample-pack: three-root-mirror=ok
sdd/<p>:  framework-skills-present=ok (8|2|3|4 SKILL.md)   claude-product-skills=0 codex-product-skills=0
none/<p>: framework-skills-present=ok (8|2|3|4 SKILL.md)   claude-product-skills=0 codex-product-skills=0
provenance: live
result: pass
```

**Result**: under `spec-kit` the three agent-skill roots mirror (ADR-0011 §1); under `sdd`/`none` the
orchestrator-owned `.claude/`/`.codex/` receive zero product skills (Templates#47 stays unblocked; SDD#57 fans out).
