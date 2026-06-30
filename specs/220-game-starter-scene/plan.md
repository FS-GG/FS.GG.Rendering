# Implementation Plan: Replaceable Game Starter Scene

**Branch**: `220-game-starter-scene` | **Date**: 2026-06-30 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/220-game-starter-scene/spec.md`

## Summary

The game/rendering scaffold ships a **controls showcase** as its default starter, and the
durable governance test pins a specific UI family's launch call so that replacing the starter
at the normal entrypoint fails a governance gate (the consumer had to hide a real Pong behind
a `-- pong` flag). This plan makes the game starter genuinely replaceable as the *default*:

- **Add an explicit `game` profile** (`template/profiles/game.yml`) that becomes the
  game/rendering provider's default starter and ships a **minimal Pong-style MVU skeleton**
  (`Model`/`Msg`/`update`/`view` + tick) clearly designated "replace me".
- **Thread `game` through every profile conditional** in the shared `template/base` product
  source so the latent `//#else` "game family" branch becomes a real, exercised path.
- **Keep the controls showcase reachable** via the explicit `app` profile (FR-006) and keep
  `headless-scene` / `governed` / `sample-pack` **byte-identical** (FR-007) — verified by a
  generated-output diff, not by inspection alone.
- **Relax the governance spine to be UI-family agnostic** at the default entrypoint: the
  game-branch launch assertion must be satisfiable by the minimal skeleton AND survive a Pong
  swap (FR-002/FR-003/FR-008).
- **Align `scaffold-map.md` to reality** (today it describes a "scaffold game model" the
  default does not ship — FR-005).
- **Coordinate the `fs-gg-ui-template` contract change** with SDD (scaffold-provider default
  selection) and Templates (governance expectations) per the cross-repo protocol (FR-009),
  and republish the template at a coherent version.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> The reachability claims below (which profiles hit which `//#else` branch, whether
> `sample-pack` currently launches via `Viewer.runApp`, whether the game `//#else` governance
> branch is truly unexercised) are read from source and **provisional**. `/speckit-tasks` MUST
> schedule an **early profile-matrix instantiation probe** in the Foundational phase — generate
> each profile (`app`, `headless-scene`, `governed`, `sample-pack`, and the new `game`), build +
> test each, and capture the actual generated output — *before* authoring the game branches, to
> confirm or replace these hypotheses.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (constitution-pinned).

**Primary Dependencies**: `FS.GG.UI.*` packages consumed by the generated product —
`Scene`, `SkiaViewer`, `Elmish`, `KeyboardInput`, `Layout`, `Controls`, `Controls.Elmish`,
`DesignSystem`, `Themes.Default`, `Testing`. Build engine `FS.GG.UI.Build` (resolved via
`FsGgUiVersion`). Test framework: Expecto.

**Storage**: N/A (text source + readiness evidence files).

**Testing**: Expecto suites in `template/base/tests/Product.Tests/`
(`GovernanceTests.fs` = durable/model-agnostic, `BehaviorTests.fs` = replaceable scaffold
behavior), plus template pack/instantiate/build/test validation.

**Target Platform**: Desktop (SkiaSharp over OpenGL); generated products are cross-platform
.NET console apps hosting the viewer.

**Project Type**: Templating / scaffold contract. The deliverable is the
**`fs-gg-ui-template`** template under `template/` (profiles, capabilities, the shared
`template/base` product source with `//#if (profile == …)` preprocessor conditionals, and
docs), not a framework package.

**Performance Goals**: N/A for correctness; the minimal game must render a live interactive
frame at the default entrypoint (no fixed perf budget introduced).

**Constraints**:
- `headless-scene` / `governed` / `sample-pack` generated output and tests MUST be unchanged
  (FR-007) — enforced by a pre/post generated-output diff.
- No governance test may be edited to make a replacement game pass (FR-002).
- No new launch flag may be required to surface the real game (FR-008).
- Cross-repo: the `fs-gg-ui-template` contract change must be sequenced so SDD/Templates are
  not silently broken (FR-009).

