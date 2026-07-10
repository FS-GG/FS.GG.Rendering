module Symbology.Tests.ApiSurfaceMirrorTests

// Issue #276 — a generated product does not carry `src/`. It reads the symbology signatures from its own
// `docs/api-surface/`, which `template/base/` mirrors from `src/` by hand: `// See skill: …` + the source
// file, byte for byte. Nothing gated that copy, and it went stale twice — losing the `Finding.Units`
// past-capacity semantics (76705f3), then the `ChannelKind` doctrine that made `Size`/`Threat`/`Charge`
// Ordered (#285). Both times the product shipped signature comments that contradicted the behaviour, and
// the symbology skill's Public Contract now sends an agent to exactly those files to learn the `Report`
// shape. So: pin the mirror.

open System.IO
open Expecto
open FS.GG.TestSupport

/// `docs/api-surface/<mirror>` <- `<source>`. One entry per public signature the pure/render packages ship.
let private mirrorPairs =
    [ "Symbology/Symbology.fsi", "src/Symbology/Symbology.fsi"
      "Symbology/Legibility.fsi", "src/Symbology/Legibility.fsi"
      "Symbology.Render/Render.fsi", "src/Symbology.Render/Render.fsi" ]

let private mirrorHeader = "// See skill: fs-gg-symbology"

[<Tests>]
let apiSurfaceMirrorTests =
    testList
        "Issue276 shipped api-surface mirror"
        [ test "every Symbology mirror is its header plus src, byte for byte" {
              for mirror, source in mirrorPairs do
                  let mirrorPath =
                      Path.Combine(RepositoryRoot.value, "template", "base", "docs", "api-surface", mirror)

                  let sourcePath = Path.Combine(RepositoryRoot.value, source)

                  // Line-wise, so the assertion does not depend on the checkout's line endings.
                  let expected = Array.append [| mirrorHeader |] (File.ReadAllLines sourcePath)

                  Expect.equal
                      (File.ReadAllLines mirrorPath)
                      expected
                      $"{mirror} has drifted from {source} — re-mirror it as '{mirrorHeader}' + the source"
          }

          // The mirror is what a product reads, so a signature the skill names must actually be there.
          test "the mirror ships all three public modules" {
              for mirror, _ in mirrorPairs do
                  let mirrorPath =
                      Path.Combine(RepositoryRoot.value, "template", "base", "docs", "api-surface", mirror)

                  Expect.isTrue (File.Exists mirrorPath) $"{mirror} is named by the skill but is not shipped"
          } ]
