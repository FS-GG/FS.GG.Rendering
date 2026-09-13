# SVG-INPUT-01 — Command and workspace interaction

Status: active; next executable window .4. Route: routine.

Owner: FS.GG.Rendering. FS.GG.Templates owns generated product composition and
external compatibility fixtures. FS.GG.Game retains command policy and gameplay
intent meaning. This is order 6 of the
[accepted SVG programme](https://github.com/FS-GG/.github/blob/main/docs/2026-09-07-064259-svg-game-engine-template-design-roadmap.md)
and covers C07/C09 plus M4.1–M4.2 and M4.6–M4.8. SVG-AUTHOR-01 is complete at
the source/generated-candidate boundary.

## Outcome and boundary

A product supplies one validated command catalog and binding profile. The same
effective catalog drives deterministic dispatch, modal precedence, key sequences,
held actions, rebinding, a command palette, possible-input help, shortcut labels,
pointer/touch/gamepad alternatives and accessible browser controls. Workspace
layout, panels, focus and modal ownership remain portable state interpreted by an
explicit browser adapter.

Rendering owns command-agnostic input and workspace mechanisms. Game owns the
meaning and default policy of gameplay commands. Products validate opaque command
arguments and turn accepted invocations into authoring transactions or gameplay
intents. SVG-INPUT-01 does not add a gameplay clock, collision, replay, networking,
browser persistence, package publication or lifecycle/default activation. Those
remain with their later features and Preview B.

S.I.R. is off limits. Use only the accepted roadmap characterization and
Templates-owned external fixtures. Do not access or modify its repository.

## Audited baseline and decisions

Planning inspected Rendering `ab0de59a5797393f1eaff6dd3146b53ae8bf1a67`,
Game `494bd45591851c03496460e250f6584025fb5e52`, Templates
`219fbc963fe50563ae584f8b788ce93da9ec072d`, and programme/index
`963fe452d48fd34e1c6550f58b6eaaff85c6f64d`.

- `FS.GG.UI.KeyboardInput` has an immutable key map, v1 JSON codec, modifier
  recovery, raw key routing and a small reducer. It does not yet model contexts,
  availability, priority, sequences, binding overrides or device-source ownership.
- `SvgBrowser` already depends on the portable KeyboardInput package and protects
  native editing and IME composition for its small retained-viewer key surface.
  `SvgStudioHost` owns only Escape cancellation and pointer-capture cleanup.
- Rendering already supplies layout/dock primitives and focus/runtime controls.
  Reuse their geometry and DOM semantics; keep the input package independent of
  Controls, Layout, browser APIs, Skia and Game.
- Game's `Command` identifiers and `DefaultKeymap` remain the gameplay policy
  adapter. Rendering stores opaque stable command IDs and never adds game cases.
- The accepted programme requires a new bounded input-command Quint module. It is
  single-actor shared state: catalog/configuration is immutable input; reducer state
  tracks modal ownership, a pending prefix and held contributions. Time is an
  injected deadline observation, never a clock inside the model.

Keep the existing `fsgg.keymap` v1 reader unchanged. Add a distinct versioned
profile envelope for richer bindings and explicit migration from v1. Preserve
logical produced-key identity separately from physical code, modifiers and device
source. AltGraph remains distinct. Imported raw duplicates and ambiguity must be
diagnosed before construction; insertion order never selects a winner.

## Executable milestones

- [x] **SVG-INPUT-01.1 — One portable catalog defines commands and gestures — route: routine**

  Depends on: SVG-AUTHOR-01 authoritative readback and unified selection.

  Scope: extend the curated KeyboardInput surface with opaque semantic command
  descriptors, typed gesture/source/context vocabulary, catalog/profile validation,
  effective override construction and a versioned portable codec. Add adapters for
  the existing key map without changing v1 meaning. Update package/Fable metadata,
  surface baselines, tests, README and the owner skill.

  A command declares stable ID, display label, contexts, availability key, trigger
  policy, argument requirement and accessible alternatives. Bindings distinguish
  logical key, physical code, pointer, touch and gamepad gestures. Profiles retain
  defaults plus ordered explicit overrides: replace, add alias or unbind. Validation
  rejects unknown commands/contexts, empty identities, reserved gestures, duplicate
  gestures at equal priority in overlapping contexts, illegal AltGraph collapse,
  terminal-prefix conflicts unless explicitly allowed, and timed sequences without
  palette/menu alternatives.

  Acceptance: equivalent catalogs/profiles yield byte-identical effective bindings
  and encoded bytes in .NET/Fable. Rebinding and explicit unbinding remain distinct;
  v1 key maps migrate without reinterpreting logical keys. Invalid raw imports
  report all located diagnostics before any last-wins construction. Existing APIs,
  tests and package closure remain compatible.

- [x] **SVG-INPUT-01.2 — Modal resolution and true sequences obey one reducer — route: routine**

  Depends on: .1 accepted catalog/profile surface.

  Scope: add the pure resolver state machine, effects and the bounded input-command
  Quint authority with generated extraction/bindings. Model event identity,
  context changes, capture, composition, injected deadlines, pending prefixes,
  held layers and source-owned releases. Compare real .NET/Fable transitions with
  model observations and retain negative mutants.

  Precedence is host/native reservation; binding capture or exclusive modal/help;
  held layer; active gesture; active tool/session context; workspace fallback.
  Availability is rechecked at invocation. Escape cancels exactly the innermost
  interaction and requests its valid focus target. A nonmatching sequence
  continuation cancels the prefix and resolves that event once from the root.
  Terminal-prefix overlap waits only when explicitly enabled and emits once at the
  injected deadline. Focus loss, composition, profile/context change and disposal
  cancel pending sequences and neutralize owned held contributions.

  Acceptance: deterministic traces cover ambiguity refusal, no double dispatch,
  terminal-prefix/deadline/cancel, release after focus/mode change, multiple sources
  holding one action, no repeated destructive command and immediate profile change.
  The model includes positive lost-release and terminal-prefix witnesses, stated
  fairness limits, safety invariants and real reducer correspondence.

- [x] **SVG-INPUT-01.3 — Workspace panels, focus and help share reducer state — route: routine**

  Depends on: .2 resolver effects.

  Scope: add portable workspace state and reusable SvgStudio integration for Create,
  Arrange, Play and Review modes; docked/floating/collapsed panels; inspectors,
  command palette, live possible-input help and rebind/conflict UI. Reuse Layout and
  Controls primitives through composition outside KeyboardInput.

  Layout preferences are versioned and bounded. Focus restoration names stable
  control/scene targets. Modal ownership blocks fallthrough according to context
  policy. The effective catalog alone produces dispatch rows, palette entries,
  help, shortcut text and representable `aria-keyshortcuts`; multi-step sequences
  use accessible prose. Tool and session modes remain projections supplied by their
  owners rather than duplicate input state.

  Acceptance: responsive dock transitions preserve accepted scene/history/camera,
  valid selection and meaningful focus. Palette/pointer/keyboard availability agree.
  Capture sees unbound raw input, reports displacement before replacement, applies
  immediately and round-trips profile/layout migration. Invalid imports and modal
  cancellation preserve previous state.

  Completed by Rendering PR #1308: `SvgWorkspace` keeps bounded versioned
  preferences separate from responsive effective placement and from authoring
  owners; `SvgWorkspaceCommands` derives every discovery surface from one
  catalog/profile/availability projection and returns displacement-first validated
  rebind candidates. Packed .NET/Fable consumers agree, and `SvgStudioHost`
  carries the same reducer state without copying scene, history, camera or selection.

- [ ] **SVG-INPUT-01.4 — Browser devices preserve native input and accessible parity — route: routine**

  Depends on: .2–.3 portable contracts.

  Scope: implement a disposable SvgBrowser input/workspace host over real DOM
  keyboard, pointer, touch and Gamepad API observations. Keep browser code outside
  KeyboardInput and player/studio entry boundaries explicit. Add three-browser and
  real assistive-technology journeys plus best-available layout/IME evidence.

  Preserve `key` and `code`, Ctrl/Meta/Alt/Shift/AltGraph, repeat, composition and
  editable targets. Prevent default only for an accepted event inside the owned
  focus scope. Route releases to the source that owned the press. Blur, visibility
  loss, modal takeover and disposal neutralize held state and remove every listener,
  poll and deadline. Pointer/touch/gamepad produce semantic observations directly,
  never synthetic keyboard events.

  Acceptance: Chromium/Firefox/WebKit cover Ctrl/Meta bindings, logical/physical
  identity, capture, sequences, modality, focus recovery, touch/pointer equivalence,
  gamepad multi-source release and disposal. Real Orca/AT-SPI activates palette,
  help and rebind controls and observes mode/conflict feedback. Composed/dead-key
  text and native controls emit no command. Unsupported OS/layout observations are
  recorded as unavailable rather than inferred from synthetic events.

- [ ] **SVG-INPUT-01.5 — Generated workspace input journey and Preview-B handoff — route: routine**

  Owner: Templates; Rendering retains producer defects and this completion ledger.

  Depends on: .1–.4 merged source and an exact immutable Rendering candidate packet.

  Scope: compose product-owned editor/workspace commands and Game's existing command
  adapter into the opt-in SVG generated candidate. Add direct, installed SDD,
  wizard-adopter and retained upgrade/rollback journeys without changing public
  provider pins. Preserve separate player/studio entry points and the Preview-A lane.

  Acceptance: an isolated generated workspace exercises all four modes, docking,
  palette/help, modal/sequence resolution, rebinding and keyboard/pointer/touch/
  gamepad parity against actual authoring state. Clean and retained receivers keep
  lifecycle provenance and authored content, refuse managed collisions before
  writes, recover interruption and restore managed bytes on rollback. Player output
  excludes Studio/workspace modules while retaining the small runtime input adapter.

  Evidence binds Rendering and Templates merge revisions, exact archives, Fable
  interfaces, generated payload, browser observations, model extraction/toolchain,
  public baseline archives and every migration/rollback result. Public installation
  remains pending SVG-PREVIEW-B after SVG-RUNTIME-01 and SVG-PRESENT-01.

## Completion and release impact

After all five milestones meet their authority boundaries, record SVG-INPUT-01
complete with publication pending Preview B, update the unified projection, and
select SVG-RUNTIME-01. A local candidate version must be unique to its bytes and
must not change the public Rendering `0.29.0` or Templates `0.11.0` pins.

The generated `svgFoundation` option remains explicit. No wizard default, registry
activation, coordination epoch or lifecycle change is part of this feature.
Telemetry uses the installed adapter; its private host is currently unconfigured,
so coverage remains unknown and cannot support an efficiency claim.
