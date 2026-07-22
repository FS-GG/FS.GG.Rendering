module AppRootCoverageGateTests

//#if (profile == "game")
// ================================================================================================
// The scaffold-emitted VISUAL-COVERAGE gate (FS.GG.Rendering#994, the follow-up to #989/#990).
//
// WHAT IT GUARDS. Every gameplay element your game draws must resolve to SOME visual — a shown
// symbol, or a deliberate, REASONED "hidden by a mechanic" opt-out. An element you add and forget
// to give a visual renders nothing, with no error, and only a human eyeballing a frame would catch
// it — the silent-omission defect. This gate turns that into a red build: it reads your product's
// element->visual CATALOG (`element-visuals.catalog`, beside this file) and reds the moment a
// declared element is left undisposed or opted out without a reason.
//
// THE CATALOG IS THE DECLARED ELEMENT SET. Per the #990 design, the catalog's rows ARE your
// renderable-element set — one row per element, each carrying its approved visual. Adding an
// element to your game is finished only when it has a catalog row (a `shown` token handle, or a
// reasoned `hidden` opt-out); the design loop is documented in the fs-gg-symbol-design skill, and
// the format in fs-gg-symbology. Edit the catalog, not this file, as your element set grows.
//
// RELEASE-SAFE INTAKE (the #992/#996 precedent). The framework owns the machine-readable format
// and its check — the `Catalog`/`Coverage` modules in `FS.GG.UI.Symbology`. Those modules are not
// yet in the `FsGgUiVersion` this product pins, so naming their dotted API here (or in a copyable
// skill fence) would hard-break the build against the pinned package. So this gate reads the
// catalog through a SMALL, self-contained mirror of the published TEXT FORMAT — no framework API,
// compiles against any pin. When the pin advances to a package that ships `Catalog`, swap this
// mirror for `Catalog.parse` + `Catalog.validate` (a `Covered` verdict is the same assertion); the
// catalog artifact and the intent are unchanged.
// ================================================================================================

open System
open Expecto

// The versioned header line every catalog artifact carries — the format marker, and the first
// thing the reader validates. Mirrors `FS.GG.UI.Symbology.Catalog.header`.
[<Literal>]
let private catalogHeader = "# fs-gg element-visual catalog v1"

/// One element's disposition in the catalog: a shown token (named by a stable HANDLE into your
/// symbol module, never inlined geometry) or a reasoned hidden opt-out. Mirror of
/// `Catalog.Visual` / `Coverage.Representation`.
type private Disposition =
    | Shown of handle: string
    | Hidden of reason: string

type private Row = { Element: string; Disposition: Disposition }

/// The gate's verdict over a catalog. `Covered` iff the artifact is well-formed AND every declared
/// element resolves to a shown token or a reasoned hidden opt-out — the exact condition
/// `Coverage.check` reports as `Covered`.
type private Verdict =
    /// The artifact is not a well-formed catalog: a wrong/missing header, a row with no
    /// disposition, a `shown` row with a blank handle (a shown-as-nothing row), an unknown
    /// disposition, or a duplicate element id. Carries the located reason.
    | Malformed of reason: string
    /// Well-formed, but one or more declared elements are opted out with a BLANK reason — an
    /// unreasoned opt-out, indistinguishable from forgetting the element. Carries the offending ids
    /// in declared order.
    | HasGaps of unreasoned: string list
    | Covered

/// Parse the canonical text form. Deterministic and IO-free: normalise line endings, validate the
/// header, then fold the rows, rejecting the first malformed one. Blank lines and `#` comment lines
/// after the header are ignored. A `hidden` row with a blank reason PARSES (structure is
/// well-formed) — its reason-quality is the coverage check's business, keeping FORMAT and POLICY
/// separate, exactly as the framework's `parse`/`validate` split does.
let private parse (text: string) : Result<Row list, string> =
    let lines =
        text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n') |> Array.toList

    let rec skipBlanks =
        function
        | (l: string) :: rest when String.IsNullOrWhiteSpace l -> skipBlanks rest
        | rest -> rest

    match skipBlanks lines with
    | [] -> Error(sprintf "empty catalog: expected the version header \"%s\"" catalogHeader)
    | first :: body when first.Trim() = catalogHeader ->
        let rec loop acc (seen: Set<string>) rows =
            match rows with
            | [] -> Ok(List.rev acc)
            | (line: string) :: rest ->
                if String.IsNullOrWhiteSpace line || line.TrimStart().StartsWith("#") then
                    loop acc seen rest
                else
                    let firstTab = line.IndexOf('\t')

                    if firstTab < 0 then
                        Error(sprintf "malformed row (no tab): %s" line)
                    else
                        let element = line.Substring(0, firstTab).Trim()
                        let afterElement = line.Substring(firstTab + 1)
                        let secondTab = afterElement.IndexOf('\t')

                        let disposition, payload =
                            if secondTab < 0 then
                                afterElement.Trim(), ""
                            else
                                afterElement.Substring(0, secondTab).Trim(), afterElement.Substring(secondTab + 1)

                        if element = "" then
                            Error(sprintf "malformed row (blank element id): %s" line)
                        elif seen.Contains element then
                            Error(sprintf "duplicate element id: %s" element)
                        else
                            match disposition with
                            | "shown" ->
                                let handle = payload.Trim()

                                if handle = "" then
                                    Error(
                                        sprintf
                                            "element %s is 'shown' with a blank token handle — name the approved token, or mark it 'hidden' with a reason"
                                            element
                                    )
                                else
                                    loop ({ Element = element; Disposition = Shown handle } :: acc) (seen.Add element) rest
                            | "hidden" -> loop ({ Element = element; Disposition = Hidden(payload.Trim()) } :: acc) (seen.Add element) rest
                            | other ->
                                Error(sprintf "element %s has an unknown disposition '%s' (expected 'shown' or 'hidden')" element other)

        loop [] Set.empty body
    | first :: _ -> Error(sprintf "expected the version header \"%s\", got: %s" catalogHeader first)

