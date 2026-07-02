# Implementation Plan: unify control-id schemes onto `Key ?? path`

**Branch**: `232-unify-control-id-schemes` | **Date**: 2026-07-02 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/232-unify-control-id-schemes/spec.md`

## Summary

Three control-id schemes coexist; two of them (`Key ?? path` and `Key ?? Kind`) address the same
lowered `Control<'msg>` tree and disagree for every **unkeyed** control, causing keyboard activation to
dispatch nothing, hover/press to stamp the wrong node, unkeyed same-kind focus stops to collapse, and
transient widgets to declare phantom overlay/focus ids. The fix collapses the `Key ?? Kind` seams onto
the single **`Key ?? path`** scheme already used by layout/hit-test/bindings, threading the structural
`path` through the runtime visual-state and scroll bridges and the Elmish focus seam, giving transient
widgets real keys, and updating the diagnostics/`.fsi` contracts. `RetainedId` (retained-tree identity)
stays out of scope. Full per-site domain analysis and the coordinated **move-groups** are in
[research.md](./research.md).

> **Standing assumption — root-cause hypotheses are unverified until exercised.** The root cause in
> research.md is diagnosed from code, not yet observed running. `tasks.md` schedules an **early
> behavioral smoke** (Foundational phase, before any fix) that reproduces ≥1 symptom through the real
> seams and confirms or replaces the hypothesis. No user-story work builds on an unconfirmed symptom.

## Technical Context

**Language/Version**: F# (.NET) — `FS.GG.UI` library (Controls / Controls.Elmish).

**Primary Dependencies**: internal only — `FS.GG.UI.Layout` (Yoga-backed), no new deps.

**Storage**: N/A (pure in-memory control trees).

**Testing**: Expecto — `tests/Controls.Tests`, `tests/Controls.Elmish.Tests` (+ diagnostics tests).

**Target Platform**: cross-platform .NET; headless render for tests.

**Project Type**: Library (design-system rendering) consumed via `fs-gg-ui`.

**Performance Goals**: preserve at-rest byte-identity and the targeted-repaint touched-node minimality
(Feature 112) — the id-scheme change must not add per-node work at rest or defeat subtree reuse.

**Constraints**: Constitution — every public module keeps its `.fsi`; no access modifiers in `.fs`;
behavior-changing code carries tests that fail before / pass after; changes ship through the normal
`fs-gg-ui` release flow.

**Scale/Scope**: ~6 code seams across 5–7 `src/` files + their `.fsi`, the diagnostics rule, and
targeted test additions/updates. No public *behavioral* API removal expected; `.fsi` doc text updates
plus one internal helper signature (path-aware retained resolver).

## Constitution Check

*GATE: must pass before Phase 0; re-checked after design.*

- **I. Spec → FSI → Semantic Tests → Implementation**: Spec written and validated. `.fsi` deltas
  enumerated in `contracts/` before implementation; semantic tests precede/accompany the `.fs` change
  (tasks are ordered tests-first). **PASS.**
- **II. Visibility in `.fsi`**: The path-aware retained resolver is `internal` (reached via
  `InternalsVisibleTo`, like the existing runtime bridges) → not in `.fsi`. Public `Focus` /
  `ControlRuntime` / widget signatures unchanged except doc text describing the unified scheme; the
  public-surface/ApiCompat gate must stay green. **PASS (no access modifiers added to `.fs`).**
- **III. Idiomatic Simplicity**: The change *reduces* schemes (collapsing B into A) and reuses the
  existing `path` derivation and the existing `authoredControlIds` path-walk — net simplification.
  **PASS.**
- **IV. Elmish is the boundary**: All stateful focus/hover routing stays inside the Controls.Elmish
  adapter; pure `Focus`/`ControlRuntime`/`Control` functions stay pure. **PASS.**
- **V. Test evidence mandatory**: Each FR gets a test that fails on `main` (unkeyed control) and passes
  after. Keyed-control tests must stay green (regression guard). Any test that currently *encodes* the
  unkeyed `Key ?? Kind` bug is updated to the unified scheme with a comment. **PASS.**
- **VI. Observability / safe failure**: The unkeyed-collapse diagnostic is retained and re-pointed to
  genuine authoring ambiguity; no silent behavior. **PASS.**

No violations → Complexity Tracking omitted.

## Project Structure

### Documentation (this feature)

```text
specs/232-unify-control-id-schemes/
├── plan.md              # this file
├── spec.md              # feature spec
├── research.md          # per-site domain map + move-groups (design record)
├── data-model.md        # id entities + derivations
├── quickstart.md        # validation / smoke guide
├── contracts/
│   └── fsi-surface-deltas.md   # expected .fsi / diagnostic contract changes
├── checklists/
│   └── requirements.md
└── tasks.md             # produced by /speckit-tasks
```

### Source Code (repository root) — seams this feature touches

