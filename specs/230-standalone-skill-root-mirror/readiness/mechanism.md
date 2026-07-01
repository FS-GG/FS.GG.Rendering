# Mirror mechanism (T003/T004) — Feature 230

## Per-skill twins required (not a blanket copy)

The scaffolded `.agents/skills/` under spec-kit is assembled as: repo-root `.agents/skills/` blanket copy,
then OVERWRITTEN by the canonical `template/product-skills/fs-gg-*` sources. The two differ byte-wise:

```
fs-gg-{scene,symbology,skiaviewer,elmish,keyboard-input,ui-widgets,layout,testing}:
  .agents/skills/fs-gg-X/SKILL.md  DIFFERS from  template/product-skills/fs-gg-X/SKILL.md
fs-gg-styling: present ONLY in template/product-skills/ (absent from repo-root .agents/skills/)
```

So a blanket `.agents/skills/`→`.claude/`/`.codex/` copy would mirror the stale repo-root bodies (and miss
styling). To make `.claude/`/`.codex/` byte-identical to `.agents/`, the mirror replicates BOTH the base
blanket AND the per-skill `template/product-skills/` overwrites into each root (24 twin sources total).

## `.codex/` is SDD-owned

The installed SDD scaffold fixtures include `fsgg-fixture-skills-intrusion-codex` ("writes into the
whole-root-reserved .codex... trees") — confirming `.codex/skills/` is guard-watched under orchestration.
The mirror twins are therefore `spec-kit`-gated (standalone, no orchestrator/guard); under `sdd`/`none`
`.codex/skills/` stays empty and the orchestrator (SDD#57) owns the fan-out.
