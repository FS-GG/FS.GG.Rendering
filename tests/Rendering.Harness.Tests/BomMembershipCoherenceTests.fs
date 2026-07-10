module BomMembershipCoherenceTests

// FS-GG/FS.GG.Rendering#366 — a PR-VISIBLE twin of the release-only BOM-membership gate.
//
// WHY THIS EXISTS. The FS.GG.UI BOM metapackage (`src/Meta/FS.GG.UI.nuspec`) must pin exactly the
// set of packable `FS.GG.UI.*` framework projects — no more, no less — so an adopter who installs the
// bare BOM gets every member and nothing that has stopped shipping. `Feature207BomMembershipTests`
// (tests/Package.Tests) enforces that parity, but Package.Tests is RELEASE-ONLY: it is not in
// `FS.GG.Rendering.slnx` and runs only under `dotnet test … -c Release` in release.yml. So a PR that
// adds a new packable `FS.GG.UI.*` project (or drops one, or flips `IsPackable`) WITHOUT the matching
// nuspec edit compiles green and only reds the release lane — the "PR-gated tests must be in the slnx"
// gap #350 and #382 already closed for the launch host and the audio wiring.
//
// WHAT IT LOCKS. Feature207BomMembershipTests is already fully static and self-contained (no pack, no
// restore, no report): it re-derives the membership set straight from the repo. So this hoist is a
// faithful mirror — it reads the SAME two source-of-truth inputs STATICALLY (the nuspec and the
// `src/**/*.fsproj` set) and asserts the SAME invariants, one gate earlier:
//   BM-A   the nuspec <dependency> id set equals the discovered packable FS.GG.UI.* set (drift in
//          EITHER direction reds — a missing member and an extra pin both fail).
//   INV-1  every dependency version is the single `[$version$]` token (no per-member literal).
//   R1     every version is exact-bracket `[...]` form (loud in both directions, not a floating range).
//   E1     the BOM is dependencies-only (explicit empty <files/>) and never lists itself.
//
// Kept in deliberate lockstep with tests/Package.Tests/Feature207BomMembershipTests.fs: the two read
// the same repo inputs and assert the same parity, so a real drift fails BOTH — the release check is
// mirrored earlier, never weakened. ReleaseOnlyTwinLockstepTests guards that pairing so the mirror
// cannot silently desync. There is no pack/restore/consumer-graph proof here by design; that half
// (Feature207BomConsumerTests) genuinely needs a feed and stays release-only.

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
/// Mirrors Feature207BomMembershipTests.discoveredMembers so the two classify membership identically.
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
let bomMembershipCoherenceTests =
    testList
        "#366 — BOM membership coherence (PR-time twin of the release-only Feature207 gate)"
        [
          // BM-A / FR-003: the membership list equals the discovered packable set, tracked as a SET
          // (count derived from discovery, never a hard-coded literal). Drift in either direction reds.
          test "nuspec dependency-id set equals the discovered packable FS.GG.UI.* set" {
              let members = discoveredMembers ()
              let depIds = nuspecDependencies () |> List.map fst |> Set.ofList
              let missingFromBom = Set.difference members depIds
              let extraInBom = Set.difference depIds members
              Expect.isEmpty missingFromBom (sprintf "members not pinned by the BOM (add to nuspec): %A" missingFromBom)
              Expect.isEmpty extraInBom (sprintf "BOM pins non-members (remove from nuspec or the project is no longer packable): %A" extraInBom)
              Expect.equal depIds members "BOM dependency-id set must equal the discovered packable FS.GG.UI.* set"
              // Count tracks the discovered set (16 today) — asserted via the set, not pinned as a magic number.
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
