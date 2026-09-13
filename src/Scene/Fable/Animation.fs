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

type ClipProperty =
    | PositionX
    | PositionY
    | RotationDegrees
    | ScaleX
    | ScaleY
    | Opacity
    | Color
    | CustomScalar of string
    | PathMorph of string

type ClipValue =
    | ScalarValue of float
    | ColorValue of Color
    | PathValue of (float * float) list

type ClipKeyframe =
    { Time: TimeSpan
      Value: ClipValue
      EasingToNext: Easing }

type AnimationCue =
    { Id: string
      Time: TimeSpan
      Payload: string }

type ClipLoop =
    | Once
    | Repeat of iterations: int
    | PingPong of iterations: int

type AnimationClip =
    { Id: string
      Duration: TimeSpan
      Tracks: (ClipProperty * ClipKeyframe list) list
      Cues: AnimationCue list
      Loop: ClipLoop }

type ClipIssue =
    | EmptyClipId
    | InvalidDuration
    | EmptyTracks
    | DuplicateTrack of ClipProperty
    | EmptyCustomProperty
    | EmptyPathIdentity
    | MissingKeyframes of ClipProperty
    | InvalidKeyframeTime of ClipProperty * int
    | InvalidKeyframeValue of ClipProperty * int
    | MismatchedKeyframeValue of ClipProperty * int
    | IncompatiblePathTopology of ClipProperty
    | InvalidLoopIterations of int
    | EmptyCueId of int
    | DuplicateCueId of string
    | InvalidCueTime of string
    | UnorderedCues

type ValidatedAnimationClip = private ValidatedAnimationClip of AnimationClip

type ClipDirection =
    | Forward
    | Reverse

type ClipCueMode =
    | LiveAdvance of previousElapsed: TimeSpan
    | Seek
    | Paused

type CueOccurrence =
    { Cue: AnimationCue
      Iteration: int
      Direction: ClipDirection }

type AnimationClipSample =
    { LocalTime: TimeSpan
      Iteration: int
      Direction: ClipDirection
      Values: Map<ClipProperty, ClipValue>
      Cues: CueOccurrence list
      IsComplete: bool }

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

    // R5/P6: the `Color` tween used to be dead weight — it only fed `isSettled`. `applyAt` composes
    // opacity + transform onto the scene structurally, and the frozen wire format has no scene-wide
    // tint node to fold a colour into, so `applyAt` cannot honestly apply `Color`. Instead of leaving
    // it a success-shaped stub, expose the sampled colour here so consumers can drive their own
    // recolouring (e.g. a `Paint` fill) from the animated value; `None` when no colour tween is set.
    let sampleColor (elapsed: TimeSpan) (animation: Animation) : Color option =
        animation.Color |> Option.map (fun tween -> Tween.sample lerpColor elapsed tween)

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

