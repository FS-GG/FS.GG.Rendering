# Quickstart / Verification: fs-gg-layout consumer product-skill

This is the runnable recipe that proves the feature. It rides the **existing** repo gates plus a live scaffold observation — no new test project. Run from repo root.

## Prerequisites

- The `227-layout-product-skill` branch checked out.
- The template graph restorable/buildable (if the stale FSharp.Core lockfile blocks restore, apply the NU1403 lockfile workaround from memory).

## 1. Static wiring & catalog gates (fast, GL-free)

```sh
# Feature 219 — per-profile emission matrix: app+game must include fs-gg-layout (8 skills), sources >=18
dotnet test tests/Package.Tests --filter Feature219EmitFrameworkSkills

# Feature 204 — framework-source floor >=18
dotnet test tests/Package.Tests --filter Feature204LifecycleTemplate

# Feature 224 — catalog currency: fs-gg-layout row resolves, nothing dangles
dotnet test tests/Package.Tests --filter Feature224SkillCatalogCurrency

# Feature 225 — product-skill leak guard: discovery includes fs-gg-layout, zero framework-token findings
dotnet test tests/Package.Tests --filter Feature225ProductSkillVocabulary
```

**Expected**: all four green. (The documented Debug-lane `FS.GG.UI.Build`-not-built environmental red is pre-existing and green on CI — disclose it if seen, do not treat it as this feature's failure.)

## 2. Skill-parity gate + report regen

```sh
# Regenerate the parity report and fail on High+ findings (writes docs/reports/skills-parity.md)
dotnet fsi scripts/check-agent-skill-parity.fsx --fail-on High
```

**Expected**: overall status `Passed`, **zero** findings; `fs-gg-layout` inventoried as canonical with its `fs-gg-product-layout` wrapper (canonical count +1, wrapper count +2 vs the pre-feature report).

## 3. Live scaffold observation (the real artifact)

Prove the produced product carries the skill under both agent surfaces and every lifecycle — the Feature 175 lesson (a green test can hide an unshipped skill).

```sh
# For each of app and game, and each lifecycle spec-kit|sdd|none:
dotnet new fs-gg-ui --profile app  --lifecycle sdd  -o /tmp/out-app-sdd
ls /tmp/out-app-sdd/.agents/skills/fs-gg-layout/SKILL.md /tmp/out-app-sdd/.claude/skills/fs-gg-layout/SKILL.md
# repeat for --profile game and lifecycle spec-kit / none
```

**Expected**: `fs-gg-layout/SKILL.md` present under **both** `.agents/skills/` and `.claude/skills/` for `app` and `game` under all three lifecycles; **absent** for `headless-scene`, `governed`, `sample-pack`. (If `dotnet new` is unavailable in-environment, the env-free Feature 219 derivation from `template.json` in step 1 is the fallback proof; record the substitution in readiness.)

## 4. Content boundary & buildability spot-check

- Read `template/product-skills/fs-gg-layout/SKILL.md`: confirms it documents the consumer region/`LayoutEvidence` surface and the Boundary section bounds out the layout-engine internals (FR-002).
- Confirm every code example corresponds to the layout surface the `app`/`game` starter actually exposes (FR-007) — cross-check names against `template/base/src/Product/LayoutEvidence.fs` and the public `FS.GG.UI.Layout` surface.

## Success mapping

| Spec criterion | Proven by |
|---|---|
| SC-001 skill present, both surfaces, all lifecycles (app+game) | Step 3 (or step 1 fallback) |
| SC-002 absent on headless-scene/governed/sample-pack | Step 3 / Feature 219 exact-set |
| SC-003 catalog currency passes | Step 1 (Feature 224) |
| SC-004 emission matrix 8 skills / floor 18 | Step 1 (Feature 219) |
| SC-005 full 226-gate suite green, no bump | Steps 1–2 + `git diff` shows no version-of-truth edit |
| SC-006 examples match shipped starter surface | Step 4 |

Record each transcript / hand-read under `specs/227-layout-product-skill/readiness/`.
