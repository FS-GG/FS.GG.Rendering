module ControlsFeature183KindRegistryTests

// Feature 183 (US1 / SC-001) — the restored "exhaustiveness" guard for the control `Kind` registry.
//
// The single internal `ControlKindRegistry` must have exactly one entry per catalog kind: a kind in
// the catalog but missing from the registry (or vice-versa) fails this test, so adding a control kind
// becomes a single-site, compiler/test-checked change instead of silent drift across the old ~13
// parallel dispatch sites. Reaches the internal registry via `InternalsVisibleTo Controls.Tests`.

open Expecto
open FS.GG.UI.Controls

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
    ]
