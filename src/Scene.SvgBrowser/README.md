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
