module Feature207BomMembershipTests

// Feature 207 — ALWAYS-ON membership-parity / shape gate for the FS.GG.UI BOM metapackage.
//
// Fully deterministic and self-contained (no pack/restore, no report dependency): it re-derives
// the parity invariant directly from the repo so membership drift is a loud RED, not a silently
// missing member in an adopter's graph (US2 AS3 / spec edge case "new member added").
//
//   parity (E2 / FR-003): { nuspec <dependency> ids } == { discovered IsPackable=true FS.GG.UI.*
//                          projects under src/** } (template & IsPackable=false excluded).
//   single token (INV-1): every dependency version is the single literal `[$version$]` — no
//                          per-member version literal anywhere (FR-009).
//   exact bracket (R1/FR-004): every version is exact-bracket `[...]` (not a floating `[..,)`),
//                          the form that makes deviation detectable in BOTH directions.
//   no lib (E1):           the nuspec is dependencies-only (explicit empty <files/>; no build output).
//
// T012 (lockstep, US2): the set-equality assertion is the drift detector — adding or removing a
// packable FS.GG.UI.* project WITHOUT a matching nuspec edit breaks equality and turns this test
// red in either direction.

open System
open System.IO
open System.Xml.Linq
open Expecto
open FS.GG.TestSupport

let private root = RepositoryRoot.value
let private repo (path: string) = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))

let private nuspecPath = repo "src/Meta/FS.GG.UI.nuspec"

let private xmlValue (doc: XDocument) (name: string) =
    doc.Descendants()
    |> Seq.tryFind (fun e -> e.Name.LocalName = name)
    |> Option.map (fun e -> e.Value.Trim())

/// The discovered packable framework set: IsPackable=true projects under src/** whose PackageId is
/// `FS.GG.UI.*`. ColorPolicy (IsPackable=false) and the bare BOM itself are excluded by construction.
let private discoveredMembers () : Set<string> =
    Directory.GetFiles(repo "src", "*.fsproj", SearchOption.AllDirectories)
    |> Array.choose (fun project ->
        let doc = XDocument.Load project
        match xmlValue doc "PackageId", xmlValue doc "IsPackable" with
        | Some id, Some packable when
            id.StartsWith("FS.GG.UI.", StringComparison.Ordinal)
            && String.Equals(packable, "true", StringComparison.OrdinalIgnoreCase) -> Some id
        | _ -> None)
    |> Set.ofArray

/// (id, rawVersionString) for every <dependency> in the nuspec.
let private nuspecDependencies () : (string * string) list =
    let doc = XDocument.Load nuspecPath
    doc.Descendants()
    |> Seq.filter (fun e -> e.Name.LocalName = "dependency")
    |> Seq.choose (fun e ->
        let attr n = e.Attributes() |> Seq.tryFind (fun a -> a.Name.LocalName = n) |> Option.map _.Value
        match attr "id", attr "version" with
        | Some id, Some v -> Some(id, v)
        | _ -> None)
    |> Seq.toList

[<Tests>]
let feature207BomMembershipTests =
    testList "Feature207 BOM membership parity" [

        // BM-A / CP-C / FR-003: the membership list equals the discovered packable set, tracked as a
        // SET (count derived from discovery, never a hard-coded literal). Drift in either direction reds.
        test "nuspec dependency-id set equals the discovered packable FS.GG.UI.* set" {
            let members = discoveredMembers ()
            let depIds = nuspecDependencies () |> List.map fst |> Set.ofList
            let missingFromBom = Set.difference members depIds
            let extraInBom = Set.difference depIds members
            Expect.isEmpty missingFromBom (sprintf "members not pinned by the BOM (add to nuspec): %A" missingFromBom)
            Expect.isEmpty extraInBom (sprintf "BOM pins non-members (remove from nuspec or the project is no longer packable): %A" extraInBom)
            Expect.equal depIds members "BOM dependency-id set must equal the discovered packable FS.GG.UI.* set"
            // count tracks the discovered set (16 today) — asserted via the set, not pinned as a magic number
            Expect.equal (nuspecDependencies ()).Length members.Count "one dependency per discovered member (no dupes, no omissions)"
        }

        // INV-1 / FR-009: every version is the single `[$version$]` token — no second version literal.
        test "every dependency version is the single [$version$] token" {
            for id, v in nuspecDependencies () do
                Expect.equal v "[$version$]" (sprintf "%s must use the single [$version$] token, found %s" id v)
        }

        // R1 / FR-004: every version is exact-bracket form (not a floating lower bound).
        test "every dependency version is exact-bracket form (loud in both directions)" {
            for id, v in nuspecDependencies () do
                Expect.isTrue (v.StartsWith "[" && v.EndsWith "]" && not (v.Contains ",")) (sprintf "%s version %s is not exact-bracket [..]" id v)
        }

        // E1: the BOM ships no assembly — explicit empty <files/>, and it does not list itself.
        test "BOM is dependencies-only and does not pin itself" {
            let text = File.ReadAllText nuspecPath
            Expect.stringContains text "<files />" "explicit empty <files/> keeps the package dependencies-only (no lib/, no build output)"
            let depIds = nuspecDependencies () |> List.map fst
            Expect.isFalse (List.contains "FS.GG.UI" depIds) "the bare FS.GG.UI BOM must not depend on itself"
        }
    ]
