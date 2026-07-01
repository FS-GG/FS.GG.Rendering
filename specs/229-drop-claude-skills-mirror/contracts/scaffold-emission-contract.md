# Contract: scaffold emission after the `.claude/skills/` UI-mirror drop

**What a scaffolded product MUST contain, per (lifecycle × surface × profile), after Feature 229.**

## Provider surface — `.agents/skills/` (unchanged, all lifecycles)

For every lifecycle (`spec-kit`, `sdd`, `none`) and every profile P, `.agents/skills/` MUST contain **exactly** the profile's UI product-skill set `UI(P)`, byte-identical to the pre-Feature-229 baseline:

- `app`: fs-gg-scene, fs-gg-skiaviewer, fs-gg-elmish, fs-gg-keyboard-input, fs-gg-ui-widgets, fs-gg-styling, fs-gg-layout, fs-gg-symbology
- `game`: same 8 as `app`
- `governed`: fs-gg-scene, fs-gg-testing, fs-gg-symbology
- `sample-pack`: fs-gg-scene, fs-gg-skiaviewer, fs-gg-elmish, fs-gg-symbology
- `headless-scene`: fs-gg-scene, fs-gg-symbology

(Plus the base `fs-gg-project` skill and, under `spec-kit` only, the sample/feedback skills — unchanged.)

## Orchestrator-owned surfaces — `.claude/skills/`, `.codex/skills/`

For every lifecycle and profile, the template MUST author **zero** `fs-gg-*` **UI product** skills into `.claude/skills/` and **zero** into `.codex/skills/`:

- `spec-kit`: `.claude/skills/fs-gg-{scene,symbology,skiaviewer,elmish,keyboard-input,ui-widgets,styling,layout,testing}` MUST be **absent**, and the base `.agents/skills/`→`.claude/skills/` mirror, sample-pack, and feedback `.claude/skills/` skills MUST also be absent (full confinement — all `.claude/skills/…` sources removed).
- `sdd` / `none`: absent (already true post-Feature-228; the whole base `.claude/` workspace is spec-kit-gated).
- `.codex/skills/`: the template writes nothing here under any lifecycle (unchanged).

**Sole surviving `.claude/skills/` entry:** under `spec-kit` only, `.claude/skills/fs-gg-project` — the base authoring skill that ships inside the base `.claude/` workspace tree (`template/base/.claude/`, settings + hooks + `fs-gg-project`). It is Spec Kit **workspace** infrastructure, not a UI product-skill mirror, and is exempt from the "0 UI product skills" invariant. No `sources` row targets `.claude/skills/…`.

## Cross-surface & cross-lifecycle invariants

- **Provider tree never shrinks**: `.agents/skills/` = `UI(P)` for all three lifecycles (FR-002/SC-003).
- **`sdd` ≡ `none`**: identical skill-tree output (FR-003/SC-003).
- **explicit `spec-kit` ≡ no-flag default**: byte-identical (GV-3 — both sides drop the UI mirror).
- **SDD scaffold clean**: `fsgg-sdd scaffold --provider rendering …` returns a success outcome with no `scaffold.providerWroteSddTree` diagnostic attributable to the template (FR-004/SC-001).

## Consumer/orchestrator responsibility (out of this repo)

Under `sdd`, the `fsgg-sdd` orchestrator (SDD#57) reads the provider's `.agents/skills/` set, unions it with the seeded `fs-gg-sdd-*` process skills, and materializes the byte-identical union into all three roots. That fan-out is **not** this template's job and is verified in the SDD repo.
