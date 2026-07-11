module FragmentProseCoherenceTests

// FS-GG/FS.GG.Rendering#366 — a PR-VISIBLE twin of the release-only fragment-prose substitution guard.
//
// WHY THIS EXISTS. The non-copyOnly scaffold sources have `Product`/`product` rewritten by the template
// engine, which happily fires on the English word `product` inside a MATHEMATICAL TERM OF ART
// (`cross product`, `dot product`, ...), turning a determinism contract into gibberish
// (`cross-breakout1`). `Feature264FragmentProseTests` (tests/Package.Tests) bans exactly those compounds
// across the substitution-subject scaffold sources. But Package.Tests is RELEASE-ONLY: it is not in
// `FS.GG.Rendering.slnx` and runs only under `dotnet test … -c Release` in release.yml. So a newly added
// `<x> product` compound in scaffold prose compiles green on a PR and only reds the release lane. That is
// the "PR-gated tests must be in the slnx" gap #350/#382 already closed for the launch host and the audio
// wiring, and the twins through #422 have been closing rule-by-rule.
//
// WHAT IT LOCKS. Feature264FragmentProseTests is already fully static and self-contained (no pack, no
// restore, no assembly load): it derives the scanned set from `ScaffoldSources.files` and regex-scans
// each line — and each adjacent PAIR, to catch a compound wrapped across two comment lines — for the
// mathematical compound, while sparing the deliberate common-noun `product`. So this hoist is a faithful
// mirror — it runs the SAME scan over the SAME derived inputs, one gate earlier.
//
// L-INPUTS EXEMPTION (SharedInputs = false). The counterpart reads its inputs through
// `ScaffoldSources.files`, not the `repositoryPath "…"` / `repo "…"` helper that
// ReleaseOnlyTwinLockstepTests's L-INPUTS keys on, so the shared-inputs invariant cannot be mechanically
// checked for this shape (a byte-faithful hoist extracts zero literal inputs and would trip L-INPUTS's
// non-empty guard). The pair is registered with SharedInputs = false — the same documented exemption the
// launch pair and the #282 embedded-token twin already carry — so L-EXISTS / L-NAMES / L-CLOSED still
// guard the pairing; only the input-set equality is waived, and byte-faithfulness (below) keeps the two
// in step in practice.
//
// Kept in deliberate lockstep with tests/Package.Tests/Feature264FragmentProseTests.fs: a verbatim hoist
// (module name, the `[<Tests>]` binding, and the testList label aside), so a real drift fails BOTH.

open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

// The same substitution-subject trees `ScaffoldIdentifierLeakGuardTests` scans, from the same shared
// list, and for the same reason: these are the sources the template engine rewrites `Product`/
// `product` in. The `copyOnly` trees (the product-skill `SKILL.md` bodies, the `docs/**` reference
// trees) are exempt from substitution, so their prose is safe by construction and is intentionally out
// of scope — which is why `template/product-skills/fs-gg-visibility/SKILL.md` may keep saying
// "cross-product".
let private scaffoldSourceFiles = ScaffoldSources.files repositoryRoot

/// The mathematical compounds whose meaning lives in the word `product` itself. A plural `s` mangles
/// just as badly (the issue's capture shows `cross products` -> `cross breakout1s`, which is also the
/// proof that `replaces` is a plain SUBSTRING match), so `products?` is accepted. The separator is
/// `[-\s]+`, not a single space: a re-justified comment can leave two spaces, and a wrapped one puts a
/// newline there (see `scanPairs`).
///
/// Scanned over the WHOLE LINE, not just its comment. Three reasons, in order of importance:
///   * the rewrite does not respect syntax — it mangles a STRING LITERAL exactly as happily as a
///     comment (`template/base/tests/Product.Tests/BehaviorTests.fs` carries the token inside a `test
///     "..."` name), and a comment-only scan is blind to that;
///   * a compound needs a hyphen or a space before `product`, and neither is legal in an F# identifier,
///     so scanning code text cannot false-positive on `dotProduct` — the identifier case is
///     `ScaffoldIdentifierLeakGuardTests`' business and stays there;
///   * it removes the "is this a comment?" question, and with it the `//`-inside-a-URL misread.
let private mathProductCompound =
    Regex(@"\b(cross|dot|scalar|vector|inner|outer|triple)[-\s]+products?\b", RegexOptions.IgnoreCase ||| RegexOptions.Compiled)

let private matchesIn (text: string) : string list =
    mathProductCompound.Matches text |> Seq.map (fun m -> m.Value) |> Seq.toList

let private scanLine (line: string) : string list = matchesIn line