**Scale/Scope**: Bounded to the interactive game/app starter path. Touch set (this repo):
`template/profiles/game.yml` (new); `template/capabilities.yml`; the six `template/base`
product files (`Model.fs`, `View.fs`, `LayoutEvidence.fs`, `WindowOptions.fs`,
`EvidenceCommands.fs`, `Program.fs`); `Product.fsproj`; `Product.Tests` (`GovernanceTests.fs`,
`BehaviorTests.fs`); `template/base/docs/scaffold-map.md`; template version metadata. Plus a
cross-repo coordination issue + ADR for the contract change.

### Change Classification

**Tier 1 (contracted change)** — alters the `fs-gg-ui-template` cross-repo contract (default
starter family, new `game` profile, relaxed governance assertion) and the observable generated
output. Requires spec, plan, contract doc, test evidence, docs (`scaffold-map.md`),
version bump, and cross-repo coordination. **No framework package `.fsi` / surface baseline
changes** — the change is confined to the template's generated *product source*, profiles, and
docs; the `FS.GG.UI.*` package public surfaces are untouched.

## Constitution Check

*GATE: must pass before Phase 0; re-checked after Phase 1 design.*

| Principle | Status | Notes |
|---|---|---|
| I. Spec → FSI → Semantic Tests → Implementation | PASS | Spec exists; the game skeleton's shape is exercised through `Product.Program`/`Product.Model` the same way `BehaviorTests.fs` already drives the product (the generated product is its own FSI audience). No new framework public surface is drafted. |
| II. Visibility in `.fsi` | PASS / N/A | No framework `.fs`/`.fsi` modules change. Generated product source has no curated `.fsi` (it is app source, not a packaged module) and adds no access modifiers. |
| III. Idiomatic simplicity | PASS | The game skeleton is plain records + a pure `update` + a `tick` subscription — simpler than the controls showcase it parallels. Mutation/recursion not required; any use disclosed at the site. |
| IV. Elmish/MVU boundary | PASS | The game starter is stateful (tick-driven ball) → modeled as `Model`/`Msg`/`update`/`AdapterCommand` + `subscriptions`, interpreted at the host edge via `generatedHost` (`Viewer.runApp`), mirroring the existing scaffold's MVU boundary. |
| V. Test evidence mandatory | PASS | New game-branch `BehaviorTests` fail before / pass after; `GovernanceTests` stay green across a Pong swap (the SC-001/SC-004 acceptance). Any synthetic evidence keeps its existing `// SYNTHETIC:` disclosure + `Synthetic` test-name token. |
| VI. Observability & safe failure | PASS | The game launch reuses the existing diagnostic/desktop-session reporting in `Program.fs`; unsupported-host classification is preserved (no new silent-failure paths). |

**Result: PASS — no Complexity Tracking entries required.** (Re-evaluated post-Phase 1: still PASS — see Phase 1 note.)

## Project Structure

### Documentation (this feature)

