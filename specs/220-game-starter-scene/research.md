# Phase 0 Research — Replaceable Game Starter Scene

All "NEEDS CLARIFICATION" from Technical Context are resolved below. Reachability findings are
read from source and flagged where an **empirical probe** must confirm them before authoring.

## Decision 1 — New `game` profile vs. re-aiming the existing default

**Decision**: Introduce an explicit **`game` profile** (`template/profiles/game.yml`) as the
game/rendering provider's default starter; keep **`app`** as the explicit controls-showcase
option.

**Rationale**:
- The shared `template/base` source already encodes a latent "game family" as the `//#else` of
  `profile == "app"` in `Program.fs`, `EvidenceCommands.fs`, `GovernanceTests.fs`, and
  `BehaviorTests.fs`. A dedicated `game` profile turns that latent branch into a **real,
  exercised path**, directly resolving the spec edge case ("the existing `//#else` game-family
  branch, currently unreachable by any profile, becomes a real, exercised path").
- Smallest coherent contract change that satisfies FR-006 (controls stays reachable) and FR-007
  (other profiles untouched): `app` keeps producing the controls showcase; nothing about
  `headless-scene`/`governed`/`sample-pack` needs to move.
- "Which profile is the *default*" is a scaffold-provider concern owned by **SDD**, so the
  default flip is naturally the cross-repo coordination point (FR-009) rather than a hidden
  in-repo behavior change.

**Alternatives considered**:
- *Re-aim `app` to emit the game skeleton, demote controls to a flag.* Rejected: inverts the
  meaning of every existing `profile == "app"` conditional (controls↔game), maximizing churn and
  the risk of silently flipping `sample-pack` (which rides several `app || sample-pack`
  conditions); harder to keep FR-007 byte-identical.
- *A runtime `-- game` / `-- controls` flag instead of a profile.* Rejected: that is the exact
  hidden-flag anti-pattern the spec removes (FR-008); the selection belongs at scaffold time.

## Decision 2 — Conditional grouping that keeps non-game profiles byte-identical

**Decision**: Use **two distinct profile groupings**, each pinning `sample-pack` to its current
behavior:
- **Content** (Model/View, the game skeleton vs the controls model): `//#if (profile == "game")`
  → game skeleton; `//#else` → controls (covers `app` + `sample-pack`, unchanged).
- **Launch host** (Program.fs default branch): `app → ControlsElmish.runInteractiveApp`;
  `game` **and** `sample-pack` → `Viewer.runApp viewerOptions generatedHost`. Today `sample-pack`
  already falls into the `//#else` of `profile == "app"` (i.e. `Viewer.runApp`), so grouping
  `game || sample-pack` on `runApp` leaves `sample-pack`'s emitted launch call unchanged.
- **Package/compile gates** (`Product.fsproj`, `WindowOptions.fs`): extend every existing
  `(profile == "app" || profile == "sample-pack")` gate to `(… || profile == "game")` so the
  game profile pulls in `SkiaViewer`/`Elmish`/`KeyboardInput`/`Layout`/`Controls`/
  `Controls.Elmish`/`DesignSystem`/`Themes.Default` + `WindowOptions.fs` it needs.

**Rationale**: The content split and the launch split have **different** correct groupings.
Reusing the single `profile == "app"` boundary for both would move `sample-pack` from `runApp`
to `runInteractiveApp` (a launch-call change) — a silent FR-007 regression. Separating them and
pinning `sample-pack` on each axis is what keeps it identical.

**Empirical probe (MUST run before authoring — see plan Foundational phase)**: instantiate each
profile and diff generated output. Confirm: (a) `sample-pack` currently emits `Viewer.runApp` in
the default branch and the controls Model/View; (b) `sample-pack` references the controls
packages; (c) whether `sample-pack` compiles/runs `Product.Tests` and therefore which governance
branch it hits. The grouping above is correct **iff** the probe confirms these; otherwise adjust
the `sample-pack` pinning to whatever keeps its output byte-identical.

## Decision 3 — Shape of the minimal game skeleton

**Decision**: A **minimal Pong-style MVU skeleton** in the game branch:
- `Model` = ball center + velocity, left/right paddle positions, score(s), playfield size, tick
  count, last input. (Record-label collision note: Scene literals use `X/Y/Width/Height`; the
  game model avoids those bare labels or qualifies them — per `fs-gg-scene` pitfalls and the
  scaffold-map pre-design pointer.)
- `Msg` = `Tick` | paddle-move (Up/Down per side) | `NoOp`; keyboard mapped via `mapKey`.
- `update` = pure step: integrate ball, clamp/bounce on walls + paddles, update score, advance
  paddles on input. No I/O inside `update`.
- `view : Model -> SceneNode` = draw playfield border, ball, paddles, and a score HUD using
  `FS.GG.UI.Scene` primitives (`Rectangle`/`Text`/`Group`).
- `subscriptions` + `tick` (≥16ms cadence) drive motion so the unmodified default is a live,
  moving, valid product (edge case: "keeps the default unchanged → still green").

**Rationale**: Pong is the spec's canonical acceptance journey (SC-001/SC-004). The skeleton is
deliberately smaller than the controls showcase, depends only on `Scene` + viewer host + keyboard
(no Controls widget tree required for the playfield itself), and is the obvious "replace me" unit
acting on the developer-owned `Model`/`View` seam. It must satisfy every game-branch governance
assertion **and** keep satisfying them after a developer swaps in their own Pong.

**Alternatives considered**: A non-interactive static "game-ish" scene (rejected — fails the
"runnable interactive scene" of FR-001 and SC-002's live-entrypoint claim); reusing the controls
host for the game (rejected — the game family is the keyboard-only `runApp` path by design,
FR-006, and pulling in the pointer/controls host couples the skeleton to widgets it doesn't need).

## Decision 4 — Durable spine: what stays, what re-points

**Decision**: Keep the durable governance spine model-agnostic; only **re-point model-field
reads** in the game branch, and **retarget the one family-specific launch assertion**:
- `LayoutEvidence.fs` (durable, re-point): the game branch maps **HUD region → score strip** and
  **gameplay region → playfield**, keeping the evidence tokens (`hud-region`, `gameplay-region`,
  `measurement-mode`, overlap checks). The existing `//#else` already computes generic
  HUD/gameplay regions; re-point the active-item/movement helpers onto the ball instead of the
  controls cursor.
- `EvidenceCommands.fs` (durable, re-point): `generatedHost.View = view`, `viewerEffectsForModel`,
  the deterministic `--scene-evidence`/`SceneEvidence.render` (`RendererMode =
  "deterministic-scene"`) re-point at the game `view`/`initialModel`; the command surface and
  must-survive tokens stay. `interactiveHost` remains **`//#if (profile == "app")`** only (the
  game family does not use the pointer host).
- `GovernanceTests.fs` (durable): the game `//#else` already asserts
  `Viewer.runApp viewerOptions generatedHost` and the model-agnostic evidence vocabulary — keep
  those; ensure none of them require a controls-only token. The retarget is that the *default
  entrypoint* assertion is the family-appropriate persistent host, not a fixed controls call
  (already structured this way; the work is making the game branch genuinely reachable + green).
- `BehaviorTests.fs` (replaceable): rewrite the game `//#else` to drive the skeleton's
  `update`/`view`/`tick`/`generatedHost` (ball motion, paddle input, score), replacing the
  controls-specific behavior tests. The `//#if (profile == "app")` pointer-click test stays for
  the controls family.

**Rationale**: This is the documented "keep the file + scanned tokens, re-point model fields"
contract from `scaffold-map.md`. The governance file never calls `view`/`update`, so it survives
the swap (the whole point). The behavior file is explicitly the replaceable one.

## Decision 5 — Versioning, docs, and cross-repo sequencing

**Decision**: Treat as **Tier 1** (`fs-gg-ui-template` contract). Bump the template version to a
coherent preview, update `scaffold-map.md` to match the real game-starter edit set (FR-005),
record an **ADR** under `docs/product/decisions/`, and file a Coordination-board issue +
contract-registry update for **SDD** (default profile selection, profile enumeration) and
**Templates** (governance expectations) per the `cross-repo-coordination` skill. Sequence so the
republished template lands before/with downstream consumption. No `FS.GG.UI.*` package `.fsi` or
surface-baseline changes (the change is in generated product source, profiles, and docs only).

**Rationale**: Spec FR-009 + Assumptions require coordinated republish; the constitution's Tier 1
artifact chain applies, minus framework `.fsi`/baseline (none change). Sibling item #32 tracks
the deeper sequencing.

## Open items for `/speckit-tasks`
- Foundational: profile-matrix instantiation probe (confirm Decision 2 reachability) **before**
  authoring game branches.
- Foundational: capture the byte-diff baseline for `headless-scene`/`governed`/`sample-pack` to
  enforce FR-007 as a gate.
- Implementation must end with the SC-001/SC-004 **swap-to-Pong** run (quickstart.md) proving a
  green build + tests with zero governance-test edits and no extra flag.
