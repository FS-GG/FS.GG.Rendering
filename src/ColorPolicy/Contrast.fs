namespace FS.GG.UI.Color

open FS.GG.UI.Scene

type Role =
    | Text
    | GraphicOrUi
    | Decorative

type Verdict =
    | Aaa
    | Aa
    | AaLarge
    | Fail
    | Exempt
    | Indeterminate

type ContrastResult =
    { Ratio: float
      Role: Role
      Verdict: Verdict }

module Contrast =

    // sRGB channel linearization (WCAG 2.x, FR-001): cs = c/255; cl = cs/12.92 if
    // cs <= 0.03928 else ((cs + 0.055) / 1.055) ** 2.4. Pure over the byte channel.
    let private linearize (channel: byte) =
        let cs = float channel / 255.0

        if cs <= 0.03928 then
            cs / 12.92
        else
            ((cs + 0.055) / 1.055) ** 2.4

    let relativeLuminance (color: Color) =
        0.2126 * linearize color.Red
        + 0.7152 * linearize color.Green
        + 0.0722 * linearize color.Blue

    let ratio (a: Color) (b: Color) =
        let la = relativeLuminance a
        let lb = relativeLuminance b
        let lighter = max la lb
        let darker = min la lb
        (lighter + 0.05) / (darker + 0.05)

    let compositeOver (background: Color) (foreground: Color) =
        if foreground.Alpha = 255uy then
            foreground
        else
            let alpha = float foreground.Alpha / 255.0

            let blend (src: byte) (dst: byte) =
                let value = float src * alpha + float dst * (1.0 - alpha)
                value |> round |> int |> max 0 |> min 255 |> byte

            { Red = blend foreground.Red background.Red
              Green = blend foreground.Green background.Green
              Blue = blend foreground.Blue background.Blue
              Alpha = 255uy }

    let verdict (role: Role) (ratio: float) =
        match role with
        | Decorative -> Exempt
        | GraphicOrUi -> if ratio >= 3.0 then Aa else Fail
        | Text ->
            if ratio >= 7.0 then Aaa
            elif ratio >= 4.5 then Aa
            elif ratio >= 3.0 then AaLarge
            else Fail

    let check (role: Role) (background: Color) (foreground: Color) =
        let resolved = compositeOver background foreground
        let measured = ratio resolved background

        { Ratio = measured
          Role = role
          Verdict = verdict role measured }

    let checkPaint (role: Role) (background: Color) (paint: Paint) =
        // A paint measures only when it resolves to a single solid fill color: a
        // SolidColor shader, or no shader with a concrete Fill. Gradient/shader fills
        // (and a paint with no resolvable color) are Indeterminate — neither pass nor
        // fail, with Ratio = nan, so the exclusion is visible (FR-004a).
        let solidFill =
            match paint.Shader with
            | Some(SolidColor color) -> Some color
            | Some _ -> None
            | None -> paint.Fill

        match solidFill with
        | Some color -> check role background color
        | None ->
            { Ratio = nan
              Role = role
              Verdict = Indeterminate }