```text
specs/220-game-starter-scene/
├── plan.md              # This file
├── research.md          # Phase 0 — design decisions (profile vs re-aim; skeleton shape; conditional grouping)
├── data-model.md        # Phase 1 — game starter Model/Msg + durable-spine entities
├── quickstart.md        # Phase 1 — the SC-001/SC-004 swap-to-Pong validation walkthrough
├── contracts/
│   └── fs-gg-ui-template-contract.md   # Phase 1 — the template contract surface + governance game-branch contract
└── tasks.md             # Phase 2 (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
template/
├── profiles/
│   ├── app.yml                  # CHANGED: re-described as the explicit controls-showcase option (no longer "the" default)
│   ├── game.yml                 # NEW: game/rendering default starter; selects the game family
│   ├── headless-scene.yml       # UNCHANGED (FR-007)
│   └── sample-pack.yml          # UNCHANGED (FR-007)  — NOTE: `governed` has no profile file; it is selected via capabilities.yml (T004 confirms instantiation)
├── capabilities.yml             # CHANGED: register `game` in capability `profiles:` lists + defaultApp note
└── base/
    ├── src/Product/
    │   ├── Model.fs             # CHANGED: add game-family branch (minimal Pong Model/Msg/update); controls stays for app+sample-pack
    │   ├── View.fs              # CHANGED: add game-family `view` (Scene playfield); controls view unchanged for app+sample-pack
    │   ├── LayoutEvidence.fs    # CHANGED (re-point): game HUD=score / gameplay=playfield regions for the game branch
    │   ├── WindowOptions.fs     # CHANGED (conditional only): include `game` in the package-gated branch
    │   ├── EvidenceCommands.fs  # CHANGED: game branch host wiring (generatedHost/view re-point); interactiveHost stays app-only
    │   ├── Program.fs           # CHANGED (conditional grouping): game launch via Viewer.runApp; sample-pack stays on runApp; app stays runInteractiveApp
    │   └── Product.fsproj        # CHANGED: thread `game` into the `(app || sample-pack)` package/compile conditionals
    ├── tests/Product.Tests/
    │   ├── GovernanceTests.fs   # CHANGED: game `//#else` assertions made satisfiable by skeleton AND a Pong swap (FR-003)
    │   └── BehaviorTests.fs     # CHANGED: replaceable game-branch behavior tests (skeleton); controls/app/sample-pack unchanged
    └── docs/
        └── scaffold-map.md      # CHANGED: replaceable-vs-durable description aligned to the real game-starter edit set (FR-005)

docs/product/decisions/                # NEW ADR: fs-gg-ui-template default-starter change (cross-repo)
```

**Structure Decision**: This is a **templating contract change**, not a new project. All work
lives under `template/` plus the cross-repo ADR. The shared `template/base` product source is
specialized per profile by `//#if (profile == …)` preprocessor conditionals; the core design
move is introducing a `game` profile and making its content/launch/governance branches real
while pinning the other profiles' output identical.

## Key Design Decisions (detail in research.md)

1. **New `game` profile, not a re-aim of `app`.** The template machinery already encodes a
   latent "game family" as the `//#else` of `profile == "app"`. A dedicated `game` profile turns
   that latent branch into a real, exercised path (resolving the spec's "unreachable `//#else`"
   edge case) with the smallest coherent contract change, and keeps `app` as the explicit
   controls option (FR-006). "Which profile is the *default*" is the cross-repo coordination
   point with the SDD scaffold-provider (FR-009).
2. **Two distinct conditional groupings — content vs launch — must each pin sample-pack.**
   The content split (game skeleton vs controls model/view) is `//#if (profile == "game")` /
   `//#else` (controls = app + sample-pack). The launch split keeps `app → runInteractiveApp`
   and `game + sample-pack → Viewer.runApp` so **sample-pack's launch call is unchanged**.
   These groupings differ; getting them wrong silently regresses sample-pack (FR-007). Verified
   by generated-output diff.
3. **Minimal Pong-style skeleton.** `Model` = ball position/velocity + two paddles + score;
   `Msg` = `Tick` / paddle key moves; `update` is a pure step; `view` draws the playfield as a
   `Scene`; `subscriptions`/`tick` drive motion. It is a valid, green product unchanged (edge
   case) and the canonical thing the developer replaces with their own game.
4. **Governance relaxation is targeted.** Only the assertion pinning a *specific UI family's*
   launch call is retargeted; the durable evidence/structure/discoverability scans stay. The
   game `//#else` branch must assert nothing the minimal skeleton (or a Pong swap) cannot
   satisfy — the SC-004 invariant.

## Cross-Repo Coordination (FR-009)

The `fs-gg-ui-template` contract is consumed by **SDD** (scaffold-provider — which profile is
the default, profile enumeration) and **Templates** (governance expectations on generated
output). Per the `cross-repo-coordination` skill: file a Coordination-board issue capturing the
default-starter change + new `game` profile, record an **ADR** in `docs/product/decisions/`,
update the contract/compatibility registry entry for `fs-gg-ui-template`, and sequence the
template republish (coherent version) so downstream is not silently broken. Tracked alongside
sibling Coordination item **#32**.

## Complexity Tracking

> No constitution violations — table intentionally empty.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |
