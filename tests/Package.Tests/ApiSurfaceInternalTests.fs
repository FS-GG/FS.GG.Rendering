module ApiSurfaceInternalTests

// FS-GG/FS.GG.Rendering#585 (criterion 3 of #507, split out).
//
// S-INT — the bundled `template/base/docs/api-surface/**` tree ships the PRODUCT-VISIBLE surface
// only. No `val internal`, no `module internal`.
//
// WHY. The mirror is the contract surface a generated product actually reads: it is copied into the
// scaffold, and an author opens it to find out what they may call. Ten `val internal` declarations
// (plus two `module internal`) shipped into it — `routeRetainedInteraction`, `resolveFocus`,
// `applyRuntimeVisualState`, `resolvedLabel`, `Coalescing`, `ControlInternals`, … — documented at
// length, with doc comments explaining their invariants, in a file the author reads from their OWN
// assembly. Where they do not exist.
//
// The `internal` keyword cannot warn them, and this is the sharp edge: `internal` is relative to the
// declaring assembly, so in the READER's assembly it is not "restricted", it is simply ABSENT. The
// word carries no meaning at their call site. They get a compile error at best; at worst they spend
// an afternoon inside a seam that was never theirs, because the doc comment above it reads exactly
// like the doc comment above a public member.
//
// WHY THE MIRROR AND NOT THE SOURCE. The `src/` originals keep these declarations — they are real
// `InternalsVisibleTo` seams that Controls.Tests / Elmish.Tests reach, and an `.fsi` that simply
// omits a value makes it PRIVATE to its implementation file rather than internal to the assembly, so
// deleting them at the source would break the tests that legitimately use them. The mirror is
// therefore its original MINUS the internals — not a byte copy — and M-MIR
// (`ApiSurfaceMirrorTests` / `ApiSurfaceMirrorCoherenceTests`, `stripInternalDeclarations`) compares
// it that way.
//
// WHY NOT S-DOC. `SurfaceDocCoverageTests`' `publicValRegex` excludes `internal` ON PURPOSE, and that
// exclusion must stay. Admitting internals there would let a leak be LAUNDERED into "documented" by
// adding a ledger line, when the right answer is that it should not be in the mirror at all. The two
// rules stay separate: S-DOC measures what a product is GIVEN and never TOLD; S-INT measures what a
// product is TOLD ABOUT and can never CALL.
//
// HONESTY CAVEAT (constitution Principle V): this is a line-shape reader over signature files, not an
// F# parser. It anchors on a DECLARATION — start of line, optional indent, `val`/`module`, then
// `internal` — which is exactly what distinguishes it from the naive `grep -rc "val internal"` in
// #585's own reproduce line. That grep counts PROSE: the mirror's doc comments legitimately mention
// `module internal RetainedRender` and `module internal Reconcile` when explaining why a public
// member behaves as it does, and those mentions must survive. `readerFindsDeclarationsNotProse`
// below pins both directions, so a future tightening cannot quietly start eating doc comments — or
// quietly stop firing.

open System.IO
open Expecto
open FS.GG.TestSupport

let private apiSurfaceRoot =
    Path.Combine(RepositoryRoot.value, "template", "base", "docs", "api-surface")

/// THE SAME reader the strip uses (`SurfaceSignature.internalDeclaration`), not a second copy of it.
/// S-INT asserts the mirror contains nothing `stripInternalDeclarations` would have removed, so the two
/// must be the same rule by construction: a private regex here that drifted from the shared one would let
/// S-INT pass a declaration the strip still deletes — the gate and the transform disagreeing about what an
/// internal declaration IS, which is the one thing neither can be wrong about.
let private internalDeclaration = SurfaceSignature.internalDeclaration

let private bundledSignatures () =
    Directory.GetFiles(apiSurfaceRoot, "*.fsi", SearchOption.AllDirectories)

let private internalDeclarationsIn (file: string) =
    File.ReadAllLines file
    |> Array.indexed
    |> Array.filter (fun (_, line) -> internalDeclaration.IsMatch line)
    |> Array.map (fun (i, line) ->
        let relative = Path.GetRelativePath(apiSurfaceRoot, file).Replace('\\', '/')
        $"{relative}:{i + 1}  {line.Trim()}")

