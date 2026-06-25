namespace FS.GG.UI.Canvas

open System
open FS.GG.UI.Scene

type Element<'props> = 'props -> Scene

[<RequireQualifiedAccess>]
module Elements =

    // Deterministic FNV-1a fold over a sub-scene's render-affecting inputs. Pure and process-stable
    // (no String.GetHashCode, which is per-process randomized): same scene ⇒ same fingerprint across
    // runs (FR-008/FR-011). Used to key `cached` replay boundaries by content.
    let private offsetBasis = 0xcbf29ce484222325UL
    let private prime = 0x100000001b3UL
    let private step (h: uint64) (x: uint64) = (h ^^^ x) * prime
    let private mixFloat (h: uint64) (f: float) = step h (uint64 (BitConverter.DoubleToInt64Bits f))

    let private mixString (h: uint64) (s: string) =
        let mutable acc = step h (uint64 s.Length)
        for b in Text.Encoding.UTF8.GetBytes s do
            acc <- step acc (uint64 b)
        acc

    let private mixColor (h: uint64) (c: Color) =
        step (step (step (step h (uint64 c.Red)) (uint64 c.Green)) (uint64 c.Blue)) (uint64 c.Alpha)

    let private mixPoint (h: uint64) (p: Point) = mixFloat (mixFloat h p.X) p.Y

    let rec private mixScene (h: uint64) (scene: Scene) : uint64 =
        let mutable acc = step h (uint64 (List.length scene.Nodes))
        for node in scene.Nodes do
            acc <- mixNode acc node
        acc

    and private mixNode (h: uint64) (node: SceneNode) : uint64 =
        match node with
        | Empty -> step h 1UL
        | Group children -> List.fold mixScene (step h 2UL) children
        | Rectangle((x, y, w, ht), fill) -> mixColor (mixFloat (mixFloat (mixFloat (mixFloat (step h 3UL) x) y) w) ht) fill
        | PaintedRectangle(b, _) -> mixFloat (mixFloat (mixFloat (mixFloat (step h 4UL) b.X) b.Y) b.Width) b.Height
        | Circle(c, r, fill) -> mixColor (mixFloat (mixPoint (step h 5UL) c) r) fill
        | Image((x, y, w, ht), src) -> mixString (mixFloat (mixFloat (mixFloat (mixFloat (step h 6UL) x) y) w) ht) src
        | Points(pts, _) -> List.fold mixPoint (step h 7UL) pts
        | Line(s, e, _) -> mixPoint (mixPoint (step h 8UL) s) e
        | Text((x, y), t, fill) -> mixColor (mixString (mixFloat (mixFloat (step h 9UL) x) y) t) fill
        | SizedText((x, y), t, sz, fill) -> mixColor (mixString (mixFloat (mixFloat (mixFloat (step h 10UL) x) y) sz) t) fill
        | Translate((dx, dy), s) -> mixScene (mixFloat (mixFloat (step h 11UL) dx) dy) s
        | ClipNode(_, s) -> mixScene (step h 12UL) s
        | PerspectiveNode(_, s) -> mixScene (step h 13UL) s
        | ColorSpaceNode(_, s) -> mixScene (step h 14UL) s
        | CachedSubtree b -> mixScene (step (step h 15UL) b.CacheId) b.Scene
        | Chart values -> List.fold mixFloat (step h 16UL) values
        | Path(spec, _) ->
            spec.Commands
            |> List.fold
                (fun acc cmd ->
                    match cmd with
                    | MoveTo p -> mixPoint (step acc 1UL) p
                    | LineTo p -> mixPoint (step acc 2UL) p
                    | QuadTo(c, p) -> mixPoint (mixPoint (step acc 3UL) c) p
                    | CubicTo(c1, c2, p) -> mixPoint (mixPoint (mixPoint (step acc 4UL) c1) c2) p
                    | ArcTo(b, sa, sw) -> mixFloat (mixFloat (mixFloat (mixFloat (mixFloat (step acc 5UL) b.X) b.Y) b.Width) sa) sw
                    | Close -> step acc 6UL)
                (step h 17UL)
        | _ -> step h 18UL

    let rect (w: float) (h: float) (paint: Paint) : Scene =
        Scene.rectangleWithPaint { X = 0.0; Y = 0.0; Width = w; Height = h } paint

    let sprite (image: string) (w: float) (h: float) : Scene = Scene.image (0.0, 0.0, w, h) image

    let circle (r: float) (fill: Color) : Scene = Scene.circle { X = 0.0; Y = 0.0 } r fill

    let polyline (points: Point list) (paint: Paint) : Scene =
        match points with
        | [] | [ _ ] -> Scene.empty
        | first :: rest ->
            let commands =
                Path.moveTo first.X first.Y :: (rest |> List.map (fun p -> Path.lineTo p.X p.Y))
            Scene.path (Path.create PathFillType.Winding commands) paint

    let at (x: float) (y: float) (scene: Scene) : Scene = Scene.translate x y scene

    let layer (scenes: Scene list) : Scene = Scene.group scenes

    let cached (key: string) (scene: Scene) : Scene =
        let cacheId = mixString offsetBasis key
        let fingerprint = mixScene offsetBasis scene
        { Nodes = [ CachedSubtree { CacheId = cacheId; Fingerprint = fingerprint; Scene = scene } ] }
