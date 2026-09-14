---
name: fs-gg-svg-performance
description: Use when measuring or repairing SVG game rendering, density, culling, and browser lifecycle behavior.
---

# SVG player performance

Measure the real production entry point with the released `FS.GG.UI.Scene.SvgBrowser` adapter. Record browser
and device identity, scene size, visible-node count, input rate, session duration, and whether the run measures
cold load, steady play, replay seek, reconnect, or editor activity. A desktop shared-runner diagnostic is not
a physical-mobile or GPU acceptance result.

Keep simulation fixed-step and independent from animation-frame timing. Build one retained scene, mutate only
the accepted state boundary, and let the SVG host batch DOM projection. Cull through the product's stable
visibility/spatial data while retaining semantic alternatives for hidden or off-screen content.

Check these boundaries separately:

- cold restore/build and served production bytes;
- dense steady play and camera movement;
- disconnect/rejoin plus replay seek;
- background/foreground and mount/dispose cycles;
- keyboard, touch, gamepad, reduced-motion, screen-reader alternatives, and 400% reflow.

Record unavailable physical mobile, GPU, or retained-heap measurements as unavailable. Do not infer them from
Chromium timing or process memory. A changed product composition must recheck its changed browser and effect
boundaries even when a matching package reference result can be reused.
