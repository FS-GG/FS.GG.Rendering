# Phase 1 Data Model: scaffold source-map categories & emission matrix

No runtime data model (content/config change). The "entities" are the classification categories of `template.json` `sources[]` and the resulting per-scaffold emission matrix.

## Entity: Scaffold source (`template.json` `sources[]` entry)

Fields used by the gates: `source` (repo-relative dir), `target` (product-relative dir), `condition` (template-engine predicate over `profile`, `lifecycle`, `feedback`).

Relevant subset — the per-profile product-skill sources come in **surface pairs** per skill id:

| Skill id | `.agents/skills/` source (provider surface) | `.claude/skills/` source (workspace mirror) | Profile predicate |
|---|---|---|---|
| fs-gg-scene | keep (profile-gated) | **+ `&& lifecycle == "spec-kit"`** | app \| headless-scene \| governed \| sample-pack \| game |
| fs-gg-symbology | keep | **+ spec-kit** | app \| headless-scene \| governed \| sample-pack \| game |
| fs-gg-skiaviewer | keep | **+ spec-kit** | app \| sample-pack \| game |
| fs-gg-elmish | keep | **+ spec-kit** | app \| sample-pack \| game |
| fs-gg-keyboard-input | keep | **+ spec-kit** | app \| game |
| fs-gg-ui-widgets | keep | **+ spec-kit** | app \| game |
| fs-gg-styling | keep | **+ spec-kit** | app \| game |
| fs-gg-layout | keep | **+ spec-kit** | app \| game |
| fs-gg-testing | keep | **+ spec-kit** | governed |

Only the right column changes (9 sources). `.codex/skills/`: no source exists (never written).

## Entity: Gating category (Feature 204 `gatedSourceAudit` classifier)

Corrected classification (target now discriminates the product-skill mirror):

| Category | Membership rule (corrected) | Condition invariant |
|---|---|---|
| **framework** (provider skill) | `source` under `template/product-skills/` **and** `target` under `.agents/skills/` | carries `profile ==`, **no** spec-kit clause |
| **lifecycle-workspace** | `target` under `.specify`/`.agents`/`.claude`/`CLAUDE.md`/`AGENTS.md`, the generated tree, the skillist exception — **including** product-skill sources whose `target` is under `.claude/skills/` | carries `lifecycle == "spec-kit"` |
| **product** | everything else | carries neither |

Count deltas from the fix: `framework` 18 → **9**; `lifecycle-workspace` +9 (→ **≥15**); `product` unchanged. GV-2 floors update to `framework >= 9`, `workspace >= 15`.

## Entity: Emission matrix (scaffolded product, per lifecycle × surface)

For a profile shipping skill-set **S** (e.g. `game` → the 8 UI skills):

| Lifecycle | `.agents/skills/` (provider) | `.claude/skills/` (mirror) | `.codex/skills/` | Base agent-context (`fs-gg-project`, AGENTS/CLAUDE.md, `.specify/`) |
|---|---|---|---|---|
| **spec-kit** | S | S | — (never written) | emitted |
| **sdd** | S | **∅** (was S — the leak) | — | suppressed |
| **none** | S | **∅** (was S — the leak) | — | suppressed |

Invariants (also the acceptance targets):
- `.agents/skills/` == S under **all** lifecycles (provider surface never shrinks — FR-002).
- `.claude/skills/` == S iff `lifecycle == spec-kit`, else ∅ (FR-001, FR-003).
- `sdd` and `none` columns are identical (FR-003, lifecycle contract).
- `spec-kit` row unchanged vs pre-fix (Feature 204 GV-3 byte-identical).

## State transition: scaffold outcome (SDD-orchestrated)

`fsgg-sdd scaffold --provider rendering …`:

- **Before fix** (`lifecycle == sdd`): template writes S into `.claude/skills/` → SDD boundary check `scaffold.providerWroteSddTree` (error) → `scaffold.outcome: providerFailed` → report `outcome: blocked` → `new-sdd-fullstack` aborts (`set -e`) before governance-overlay/`doctor`.
- **After fix**: template writes ∅ into `.claude/skills/` (S only in `.agents/skills/`) → boundary check passes → `outcome: success` → script proceeds to governance-overlay + `doctor`.
