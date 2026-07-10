module ScaffoldIdentifierLeakGuardTests

// Cross-repo FS-GG/FS.GG.Rendering#149 + #152 (from FS.GG.SDD; blocks FS.GG.SDD#150, epic #148).
//
// The template imprints the product name into two token classes: the capitalized `Product`
// (-> effectiveName) and the lowercased `product` (-> effectiveNameLower). Both carry the RAW
// name, which for a legal-but-hyphenated product name (e.g. `Roquelike-DungeonCrawler`) contains
// a hyphen. A hyphen is fine in a string/path/comment but ILLEGAL in an F# identifier — so any
// `product`/`Product` token that lands ANYWHERE in an *identifier* position (a `let`/`type`/
// `module`/DU/member declaration name) becomes uncompilable (`error FS0010: Unexpected symbol '-'`).
//
// The #142 identifier split correctly routed the module/type/`open` positions through the derived
// valid namespace (`AppRoot` -> effectiveIdentifier), but shipped WITHOUT a repeatable gate, so a
// residual `let productDefectMessage` binding in EvidenceCommands.fs slipped through (#149) and was
// caught only downstream by SDD's composition smoke. #149 added this gate — but scoped it to `src/`
// AND anchored the token at the START of the identifier, so a SECOND leak (#152) slipped past it: a
// `let readProductFile` helper in the game-starter TEST project (`tests/`) with `Product` EMBEDDED
// mid-identifier. This gate now covers `tests/` too and matches the token embedded anywhere in a
// declaration identifier, not just at its start.
//
// HONESTY CAVEAT (constitution Principle V): this is a static, declaration-anchored regex scan, not
// an F# parse. It proves ONE bounded thing precisely — no `product`/`Product` substitution token
// appears within a `let`/`and`/`type`/`module`/DU-case/`member` declaration name in the compiled,
// substitution-subject scaffold `src/` AND `tests/`. It cannot see identifier positions with no
// declaration keyword on the line (e.g. a leaked function PARAMETER name) or exotic declaration
// forms; those remain covered by the env-gated live build (Feature217) and SDD#150's smoke. It is
// deliberately cheap and always-on so the common case (a new `let …product…` binding) fails HERE,
// at template-author time, instead of downstream.

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
let scaffoldIdentifierLeakGuardTests =
    testList
        "scaffold identifier leak guard (#149/#152)"
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
