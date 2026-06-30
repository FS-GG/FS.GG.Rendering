# Implementation Plan: Deliver the Symbology Product Skill to Consumers

**Branch**: `223-deliver-symbology-skill` | **Date**: 2026-06-30 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/223-deliver-symbology-skill/spec.md`

## Summary

The content-complete `fs-gg-symbology` product skill is stranded in the framework repo: it is
sourced by no `.template.config/template.json` entry, has no `fs-gg-product-symbology` consumer
wrapper, and the skill-parity harness is blind to the missing wrapper because the bare framework
wrapper `fs-gg-symbology` satisfies its check. This feature delivers it by mirroring the exact
pattern of the other six product skills:

1. **Source the skill** — add the two-entry (`.agents/skills/` + `.claude/skills/`) ungated,
   profile-gated product-skill source pair to `template.json`, scoped to the same profile set as
   `fs-gg-scene` (symbology is scene-token authoring). This delivers the full skill content to
   every scene-bearing generated app under every lifecycle.
2. **Add the consumer wrapper** — create `fs-gg-product-symbology` on both repo-root wrapper
   surfaces, byte-shaped exactly like the existing `fs-gg-product-*` wrappers.
3. **Close the parity blind spot** — narrow `SkillParity.missingWrapperFindings` so a
   product-skill canonical's wrapper requirement is satisfied **only** by its `fs-gg-product-*`
   alias, not by a bare same-named framework wrapper.
4. **Reverse the Feature 219 "not-vendored" record** — update the 219/204 emit tests, the
   lifecycle validator, and the data-model matrix that deliberately recorded symbology as the lone
   unwired product skill.
5. **Deliver to consumers** — carry the change on the next `fs-gg-ui-template` republish and update
   the cross-repo `fs-gg-ui-template` contract entry; close Coordination issue #35.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> The central de-risking finding below (GV-3 stays green) is verified by *reading* the validator,
> not by *running* it. `/speckit-tasks` MUST schedule an early live run of the env-gated lifecycle
> validator (`FS_GG_RUN_LIFECYCLE_VALIDATION=1`) in the Foundational phase — before the test edits
> harden — to confirm that wiring the symbology source actually leaves the explicit-`spec-kit` ==
> default invariant byte-identical for every profile, and to confirm the new content emits under
> `sdd`/`none`.

### Key research finding that de-risks the feature

Feature 219 deferred wiring symbology on the stated grounds that adding the overwrite source "would
red GV-3 (spec-kit byte-identical)." **That rationale was a misread of what GV-3 checks.** GV-3
(`validate-lifecycle-template.fsx:318-322`, `Feature204LifecycleTemplateTests.fs:172`) compares an
explicit `--lifecycle spec-kit` scaffold against the **no-flag default** scaffold of the *same
template* — the explicit-vs-implicit-default invariant — **not** a frozen historical snapshot. A new
ungated source emits identically under both invocations, so GV-3 stays green. The thing that *does*
change is the symbology file's content in a spec-kit app (a 506-byte dangling `../../../src/...`
stub → the 12788-byte real skill) — which is the entire intended point of the feature and is gated
by no test against a frozen baseline. See [research.md](./research.md) R1.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (repo standard).

**Primary Dependencies**: `dotnet new` template engine (`.template.config/template.json`); Expecto
(tests); the in-repo `Rendering.Harness` skill-parity tool. No new dependencies.

**Storage**: N/A — file/manifest content only (template sources, skill wrapper `SKILL.md` files).

**Testing**: Expecto. New/changed tests in `tests/Package.Tests/` (emit + lifecycle) and
`tests/Rendering.Harness.Tests/` (parity). The env-gated live validator
`scripts/validate-lifecycle-template.fsx` provides the report-backed evidence.

**Target Platform**: Linux dev/CI; GL-free, deterministic (no `dotnet new` required for the
env-free assertions; live scaffolds are env-gated).

**Project Type**: F# UI-framework + `dotnet new` template repo. Two distinct surfaces are touched:
(a) the **generated-app** surface (template ship list → what a scaffolded product receives) and
(b) the **framework-repo** wrapper surface (the repo-root `.claude/skills/` + `.agents/skills/`
trees the parity harness audits).

**Performance Goals**: N/A.

**Constraints**: Mirror the existing six-product-skill pattern exactly (two-entry per-surface,
profile-gated, no `lifecycle` clause). The parity fix MUST preserve the ant/package/fixture
self-exposure paths and MUST NOT regress the six already-delivered product skills.

**Scale/Scope**: ~2 manifest entries, 2 wrapper `SKILL.md` files, 1 small harness predicate change,
3–4 test edits/additions, 1 validator-line edit, 1 cross-repo contract/registry update + republish.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Change classification: Tier 1 (contracted change).** It changes the `fs-gg-ui-template` ship-list
contract (consumers receive new content), alters parity-harness behavior covered by the Feature 168
spec, reverses a Feature 219 recorded decision, and updates the cross-repo `fs-gg-ui-template`
contract entry.

| Principle | Status | Notes |
|---|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | **PASS** | FSI sketching is N/A — no new public F# surface is introduced (the change is to a template manifest, two wrapper Markdown files, and one *private* harness function). The behavior is still driven test-first: the parity test (US3) and the emit test (US1) fail before and pass after. |
| II. Visibility lives in `.fsi` | **PASS** | No new/changed public module. `missingWrapperFindings` is `let private`; no `src/` public surface or `.fsi`/surface-area baseline changes. |
| III. Idiomatic simplicity | **PASS** | The parity fix is a single guarded boolean (`not isProductSkill && names.Contains canonicalName`); manifest/wrapper entries are copies of the existing pattern. No clever F# features. |
| IV. Elmish/MVU boundary | **N/A** | No new stateful or I/O workflow. The parity harness is pure analysis over already-loaded entries; emission is template-engine tooling. |
| V. Test evidence is mandatory | **PASS** | Each behavior change gets a fail-before/pass-after test: emit test (SC-001/SC-005), parity-blind-spot test (SC-003), regression assertion (SC-004/FR-006), plus the live validator report (GV-3/emit). No synthetic substitution needed — all paths use real files/real scaffolds. |
| VI. Observability and safe failure | **PASS** | The change *increases* observability: it makes a previously-silent missing-wrapper hole a loud parity finding. No critical runtime path added. |

**Gate result: PASS — no violations.** Complexity Tracking is empty.

## Project Structure

### Documentation (this feature)

```text
specs/223-deliver-symbology-skill/
├── plan.md              # This file
├── research.md          # Phase 0 output — R1 (GV-3), R2 (profile set), R3 (parity fix), R4 (219/204 test deltas), R5 (cross-repo)
├── data-model.md        # Phase 1 output — entities, the profile→skill matrix delta, parity predicate state
├── quickstart.md        # Phase 1 output — runnable validation scenarios mapping SC-001..SC-006
├── contracts/
│   ├── template-ship-list.md     # the symbology source-pair contract (manifest shape)
│   ├── consumer-wrapper.md       # the fs-gg-product-symbology wrapper contract (both surfaces)
│   └── parity-missing-wrapper.md # the narrowed missing-wrapper rule contract
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
.template.config/template.json          # EDIT — add the fs-gg-symbology product-skill source pair
                                         #        (.agents/skills/ + .claude/skills/), profile-gated to
                                         #        the fs-gg-scene set, NO lifecycle clause

