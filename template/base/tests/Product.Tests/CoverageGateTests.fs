module AppRootCoverageGateTests

//#if (profile == "game")
// Two independent witnesses fail closed:
//   1. AppRoot.GameplayVisualInventory is the runtime-owned subject set and element-bound registry.
//   2. element-visuals.catalog records each subject's shown/hidden disposition.
// View.view consumes the same projection audited here. Representative states must yield non-empty,
// deterministic SceneCodec evidence before a handle counts as observed.

open System
open System.IO
open System.Security.Cryptography
open System.Text
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Symbology
open AppRoot

let private catalogPath =
    Path.Combine(__SOURCE_DIRECTORY__, "element-visuals.catalog")

let private sha256 (bytes: byte[]) =
    SHA256.HashData bytes |> Convert.ToHexString |> fun value -> value.ToLowerInvariant()

let private textBytes (value: string) =
    Encoding.UTF8.GetBytes value

let private declared =
    GameplayVisualInventory.all
    |> List.map GameplayVisualInventory.elementId

let private readCatalog () =
    Catalog.parse (File.ReadAllText catalogPath)

let private runtimeFrames =
    GameplayVisualInventory.representativeModels
    |> List.map (fun model ->
        let projected = GameplayVisualInventory.project model
        let actualView = View.view model
        let expectedView = projected |> List.map _.Scene |> Group
        let frame = { Nodes = [ actualView ] }
        model, projected, actualView, expectedView, (SceneCodec.export frame).CanonicalBytes)

let private bindingEvidence =
    GameplayVisualInventory.bindings
    |> List.collect (fun binding ->
        binding.RequiredStates
        |> List.map (fun (state, model) ->
            let scene = binding.Project model
            let bytes = (SceneCodec.export scene).CanonicalBytes
            binding.Element, binding.Handle, state, scene, bytes))

let private evidenceDigests (catalog: Catalog.Catalog) : Catalog.EvidenceDigests =
    { Inventory = declared |> String.concat "\n" |> (+) "\n" |> textBytes |> sha256
      Catalog = Catalog.render catalog |> textBytes |> sha256
      Render =
        bindingEvidence
        |> List.map (fun (element, handle, state, _, bytes) ->
            String.concat
                "|"
                [ GameplayVisualInventory.elementId element
                  handle
                  state
                  sha256 bytes ])
        |> String.concat "\n"
        |> textBytes
        |> sha256 }

let private observedBindings =
    bindingEvidence
    |> List.choose (fun (element, handle, _, scene, _) ->
        if List.isEmpty scene.Nodes then
            None
        else
            Some(GameplayVisualInventory.elementId element, handle))
    |> List.distinct

let private runtimeAudit (catalog: Catalog.Catalog) =
    Catalog.audit
        declared
        catalog
        GameplayVisualInventory.registeredBindings
        observedBindings
        (evidenceDigests catalog)

let private finding gap element (report: Catalog.BindingReport) =
    report.Findings
    |> List.exists (fun item -> item.Gap = gap && item.Element = element)

let private mkCatalog (entries: Catalog.Entry list) : Catalog.Catalog =
    { Entries = entries }

let private syntheticEvidence: Catalog.EvidenceDigests =
    { Inventory = "fixture-inventory"
      Catalog = "fixture-catalog"
      Render = "fixture-render" }

type private RogueVisualElement =
    | DoorOpen
    | DoorLocked
    | Trapdoor

type private RogueState =
    { Locked: bool
      Descending: bool }

let private rogueElements =
    [ DoorOpen; DoorLocked; Trapdoor ]

let private rogueId =
    function
    | DoorOpen -> "DoorOpen"
    | DoorLocked -> "DoorLocked"
    | Trapdoor -> "Trapdoor"

let private rogueHandle =
    function
    | DoorOpen -> "scene/door-open"
    | DoorLocked -> "scene/door-locked"
    | Trapdoor -> "scene/trapdoor"

let private rogueProjection state =
    let color red green blue =
        { Red = red; Green = green; Blue = blue; Alpha = 255uy }

    let element =
        if state.Descending then Trapdoor
        elif state.Locked then DoorLocked
        else DoorOpen

    let scene =
        match element with
        | DoorOpen -> { Nodes = [ Rectangle((8.0, 8.0, 8.0, 32.0), color 80uy 180uy 110uy) ] }
        | DoorLocked -> { Nodes = [ Rectangle((8.0, 8.0, 24.0, 32.0), color 210uy 90uy 70uy) ] }
        | Trapdoor -> { Nodes = [ Rectangle((8.0, 24.0, 32.0, 8.0), color 150uy 100uy 60uy) ] }

    element, rogueHandle element, scene

