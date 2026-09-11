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
