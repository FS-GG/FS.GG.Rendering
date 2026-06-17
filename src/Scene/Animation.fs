namespace FS.GG.UI.Scene

open System

type Easing =
    | Linear
    | EaseIn
    | EaseOut
    | EaseInOut

type Transform =
    { TranslateX: float
      TranslateY: float
      ScaleX: float
      ScaleY: float
      RotationDegrees: float }

type Tween<'a> =
    { Start: 'a
      End: 'a
      Duration: TimeSpan
      Easing: Easing }

type Animation =
    { Opacity: Tween<float> option
      Transform: Tween<Transform> option
      Color: Tween<Color> option }

type AnimationState<'a> =
    { Current: 'a
      Start: 'a
      Target: 'a
      Elapsed: TimeSpan
      Duration: TimeSpan
      Easing: Easing
      Interp: 'a -> 'a -> float -> 'a }

module Easing =
    let private clamp01 (t: float) =
        if t < 0.0 then 0.0
        elif t > 1.0 then 1.0
        else t

    let apply (easing: Easing) (t: float) : float =
        let t = clamp01 t

        match easing with
        | Linear -> t
        | EaseIn -> t * t * t
        | EaseOut ->
            let u = 1.0 - t
            1.0 - (u * u * u)
        | EaseInOut ->
            if t < 0.5 then
                4.0 * t * t * t
            else
                let u = -2.0 * t + 2.0
                1.0 - (u * u * u) / 2.0

    let Default: Easing = EaseInOut

module Transform =
    let identity: Transform =
        { TranslateX = 0.0
          TranslateY = 0.0
          ScaleX = 1.0
          ScaleY = 1.0
          RotationDegrees = 0.0 }

    let isIdentity (transform: Transform) : bool = transform = identity

    let private lerp1 (a: float) (b: float) (t: float) = a + (b - a) * t

    let lerp (a: Transform) (b: Transform) (t: float) : Transform =
        { TranslateX = lerp1 a.TranslateX b.TranslateX t
          TranslateY = lerp1 a.TranslateY b.TranslateY t
          ScaleX = lerp1 a.ScaleX b.ScaleX t
          ScaleY = lerp1 a.ScaleY b.ScaleY t
          RotationDegrees = lerp1 a.RotationDegrees b.RotationDegrees t }

    let toPerspectiveTransform (transform: Transform) : PerspectiveTransform =
        // Compose translate ∘ rotate ∘ scale into a 2D affine 3×3.
        let theta = transform.RotationDegrees * Math.PI / 180.0
        let cos = Math.Cos theta
        let sin = Math.Sin theta
        let sx = transform.ScaleX
        let sy = transform.ScaleY

        { M11 = sx * cos
          M12 = -(sy * sin)
          M13 = transform.TranslateX
          M21 = sx * sin
          M22 = sy * cos
          M23 = transform.TranslateY
          M31 = 0.0
          M32 = 0.0
          M33 = 1.0 }

module Tween =
    let progress (elapsed: TimeSpan) (tween: Tween<'a>) : float =
        if tween.Duration <= TimeSpan.Zero then
            1.0
        else
            let raw = float elapsed.Ticks / float tween.Duration.Ticks
            Easing.apply tween.Easing raw

    let sample (interp: 'a -> 'a -> float -> 'a) (elapsed: TimeSpan) (tween: Tween<'a>) : 'a =
        let p = progress elapsed tween
        // Pin endpoints exactly so a settled sample is byte-identical to `End`
        // (identity-at-rest) and a pre-start sample is exactly `Start`.
        if p >= 1.0 then tween.End
        elif p <= 0.0 then tween.Start
        else interp tween.Start tween.End p

module private Lower =
    let private clampByte (v: float) =
        let r = Math.Round v

        if r < 0.0 then 0uy
        elif r > 255.0 then 255uy
        else byte r

    let private scaleColor (o: float) (c: Color) : Color =
        { c with Alpha = clampByte (float c.Alpha * o) }

    let private scalePaint (o: float) (p: Paint) : Paint = { p with Opacity = p.Opacity * o }

    let rec scaleScene (o: float) (scene: Scene) : Scene =
        { Nodes = scene.Nodes |> List.map (scaleNode o) }

    and scaleNode (o: float) (node: SceneNode) : SceneNode =
        match node with
        | Empty -> Empty
        | Group scenes -> Group(scenes |> List.map (scaleScene o))
        | Rectangle(bounds, color) -> Rectangle(bounds, scaleColor o color)
        | PaintedRectangle(rect, paint) -> PaintedRectangle(rect, scalePaint o paint)
        | Circle(center, radius, fill) -> Circle(center, radius, scaleColor o fill)
        | FilledEllipse(bounds, fill) -> FilledEllipse(bounds, scaleColor o fill)
        | Ellipse(rect, paint) -> Ellipse(rect, scalePaint o paint)
        | Line(a, b, paint) -> Line(a, b, scalePaint o paint)
        | Path(spec, paint) -> Path(spec, scalePaint o paint)
        | Points(pts, paint) -> Points(pts, scalePaint o paint)
        | Vertices(mode, vs, paint) -> Vertices(mode, vs, scalePaint o paint)
        | Arc(rect, sa, ea, paint) -> Arc(rect, sa, ea, scalePaint o paint)
        | Text(pos, s, color) -> Text(pos, s, scaleColor o color)
        | TextRun run -> TextRun { run with Paint = scalePaint o run.Paint }
        | Image(bounds, src) -> Image(bounds, src)
        | ClipNode(clip, scene) -> ClipNode(clip, scaleScene o scene)
        | RegionNode(region, paint) -> RegionNode(region, scalePaint o paint)
        | ColorSpaceNode(cs, scene) -> ColorSpaceNode(cs, scaleScene o scene)
        | PerspectiveNode(t, scene) -> PerspectiveNode(t, scaleScene o scene)
        | PictureNode picture -> PictureNode { picture with Scene = scaleScene o picture.Scene }
        | Chart values -> Chart values
        | Translate(offset, scene) -> Translate(offset, scaleScene o scene)
        | SizedText(pos, s, size, color) -> SizedText(pos, s, size, scaleColor o color)
        | GlyphRun run -> GlyphRun { run with Paint = scalePaint o run.Paint }
        // Feature 120 (FR-007): transparent — scaling changes content, so unwrap the boundary and
        // scale the inner subtree (a scaled subtree is no longer byte-identical to its recorded
        // picture, so it must not carry the cache marker into the overlay sampler).
        | CachedSubtree boundary -> Group [ scaleScene o boundary.Scene ]

    /// Collapse a target scene to a single `SceneNode`: a lone node passes
    /// through unwrapped (byte-identical to the static render), otherwise the
    /// nodes are grouped.
    let unwrap (scene: Scene) : SceneNode =
        match scene.Nodes with
        | [ single ] -> single
        | nodes -> Group [ { Nodes = nodes } ]

module Animation =
    let lerpFloat (a: float) (b: float) (t: float) : float = a + (b - a) * t

    let private clampByte (v: float) =
        let r = Math.Round v

        if r < 0.0 then 0uy
        elif r > 255.0 then 255uy
        else byte r

    let private lerpByte (a: byte) (b: byte) (t: float) =
        clampByte (float a + (float b - float a) * t)

    let lerpColor (a: Color) (b: Color) (t: float) : Color =
        { Red = lerpByte a.Red b.Red t
          Green = lerpByte a.Green b.Green t
          Blue = lerpByte a.Blue b.Blue t
          Alpha = lerpByte a.Alpha b.Alpha t }

    let empty: Animation =
        { Opacity = None
          Transform = None
          Color = None }

    let private sampleOpacity (elapsed: TimeSpan) (animation: Animation) : float =
        match animation.Opacity with
        | Some tween -> Tween.sample lerpFloat elapsed tween
        | None -> 1.0

    let private sampleTransform (elapsed: TimeSpan) (animation: Animation) : Transform =
        match animation.Transform with
        | Some tween -> Tween.sample Transform.lerp elapsed tween
        | None -> Transform.identity

    let applyAt (elapsed: TimeSpan) (animation: Animation) (target: Scene) : SceneNode =
        let opacity = sampleOpacity elapsed animation
        let transform = sampleTransform elapsed animation
        let opacityAtRest = opacity = 1.0
        let transformAtRest = Transform.isIdentity transform

        if opacityAtRest && transformAtRest then
            // Identity-at-rest (R5): byte-identical to the static render.
            Lower.unwrap target
        else
            let folded = if opacityAtRest then target else Lower.scaleScene opacity target

            if transformAtRest then
                Lower.unwrap folded
            else
                PerspectiveNode(Transform.toPerspectiveTransform transform, folded)

    let sampleFrames (times: TimeSpan list) (animation: Animation) (target: Scene) : Scene list =
        times |> List.map (fun t -> { Nodes = [ applyAt t animation target ] })

    let isSettled (elapsed: TimeSpan) (animation: Animation) : bool =
        [ animation.Opacity |> Option.map (fun t -> t.Duration)
          animation.Transform |> Option.map (fun t -> t.Duration)
          animation.Color |> Option.map (fun t -> t.Duration) ]
        |> List.choose id
        |> List.forall (fun duration -> elapsed >= duration)

module AnimationState =
    let create (interp: 'a -> 'a -> float -> 'a) (initial: 'a) (duration: TimeSpan) (easing: Easing) : AnimationState<'a> =
        { Current = initial
          Start = initial
          Target = initial
          Elapsed = TimeSpan.Zero
          Duration = duration
          Easing = easing
          Interp = interp }

    let advance (delta: TimeSpan) (state: AnimationState<'a>) : AnimationState<'a> =
        let raw = state.Elapsed + delta

        let capped =
            if raw > state.Duration then state.Duration
            elif raw < TimeSpan.Zero then TimeSpan.Zero
            else raw

        let fraction =
            if state.Duration <= TimeSpan.Zero then
                1.0
            else
                float capped.Ticks / float state.Duration.Ticks

        let eased = Easing.apply state.Easing fraction

        let current =
            if fraction >= 1.0 then state.Target
            elif fraction <= 0.0 then state.Start
            else state.Interp state.Start state.Target eased

        { state with
            Elapsed = capped
            Current = current }

    let retarget (newTarget: 'a) (state: AnimationState<'a>) : AnimationState<'a> =
        { state with
            Start = state.Current
            Target = newTarget
            Elapsed = TimeSpan.Zero }

    let value (state: AnimationState<'a>) : 'a = state.Current

    let isActive (state: AnimationState<'a>) : bool =
        state.Elapsed < state.Duration && state.Current <> state.Target