.claude/skills/fs-gg-product-symbology/  # NEW  — Claude-active consumer wrapper (mirror fs-gg-product-scene)
└── SKILL.md
.agents/skills/fs-gg-product-symbology/  # NEW  — Codex-active consumer wrapper (mirror fs-gg-product-scene)
└── SKILL.md

template/product-skills/fs-gg-symbology/ # UNCHANGED — already content-complete (SKILL.md 12788B + reference.fsx)
.claude/skills/fs-gg-symbology/          # UNCHANGED — framework wrapper keeps the bare canonical name
.agents/skills/fs-gg-symbology/          # UNCHANGED — framework wrapper keeps the bare canonical name

tools/Rendering.Harness/SkillParity.fs   # EDIT — narrow missingWrapperFindings: a product-skill
                                         #        canonical is satisfied ONLY by its fs-gg-product-* alias

tests/Rendering.Harness.Tests/
└── Feature223SymbologyParityTests.fs    # NEW  — US3: bare-name wrapper does NOT satisfy a product
                                         #        skill; alias does; the six are regression-clean
tests/Package.Tests/
└── Feature219EmitFrameworkSkillsTests.fs # EDIT — G-EMIT matrix gains symbology rows; G-NODANGLE-SYMB
                                          #        unwired set → empty; report token not-vendored→vendored
tests/Package.Tests/
└── Feature204LifecycleTemplateTests.fs  # EDIT (if needed) — bump framework-skill/product counts; GV-3 unchanged
scripts/validate-lifecycle-template.fsx  # EDIT — `symbology: not-vendored` → `symbology: vendored`

# Cross-repo (FS-GG/.github) — handled via the cross-repo-coordination skill, not in this repo:
#   registry/dependencies.yml + docs/registry/compatibility.md  # fs-gg-ui-template contract status
#   Coordination board item #35                                 # close / mark Done
```

**Structure Decision**: No new project or module. The feature edits one template manifest, adds two
Markdown wrappers, makes one small change to a private harness predicate, and updates the tests and
validator that encoded the now-reversed 219 "not-vendored" decision. The two surfaces (generated-app
ship list vs. framework-repo wrapper tree) are addressed by distinct artifacts — the source pair
delivers content (US1), the wrappers + parity fix make it reachable and keep the harness honest
(US2/US3).

## Complexity Tracking

> No constitution violations. Section intentionally empty.