/// A compound wrapped across two lines (`... a cross` / `/// product comparator ...`) is invisible to a
/// line-at-a-time scan, yet the second line's bare `product` is rewritten all the same. So also scan
/// each ADJACENT PAIR, with the continuation's indentation and `//`/`///` marker collapsed to a single
/// space so the join reads as running prose; `[-\s]+` then spans the seam. Reported against the FIRST
/// line of the pair. The doc comment this feature reworks wraps at exactly that spot, one
/// re-justification away from the gap.
let private continuationText (line: string) =
    Regex.Replace(line, @"^\s*(///?/?)?\s*", " ")

let private scanPairs (lines: string[]) : (int * string) list =
    lines
    |> Array.pairwise
    |> Array.mapi (fun i (first, second) ->
        // Only the seam is new information; a match wholly inside `first` or `second` is already
        // reported by `scanLine`, so subtract those to keep the pair scan from double-reporting.
        let joined = first + continuationText second
        let seamOnly = matchesIn joined |> List.except (matchesIn first @ matchesIn second)
        seamOnly |> List.map (fun term -> i + 1, term))
    |> Array.toList
    |> List.concat

type private Finding =
    { File: string
      Line: int
      Term: string
      Text: string }

let private scanFile (path: string) : Finding list =
    let rel = Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/')
    let lines = File.ReadAllLines path

    let finding n term =
        { File = rel
          Line = n
          Term = term
          Text = lines.[n - 1].Trim() }

    let inLine =
        lines
        |> Array.mapi (fun i line -> i + 1, line)
        |> Array.collect (fun (n, line) -> scanLine line |> List.map (finding n) |> List.toArray)
        |> Array.toList

    let acrossWrap = scanPairs lines |> List.map (fun (n, term) -> finding n term)

    inLine @ acrossWrap

let private findings = scaffoldSourceFiles |> List.collect scanFile

[<Tests>]
let feature264FragmentProseCoherenceTests =
    testList
        "#366 twin — fragment prose substitution guard (#264)"
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
                    "// a Dot-Product, capitalized and hyphenated"
                    "// a cross  product with two spaces, as a re-justified comment leaves"
                    "    let message = \"cross-product\"" ] // a STRING LITERAL mangles exactly as a comment does

              let mustPass =
                  [ "/// Product-owned 2D-visibility helper — THIS FILE IS YOURS TO ADAPT." // the intended header
                    "/// Where `resolver` looks for PCM WAV files, relative to the running product."
                    "/// the ONE place bare `Scene` record literals appear in your product tree, where only `Scene` types"
                    "// `forTransition` is the ONLY place this product decides what to play."
                    // NOT a claim that `production` is safe — it is NOT (see the caveat above); it is
                    // simply not a `<x> product` compound, which is all THIS scan claims to see.
                    "// production tree-render path (`Control.renderTree`) at the output extent"
                    // No separator, so no match: a hyphen/space cannot occur in an F# identifier, which is
                    // exactly why scanning code text alongside comments costs no false positives here.
                    "let dotProduct a b = a.X * b.X + a.Y * b.Y"
                    "/// endpoints are ordered by a perp-dot (the 2-D cross) angular comparator" ] // the fix shape

              mustFlag
              |> List.iter (fun l -> Expect.isNonEmpty (scanLine l) (sprintf "synthetic mangle site not detected: %s" l))

              mustPass
              |> List.iter (fun l -> Expect.isEmpty (scanLine l) (sprintf "false positive on legitimate line: %s" l))
          }

          test "the scanner detects a compound wrapped across two comment lines" {
              // The line-at-a-time scan cannot see this; `scanPairs` is why it is caught. The seam match
              // is attributed to the FIRST line, and a compound lying wholly within one line is not
              // double-reported by the pair scan.
              let wrapped =
                  [| "/// Everything here is deterministic: endpoints are ordered by a cross"
                     "/// product angular comparator (NO `atan2`) with an integer-index tiebreak." |]

              Expect.isEmpty (scanLine wrapped.[0]) "precondition: neither line matches on its own"
              Expect.isEmpty (scanLine wrapped.[1]) "precondition: neither line matches on its own"

              Expect.equal (scanPairs wrapped) [ 1, "cross product" ] "the wrapped compound must be caught, at line 1"

              let notWrapped =
                  [| "// the tiebreak cross-product is `int64`"; "// and the deltas are too." |]

              Expect.isEmpty
                  (scanPairs notWrapped)
                  "a compound wholly inside one line is reported by scanLine, and must not be double-reported here"
          }
        ]
