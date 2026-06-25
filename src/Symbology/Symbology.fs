namespace FS.GG.UI.Symbology

open System
open FS.GG.UI.Scene

type Faction =
    | Ally
    | Enemy
    | Neutral
    | Custom of Color

type Klass =
    | Mobile
    | Heavy
    | Scout

type Sigil =
    | Bolt
    | Ring
    | Fang
    | Mark of PathSpec

type TokenState =
    | Confirmed
    | Suspected

type Motion =
    | Idle
    | Pulse
    | Spin
    | Blink
    | Damage
    | Moving

type LabelRun =
    { Text: string
      Color: Color option
      Weight: int option
      Scale: float option }

[<RequireQualifiedAccess>]
type LabelText =
    | Plain of string
    | Rich of LabelRun list

type Token =
    { Cx: float
      Cy: float
      R: float
      Heading: float
      Faction: Faction
      Klass: Klass
      Sigil: Sigil
      State: TokenState
      Threat: float
      Charge: float
      Speed: int
      Health: float
      Shield: bool
      Label: LabelText option }

[<RequireQualifiedAccess>]
type Grammar =
    | Token
    | Badge
    | Ring

module Symbology =

    let private clamp01 (v: float) = max 0.0 (min 1.0 v)

    // Saturated faction palette — encodes affiliation on STROKE HUE only. State semantics ride the
    // dash channel (Confirmed/Suspected), so faction and state never share the hue channel (FR-019).
    let private factionColor (f: Faction) : Color =
        match f with
        | Ally -> Colors.rgb 24uy 144uy 255uy
        | Enemy -> Colors.rgb 245uy 34uy 45uy
        | Neutral -> Colors.rgb 250uy 173uy 20uy
        | Custom c -> c

    // Linear interpolate a -> b by t in [0,1]; pure and deterministic.
    let private lerpColor (a: Color) (b: Color) (t: float) : Color =
        let t = clamp01 t
        let lerp (x: byte) (y: byte) = byte (float x + (float y - float x) * t)
        Colors.rgba (lerp a.Red b.Red) (lerp a.Green b.Green) (lerp a.Blue b.Blue) (lerp a.Alpha b.Alpha)

    // Rotate p about (cx,cy) by `angle` radians. The "point transform" heading channel — the body,
    // sigil, and tail rotate rigidly; the health/shield gauges stay screen-aligned.
    let private rotate (cx: float) (cy: float) (angle: float) (p: Point) : Point =
        let s = sin angle
        let c = cos angle
        let dx = p.X - cx
        let dy = p.Y - cy
        { X = cx + dx * c - dy * s
          Y = cy + dx * s + dy * c }

    // Class -> fixed silhouette (unit coords, north-up nose at (0,-1)). FR-005.
    let private silhouette (k: Klass) : (float * float) list =
        match k with
        | Mobile -> [ (0.0, -1.0); (0.78, 0.62); (0.0, 0.28); (-0.78, 0.62) ]
        | Heavy -> [ (0.0, -0.9); (0.78, -0.45); (0.78, 0.45); (0.0, 0.9); (-0.78, 0.45); (-0.78, -0.45) ]
        | Scout -> [ (0.0, -1.0); (0.45, 0.0); (0.0, 1.0); (-0.45, 0.0) ]

    let private bodyPath (t: Token) : PathSpec =
        let pts =
            silhouette t.Klass
            |> List.map (fun (ux, uy) -> rotate t.Cx t.Cy t.Heading { X = t.Cx + ux * t.R; Y = t.Cy + uy * t.R })

        match pts with
        | [] -> Path.create Winding []
        | first :: rest ->
            Path.create
                Winding
                ([ Path.moveTo first.X first.Y ]
                 @ (rest |> List.map (fun p -> Path.lineTo p.X p.Y))
                 @ [ Path.close ])

    // Stroke hue -> faction; stroke width -> threat (~4 ordered levels); dash -> inspection state.
    let private strokePaint (t: Token) : Paint =
        let width = 1.0 + 5.0 * clamp01 t.Threat

        let basePaint =
            Paint.stroke (factionColor t.Faction) width
            |> Paint.withStrokeJoin RoundJoin
            |> Paint.withStrokeCap Round

        match t.State with
        | Confirmed -> basePaint
        | Suspected -> basePaint |> Paint.withPathEffect (Dash([ 7.0; 5.0 ], 0.0))

    // Interior radial gradient -> charge/energy. Inner alpha scales with charge (~4 ordered levels).
    let private chargeFill (t: Token) : Scene =
        if t.R <= 0.0 then
            Scene.empty
        else
            let charge = clamp01 t.Charge
            let c = factionColor t.Faction
            let inner = Colors.rgba c.Red c.Green c.Blue (byte (40.0 + 180.0 * charge))
            let outer = Colors.rgba c.Red c.Green c.Blue 0uy
            let center = { X = t.Cx; Y = t.Cy }
            let shader = RadialGradient(center, t.R * 0.95, [ inner; outer ])
            let paint = Paint.fill Colors.transparent |> Paint.withShader shader
            let bounds = { X = t.Cx - t.R; Y = t.Cy - t.R; Width = t.R * 2.0; Height = t.R * 2.0 }
            Scene.ellipse bounds paint

    // Centre identity mark (rotates with the body). No label text (FR-022).
    let private sigilScene (t: Token) : Scene =
        let paint = Paint.stroke (factionColor t.Faction) 1.5 |> Paint.withStrokeCap Round
        let r = t.R * 0.42
        let pt ux uy = rotate t.Cx t.Cy t.Heading { X = t.Cx + ux * r; Y = t.Cy + uy * r }

        match t.Sigil with
        | Ring ->
            let bounds = { X = t.Cx - r; Y = t.Cy - r; Width = r * 2.0; Height = r * 2.0 }
            Scene.ellipse bounds paint
        | Bolt ->
            let p1 = pt 0.2 (-0.9)
            let p2 = pt (-0.3) 0.05
            let p3 = pt 0.25 0.05
            let p4 = pt (-0.2) 0.9

            Scene.path
                (Path.create
                    Winding
                    [ Path.moveTo p1.X p1.Y
                      Path.lineTo p2.X p2.Y
                      Path.lineTo p3.X p3.Y
                      Path.lineTo p4.X p4.Y ])
                paint
        | Fang ->
            let p1 = pt (-0.6) (-0.5)
            let p2 = pt 0.6 (-0.5)
            let p3 = pt 0.0 0.9

            Scene.path
                (Path.create
                    Winding
                    [ Path.moveTo p1.X p1.Y
                      Path.lineTo p2.X p2.Y
                      Path.lineTo p3.X p3.Y
                      Path.close ])
                paint
        | Mark spec -> Scene.path spec paint

    // Belly arc -> health (length + green->red hue). Screen-aligned: stays at the bottom under rotation.
    let private healthArc (t: Token) : Scene =
        if t.R <= 0.0 then
            Scene.empty
        else
            let h = clamp01 t.Health
            let green = Colors.rgb 82uy 196uy 26uy
            let red = Colors.rgb 245uy 34uy 45uy
            let color = lerpColor red green h
            let ar = t.R * 1.18
            let bounds = { X = t.Cx - ar; Y = t.Cy - ar; Width = ar * 2.0; Height = ar * 2.0 }
            let sweep = 130.0 * h
            let start = 90.0 - sweep / 2.0
            let paint = Paint.stroke color 3.0 |> Paint.withStrokeCap Round
            Scene.arc bounds start sweep paint

    // Tail beads -> speed (0..4). Trail behind the body (opposite the nose), rotating with heading.
    let private tailBeads (t: Token) : Scene =
        let n = max 0 (min 4 t.Speed)

        if n = 0 || t.R <= 0.0 then
            Scene.empty
        else
            let color = factionColor t.Faction

            let beads =
                [ for i in 1..n ->
                      let dist = 1.1 + 0.42 * float i
                      let p = rotate t.Cx t.Cy t.Heading { X = t.Cx; Y = t.Cy + dist * t.R * 0.5 }
                      let br = t.R * (0.16 - 0.015 * float i)
                      Scene.circle p (max 1.0 br) color ]

            Scene.group beads

    // Corner mount -> boolean shield flag. Screen-aligned inspection slot (top-right corner).
    let private shieldMount (t: Token) : Scene =
        if not t.Shield || t.R <= 0.0 then
            Scene.empty
        else
            let color = Colors.rgb 19uy 194uy 194uy
            let p = { X = t.Cx + t.R * 0.85; Y = t.Cy - t.R * 0.85 }
            Scene.circle p (max 2.0 (t.R * 0.18)) color

    // FR-020: a Token with no drawable area renders a visible placeholder, never a blank/crash.
    let private placeholder (t: Token) : Scene =
        let s = 6.0
        let color = Colors.rgb 140uy 140uy 140uy
        let paint = Paint.stroke color 1.5

        let rectPath =
            Path.create
                Winding
                [ Path.moveTo (t.Cx - s) (t.Cy - s)
                  Path.lineTo (t.Cx + s) (t.Cy - s)
                  Path.lineTo (t.Cx + s) (t.Cy + s)
                  Path.lineTo (t.Cx - s) (t.Cy + s)
                  Path.close ]

        Scene.group
            [ Scene.path rectPath paint
              Scene.line { X = t.Cx - s; Y = t.Cy - s } { X = t.Cx + s; Y = t.Cy + s } paint
              Scene.line { X = t.Cx - s; Y = t.Cy + s } { X = t.Cx + s; Y = t.Cy - s } paint ]

    let defaultToken: Token =
        { Cx = 0.0
          Cy = 0.0
          R = 1.0
          Heading = 0.0
          Faction = Neutral
          Klass = Mobile
          Sigil = Ring
          State = Confirmed
          Threat = 0.5
          Charge = 0.5
          Speed = 0
          Health = 0.5
          Shield = false
          Label = None }

    // ---- Rich-text label constructors (feature 198) ----
    let plainLabel (text: string) : LabelText = LabelText.Plain text

    let run (text: string) : LabelRun =
        { Text = text; Color = None; Weight = None; Scale = None }

    let richLabel (runs: LabelRun list) : LabelText = LabelText.Rich runs

    // ---- Optional identity-label channel (FR-001..FR-009) -------------------------------------
    // Screen-aligned short text drawn in a per-grammar label region. The node is emitted ONLY when a
    // label is present and non-blank, so a `Label = None` (or empty/whitespace) token's element list is
    // byte-IDENTICAL to the pre-feature symbol (FR-002/SC-003) — the helpers return `Scene option` and
    // the grammars append the node only on `Some`. Pure scene-only: consumes the already-referenced
    // FS.GG.UI.Scene text vocabulary (measureTextResolved / glyphRunProof), no raster/GL/IO (FR-014).

    let private labelInk = Colors.rgb 235uy 235uy 235uy
    let private ellipsis = "…"

    // Weight-aware label font (feature 198). `labelFontWith None size` reproduces the pre-198
    // `labelFontOf` exactly (`{ Family = None; Size; Weight = None }`), so the plain/all-default path
    // stays BYTE-IDENTICAL; a styled run passes its own `Weight` through to `FontSpec.Weight` (FR-003).
    let private labelFontWith (weight: int option) (size: float) : FontSpec =
        { Family = None; Size = max 1.0 size; Weight = weight }

    let private labelFontOf (size: float) : FontSpec = labelFontWith None size

    let private labelWidthW (weight: int option) (text: string) (size: float) : float =
        (Scene.measureTextResolved text (labelFontWith weight size)).Width

    let private labelWidth (text: string) (size: float) : float = labelWidthW None text size

    // Measured line-height for stacking (FR-003 / research.md R4): the resolved `TextMetrics.Height` of the
    // base font, falling back to `baseSize * 1.15` when the provider reports a non-positive height. Pure and
    // deterministic for a fixed measurement provider; only affects lines below the first (i >= 1), so a
    // single-line label is unaffected (its baseline stays the spec-196 anchor — zero drift).
    let private lineHeightOfW (weight: int option) (baseSize: float) : float =
        let h = (Scene.measureTextResolved "Mg" (labelFontWith weight baseSize)).Height
        if h > 0.0 then h else baseSize * 1.15

    let private lineHeightOf (baseSize: float) : float = lineHeightOfW None baseSize

    // Fit the trimmed label to `regionWidth` via real text measurement (FR-005): empty/whitespace => None;
    // else shrink the font toward a floor, and if still over at the floor, ellipsis-truncate at a measured
    // glyph boundary (re-measuring the candidate incl. the ellipsis). The result is always within the
    // region width and never cut mid-glyph (research.md R3). Deterministic for a fixed measurement provider.
    // Weight-aware fit (feature 198): identical to the pre-198 `fitLabel` for `weight = None` (it routes
    // through `labelFontWith None`/`labelWidthW None`, byte-identical to the old `labelFontOf`/`labelWidth`),
    // so the plain path is unchanged; a styled segment fits in ITS OWN weight + scaled size (FR-006). A
    // single over-wide run with no wrap point degrades through exactly this shrink → ellipsis path per
    // segment, so no segment ever clips mid-glyph or overflows the region (research.md R3).
    let private fitLabelW (weight: int option) (regionWidth: float) (baseSize: float) (raw: string) : (string * FontSpec) option =
        if String.IsNullOrWhiteSpace raw then
            None
        else
            let text = raw.Trim()
            let wBase = labelWidthW weight text baseSize

            if wBase <= regionWidth || regionWidth <= 0.0 then
                Some(text, labelFontWith weight baseSize)
            else
                let floor = baseSize * 0.62
                // Linear-measure estimate of the size that fits the whole string; verify before using it,
                // so a non-linear real measurer can never push the drawn label past the region.
                let est = baseSize * regionWidth / wBase

                if est >= floor && labelWidthW weight text est <= regionWidth then
                    Some(text, labelFontWith weight est)
                else
                    // Truncate at the floor size: longest prefix whose `prefix + ellipsis` measures within.
                    let fits (s: string) = labelWidthW weight (s + ellipsis) floor <= regionWidth

                    let rec longest (n: int) =
                        if n <= 0 then ""
                        elif fits (text.Substring(0, n)) then text.Substring(0, n)
                        else longest (n - 1)

                    match longest (text.Length - 1) with
                    | "" -> Some(ellipsis, labelFontWith weight floor) // even one glyph + ellipsis overflows: the ellipsis alone
                    | prefix -> Some(prefix + ellipsis, labelFontWith weight floor)

    let private fitLabel (regionWidth: float) (baseSize: float) (raw: string) : (string * FontSpec) option =
        fitLabelW None regionWidth baseSize raw

    // ---- Multi-line widening (feature 197, FR-001/FR-003/FR-005/FR-006) ----------------------------
    // The label is interpreted as possibly multi-line: embedded `\n`/`\r\n` are hard breaks; a long line
    // soft-wraps to the region width. No new public surface — multi-line rides the existing
    // `Label : string option`. A no-label token and a one-line-fitting label stay byte-identical to the
    // pre-feature / spec-196 renders (layered zero-drift), because both reduce to the exact 196 child list.

    // Greedy WHITESPACE word-wrap of one segment to `regionWidth` (measured at `baseSize`): pack words while
    // `prefix + " " + word` fits, else start a new line; NEVER break inside a word (research.md R2). A single
    // word wider than the region has no wrap point and becomes its own (over-wide) line — handled downstream
    // by the per-line `fitLabel` (shrink → ellipsis). Pure fold (no mutable); deterministic per provider.
    let private wrapSegment (regionWidth: float) (baseSize: float) (segment: string) : string list =
        match segment.Split([| ' '; '\t' |], StringSplitOptions.RemoveEmptyEntries) |> List.ofArray with
        | [] -> []
        | first :: rest ->
            let completed, current =
                rest
                |> List.fold
                    (fun (acc, cur) (w: string) ->
                        let candidate = cur + " " + w

                        if regionWidth <= 0.0 || labelWidth candidate baseSize <= regionWidth then
                            (acc, candidate)
                        else
                            (cur :: acc, w))
                    ([], first)

            List.rev (current :: completed)

    // Normalise a raw label into the ordered set of lines to draw (FR-001/FR-005/FR-006): split on hard
    // breaks, trim, drop empty/whitespace segments (deterministic collapse), greedy-wrap each to the region,
    // then CAP to the grammar's `budget`; when the cap drops lines, the last kept line gains an ellipsis to
    // signal the surplus (re-fitted ≤ region by the per-line `fitLabel`). Result length is `0 … budget`.
    let private wrapLabel (regionWidth: float) (baseSize: float) (budget: int) (raw: string) : string list =
        if String.IsNullOrWhiteSpace raw then
            []
        else
            let wrapped =
                raw.Replace("\r\n", "\n").Split('\n')
                |> Array.map (fun s -> s.Trim())
                |> Array.filter (fun s -> s.Length > 0)
                |> List.ofArray
                |> List.collect (wrapSegment regionWidth baseSize)

            let budget = max 1 budget

            if wrapped.Length <= budget then
                wrapped
            else
                // Drop the surplus; mark the last KEPT line with an ellipsis (FR-005 / SC-005).
                let kept = wrapped |> List.truncate (budget - 1)
                let lastKept = wrapped |> List.item (budget - 1)
                kept @ [ lastKept + ellipsis ]

    // Emit one centred glyph-run node per wrapped line: the first at `baselineY` (the spec-196 anchor) and
    // each subsequent line a measured `lineHeight` lower (downward stacking, FR-003). Each line passes
    // through the existing `fitLabel` so it is guaranteed ≤ region width and never clipped mid-glyph
    // (FR-005). Returns [] when there is no drawable line — byte-identical to no-label; a single fitting
    // line reproduces spec 196 exactly (one node, same baseline — FR-002/SC-003). `glyphRunProof` carries
    // per-glyph `Missing`/`FallbackMode` evidence so the render edge can verify tofu-free output (FR-004);
    // the pure library never installs/requires a measurer and never throws without one (FR-009).
    let private labelNodes
        (centerX: float)
        (baselineY: float)
        (regionWidth: float)
        (baseSize: float)
        (lineHeight: float)
        (budget: int)
        (label: string option)
        : Scene list =
        match label with
        | None -> []
        | Some raw ->
            wrapLabel regionWidth baseSize budget raw
            |> List.choose (fitLabel regionWidth baseSize)
            |> List.mapi (fun i (text, font) ->
                let w = (Scene.measureTextResolved text font).Width
                let pos = { X = centerX - w / 2.0; Y = baselineY + lineHeight * float i }
                Scene.glyphRunProof pos text font (Paint.fill labelInk))

    // ---- Rich-text runs (feature 198, FR-001..FR-013) -----------------------------------------------
    // Per-run colour / weight / size styling of the SAME label channel. The zero-drift cases (no label,
    // plain, all-default `Rich`) delegate to the VERBATIM spec-197 path above (`labelNodes`), so every
    // pinned golden stays byte-identical (FR-002/SC-003); only a `Rich` label with ≥1 non-default run
    // reaches `richLabelNodes`. Pure scene-only: reuses `measureTextResolved`/`glyphRunProof`/`FontSpec`/
    // `Color` — no new vocabulary, no raster/GL/IO, never installs/requires a measurer (FR-016/FR-010).

    // A run is "default-styled" when every attribute is unset (Scale = Some 1.0 is also the default).
    let private isDefaultRun (r: LabelRun) =
        r.Color = None && r.Weight = None && (r.Scale = None || r.Scale = Some 1.0)

    // The plain-equivalent of an all-default run list: concatenate the run texts (each run keeps its own
    // interior spacing). `Rich [ run "HMR-7" ]` ⇒ "HMR-7"; `Rich []`/all-empty ⇒ "" ⇒ no label (FR-007).
    let private joinRuns (runs: LabelRun list) =
        runs |> List.map (fun r -> r.Text) |> String.concat ""

    // A run resolved to its drawable style at a grammar base size: colour defaults to `labelInk`, size is
    // `base * scale` (floored at 1.0), weight passes straight through (FR-003 / research.md R4).
    type private RunStyle =
        { Color: Color
          Weight: int option
          Size: float }

    let private resolveStyle (baseSize: float) (r: LabelRun) : RunStyle =
        { Color = r.Color |> Option.defaultValue labelInk
          Weight = r.Weight
          Size = max 1.0 (baseSize * (r.Scale |> Option.defaultValue 1.0)) }

    // An atom of the inline stream: a styled word, or a hard line break (research.md R2).
    type private Atom =
        | Word of string * RunStyle
        | LineBreak

    // Atomise the run sequence in reading order: split each run's `Text` on `\n`/`\r\n` (hard breaks),
    // then on whitespace into words carrying the run's resolved style; empty/whitespace runs and words
    // drop (FR-007). A `\n` between two segments of a run becomes a `LineBreak` atom.
    let private atomsOf (baseSize: float) (runs: LabelRun list) : Atom list =
        runs
        |> List.collect (fun r ->
            let style = resolveStyle baseSize r

            r.Text.Replace("\r\n", "\n").Split('\n')
            |> Array.toList
            |> List.mapi (fun i seg ->
                let words =
                    seg.Split([| ' '; '\t' |], StringSplitOptions.RemoveEmptyEntries)
                    |> Array.toList
                    |> List.map (fun w -> Word(w, style))

                if i = 0 then words else LineBreak :: words)
            |> List.concat)

    // Greedy inline break: pack words while the running line width (each word measured in its OWN resolved
    // font, plus a base-size inter-word space) ≤ region; a `LineBreak` forces a new line; never break inside
    // a word (research.md R2). Pure fold (no mutable), mirroring `wrapSegment`. Empty lines are dropped.
    let private breakLines (regionWidth: float) (baseSize: float) (atoms: Atom list) : (string * RunStyle) list list =
        let spaceW = labelWidth " " baseSize

        let completed, current, _ =
            atoms
            |> List.fold
                (fun (lines, cur, w) atom ->
                    match atom with
                    | LineBreak -> (List.rev cur :: lines, [], 0.0)
                    | Word(text, style) ->
                        let ww = labelWidthW style.Weight text style.Size

                        if List.isEmpty cur then
                            (lines, [ (text, style) ], ww)
                        else
                            let nw = w + spaceW + ww

                            if regionWidth <= 0.0 || nw <= regionWidth then
                                (lines, (text, style) :: cur, nw)
                            else
                                (List.rev cur :: lines, [ (text, style) ], ww))
                ([], [], 0.0)

        List.rev (List.rev current :: completed)
        |> List.filter (List.isEmpty >> not)

    // Cap to the grammar budget; when lines are dropped, append the ellipsis to the LAST word of the last
    // kept line (re-fitted ≤ region downstream) so the surplus is signalled (FR-006/SC-005).
    let private capLines (budget: int) (lines: (string * RunStyle) list list) =
        let budget = max 1 budget

        if List.length lines <= budget then
            lines
        else
            let kept = lines |> List.truncate (budget - 1)

            let lastKept =
                match lines |> List.item (budget - 1) |> List.rev with
                | (t, st) :: restRev -> List.rev ((t + ellipsis, st) :: restRev)
                | [] -> []

            kept @ [ lastKept ]

    // Group a line's words into contiguous same-style segments; each segment's words rejoin with a space.
    let private segmentsOf (line: (string * RunStyle) list) : (string * RunStyle) list =
        line
        |> List.fold
            (fun acc (t, st) ->
                match acc with
                | (pt, pst) :: rest when pst = st -> (pt + " " + t, st) :: rest
                | _ -> (t, st) :: acc)
            []
        |> List.rev

    // Inline-run layout (FR-004/FR-006): atomise → greedy break → cap+ellipsis → per line emit one centred
    // `glyphRunProof` per contiguous same-style segment, fitted in its own weight+size; the first line at
    // the spec-197 baseline, subsequent lines stacked downward by the per-line max run height (common
    // baseline). Returns [] for an empty/all-whitespace run set — no node, no throw (FR-007).
    let private richLabelNodes
        (centerX: float)
        (baselineY: float)
        (regionWidth: float)
        (baseSize: float)
        (budget: int)
        (runs: LabelRun list)
        : Scene list =
        let spaceW = labelWidth " " baseSize

        let lines = atomsOf baseSize runs |> breakLines regionWidth baseSize |> capLines budget

        // Per-line height = tallest run on the line; baseline offsets are cumulative prefix sums.
        let heights =
            lines
            |> List.map (fun line -> line |> List.map (fun (_, st) -> lineHeightOfW st.Weight st.Size) |> List.fold max 0.0)

        let offsets = heights |> List.scan (+) 0.0 // [0; h0; h0+h1; …]; entry i is the offset of line i

        lines
        |> List.mapi (fun i line ->
            let y = baselineY + List.item i offsets

            // Fit each segment in its own weight+size (≤ region, never clipped mid-glyph).
            let segs =
                segmentsOf line
                |> List.choose (fun (text, st) ->
                    match fitLabelW st.Weight regionWidth st.Size text with
                    | Some(ftext, font) -> Some(ftext, font, st.Color)
                    | None -> None)

            let widths = segs |> List.map (fun (t, font, _) -> (Scene.measureTextResolved t font).Width)
            let total = List.sum widths + spaceW * float (max 0 (List.length segs - 1))
            let startX = centerX - total / 2.0

            // Place left-to-right from the centred start; one node per segment at the shared baseline.
            (List.zip segs widths
             |> List.fold
                 (fun (x, acc) ((t, font, color), w) ->
                     let node = Scene.glyphRunProof { X = x; Y = y } t font (Paint.fill color)
                     (x + w + spaceW, node :: acc))
                 (startX, []))
            |> snd
            |> List.rev)
        |> List.concat

    // Per-grammar label dispatch (research.md R6): the structural zero-drift router. `None` and the
    // plain / all-default cases delegate to the VERBATIM `labelNodes` (byte-identical to spec 197); only a
    // `Rich` label with a non-default run takes `richLabelNodes`. `lineHeight`/`budget` mirror spec 197.
    let private labelDispatch
        (centerX: float)
        (baselineY: float)
        (regionWidth: float)
        (baseSize: float)
        (lineHeight: float)
        (budget: int)
        (label: LabelText option)
        : Scene list =
        match label with
        | None -> []
        | Some(LabelText.Plain s) -> labelNodes centerX baselineY regionWidth baseSize lineHeight budget (Some s)
        | Some(LabelText.Rich runs) ->
            if List.forall isDefaultRun runs then
                labelNodes centerX baselineY regionWidth baseSize lineHeight budget (Some(joinRuns runs))
            else
                richLabelNodes centerX baselineY regionWidth baseSize budget runs

    // Per-grammar label region (provisional geometry — the contract is FR-004: sited, observable,
    // non-overlapping; coordinates + per-grammar line budgets are a design-loop detail, see data-model.md).
    // Each sits in the one uncrowded zone of its grammar, screen-aligned (never rotates with Heading); the
    // FIRST line keeps spec 196's exact baseline / region width / base size (the zero-drift anchor).
    let private tokenLabelNodes (t: Token) : Scene list =
        let baseSize = t.R * 0.5
        labelDispatch t.Cx (t.Cy + t.R * 1.5) (t.R * 1.9) baseSize (lineHeightOf baseSize) 3 t.Label // caption strip below the health arc (≤ 3 lines)

    let private badgeLabelNodes (t: Token) : Scene list =
        let baseSize = t.R * 0.42
        labelDispatch t.Cx (t.Cy + t.R * 1.42) (t.R * 1.7) baseSize (lineHeightOf baseSize) 2 t.Label // band below the health bar / pips (≤ 2 lines)

    let private ringLabelNodes (t: Token) : Scene list =
        let baseSize = t.R * 0.34
        labelDispatch t.Cx (t.Cy + t.R * 0.52) (t.R * 1.05) baseSize (lineHeightOf baseSize) 2 t.Label // caption beneath the sigil, inner disc (≤ 2 lines)

    // Append the label line nodes to a grammar's child list as bare siblings (research.md R5): `[]` ⇒
    // `Scene.group nodes` (byte-identical to no-label), `[one]` ⇒ `nodes @ [one]` (byte-identical to the
    // spec-196 single-line label). Never wraps the lines in an extra group — that would drift the goldens.
    let private withLabel (lineNodes: Scene list) (nodes: Scene list) : Scene =
        Scene.group (nodes @ lineNodes)

    let private drawSymbol (t: Token) : Scene =
        if t.R <= 0.0 then
            placeholder t // placeholder rule wins over the label (FR-007); no label on a degenerate token
        else
            withLabel
                (tokenLabelNodes t)
                [ chargeFill t
                  Scene.path (bodyPath t) (strokePaint t)
                  sigilScene t
                  tailBeads t
                  healthArc t
                  shieldMount t ]

    let token (token: Token) : Scene = drawSymbol token

    let animate (motion: Motion) (token: Token) (phase: float) : Scene =
        let t = token
        let ph = phase - floor phase
        let baseSymbol = drawSymbol t

        match motion with
        | Idle -> baseSymbol
        | Pulse ->
            if t.R <= 0.0 then
                baseSymbol
            else
                let rr = t.R * (1.1 + 0.6 * ph)
                let alpha = byte (180.0 * (1.0 - ph))
                let c = factionColor t.Faction

                let ring =
                    Scene.ellipse
                        { X = t.Cx - rr
                          Y = t.Cy - rr
                          Width = rr * 2.0
                          Height = rr * 2.0 }
                        (Paint.stroke (Colors.rgba c.Red c.Green c.Blue alpha) 2.0)

                Scene.group [ baseSymbol; ring ]
        | Spin ->
            let ang = ph * 2.0 * Math.PI
            let rr = t.R * 1.05
            let p1 = rotate t.Cx t.Cy ang { X = t.Cx; Y = t.Cy - rr }
            let p2 = rotate t.Cx t.Cy ang { X = t.Cx; Y = t.Cy - rr * 1.25 }

            Scene.group
                [ baseSymbol
                  Scene.line p1 p2 (Paint.stroke (factionColor t.Faction) 2.5 |> Paint.withStrokeCap Round) ]
        | Blink ->
            if ph < 0.5 then
                let p = { X = t.Cx - t.R * 0.85; Y = t.Cy - t.R * 0.85 }
                Scene.group [ baseSymbol; Scene.circle p (max 2.0 (t.R * 0.2)) (Colors.rgb 245uy 34uy 45uy) ]
            else
                baseSymbol
        | Damage ->
            let rr = t.R * (1.0 + 0.15 * sin (ph * 2.0 * Math.PI))
            let wash = Colors.rgba 245uy 34uy 45uy 90uy

            Scene.group
                [ baseSymbol
                  Scene.ellipse
                      { X = t.Cx - rr
                        Y = t.Cy - rr
                        Width = rr * 2.0
                        Height = rr * 2.0 }
                      (Paint.stroke wash 3.0) ]
        | Moving ->
            let off = rotate t.Cx t.Cy t.Heading { X = t.Cx; Y = t.Cy + t.R * (0.6 + ph) }
            let dx = off.X - t.Cx
            let dy = off.Y - t.Cy
            let echo = drawSymbol { t with Cx = t.Cx - dx * 0.5; Cy = t.Cy - dy * 0.5 }
            Scene.group [ echo; baseSymbol ]

    let gallery (cols: int) (spacing: float) (tokens: Token list) : Scene =
        let cols = max 1 cols

        tokens
        |> List.mapi (fun i tk ->
            let row = i / cols
            let col = i % cols
            let cx = spacing * (float col + 0.5)
            let cy = spacing * (float row + 0.5)
            token { tk with Cx = cx; Cy = cy })
        |> Scene.group

    let filmstrip (samples: int) (entries: (Motion * Token) list) : Scene =
        let samples = max 1 samples
        let maxR = entries |> List.fold (fun acc (_, tk) -> max acc tk.R) 1.0
        let spacing = maxR * 2.6

        entries
        |> List.mapi (fun row (m, tk) ->
            [ for s in 0 .. samples - 1 ->
                  let phase = if samples = 1 then 0.0 else float s / float (samples - 1)
                  let cx = spacing * (float s + 0.5)
                  let cy = spacing * (float row + 0.5)
                  animate m { tk with Cx = cx; Cy = cy } phase ])
        |> List.concat
        |> Scene.group

    // ---- NEW grammars (FR-001) ----
    // Both reuse the Token grammar's channel helpers (clamp01/factionColor/lerpColor/strokePaint/
    // chargeFill/sigilScene/shieldMount/placeholder) so faction/threat/state/charge/shield/sigil read
    // identically across grammars. Badge & Ring are SCREEN-ALIGNED (FR-006): the frame/ring never rotate
    // with heading — heading is a discrete edge indicator only.

    let private healthGreen = Colors.rgb 82uy 196uy 26uy
    let private healthRed = Colors.rgb 245uy 34uy 45uy

    // Badge frame polygon — class drives the corner profile (Klass channel). Screen-aligned (no rotation).
    let private badgeFramePoints (k: Klass) (cx: float) (cy: float) (s: float) : Point list =
        match k with
        | Heavy -> [ { X = cx - s; Y = cy - s }; { X = cx + s; Y = cy - s }; { X = cx + s; Y = cy + s }; { X = cx - s; Y = cy + s } ]
        | Scout -> [ { X = cx; Y = cy - s }; { X = cx + s; Y = cy }; { X = cx; Y = cy + s }; { X = cx - s; Y = cy } ]
        | Mobile ->
            let o = s * 0.41
            [ { X = cx - o; Y = cy - s }
              { X = cx + o; Y = cy - s }
              { X = cx + s; Y = cy - o }
              { X = cx + s; Y = cy + o }
              { X = cx + o; Y = cy + s }
              { X = cx - o; Y = cy + s }
              { X = cx - s; Y = cy + o }
              { X = cx - s; Y = cy - o } ]

    let private polyPath (pts: Point list) : PathSpec =
        match pts with
        | [] -> Path.create Winding []
        | first :: rest ->
            Path.create
                Winding
                ([ Path.moveTo first.X first.Y ]
                 @ (rest |> List.map (fun p -> Path.lineTo p.X p.Y))
                 @ [ Path.close ])

    // Bottom health bar -> health (length + green->red hue). Screen-aligned under the frame.
    let private badgeHealthBar (t: Token) : Scene =
        if t.R <= 0.0 then
            Scene.empty
        else
            let h = clamp01 t.Health
            let color = lerpColor healthRed healthGreen h
            let fullW = t.R * 1.4
            let w = fullW * h
            let y = t.Cy + t.R * 1.05
            let x0 = t.Cx - fullW / 2.0
            let paint = Paint.stroke color 3.0 |> Paint.withStrokeCap Round
            Scene.line { X = x0; Y = y } { X = x0 + w; Y = y } paint

    // Pip row -> speed (0..4). Screen-aligned beneath the sigil.
    let private badgeSpeedPips (t: Token) : Scene =
        let n = max 0 (min 4 t.Speed)

        if n = 0 || t.R <= 0.0 then
            Scene.empty
        else
            let color = factionColor t.Faction
            let y = t.Cy + t.R * 0.68
            let gap = t.R * 0.34
            let x0 = t.Cx - gap * float (n - 1) / 2.0
            Scene.group [ for i in 0 .. n - 1 -> Scene.circle { X = x0 + gap * float i; Y = y } (max 1.0 (t.R * 0.09)) color ]

    // Discrete edge pip -> heading (FR-006). The frame stays screen-aligned; only the pip moves around it.
    // Heading 0 points north (matches the Token nose at (0,-1)).
    let private badgeHeadingPip (t: Token) : Scene =
        if t.R <= 0.0 then
            Scene.empty
        else
            let r = t.R * 1.0
            let p = { X = t.Cx + sin t.Heading * r; Y = t.Cy - cos t.Heading * r }
            Scene.circle p (max 1.5 (t.R * 0.12)) (factionColor t.Faction)

    let private drawBadge (t: Token) : Scene =
        if t.R <= 0.0 then
            placeholder t // placeholder rule wins over the label (FR-007)
        else
            withLabel
                (badgeLabelNodes t)
                [ chargeFill t
                  Scene.path (polyPath (badgeFramePoints t.Klass t.Cx t.Cy t.R)) (strokePaint t)
                  sigilScene { t with Heading = 0.0 } // screen-aligned centre identity (heading is the edge pip)
                  badgeSpeedPips t
                  badgeHealthBar t
                  shieldMount t
                  badgeHeadingPip t ]

    let badge (token: Token) : Scene = drawBadge token

    // Ring health gauge -> health. A fixed-start (top, screen-aligned) arc sweep built from discrete
    // segments: the number of lit segments grows MONOTONICALLY with Health (FR-007), so the sweep extent
    // (and the rendered element count) is monotone non-decreasing in Health. Hue lerps green->red.
    let private ringMaxHealthSegments = 24

    let private ringHealthSegments (h: float) : int =
        // floor is monotone non-decreasing; clamp01 keeps [0,1] -> [0,maxSeg]. The +eps avoids a float
        // floor cliff exactly at segment boundaries without ever breaking monotonicity.
        int (floor (float ringMaxHealthSegments * clamp01 h + 1e-9))

    let private ringHealthArc (t: Token) : Scene =
        let lit = ringHealthSegments t.Health

        if lit <= 0 || t.R <= 0.0 then
            Scene.empty
        else
            let color = lerpColor healthRed healthGreen (clamp01 t.Health)
            let ar = t.R * 1.16
            let bounds = { X = t.Cx - ar; Y = t.Cy - ar; Width = ar * 2.0; Height = ar * 2.0 }
            let maxSweep = 300.0
            let segSweep = maxSweep / float ringMaxHealthSegments
            let start0 = -90.0 // top, fixed screen-aligned start
            let paint = Paint.stroke color 3.0 |> Paint.withStrokeCap Round

            Scene.group
                [ for i in 0 .. lit - 1 ->
                      let a = start0 + segSweep * float i
                      Scene.arc bounds a (segSweep * 0.85) paint ]

    // Rim beads -> speed (0..4). Spread along the bottom rim; screen-aligned.
    let private ringSpeedBeads (t: Token) : Scene =
        let n = max 0 (min 4 t.Speed)

        if n = 0 || t.R <= 0.0 then
            Scene.empty
        else
            let color = factionColor t.Faction
            let rr = t.R * 0.82

            Scene.group
                [ for i in 0 .. n - 1 ->
                      let ang = Math.PI / 2.0 + (float i - float (n - 1) / 2.0) * 0.5
                      let p = { X = t.Cx + cos ang * rr; Y = t.Cy + sin ang * rr }
                      Scene.circle p (max 1.0 (t.R * 0.08)) color ]

    // Inner glyph -> class. Reuses the Badge per-class corner profile at a smaller radius so Klass reads
    // distinctly inside the ring (screen-aligned).
    let private ringClassGlyph (t: Token) : Scene =
        let paint = Paint.stroke (factionColor t.Faction) 1.5 |> Paint.withStrokeJoin RoundJoin
        Scene.path (polyPath (badgeFramePoints t.Klass t.Cx t.Cy (t.R * 0.32))) paint

    // Heading needle from centre -> heading (FR-006). Only the needle turns; the ring stays screen-aligned.
    let private ringHeadingNeedle (t: Token) : Scene =
        if t.R <= 0.0 then
            Scene.empty
        else
            let inner = t.R * 0.15
            let outer = t.R * 0.95
            let p1 = { X = t.Cx + sin t.Heading * inner; Y = t.Cy - cos t.Heading * inner }
            let p2 = { X = t.Cx + sin t.Heading * outer; Y = t.Cy - cos t.Heading * outer }
            Scene.line p1 p2 (Paint.stroke (factionColor t.Faction) 2.0 |> Paint.withStrokeCap Round)

    let private drawRing (t: Token) : Scene =
        if t.R <= 0.0 then
            placeholder t
        else
            let bounds = { X = t.Cx - t.R; Y = t.Cy - t.R; Width = t.R * 2.0; Height = t.R * 2.0 }

            withLabel
                (ringLabelNodes t)
                [ chargeFill t
                  Scene.ellipse bounds (strokePaint t) // outer ring: hue=faction, width=threat, dash=state
                  ringClassGlyph t
                  sigilScene { t with Heading = 0.0 } // screen-aligned centre identity
                  ringSpeedBeads t
                  ringHealthArc t
                  shieldMount t
                  ringHeadingNeedle t ]

    let ring (token: Token) : Scene = drawRing token

    let render (grammar: Grammar) (token: Token) : Scene =
        match grammar with
        | Grammar.Token -> drawSymbol token
        | Grammar.Badge -> badge token
        | Grammar.Ring -> ring token

    let galleryIn (grammar: Grammar) (cols: int) (spacing: float) (tokens: Token list) : Scene =
        match grammar with
        | Grammar.Token -> gallery cols spacing tokens // byte-identical to the existing gallery (FR-010)
        | g ->
            let cols = max 1 cols

            tokens
            |> List.mapi (fun i tk ->
                let row = i / cols
                let col = i % cols
                let cx = spacing * (float col + 0.5)
                let cy = spacing * (float row + 0.5)
                render g { tk with Cx = cx; Cy = cy })
            |> Scene.group

    // Grammar-agnostic motion overlay (FR-014): centre/radius rhythms that read identically on any grammar
    // base (Pulse/Blink/Damage), reproducing the Token `animate` overlay geometry. Directional rhythms
    // (Idle/Spin/Moving) have no grammar-agnostic form -> None -> the static base symbol is drawn.
    let private agnosticOverlay (t: Token) (motion: Motion) (ph: float) : Scene option =
        if t.R <= 0.0 then
            None
        else
            let c = factionColor t.Faction

            match motion with
            | Pulse ->
                let rr = t.R * (1.1 + 0.6 * ph)
                let alpha = byte (180.0 * (1.0 - ph))

                Some(
                    Scene.ellipse
                        { X = t.Cx - rr; Y = t.Cy - rr; Width = rr * 2.0; Height = rr * 2.0 }
                        (Paint.stroke (Colors.rgba c.Red c.Green c.Blue alpha) 2.0)
                )
            | Blink ->
                if ph < 0.5 then
                    let p = { X = t.Cx - t.R * 0.85; Y = t.Cy - t.R * 0.85 }
                    Some(Scene.circle p (max 2.0 (t.R * 0.2)) (Colors.rgb 245uy 34uy 45uy))
                else
                    None
            | Damage ->
                let rr = t.R * (1.0 + 0.15 * sin (ph * 2.0 * Math.PI))
                let wash = Colors.rgba 245uy 34uy 45uy 90uy
                Some(Scene.ellipse { X = t.Cx - rr; Y = t.Cy - rr; Width = rr * 2.0; Height = rr * 2.0 } (Paint.stroke wash 3.0))
            | Idle
            | Spin
            | Moving -> None

    let animateIn (grammar: Grammar) (motion: Motion) (token: Token) (phase: float) : Scene =
        match grammar with
        | Grammar.Token -> animate motion token phase // byte-identical to the existing animate (FR-010)
        | g ->
            let baseSymbol = render g token
            let ph = phase - floor phase

            match agnosticOverlay token motion ph with
            | Some overlay -> Scene.group [ baseSymbol; overlay ]
            | None -> baseSymbol

    let filmstripIn (grammar: Grammar) (samples: int) (entries: (Motion * Token) list) : Scene =
        match grammar with
        | Grammar.Token -> filmstrip samples entries // byte-identical to the existing filmstrip (FR-010)
        | g ->
            let samples = max 1 samples
            let maxR = entries |> List.fold (fun acc (_, tk) -> max acc tk.R) 1.0
            let spacing = maxR * 2.6

            entries
            |> List.mapi (fun row (m, tk) ->
                [ for s in 0 .. samples - 1 ->
                      let phase = if samples = 1 then 0.0 else float s / float (samples - 1)
                      let cx = spacing * (float s + 0.5)
                      let cy = spacing * (float row + 0.5)
                      animateIn g m { tk with Cx = cx; Cy = cy } phase ])
            |> List.concat
            |> Scene.group
