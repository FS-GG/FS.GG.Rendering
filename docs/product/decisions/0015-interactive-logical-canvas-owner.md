# ADR-0015: SkiaViewer owns the interactive logical canvas

Status: Accepted

## Context

An interactive Controls game has three coordinate spaces: native window pointer samples, the
physical framebuffer, and the logical canvas authored by the product. The game-shell settings can
replace that logical canvas while a window is running. Previously the generated host used
`LogicalSize = None` to keep Controls hit tests aligned, while the shell persisted a different
resolution and claimed it had applied it. Guidance spread the fit and inverse mapping across the
game shell, SkiaViewer, Scene, and Controls, leaving double transforms and inert settings both
possible.

## Decision

SkiaViewer is the single owner of logical-canvas policy for every interactive host:

- `ViewerOptions.LogicalSize` seeds the policy at launch.
- `ViewerEffect.ApplyLogicalCanvas` replaces it at runtime.
- The product `View`, Controls layout, retained bounds, and Controls hit testing all speak the
  selected logical size directly.
- SkiaViewer alone uniformly fits and centers that canvas on the physical framebuffer, producing
  letterbox bars where aspect ratios differ.
- SkiaViewer alone converts native window coordinates to framebuffer coordinates and applies the
  inverse fit before forwarding a pointer sample to the interactive host.
- `ApplyWindowOptions` remains the independent window-presentation request. Its interpretation must
  not mutate the logical canvas: windowed, borderless, and fullscreen requests all retain the same
  coordinate policy.

The generated game host therefore supplies its initial shell resolution as `LogicalSize`, emits
both `ApplyWindowOptions` and `ApplyLogicalCanvas` for `DisplayChanged`, and does not scale its
Controls tree or pointer samples itself.

## Consequences

A resolution change re-authors the Controls tree in the new logical size, presents it with the new
fit, and routes the next native pointer through the matching inverse. The visible control and its hit
bounds remain the same semantic target. Invalid non-positive runtime sizes are rejected with a
viewer diagnostic and do not replace the last valid policy.

The new `ViewerEffect` case is an additive public API change and requires a coherent FS.GG.UI
package release plus a template release that pins that framework version.
