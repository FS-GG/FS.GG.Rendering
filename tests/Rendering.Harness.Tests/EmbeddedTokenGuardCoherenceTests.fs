module EmbeddedTokenGuardCoherenceTests

// FS-GG/FS.GG.Rendering#366 — a PR-VISIBLE twin of the release-only embedded-token substitution guard.
//
// WHY THIS EXISTS. `.template.config/template.json` rewrites `Product`/`product` into every non-copyOnly
// scaffold file via a plain SUBSTRING `replaces`, so any word that merely CONTAINS `product`
// (`production`, `products`, `dotProduct`) is mangled by `dotnet new`. `Feature282EmbeddedTokenGuardTests`
// (tests/Package.Tests) bans exactly that embedded-token shape across the substitution-subject file set.
// But Package.Tests is RELEASE-ONLY: it is not in `FS.GG.Rendering.slnx` and runs only under
// `dotnet test … -c Release` in release.yml. So a new `product`-containing word added to a substituted
// scaffold file compiles green on a PR and only reds the release lane — the exact "reword only buys time"
// regression #282 exists to make impossible at template-author time. That is the "PR-gated tests must be
// in the slnx" gap #350/#382 already closed for the launch host and the audio wiring, and the twins
// through #421 have been closing rule-by-rule.
//
// WHAT IT LOCKS. Feature282EmbeddedTokenGuardTests is already fully static and self-contained (no pack,
// no restore, no assembly load): it derives the scanned file set from `template.json` via
// `ScaffoldSources.substitutionSubjectFiles` and regex-scans each for the embedded token. So this hoist
// is a faithful mirror — it runs the SAME scan over the SAME derived inputs, one gate earlier.
//
// L-INPUTS EXEMPTION (SharedInputs = false). The counterpart reads its inputs through
// `ScaffoldSources.substitutionSubjectFiles`, not the `repositoryPath "…"` / `repo "…"` helper that
// ReleaseOnlyTwinLockstepTests's L-INPUTS keys on, so the shared-inputs invariant cannot be mechanically
// checked for this shape (a byte-faithful hoist extracts zero literal inputs and would trip L-INPUTS's
// non-empty guard). The pair is registered with SharedInputs = false — the same documented exemption the
// launch pair already carries — so L-EXISTS / L-NAMES / L-CLOSED still guard the pairing; only the
// input-set equality is waived, and byte-faithfulness (below) is what keeps the two in step in practice.
//
// Kept in deliberate lockstep with tests/Package.Tests/Feature282EmbeddedTokenGuardTests.fs: a verbatim
// hoist (module name, the `[<Tests>]` binding, and the testList label aside), so a real drift fails BOTH.

open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

// EVERY file the engine rewrites, of every extension — derived from `template.json`'s `sources`, not
// restated here. `Feature264FragmentProseTests` and `ScaffoldIdentifierLeakGuardTests` scan the
// narrower `.fs`/`.fsi` view (`ScaffoldSources.files`) because their defects are F#-only. #282's is
// not: four of its nine sites are `.props`, `.fsx` and `.md`.
let private substitutionSubjectFiles = ScaffoldSources.substitutionSubjectFiles repositoryRoot

/// `product`/`Product` with a word character welded to it on either side — the exact shape the
/// substring `replaces` mangles and a word-boundary `replaces` would not. Case is SIGNIFICANT: the
/// engine declares two exact-case tokens (`Product`, `product`), so `PRODUCT` is never rewritten and
/// must not be flagged. Both adjacencies matter — `products` (trailing) and `dotProduct` (leading)
/// are each rewritten.
let private embeddedToken =
    Regex(@"[A-Za-z0-9_](?:Product|product)|(?:Product|product)[A-Za-z0-9_]", RegexOptions.Compiled)

/// Report the whole word the token is embedded in, not the two-character regex match — `products` is
/// an actionable message where `ts` is a puzzle.
let private enclosingWord (line: string) (index: int) =
    // The SAME ASCII class the matcher uses. `Char.IsLetterOrDigit` is Unicode-aware and would widen
    // the reported word across letters the pattern never considered an adjacency, so the message
    // would name a word the scan did not actually match on.
    let isWord c = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c = '_'
    let mutable start = index
    let mutable finish = index
    while start > 0 && isWord line.[start - 1] do
        start <- start - 1
    while finish < line.Length - 1 && isWord line.[finish + 1] do
        finish <- finish + 1
    line.Substring(start, finish - start + 1)

