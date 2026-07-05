# Phase 0 Research: Surface the Keyboard-Only Host Input Boundary

**Feature**: 251-keyboard-host-boundary | **Date**: 2026-07-05

This feature has no external-technology unknowns; the research resolves (a) the *exact host-contract facts* to
surface, (b) *where* to surface them, and (c) the *change classification*. All decisions are grounded in the shipped
public surface (`SkiaViewer.fsi`, `KeyboardInput.fsi`) and the game-starter template, not memory.

## Decision 1 — Change classification: Tier 2 (libraries) + template-content

**Decision**: Classify as **Tier 2** for the `FS.GG.UI.*` libraries (no public API added/changed) plus a
**template-content** change to the `FS.GG.UI.Template` product contract (a starter comment + a shipped keyboard-input
skill/fragment note). No `.fsi`/surface-area baseline churn.

**Rationale**: The deliverable *describes* the existing host input contract; it neither modifies `SkiaViewer` /
`KeyboardInput` nor adds an emitted type. It is the same posture as sibling #138 (template-content, no library surface
change) and helper features 246–250.

**Alternatives considered**: (a) Add a mouse/pointer seam to `GeneratedAppHost` — rejected: that is a real capability
change (Tier 1-ish, larger), explicitly out of scope for issue #139 ("documented/surfaced", not "added"). (b) Rewire
the game starter onto `InteractiveAppHost` — rejected: touches durable host wiring and changes the default host; a
separate, larger feature.

## Decision 2 — The precise boundary facts to surface (accuracy, FR-006)

**Decision**: Surface these facts, verbatim to the shipped surface:

- The game family's governed default persistent host is **`Viewer.runApp`** over **`GeneratedAppHost`**; its **only**
  input seam is **`MapKey: ViewerKey -> bool -> 'msg option`** — keyboard only.
- **`ViewerKey`** enumerates keyboard keys only — `ArrowLeft/Right/Up/Down`, `Enter`, `Space`, `Escape`, `Backspace`,
  `Letter of char`, `Digit of int`, `Function of int`, `Unknown of raw` — with **no** mouse-button or pointer case.
- At the host boundary, keyboard input is delivered as **`DispatchInput of ViewerKey * isDown`**.
- Mouse/pointer input exists only on the **pointer-aware** host: **`InteractiveAppHost`** (features 085/092), driven
  by **`Controls.Elmish.runInteractiveApp`**, carrying **`MapPointer: ViewerPointerInput -> Size -> 'model -> 'msg list`**
  alongside its own `MapKey` (which there returns `'msg list`). This is a **different, non-default** host path from the
  game family's `runApp`/`GeneratedAppHost`.

**Rationale**: These are the exact seams a mouse-aim author reaches for; naming them (and the real alternative) makes
the note a signpost, not a dead end (FR-002). Source of truth:
- `template/base/docs/api-surface/KeyboardInput/KeyboardInput.fsi` — `ViewerKey` cases (no mouse case).
- `template/base/docs/api-surface/SkiaViewer/SkiaViewer.fsi` — `DispatchInput of ViewerKey * isDown` (~L480),
  `GeneratedAppHost.MapKey: ViewerKey -> bool -> 'msg option` (~L534), `ViewerPointerInput` (~L556),
  `InteractiveAppHost.MapPointer` (~L590), `runApp` (~L631), and the comment noting `GeneratedAppHost.MapKey` is
  DELIBERATELY `'msg option` (backs the non-interactive `runApp` path) while the interactive host is `'msg list`.
- `template/base/src/Product/Program.fs` — the game host launches through `Viewer.runApp` (keyboard path).

**Alternatives considered**: Surface only "no mouse support" without naming the interactive host — rejected: fails
FR-002 (leaves the author at a dead end) and is less accurate.

## Decision 3 — Placement: the game-starter input-wiring site + the keyboard-input skill/fragment

