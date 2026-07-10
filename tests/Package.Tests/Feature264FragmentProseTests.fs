module Feature264FragmentProseTests

// FS-GG/FS.GG.Rendering#264 (from FS.GG.Game; evidence from a real scaffolded product on disk).
//
// The scaffold sources under `template/base/{src,tests}` and `template/fragments/*/{src,tests}` ship
// WITHOUT `copyOnly`, so the template engine rewrites both substitution tokens in them: `Product` ->
// effectiveName and `product` -> effectiveNameLower. That rewrite is *unscoped* — it does not know a
// comment from a namespace — and it is what makes the fragments' `namespace`/`Product-owned` headers
// carry the scaffolded name. That is intended.
//
// What is NOT intended is that the same rewrite fires on the English word `product` when it appears
// inside a MATHEMATICAL TERM OF ART. In a product named `Breakout1`, the fragments' determinism
// contracts came out as:
//
//     /// ...endpoints are ordered by a cross-breakout1 angular
//     // ...computed from cross breakout1s only (no `atan2`):
//     // Deltas and the tiebreak cross-breakout1 are `int64` for ...
//
// Those comments are the ONLY place the determinism guarantee of `Visibility.polygon` and
// `LineDrawing.supercover` is written down ("ordered by a cross-product angular comparator (NO
// `atan2`) with an integer-index tiebreak, so identical inputs yield byte-identical output"). After
// substitution the sentence is gibberish, and the next maintainer of a scaffolded product — whom the
// fragments explicitly tell "THIS FILE IS YOURS TO ADAPT" — has no way to learn why `atan2` is
// forbidden. It also scales badly: every product name mangles differently, so no two scaffolds agree
// and a diff against upstream is noise.
//
// WHY THIS GATE BANS THE TERM OF ART AND NOT THE WORD. `product` is also used, deliberately and
// correctly, as a common noun meaning *the generated application* — `Product-owned ... YOURS TO
// ADAPT`, "relative to the running product", "the ONE place bare `Scene` record literals appear in
// your product tree". Substituting the scaffolded name into THOSE reads naturally ("your Breakout1
// tree") and is the point of the non-`copyOnly` rewrite; `template/base` alone carries ~21 of them.
// A blanket "no `product` token in any comment" rule would condemn all of that intended prose. The
// defect is narrower and sharper: a term of art whose meaning lives in the word itself, where the
// substitution silently destroys meaning rather than carrying it. So the gate bans exactly the
// mathematical compounds (`cross product`, `dot product`, ...) and leaves the common noun alone.
//
// The fix in the fragments is the cheap correct one from the issue's option (1): reword to `perp-dot`
// (the standard name for the 2-D cross), which carries no substitution token. Option (2) — scoping
// the substitution to a distinct `__ProductNamespace__`-style symbol — remains the durable fix and
// composes with this; it is a template-machinery change and out of scope here.
//
// HONESTY CAVEAT (constitution Principle V): this is a static regex scan over the substitution-subject
// scaffold sources, not a `dotnet new` instantiation. It proves ONE bounded thing precisely — no
// mathematical `<x> product` compound survives in the trees the engine rewrites, so none can be
// mangled. It does NOT prove the scaffolded output is otherwise byte-identical to upstream prose (the
// env-gated `scripts/validate-productname-template.fsx` byte-diff covers that), and it cannot catch a
// future term of art outside `mathProductCompounds` (e.g. a newly coined "Hadamard product") until
// that term is added below. It is deliberately cheap and always-on so the common case fails HERE, at
// template-author time, instead of in a user's scaffolded game.

open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

// The same substitution-subject trees `ScaffoldIdentifierLeakGuardTests` scans, and for the same
// reason: these are the sources the template engine rewrites `Product`/`product` in. The `copyOnly`
// trees (the product-skill `SKILL.md` bodies, the `docs/**` reference trees) are exempt from
// substitution, so their prose is safe by construction and is intentionally out of scope — which is
// why `template/product-skills/fs-gg-visibility/SKILL.md` may keep saying "cross-product".
let private scaffoldSourceRoots =
    [ "template/base/src"; "template/base/tests" ]
    @ (let fragments = repositoryPath "template/fragments"

       if Directory.Exists fragments then
           Directory.GetDirectories fragments
           |> Array.collect (fun d -> [| Path.Combine(d, "src"); Path.Combine(d, "tests") |])
           |> Array.filter Directory.Exists
           |> Array.map (fun d -> Path.GetRelativePath(repositoryRoot, d).Replace('\\', '/'))
           |> Array.toList
       else
           [])

let private scaffoldSourceFiles =
    scaffoldSourceRoots
    |> List.collect (fun root ->
        let full = repositoryPath root

        if Directory.Exists full then
            Directory.GetFiles(full, "*.fs", SearchOption.AllDirectories)
            |> Array.append (Directory.GetFiles(full, "*.fsi", SearchOption.AllDirectories))
            |> Array.filter (fun p ->
                let n = p.Replace('\\', '/')
                not (n.Contains "/obj/") && not (n.Contains "/bin/"))
            |> Array.toList
        else
            [])

/// The mathematical compounds whose meaning lives in the word `product` itself. Hyphen, space, or a
/// plural `s` all mangle identically (`cross-breakout1`, `cross breakout1s`), so the pattern accepts
/// any of them. `\b` on the left keeps this off unrelated words; the common noun `product` standing
/// alone never matches, because every alternative requires a qualifier immediately before it.
let private mathProductCompound =
    Regex(@"\b(cross|dot|scalar|vector|inner|outer|triple)[- ]products?\b", RegexOptions.IgnoreCase ||| RegexOptions.Compiled)

