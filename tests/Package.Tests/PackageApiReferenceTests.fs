module PackageApiReferenceTests

open System
open System.IO
open Expecto

let rec findRepositoryRoot (directory: string) =
    if Directory.GetFiles(directory, "*.sln").Length > 0 || File.Exists(Path.Combine(directory, "build.fsx")) then
        directory
    else
        match Directory.GetParent directory |> Option.ofObj with
        | Some parent -> findRepositoryRoot parent.FullName
        | None -> failwithf "Could not locate repository root from %s" directory

let repositoryRoot = findRepositoryRoot AppContext.BaseDirectory

let repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

let referenceRoot =
    repositoryPath "specs/035-api-discovery-names/readiness/package/api-reference"

let archiveFeatureReadinessRoot =
    repositoryPath "specs/036-archive-readiness-api-docs/readiness"

let referencePath packageId =
    Path.Combine(referenceRoot, packageId + ".md")

let readReference packageId =
    let path = referencePath packageId
    Expect.isTrue (File.Exists path) $"{packageId} source-shaped package API reference exists at {path}"
    File.ReadAllText path

/// FR-004 (feature 107): the PACKAGE-AGNOSTIC doc-preservation signal. A generated reference
/// "carries XML summaries" iff its embedded curated-signature body preserved at least one `///`
/// summary line. This holds whether those summaries are still placeholder boilerplate (today's
/// Scene/Testing) or substantive (post documentation cleanup), so documenting any package's
/// surface never breaks the check — replacing feature 106's brittle dependence on the placeholder
/// boilerplate sentence. It FAILS (FR-005) when the generator drops `///` summaries entirely.
let preservesXmlSummaries (reference: string) =
    reference.Replace("\r\n", "\n").Split('\n')
    |> Array.exists (fun line -> line.TrimStart().StartsWith("///", StringComparison.Ordinal))

let requiredPackages =
    [ "FS.GG.UI.Scene", [ "src/Scene/Scene.fsi" ]
      "FS.GG.UI.SkiaViewer", [ "src/SkiaViewer/SkiaViewer.fsi" ]
      "FS.GG.UI.Elmish", [ "src/Elmish/Elmish.fsi" ]
      "FS.GG.UI.KeyboardInput", [ "src/KeyboardInput/KeyboardInput.fsi" ]
      "FS.GG.UI.Layout", [ "src/Layout/Layout.fsi"; "src/Layout/Types.fsi"; "src/Layout/Graph.fsi"; "src/Layout/GraphValidation.fsi" ]
      "FS.GG.UI.Controls", [ "src/Controls/Types.fsi"; "src/Controls/Control.fsi"; "src/Controls/Attributes.fsi"; "src/Controls/DataGrid.fsi"; "src/Controls/Charts.fsi"; "src/Controls/TextInput.fsi"; "src/Controls/RichText.fsi" ]
      "FS.GG.UI.Controls.Elmish", [ "src/Controls.Elmish/ControlsElmish.fsi" ]
      "FS.GG.UI.Testing", [ "src/Testing/Testing.fsi" ] ]

