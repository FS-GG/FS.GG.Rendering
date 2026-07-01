# Phase 1 — Data Model: scaffold source-map categories & emission matrix

No runtime entities. The "data" here is the template's **scaffold source map** (`template.json` `sources`) and the classification the gates derive from it.

## Template source categories (corrected for ADR-0011)

| Category | Definition | Lifecycle gating | Count (after fix) |
|----------|-----------|------------------|-------------------|
| **Framework product-skill** | `source` under `template/product-skills/` **and** `target` under `.agents/skills/` | none — profile-gated only (follows the product, present under every lifecycle) | 9 |
| **Lifecycle-workspace** | `target` under `.specify/` \| `.agents/` (non-skill) \| `.claude/` \| `CLAUDE.md` \| `AGENTS.md`, the generated tree, or the `docs/skillist-reference.md` catalog exception — **including** the base `.agents/skills/`→`.claude/skills/` mirror and the sample/feedback `.claude/skills/` skills | `lifecycle == "spec-kit"` | ≥ 6 |
| **Product** | everything else (base → `./`, samples → `samples/`, ant overlay → `./`) | none | ≥ 3 |

**The change vs Feature 228**: Feature 228 introduced a fourth de-facto shape — a *product-skill source targeting `.claude/skills/`*, classified as lifecycle-workspace (spec-kit-gated). ADR-0011 removes that shape entirely: **no** `template/product-skills/` source may target `.claude/skills/`. The classifier therefore treats a `template/product-skills/` source whose target is **not** under `.agents/skills/` as a **violation**, not a workspace member.

Floors move accordingly: `framework = 9` (unchanged), `workspace ≥ 15 → ≥ 6` (loses the 9 `.claude/skills/` product mirrors), `product ≥ 3` (unchanged).

## The 9 product-skill sources (what is deleted vs kept)

| Skill id | Profiles | `.agents/skills/` row | `.claude/skills/` row |
|----------|----------|----------------------|----------------------|
| fs-gg-scene | app, headless-scene, governed, sample-pack, game | **KEEP** | **DELETE** |
| fs-gg-symbology | app, headless-scene, governed, sample-pack, game | **KEEP** | **DELETE** |
| fs-gg-skiaviewer | app, sample-pack, game | **KEEP** | **DELETE** |
| fs-gg-elmish | app, sample-pack, game | **KEEP** | **DELETE** |
| fs-gg-keyboard-input | app, game | **KEEP** | **DELETE** |
| fs-gg-ui-widgets | app, game | **KEEP** | **DELETE** |
| fs-gg-styling | app, game | **KEEP** | **DELETE** |
| fs-gg-layout | app, game | **KEEP** | **DELETE** |
| fs-gg-testing | governed | **KEEP** | **DELETE** |

"8 UI skills" (ADR-0011/#42) vs "9 sources": `fs-gg-testing` is a `governed`-only skill outside the app/game "8"; both descriptions count the same 9-source set.

## Emission matrix after the fix (skill destinations per lifecycle × surface)

For a given profile P, let `UI(P)` = the profile's UI product-skill set (the `.agents/skills/` column above filtered by P).

| Lifecycle | `.agents/skills/` | `.claude/skills/` (UI product skills) | `.claude/skills/` (base `fs-gg-project`) | `.codex/skills/` |
|-----------|-------------------|-------------------------------|------------------------------------------|------------------|
| `spec-kit` | `UI(P)` (unchanged) | **∅** (was `UI(P)` — **now removed**, incl. base mirror) | `{fs-gg-project}` (base `.claude/` workspace, kept) | ∅ (never written) |
| `sdd` | `UI(P)` (unchanged) | ∅ (already ∅ post-228) | ∅ (base `.claude/` is spec-kit-gated) | ∅ |
| `none` | `UI(P)` (unchanged) | ∅ (already ∅ post-228) | ∅ | ∅ |

**Full confinement**: no `sources` row targets `.claude/skills/…`. Under `spec-kit`, the only `.claude/skills/`
entry is `fs-gg-project`, which ships inside the base `.claude/` workspace tree (`template/base/.claude/`) — it
is workspace infrastructure, not a UI product-skill mirror, and is excluded from the UI-product count. The
base mirror, sample-pack, and feedback `.claude/skills/` sources were **all removed** (revised scope).

**Invariants preserved**:
- `.agents/skills/` = `UI(P)` for every lifecycle (provider tree never shrinks — FR-002/SC-003).
- `sdd` output ≡ `none` output (FR-003/SC-003).
- explicit `spec-kit` ≡ no-flag default (GV-3 — unchanged, both sides drop the mirror equally).

**Invariant deliberately changed**:
- `spec-kit` `.claude/skills/` UI mirror: `UI(P)` → **∅** (FR-003 supersedes Feature 228 FR-003). The union in the consumer's `.claude/skills/` is the orchestrator's responsibility (SDD#57).

## Gate-visible facts the report must carry

- `spec-kit/<profile>: claude-product-skills=0` (**new** — the spec-kit live observation).
- `sdd/<profile>: claude-product-skills=0` and `none/<profile>: claude-product-skills=0` (unchanged — still hold).
- `sdd/<profile>: framework-skills-present=ok`, `none/<profile>: framework-skills-present=ok` (unchanged).
- `gated-condition:` line reworded to drop "`.claude/skills/` product mirror" from the workspace description.
