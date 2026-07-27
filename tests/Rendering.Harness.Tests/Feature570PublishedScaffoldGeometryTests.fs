module Feature570PublishedScaffoldGeometryTests

// #570 (from #519) — FS.GG.Game hand-re-declares `Geometry.Vec2` because it "cannot reference ours".
//
// THE DECISION, recorded here because #570 asked for it explicitly and because the adaptability it turns
// on is load-bearing.
//
// Vec2 is deliberately PRODUCT-OWNED: the fragment header says "THIS FILE IS YOURS TO ADAPT — rename
// Vx/Vy, add a Z, or delete it", and its compile item is `Exists`-guarded so deleting it keeps the build
// green. #570 lists three ways out and warns that the conventional one — ship Vec2 in a package and have
// products reference it — would DESTROY that property: a product cannot adapt a type it references from a
// package. That option is rejected.
//
// What is published instead is the SOURCE, and it already is: `.template.package/FS.GG.UI.Template.fsproj`
// packs the repo under `content/`, so the canonical fragment ships inside the `FS.GG.UI.Template` package
// at
//
//     content/template/fragments/vec2/src/Product/Vec2.fs
//
// FS.GG.Game can therefore GENERATE its `_scaffold.fs` from that file instead of hand-writing a twin. The
// fragment stays exactly as adaptable as it is today — nothing about the product's copy changes — and the
// duplicate disappears at its root rather than being guarded (FS-GG/FS.GG.Game#189 is the consuming half).
//
// Deliberately NOT a new machine-readable surface format. A generated declaration would be a THIRD
// statement of the same shape, needing its own generator and its own drift gate — the exact "one more copy
// to keep in step" this issue exists to remove. The source IS the surface, and it is the one artifact that
// cannot disagree with itself. It also gives FS.GG.Game something a declaration could not: the real
// `toPoint`/`toRect`/`toSimPoint`/`toSimRect` bodies, which is why their gate currently cannot cover the
// render/sim boundary at all (it omits those helpers rather than fake them).
//
// WHY THIS PACKS THE PACKAGE INSTEAD OF READING THE .fsproj.
//
// The first version of this guard parsed the `Exclude=` attribute and re-implemented MSBuild's glob
// semantics — a MODEL of the artifact. Review broke it three ways in one sitting, and every one of them is
// a way somebody would really do it:
//
//   * `<Content Remove="..\template\fragments\vec2\**" />` — the idiomatic MSBuild way to drop an item.
//     The fragment vanished from the nupkg; the guard passed, because `Remove` is not `Exclude`.
//   * narrowing `Include="..\**\*"` to `Include="..\src\**\*"`. The guard never read `Include` at all.
//     Same for `PackagePath`: move it and the consumed path moves with it, silently.
//   * the conditional check tested `<!--#if`, the XML form. F# template sources use `//#if` (see
//     template/base/src/Product/Model.fs:4). A real conditional in Vec2.fs sailed through.
//
// The lesson is not "add the three missing cases" — it is that modeling MSBuild is the wrong altitude, and
// a fourth construct would have been found the moment the third was patched. The CONTRACT is about the
// nupkg, so this reads the nupkg: pack it, open it, look. ~3s, and it cannot be fooled by an MSBuild
// construct nobody thought of, because it never reasons about MSBuild at all.

open System.Diagnostics
open System.IO
open System.IO.Compression
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relative: string) =
    Path.Combine(repositoryRoot, relative.Replace('/', Path.DirectorySeparatorChar))

/// The path FS.GG.Game consumes, on both sides of the package boundary. Changing either is a cross-repo
/// break, so they are spelled once.
let private fragmentRelative = "template/fragments/vec2/src/Product/Vec2.fs"
let private packedEntry = "content/" + fragmentRelative

/// Pack the template package into a scratch directory and return its entries.
///
/// Deliberately isolated: `-o` and `BaseIntermediateOutputPath` both point at the scratch dir, and the
/// restore cannot write a lock file, so a run leaves the working tree byte-for-byte unchanged. (It does
/// not: a stray `packages.lock.json` from an unisolated pack is exactly the kind of artifact that reddens
/// an unrelated regeneration gate later.)
///
/// 1089 — HOW THAT ISOLATION IS SPELLED CHANGED, AND IT HAD TO. This used to pass
/// `-p:RestorePackagesWithLockFile=false`, which was the right way to say "do not write a lock" while
/// `.template.package` had none. #1089 committed one — the project whose output IS the published package
/// was restoring UNLOCKED in the release lane — and NuGet rejects that combination outright:
///
///   error NU1005: Invalid restore input where RestorePackagesWithLockFile property is set to false
///   but a packages lock file exists at .../.template.package/packages.lock.json
///
/// `RestoreLockedMode=true` states the same intent against a project that now HAS a lock: locked restore
/// validates the committed graph and never rewrites the file, so the working tree is still untouched.
/// It is also strictly stronger — this pack is the one place outside `release.yml` that restores this
/// project, so it is now also where a lock that has drifted from the central pin gets caught, on the PR
/// rather than in the middle of a release. Do not "simplify" this back to dropping the flag: without it a
/// local (non-CI) run restores unlocked and MAY rewrite the committed lock, which is the working-tree
/// mutation this comment has always been about.
let private packedFragmentBytes =
    lazy
        (let scratch = Path.Combine(Path.GetTempPath(), "fs-gg-570-" + System.Guid.NewGuid().ToString("N"))
         Directory.CreateDirectory scratch |> ignore

         let psi = ProcessStartInfo "dotnet"
         psi.WorkingDirectory <- repositoryRoot
         psi.UseShellExecute <- false
         psi.RedirectStandardOutput <- true
         psi.RedirectStandardError <- true

         [ "pack"
           ".template.package/FS.GG.UI.Template.fsproj"
           "-o"
           scratch
           "-m:1"
           "--nologo"
           $"-p:BaseIntermediateOutputPath={scratch}/obj/"
           "-p:RestoreLockedMode=true" ]
         |> List.iter psi.ArgumentList.Add

         match Process.Start psi with
         | null -> failwith "could not start `dotnet pack` for the template package"
         | started ->
             use proc = started
             // Drain both streams BEFORE WaitForExit, or a full pipe buffer deadlocks the child.
             let out = proc.StandardOutput.ReadToEnd()
             let err = proc.StandardError.ReadToEnd()
             proc.WaitForExit()

             if proc.ExitCode <> 0 then
                 failwithf "dotnet pack of the template package failed (exit %d) — this guard cannot verify what the package publishes:\n%s\n%s" proc.ExitCode out err

             let nupkg =
                 match Directory.GetFiles(scratch, "*.nupkg") with
                 | [| single |] -> single
                 | found -> failwithf "expected exactly one .nupkg from the template package, found %d" found.Length

             let bytes, names =
                 use archive = ZipFile.OpenRead nupkg

                 let bytes =
                     archive.Entries
                     |> Seq.tryFind (fun entry -> entry.FullName.Replace('\\', '/') = packedEntry)
                     |> Option.map (fun entry ->
                         use stream = entry.Open()
                         use buffer = new MemoryStream()
                         stream.CopyTo buffer
                         buffer.ToArray())

                 bytes, (archive.Entries |> Seq.map (fun e -> e.FullName) |> Seq.toList)

             Directory.Delete(scratch, true)
             bytes, names)

