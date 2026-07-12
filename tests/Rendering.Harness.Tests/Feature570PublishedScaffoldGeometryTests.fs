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
// duplicate disappears at its root rather than being guarded (FS-GG/FS.GG.Game#141 is the consuming half).
//
// Deliberately NOT a new machine-readable surface format. A generated declaration would be a THIRD
// statement of the same shape, needing its own generator and its own drift gate — the exact "one more copy
// to keep in step" this issue exists to remove. The source IS the surface, and it is the one artifact that
// cannot disagree with itself. It also gives FS.GG.Game something a declaration could not: the real
// `toPoint`/`toRect`/`toSimPoint`/`toSimRect` bodies, which is why their gate currently cannot cover the
// render/sim boundary at all (it omits those helpers rather than fake them).
//
// WHAT THIS FILE GUARDS. The package's `Content` item is one broad `..\**\*` glob with an Exclude list. So
// the fragment ships today by DEFAULT, not by decision — and one added Exclude would stop publishing it
// with nothing to say so. That is a silent break of a cross-repo contract another repo compiles against.
// This is the assertion that makes the publication deliberate.

open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relative: string) =
    Path.Combine(repositoryRoot, relative.Replace('/', Path.DirectorySeparatorChar))

/// The published path FS.GG.Game consumes. Changing it is a cross-repo break, so it is spelled once.
let private fragmentRelative = "template/fragments/vec2/src/Product/Vec2.fs"

let private templatePackageProject =
    File.ReadAllText(repositoryPath ".template.package/FS.GG.UI.Template.fsproj")

/// The `Exclude=` globs on the package's Content item, normalised to repo-relative forward-slash form
/// (they are written relative to `.template.package/`, hence the leading `..\`).
let private excludeGlobs =
    let attribute = Regex.Match(templatePackageProject, "Exclude=\"(?<globs>[^\"]*)\"", RegexOptions.Singleline)

    if not attribute.Success then
        []
    else
        attribute.Groups.["globs"].Value.Split(';')
        |> Array.map (fun glob -> glob.Trim().Replace('\\', '/'))
        |> Array.filter (fun glob -> glob <> "")
        |> Array.map (fun glob -> if glob.StartsWith "../" then glob.Substring 3 else glob)
        |> List.ofArray

/// MSBuild glob semantics, narrowed to the two forms this Exclude list actually uses: `**` spans any number
/// of directories, `*` stops at a separator.
let private globMatches (glob: string) (path: string) =
    let pattern =
        Regex.Escape(glob).Replace(@"\*\*/", "(.*/)?").Replace(@"\*\*", ".*").Replace(@"\*", "[^/]*")

    Regex.IsMatch(path, "^" + pattern + "$")

[<Tests>]
let tests =
    testList
        "Feature570 published scaffold geometry"
        [ test "the canonical Vec2 fragment exists at the path FS.GG.Game consumes" {
              Expect.isTrue
                  (File.Exists(repositoryPath fragmentRelative))
                  $"`{fragmentRelative}` is the file FS.GG.Game generates its skill-block scaffold FROM (FS-GG/FS.GG.Game#141). Moving it is a cross-repo break: update their generator in the same change."
          }

          // The whole point. The package ships the fragment because a broad glob happens to include it; this
          // makes it a decision. An Exclude that swallowed `template/**` would silently stop publishing the
          // file another repo compiles against, and nothing anywhere would say so.
          test "the FS.GG.UI.Template package still publishes it — no Exclude may swallow the fragment" {
              Expect.isNonEmpty
                  excludeGlobs
                  "the package's Content Exclude list parsed as empty — if this fails the guard below is vacuous and would bless any exclusion"

              let swallowing =
                  excludeGlobs |> List.filter (fun glob -> globMatches glob fragmentRelative)

              Expect.isEmpty
                  swallowing
                  $"`.template.package/FS.GG.UI.Template.fsproj` excludes {swallowing} from the package, which drops `{fragmentRelative}` out of `content/`. FS.GG.Game restores FS.GG.UI.Template and GENERATES its skill-block scaffold from that file (#570); excluding it makes their gate compile a stale hand-written twin, or nothing at all, and reports green either way. If the exclusion is intended, re-home the fragment and file the cross-repo issue on FS-GG/FS.GG.Game in the SAME change."
          }

          // Guards the guard: the matcher must actually match, or the assertion above is decorative.
          test "the exclude matcher really matches the globs this project uses" {
              Expect.isTrue (globMatches "obj/**" "obj/Debug/x.fs") "a directory-prefix glob matches beneath it"
              Expect.isTrue (globMatches "**/bin/**" "src/Scene/bin/x.dll") "a **/ glob spans directories"
              Expect.isTrue (globMatches "specs/**" "specs/192-x/spec.md") "the specs exclusion matches specs"
              Expect.isFalse (globMatches "specs/**" fragmentRelative) "and does NOT match the fragment"
              Expect.isTrue (globMatches "template/**" fragmentRelative) "a template/** exclusion WOULD swallow the fragment — this is the case the guard above must catch"
          }

          // The consumption contract is the SHAPE, not just the path: FS.GG.Game compiles this file, so it
          // must stay a self-contained `module Geometry` that opens only published packages.
          test "the published fragment is compilable by a consumer that is not a generated product" {
              let source = File.ReadAllText(repositoryPath fragmentRelative)

              Expect.stringContains source "module Geometry" "FS.GG.Game's skills say `Geometry.Vec2`; the module name is half the contract (#519)"
              Expect.stringContains source "open FS.GG.UI.Scene" "the scene edge (toPoint/toRect) resolves from the PUBLISHED FS.GG.UI.Scene package, which is what lets a consumer compile the helpers their own gate cannot fake"
              Expect.stringContains source "FS.GG.Game.Core" "the sim edge (toSimPoint/toSimRect) resolves from FS.GG.Game.Core — also a package, so a consumer needs no scaffolded product to compile this file"

              // A `#if`/template token would make the published file uncompilable as-is, and the generator on
              // the other side would have to strip it — re-introducing a hand-maintained transform.
              Expect.isFalse (source.Contains "<!--#if") "the fragment must carry no dotnet-new conditional, or a consumer cannot compile the published text verbatim"
          } ]