let private scanLine (line: string) : string list =
    embeddedToken.Matches line
    |> Seq.map (fun m -> enclosingWord line m.Index)
    |> Seq.distinct
    |> Seq.toList

type private Finding =
    { File: string
      Line: int
      Word: string
      Text: string }

let private scanFile (path: string) : Finding list =
    let relative = Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/')

    File.ReadAllLines path
    |> Array.mapi (fun i line -> i + 1, line)
    |> Array.collect (fun (n, line) ->
        scanLine line
        |> List.map (fun word ->
            { File = relative
              Line = n
              Word = word
              Text = line.Trim() })
        |> List.toArray)
    |> Array.toList

/// Text files only. A binary asset cannot carry prose, and `File.ReadAllLines` on one is noise.
/// An extensionless file is assumed to be text (a `.gitignore`-shaped file), which errs toward
/// scanning — the safe direction.
let private isTextFile (path: string) =
    match Path.GetExtension path with
    | null -> true
    | extension ->
        match extension.ToLowerInvariant() with
        | ".png" | ".jpg" | ".jpeg" | ".gif" | ".ico" | ".woff" | ".woff2" | ".ttf" | ".zip" | ".dll" | ".pdb" -> false
        | _ -> true

let private findings = substitutionSubjectFiles |> List.filter isTextFile |> List.collect scanFile

