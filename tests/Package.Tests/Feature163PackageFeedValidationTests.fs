module Feature163PackageFeedValidationTests

open System
open System.IO
open System.Xml.Linq
open Expecto
open FS.GG.TestSupport

let private root = RepositoryRoot.value
let private repo (path: string) = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))

let private xmlValue (doc: XDocument) (name: string) =
    doc.Descendants()
    |> Seq.tryFind (fun e -> e.Name.LocalName = name)
    |> Option.map (fun e -> e.Value.Trim())

let private packageVersions () : Map<string, string> =
    Directory.GetFiles(repo "src", "*.fsproj", SearchOption.AllDirectories)
    |> Array.choose (fun (project: string) ->
        let doc = XDocument.Load project
        match xmlValue doc "PackageId", xmlValue doc "Version", xmlValue doc "IsPackable" with
        | Some id, Some version, Some packable when id.StartsWith("FS.GG.UI.", StringComparison.Ordinal) && String.Equals(packable, "true", StringComparison.OrdinalIgnoreCase) ->
            Some(id, version)
        | _ -> None)
    |> Map.ofArray

let private packageReferences (project: string) : (string * string) list =
    let doc = XDocument.Load project

    doc.Descendants()
    |> Seq.filter (fun e -> e.Name.LocalName = "PackageReference")
    |> Seq.choose (fun reference ->
        let packageId =
            reference.Attributes()
            |> Seq.tryFind (fun a -> a.Name.LocalName = "Include")
            |> Option.map _.Value

        let version =
            reference.Attributes()
            |> Seq.tryFind (fun a -> a.Name.LocalName = "Version")
            |> Option.map _.Value

        match packageId, version with
        | Some id, Some v when id.StartsWith("FS.GG.UI.", StringComparison.Ordinal) -> Some(id, v)
        | _ -> None)
    |> Seq.toList

[<Tests>]
let tests =
    testList "Feature163 package feed validation" [
        test "AntShowcase FS.GG.UI package pins match source-controlled package versions" {
            let versions = packageVersions ()
            let projects = Directory.GetFiles(repo "samples/AntShowcase", "*.fsproj", SearchOption.AllDirectories)

            let pins =
                projects
                |> Array.collect (fun project -> packageReferences project |> List.map (fun pin -> project, pin) |> List.toArray)

            Expect.isGreaterThan pins.Length 0 "AntShowcase package pins exist"

            for project, (packageId, declared) in pins do
                Expect.isTrue (versions.ContainsKey packageId) $"{packageId} has a source version"
                Expect.equal declared versions[packageId] $"{packageId} pin in {project}"
        }

        test "AntShowcase selected sample does not directly reference framework src projects" {
            let projects = Directory.GetFiles(repo "samples/AntShowcase", "*.fsproj", SearchOption.AllDirectories)

            for project in projects do
                let text = File.ReadAllText project
                Expect.isFalse (text.Contains("..\\..\\..\\src\\", StringComparison.OrdinalIgnoreCase)) $"no source ProjectReference in {project}"
                Expect.isFalse (text.Contains("../../../src/", StringComparison.OrdinalIgnoreCase)) $"no source ProjectReference in {project}"
        }

        test "package source mapping keeps FS.GG.UI packages on the local feed" {
            let text = File.ReadAllText(repo "samples/AntShowcase/nuget.config")

            Expect.stringContains text "<packageSourceMapping>" "source mapping"
            Expect.stringContains text "key=\"nuget-local\"" "local feed"
            Expect.stringContains text "pattern=\"FS.GG.UI.*\"" "framework packages"
            Expect.stringContains text "key=\"nuget.org\"" "third-party source"
        }

        test "source-controlled readiness evidence links package proof lanes and validation records" {
            [ "specs/163-package-feed-validation-lanes/readiness/compatibility-ledger.md", "repository validation harness/script contracts changed"
              "specs/163-package-feed-validation-lanes/readiness/package-validation.md", "Feature163"
              "specs/163-package-feed-validation-lanes/readiness/regression-validation.md", "AntShowcase package-only restore behavior"
              "specs/163-package-feed-validation-lanes/readiness/validation-summary.md", "lanes/summary.md" ]
            |> List.iter (fun (path, token) ->
                let full = repo path
                Expect.isTrue (File.Exists full) $"exists: {path}"
                Expect.stringContains (File.ReadAllText full) token token)
        }
    ]
