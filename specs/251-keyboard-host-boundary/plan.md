# Implementation Plan: Surface the Keyboard-Only Host Input Boundary

**Branch**: `251-keyboard-host-boundary` | **Date**: 2026-07-05 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/251-keyboard-host-boundary/spec.md`

## Summary

Surface — where a game author **first wires input** — that the game family's governed default persistent host
(`Viewer.runApp` over `GeneratedAppHost`) is **keyboard-only**: its sole input seam is
`MapKey: ViewerKey -> bool -> 'msg option`, `ViewerKey` carries no mouse/pointer case, and input reaches the product
as `DispatchInput of ViewerKey * isDown`. A mouse-aimed control scheme requires the **different, non-default**
pointer-aware host path (`InteractiveAppHost` / `Controls.Elmish.runInteractiveApp`, whose `MapPointer` seam reads
`ViewerPointerInput`). The deliverable is **documentation/surfacing only**: (1) a comment at the input-wiring site in
the replaceable game-starter `Model.fs` (`profile == "game"` branch — the `paddleForKey` mapping / `ViewerInput`
handler), and (2) a capability-boundary note in the shipped keyboard-input product skill
(`template/product-skills/fs-gg-keyboard-input/SKILL.md`) and its fragment mirror
(`template/fragments/keyboard-input/README.md`). No durable, governance-scanned host wiring changes; no new input
capability; no emitted-host change. A generated-product test asserts the surfaced note is present and accurate, and
the durable spine, evidence tokens, and a starter swap all keep passing. This is the sibling of #138 (the
`Scene`-field-label collision surfaced up front by a collision-safe `Vec2`) — both turn a trap discovered after work
is done into a constraint stated at the point of authoring. Board: #139 → epic #137.

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> The claimed boundary — that the game default host (`runApp`/`GeneratedAppHost`) exposes only `MapKey` (keyboard,
> `'msg option`, no `MapPointer`) while `InteractiveAppHost`/`runInteractiveApp` carries a `MapPointer` pointer seam —
> is corroborated by the shipped `SkiaViewer.fsi` / `KeyboardInput.fsi` public surface (`ViewerKey` has no mouse case;
> `GeneratedAppHost.MapKey` vs `InteractiveAppHost.MapPointer`), by the game starter's keyboard-only `paddleForKey`,
> and by the *Hollow Depths* §2.5 report. It remains provisional until reproduced. `/speckit-tasks` MUST schedule an
> **early live confirmation** in the Foundational phase — scaffold a game product and read the actual emitted host
> surface (`GeneratedAppHost` seams, `ViewerKey` cases) — so the surfaced text is confirmed against the current
> template **before** the note wording is finalized. Because this feature ships no runtime change, "run the app" here
> means *inspect the real generated host contract*, not drive a window; that is the honest end-to-end check for a
> documentation-accuracy claim (FR-006).

## Technical Context

**Language/Version**: F# on .NET `net10.0` (constitution Engineering Constraints). No code-behavior change; the
deliverable is F# source comments + Markdown skill/fragment text.

**Primary Dependencies**: Describes (does not modify) the emitted host contract — `FS.GG.UI.SkiaViewer`
(`GeneratedAppHost.MapKey`, `InteractiveAppHost.MapPointer`, `runApp`, `runInteractiveApp`, `DispatchInput`,
`ViewerPointerInput`) and `FS.GG.UI.KeyboardInput` (`ViewerKey`). No new framework package or public-API surface.

**Storage**: N/A (template content — a source comment + skill/fragment Markdown).

**Testing**: Generated-product test (`tests/Product.Tests/BehaviorTests.fs`, replaceable) asserting the surfaced
boundary is **present** at the game-starter input-wiring site and **accurate** to the emitted host contract (the
keyboard-only `MapKey`/`ViewerKey`, the pointer-aware `MapPointer` alternative). The durable `GovernanceTests.fs`
(source-scan invariants) is unchanged. Template pack→install→instantiate→build→test composition tests exercise the
game profile. No FSI surface is added (no new public API — see Constitution Check II).

**Target Platform**: Cross-platform .NET; the surfaced facts are host-contract facts, independent of whether a GL
window is launched (assertions are pure text/contract checks).

**Project Type**: F# UI framework **template** change (generated-product source + shipped product-skill), delivered
through the `FS.GG.UI.Template` package — the same template/skill/release path as sibling #138 and helper features
246–250.

**Performance Goals**: N/A — no runtime code path changes; output stays byte-identical except the added game-starter
comment and skill/fragment text.

**Constraints**: Documentation only. MUST NOT modify the durable, governance-scanned host wiring
(`Program.fs`, `LayoutEvidence.fs`, `EvidenceCommands.fs`, `WindowOptions.fs`) or the emitted host/seams; MUST NOT
add an input capability; non-game profiles (`app`/`governed`/`headless-scene`) stay byte-identical; the surfaced
claims MUST match the shipped `SkiaViewer`/`KeyboardInput` surface (FR-006).

