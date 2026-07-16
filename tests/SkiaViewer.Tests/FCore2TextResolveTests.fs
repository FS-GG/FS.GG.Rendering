module FCore2TextResolveTests

// F-CORE-2 (2026-07-15 review): the draw path used to resolve the whole string twice per frame — once
// inside `buildShapedGlyphRunData`/`shapeText` and again in `drawText` purely to gather fallback events.
// The fix routes the draw path through `buildShapedGlyphRunDataResolved`, which resolves once and hands
// the resolution back for reuse. These guards pin that the reused resolution is byte-for-byte the same
// disclosure a standalone `resolveText` would produce (so the dedup can never silently drift), and that
// the returned glyph run matches the non-deduped builder. The projection compares exactly what the
// renderer's fallback-event loop reads (`Original`/`Rendered`/`Resolution`), not the cached `SKFont`.

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let private font: FontSpec = { Family = None; Size = 16.0; Weight = None }

/// 一 — no bundled coverage in any face, so it always resolves to a disclosed tofu box. `—`/`▸` are
/// deliberate substitutions. Mixing them guarantees the resolution carries non-`Authored` entries, so a
/// green assertion cannot be vacuously green over an all-authored string.
let private mixedText = sprintf "Ada %s Stable —▸" (Char.ConvertFromUtf32 0x4E00)

let private disclosure (rc: Fonts.ResolvedChar) = rc.Original, rc.Rendered, rc.Resolution

[<Tests>]
let tests =
    testList "F-CORE-2 text resolution reuse" [
        test "buildShapedGlyphRunDataResolved reuses the same resolution resolveText produces" {
            Text.installShapingProvider () |> ignore

            let shaped, resolved = Fonts.buildShapedGlyphRunDataResolved mixedText font
            let standalone = Fonts.resolveText font mixedText

            // Non-vacuity floor: the resolution must actually exercise non-authored disclosure, else this
            // proves nothing about the fallback-event path the dedup replaced.
            Expect.equal shaped.Provider.Availability ProviderInstalled "installed path under test"
            Expect.isTrue
                (resolved |> List.exists (fun rc ->
                    match rc.Resolution with
                    | Fonts.FallbackResolution.Authored _ -> false
                    | _ -> true))
                "fixture carries substituted/tofu disclosure (guard is non-vacuous)"

            Expect.equal
                (resolved |> List.map disclosure)
                (standalone |> List.map disclosure)
                "reused resolution equals a standalone resolveText, so fallback events are unchanged"
        }

        test "the resolved builder returns the same glyph run as the non-deduped builder" {
            Text.installShapingProvider () |> ignore

            let deduped, _ = Fonts.buildShapedGlyphRunDataResolved mixedText font
            let plain = Fonts.buildShapedGlyphRunData mixedText font

            Expect.equal deduped plain "dedup does not change the drawable glyph run"
        }

        test "the fallback path returns an empty resolution and does not resolve" {
            Text.clearShapingProvider () |> ignore

            let shaped, resolved = Fonts.buildShapedGlyphRunDataResolved mixedText font

            Expect.notEqual shaped.Provider.Availability ProviderInstalled "provider is cleared"
            Expect.isEmpty resolved "no resolution is computed on the non-installed fallback path"

            Text.installShapingProvider () |> ignore
        }
    ]
