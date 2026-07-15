namespace FS.GG.UI.Symbology

open System
open FS.GG.UI.Scene

type LabelRun =
    { Text: string
      Color: Color option
      Weight: int option
      Scale: float option
      Italic: bool option
      Underline: bool option
      Strike: bool option
      Tracking: float option }

type LabelAlign =
    | Leading
    | Center
    | Trailing
    | Justify

type LabelParagraph =
    { Runs: LabelRun list
      Align: LabelAlign }

[<RequireQualifiedAccess>]
type LabelText =
    | Plain of string
    | Rich of LabelRun list
    | Laid of LabelParagraph list

/// Feature 200 — opt-in label-bound motion kind (FR-005). RQA so `LabelMotion.Pulse` never collides
/// with `Motion.Pulse`.
[<RequireQualifiedAccess>]
type LabelMotion =
    | TypeOn
    | Fade
    | Pulse
    | Scroll

/// The pure identity-label / rich-text LAYOUT engine, extracted out of `Symbology` (F-CORE-1). It owns
/// the whole text-to-glyph path — weight-aware fit, whitespace word-wrap, inline styled runs, laid-out
/// paragraphs, and the per-phase label-motion transforms — over the label text types above and the
/// `FS.GG.UI.Scene` vocabulary only. It never sees a `Token`: the grammar code in `Symbology` threads
/// the Token-derived geometry (centre / baseline / region / base size / budget) in and this module
/// returns the fitted `Scene` nodes, so the drawn goldens are byte-identical to the pre-extraction path.
module internal LabelLayout =

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

    let lineHeightOf (baseSize: float) : float = lineHeightOfW None baseSize

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
    // Widened for feature 199: the new slant / decoration / tracking attributes must also be at their
    // no-op default (unset / false / 0.0) for the all-default join-to-`Plain` and the single-`Center`
    // -paragraph reduction to stay byte-clean (FR-004/SC-003).
    let private isDefaultRun (r: LabelRun) =
        r.Color = None
        && r.Weight = None
        && (r.Scale = None || r.Scale = Some 1.0)
        && (r.Italic = None || r.Italic = Some false)
        && (r.Underline = None || r.Underline = Some false)
        && (r.Strike = None || r.Strike = Some false)
        && (r.Tracking = None || r.Tracking = Some 0.0)

    // The plain-equivalent of an all-default run list: concatenate the run texts (each run keeps its own
    // interior spacing). `Rich [ run "HMR-7" ]` ⇒ "HMR-7"; `Rich []`/all-empty ⇒ "" ⇒ no label (FR-007).
    let private joinRuns (runs: LabelRun list) =
        runs |> List.map (fun r -> r.Text) |> String.concat ""

    // A run resolved to its drawable style at a grammar base size: colour defaults to `labelInk`, size is
    // `base * scale` (floored at 1.0), weight passes straight through (FR-003 / research.md R4).
    type private RunStyle =
        { Color: Color
          Weight: int option
          Size: float
          Italic: bool
          Underline: bool
          Strike: bool
          Tracking: float } // letter-spacing as an em-fraction of `Size` (feature 199, FR-003)

    let private resolveStyle (baseSize: float) (r: LabelRun) : RunStyle =
        { Color = r.Color |> Option.defaultValue labelInk
          Weight = r.Weight
          Size = max 1.0 (baseSize * (r.Scale |> Option.defaultValue 1.0))
          Italic = r.Italic |> Option.defaultValue false
          Underline = r.Underline |> Option.defaultValue false
          Strike = r.Strike |> Option.defaultValue false
          Tracking = r.Tracking |> Option.defaultValue 0.0 }

    // Synthetic-slant shear factor (≈12°) — a design-loop constant (data-model §8). The matrix is a
    // baseline-pivoted horizontal shear so glyphs lean while the baseline stays fixed (FR-003/FR-018).
    let private slantFactor = 0.21

    // Per-run tracked width: the plain measured width PLUS letter-spacing between glyphs (em-fraction of
    // the size). Folded into break / fit / placement so tracking never pushes the block past the region
    // (feature 199, FR-007). `Tracking = 0` ⇒ exactly the plain measured width (zero drift).
    let private trackedWidth (style: RunStyle) (text: string) : float =
        let baseW = labelWidthW style.Weight text style.Size
        baseW + style.Tracking * style.Size * float (max 0 (text.Length - 1))

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
                        let ww = trackedWidth style text // tracking-aware (feature 199, FR-007)

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

    // Fit a styled segment to the region (tracking-deflated) → (drawn text, fitted font, style, tracked
    // drawn width). The fit target is deflated by the tracking overhead so the tracked draw still lands
    // ≤ region (feature 199, FR-007). `Tracking = 0` ⇒ fit against the full region with the plain measured
    // width (zero drift). Returns None for empty/whitespace (drops the segment).
    let private fitSegment (regionWidth: float) (style: RunStyle) (text: string) : (string * FontSpec * RunStyle * float) option =
        let trackingPad = style.Tracking * style.Size * float (max 0 (text.Length - 1))
        let regionForFit = if style.Tracking > 0.0 then max 1.0 (regionWidth - trackingPad) else regionWidth

        match fitLabelW style.Weight regionForFit style.Size text with
        | None -> None
        | Some(ftext, font) ->
            let drawnWidth =
                if style.Tracking <> 0.0 && ftext.Length > 0 then
                    (Scene.measureTextResolved ftext font).Width + style.Tracking * font.Size * float (ftext.Length - 1)
                else
                    (Scene.measureTextResolved ftext font).Width

            Some(ftext, font, style, drawnWidth)

    // Emit one fitted segment at (x, y) (feature 199): real glyphs — tracked ⇒ one `glyphRunProof` per glyph
    // advanced by `charWidth + trackPx`, else a single node; optionally baseline-sheared for italic; with
    // underline / strike rules spanning the drawn extent only. An all-default style hits NONE of the new
    // branches and emits the EXACT spec-198 single node (zero drift). Tofu-free: every glyph is a real
    // `glyphRunProof`; slant wraps them (glyphs unchanged); decoration is a non-text `line` (FR-006/FR-008).
    let private emitFitted (x: float) (y: float) (ftext: string) (font: FontSpec) (style: RunStyle) (drawnWidth: float) : Scene list =
        let paint = Paint.fill style.Color

        let glyphNodes =
            if style.Tracking <> 0.0 && ftext.Length > 0 then
                let trackPx = style.Tracking * font.Size

                ftext
                |> Seq.fold
                    (fun (acc, cx) ch ->
                        let s = string ch
                        let cw = (Scene.measureTextResolved s font).Width
                        (Scene.glyphRunProof { X = cx; Y = y } s font paint :: acc, cx + cw + trackPx))
                    ([], x)
                |> fst
                |> List.rev
            else
                [ Scene.glyphRunProof { X = x; Y = y } ftext font paint ]

        let glyphScene =
            if style.Italic then
                let shear =
                    { M11 = 1.0
                      M12 = slantFactor
                      M13 = -slantFactor * y
                      M21 = 0.0
                      M22 = 1.0
                      M23 = 0.0
                      M31 = 0.0
                      M32 = 0.0
                      M33 = 1.0 }

                [ Scene.withPerspective shear (Scene.group glyphNodes) ]
            else
                glyphNodes

        let rule (offY: float) =
            let thick = max 0.5 (font.Size * 0.07)
            Scene.line { X = x; Y = y + offY } { X = x + drawnWidth; Y = y + offY } (Paint.stroke style.Color thick)

        let decoration =
            [ if style.Underline then
                  yield rule (font.Size * 0.12)
              if style.Strike then
                  yield rule (-font.Size * 0.30) ]

        glyphScene @ decoration

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

            // Fit each segment in its own weight+size+tracking (≤ region, never clipped mid-glyph).
            let segs =
                segmentsOf line
                |> List.choose (fun (text, st) -> fitSegment regionWidth st text)

            let total = (segs |> List.sumBy (fun (_, _, _, w) -> w)) + spaceW * float (max 0 (List.length segs - 1))
            let startX = centerX - total / 2.0

            // Place left-to-right from the centred start; emit each segment at the shared baseline.
            ((startX, []), segs)
            ||> List.fold (fun (x, acc) (ftext, font, st, w) -> (x + w + spaceW, acc @ emitFitted x y ftext font st w))
            |> snd)
        |> List.concat

    // ---- Paragraph layout (feature 199, FR-001/FR-002/FR-007) ---------------------------------------
    // One drawn line of a laid-out label: its words (each a styled token), the paragraph alignment it was
    // authored with, and whether it is the LAST line of its paragraph (justify leaves that line + any
    // single-token line un-justified, FR-008).
    type private LaidLine =
        { Words: (string * RunStyle) list
          Align: LabelAlign
          IsParaLast: bool }

    // Place one drawn line's fitted words at baseline `y`, honouring the paragraph alignment within the
    // region span [left, left+regionWidth]. Leading/Center/Trailing position the block; Justify (unless
    // suppressed — last line / single-token line) distributes the slack evenly across inter-word gaps so the
    // line fills the width (FR-007/FR-008). Emits each word via `emitFitted` (slant/decoration/tracking).
    let private placeLine
        (alignment: LabelAlign)
        (suppressJustify: bool)
        (left: float)
        (regionWidth: float)
        (spaceW: float)
        (y: float)
        (words: (string * FontSpec * RunStyle * float) list)
        : Scene list =
        let n = List.length words
        let sumW = words |> List.sumBy (fun (_, _, _, w) -> w)
        let gaps = max 0 (n - 1)

        let emitFrom (startX: float) (gap: float) =
            ((startX, []), words)
            ||> List.fold (fun (x, acc) (ftext, font, st, w) -> (x + w + gap, acc @ emitFitted x y ftext font st w))
            |> snd

        match alignment with
        | Justify when not suppressJustify && gaps >= 1 ->
            emitFrom left ((regionWidth - sumW) / float gaps) // distribute slack: the last word lands on the right edge
        | _ ->
            let total = sumW + spaceW * float gaps

            let startX =
                match alignment with
                | Leading -> left
                | Trailing -> left + (regionWidth - total)
                | Center -> left + (regionWidth - total) / 2.0
                | Justify -> left // fallback (last line / single token) ⇒ leading

            emitFrom startX spaceW

    // Laid-out (multi-paragraph) layout (FR-001/FR-002/FR-007): break each paragraph into lines (reusing the
    // 197/198 tracking-aware break), flatten into the shared per-grammar line budget (ellipsis the last kept
    // line), then place each line by its paragraph alignment. The first drawn line keeps the spec-197
    // first-line baseline; lines stack downward by the per-line max run height (common baseline). Returns []
    // when no paragraph yields a drawable line (FR-009).
    let private laidLabelNodes
        (centerX: float)
        (baselineY: float)
        (regionWidth: float)
        (baseSize: float)
        (budget: int)
        (paras: LabelParagraph list)
        : Scene list =
        let spaceW = labelWidth " " baseSize
        let left = centerX - regionWidth / 2.0

        // Per-paragraph break → flat list of laid lines (empty lines already dropped by `breakLines`).
        let laidLines =
            paras
            |> List.collect (fun p ->
                let lines = atomsOf baseSize p.Runs |> breakLines regionWidth baseSize
                let n = List.length lines
                lines |> List.mapi (fun i line -> { Words = line; Align = p.Align; IsParaLast = i = n - 1 }))

        // Cap to the shared per-grammar budget; ellipsis the last word of the last kept line (FR-007).
        let budget = max 1 budget

        let capped =
            if List.length laidLines <= budget then
                laidLines
            else
                let kept = laidLines |> List.truncate (budget - 1)

                let lastKept =
                    let ll = laidLines |> List.item (budget - 1)

                    let words' =
                        match List.rev ll.Words with
                        | (t, st) :: restRev -> List.rev ((t + ellipsis, st) :: restRev)
                        | [] -> []

                    { ll with Words = words' }

                kept @ [ lastKept ]

        // Per-line height = tallest run; baseline offsets are cumulative prefix sums (common baseline).
        let heights =
            capped
            |> List.map (fun ll -> ll.Words |> List.map (fun (_, st) -> lineHeightOfW st.Weight st.Size) |> List.fold max 0.0)

        let offsets = heights |> List.scan (+) 0.0
        let lastIndex = List.length capped - 1

        capped
        |> List.mapi (fun i ll ->
            let y = baselineY + List.item i offsets
            let suppress = ll.IsParaLast || i = lastIndex // ellipsised / final drawn line ⇒ never justified
            let fitted = ll.Words |> List.choose (fun (t, st) -> fitSegment regionWidth st t)
            placeLine ll.Align suppress left regionWidth spaceW y fitted)
        |> List.concat

    // Per-grammar label dispatch (research.md R6): the structural zero-drift router. `None` and the
    // plain / all-default cases delegate to the VERBATIM `labelNodes` (byte-identical to spec 197); only a
    // `Rich` label with a non-default run takes `richLabelNodes`. `lineHeight`/`budget` mirror spec 197.
    let labelDispatch
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
        | Some(LabelText.Laid paras) ->
            // Drop empty/whitespace paragraphs (FR-009); a single `Center` all-default paragraph reduces to
            // the Rich/Plain flow VERBATIM (byte-identical to spec 198, B4); everything else (any non-default
            // alignment, >1 paragraph, or a styled run) takes the real `laidLabelNodes` layout.
            let nonEmpty =
                paras
                |> List.filter (fun p -> p.Runs |> List.exists (fun r -> not (String.IsNullOrWhiteSpace r.Text)))

            match nonEmpty with
            | [] -> []
            | [ { Runs = runs; Align = Center } ] when List.forall isDefaultRun runs ->
                labelNodes centerX baselineY regionWidth baseSize lineHeight budget (Some(joinRuns runs))
            | _ -> laidLabelNodes centerX baselineY regionWidth baseSize budget nonEmpty

    // ---- Label-bound motion (feature 200, FR-005/FR-006/FR-007) -------------------------------------
    // The motion is a pure per-phase transform of the ALREADY-RESOLVED, ALREADY-FITTED label nodes. At
    // `restPhase` every kind is the identity transform, so a motion-bound label at rest is byte-identical
    // to the static spec-199 label (FR-007); `LabelMotion = None` skips the transform entirely (FR-008).
    let restPhase = 0.0

    // Rebuild a label sub-scene, mapping every GlyphRun via `glyph` (return `None` to drop it) and every
    // leaf paint via `paint`. Recurses through exactly the wrapper nodes the label emitter can produce
    // (Group / italic PerspectiveNode / Translate / Clip). Glyphs stay real `glyphRunProof` nodes ⇒
    // tofu-freeness is preserved across phases (FR-010).
    let rec private rebuildLabel (glyph: GlyphRun -> SceneNode option) (paint: Paint -> Paint) (s: Scene) : Scene =
        { Nodes = s.Nodes |> List.choose (rebuildLabelNode glyph paint) }

    and private rebuildLabelNode (glyph: GlyphRun -> SceneNode option) (paint: Paint -> Paint) (node: SceneNode) : SceneNode option =
        match node with
        | Group scenes -> Some(Group(scenes |> List.map (rebuildLabel glyph paint)))
        | PerspectiveNode(tf, sc) -> Some(PerspectiveNode(tf, rebuildLabel glyph paint sc))
        | Translate(o, sc) -> Some(Translate(o, rebuildLabel glyph paint sc))
        | ClipNode(c, sc) -> Some(ClipNode(c, rebuildLabel glyph paint sc))
        | GlyphRun g -> glyph g
        | Line(a, b, p) -> Some(Line(a, b, paint p))
        | other -> Some other

    // Apply the bound `LabelMotion` to the static label node list as a pure function of the normalised
    // phase `ph` (FR-006). Each kind's rest value (`ph = restPhase`) is the identity transform, so the
    // rest frame returns the static nodes VERBATIM (FR-007); non-rest frames stay fitted within the region
    // (FR-011): TypeOn reveals a measured whole-glyph prefix (never mid-glyph); Fade ramps paint alpha
    // (geometry unchanged); Pulse scales by a factor capped to ≤ 1 about the region centre (never grows
    // past the region); Scroll offsets the line and CLIPS to the region extent (no overflow into adjacent
    // channels). Reuses existing primitives only — no new scene primitive (FR-019).
    let motionLabelNodes
        (kind: LabelMotion)
        (ph: float)
        (centerX: float)
        (baselineY: float)
        (regionWidth: float)
        (staticNodes: unit -> Scene list)
        : Scene list =
        if ph = restPhase then
            staticNodes () // identity at rest ⇒ byte-identical to the static spec-199 label (FR-007)
        else
            let nodes = staticNodes ()

            if List.isEmpty nodes then
                [] // a motion-bound label resolving to no glyphs draws nothing, every phase (FR-012)
            else

            match kind with
            | LabelMotion.Fade ->
                let fade = Paint.withOpacity ph
                nodes
                |> List.map (rebuildLabel (fun g -> Some(GlyphRun { g with Paint = fade g.Paint })) fade)
            | LabelMotion.TypeOn ->
                // Reveal a whole-glyph PREFIX sized by `ph` (char boundary ⇒ never mid-glyph). The prefix is
                // re-emitted as a real `glyphRunProof` (tofu-free); `k = 0` drops the run for this frame.
                let reveal (g: GlyphRun) : SceneNode option =
                    let text = g.Data.Text
                    let k = min text.Length (max 0 (int (floor (ph * float text.Length + 1e-9))))

                    if k <= 0 then None
                    elif k >= text.Length then Some(GlyphRun g)
                    else (Scene.glyphRunProof g.Position (text.Substring(0, k)) g.Data.Font g.Paint).Nodes |> List.tryHead

                nodes |> List.map (rebuildLabel reveal id)
            | LabelMotion.Pulse ->
                // Size oscillation about the region centre; factor in [1-k, 1] ⇒ never larger than the
                // already-fitted label ⇒ always within the region (FR-011). Rest (ph=0) ⇒ factor 1 (identity).
                let f = 1.0 - 0.18 * (0.5 - 0.5 * cos (ph * 2.0 * Math.PI))

                let m =
                    { M11 = f
                      M12 = 0.0
                      M13 = centerX * (1.0 - f)
                      M21 = 0.0
                      M22 = f
                      M23 = baselineY * (1.0 - f)
                      M31 = 0.0
                      M32 = 0.0
                      M33 = 1.0 }

                [ Scene.withPerspective m (Scene.group nodes) ]
            | LabelMotion.Scroll ->
                // Overflow ticker: translate the line by an X offset and CLIP to the region span so nothing
                // ever draws outside [centerX ± regionWidth/2] (no overflow into adjacent channels — FR-011).
                let region =
                    { X = centerX - regionWidth / 2.0
                      Y = baselineY - regionWidth
                      Width = regionWidth
                      Height = regionWidth * 2.0 }

                let offset = regionWidth * 0.5 * sin (ph * 2.0 * Math.PI)
                [ Scene.clipped (RectClip region) (Scene.translate offset 0.0 (Scene.group nodes)) ]