**Scale/Scope**: One comment block at the game-starter input-wiring site in `Model.fs`; one boundary note in the
keyboard-input product skill + the same note in its fragment README; one generated-product assertion; a skill-manifest
digest regeneration (the keyboard-input skill body changed); a `FS.GG.UI.Template` republish. Coordinated with the org
board (#139 → epic #137). No new skill id.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Spec → FSI → Semantic Tests → Implementation** — PASS (adapted). No new API surface exists to sketch in FSI;
  the feature *describes* an existing surface. The "validate by use" step is satisfied by reading the real emitted
  host contract during Foundational confirmation (the standing assumption), and the semantic test asserts the surfaced
  text against that contract. Tasks order confirm-contract → test → surfacing edits.
- **II. Visibility Lives in `.fsi`** — N/A. No public F# module is added or changed; the deliverable is a source
  comment + Markdown. No `.fsi`/surface-area baseline changes (see Change Classification).
- **III. Idiomatic Simplicity Is the Default** — PASS. No code logic added; a comment and prose. No operators, SRTP,
  reflection, or computation expressions.
- **IV. Elmish/MVU Boundary** — PASS. `update`/`view`/host wiring are untouched; no I/O crosses `update`. The comment
  sits beside the existing pure `paddleForKey`/`ViewerInput` mapping without altering it.
- **V. Test Evidence Is Mandatory** — PASS (planned). The assertion fails before (no surfaced note / an inaccurate
  note) and passes after, and it checks the note against the **real** emitted host contract rather than a synthetic
  string — real evidence preferred per Principle V.
- **VI. Observability and Safe Failure** — PASS. The surfacing itself is the observability improvement: it makes a
  hard host capability boundary visible at the authoring site instead of failing late when an author reaches for an
  absent mouse case. No silent failure introduced.

**Change Classification**: **Tier 2 for the `FS.GG.UI.*` libraries** (no public API surface added or changed — the
`SkiaViewer`/`KeyboardInput` contracts are *described*, not modified) **and a template-content change** to the
`FS.GG.UI.Template` product contract (a starter comment + a shipped keyboard-input skill/fragment note), validated by
the template composition/governance tests and shipped via a template republish. No `.fsi`/baseline churn on the
framework. Rationale recorded in [research.md](./research.md) (Decision 1).

**Gate result**: PASS — no violations; Complexity Tracking left empty.

## Project Structure

### Documentation (this feature)

```text
specs/251-keyboard-host-boundary/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 output — boundary-facts + placement + wording decisions
├── data-model.md        # Phase 1 output — the host input-contract facts being surfaced (no new types)
├── quickstart.md        # Phase 1 output — confirm-the-boundary + verify-the-surfacing run guide
├── contracts/
│   └── boundary-note-surface.md  # Phase 1 output — required content + assertions for the surfaced note
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
template/base/src/Product/
└── Model.fs             # EDIT (replaceable) — game branch (`profile == "game"`) only: a comment at the
                         #   input-wiring site (near `paddleForKey` / the `ViewerInput` handler) stating the
                         #   default host is keyboard-only and naming the pointer-aware interactive host path.
                         #   No logic change; no touch to the app/governed/headless-scene branches.

template/product-skills/fs-gg-keyboard-input/
└── SKILL.md             # EDIT — add a "Capability boundary" note: game default host keyboard-only (MapKey /
                         #   ViewerKey, no MapPointer); mouse-aimed input needs InteractiveAppHost / runInteractiveApp.

template/fragments/keyboard-input/
└── README.md            # EDIT — mirror the same capability-boundary note (fragment source parity, FR-004).

template/base/tests/Product.Tests/
└── BehaviorTests.fs     # EDIT (replaceable) — assert the surfaced note is present at the game input-wiring site and
                         #   accurate to the emitted host contract (keyboard-only MapKey/ViewerKey; MapPointer path).

# Skill manifest (regenerated because a shipped skill body changed)
template/**/skill-manifest.json                     # REGEN — digest for fs-gg-keyboard-input (scripts/generate-skill-manifest.fsx)

# Authoring-surface reinforcement (confirm during tasks; surface the boundary where authors also look)
template/base/docs/scaffold-map.md                  # OPTIONAL EDIT — note the keyboard-only default-host boundary by
                                                    #   the input/model-swap guidance (confirm need in research)
```

**Structure Decision**: The primary surface is the **game-starter `Model.fs` input-wiring site** (the file an author
first edits to wire input), edited **only** inside the `profile == "game"` template branch so the non-game variants
stay byte-identical. The secondary surface is the shipped **keyboard-input product skill** + its **fragment mirror**
(the guidance an author reads when mapping input), kept in parity per FR-004. No `template/base/src/**` framework
library file changes; the durable host spine (`Program.fs` and the governance-scanned files) is not touched — the note
only *points at* the interactive host as the non-default path (FR-002/FR-005). Because a shipped skill body changes,
the skill-manifest digest is regenerated (no new skill id). Delivery is a `FS.GG.UI.Template` republish coordinated on
the org board (#139 → epic #137), following the publish-before-flip protocol like #138.

## Complexity Tracking

> No constitution violations — section intentionally empty.
