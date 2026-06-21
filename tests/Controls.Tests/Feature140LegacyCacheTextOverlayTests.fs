module Feature140LegacyCacheTextOverlayTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls

let private content = [ Scene.textAt { X = 0.0; Y = 12.0 } "cached text" Colors.white ]

// Feature 184 (US2): the retired `Composition.legacyLower` produced these literal modifier entries.
// The cache/text/overlay node forms still route through the modern modifier IR byte-identically, so the
// tests construct the entries directly (same Source/Effect) and assert the unchanged `applyChain` output.
let private entry source effect : Composition.ModifierEntry list =
    [ { Composition.Source = source; Composition.Effect = effect } ]

[<Tests>]
let tests =
    testList
        "Feature140 cache text overlay compatibility"
        [ test "cache node form wraps content in a transparent CachedSubtree boundary" {
              let chain =
                  entry Composition.LegacyCacheSource (Composition.CacheBoundary 99UL)
                  |> Composition.normalize

              let lowered = Composition.applyChain chain content

              match lowered with
              | [ { Nodes = [ CachedSubtree boundary ] } ] ->
                  Expect.equal boundary.CacheId 99UL "cache id is preserved"
                  Expect.contains (Scene.describe boundary.Scene) TextElement "wrapped scene remains transparent to describe"
              | other -> failtestf "expected one cached subtree, got %A" other
          }

          test "text node form stays on the content layer while glyph-run proof is opt-in" {
              let textChain =
                  entry Composition.LegacyTextSource (Composition.LayerHint "content")
                  |> Composition.normalize

              let scene = Composition.applyChain textChain content |> Scene.group

              Expect.contains (Scene.describe scene) TextElement "text remains a text node"
              Expect.isFalse (Scene.describe scene |> List.contains GlyphRunElement) "glyph-run proof is not introduced unless explicitly authored"
          }

          test "overlay node form routes layer evidence without changing text output" {
              let overlayChain =
                  entry Composition.LegacyOverlaySource (Composition.LayerHint "overlay")
                  |> Composition.normalize

              let scene = Composition.applyChain overlayChain content |> Scene.group

              Expect.contains (overlayChain.NormalizedEffects |> List.map _.Effect) (Composition.LayerHint "overlay") "overlay routing records layer intent"
              Expect.contains (Scene.describe scene) TextElement "overlay scene content remains visible text"
          } ]