[<Tests>]
let packageApiReferenceTests =
    testList "Package API reference generation" [
        test "curated fsi package reference emits one source-shaped index per package" {
            let indexPath = Path.Combine(referenceRoot, "index.md")
            Expect.isTrue (File.Exists indexPath) $"package API reference index exists at {indexPath}"

            let index = File.ReadAllText indexPath

            requiredPackages
            |> List.iter (fun (packageId, sourcePaths) ->
                Expect.stringContains index packageId $"index lists {packageId}"
                Expect.isTrue (File.Exists(referencePath packageId)) $"{packageId} has a package-specific reference file"

                let reference = readReference packageId
                Expect.stringContains reference $"package-id: {packageId}" $"{packageId} reference records package id"
                Expect.stringContains reference "package-version:" $"{packageId} reference records package version"
                Expect.stringContains reference "symbol-count:" $"{packageId} reference records sampled symbol count"
                Expect.stringContains reference "omitted-symbol-reasons:" $"{packageId} reference records omitted symbol reasons"

                sourcePaths
                |> List.iter (fun sourcePath ->
                    Expect.stringContains reference sourcePath $"{packageId} reference cites curated input {sourcePath}"))
        }

        test "source-shaped reference preserves F# authoring names and parameter labels" {
            let scene = readReference "FS.GG.UI.Scene"
            let controls = readReference "FS.GG.UI.Controls"
            let viewer = readReference "FS.GG.UI.SkiaViewer"
            let keyboard = readReference "FS.GG.UI.KeyboardInput"

            [ "type Rect ="
              "Width: float"
              "Height: float"
              "LinearGradient of startPoint: Point * endPoint: Point * colors: Color list"
              "DropShadow of dx: float * dy: float * blur: float * color: Color"
              "TextRun"
              "SceneElementKind" ]
            |> List.iter (fun sample -> Expect.stringContains scene sample $"Scene reference preserves {sample}")

            [ "type Control<'msg>"
              "KnownControl.TextBlock"
              "StandardAttributeName.VisibleRange"
              "DataGrid.create"
              "LineChart.series"
              "TextBox.onChanged" ]
            |> List.iter (fun sample -> Expect.stringContains controls sample $"Controls reference preserves {sample}")

            [ "type ViewerOptions ="
              "InitialSize: Size"
              "ViewerWindowPosition"
              "Coordinates of x: int * y: int" ]
            |> List.iter (fun sample -> Expect.stringContains viewer sample $"SkiaViewer reference preserves {sample}")

            [ "KeyboardModel"
              "KeyboardEvent"
              "KeyDown"
              "KeyUp" ]
            |> List.iter (fun sample -> Expect.stringContains keyboard sample $"KeyboardInput reference preserves {sample}")
        }

        test "reference output carries XML summaries and unsupported symbol diagnostics" {
            let scene = readReference "FS.GG.UI.Scene"
            let controls = readReference "FS.GG.UI.Controls"
            let testing = readReference "FS.GG.UI.Testing"

            // FR-004 (feature 107): preservation is proven by a PACKAGE-AGNOSTIC signal — every
            // tracked package's reference preserved at least one `///` summary line — not by
            // asserting the placeholder boilerplate sentence is present in any particular package.
            // So the deferred non-Controls documentation cleanup (which removes that boilerplate
            // from Scene/Testing) cannot re-break this check the way feature 106 had to
            // special-case Controls.
            requiredPackages
            |> List.iter (fun (packageId, _) ->
                Expect.isTrue
                    (preservesXmlSummaries (readReference packageId))
                    $"{packageId} reference preserves at least one /// summary (FR-004)")

            // The substantive-summary state is equally accepted: Controls (feature 106) carries
            // member-specific summaries (cross-referencing the typed `Props` front door) and NO
            // placeholder boilerplate, and still satisfies the same package-agnostic signal (SC-002).
            Expect.stringContains controls "typed `Props`" "Controls reference preserves substantive XML summaries (typed Props cross-reference)"
            Expect.isTrue
                (preservesXmlSummaries controls
                 && not (controls.Contains "Public contract function exposed by this FS.GG.UI package."))
                "a fully-documented package (no placeholder boilerplate) still satisfies the preservation signal (SC-002)"

            [ scene; controls; testing ]
            |> List.iter (fun reference ->
                Expect.stringContains reference "omitted-symbol-reasons:" "omitted symbols are reported even when the list is empty"
                Expect.stringContains reference "unsupported-symbols:" "unsupported signatures are diagnostic instead of silently dropped")
        }

        // FR-004 / SC-002: documenting a package (removing the placeholder boilerplate, leaving
        // substantive summaries) keeps the package-agnostic check green — the failure mode feature
        // 106 hit is gone. Driven by a simulated post-cleanup reference so the guarantee is proven
        // without waiting for the deferred non-Controls documentation pass to land.
        test "FR-004 a placeholder-free reference still satisfies the preservation signal" {
            let documented =
                "package-id: FS.GG.UI.Scene\nxml-summary-count: 2\n## Curated Signatures\n```fsharp\n"
                + "/// The drawing surface a scene paints onto.\ntype Surface = { Width: int }\n"
                + "/// Begins a new frame on the surface.\nval beginFrame: surface: Surface -> unit\n```\n"
            Expect.isTrue
                (preservesXmlSummaries documented)
                "documenting a package (placeholder removed) keeps the preservation check green"
            Expect.isFalse
                (documented.Contains "Public contract type exposed by this FS.GG.UI package.")
                "the documented reference carries no placeholder boilerplate"
        }

        // FR-005 / SC-002: the guarantee is RETAINED — if reference generation drops `///`
        // summaries, the package-agnostic check still fails. Only the brittle placeholder-sentence
        // sample was replaced, not the guarantee itself.
        test "FR-005 the preservation check still fails when /// summaries are dropped" {
            let dropped =
                "package-id: FS.GG.UI.Scene\nxml-summary-count: 0\n## Curated Signatures\n```fsharp\n"
                + "type Surface = { Width: int }\nval beginFrame: surface: Surface -> unit\n```\n"
            Expect.isFalse
                (preservesXmlSummaries dropped)
                "a reference whose body carries no /// summaries fails preservation (guarantee retained)"
        }

        test "reference generation declares no reflection or repository-source authoring fallback" {
            let index = File.ReadAllText(Path.Combine(referenceRoot, "index.md"))

            [ "generated-from: curated-fsi"
              "assembly-reflection: false"
              "repository-source-authoring-fallback: false" ]
            |> List.iter (fun required ->
                Expect.stringContains index required $"reference index records {required}")
        }

        test "archive API reference decision covers required packages dimensions and authority" {
            let evaluationPath = Path.Combine(archiveFeatureReadinessRoot, "api-reference-generator-evaluation.md")
            Expect.isTrue (File.Exists evaluationPath) $"API reference generator evaluation exists at {evaluationPath}"

            let evaluation = File.ReadAllText evaluationPath

            [ "FS.GG.UI.Scene"
              "FS.GG.UI.Controls"
              "FS.GG.UI.SkiaViewer"
              "F# authoring spelling fidelity"
              "record-field and union-case visibility"
              "parameter names and labels"
              "XML documentation preservation"
              "package-adjacent discoverability"
              "Markdown or HTML output suitability"
              "dependency and build impact"
              "generated product guidance compatibility"
              "mixed Scene/Controls qualification guidance"
              "clean package-consumer discovery without repository source inspection or reflection as the authoring strategy"
              "current-fsi"
              "authoritative"
              "fsdocs"
              "secondary" ]
            |> List.iter (fun required -> Expect.stringContains evaluation required $"evaluation records {required}")
        }

        test "fsdocs spike blocker records command log path reason and next action" {
            let spikePath = Path.Combine(archiveFeatureReadinessRoot, "fsharp-formatting-spike.md")
            Expect.isTrue (File.Exists spikePath) $"FSharp.Formatting spike record exists at {spikePath}"

            let spike = File.ReadAllText spikePath

            [ "command:"
              "log path:"
              "blocker:"
              "reason:"
              "next action:"
              "secondary or hybrid"
              "not authoritative" ]
            |> List.iter (fun required -> Expect.stringContains spike required $"fsdocs spike records {required}")
        }
    ]