[<Tests>]
let apiSurfaceInternalTests =
    testList
        "#585 — S-INT: the bundled api-surface ships the product-visible surface only"
        [
          // The gate has a subject. A reader that globs an empty or moved tree reports green while
          // checking nothing — the fail-open shape this repo keeps rediscovering — so the subject is
          // asserted before the rule that depends on it.
          test "the bundled api-surface tree exists and has signature files to check" {
              Expect.isTrue (Directory.Exists apiSurfaceRoot) $"the bundled api-surface tree exists at {apiSurfaceRoot}"

              Expect.isGreaterThan
                  (bundledSignatures ()).Length
                  0
                  "the bundled api-surface tree contains .fsi files (an empty glob would pass S-INT vacuously)"
          }

          // S-INT itself. `type` as well as `val`/`module`: the issue counts only the latter two, but the
          // mirror was also shipping `type internal RuntimeStampResult<'msg>` — the return type OF the
          // internal functions — and a gate held to the issue's literal wording would have passed it.
          test "no internal `val`, `module` or `type` ships in the product api-surface" {
              let offenders = bundledSignatures () |> Array.collect internalDeclarationsIn

              Expect.isEmpty
                  offenders
                  "the bundled api-surface declares no internal members — a product reads this tree from its own \
                   assembly, where an `internal` member does not exist and the keyword cannot warn it. Keep the \
                   declaration in src/ (it is a real InternalsVisibleTo seam) and leave it out of the mirror; M-MIR's \
                   `stripInternalDeclarations` already expects the difference"
          }

          // The reader can fail, and fails on the right thing. Without this, S-INT is a regex nobody
          // has ever seen go red, and its two failure modes are silent: too loose and it eats the doc
          // comments that explain the public surface; too tight and it stops seeing the leak it exists
          // to catch.
          test "the reader matches declarations and not the doc comments that mention them" {
              let declarations =
                  [ "val internal resolvedLabel: token: Token -> LabelText option"
                    "    val internal applyRuntimeVisualState: model: ControlRuntimeModel -> Control<'msg>"
                    "module internal ControlInternals ="
                    "    module internal Coalescing ="
                    // `type` is not decoration: `type internal RuntimeStampResult<'msg>` shipped in the
                    // mirror as the return type of three `val internal` functions. Strip only the functions
                    // and the type is left ORPHANED — declared, unreferenced, and still uncallable — while a
                    // gate written to the issue's literal "10 `val internal`" reports green over it.
                    "type internal RuntimeStampResult<'msg> ="
                    "    type internal Cache" ]

              for line in declarations do
                  Expect.isTrue (internalDeclaration.IsMatch line) $"S-INT sees the declaration: {line}"

              let prose =
                  [ "/// entry (mirrors `module internal Reconcile`); reached from `Controls.Tests` via"
                    "    /// retained path (`module internal RetainedRender`) measures with the IDENTICAL function."
                    "/// `internal` for the same reason as `resolvedLabel`: the emitters cap at this, and"
                    "    /// the next tree against a retained previous tree (`module internal RetainedRender`). So" ]

              for line in prose do
                  Expect.isFalse (internalDeclaration.IsMatch line) $"S-INT does not eat the doc comment: {line}"
          }

          // The TRANSFORM, not just the regex. `stripInternalDeclarations` is what three mirror gates
          // compare against (Package.Tests, Rendering.Harness.Tests, Symbology.Tests), so the mirror on
          // disk is only as correct as this is. It is also subtle — it measures a declaration's INDENT to
          // decide how much of what follows belongs to it — and its failures are quiet: take the indent
          // from the wrong regex group and it silently swallows the next public member, or stops at the
          // first continuation line and leaves half a signature behind. Pinned directly, on the two shapes
          // that are hard: a multi-line `val internal` signature, and a `module internal` with a body.
          test "stripInternalDeclarations removes the declaration, its doc comment, its continuations and its body" {
              let source =
                  [| "module Widget ="
                     "    /// Public and stays."
                     "    val render: control: Control -> Scene"
                     ""
                     "    /// Internal, with a continuation-line signature."
                     "    /// Reached from Controls.Tests via InternalsVisibleTo."
                     "    val internal routeFocusedKey:"
                     "        retained: RetainedRender<'msg> ->"
                     "        key: Key ->"
                     "            RetainedRender<'msg> * 'msg list"
                     ""
                     "    /// Mentions `module internal Reconcile` in prose — must survive."
                     "    val hitTest: x: float -> y: float -> ControlId option"
                     ""
                     "/// A whole internal module, body and all."
                     "module internal ControlInternals ="
                     "    /// Internal by enclosure — note NO `internal` keyword of its own."
                     "    val chartValues: control: Control -> ChartPoint list"
                     ""
                     "    val setMeasureTextHook: hook: (string -> TextMetrics) option -> unit"
                     ""
                     "/// Public and stays, after the internal module."
                     "module Trailing ="
                     "    val ofMessage: msg: 'msg -> Command<'msg>" |]

              let expected =
                  [| "module Widget ="
                     "    /// Public and stays."
                     "    val render: control: Control -> Scene"
                     ""
                     "    /// Mentions `module internal Reconcile` in prose — must survive."
                     "    val hitTest: x: float -> y: float -> ControlId option"
                     ""
                     "/// Public and stays, after the internal module."
                     "module Trailing ="
                     "    val ofMessage: msg: 'msg -> Command<'msg>" |]

              Expect.equal
                  (SurfaceSignature.stripInternalDeclarations source)
                  expected
                  "the internal `val` (with its continuations) and the internal `module` (with its whole body, \
                   including the members that carry no `internal` keyword of their own) are gone; every public \
                   member, and the doc comment that merely MENTIONS an internal module, survives"
          }

          // An internal TYPE is the same defect, and the one the issue's own wording would have missed:
          // `type internal RuntimeStampResult<'msg>` shipped as the RETURN type of the three `val internal`
          // functions in ControlRuntime.fsi. A strip that took only #585's literal "`val internal` /
          // `module internal`" would delete the functions and leave the type behind — declared in the
          // mirror, referenced by nothing in it, and still uncallable by the product reading it.
          test "stripInternalDeclarations removes an internal type, with its whole body" {
              let source =
                  [| "namespace FS.GG.UI.Controls"
                     ""
                     "/// Internal: the result of a targeted runtime stamp."
                     "type internal RuntimeStampResult<'msg> ="
                     "    { Stamped: Control<'msg>"
                     "      Touched: int }"
                     ""
                     "/// Public and stays."
                     "type ControlRuntimeModel ="
                     "    { Hovered: ControlId option }" |]

              let expected =
                  [| "namespace FS.GG.UI.Controls"
                     ""
                     "/// Public and stays."
                     "type ControlRuntimeModel ="
                     "    { Hovered: ControlId option }" |]

              Expect.equal
                  (SurfaceSignature.stripInternalDeclarations source)
                  expected
                  "an internal type goes with its record/DU body; the public type beside it is untouched"
          }

          // The transform must be the IDENTITY on a signature with no internals. The Symbology gate strips
          // Legibility.fsi and Render.fsi — which declare none — and compares the result against a BYTE
          // copy. A strip that "tidied" anything on that path (a trailing blank line, say) would report a
          // faithful mirror as drifted, and the message would send the next reader hunting a drift that
          // does not exist.
          test "stripInternalDeclarations leaves a signature with no internals exactly as it found it" {
              let source =
                  [| "namespace FS.GG.UI.Symbology"
                     ""
                     "module Legibility ="
                     "    /// Score a token's label against the grammar that draws it."
                     "    val scoreIn: grammar: Grammar -> token: Token -> Score"
                     ""
                     "" |]

              Expect.equal
                  (SurfaceSignature.stripInternalDeclarations source)
                  source
                  "no internal declaration, no change — not even to the trailing blank lines, which the \
                   byte-for-byte Symbology mirror comparison would otherwise report as drift"
          }
        ]
