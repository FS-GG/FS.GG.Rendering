---
name: fs-gg-svg-assets
description: Use when importing, arranging, validating, saving, or exporting editable SVG game assets.
---

# Editable SVG game assets

Keep the authored SVG document as product content. Parse only the supported SVG subset, retain stable element
ids, and map game meaning through the product's element-to-visual catalog. Scene nodes and hit regions are
derived views; do not make the runtime projection a second authoring format.

Import active content, scripts, external network references, malformed geometry, and resources over declared
limits as explicit refusals before replacing the last valid document. Missing references remain visible
diagnostics. Preserve the last valid saved value when a subsequent import or browser-storage write fails.

Use one transaction for Create and Arrange operations. Retain the document root, camera, selection, focus, and
undo boundary together, then project the accepted document through `FS.GG.UI.Scene`. Play and Review consume
that same accepted content. Export canonical SVG and the product manifest from stable ordering and invariant
number formatting so reload is byte-stable for unchanged content.

```fsharp
let scene = Scene.group []
```

The production player consumes the exported content but does not ship this authoring guidance or Studio code.
Package-owned parsing, scene, and rendering implementations remain package references; authored adapters hold
only product-specific ids, catalogs, and mappings.