/// The comment text on a line, or `""` if the line carries none. Both `//` and `///` start with `//`.
/// Takes the first `//` onward, so a `product` inside an identifier or an ordinary string literal
/// cannot trip this scan (those are `ScaffoldIdentifierLeakGuardTests`' business). Note the direction
/// of the imprecision, which is the opposite of that guard's `stripLineComment`: a `//` INSIDE a
/// string literal (a URL) would be read as a comment, so this can add a false POSITIVE, never a false
/// negative. That fails loudly and is trivially fixed by rewording; a false negative would ship a
/// mangled scaffold silently. Block comments `(* ... *)` are not tracked; no scaffold source uses one
/// for prose.
let private commentText (line: string) =
    match line.IndexOf "//" with
    | -1 -> ""
    | i -> line.Substring i

let private scanLine (line: string) : string list =
    let comment = commentText line

    if comment = "" then
        []
    else
        mathProductCompound.Matches comment
        |> Seq.map (fun m -> m.Value)
        |> Seq.toList

type private Finding =
    { File: string
      Line: int
      Term: string
      Text: string }

let private scanFile (path: string) : Finding list =
    let rel = Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/')

    File.ReadAllLines path
    |> Array.mapi (fun i line -> i + 1, line)
    |> Array.collect (fun (n, line) ->
        scanLine line
        |> List.map (fun term ->
            { File = rel
              Line = n
              Term = term
              Text = line.Trim() })
        |> List.toArray)
    |> Array.toList

let private findings = scaffoldSourceFiles |> List.collect scanFile

[<Tests>]
let feature264FragmentProseTests =
    testList
        "fragment prose substitution guard (#264)"
        [ test "no mathematical `<x> product` compound survives in a substitution-subject scaffold comment" {
              // A hit here means `dotnet new` will rewrite the term of art into the product's name and
              // destroy the sentence. Reword to a token-free synonym — `perp-dot` / `the 2-D cross` for
              // a cross product, `scalar dot` for a dot product.
              Expect.isEmpty
                  findings
                  (findings
                   |> List.map (fun f -> sprintf "%s:%d [%s] %s" f.File f.Line f.Term f.Text)
                   |> String.concat "\n"
                   |> sprintf
                       "a term of art containing the `product` substitution token survives in scaffold prose;\n\
                        `dotnet new` will mangle it into the product name:\n%s")
          }

          test "the scan actually enumerates the scaffold sources (must not silently narrow to zero)" {
              // Backstop: if discovery ever returns an empty file set, the guard above passes vacuously.
              Expect.isGreaterThan
                  (List.length scaffoldSourceFiles)
                  4
                  "scaffold source enumeration collapsed — the prose scan would pass vacuously"

              Expect.isTrue
                  (scaffoldSourceFiles
                   |> List.exists (fun p ->
                       p.Replace('\\', '/').EndsWith "template/fragments/visibility/src/Product/Visibility.fs"))
                  "Visibility.fs (a #264 mangle site) must be in the scanned set"

              Expect.isTrue
                  (scaffoldSourceFiles
                   |> List.exists (fun p ->
                       p.Replace('\\', '/').EndsWith "template/fragments/line-drawing/src/Product/LineDrawing.fs"))
                  "LineDrawing.fs (a #264 mangle site) must be in the scanned set"
          }

          test "the scanner detects the synthetic mangle sites and spares the intended common noun" {
              // Prove the pattern fires on the four lines the issue reported verbatim, and that it does
              // NOT fire on the deliberate common-noun prose the non-copyOnly rewrite exists to serve.
              let mustFlag =
                  [ "/// ...endpoints are ordered by a cross-product angular"
                    "    // Total rotational order of points around `source`, computed from cross products only (no `atan2`):"
                    "    // half-plane first, then cross-product sign, then squared distance, ..."
                    "        // Deltas and the tiebreak cross-product are `int64` for ..."
                    "// the dot product of the two vectors"
                    "// a Dot-Product, capitalized and hyphenated" ]

              let mustPass =
                  [ "/// Product-owned 2D-visibility helper — THIS FILE IS YOURS TO ADAPT." // the intended header
                    "/// Where `resolver` looks for PCM WAV files, relative to the running product."
                    "/// the ONE place bare `Scene` record literals appear in your product tree, where only `Scene` types"
                    "// `forTransition` is the ONLY place this product decides what to play."
                    // NOT a claim that `production` is safe — it is NOT: `replaces` is a plain substring
                    // match (the issue's capture shows `products` -> `breakout1s`), so `production` mangles
                    // to `breakout1ion`. That is a distinct defect class (token inside a word, and it also
                    // reaches string literals) tracked separately; it is simply not a `<x> product` compound,
                    // which is the only thing THIS scan claims to see.
                    "// production tree-render path (`Control.renderTree`) at the output extent"
                    "let dotProduct a b = a.X * b.X + a.Y * b.Y" // an identifier, not comment prose
                    "    let message = \"cross-product\"" // a string literal, not comment prose
                    "/// endpoints are ordered by a perp-dot (the 2-D cross) angular comparator" ] // the fix shape

              mustFlag
              |> List.iter (fun l -> Expect.isNonEmpty (scanLine l) (sprintf "synthetic mangle site not detected: %s" l))

              mustPass
              |> List.iter (fun l -> Expect.isEmpty (scanLine l) (sprintf "false positive on legitimate line: %s" l))
          }
        ]
