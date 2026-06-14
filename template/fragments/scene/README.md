# Scene Fragment

Adds Scene package references and pure scene authoring guidance.

Generated game, chart, and interaction examples should use shared Scene geometry
and first-class shape primitives when the entity is circular or
elliptical. Prefer `Scene.circle`, `Scene.filledEllipse`, `Circle`, or
`FilledEllipse` over rectangle substitutions for balls, markers, pucks,
projectiles, cursors, and status indicators. Reuse the same bounds evidence for
layout checks, containment checks, collision checks, and rendering facts.
When product code needs app-owned geometry types, use domain names such as
`WorldRect`, `WorldPoint`, `TrackBounds`, `CarPose`, or `CheckpointBounds`.
Avoid using bare `Rect`, `Point`, or `Size` for app-domain concepts while Scene
and layout primitives are in scope.

Generated app message examples must qualify app-owned messages such as
`Product.Program.Msg.CloseRequested`. `CloseRequested` remains an app-owned
message even when the viewer reports a close request. Convert domain vector
values explicitly with a helper such as `toScenePoint` before constructing a
`Scene.Point`, so the domain vector to scene point boundary is visible in
generated code and review evidence as an explicit conversion.

```fsharp
open FS.Skia.UI.Scene

let panel = { Red = 40uy; Green = 90uy; Blue = 200uy; Alpha = 255uy }
let ink = { Red = 255uy; Green = 255uy; Blue = 255uy; Alpha = 255uy }

// Self-describing constructors avoid the positional-tuple arity slip.
let hud : Scene =
    Scene.group
        [ Scene.filledRectangle { X = 0.0; Y = 0.0; Width = 320.0; Height = 48.0 } panel
          Scene.textAt { X = 12.0; Y = 30.0 } "tally: 0" ink ]
```
