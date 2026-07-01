# Phase 0 — Research: drop the `.claude/skills/` UI-skill mirror

All unknowns resolved; no `[NEEDS CLARIFICATION]` remains.

## R1 — Gate decision: unconditional provider confinement (delete, not gate)

**Decision**: Remove the 9 per-profile `.claude/skills/fs-gg-*/` product-skill sources from `template.json` **entirely** (every lifecycle), rather than keeping them gated on `lifecycle == "spec-kit"` (the Feature 228 state).

**Rationale**: **ADR-0011** (Accepted 2026-07-01, `FS-GG/.github/docs/adr/0011-…`) §3: "A provider's product output for skills is `.agents/skills/` only; it MUST NOT write `.claude/skills/` or `.codex/skills/`." §4: "The `fs-gg-ui` template drops its `.claude/` skill emission … emit to `.agents/` only; the orchestrator fans them out." The confinement is stated without a lifecycle qualifier — so `spec-kit` is included. The three-root union (needed so Claude/Codex/generic runtimes are interchangeable) is materialized by the `fsgg-sdd` orchestrator (SDD#57), not by the provider.

**Alternatives considered**:
- *Keep the Feature 228 spec-kit gating and just release it* — rejected: leaves the provider writing `.claude/skills/` under `spec-kit`, violating ADR-0011 §3; and #42's DoD reads "emitting UI skills to `.agents/skills/` only (no `.claude/skills/` destination)."
- *Narrower `lifecycle != "sdd"` gate* — rejected: same violation under `none`/`spec-kit`, and inconsistent with the ADR.

## R2 — Confinement surface: every `.claude/skills/…` write (full confinement)

**Decision (revised during /implement after live evidence)**: Delete **every** `sources` row that targets `.claude/skills/…`: the 9 `.claude/skills/fs-gg-*/` product-skill rows, the base `.agents/skills/`→`.claude/skills/` mirror row, and the sample/feedback rows. Leave untouched: the 9 `.agents/skills/fs-gg-*/` sibling rows (provider surface); the base `.claude/` **workspace** row (`template/base/.claude/` — settings, hooks, the standalone `fs-gg-project` skill).

**Rationale**: The initial scope (delete only the 9 per-skill rows, per ADR-0011 §4's "Feature 219" wording) was shown by live scaffold to be insufficient — the base mirror still copied 7 of the 8 UI skills into `spec-kit`'s `.claude/skills/` (missing `fs-gg-styling`, absent from the repo-root `.agents/skills/`), leaving `spec-kit` **inconsistent** and violating SC-002 ("0 under any lifecycle") and issue #42's "no `.claude/skills/` destination." ADR-0011 §3 ("a provider MUST NOT write `.claude/skills/`") is unconditional, so the maintainer chose **full confinement**: remove all `.claude/skills/` writes. `.codex/skills/` is never written (confirmed: no source targets it).

**`fs-gg-project` exemption**: the base `.claude/` workspace tree (`template/base/.claude/`) carries `fs-gg-project` alongside settings/hooks — this is the standalone Spec Kit **workspace** the product ships, not a UI-product mirror, so it is kept and excluded from the "UI product skills in `.claude/skills/`" count. Under `sdd`/`none` the whole base `.claude/` tree is `spec-kit`-gated, so even `fs-gg-project` is absent there.

**Net invariant**: no `sources` row targets `.claude/skills/…`; live `.claude/skills/` holds zero UI product skills under every lifecycle (`{fs-gg-project}` under `spec-kit`, empty under `sdd`/`none`).

## R3 — The existing gates assert the *Feature 228* shape (the non-obvious core)

**Finding**: `Feature219EmitFrameworkSkillsTests.fs` (test at L144) asserts `sources.Length >= 18` and that **each** product-skill id emits under **both** `.agents/skills/` **and** `.claude/skills/`, with the `.claude/skills/` mirror `spec-kit`-gated. `Feature204LifecycleTemplateTests.fs` (`gatedSourceAudit` + GV-2) classifies the `.claude/skills/` product mirror as lifecycle-workspace and asserts `workspace >= 15`, with a `gated-condition:` report string naming "`.claude/skills/` product mirror". The fsx validator mirrors this (`workspaceChecked >= 15`, same `gated-condition:` line).

**Decision**: Correct all three in lockstep to encode the ADR-0011 invariant — **no** `template/product-skills/` source targets `.claude/skills/`/`.codex/skills/`; product-skill sources target `.agents/skills/` only. Concretely: Feature 219 `>= 18`→`>= 9`, surface assertion flips to `.agents`-only + "no id under `.claude/skills/`"; Feature 204 workspace floor `>= 15`→`>= 6`, classifier flags a product-skill→`.claude/skills/` as a violation, `gated-condition:` string updated; fsx mirrors the floor + string. See [gate-assertion-contract](./contracts/gate-assertion-contract.md).

**Rationale**: Left unchanged, the gates fail on the corrected template (Constitution V — they must fail pre-fix, pass post-fix). Making the invariant *stricter* (not looser) satisfies "never weaken an assertion to green a build."

## R4 — Regression guard

**Decision**: The corrected uniform invariant *is* the guard: any `template/product-skills/` source that targets `.claude/skills/`/`.codex/skills/` is a hard violation in both gates. Add a live `spec-kit` `claude-product-skills=0` observation in the fsx report for defense in depth (alongside the existing `sdd`/`none` observations).

**Rationale**: A future skill added the way Feature 227 added `fs-gg-layout` (both surfaces) would now trip the gate immediately, not silently reacquire the leak.

## R5 — GV-3 (spec-kit byte-identical) survives untouched

**Finding**: GV-3 compares explicit `--lifecycle spec-kit` against the **no-flag default of the same template** (default lifecycle = `spec-kit`), asserting `diff-vs-today=none`. It is **not** a diff against a prior released package.

**Decision**: Leave GV-3 unchanged. Removing the `.claude/skills/` product sources changes both the default and the explicit-`spec-kit` output equally, so they remain byte-identical; GV-3 still passes. (The `spec-kit` skill-set *does* change versus the prior release — that is FR-003's intended reversal of Feature 228 — but GV-3 does not measure that.)

## R6 — Re-release is required (FR-008)

**Decision**: Bump the `fs-gg-ui-template` coherent-set version and pack the local feed as part of delivery. Execute the bump + pack via `/speckit-merge` (the repo's release/merge lane). File/track the org-feed publish, registry flip (`FS-GG/.github`), and `FS.GG.Templates` provider re-pin as cross-repo follow-ons under **publish-before-flip**.

**Rationale**: #42's DoD requires the coherent set re-released and published so Templates can re-pin. Feature 228 was merged unreleased; the next release ships #228 + #229 together. The cross-repo coordination (registry/compat, Templates pin) is governed by the `cross-repo-coordination` protocol, not this repo's implement phase.

## Verified facts (against the current tree, post-Feature-228)

- `template.json` has 33 `sources`; 18 are product-skill rows (9 `.agents/skills/` + 9 `.claude/skills/`, the latter `… && lifecycle == "spec-kit"`). Deleting the 9 `.claude/skills/` rows leaves 9 product-skill sources, all `.agents/skills/`.
- No `sources` entry targets `.codex/skills/`.
- `Feature219…fs:147` asserts `>= 18`; `:150-163` the both-surfaces logic. `Feature204…fs:169-176` the floors + report string. `validate-lifecycle-template.fsx:174-176,438` the floors + `gated-condition:` line; `:315-324,347-348,367-368` the `claudeProductSkillCount` machinery (reused for the new spec-kit observation).
