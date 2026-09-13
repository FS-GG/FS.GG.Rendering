# FS.GG.UI.Scene.SvgBrowser

`FS.GG.UI.Scene.SvgBrowser` is the Fable/browser adapter for the supported
`FS.GG.UI.Scene` subset. It maintains a retained SVG root, routes pointer and
keyboard selection through the Scene reducer, and provides camera-aware picking.

Use the curated Fable project shipped in the package under
`fable/FS.GG.UI.Scene.SvgBrowser.fsproj`. The adapter depends on
`Fable.Browser.Dom`; the base `FS.GG.UI.Scene` package remains browser-independent.

The identified-document entry point validates and exports the complete selected
Preview-A paint/definition surface before mounting or replacing DOM. Its mount
namespace produces collision-free local definition references, and duplicate
active namespaces are rejected. `ObserveFonts` distinguishes an available
declared family from an explicit fallback diagnostic; it is not a font-fidelity
claim. Typed document deserialization accepts only `fsgg.svg-document/1`, not
arbitrary SVG XML.

Identified replacements reconcile stable elements and definitions in place. Browser hit testing follows
paint order and native SVG path/text/symbol/clip semantics; masked painted geometry remains targetable
while clipping removes targets. The retained entry point projects semantic selection through pointer,
portable keyboard intent, and sibling HTML controls. Native editable/composition targets are left alone,
and pointer loss, cancellation, blur, and disposal clear adapter-owned capture and scheduled work. These
are accessible interaction contract foundations, not a complete editor or input toolkit.

`SvgStudio.mount` is the explicit optional authoring entry. It owns one retained document host,
native toolbar/property/point-list controls, accessible mode/palette/help/rebind controls, and disposable
Escape, pointer-loss, and blur handlers.
Camera, selection, tool state, accepted content, and `SvgAuthoring` history remain separate. Consumers
that enable Boolean tools provide a bundler-resolved module-worker factory; the packaged worker imports
exactly `polygon-clipping` 0.15.7 and permits one request with no queue and a two-second refusal deadline.
Player entries do not import or initialize studio code.

The retained host displays the current gesture candidate without accepting it, restores accepted content
on cancellation, applies camera matrices to the visible SVG, and inverse-transforms `Pick` coordinates.
The browser package's worker is the sole geometry implementation: test and consumer bundlers load
`contentFiles/any/any/svg-geometry-worker.js` from the exact restored package.

`SvgStudio.activateFont` accepts only a resource verified by `SvgResourceInterchange.notoSansLatin400`.
It adds the loaded face after byte validation and removes the face and blob URL on disposal. The package
carries the exact base64-encoded WOFF2, SHA-256/provenance manifest, full OFL 1.1 text, and the clipping
library's MIT notice.

`SvgInputHost` is the disposable command adapter for an owned browser focus scope. It preserves logical
`key` and physical `code` identity, all modifiers including AltGraph, repeat, composition, and native
controls before passing normalized observations to `CommandResolver`. Pointer and touch remain distinct
semantic gestures, and Gamepad API buttons retain their source until release or disconnection. The host
prevents default only when the resolver accepts the exact event; blur, visibility loss, modal takeover,
and disposal neutralize held actions and cancel every listener, deadline, and animation-frame poll.
