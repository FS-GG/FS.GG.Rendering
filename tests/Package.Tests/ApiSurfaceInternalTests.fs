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
// deleting them at the source would break the tests that legitimately use them. The mirror ships the
// product-visible surface; `src/` keeps the seams.
//
// WHY THIS SURVIVED #753, WHEN M-MIR AND THE LEDGER DID NOT. #752 made the mirror a BUILD OUTPUT
// (`scripts/refresh-api-surface-mirror.fsx`, emitted from the pinned package's `.fsi` + the curation in
// `scripts/api-surface-manifest.txt`), and #694's test retires any gate that compares a generated
// artifact against its generator's input. #753 listed S-INT for deletion on that basis. It does not
// meet the test, and deleting it would have been a fail-open:
//
//   S-INT does not compare the mirror to the generator's input. It asserts a PROPERTY of the emitted
//   tree — and THE GENERATOR DOES NOT ENFORCE THAT PROPERTY. There is no `internal` filter anywhere in
//   it. The only place the generator touches the word is `FsiSurface.nameOf`, which STRIPS the
//   accessibility keyword off a declaration to key the lookup — so a manifest line naming an internal
//   member RESOLVES to it and `render` then emits `node.Text` verbatim, `internal` keyword and all.
//   `FsiSurface.fsx` even states the invariant it never checks: "`module internal` matters — the pin
//   ships three, they are NOT public surface, and a mirror must never declare one."
//
// So S-INT's subject did not disappear with the hand-maintained tree — it MOVED. It used to guard a
// hand-copied mirror; it now guards the hand-edited MANIFEST, by reading what the manifest made the
// generator emit. The manifest is the surviving human-authored input and `internal` members are live in
// the pin's `.fsi`, so the leak #585 recorded (ten `val internal`, two `module internal`, and
// `type internal RuntimeStampResult<'msg>`) is still one manifest line away. This gate is what catches
// it. (M-MIR used to be named here alongside a PR-time twin, `ApiSurfaceMirrorCoherenceTests`; #613
// retired it — this project has been on the PR gate since #540.)
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

/// The shared reader (`SurfaceSignature.internalDeclaration`), not a second copy of it. It is the only
/// definition in the tree of what an internal DECLARATION is, and #753 left it that way deliberately: the
/// transform it used to share this module with (`stripInternalDeclarations`) went with the mirror gates
/// that were its only callers, and a private regex re-spelled here would be the fourth reader of a
/// grammar that has already cost this repo #648 and #669.
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
                   declaration in src/ (it is a real InternalsVisibleTo seam) and leave it out of the mirror. The \
                   mirror is generated (#752), so the fix is in `scripts/api-surface-manifest.txt`: drop the line \
                   naming this member. The generator has no internal filter — it will emit whatever the manifest \
                   names, `internal` keyword and all, which is why this gate and not the generator catches it"
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
        ]
