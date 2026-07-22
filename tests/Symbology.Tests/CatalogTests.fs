module Symbology.Tests.CatalogTests

// Issue #990 — the machine-readable element↔visual CATALOG format: the single source of truth the
// `fs-gg-symbol-design` loop authors/maintains and #989's `Coverage` consumes. These tests exercise the
// public `Catalog` surface — the deterministic (de)serialization, the round-trip, and the `Coverage`
// bridge — and double as the documented artifact + gate pattern a product (and #994's scaffold) copies.

open Expecto
open FS.GG.UI.Symbology

// ---------------------------------------------------------------------------------------------------
// A sample game's element↔visual catalog. The catalog is PRODUCT-DATA: a game's element ids and token
// handles are its own strings — the framework owns the format and the check, never the per-game list.
// This mirrors the FULL gameplay-element set the broadened design loop enumerates (units AND
// projectiles/explosions/doors/hazards/…), not just the unit roster.
// ---------------------------------------------------------------------------------------------------
let private sampleCatalog: Catalog.Catalog =
    { Entries =
        [ { Element = "Enemy"; Visual = Catalog.Shown "token/enemy" }
          { Element = "Door"; Visual = Catalog.Shown "token/door" }
          { Element = "Bomb"; Visual = Catalog.Shown "token/bomb" }
          { Element = "Explosion"; Visual = Catalog.Shown "token/explosion" }
          { Element = "Projectile"; Visual = Catalog.Shown "token/projectile" }
          { Element = "Hazard"; Visual = Catalog.Shown "token/hazard" }
          { Element = "StealthAmbusher"
            Visual = Catalog.Hidden "stealth: invisible to the player until it attacks (fog-of-war mechanic)" } ] }

[<Tests>]
let catalogTests =
    testList
        "Issue990 Catalog — element↔visual catalog format"
        [

          // ---- FORMAT: deterministic serialization + round-trip ------------------------------------
          test "render is deterministic and carries the versioned header" {
              let text = Catalog.render sampleCatalog
              Expect.stringContains text "# fs-gg element-visual catalog v1" "the versioned header is present"
              Expect.equal (Catalog.render sampleCatalog) text "an unchanged catalog re-renders byte-identically"
          }

          test "parse of render round-trips every well-formed catalog" {
              match Catalog.parse (Catalog.render sampleCatalog) with
              | Ok parsed -> Expect.equal parsed sampleCatalog "parse (render c) = Ok c"
              | Error e -> failtestf "expected a round-trip, got Error %s" e
          }

          test "the row form is element<TAB>disposition<TAB>payload, in declared order" {
              let lines = (Catalog.render sampleCatalog).TrimEnd('\n').Split('\n')
              Expect.equal lines.[1] "Enemy\tshown\ttoken/enemy" "a shown row names its token handle"

              Expect.equal
                  lines.[7]
                  "StealthAmbusher\thidden\tstealth: invisible to the player until it attacks (fog-of-war mechanic)"
                  "a hidden row carries its mechanic reason; order matches declaration"
          }

          // ---- PARSE: malformed artifacts are rejected ---------------------------------------------
          test "a missing/wrong header is rejected" {
              Expect.isError (Catalog.parse "Enemy\tshown\ttoken/enemy\n") "no header is an error"
          }

          test "a shown row with a blank token handle is a malformed shown-as-nothing row" {
              let text = "# fs-gg element-visual catalog v1\nEnemy\tshown\t\n"
              Expect.isError (Catalog.parse text) "shown-as-nothing must be rejected at parse"
          }

          test "an unknown disposition is rejected" {
              let text = "# fs-gg element-visual catalog v1\nEnemy\tmaybe\tx\n"
              Expect.isError (Catalog.parse text) "only 'shown'/'hidden' are legal dispositions"
          }

          test "a duplicate element id is rejected" {
              let text = "# fs-gg element-visual catalog v1\nEnemy\tshown\ta\nEnemy\tshown\tb\n"
              Expect.isError (Catalog.parse text) "an element may have at most one catalog row"
          }

          test "blank lines and # comments after the header are ignored" {
              let text = "# fs-gg element-visual catalog v1\n\n# a note\nEnemy\tshown\ttoken/enemy\n"

              match Catalog.parse text with
              | Ok c -> Expect.equal c.Entries [ { Element = "Enemy"; Visual = Catalog.Shown "token/enemy" } ] "one row parsed"
              | Error e -> failtestf "expected Ok, got %s" e
          }

          // ---- COVERAGE BRIDGE: the artifact IS what #989 checks ------------------------------------
          test "validate — a complete, well-formed catalog is Covered with the opt-out on the ledger" {
              let report = Catalog.validate sampleCatalog
              Expect.equal report.Verdict Coverage.Covered "every row is shown or reasoned-hidden"
              Expect.isEmpty report.Findings "a well-formed catalog has no gaps"

              Expect.equal
                  (report.OptedOut |> List.map fst)
                  [ "StealthAmbusher" ]
                  "the deliberately-hidden element is on the audit ledger"
          }

          test "coverage — a declared element the catalog forgot is a Missing gap (the silent omission)" {
              // The product adds `Pickup` to its declared element set but forgets to catalog it.
              let declared = Catalog.declaredElements sampleCatalog @ [ "Pickup" ]
              let report = Catalog.coverage declared sampleCatalog

              Expect.equal report.Verdict Coverage.HasGaps "a forgotten element fails coverage"
              Expect.equal report.Findings.Length 1 "exactly the forgotten element is reported"
              Expect.equal report.Findings.Head.Element "Pickup" "the finding names the un-catalogued element"
              Expect.equal report.Findings.Head.Gap Coverage.Missing "no row -> Missing"
          }

          test "validate — a blank-reason opt-out surfaces as Unreasoned (format parses, policy rejects)" {
              let blank: Catalog.Catalog =
                  { Entries = [ { Element = "Ghost"; Visual = Catalog.Hidden "  " } ] }

              let report = Catalog.validate blank
              Expect.equal report.Verdict Coverage.HasGaps "a blank opt-out is not a reasoned one"
              Expect.equal report.Findings.Head.Gap Coverage.Unreasoned "blank Hidden reason -> Unreasoned"
          }

          test "toRepresentation bridges shown->Shown and hidden->Hidden for Coverage" {
              match Catalog.toRepresentation (Catalog.Shown "token/x") with
              | Coverage.Shown _ -> ()
              | other -> failtestf "expected Coverage.Shown, got %A" other

              Expect.equal
                  (Catalog.toRepresentation (Catalog.Hidden "off-screen"))
                  (Coverage.Hidden "off-screen")
                  "a hidden reason passes straight through to Coverage.Hidden"
          }

          test "tryFind resolves a catalogued element and misses an un-catalogued one" {
              Expect.equal (Catalog.tryFind "Door" sampleCatalog) (Some(Catalog.Shown "token/door")) "found"
              Expect.equal (Catalog.tryFind "Nope" sampleCatalog) None "an absent element resolves to None"
          } ]
