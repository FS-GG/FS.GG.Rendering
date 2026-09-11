# SVG-SCENE-02.3 paint and definition evidence

Date: 2026-09-11

Rendering base: `acfca1e87866f7c1b4cab3b064e225d8f585a977`

## Selected public surface

`FS.GG.UI.Scene` now validates, canonically serializes, deserializes and exports the selected
`fsgg.svg-document/1` surface. The one-way SVG exporter covers existing empty/group/rectangle/circle/
ellipse/line/path/translation nodes plus Arc/ArcTo, TextRun, clipping and transparent cached subtrees.
Complete revolutions are split into representable SVG arc segments. Existing Scene text emits explicit
start-anchor/automatic-direction semantics, while TextRun preserves its authored family, size and weight.

Paint export preserves independent fill/stroke presentation, cap/join/miter, opacity, positive dash arrays,
nonzero/evenodd rules and stroke-only interiors. `Shader.SolidColor` takes precedence over the legacy fill,
and existing linear/radial color lists become evenly spaced sRGB stops. Typed definitions cover linear and
radial gradients in both coordinate systems, transforms, pad/repeat/reflect, ordered bounded stops and local
inheritance; symbols and instances; rect/path/nested-intersection clips; alpha and luminance masks; and local
WOFF2 font declarations. IDs use collision-free encoding of the caller's mount namespace, document identity
and typed local identity. No external `<use>`, raster resource, script, CSS, event handler or executable
extension is accepted.

`FS.GG.UI.Scene.SvgBrowser` adds a public identified-document host. It completes validation and export before
mount or replacement, rejects a duplicate active mount namespace, replaces atomically, preserves the exact
standalone export, and exposes font-ready/fallback observations. A missing declared font produces the stable
diagnostic `font-unavailable:font:Noto Sans`; browser fallback is not treated as font-fidelity success. The
original retained browser entry point also preflights and renders through the same selected export mapping, so
a newly supported Scene node cannot be silently omitted.

Unsupported or malformed content returns stable typed diagnostics before DOM mutation. The fixture exercises
duplicate element identities at `/children`, an unsupported image at `/children/0/scene/nodes/0`, missing and
wrong-kind references, cyclic references, invalid geometry/paint/stops/fonts, depth and resource limits.

## Portable and browser evidence

The isolated local `FS.GG.UI.Scene 0.29.0-preview.1` consumers compile and execute on .NET 10 and Fable/Node.
They build the same document, perform an exact typed serialize/deserialize/serialize round trip and produce the
same document SHA-256 `92769fe60988b62b044b10c7fcd6f50b921bfeae6a82e20c6073818d2c49f31a`.
This is a typed-document round trip only: arbitrary SVG XML import remains explicitly unsupported. The same
consumer run replays all 192 retained transitions with unchanged projection SHA-256
`cd1b2c74c95a5f25df06ca0921af7d7af9c578d1f589ef6012fea56fadd5c0d2`.

The checked-in [browser observation](../../readiness/svg-scene-02-3/browser-observations.json) records an
isolated packed Fable adapter running in Chromium 151. It analytically verifies five emitted gradients, one
symbol/use pair, three clips including a nested intersection, alpha and luminance masks, both coordinate-unit
modes, explicit transform/sRGB/stops, symbol viewport, text layout, unique IDs and resolved local references.
Stroke-only fill, solid-shader precedence, evenodd fill and two-segment complete arcs are asserted directly.
An invalid replacement leaves both the root and exported bytes unchanged. A second isolated page loads the
[standalone SVG](../../readiness/svg-scene-02-3/browser-observations.exported.svg), resolves its local symbol
reference and matches the source rendering at sampled interiors within two channels values. The Chromium visual
reference uses a per-channel tolerance of 28 and separately asserts the white evenodd hole and left/right
gradient interior colors; it is not a permissive whole-image screenshot threshold.

Focused verification passed 104 Scene tests, the isolated .NET/Fable consumers, and the packed Chromium
gallery. Firefox, WebKit, assistive-technology announcements, physical touch/mobile hardware, heap/detached-node
measurements and compositor presentation timestamps remain explicitly unavailable here; SVG-SCENE-02.5/.6 own
those broader qualification dimensions.

## Boundary

Rendering has no dependency on Game. This milestone adds no reducer state semantics and therefore does not
amend or supersede the canonical 192-transition corpus; SVG-SCENE-02.4 owns the document reducer/model seam.
It makes no arbitrary-import, complete C03, M5-session, complete M9, cross-browser or publication claim.
Candidate packages remain local: no package was published and no default/provider/lifecycle was activated.
S.I.R. was not accessed.
