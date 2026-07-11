module ScaffoldIdentifierLeakCoherenceTests

// FS-GG/FS.GG.Rendering#366 — a PR-VISIBLE twin of the release-only scaffold identifier-leak guard.
//
// WHY THIS EXISTS. The template imprints the raw product name into `Product`/`product` tokens, and a
// legal-but-hyphenated name (`Roquelike-DungeonCrawler`) is fine in a string/path/comment but ILLEGAL in
// an F# identifier — so a `product`/`Product` token that lands in a `let`/`type`/`module`/DU/member
// declaration name makes the scaffolded product uncompilable. `ScaffoldIdentifierLeakGuardTests`
// (tests/Package.Tests) bans exactly that across the substitution-subject scaffold `src/` AND `tests/`.
// But Package.Tests is RELEASE-ONLY: it is not in `FS.GG.Rendering.slnx` and runs only under
// `dotnet test … -c Release` in release.yml. So a new `let …product…` binding added to a scaffold source
// compiles green on a PR (the token is not yet substituted here) and only reds the release lane — the
// exact class #149 and #152 each shipped. That is the "PR-gated tests must be in the slnx" gap #350/#382
// already closed for the launch host and the audio wiring, and the twins through #423 have been closing
// rule-by-rule.
//
// WHAT IT LOCKS. ScaffoldIdentifierLeakGuardTests is already fully static and self-contained (no pack, no
// restore, no assembly load): it derives the scanned set from `ScaffoldSources.files` and declaration-
// anchored regex-scans each for a `product`/`Product` token embedded anywhere in a binding/type/module/
// DU-case/member declaration identifier. So this hoist is a faithful mirror — it runs the SAME scan over
// the SAME derived inputs, one gate earlier.
//
// L-INPUTS EXEMPTION (SharedInputs = false). The counterpart reads its inputs through
// `ScaffoldSources.files`, not the `repositoryPath "…"` / `repo "…"` helper that
// ReleaseOnlyTwinLockstepTests's L-INPUTS keys on, so the shared-inputs invariant cannot be mechanically
// checked for this shape (a byte-faithful hoist extracts zero literal inputs and would trip L-INPUTS's
// non-empty guard). The pair is registered with SharedInputs = false — the same documented exemption the
// launch pair and the #282/#264 token-leak twins already carry — so L-EXISTS / L-NAMES / L-CLOSED still
// guard the pairing; only the input-set equality is waived, and byte-faithfulness (below) keeps the two
// in step in practice.
//
// Kept in deliberate lockstep with tests/Package.Tests/ScaffoldIdentifierLeakGuardTests.fs: a verbatim
// hoist (module name, the `[<Tests>]` binding, and the testList label aside), so a real drift fails BOTH.

open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

// The compiled, non-copyOnly scaffold sources: `template/base/{src,tests}` plus the game/sample-pack
// capability fragments under `template/fragments/*/{src,tests}`. These are the trees the template
// engine rewrites `Product`/`product` in (the copyOnly `docs/**` reference trees are exempt from
// substitution, so they carry no hyphen risk and are intentionally out of scope). `tests/` is in
// scope because #152's leak (`let readProductFile` in the game-starter test project) shipped there
// while #149's src-only gate looked away.
//
// Shared with `Feature264FragmentProseTests`, which guards the same trees against the same rewrite
// from the other side (the token inside a mathematical term of art, not an identifier). One list, so
// a newly added substitution-subject tree cannot narrow one guard while the other still covers it.
let private scaffoldSourceFiles = ScaffoldSources.files repositoryRoot

// Declaration-anchored patterns: a `product`/`Product` token appearing ANYWHERE in the declaration
// IDENTIFIER of a binding/type/module/DU-case/member — at its start (`productDefectMessage`, #149)
// OR embedded (`readProductFile`, #152). Each pattern anchors on `^\s*<keyword>` then consumes only
// identifier characters (`[A-Za-z0-9_']*`) before the token — since an identifier run cannot contain
// a space, the match can never reach past the identifier into a later string literal ("product-defect")
// or an `=`-separated value, so those legitimate string/path uses still do NOT match. Comments are
// stripped first (see `stripLineComment`).
let private identifierLeakPatterns =
    [ "let/and binding",
      Regex(@"^\s*(let|and)\s+(mutable\s+|rec\s+|inline\s+|private\s+|internal\s+|public\s+)*[A-Za-z0-9_']*[Pp]roduct", RegexOptions.Compiled)
      "type/module declaration",
      Regex(@"^\s*(type|module)\s+[A-Za-z0-9_']*[Pp]roduct", RegexOptions.Compiled)
      "DU case declaration",
      Regex(@"^\s*\|\s*[A-Za-z0-9_']*[Pp]roduct", RegexOptions.Compiled)
      "member declaration",
      Regex(@"^\s*member\s+(?:[A-Za-z0-9_']+\.)?[A-Za-z0-9_']*[Pp]roduct", RegexOptions.Compiled) ]

