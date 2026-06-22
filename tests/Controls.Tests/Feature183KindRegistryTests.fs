module ControlsFeature183KindRegistryTests

// Feature 183 (US1 / SC-001) — the restored "exhaustiveness" guard for the control `Kind` registry.
//
// The single internal `ControlKindRegistry` must have exactly one entry per catalog kind: a kind in
// the catalog but missing from the registry (or vice-versa) fails this test, so adding a control kind
// becomes a single-site, compiler/test-checked change instead of silent drift across the old ~13
// parallel dispatch sites. Reaches the internal registry via `InternalsVisibleTo Controls.Tests`.

open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default

[<Tests>]
let tests =
    testList "Feature183KindRegistry" [
        test "registry keys equal the catalog kind set, both directions (SC-001)" {
            let catalogKinds =
                Catalog.supportedControls |> List.map (fun d -> d.Id) |> Set.ofList

            let registryKeys =
                ControlKindRegistry.registry |> Map.toList |> List.map fst |> Set.ofList

            let missingFromRegistry = Set.difference catalogKinds registryKeys |> Set.toList
            let extraInRegistry = Set.difference registryKeys catalogKinds |> Set.toList

            Expect.isEmpty missingFromRegistry "catalog kinds with no registry entry"
            Expect.isEmpty extraInRegistry "registry keys that are not catalog kinds"
            Expect.equal registryKeys catalogKinds "registry keys must equal the catalog kind set"
        }

        // Feature 189 (US3 / FR-007 / SC-007 / C4) — the painter-completeness oracle. Every catalog
        // kind must resolve EXPLICIT faithful geometry in `ContentRender.faithfulContent`; a kind that
        // falls through to the unknown-kind fallback (`emptyState theme box <kind>`, caption = the kind
        // string) is a missing painter and fails loudly here. The empty-DATA fallback uses the distinct
        // caption "(no data)", so a handled-but-dataless chart is correctly NOT flagged. Reaches the
        // internal `ContentRender`/`ChartGeometry` via `InternalsVisibleTo Controls.Tests`.
        test "every catalog kind resolves an explicit painter (no unknown-kind fallback) (SC-007)" {
            let theme = Theme.light
            let box: FS.GG.UI.Scene.Rect = { X = 0.0; Y = 0.0; Width = 200.0; Height = 120.0 }

            // `faithfulContent` is the rich-family dispatch — `renderNode` only invokes it when
            // `ControlKindRegistry.isRich kind`; non-rich kinds render via the box+label path and
            // legitimately have no painter arm. So the completeness invariant is: every RICH catalog
            // kind resolves an explicit painter.
            let unpaintedKinds =
                Catalog.supportedControls
                |> List.map (fun d -> d.Id)
                |> List.filter ControlKindRegistry.isRich
                |> List.filter (fun kind ->
                    let control = Control.create kind []
                    // The unknown-kind fallback is exactly `emptyState theme box <kind>`; if the kind is
                    // explicitly painted, faithfulContent returns something else (real geometry, or the
                    // distinct "(no data)" empty state for a dataless chart).
                    ContentRender.faithfulContent theme box control = ChartGeometry.emptyState theme box kind)

            Expect.isEmpty unpaintedKinds "rich catalog kinds that fall through to the unknown-kind fallback (no explicit painter)"
        }
    ]