```text
src/Controls/
├── Focus.fs / Focus.fsi                     # Group A: order mints Key ?? path; indexed walk
├── ControlRuntime.fs / ControlRuntime.fsi   # Group B: thread path through the 3 bridges
├── Diagnostics.fs / Diagnostics.fsi         # unkeyed-collapse rule → unified-scheme wording
├── Widgets/
│   ├── WidgetLowering.fs                     # focusScope stops → real ids
│   ├── Pickers.fs                            # DatePicker: key the trigger Button
│   └── Buttons.fs                            # SplitButton: key the trigger Button
└── RetainedRender.fs / RetainedRender.fsi   # internal path-aware RetainedId → Key ?? path resolver

src/Controls.Elmish/
└── ControlsElmish.fs                         # Group A/B host seams: 969*, 1220, 1394, 1537 + routeFocusedKey filter

tests/
├── Controls.Tests/                           # Focus.order, runtime bridge, widgets, diagnostics
└── Controls.Elmish.Tests/                    # routeFocusedKey dispatch, hover/press stamp, focus ring
```

**Structure Decision**: Single-library change within `FS.GG.UI`. No new projects/modules; one new
`internal` helper in `RetainedRender` (path-aware resolver). Test additions live beside existing
Feature-098/108/112/175 suites.

## Design — the coordinated changes (per research.md move-groups)

1. **Group A · Focus traversal → `Key ?? path`** (unkeyed-sibling disambiguation, FR-005/FR-006):
   - `Focus.order`: replace `controlId c = Key ?? Kind` with an **indexed** walk that threads `path`
     (root `"0"`, child *i* → `path + "." + i`) and mints `FocusStop.Control = Key ?? path`. The walk
     must build `path` even across focusable subtrees it does not descend (switch `for … do` → indexed
     traversal). Two unkeyed same-kind siblings then get distinct paths.
   - `ControlsElmish` 1537 `retainedIdOfControl`: match a traverse `next` id against each node's
     `Key ?? path` (via the new path-aware resolver), not `Key ?? Kind`.
   - `ControlsElmish` 1220 / `routeFocusedKey`: derive the focused node's **full-tree** `Key ?? path`
     id (resolver) and feed it to `Focus.traverse`.

2. **Group B · Visual-state + focus ring → `Key ?? path`** (FR-001/FR-003, hover/press fix):
   - `applyRuntimeVisualState`, `finalVisualState`, `targetedWalk`: thread `path` and compute the node
     id as `Key ?? path`; `deriveVisualState model (Key ?? path)`. Preserve precedence (consumer-set
     non-Normal wins) and at-rest byte-identity (FR-009) — only the *id* changes.
   - `ControlsElmish` 1394 `focusedControlId`: resolve `loopState.Focused: RetainedId` → node's
     `Key ?? path` (resolver), so the ring branch matches the re-pointed bridge.

3. **Independent · Scroll → `Key ?? path`** (FR-001):
   - `applyScrollOffsets`: thread `path`; look up `ScrollOffsets` (already path-keyed) by `Key ?? path`.
     Unkeyed `scroll-viewer`s scroll after this. No producer change.

4. **`routeFocusedKey` binding filter** (FR-004, keyboard dispatch fix):
   - Filter the **full-tree** `eventBindingsOf r.Root.Control` by the focused node's full-tree
     `Key ?? path`, replacing the node-re-rooted `eventBindingsOf node.Control |> filter (= nodeId)`.
     Unkeyed focused controls then dispatch their activation bindings.

5. **Text label** (`ControlsElmish` 969): switch the free-form label to `Key ?? path` for consistency
   (non-correctness; verified no consumer compares it to a tree scheme).

6. **Widgets** (FR-007): key the DatePicker/SplitButton trigger `Button` with its declared `triggerId`;
   re-point `focusScope` stops/`InitialFocus`/`RecoveryTarget` at real lowered ids.

7. **Path-aware resolver** (enabler): add an `internal` `RetainedRender` helper resolving a `RetainedId`
   to its full-tree `Key ?? path` (mirrors `authoredControlIds`' walk).

8. **Diagnostics + `.fsi`** (FR-010/FR-011): re-point the unkeyed-collapse rule and update the doc/`.fsi`
   text (`Focus`, `ControlRuntime`, `Diagnostics`, widget lowering) to the unified scheme; keep the
   public-surface/ApiCompat gate green.

### Key risks / guards

- **Island coherence**: Group A and Group B members must land together (each is self-cancelling today);
  partial landing regresses focus/ring. Tasks sequence them as atomic groups with tests.
- **`routeFocusedKey` two-frame `nodeId`**: the traverse id (full-tree) and the old local-filter id
  (`Key ?? "0"`) differ for unkeyed nodes — do NOT reuse one for both; route via full-tree bindings.
- **At-rest byte-identity (FR-009)** and **targeted-walk minimality (Feature 112)**: add explicit tests
  that the bridged tree at rest is byte-identical and touched-node count is unchanged for keyed trees.
- **Path parity**: newly path-threaded walks must derive `path` identically to `eventBindingsOf`
  (`"0"`, `path + "." + i`) so ids match across seams for the same tree — assert on a mixed tree.

## Phase 0 — research.md ✅ (complete; all unknowns resolved)

## Phase 1 — data-model.md, contracts/, quickstart.md, agent-context ✅ (this command)

## Phase 2 — /speckit-tasks (NOT this command)

Generates `tasks.md`: early behavioral smoke → path-aware resolver → Group A → Group B → scroll →
routeFocusedKey filter → widgets → diagnostics/`.fsi` → full-suite + gate, tests-first per FR.
