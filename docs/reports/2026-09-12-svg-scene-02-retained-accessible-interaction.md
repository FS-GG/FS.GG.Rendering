# SVG-SCENE-02.4 retained and accessible interaction evidence

Date: 2026-09-12

Rendering base: `b3a8a2c4caf6945bb0a2d9570a11d6b27fd8fb96`

## Reducer and canonical model

`FS.GG.UI.Scene` now exposes a minimal identified-document interaction reducer. Replacement is guarded
by both expected and strictly increasing candidate revisions, validates the complete document before
mutation, retains selection/focus only for still-visible semantic identities, and retains independent
camera and pointer-capture state. Rejected edits are atomic. Undo/redo store typed document values, and
the play handoff captures immutable canonical serialized bytes rather than sharing later editor history.
This is contract foundation only, not complete editor, input-tooling, or session-lifecycle support.

The single profile-2 literate authority adds `ReplaceDocument` and a bounded `documentStep`; it does not
replace the original retained evidence. The retained corpus remains byte-for-byte at 192 transitions and
SHA-256 `3e4d85cd1bedd74feb024b5cdc59f8a69035934ef22eccadcd9360905ab727cf`.
The additive document corpus contains 192 transitions at SHA-256
`4030ed2449b340ac1b893406e72eeb5f33a9940c6ac66029dbcb9e092222653d`.
Installed SDD 1.7.0 author/inspect, exact Quint 0.32.0 typecheck/tests, two deterministic bounded runs,
and stale range/action refusals passed; the checked-in
[qualification receipt](../../readiness/svg-scene-02-4/model-qualification.json) binds those identities.

Isolated local `FS.GG.UI.Scene 0.29.0-preview.1` consumers replay both corpora through the real reducers.
.NET 10 and Fable/Node retain the previous retained projection SHA-256
`cd1b2c74c95a5f25df06ca0921af7d7af9c578d1f589ef6012fea56fadd5c0d2` and agree on the new document
projection SHA-256 `6eee28de98d551c738113152464834969a411470c2f90bcafb6151be6965d997`.

## Retained browser and accessible routes

`FS.GG.UI.Scene.SvgBrowser` reconciles keyed layers, objects, identified document elements, and definitions,
moving stable nodes for paint-order changes and replacing only changed content. Camera/selection updates do
not rebuild scene children. Identified-document hit testing delegates to the mounted DOM, so transformed
path, text, symbol, paint order, and nested clipping follow declared browser semantics. The selected masked
transparency policy is explicit: masked painted geometry remains targetable; clipping removes targets.

Pointer selection, the portable `FS.GG.UI.KeyboardInput` SVG intent adapter, and sibling HTML buttons select
the same semantic identity. SVG shapes keep SVG-native semantics while labelled HTML buttons provide the
keyboard/control route and a live selection status. IME composition and native editable targets are not
consumed. Lost capture, pointer cancellation, blur, and disposal clear adapter-owned capture; disposal also
removes all owned listeners, controls, roots, and scheduled work.

The checked-in [Chromium observation](../../readiness/svg-scene-02-4/browser-observations.json) uses isolated
locally packed Scene, KeyboardInput, and SvgBrowser candidates. It verifies node/definition identity across
reorder and update, path/text/symbol/clip hits, masked-hit policy, local namespace refusal, semantic-equivalent
control routes, native editing/composition, repeated mount/dispose, and the SVG-SCENE-02.3 analytic/visual
definition surface plus standalone [export reload](../../readiness/svg-scene-02-4/browser-observations.exported.svg).

## Verification and boundary

Focused verification passed 108 Scene tests, 65 KeyboardInput tests, 531 package tests, the full Debug build,
both isolated portable runtime consumers, exact installed model qualification, and the packed Chromium gallery.
Rendering remains independent of Game. Candidate packages stayed local; none was published or activated as a
default. This work makes no complete editor/input-tooling, arbitrary SVG import, cross-browser, complete M5,
or publication claim. S.I.R. was not accessed.