[<Tests>]
let coverageGateTests =
    testList "visual-coverage-gate" [
        test "the typed runtime inventory is exhaustive, readable, non-empty, and unique" {
            Expect.isNonEmpty declared "a game profile must declare its gameplay-visual inventory in runtime source"
            Expect.equal (List.distinct declared) declared "the runtime gameplay-visual inventory must not contain duplicates"
            Expect.equal GameplayVisualInventory.bindings.Length declared.Length "every reflected union case has an exhaustive binding"
        }

        test "representative states traverse View.view and yield non-empty runtime frames" {
            for _, projected, actual, expected, _ in runtimeFrames do
                Expect.equal actual expected "View.view must consume the audited projection without a parallel renderer"

                for item in projected do
                    Expect.isNonEmpty item.Scene.Nodes $"{GameplayVisualInventory.elementId item.Element} must produce real Scene nodes"

        }

        test "required states differ per element and distinct elements cannot share byte-identical evidence" {
            for binding in GameplayVisualInventory.bindings do
                Expect.isNonEmpty binding.RequiredStates $"{GameplayVisualInventory.elementId binding.Element} must declare evidence states"

                let labels = binding.RequiredStates |> List.map fst
                Expect.equal (List.distinct labels) labels "evidence-state labels must be unique per element"

                let digests =
                    binding.RequiredStates
                    |> List.map (fun (state, model) ->
                        let scene = binding.Project model
                        Expect.isNonEmpty scene.Nodes $"{GameplayVisualInventory.elementId binding.Element}/{state} must emit Scene nodes"
                        (SceneCodec.export scene).CanonicalBytes |> sha256)

                Expect.equal
                    (List.distinct digests).Length
                    digests.Length
                    $"{GameplayVisualInventory.elementId binding.Element} required states must be visually distinct"

            let elementBaselines =
                GameplayVisualInventory.bindings
                |> List.map (fun binding ->
                    let _, model = binding.RequiredStates.Head
                    let exported = binding.Project model |> SceneCodec.export
                    sha256 exported.CanonicalBytes)

            Expect.equal
                (List.distinct elementBaselines).Length
                elementBaselines.Length
                "distinct gameplay elements cannot use byte-identical baseline Scene evidence"
        }

        test "catalog, element-bound registry, and representative runtime rendering are complete" {
            match readCatalog () with
            | Error reason -> failtestf "element-visuals.catalog is malformed: %s" reason
            | Ok catalog ->
                let report = runtimeAudit catalog

                Expect.equal
                    report.Verdict
                    Catalog.BindingVerdict.Complete
                    (report.Findings |> List.map _.Message |> String.concat "\n")
        }

        test "Rogue2 old self-declared control stays green while typed runtime inventory names Door and Trapdoor omissions" {
            let staleStarter =
                mkCatalog
                    [ { Element = "Ball"; Visual = Catalog.Visual.Shown "scene/ball" }
                      { Element = "LeftPaddle"; Visual = Catalog.Visual.Shown "scene/paddle" } ]

            Expect.equal (Catalog.validate staleStarter).Verdict Coverage.Verdict.Covered "old control remains green"

            let report =
                Catalog.audit
                    (rogueElements |> List.map rogueId)
                    staleStarter
                    []
                    []
                    syntheticEvidence

            for element in [ "DoorOpen"; "DoorLocked"; "Trapdoor" ] do
                Expect.isTrue (finding Catalog.BindingGap.Missing element report) $"typed fixture names omitted {element}"

            for stale in [ "Ball"; "LeftPaddle" ] do
                Expect.isTrue (finding Catalog.BindingGap.Stale stale report) $"dungeon fixture rejects stale {stale}"
        }

        test "Rogue2 open, locked/sealed, and trapdoor states are element-bound, rendered, and visually distinct" {
            let catalog =
                rogueElements
                |> List.map (fun element ->
                    ({ Element = rogueId element
                       Visual = Catalog.Visual.Shown(rogueHandle element) }: Catalog.Entry))
                |> mkCatalog

            let registry =
                rogueElements |> List.map (fun element -> rogueId element, rogueHandle element)

            let projections =
                [ { Locked = false; Descending = false }
                  { Locked = true; Descending = false }
                  { Locked = false; Descending = true } ]
                |> List.map rogueProjection

            let observed =
                projections
                |> List.map (fun (element, handle, scene) ->
                    Expect.isNonEmpty scene.Nodes $"{rogueId element} emits Scene nodes"
                    rogueId element, handle)

            let sceneDigests =
                projections
                |> List.map (fun (_, _, scene) -> (SceneCodec.export scene).CanonicalBytes |> sha256)

            Expect.equal (List.distinct sceneDigests).Length 3 "open, locked/sealed, and trapdoor scenes differ"

            let complete =
                Catalog.audit (rogueElements |> List.map rogueId) catalog registry observed syntheticEvidence

            Expect.equal complete.Verdict Catalog.BindingVerdict.Complete "all typed Rogue2 states are bound and observed"

            let swapped =
                Catalog.audit
                    (rogueElements |> List.map rogueId)
                    catalog
                    [ "DoorOpen", "scene/door-locked"; "DoorLocked", "scene/door-open"; "Trapdoor", "scene/trapdoor" ]
                    observed
                    syntheticEvidence

            Expect.isTrue (finding Catalog.BindingGap.Unbound "DoorOpen" swapped) "swapped element/handle pairs cannot pass"
        }

    ]
//#endif
