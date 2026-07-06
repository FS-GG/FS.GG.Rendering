module ScaffoldIdentifierLeakGuardTests

// Cross-repo FS-GG/FS.GG.Rendering#149 (from FS.GG.SDD; blocks FS.GG.SDD#150, epic #148).
//
// The template imprints the product name into two token classes: the capitalized `Product`
// (-> effectiveName) and the lowercased `product` (-> effectiveNameLower). Both carry the RAW
// name, which for a legal-but-hyphenated product name (e.g. `Roquelike-DungeonCrawler`) contains
// a hyphen. A hyphen is fine in a string/path/comment but ILLEGAL in an F# identifier — so any
// `product`/`Product` token that lands in an *identifier* position (a `let`/`type`/`module`/DU/
// member declaration name) becomes uncompilable (`error FS0010: Unexpected symbol '-'`).
//
// The #142 identifier split correctly routed the module/type/`open` positions through the derived
// valid namespace (`AppRoot` -> effectiveIdentifier), but shipped WITHOUT a repeatable gate, so a
// residual `let productDefectMessage` binding in EvidenceCommands.fs slipped through and was caught
// only downstream by SDD's composition smoke. This is that missing gate, pulled local.
//
// HONESTY CAVEAT (constitution Principle V): this is a static, declaration-anchored regex scan, not
// an F# parse. It proves ONE bounded thing precisely — no `product`/`Product` substitution token
// begins a `let`/`and`/`type`/`module`/DU-case/`member` declaration name in the compiled,
// substitution-subject scaffold sources. It cannot see mid-line bindings or exotic declaration
// forms; those remain covered by the env-gated live build (Feature217) and SDD#150's smoke. It is
// deliberately cheap and always-on so the common case (a new `let {slug}Foo` binding) fails HERE,
// at template-author time, instead of downstream.

open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

// The compiled, non-copyOnly scaffold sources: `template/base/src` plus the game/sample-pack
// capability fragments under `template/fragments/*/src`. These are the trees the template engine
// rewrites `Product`/`product` in (the copyOnly `docs/**` reference trees are exempt from
// substitution, so they carry no hyphen risk and are intentionally out of scope).
let private scaffoldSourceRoots =
    [ "template/base/src" ]
    @ (let fragments = repositoryPath "template/fragments"
       if Directory.Exists fragments then
           Directory.GetDirectories(fragments)
           |> Array.map (fun d -> Path.Combine(d, "src"))
           |> Array.filter Directory.Exists
           |> Array.map (fun d -> Path.GetRelativePath(repositoryRoot, d).Replace('\\', '/'))
           |> Array.toList
       else [])

let private scaffoldSourceFiles =
    scaffoldSourceRoots
    |> List.collect (fun root ->
        let full = repositoryPath root
        if Directory.Exists full then
            Directory.GetFiles(full, "*.fs", SearchOption.AllDirectories)
            |> Array.filter (fun p ->
                let n = p.Replace('\\', '/')
                not (n.Contains "/obj/") && not (n.Contains "/bin/"))
            |> Array.toList
        else [])

// Declaration-anchored patterns: a `product`/`Product` token that STARTS an F# identifier in a
// binding/type/module/DU-case/member declaration position. Anchored to `^\s*<keyword>` so that
// string literals ("product-defect", "Generated Product") and comments ("production", "cross
// products") — which never begin a line with a declaration keyword — do not match.
let private identifierLeakPatterns =
    [ "let/and binding",
      Regex(@"^\s*(let|and)\s+(mutable\s+|rec\s+|inline\s+|private\s+|internal\s+|public\s+)*[Pp]roduct[A-Za-z0-9_']", RegexOptions.Compiled)
      "type/module declaration",
      Regex(@"^\s*(type|module)\s+[Pp]roduct[A-Za-z0-9_']", RegexOptions.Compiled)
      "DU case declaration",
      Regex(@"^\s*\|\s*[Pp]roduct[A-Za-z0-9_']", RegexOptions.Compiled)
      "member declaration",
      Regex(@"^\s*member\s+[^=\n]*?\.?[Pp]roduct[A-Za-z0-9_']", RegexOptions.Compiled) ]

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
        "scaffold identifier leak guard (#149)"
        [ test "no product-name substitution token begins an F# declaration identifier in the scaffold sources" {
              // A hit here means a hyphenated product name (a legal name, illegal identifier) will not
              // compile — route the identifier through the derived valid namespace (`approot` ->
              // effectiveIdentifierLower) and keep the raw slug only in string/path contexts.
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
          }

          test "the scanner detects a synthetic identifier leak (not narrowed shut)" {
              // Prove the patterns fire: each synthetic declaration must be flagged, and a string /
              // comment carrying the same token must NOT be (the string/path context is legitimate).
              let mustFlag =
                  [ "    let productDefectMessage = liveClassMessage \"product-defect\""
                    "type ProductState = { Tick: int }"
                    "    | ProductDefect"
                    "    member _.productLabel = \"x\"" ]
              let mustPass =
                  [ "    let defectMessage = liveClassMessage \"product-defect\""
                    "        Authority = \"product-owned interactive launch\""
                    "// production tree-render path is the point of this comment"
                    "    let approotDefectMessage = liveClassMessage \"product-defect\"" ]
              mustFlag
              |> List.iter (fun l ->
                  Expect.isNonEmpty (scanLine l) (sprintf "synthetic leak not detected: %s" l))
              mustPass
              |> List.iter (fun l ->
                  Expect.isEmpty (scanLine l) (sprintf "false positive on legitimate line: %s" l))
          }
        ]
