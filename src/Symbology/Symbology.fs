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

/// Feature 200 — auto-label channel selectors (each reads ONLY the named Token channel, FR-002).
type AutoField =
    | FactionCode
    | KlassCode
    | StateCode
    | HealthTier
    | ThreatTier
    | SpeedPips
    | ShieldFlag

/// Feature 200 — an opt-in auto-label projection request (FR-001).
type AutoLabelSpec =
    { Fields: AutoField list
      Separator: string }

type Token =
    { Cx: float
      Cy: float
      R: float
      Heading: float
      SecondaryHeading: float option
      Faction: Faction
      Klass: Klass
      Sigil: Sigil
      State: TokenState
      Threat: float
      Charge: float
      Speed: int
      Health: float
      Shield: bool
      Label: LabelText option
      AutoLabel: AutoLabelSpec option
      LabelMotion: LabelMotion option }

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

    // Secondary heading -> a centre-out barrel with a tip mark (feature 254, FR-003). Angle 0 points
    // north, matching `Heading` (the Token nose at (0,-1), the Badge pip, the Ring needle). The barrel
    // is sited so it cannot be misread as the primary indicator: it starts at the centre and its tip
    // mark sits at a radius no primary indicator occupies. Callers pass the per-grammar geometry.
    //
    // Never called with `SecondaryHeading = None` — absence contributes NO node at all. Note that
    // `Scene.empty` is itself a node (`describe` yields `EmptyElement` for it), so the usual
    // "return Scene.empty when off" shape would have drifted every existing golden; the caller omits
    // these nodes instead. Returned as BARE SIBLINGS, never wrapped in a group, for the same reason
    // `withLabel` appends bare line nodes: a wrapper would add a third node to the tree.
    //
    // The barrel always starts clear of the centre identity sigil (which every grammar draws out to
    // 0.42R): a line struck through the sigil muddies the identity channel exactly where it is read.
    // Only `outer` varies per grammar, and it is measured from the centre in units of R.
    let private secondaryHeadingInner = 0.15

    let private secondaryHeadingTipRadius (t: Token) = max 1.0 (t.R * 0.1)

    /// The farthest any secondary-heading indicator reaches, in units of `R`. `filmstrip` reads this
    /// to size its cells, so the two cannot drift apart.
    let private secondaryHeadingMaxExtent = 1.32 + 0.1

    let private secondaryHeadingIndicator (outer: float) (width: float) (t: Token) (angle: float) : Scene list =
        let color = factionColor t.Faction
        let at radius = { X = t.Cx + sin angle * t.R * radius; Y = t.Cy - cos angle * t.R * radius }
        let tip = at outer

        [ Scene.line (at secondaryHeadingInner) tip (Paint.stroke color width |> Paint.withStrokeCap Round)
          Scene.circle tip (secondaryHeadingTipRadius t) color ]

    let defaultToken: Token =
        { Cx = 0.0
          Cy = 0.0
          R = 1.0
          Heading = 0.0
          SecondaryHeading = None
          Faction = Neutral
          Klass = Mobile
          Sigil = Ring
          State = Confirmed
          Threat = 0.5
          Charge = 0.5
          Speed = 0
          Health = 0.5
          Shield = false
          Label = None
          AutoLabel = None
          LabelMotion = None }

    // ---- Rich-text label constructors (feature 198) ----
    let plainLabel (text: string) : LabelText = LabelText.Plain text

    let run (text: string) : LabelRun =
        { Text = text
          Color = None
          Weight = None
          Scale = None
          Italic = None
          Underline = None
          Strike = None
          Tracking = None }

    let richLabel (runs: LabelRun list) : LabelText = LabelText.Rich runs

    // ---- Laid-out label constructors (feature 199) ----
    let paragraph (runs: LabelRun list) : LabelParagraph = { Runs = runs; Align = Center }

    let align (alignment: LabelAlign) (runs: LabelRun list) : LabelParagraph = { Runs = runs; Align = alignment }

    let laidLabel (paragraphs: LabelParagraph list) : LabelText = LabelText.Laid paragraphs

    // ---- Auto-label / label-motion constructors (feature 200) ----
    let autoLabel (fields: AutoField list) : AutoLabelSpec = { Fields = fields; Separator = " " }

    let autoLabelSep (separator: string) (fields: AutoField list) : AutoLabelSpec =
        { Fields = fields; Separator = separator }

    let labelMotion (kind: LabelMotion) : LabelMotion = kind

    // ---- Auto-label projection (feature 200, FR-001/FR-002/FR-004) ----------------------------------
    // Render one AutoField as its fixed, game-agnostic, compact code, reading ONLY the named Token channel
    // (never a game's raw stats — FR-002). `ShieldFlag` with `Shield = false` contributes NOTHING (None);
    // every other selector always renders a code. Pure: no wall-clock / randomness / IO (FR-015).
    let private renderAutoField (t: Token) (field: AutoField) : string option =
        match field with
        | FactionCode ->
            Some(
                match t.Faction with
                | Ally -> "ALY"
                | Enemy -> "ENY"
                | Neutral -> "NEU"
                | Custom _ -> "CUS"
            )
        | KlassCode ->
            Some(
                match t.Klass with
                | Mobile -> "MOB"
                | Heavy -> "HVY"
                | Scout -> "SCT"
            )
        | StateCode ->
            Some(
                match t.State with
                | Confirmed -> "CFM"
                | Suspected -> "SUS"
            )
        | HealthTier -> Some(sprintf "H%d" (int (round (clamp01 t.Health * 100.0))))
        | ThreatTier -> Some(sprintf "T%d" (min 4 (int (clamp01 t.Threat * 5.0)))) // [0,1] -> T0..T4
        | SpeedPips -> Some(sprintf "S%d" (max 0 (min 4 t.Speed)))
        | ShieldFlag -> if t.Shield then Some "SHD" else None

    // Project a styled label from the Token's OWN channels (FR-002): a pure fold over `spec.Fields`, each
    // arm reading one channel; the rendered codes join with `spec.Separator`. Empty `Fields`, or a joined
    // text that is empty/all-whitespace, yields `None` — no label, exactly like an empty hand-authored
    // label (FR-004/FR-012). The result rides the existing `LabelText.Plain` path (zero new vocabulary).
    let private projectAutoLabel (t: Token) (spec: AutoLabelSpec) : LabelText option =
        let joined =
            spec.Fields
            |> List.choose (renderAutoField t)
            |> String.concat spec.Separator

        if String.IsNullOrWhiteSpace joined then
            None
        else
            Some(LabelText.Plain joined)

    // Resolution order (FR-003): an explicit `Label` ALWAYS wins; else the projected `AutoLabel`; else
    // none. Exactly one resolved label or none — never two stacked. A Token opting into neither reaches
    // `labelDispatch` with `resolveLabel t = t.Label`, hitting the EXACT spec-199 path (zero drift, FR-008).
    let private resolveLabel (t: Token) : LabelText option =
        t.Label |> Option.orElseWith (fun () -> t.AutoLabel |> Option.bind (projectAutoLabel t))

    // Assembly-internal so `Legibility.scoreIn` scores the very text the grammar draws, rather than a
    // second copy of the resolution order that would drift from this one.
    let internal resolvedLabel (token: Token) : LabelText option = resolveLabel token

    // The label-motion / static-symbol rest phase now lives with the extracted layout engine; alias it
    // locally so the grammar code below reads unchanged (single source — `LabelLayout.restPhase`).
    let private restPhase = LabelLayout.restPhase

    // Per-grammar label region (provisional geometry — the contract is FR-004: sited, observable,
    // non-overlapping; coordinates + per-grammar line budgets are a design-loop detail, see data-model.md).
    // Each sits in the one uncrowded zone of its grammar, screen-aligned (never rotates with Heading); the
    // FIRST line keeps spec 196's exact baseline / region width / base size (the zero-drift anchor).
    // Route the resolved label through the existing fit/wrap/cap dispatch, then — only when motion is bound
    // AND the phase is off-rest — apply the per-phase transform. `LabelMotion = None` or `labelPhase = rest`
    // calls `labelDispatch` DIRECTLY (zero drift — FR-008); the rest-phase identity gives FR-007.
    let private labelNodesAt
        (centerX: float)
        (baselineY: float)
        (regionWidth: float)
        (baseSize: float)
        (lineHeight: float)
        (budget: int)
        (t: Token)
        (labelPhase: float)
        : Scene list =
        let staticNodes () =
            LabelLayout.labelDispatch centerX baselineY regionWidth baseSize lineHeight budget (resolveLabel t)

        match t.LabelMotion with
        | Some kind when labelPhase <> restPhase ->
            LabelLayout.motionLabelNodes kind labelPhase centerX baselineY regionWidth staticNodes
        | _ -> staticNodes ()

    // The ONE source of the per-grammar line budget: the emitters below pass it through
    // `LabelLayout.labelDispatch` (which caps each label at it), and `Legibility.scoreIn` reads it to warn
    // before the surplus is silently dropped. Two copies would drift, and the linter's whole job here is
    // to be right about this number.
    let internal labelLineBudget (grammar: Grammar) : int =
        match grammar with
        | Grammar.Token -> 3 // caption strip below the health arc
        | Grammar.Badge -> 2 // band below the health bar / pips
        | Grammar.Ring -> 2 // caption beneath the sigil, inner disc

    let private tokenLabelNodes (t: Token) (labelPhase: float) : Scene list =
        let baseSize = t.R * 0.5
        let budget = labelLineBudget Grammar.Token
        labelNodesAt t.Cx (t.Cy + t.R * 1.5) (t.R * 1.9) baseSize (LabelLayout.lineHeightOf baseSize) budget t labelPhase

    let private badgeLabelNodes (t: Token) (labelPhase: float) : Scene list =
        let baseSize = t.R * 0.42
        let budget = labelLineBudget Grammar.Badge
        labelNodesAt t.Cx (t.Cy + t.R * 1.42) (t.R * 1.7) baseSize (LabelLayout.lineHeightOf baseSize) budget t labelPhase

    let private ringLabelNodes (t: Token) (labelPhase: float) : Scene list =
        let baseSize = t.R * 0.34
        let budget = labelLineBudget Grammar.Ring
        labelNodesAt t.Cx (t.Cy + t.R * 0.52) (t.R * 1.05) baseSize (LabelLayout.lineHeightOf baseSize) budget t labelPhase

    // Append the label line nodes to a grammar's child list as bare siblings (research.md R5): `[]` ⇒
    // `Scene.group nodes` (byte-identical to no-label), `[one]` ⇒ `nodes @ [one]` (byte-identical to the
    // spec-196 single-line label). Never wraps the lines in an extra group — that would drift the goldens.
    let private withLabel (lineNodes: Scene list) (nodes: Scene list) : Scene =
        Scene.group (nodes @ lineNodes)

    // The placeholder guard (`R <= 0`) stays BEFORE label resolution/animation so it always wins (FR-014).
    let private drawSymbolAt (labelPhase: float) (t: Token) : Scene =
        if t.R <= 0.0 then
            placeholder t // placeholder rule wins over the label (FR-007); no label on a degenerate token
        else
            withLabel
                (tokenLabelNodes t labelPhase)
                [ yield chargeFill t
                  yield Scene.path (bodyPath t) (strokePaint t)
                  yield sigilScene t
                  yield tailBeads t
                  yield healthArc t
                  yield shieldMount t
                  // Here the primary heading IS the rotated silhouette, so the barrel only has to clear
                  // the hull (1.0R) and the belly arc (1.18R) to read as a separate channel.
                  match t.SecondaryHeading with
                  | Some angle -> yield! secondaryHeadingIndicator 1.32 2.5 t angle
                  | None -> () ]

    let private drawSymbol (t: Token) : Scene = drawSymbolAt restPhase t

    let token (token: Token) : Scene = drawSymbol token

    let animate (motion: Motion) (token: Token) (phase: float) : Scene =
        let t = token
        let ph = phase - floor phase
        let baseSymbol = drawSymbolAt ph t // the label animates at `ph` alongside the existing overlay (FR-005)

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

    // 2.6R per cell is the pre-feature spacing and must stay EXACTLY that when nothing draws a barrel,
    // or every filmstrip golden drifts. But each cell then owns only 1.3R, and a barrel reaches
    // `secondaryHeadingMaxExtent` (1.42R) — so a filmstrip of turreted units would overrun its
    // neighbour. Widen to fit the barrel, and only when one is present.
    let private filmstripSpacing (entries: (Motion * Token) list) (maxR: float) : float =
        let anyBarrel = entries |> List.exists (fun (_, tk) -> Option.isSome tk.SecondaryHeading)
        maxR * (if anyBarrel then 2.0 * secondaryHeadingMaxExtent else 2.6)

    let filmstrip (samples: int) (entries: (Motion * Token) list) : Scene =
        let samples = max 1 samples
        let maxR = entries |> List.fold (fun acc (_, tk) -> max acc tk.R) 1.0
        let spacing = filmstripSpacing entries maxR

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

    let private drawBadgeAt (labelPhase: float) (t: Token) : Scene =
        if t.R <= 0.0 then
            placeholder t // placeholder rule wins over the label (FR-007)
        else
            withLabel
                (badgeLabelNodes t labelPhase)
                [ yield chargeFill t
                  yield Scene.path (polyPath (badgeFramePoints t.Klass t.Cx t.Cy t.R)) (strokePaint t)
                  yield sigilScene { t with Heading = 0.0 } // screen-aligned centre identity (heading is the edge pip)
                  yield badgeSpeedPips t
                  yield badgeHealthBar t
                  yield shieldMount t
                  yield badgeHeadingPip t
                  // Stops WELL inside the frame. The rim pip carrying the primary heading sits at 1.0R
                  // with radius 0.12R, so a barrel reaching 0.86R would have its 0.10R tip mark merge
                  // into the pip whenever the two headings agree — and "turret forward" is the common
                  // rest state. Ending at 0.70R leaves a visible gap in exactly that case.
                  match t.SecondaryHeading with
                  | Some angle -> yield! secondaryHeadingIndicator 0.7 2.0 t angle
                  | None -> () ]

    let private drawBadge (t: Token) : Scene = drawBadgeAt restPhase t

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

    let private drawRingAt (labelPhase: float) (t: Token) : Scene =
        if t.R <= 0.0 then
            placeholder t
        else
            let bounds = { X = t.Cx - t.R; Y = t.Cy - t.R; Width = t.R * 2.0; Height = t.R * 2.0 }

            withLabel
                (ringLabelNodes t labelPhase)
                [ yield chargeFill t
                  yield Scene.ellipse bounds (strokePaint t) // outer ring: hue=faction, width=threat, dash=state
                  yield ringClassGlyph t
                  yield sigilScene { t with Heading = 0.0 } // screen-aligned centre identity
                  yield ringSpeedBeads t
                  yield ringHealthArc t
                  yield shieldMount t
                  yield ringHeadingNeedle t
                  // The primary needle stops inside the ring (0.95R); the barrel pushes its tip mark
                  // outside it, so the two are told apart by extent even when they point the same way.
                  match t.SecondaryHeading with
                  | Some angle -> yield! secondaryHeadingIndicator 1.3 1.5 t angle
                  | None -> () ]

    let private drawRing (t: Token) : Scene = drawRingAt restPhase t

    let ring (token: Token) : Scene = drawRing token

    let render (grammar: Grammar) (token: Token) : Scene =
        match grammar with
        | Grammar.Token -> drawSymbol token
        | Grammar.Badge -> badge token
        | Grammar.Ring -> ring token

    // Render in the selected grammar with the label animated at `labelPhase` (FR-005). `render` is the
    // rest-phase specialisation (`renderAt restPhase`).
    let private renderAt (labelPhase: float) (grammar: Grammar) (t: Token) : Scene =
        match grammar with
        | Grammar.Token -> drawSymbolAt labelPhase t
        | Grammar.Badge -> drawBadgeAt labelPhase t
        | Grammar.Ring -> drawRingAt labelPhase t

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
            let ph = phase - floor phase
            let baseSymbol = renderAt ph g token // the label animates at `ph` on Badge/Ring too (FR-005)

            match agnosticOverlay token motion ph with
            | Some overlay -> Scene.group [ baseSymbol; overlay ]
            | None -> baseSymbol

    let filmstripIn (grammar: Grammar) (samples: int) (entries: (Motion * Token) list) : Scene =
        match grammar with
        | Grammar.Token -> filmstrip samples entries // byte-identical to the existing filmstrip (FR-010)
        | g ->
            let samples = max 1 samples
            let maxR = entries |> List.fold (fun acc (_, tk) -> max acc tk.R) 1.0
            let spacing = filmstripSpacing entries maxR

            entries
            |> List.mapi (fun row (m, tk) ->
                [ for s in 0 .. samples - 1 ->
                      let phase = if samples = 1 then 0.0 else float s / float (samples - 1)
                      let cx = spacing * (float s + 0.5)
                      let cy = spacing * (float row + 0.5)
                      animateIn g m { tk with Cx = cx; Cy = cy } phase ])
            |> List.concat
            |> Scene.group
