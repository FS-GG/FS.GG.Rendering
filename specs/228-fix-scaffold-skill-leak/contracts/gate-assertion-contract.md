# Contract: gate assertions after the fix

The invariants the corrected repo-owned gates MUST encode. These replace the assertions that currently enforce the leaky shape. Each must **fail on the pre-fix template** and **pass on the fixed template**.

## G-219 (Feature219EmitFrameworkSkillsTests.fs — G-EMIT)

- **G-219.1** Per-profile emitted-under-non-spec-kit set equals the matrix (`emittedFor` over `.agents/skills/` sources): app/game → the 8 UI skills; headless-scene → {scene, symbology}; governed → {scene, testing, symbology}; sample-pack → {scene, skiaviewer, elmish, symbology}. *(Unchanged in intent; still passes because `emittedFor` already excludes spec-kit-gated sources.)*
- **G-219.2 (corrected)** For every `template/product-skills/` source: if its target is under `.agents/skills/`, its condition MUST NOT contain `lifecycle == "spec-kit"` and MUST contain a `profile ==` predicate; if its target is under `.claude/skills/`, its condition MUST contain `lifecycle == "spec-kit"`. *(Replaces the old blanket "must not be lifecycle-gated" over all product sources.)*
- **G-219.3** Every skill id still has both an `.agents/skills/` source row and a `.claude/skills/` source row (structural pairing preserved; count `>= 18`).

## G-204 (Feature204LifecycleTemplateTests.fs — GV-2 / GV-4 / GV-5)

- **G-204.1 (corrected classifier)** `gatedSourceAudit` routes a `template/product-skills/` source to **framework** only when its target is under `.agents/skills/`; a `template/product-skills/` source whose target is under `.claude/skills/` goes to **lifecycle-workspace**. Framework sources: profile-gated, no spec-kit clause. Workspace sources: spec-kit clause required. No violations.
- **G-204.2 (floors)** `framework >= 9`, `workspace >= 15`, `product >= 3`.
- **G-204.3 (report string)** The `gated-condition:` line reflects the corrected rule (framework = `.agents/skills/` provider surface, lifecycle-independent; the `.claude/skills/` mirror is spec-kit-gated).
- **G-204.4 (sdd/none present + absent)** For every profile: `sdd/<p>: framework-skills-present=ok` and `none/<p>: framework-skills-present=ok` (`.agents/skills/` intact — unchanged) **and** a new observation that `.claude/skills/` holds zero product skills under `sdd` and `none`.
- **G-204.5 (spec-kit unchanged)** `spec-kit/<p>: generate=pass diff-vs-today=none` still holds for every profile.

## G-live (scripts/validate-lifecycle-template.fsx → readiness artifact)

- Emits, per (lifecycle ∈ {sdd, none} × profile): `claude-product-skills=0` (new) and `framework-skills-present=ok` (`.agents/skills/` == S(profile)).
- Emits, per (spec-kit × profile): both surfaces == S(profile) (subsumed by `diff-vs-today=none`).
- The regenerated `specs/204-template-lifecycle-symbol/readiness/lifecycle-template-validation.md` is the artifact G-219.3-report and G-204.3/.4 read.

## Non-goals (must NOT change)

- No assertion over `.agents/skills/` membership is loosened (provider surface still fully checked).
- Feature 224 (catalog currency), Feature 225 (product-skill vocabulary backstop) — unaffected (no skill added/removed; the `template/product-skills/` sources still exist and resolve). Do not edit.
- `docs/reports/skills-parity.md` — not regenerated (no canonical/wrapper skill added).
