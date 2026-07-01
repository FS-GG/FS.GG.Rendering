# Contract: scaffold-emission (fs-gg-ui template × lifecycle × surface)

The observable contract of a product scaffolded from `dotnet new fs-gg-ui` (directly or via `fsgg-sdd scaffold --provider rendering`). Consumers: SDD's scaffold boundary check, and the generated product's agent tooling. This encodes the `fs-gg-ui-template` cross-repo contract (FR-008/FR-011) for the skill trees.

Let **S(profile)** = the UI product skills the profile ships (e.g. `game`/`app` → scene, symbology, skiaviewer, elmish, keyboard-input, ui-widgets, styling, layout).

## C-1 Provider surface is universal

For every lifecycle ∈ {spec-kit, sdd, none} and every profile, the scaffold MUST place **exactly S(profile)** under `.agents/skills/` (each `fs-gg-<id>/SKILL.md` present, plus any skill assets such as `fs-gg-symbology/reference.fsx`). The provider surface never depends on lifecycle and never shrinks.

## C-2 Workspace mirror is spec-kit-only

The scaffold MUST place UI product skills under `.claude/skills/` **iff** `lifecycle == "spec-kit"`:
- `spec-kit`: `.claude/skills/` contains S(profile) (unchanged vs today).
- `sdd`, `none`: `.claude/skills/` contains **zero** `fs-gg-*` UI product skills.

## C-3 No provider write to orchestrator-owned trees under SDD

Under `lifecycle == "sdd"`, the template MUST write **nothing** into any SDD-owned tree: `.claude/skills/`, `.codex/skills/`, `.fsgg/`, `work/`, `readiness/`. (The template already writes none of `.codex/skills/`, `.fsgg/`, `work/`, `readiness/`; this contract adds `.claude/skills/`.) Result: `fsgg-sdd scaffold --provider rendering …` returns `outcome: success` with no `scaffold.providerWroteSddTree` diagnostic.

## C-4 sdd ≡ none

For every profile, the `sdd` and `none` scaffolds MUST produce identical skill-tree output (per `symbols.lifecycle`: "`none` = same template-level output as `sdd`").

## C-5 spec-kit unchanged

For every profile, the `spec-kit` scaffold's full emitted skill set (across `.agents/skills/`, `.claude/skills/`, base agent-context) MUST be byte-identical to the pre-fix template (Feature 204 GV-3).

## Verification hooks

- **Static** (env-free): `template.json` `sources[]` — every `.claude/skills/` (and `.codex/skills/`) target carries `lifecycle == "spec-kit"`; every `.agents/skills/` product-skill target carries a `profile` predicate and no spec-kit clause. (Feature 204 `gatedSourceAudit`, Feature 219 G-EMIT.)
- **Live** (env-gated scaffold): under `sdd`/`none`, `count(.claude/skills/fs-gg-*) == 0` and `set(.agents/skills/fs-gg-*) == S(profile)`; under `spec-kit`, both surfaces == S(profile). (`scripts/validate-lifecycle-template.fsx`.)
