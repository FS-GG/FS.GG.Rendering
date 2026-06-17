module Feature140LegacyCacheTextOverlayTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls

let private content = [ Scene.textAt { X = 0.0; Y = 12.0 } "cached text" Colors.white ]

[<Tests>]
let tests =
    testList
        "Feature140 cache text overlay compatibility"
        [ test "legacy cache lowering wraps content in a transparent CachedSubtree boundary" {
              let chain = Composition.LegacyCachedSubtree 99UL |> Composition.legacyLower |> Composition.normalize
              let lowered = Composition.applyChain chain content

              match lowered with
              | [ { Nodes = [ CachedSubtree boundary ] } ] ->
                  Expect.equal boundary.CacheId 99UL "cache id is preserved"
                  Expect.contains (Scene.describe boundary.Scene) TextElement "wrapped scene remains transparent to describe"
              | other -> failtestf "expected one cached subtree, got %A" other
          }

          test "legacy text stays on the content layer while glyph-run proof is opt-in" {
              let textChain = Composition.LegacyText |> Composition.legacyLower |> Composition.normalize
              let scene = Composition.applyChain textChain content |> Scene.group

              Expect.contains (Scene.describe scene) TextElement "legacy text remains a text node"
              Expect.isFalse (Scene.describe scene |> List.contains GlyphRunElement) "glyph-run proof is not introduced unless explicitly authored"
          }

          test "legacy overlay lowers to layer routing evidence without changing text output" {
              let overlayChain = Composition.LegacyOverlay |> Composition.legacyLower |> Composition.normalize
              let scene = Composition.applyChain overlayChain content |> Scene.group

              Expect.contains (overlayChain.NormalizedEffects |> List.map _.Effect) (Composition.LayerHint "overlay") "overlay lowering records layer intent"
              Expect.contains (Scene.describe scene) TextElement "overlay scene content remains visible text"
          } ]