/// The whole gate over a catalog artifact's text — the release-safe mirror of `Catalog.validate`:
/// well-formed AND no unreasoned opt-out => `Covered`.
let private assess (text: string) : Verdict =
    match parse text with
    | Error reason -> Malformed reason
    | Ok rows ->
        let unreasoned =
            rows
            |> List.choose (fun r ->
                match r.Disposition with
                | Hidden reason when String.IsNullOrWhiteSpace reason -> Some r.Element
                | _ -> None)

        if List.isEmpty unreasoned then Covered else HasGaps unreasoned

/// The catalog artifact ships beside this test in the generated product; `__SOURCE_DIRECTORY__` is
/// this file's own directory, so the gate finds the catalog wherever the product is scaffolded.
let private catalogPath =
    System.IO.Path.Combine(__SOURCE_DIRECTORY__, "element-visuals.catalog")

[<Tests>]
let coverageGateTests =
    testList "visual-coverage-gate" [
        test "the generated product ships an element-visual catalog the gate can read" {
            Expect.isTrue (System.IO.File.Exists catalogPath) "element-visuals.catalog ships beside the coverage gate"

            match parse (System.IO.File.ReadAllText catalogPath) with
            | Ok rows -> Expect.isNonEmpty rows "the catalog declares at least one gameplay element"
            | Error reason -> failtestf "the shipped catalog is not a well-formed element-visual catalog: %s" reason
        }

        test "every declared gameplay element resolves to a shown token or a reasoned hidden opt-out (Coverage is Covered)" {
            // THE GATE. Adding a gameplay element without a visual (or an explicit, reasoned opt-out)
            // leaves an undisposed / unreasoned row here and reds this build before ship — the
            // silent-omission protection #989 targets, wired to the product's declared element set.
            match assess (System.IO.File.ReadAllText catalogPath) with
            | Covered -> ()
            | Malformed reason -> failtestf "element-visuals.catalog is malformed: %s" reason
            | HasGaps unreasoned ->
                failtestf
                    "these declared elements are opted out with no reason (name the hiding mechanic, or give them a shown token): %s"
                    (String.concat ", " unreasoned)
        }

        test "the gate has teeth — it reds on silent omission, not just passes vacuously" {
            // Guard the guard (the #111 pattern): prove each silent-omission class is actually caught,
            // so the green gate above means something. A stub that only ever returns `Covered` fails HERE.
            let bad name expectedIsCovered text =
                let covered =
                    match assess text with
                    | Covered -> true
                    | _ -> false

                Expect.equal covered expectedIsCovered name

            // a forgotten disposition: the element has a row but no token and no opt-out
            bad "an element with no disposition is a gap" false (catalogHeader + "\nGhost\n")
            // an unreasoned opt-out: hidden, but the mechanic is not named
            bad "an unreasoned (blank-reason) hidden opt-out is a gap" false (catalogHeader + "\nGhost\thidden\t")
            // a shown-as-nothing row: shown, but no token handle
            bad "a shown row with a blank token handle is malformed" false (catalogHeader + "\nGhost\tshown\t")
            // a wrong header is not this format at all
            bad "a catalog with the wrong header is malformed" false "not a catalog\nGhost\tshown\ttoken/x"
            // a duplicate element id is malformed
            bad "a duplicate element id is malformed" false (catalogHeader + "\nGhost\tshown\ttoken/x\nGhost\tshown\ttoken/y")
            // and the covered case genuinely passes: a shown token and a reasoned opt-out
            bad "a shown token plus a reasoned opt-out is covered" true (catalogHeader + "\nDoor\tshown\ttoken/door\nFog\thidden\tstealth: cloaked until it moves")
        }
    ]
//#endif