[<Tests>]
let feature282EmbeddedTokenGuardCoherenceTests =
    testList
        "#366 twin — scaffold embedded-token substitution guard (#282)"
        [ test "no word merely CONTAINING the `product` substitution token survives in a substituted scaffold file" {
              // A hit here means `dotnet new` will rewrite the token inside a longer word and leave the
              // remainder dangling (`production` -> `<Name>ion`, `products` -> `<Name>s`). Reword to a
              // token-free synonym — `scaffolds`/`generated apps` for the generic plural, `framework`
              // or `real` for `production` used as an adjective.
              Expect.isEmpty
                  findings
                  (findings
                   |> List.map (fun f -> sprintf "%s:%d [%s] %s" f.File f.Line f.Word f.Text)
                   |> String.concat "\n"
                   |> sprintf
                       "a word containing the `product` substitution token as a proper substring survives\n\
                        in a substituted scaffold file; `dotnet new` will mangle it:\n%s")
          }

          test "the scan enumerates the substituted scaffold files (must not silently narrow to zero)" {
              // Backstop: if the `template.json` read or the glob translation ever returns an empty set,
              // the guard above passes vacuously. Pin a lower bound and the four non-F# sites the F#-only
              // enumeration (`ScaffoldSources.files`) structurally cannot reach.
              Expect.isGreaterThan
                  (List.length substitutionSubjectFiles)
                  20
                  "substitution-subject enumeration collapsed — the embedded-token scan would pass vacuously"

              let mustScan =
                  [ "template/base/Directory.Build.props"
                    "template/base/Directory.Packages.props"
                    "template/base/build.fsx"
                    "template/base/docs/product.md"
                    "template/base/src/Product/View.fs"
                    "template/base/tests/Product.Tests/BehaviorTests.fs" ]

              let scanned =
                  substitutionSubjectFiles
                  |> List.map (fun p -> Path.GetRelativePath(repositoryRoot, p).Replace('\\', '/'))
                  |> Set.ofList

              mustScan
              |> List.iter (fun p ->
                  Expect.isTrue (Set.contains p scanned) (sprintf "a known #282 mangle site is not in the scanned set: %s" p))
          }

          test "the scan honours copyOnly and exclude (must not over-scan into verbatim trees)" {
              // The `copyOnly` trees are NOT substituted, so their prose is safe by construction and a
              // hit there would be a false positive. `docs/api-surface/**` in particular is full of
              // `ProductDefect`, and `template/base/` is re-emitted a second time with an `include` that
              // a naive union would read as "all of template/base", dragging in `.claude/` too.
              let scanned =
                  substitutionSubjectFiles
                  |> List.map (fun p -> Path.GetRelativePath(repositoryRoot, p).Replace('\\', '/'))
                  |> Set.ofList

              let mustNotScan =
                  [ "template/base/docs/evidence-formats.md" // copyOnly
                    "template/base/docs/scaffold-map.md" // copyOnly
                    "template/base/docs/interactive-readiness.md" ] // copyOnly

              mustNotScan
              |> List.iter (fun p ->
                  Expect.isFalse (Set.contains p scanned) (sprintf "a copyOnly (verbatim) file leaked into the scanned set: %s" p))

              Expect.isFalse
                  (scanned |> Set.exists (fun p -> p.StartsWith "template/base/docs/api-surface/"))
                  "the copyOnly docs/api-surface/** contract tree leaked into the scanned set"

              Expect.isFalse
                  (scanned |> Set.exists (fun p -> p.StartsWith "template/base/.claude/"))
                  "the excluded template/base/.claude/ tree leaked into the scanned set (an `include` was ignored)"
          }

          test "the scanner detects the real mangle sites and spares the intended common noun" {
              // Prove the pattern separates collateral from intent. `mustFlag` is the nine sites #282
              // found, verbatim; `mustPass` is the deliberate common-noun prose the rewrite exists to
              // serve, which a bare-token scan would have condemned.
              let mustFlag =
                  [ "// production tree-render path (`Control.renderTree`) at the output extent, so the"
                    "test \"default view renders real controls through the production render path\" {"
                    "       spec-kit lifecycle emitted the vendored script (sdd/none products never run it — the"
                    "         products pin it here; simulation profiles only. -->"
                    "// are copied into or executed by generated products; the only retained external process is"
                    "Generated products expose a compact consumer API map before app-specific code:"
                    "let dotProduct a b = a.X * b.X + a.Y * b.Y" // leading adjacency mangles too
                    "  <ProductName>x</ProductName>" ]

              let mustPass =
                  [ "/// Product-owned 2D-visibility helper — THIS FILE IS YOURS TO ADAPT." // the intended header
                    "/// Where `resolver` looks for PCM WAV files, relative to the running product."
                    "/// the ONE place bare `Scene` record literals appear in your product tree, where only `Scene` types"
                    "// `forTransition` is the ONLY place this product decides what to play."
                    "Expect.isEmpty subscriptions \"default generated product has no subscriptions\""
                    "namespace Product.Tests" // `.` is not a word character
                    "// see src/Product/View.fs" // `/` is not a word character
                    "// a cross-product, which is #264's business and not this gate's"
                    "// PRODUCTION, shouted, is not a declared token and is never rewritten" ] // case is significant

              mustFlag
              |> List.iter (fun l -> Expect.isNonEmpty (scanLine l) (sprintf "real mangle site not detected: %s" l))

              mustPass
              |> List.iter (fun l -> Expect.isEmpty (scanLine l) (sprintf "false positive on legitimate line: %s" l))
          }

          test "a finding names the whole enclosing word, not the two-character match" {
              // The message must be actionable: `products`, not `ts`.
              Expect.equal (scanLine "generated products pin it") [ "products" ] "the plural is reported whole"
              Expect.equal (scanLine "// production path") [ "production" ] "the adjective is reported whole"
              Expect.equal (scanLine "let dotProduct a b = 0") [ "dotProduct" ] "a leading-adjacency identifier is reported whole"
          }

          test "a declared source root that vanished fails loudly instead of narrowing the scan" {
              // The one narrowing no downstream count backstop can see: rename `template/fragments/vec2/src`
              // and its files simply stop being scanned, while the OTHER roots keep the total well above
              // any lower bound. So drive the real branch with a fixture repo whose template.json declares
              // one root that exists and one that does not.
              let fixture = Path.Combine(Path.GetTempPath(), "fsgg-282-fixture-" + string (System.Guid.NewGuid()))
              let present = Path.Combine(fixture, "template", "base")

              try
                  Directory.CreateDirectory(Path.Combine(fixture, ".template.config")) |> ignore
                  Directory.CreateDirectory present |> ignore
                  File.WriteAllText(Path.Combine(present, "View.fs"), "// nothing to see")

                  let templateJson =
                      """{ "sources": [ { "source": "template/base/" }, { "source": "template/gone/" } ] }"""

                  File.WriteAllText(Path.Combine(fixture, ".template.config", "template.json"), templateJson)

                  Expect.throws
                      (fun () -> ScaffoldSources.substitutionSubjectFiles fixture |> ignore)
                      "a declared-but-absent source root must raise, not silently contribute zero files"

                  // And the same fixture WITHOUT the absent root scans normally — proving the throw is
                  // caused by the missing root, not by the fixture being malformed.
                  File.WriteAllText(
                      Path.Combine(fixture, ".template.config", "template.json"),
                      """{ "sources": [ { "source": "template/base/" } ] }"""
                  )

                  Expect.hasLength (ScaffoldSources.substitutionSubjectFiles fixture) 1 "the surviving root is scanned normally"
              finally
                  if Directory.Exists fixture then
                      Directory.Delete(fixture, true)
          }
        ]