module AnimationClip =
    let private finite value = not (Double.IsNaN value || Double.IsInfinity value)

    let private propertyAccepts property value =
        match property, value with
        | Color, ColorValue _ -> true
        | PathMorph _, PathValue _ -> true
        | (PositionX | PositionY | RotationDegrees | ScaleX | ScaleY | Opacity | CustomScalar _), ScalarValue _ -> true
        | _ -> false

    let private validValue value =
        match value with
        | ScalarValue scalar -> finite scalar
        | ColorValue _ -> true
        | PathValue points -> points.Length >= 2 && (points |> List.forall (fun (x, y) -> finite x && finite y))

    let private trackIssues duration (property, frames: ClipKeyframe list) =
        let identityIssues =
            match property with
            | CustomScalar id when String.IsNullOrWhiteSpace id -> [ EmptyCustomProperty ]
            | PathMorph id when String.IsNullOrWhiteSpace id -> [ EmptyPathIdentity ]
            | _ -> []

        match frames with
        | [] -> identityIssues @ [ MissingKeyframes property ]
        | _ ->
            let timeIssues =
                frames
                |> List.indexed
                |> List.choose (fun (index, frame) ->
                    let prior = if index = 0 then None else Some frames[index - 1].Time
                    if frame.Time < TimeSpan.Zero || frame.Time > duration || (prior |> Option.exists (fun value -> frame.Time <= value)) then
                        Some(InvalidKeyframeTime(property, index))
                    else None)

            let endpointIssues =
                if frames.Head.Time <> TimeSpan.Zero || frames[frames.Length - 1].Time <> duration then
                    [ InvalidKeyframeTime(property, if frames.Head.Time <> TimeSpan.Zero then 0 else frames.Length - 1) ]
                else []

            let valueIssues =
                frames
                |> List.indexed
                |> List.collect (fun (index, frame) ->
                    [ if not (propertyAccepts property frame.Value) then MismatchedKeyframeValue(property, index)
                      if not (validValue frame.Value) then InvalidKeyframeValue(property, index) ])

            let topologyIssues =
                match property with
                | PathMorph _ ->
                    let counts =
                        frames
                        |> List.choose (fun frame -> match frame.Value with PathValue points -> Some points.Length | _ -> None)
                        |> Set.ofList
                    if counts.Count > 1 then [ IncompatiblePathTopology property ] else []
                | _ -> []

            identityIssues @ timeIssues @ endpointIssues @ valueIssues @ topologyIssues

    let validate clip =
        let basic =
            [ if String.IsNullOrWhiteSpace clip.Id then EmptyClipId
              if clip.Duration <= TimeSpan.Zero || clip.Duration.TotalMilliseconds > 86400000.0 || not (finite clip.Duration.TotalMilliseconds) then InvalidDuration
              if List.isEmpty clip.Tracks then EmptyTracks
              match clip.Loop with
              | Repeat iterations
              | PingPong iterations when iterations < 1 || iterations > 10000 -> InvalidLoopIterations iterations
              | _ -> () ]

        let duplicateTracks =
            clip.Tracks
            |> List.countBy fst
            |> List.choose (fun (property, count) -> if count > 1 then Some(DuplicateTrack property) else None)

        let cueIssues =
            let identities =
                clip.Cues
                |> List.indexed
                |> List.collect (fun (index, cue) ->
                    [ if String.IsNullOrWhiteSpace cue.Id then EmptyCueId index
                      if cue.Time < TimeSpan.Zero || cue.Time > clip.Duration then InvalidCueTime cue.Id ])
            let duplicates =
                clip.Cues
                |> List.filter (fun cue -> not (String.IsNullOrWhiteSpace cue.Id))
                |> List.countBy _.Id
                |> List.choose (fun (id, count) -> if count > 1 then Some(DuplicateCueId id) else None)
            let ordered =
                if clip.Cues |> List.pairwise |> List.exists (fun (a, b) -> b.Time < a.Time) then [ UnorderedCues ] else []
            identities @ duplicates @ ordered

        let trackValidation =
            if clip.Duration <= TimeSpan.Zero then []
            else clip.Tracks |> List.collect (trackIssues clip.Duration)

        let issues = basic @ duplicateTracks @ trackValidation @ cueIssues
        if List.isEmpty issues then Ok(ValidatedAnimationClip clip) else Error issues

    let value (ValidatedAnimationClip clip) = clip

    let private interpolateValue easing progress startValue endValue =
        let t = Easing.apply easing progress
        if progress <= 0.0 then startValue
        elif progress >= 1.0 then endValue
        else
            match startValue, endValue with
            | ScalarValue a, ScalarValue b -> ScalarValue(Animation.lerpFloat a b t)
            | ColorValue a, ColorValue b -> ColorValue(Animation.lerpColor a b t)
            | PathValue a, PathValue b ->
                List.zip a b
                |> List.map (fun ((ax, ay), (bx, by)) -> Animation.lerpFloat ax bx t, Animation.lerpFloat ay by t)
                |> PathValue
            | _ -> startValue

    let private sampleTrack (localTime: TimeSpan) (frames: ClipKeyframe list) =
        match frames |> List.tryFindIndex (fun frame -> frame.Time >= localTime) with
        | None -> frames[frames.Length - 1].Value
        | Some 0 -> frames[0].Value
        | Some upper ->
            let first = frames[upper - 1]
            let second = frames[upper]
            let span = (second.Time - first.Time).TotalMilliseconds
            let progress = if span <= 0.0 then 1.0 else (localTime - first.Time).TotalMilliseconds / span
            interpolateValue first.EasingToNext progress first.Value second.Value

    let private loopCount loop =
        match loop with
        | Once -> 1
        | Repeat count
        | PingPong count -> count

    let private position (elapsed: TimeSpan) (clip: AnimationClip) =
        let elapsedMs = max 0.0 elapsed.TotalMilliseconds
        let durationMs = clip.Duration.TotalMilliseconds
        let count = loopCount clip.Loop
        let totalMs = durationMs * float count
        let complete = elapsedMs >= totalMs
        let iteration = if complete then count - 1 else int (Math.Floor(elapsedMs / durationMs))
        let rawLocal = if complete then durationMs else elapsedMs - float iteration * durationMs
        let direction =
            match clip.Loop with
            | PingPong _ when iteration % 2 = 1 -> Reverse
            | _ -> Forward
        let localMs =
            if complete then
                match direction with Forward -> durationMs | Reverse -> 0.0
            else
                match direction with Forward -> rawLocal | Reverse -> durationMs - rawLocal
        TimeSpan.FromMilliseconds localMs, iteration, direction, complete, min elapsedMs totalMs

    let private cueOccurrences (previous: float) (current: float) (clip: AnimationClip) =
        if current <= previous then []
        else
            let durationMs = clip.Duration.TotalMilliseconds
            let count = loopCount clip.Loop
            [ for iteration in 0 .. count - 1 do
                let direction =
                    match clip.Loop with
                    | PingPong _ when iteration % 2 = 1 -> Reverse
                    | _ -> Forward
                for cue in clip.Cues do
                    let local = cue.Time.TotalMilliseconds
                    let offset = match direction with Forward -> local | Reverse -> durationMs - local
                    let absolute = float iteration * durationMs + offset
                    // A ping-pong turn belongs to the pass that arrives at the endpoint. Suppress the
                    // following pass's coincident start boundary so one cue cannot fire twice at one instant.
                    let startsLaterIteration =
                        match clip.Loop with
                        | PingPong _ -> iteration > 0 && offset = 0.0
                        | _ -> false
                    if not startsLaterIteration && absolute > previous && absolute <= current then
                        yield absolute, { Cue = cue; Iteration = iteration; Direction = direction } ]
            |> List.sortBy (fun (absolute, occurrence) -> absolute, occurrence.Cue.Id)
            |> List.map snd

    let sample elapsed cueMode (ValidatedAnimationClip clip) =
        let localTime, iteration, direction, complete, boundedElapsed = position elapsed clip
        let values = clip.Tracks |> List.map (fun (property, frames) -> property, sampleTrack localTime frames) |> Map.ofList
        let cues =
            match cueMode with
            | Seek
            | Paused -> []
            | LiveAdvance previous -> cueOccurrences previous.TotalMilliseconds boundedElapsed clip
        { LocalTime = localTime
          Iteration = iteration
          Direction = direction
          Values = values
          Cues = cues
          IsComplete = complete }
