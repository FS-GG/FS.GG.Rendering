module Feature093StylePropertyTests

// Feature 093 (E3) — SC-004 (T015): style resolution is pure, deterministic, and obeys the fixed
// precedence base < classes-in-order < state for EVERY generated (theme, base, classes, state)
// combination over ≥1000 inputs. The precedence is encoded structurally as "the state layer is
// outermost": applying the classes then the state equals re-resolving the class-folded style
// under that state with no classes — so a state's owned field always wins over any class.

open Expecto
open FsCheck
open FsCheck.FSharp
open FS.Skia.UI.Scene
open FS.Skia.UI.Controls

module private Gen093 =
    let private colors =
        [ Colors.rgb 10uy 20uy 30uy
          Colors.rgb 200uy 40uy 40uy
          Colors.rgb 40uy 160uy 90uy
          Colors.transparent
          Colors.black
          Colors.white ]

    let private genColor: Gen<Color> = Gen.elements colors

    let genResolved: Gen<ResolvedStyle> =
        gen {
            let! fg = genColor
            let! fill = genColor
            let! stroke = genColor
            let! sw = Gen.elements [ 0.0; 1.0; 2.0; 3.0 ]
            let! ff = Gen.elements [ None; Some "Inter"; Some "Roboto" ]
            let! fs = Gen.elements [ 12.0; 13.0; 14.0; 15.0 ]
            let! fw = Gen.elements [ None; Some 400; Some 700 ]
            return
                { Foreground = fg
                  Fill = fill
                  Stroke = stroke
                  StrokeWidth = sw
                  FontFamily = ff
                  FontSize = fs
                  FontWeight = fw }
        }

    let private genVariant: Gen<StyleVariant> =
        Gen.elements
            [ StyleVariant.Primary
              StyleVariant.Danger
              StyleVariant.Ghost
              StyleVariant.Neutral
              StyleVariant.Success
              StyleVariant.Warning ]

    let private genCustom: Gen<string> =
        Gen.elements [ "primary"; "danger"; "success"; "warning"; "ghost"; "subtle"; "muted"; "no-such"; "" ]

    let private genClass: Gen<StyleClass> =
        Gen.oneof [ Gen.map Variant genVariant; Gen.map Custom genCustom ]

    let genClasses: Gen<StyleClass list> =
        gen {
            let! n = Gen.choose (0, 4)
            return! Gen.listOfLength n genClass
        }

    let genState: Gen<VisualState> =
        Gen.elements
            [ Normal
              Disabled
              Hover
              Pressed
              Focused
              Selected
              Loading
              VisualState.Validation Valid
              VisualState.Validation(Invalid "e")
              VisualState.Validation(Pending "p") ]

    let genTheme: Gen<Theme> = Gen.elements [ Theme.light; Theme.dark ]

    // (theme, base, classes, state)
    let tuple: Gen<Theme * ResolvedStyle * StyleClass list * VisualState> =
        gen {
            let! t = genTheme
            let! b = genResolved
            let! cs = genClasses
            let! s = genState
            return (t, b, cs, s)
        }

[<Tests>]
let feature093StylePropertyTests =
    testList "Feature 093 resolver properties (FsCheck, SC-004)" [

        testCase "purity / determinism — identical inputs produce an identical ResolvedStyle (≥1000)" (fun () ->
            let deterministic (t, b, cs, s) = Style.resolve t b cs s = Style.resolve t b cs s
            let config = Config.QuickThrowOnFailure.WithMaxTest 1000
            Check.One(config, Prop.forAll (Arb.fromGen Gen093.tuple) deterministic))

        testCase "fixed precedence — the visual state is outermost (state > classes > base) (≥1000)" (fun () ->
            // resolve t b cs s == resolve t (class-folded under Normal) [] s, i.e. the state layer
            // always applies on top of the class-resolved style, so a state's owned field wins.
            let stateOutermost (t, b, cs, s) =
                let classFolded = Style.resolve t b cs Normal
                Style.resolve t b cs s = Style.resolve t classFolded [] s
            let config = Config.QuickThrowOnFailure.WithMaxTest 1000
            Check.One(config, Prop.forAll (Arb.fromGen Gen093.tuple) stateOutermost))

        testCase "base identity — resolve t b [] Normal = b for every generated base (≥1000)" (fun () ->
            let baseIdentity (t, b, _, _) = Style.resolve t b [] Normal = b
            let config = Config.QuickThrowOnFailure.WithMaxTest 1000
            Check.One(config, Prop.forAll (Arb.fromGen Gen093.tuple) baseIdentity))
    ]
