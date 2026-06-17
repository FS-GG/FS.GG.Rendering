# Provenance — Games + Productivity Sample Apps (feature 134, G2; FR-013 / research R6)

This sample **adopts and rebrands** material from the archived `EHotwagner/FS-Skia-UI`
games + productivity sample specifications (`docs/testSpecs/Games|Productivity/*`). All
imported identifiers are rebranded **`FS.Skia.UI.*` → `FS.GG.UI.*`**.

## Adopted material

| Item | Source | Adaptation |
|------|--------|------------|
| The curated six (Tetris/Snake/Pong; Kanban/Todo/Calendar) | FS-Skia-UI Games/Productivity archive | A representative slice; the other 16 archived specs are the disclosed `Deferred` backlog (`coverage-backlog.md`, FR-012). |
| Each sample's goal / control scheme / state model | FS-Skia-UI archive (intent) | **Authored here** from the implementation plan §10 description and the live public surface, since the archive text is not in this repo (R6). |
| Per-sample **acceptance outcome** | FS-Skia-UI archive (intent) | **Authored** and pinned as each sample's `ExpectedOutcome`, asserted by the build-outcome + determinism suites. Real deterministic assertions — not mocks. |
| The deterministic seeded-evidence harness | G1 Controls Gallery (feature 123) `ControlsGallery.Core/Evidence.fs` | Ported to `SampleApps.Core/Evidence.fs` and **extended** with the `Outcome` block (R5), so the sample stays package-only. |

## Authored acceptance-outcome disclosure

Because the archived specs are **not present in this repository**, the pinned outcomes are
**authored** from the plan and verified against the deterministic seed-7 run. They are honest
deterministic results, disclosed here:

- **Tetris** — the scripted hard-drops top the stack out (`terminal=game-over`). Real-piece
  play leaves holes, so this seed clears no full lines (`clearedRows=0`, `score=0`); the
  line-clear/scoring **logic** is proven separately by a pure-reducer test.
- **Snake** — advances into the wall (`terminal=collision`), no pellet eaten on this seed
  (`score=0`, `length=3`).
- **Pong** — the slow left paddle is beaten to the target score (`terminal=match-over`,
  `0:3`).
- **Todo / Kanban / Calendar** — seed-independent (no PRNG): committed/rejected/etc. counts
  follow directly from the seeded keyboard+pointer script.

## Authoritative sources where the archive is unavailable

1. `specs/134-sample-apps-g2/plan.md` §10 (the curated set + the 22-spec enumeration) and the
   contracts under `specs/134-sample-apps-g2/contracts/`.
2. The live `src/Controls/Catalog.fs` (`Catalog.supportedControls`) — every coverage control
   id was validated against it, not taken from narrative.

## Authoritative fallbacks (evidence)

Every evidence record carries a **non-empty `notAuthoritativeFor`** (`renderer-vs-desktop-
pixels`, `live-host`, `timing`). A no-GL host degrades-and-discloses
(`provesScreenshot=false`, `fallback=deterministic-state-only`) and never fabricates a frame.
The deterministic state + outcome are the authoritative product; the screenshot is best-effort.

## Rebrand note

No `FS.Skia.UI.*` identifier appears in this sample's source; the framework is consumed
exclusively as the packed `FS.GG.UI.*` packages from `~/.local/share/nuget-local/`.