/// Strip a trailing `//` line comment so a token merely MENTIONED in a comment cannot trip the
/// scan. Only ever removes text, so it can lower false positives, never add false negatives.
let private stripLineComment (line: string) =
    match line.IndexOf "//" with
    | -1 -> line
    | i -> line.Substring(0, i)

let private scanLine (line: string) : string list =
    let code = stripLineComment line
    identifierLeakPatterns
    |> List.choose (fun (name, rx) -> if rx.IsMatch code then Some name else None)

type private Finding =
    { File: string
      Line: int
      Class: string
      Text: string }

let private scanFile (path: string) : Finding list =
    let rel = Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/')
    File.ReadAllLines path
    |> Array.mapi (fun i line -> i + 1, line)
    |> Array.collect (fun (n, line) ->
        scanLine line
        |> List.map (fun cls -> { File = rel; Line = n; Class = cls; Text = line.Trim() })
        |> List.toArray)
    |> Array.toList

let private findings = scaffoldSourceFiles |> List.collect scanFile

[<Tests>]
let scaffoldIdentifierLeakGuardCoherenceTests =
    testList
        "#366 twin — scaffold identifier leak guard (#149/#152)"
        [ test "no product-name substitution token appears in an F# declaration identifier in the scaffold sources" {
              // A hit here means a hyphenated product name (a legal name, illegal identifier) will not
              // compile — route the identifier through the derived valid namespace (`approot` ->
              // effectiveIdentifierLower / `AppRoot` -> effectiveIdentifier) and keep the raw slug only
              // in string/path contexts.
              Expect.isEmpty
                  findings
                  (findings
                   |> List.map (fun f -> sprintf "%s:%d [%s] %s" f.File f.Line f.Class f.Text)
                   |> String.concat "\n"
                   |> sprintf "product-name slug leaks into an F# identifier (breaks hyphenated names, FS0010):\n%s")
          }

          test "the scan actually enumerates the scaffold sources (must not silently narrow to zero)" {
              // Backstop: if discovery ever returns an empty file set, the guard above passes
              // vacuously. Pin a non-trivial lower bound so a broken enumerator fails loudly.
              Expect.isGreaterThan
                  (List.length scaffoldSourceFiles)
                  4
                  "scaffold source enumeration collapsed — the leak scan would pass vacuously"
              Expect.isTrue
                  (scaffoldSourceFiles
                   |> List.exists (fun p -> p.Replace('\\', '/').EndsWith "template/base/src/Product/EvidenceCommands.fs"))
                  "EvidenceCommands.fs (the #149 leak site) must be in the scanned set"
              Expect.isTrue
                  (scaffoldSourceFiles
                   |> List.exists (fun p -> p.Replace('\\', '/').EndsWith "template/base/tests/Product.Tests/BehaviorTests.fs"))
                  "the game-starter TEST project BehaviorTests.fs (the #152 leak site) must be in the scanned set — tests/ is in scope"
          }

          test "the scanner detects a synthetic identifier leak (not narrowed shut)" {
              // Prove the patterns fire: each synthetic declaration must be flagged, and a string /
              // comment carrying the same token must NOT be (the string/path context is legitimate).
              let mustFlag =
                  [ "    let productDefectMessage = liveClassMessage \"product-defect\""   // #149: leading
                    "            let readProductFile parts ="                              // #152: EMBEDDED
                    "type ProductState = { Tick: int }"
                    "    | ProductDefect"
                    "    member _.productLabel = \"x\""
                    "    member _.readProductFile parts = ()" ]                            // #152: embedded member
              let mustPass =
                  [ "    let defectMessage = liveClassMessage \"product-defect\""          // token only in the string
                    "        Authority = \"product-owned interactive launch\""            // no declaration keyword
                    "// production tree-render path is the point of this comment"
                    "    let approotDefectMessage = liveClassMessage \"product-defect\""   // #149 fix shape
                    "            let readAppRootFile parts =" ]                            // #152 fix shape
              mustFlag
              |> List.iter (fun l ->
                  Expect.isNonEmpty (scanLine l) (sprintf "synthetic leak not detected: %s" l))
              mustPass
              |> List.iter (fun l ->
                  Expect.isEmpty (scanLine l) (sprintf "false positive on legitimate line: %s" l))
          }
        ]