[<Tests>]
let tests =
    testList
        "Feature570 published scaffold geometry"
        [ test "the canonical Vec2 fragment exists at the path FS.GG.Game consumes" {
              Expect.isTrue
                  (File.Exists(repositoryPath fragmentRelative))
                  $"`{fragmentRelative}` is the file FS.GG.Game generates its skill-block scaffold FROM (FS-GG/FS.GG.Game#189). Moving it is a cross-repo break: update their generator in the same change."
          }

          // THE CONTRACT. Not "the .fsproj looks right" — "the artifact contains the file".
          test "the FS.GG.UI.Template package really publishes it — asserted against the packed nupkg" {
              let bytes, names = packedFragmentBytes.Force()

              Expect.isGreaterThan
                  (List.length names)
                  10
                  "the packed nupkg has entries at all (if this fails the assertion below is vacuous)"

              match bytes with
              | Some _ -> ()
              | None ->
                  failtestf
                      "the FS.GG.UI.Template package does NOT contain `%s`. FS.GG.Game restores this package and GENERATES its skill-block scaffold from that file (#570, FS-GG/FS.GG.Game#189); without it their gate compiles a stale hand-written twin — or nothing — and reports green either way. Whatever dropped it (an Exclude, a Content Remove, a narrowed Include, a changed PackagePath) is a silent cross-repo break: re-home the fragment and update their generator in the SAME change. The package DOES contain %d entries, e.g. %A"
                      packedEntry
                      (List.length names)
                      (names |> List.truncate 5)
          }

          // The published bytes must be the canonical bytes. `dotnet new`'s `sourceName` / `replaces`
          // transforms happen at INSTANTIATION, not at pack — so a consumer who unzips gets this file
          // verbatim. If that ever stopped being true, "generate from the published source" would quietly
          // become "generate from a transformed copy of it", which is a twin again.
          test "the published bytes are the canonical source, unmodified" {
              let bytes, _ = packedFragmentBytes.Force()
              let onDisk = File.ReadAllBytes(repositoryPath fragmentRelative)

              match bytes with
              | None -> failtest "the fragment is not in the package (see the previous test)"
              | Some packed ->
                  Expect.equal
                      (packed.Length)
                      (onDisk.Length)
                      "the packed fragment must be byte-identical to the working-tree source — a consumer generating from the package must get exactly what this repo reviews"

                  Expect.isTrue (packed = onDisk) "the packed fragment must be byte-identical to the working-tree source"
          }

          // The consumption contract is the SHAPE too: FS.GG.Game compiles this file OUTSIDE a generated
          // product, so it must stay self-contained and free of `dotnet new` conditionals.
          test "the published fragment is compilable by a consumer that is not a generated product" {
              let source = File.ReadAllText(repositoryPath fragmentRelative)

              Expect.stringContains source "module Geometry" "FS.GG.Game's skills say `Geometry.Vec2`; the module name is half the contract (#519)"
              Expect.stringContains source "open FS.GG.UI.Scene" "the scene edge (toPoint/toRect) resolves from the PUBLISHED FS.GG.UI.Scene package — which is what lets a consumer compile the helpers their own gate cannot fake"
              Expect.stringContains source "FS.GG.Game.Core" "the sim edge (toSimPoint/toSimRect) resolves from FS.GG.Game.Core — also a package, so a consumer needs no scaffolded product to compile this file"

              // `//#if`, NOT `<!--#if`. The F# template sources use the slash form (template/base/src/Product/
              // Model.fs:4); the first version of this guard checked the XML form and so was decorative
              // against the only conditional syntax that can actually appear here.
              Expect.isFalse
                  (System.Text.RegularExpressions.Regex.IsMatch(source, @"^\s*//#(if|else|endif)", System.Text.RegularExpressions.RegexOptions.Multiline))
                  "the fragment must carry no `dotnet new` conditional (`//#if`), or the published text is not compilable verbatim and the consumer needs a hand-maintained transform to strip it — which is the twin, back again"
          } ]
