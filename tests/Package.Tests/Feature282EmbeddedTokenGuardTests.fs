module Feature282EmbeddedTokenGuardTests

// FS-GG/FS.GG.Rendering#282 (generalizes #264).
//
// `.template.config/template.json` rewrites two tokens into every non-`copyOnly` scaffold file:
// `Product` -> effectiveName and `product` -> effectiveNameLower. `replaces` is a plain SUBSTRING
// match, not a word-boundary match — #264's capture from a real scaffold proves it, because
// `cross products` came out as `cross breakout1s`: the engine matched `product` INSIDE `products`
// and left the trailing `s` behind.
//
// So ANY word that merely CONTAINS `product` is collateral. In a product named `Breakout1`:
//
//     // production tree-render path ...      ->  // breakout1ion tree-render path ...
//     Game/sample-pack products pin it here   ->  Game/sample-pack breakout1s pin it here
//
// WHY THE TWO EXISTING GATES CANNOT SEE THIS. `ScaffoldIdentifierLeakGuardTests` (#149/#152) is
// declaration-anchored and strips comments, so a comment or a `.props` file is invisible to it.
// `Feature264FragmentProseTests` (#264) bans the mathematical compounds (`cross product`, ...);
// `production` is not a compound, and its own honesty caveat says so in as many words. Neither gate
// is wrong — the class simply falls between them.
//
// WHY THIS GATE BANS THE EMBEDDED TOKEN AND NOT THE WORD. `product` is also used, deliberately, as
// the common noun for the generated app ("your product tree", `Product-owned ... YOURS TO ADAPT`),
// where substituting the scaffolded name reads naturally and is the POINT of the non-`copyOnly`
// rewrite; `template/base` alone carries ~21 of them. #264 concluded that a bare-token scan "would
// drown in the ~21 deliberate common-noun uses" and deferred the class to the durable sentinel fix.
//
// That conclusion holds for the BARE token and only for it. This gate matches something strictly
// narrower: `product`/`Product` immediately adjacent to another word character on either side. The
// ~21 deliberate uses are all free-standing nouns, so they carry a space or punctuation on both
// sides and NONE of them match. On the tree as it stood at #282 the scan found exactly the nine
// real defects and nothing else — a zero-false-positive separation of the intended rewrite from the
// collateral one. The plural is the clearest case: `products` refers to the CLASS of generated
// products, never to this one, so substituting it is semantically wrong even before it mangles.
//
// This closes the issue's worry that a reword "only buys time — the next `product`-containing word
// reintroduces it". It cannot: the next one fails HERE, at template-author time. The sentinel
// (`__ProductNamespace__`, #264's option (2)) remains the durable fix — it would make collateral
// impossible BY CONSTRUCTION rather than by gate — but it is a template-machinery change that must
// re-decide each of the ~21 common-noun sites, and it is no longer urgent.
//
// HONESTY CAVEAT (constitution Principle V): a static regex scan over the substitution-subject file
// set, not a `dotnet new` instantiation. It proves ONE bounded thing precisely — no word containing
// `product`/`Product` as a PROPER substring survives in a file the engine rewrites, so none can be
// mangled. It does NOT see: the bare common noun (intended, and out of scope by construction); a
// token split across a line break; or byte-identity of scaffolded output against upstream, which the
// env-gated `scripts/validate-productname-template.fsx` diff covers.

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
    let isWord c = System.Char.IsLetterOrDigit c || c = '_'
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
let feature282EmbeddedTokenGuardTests =
    testList
        "scaffold embedded-token substitution guard (#282)"
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
        ]
