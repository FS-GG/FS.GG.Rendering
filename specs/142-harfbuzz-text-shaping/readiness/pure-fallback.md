# Feature 142 Pure Fallback

Status: preserved.

- `Scene.buildFallbackShapedText` uses the existing dependency-light measurement heuristic.
- `Scene.measureShapedText (Scene.buildFallbackShapedText text font)` equals `Scene.measureText text font`.
- Pure fallback baseline changes: zero.
