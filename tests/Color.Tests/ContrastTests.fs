module ContrastTests

(* Feature 083 / US1 (SC-002) + US3 (SC-004) — failing-first (Principle I/VI) tests for the
   WCAG 2.x contrast surface in FS.GG.UI.Color. Each case calls the real
   Contrast.relativeLuminance / ratio / verdict / check / checkPaint functions over literal
   Scene colors and asserts the TYPED result, never a string/IO scrape. Before Contrast.fs
   existed these did not compile. *)

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Color

let private tol = 0.01

[<Tests>]
let referencePairTests =
    testList "Feature 083 WCAG reference pairs (SC-002)" [

        test "black-on-white ratio is 21:1 within 0.01" {
            let measured = Contrast.ratio Colors.black Colors.white
            Expect.floatClose Accuracy.medium measured 21.0 "black/white is the WCAG 21:1 maximum"
            Expect.isLessThan (abs (measured - 21.0)) tol "within SC-002 tolerance 0.01"
        }

        test "white-on-white ratio is 1:1" {
            let measured = Contrast.ratio Colors.white Colors.white
            Expect.isLessThan (abs (measured - 1.0)) tol "identical colors are the 1:1 minimum"
        }

        test "ratio is symmetric (order-independent)" {
            let a = Colors.rgba 37uy 99uy 235uy 255uy
            let b = Colors.white
            Expect.equal (Contrast.ratio a b) (Contrast.ratio b a) "lighter/darker pick is order-independent"
        }

        test "relativeLuminance is 1.0 for white and 0.0 for black" {
            Expect.isLessThan (abs (Contrast.relativeLuminance Colors.white - 1.0)) tol "white luminance is 1.0"
            Expect.isLessThan (abs (Contrast.relativeLuminance Colors.black - 0.0)) tol "black luminance is 0.0"
        }

        test "alpha foreground composites over the background before measuring" {
            // A 50% black over white must measure as a mid-grey, not as transparent.
            let translucent = Colors.rgba 0uy 0uy 0uy 128uy
            let composited = Contrast.compositeOver Colors.white translucent
            Expect.equal composited.Alpha 255uy "compositing yields an opaque color"
            let measured = Contrast.ratio composited Colors.white
            Expect.isGreaterThan measured 1.0 "a translucent black darkens white (ratio above 1:1)"
            Expect.isLessThan measured 21.0 "but not the full black-on-white 21:1"
        }
    ]

[<Tests>]
let verdictTests =
    testList "Feature 083 per-role verdict thresholds (SC-004, FR-003/FR-004a)" [

        test "Text role: AAA at >=7, AA at >=4.5, AA-Large at >=3, Fail below" {
            Expect.equal (Contrast.verdict Text 7.0) Aaa "Text >= 7:1 is AAA"
            Expect.equal (Contrast.verdict Text 4.5) Aa "Text >= 4.5:1 is AA"
            Expect.equal (Contrast.verdict Text 3.0) AaLarge "Text >= 3:1 (large) is AA-Large"
            Expect.equal (Contrast.verdict Text 2.99) Fail "Text below 3:1 fails"
        }

        test "GraphicOrUi role: AA at >=3, Fail below (binary 3:1)" {
            Expect.equal (Contrast.verdict GraphicOrUi 3.0) Aa "GraphicOrUi >= 3:1 is AA"
            Expect.equal (Contrast.verdict GraphicOrUi 2.99) Fail "GraphicOrUi below 3:1 fails"
            Expect.equal (Contrast.verdict GraphicOrUi 21.0) Aa "GraphicOrUi never reports AAA"
        }

        test "Decorative role: Exempt for any ratio" {
            Expect.equal (Contrast.verdict Decorative 1.0) Exempt "Decorative is exempt at 1:1"
            Expect.equal (Contrast.verdict Decorative 21.0) Exempt "Decorative is exempt at 21:1"
        }

        test "check returns ratio + role + verdict in one call (SC-004)" {
            let result = Contrast.check Text Colors.black Colors.white
            Expect.isLessThan (abs (result.Ratio - 21.0)) tol "white-on-black measures 21:1"
            Expect.equal result.Role Text "the role is echoed"
            Expect.equal result.Verdict Aaa "21:1 text is AAA"
        }

        test "checkPaint on a non-solid (gradient) paint is Indeterminate with nan ratio (FR-004a)" {
            let gradient =
                { Paint.fill Colors.white with
                    Shader = Some(LinearGradient({ X = 0.0; Y = 0.0 }, { X = 1.0; Y = 1.0 }, [ Colors.black; Colors.white ])) }
            let result = Contrast.checkPaint Text Colors.white gradient
            Expect.equal result.Verdict Indeterminate "a gradient fill is neither pass nor fail"
            Expect.isTrue (System.Double.IsNaN result.Ratio) "Ratio is the nan not-applicable sentinel"
        }

        test "checkPaint on a solid paint measures a ratio with no render pass (FR-001a)" {
            let solid = Paint.fill Colors.black
            let result = Contrast.checkPaint Text Colors.white solid
            Expect.equal result.Verdict Aaa "solid black on white is AAA"
            Expect.isLessThan (abs (result.Ratio - 21.0)) tol "solid fill measures the declared color"
        }
    ]