**Decision**: Two surfaces.
1. **`template/base/src/Product/Model.fs`**, inside the **`profile == "game"`** template branch only, as a comment at
   the input-wiring site — adjacent to `paddleForKey` (the `ViewerKey -> command` mapping, ~L135) and/or the
   `ViewerInput of ViewerKey * isDown` handler (~L209), which is exactly where an author first wires input.
2. **`template/product-skills/fs-gg-keyboard-input/SKILL.md`** — a "Capability boundary" note; mirrored in
   **`template/fragments/keyboard-input/README.md`** (fragment source parity, FR-004).

**Rationale**: The `Model.fs` game branch is the file an author first opens to wire input (it already carries the
"Replace this mapping when you swap in your own game" comment at `paddleForKey`), so the boundary is unmissable at the
edit site (US1). The keyboard-input skill is the guidance an author reads when mapping input (US2). Editing only the
`profile == "game"` branch keeps `app`/`governed`/`headless-scene` byte-identical (FR-007).

**Open item for tasks**: The shipped `fs-gg-keyboard-input/SKILL.md` currently scopes itself to the **`app` profile**
("Use this skill for product keyboard handling in the `app` profile"). Tasks MUST confirm whether the boundary note
belongs (a) in that skill regardless of its stated scope (the boundary is a *host* fact relevant to any keyboard
author), (b) with a scope-widening tweak, and whether the game author actually reads a different keyboard skill. The
`Model.fs` comment (surface 1) is authoritative and unaffected by this question; surface 2's exact home is confirmed
in the Foundational phase.

**Alternatives considered**: Put the note only in the skill — rejected: an author editing `Model.fs` may never open
the skill; US1 requires the in-file comment. Put it only in `Program.fs` — rejected: `Program.fs` is durable,
governance-scanned host wiring the author is told not to touch, and is not where input is *mapped*.

## Decision 4 — Test strategy: assert present + accurate, against the real contract

**Decision**: A generated-product assertion (in the replaceable `BehaviorTests.fs`) that (a) the game-starter input
site carries the boundary note (present), and (b) the surfaced claim matches the emitted host contract — e.g. the
generated `ViewerKey` has no mouse/pointer case and the generated `GeneratedAppHost` exposes `MapKey` but no
`MapPointer`, whereas `InteractiveAppHost` does. Prefer asserting against the real generated surface over matching a
synthetic string (Principle V).

**Rationale**: Keeps the note honest over time (FR-006): if a future change adds a pointer seam to the default host,
the accuracy assertion fails and forces the note to be updated. A pure text-presence check alone would rot silently.

**Alternatives considered**: Only a string-contains check on the comment — rejected: rots when the contract changes.
Only a contract check with no presence check — rejected: doesn't prove the author actually meets the note (US1).

## Decision 5 — Durable spine, evidence tokens, starter swap unchanged

**Decision**: Touch no durable file (`Program.fs`, `LayoutEvidence.fs`, `EvidenceCommands.fs`, `WindowOptions.fs`),
no evidence token, and keep the starter-swap contract intact — the comment lives in the replaceable starter and may be
removed by a swap without breaking the spine.

**Rationale**: FR-005/FR-007 and the standing scaffold-map contract. This feature is additive documentation on
replaceable/authoring surfaces only.

## Summary of resolved unknowns

| Unknown | Resolution |
|---|---|
| Classification | Tier 2 libraries + template-content (Decision 1) |
| Exact facts to surface | `runApp`/`GeneratedAppHost.MapKey`/`ViewerKey` (no mouse) vs `InteractiveAppHost.MapPointer`/`runInteractiveApp` (Decision 2) |
| Where to surface | game-branch `Model.fs` input-wiring site + keyboard-input skill + fragment mirror (Decision 3) |
| Skill scope question | flagged for Foundational-phase confirmation (Decision 3, open item) |
| Test strategy | present + accurate-to-contract assertion (Decision 4) |
| Durable/evidence/swap impact | none (Decision 5) |
