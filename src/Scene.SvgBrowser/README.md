# FS.GG.UI.Scene.SvgBrowser

`FS.GG.UI.Scene.SvgBrowser` is the Fable/browser adapter for the supported
`FS.GG.UI.Scene` subset. It maintains a retained SVG root, routes pointer and
keyboard selection through the Scene reducer, and provides camera-aware picking.

Use the curated Fable project shipped in the package under
`fable/FS.GG.UI.Scene.SvgBrowser.fsproj`. The adapter depends on
`Fable.Browser.Dom`; the base `FS.GG.UI.Scene` package remains browser-independent.

This package is an early foundation surface. The supported node and transform
subset is validated by the Scene contract before mounting.
