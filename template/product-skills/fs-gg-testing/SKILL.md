---
name: fs-gg-testing
description: Test a generated FS.GG.UI product — assert generated-product expectations and evidence, and test that the UI actually responds (drive interaction headlessly through the real route, and guard clicks with BoundIds so a silent no-op cannot pass green).
---

# Testing Capability

## Scope

Use this skill for product test and evidence helpers: declaring
generated-product expectations, classifying local package drift, and building
evidence reports from pure inputs.

If your product has a UI, read **Test that your UI actually responds** first — it
is the test a product suite is most likely missing.

## Test that your UI actually responds

A UI suite that only exercises `update` proves the part that was never in doubt.
It never asks the question that actually breaks: **when the user clicks this
control, does anything happen?** A control can render perfectly and be wired to
nothing, and no amount of `update` testing will notice. This has shipped silently
more than once.

Two facts to test on, both of which your product can reach today:

- **Drive the real route, don't simulate it.** `ControlsElmish.Perf.runScriptToModel`
  folds an ordered script of clicks / keys / scrolls / ticks through the REAL
  retained pointer route and returns the **final model** — pure, headless, no GL,
  no window, deterministic. Assert on the model the interaction produced, not on a
  message you dispatched by hand.
- **An unbound click is silent, so guard it.** `ControlRenderResult.BoundIds` is
  the set of ids that actually carry an event binding. A click at an id that is
  *not* in it dispatches nothing and raises nothing. So a typo'd `ControlId` in a
  test drives *nothing* — and if the assertion is negative ("the screen did not
  change", "no error appeared") the test **passes**. An entire headless UI suite
  can be green and pressing nothing. Assert `Set.contains id result.BoundIds`
  before you drive a click, and build it into any click helper you write so it
  cannot be forgotten per-test.

**The runnable recipe — locate → guard → drive → assert, plus capturing the
post-interaction frame — is in [[fs-gg-elmish]].** It lives there because
`Perf` and `BoundIds` come from the Controls packages, which only the
control-bearing profiles (`app`, `sample-pack`, `game`) ship; a `headless-scene`
or `governed` product has no controls to click, and this skill must not tell it to
open a package it was never given.

**A generated shell game must prove the host it actually launches.** Its default is the
pointer-aware `interactiveHost`, not the retained keyboard-only `generatedHost` used by
some evidence commands. At the exact `ViewerOptions.InitialSize`, require a
`captureRespondsProof` verdict of `Responsive` for one menu button and one rebind row,
then complete capture through `interactiveHost.MapKey`. Also drive the same native key
seam with down → fixed tick → fixed tick → up → fixed tick; gameplay must advance on both
held ticks and stop on the final one. **Pure-`update` acceptance is necessary but not
sufficient**: direct `Msg` injection cannot see a dropped key-up, a one-shot translation,
the wrong launch overload, or a pointer/layout coordinate mismatch.

For a profile that genuinely launches the keyboard scene host, retain the existing
`GeneratedAppHost.runKeyScriptToModel` plus `auditKeyWiring` / `reachableMessages` pairing.
The rule is one invariant at the production host boundary, not one favorite driver.

## Assert at the sink, not at the model

The same shape one level out, and it is the only test that catches this class.

An **effect seam** — `AudioCues.forTransition` is the one your product ships — turns
a model transition into effects that go to a **sink** (the mixer, the file, the
device). The model is the *input* to that decision, never the evidence that it was
made. So a test that asserts on the model **cannot see whether the effect ever
left the building**, and it passes just as happily when it did not.

<!-- skill-refs: closed-ok FS.GG.Rendering#458 — cited as the issue where this bug SHIPPED, not as somewhere to go. Closed is correct; it stays closed. -->
The bite (issue FS.GG.Rendering#458, and it shipped): `forTransition` is a function of a
**transition**, and *loaded* state does not make one. Restore the player's saved
volume in `initialModel` and the model is correct, the setting genuinely **is**
loaded — and the mixer is never told. Nothing catches it. No type is wrong. A test
that asserts on the model passes, because from inside the model a restored volume
the mixer never heard is **indistinguishable** from one that was applied. It
surfaces to a player as *"turn the music down, restart, and get full-volume music
from a settings screen that correctly reports it as quiet."*

The scaffold closes the hole with a `Started` message the host dispatches as
`AudioCues.forTransition Started m m`, so the initial model still crosses the seam.
Your test's job is to prove the batch **arrived**:

```fsharp
// Assert on what was REQUESTED — no window, no device, no GL.
GeneratedAppHost.dispatchKey host keyEvent model
|> snd
|> GeneratedAppHost.audioRequests   // ViewerEffect list -> AudioEffect list, in dispatch order
|> Audio.interpret                  // AudioEvidence
```

Two rules that follow, and they generalise past audio:

- **Assert at the sink.** `audioRequests` flattens a frame's batches, so the
  question "did this interaction ask for a sound?" is answerable purely, headlessly
  and deterministically. Ask *that*, not "is the model's `Volume` field 0.3".
- **Cover the state you LOAD, not just the state you transition into.** Every seam
  of this shape has an initial-state blind spot, because initialisation is not a
  transition. Save/load is the worst case — loaded state is the entire point — so a
  product with persistence should have a test that boots from a restored model and
  asserts the sink heard about it.

If you write your own cue seam by analogy (a `SaveCues.forTransition`, say), you
inherit this blind spot along with the pattern. Give it a `Started` too, and test it
here.

## Seeded generation — pin it byte-for-byte, and prove the streams are independent

This one is for products that generate content from a seed — the `game` and
`sample-pack` profiles that carry the `Rng` value type ([[fs-gg-game:fs-gg-game-core]]); a
`headless-scene` or `governed` product with no seeded generator can skip it. Your
product already `open`s `FS.GG.Game.Core`, so the snippets below use `Rng` as an
in-scope symbol rather than repeating the open.

Seeded generation makes two promises the type system does **not** keep for you, and a
suite that spot-checks a field or two proves neither:

1. **Determinism** — the same seed produces the same artifact, every run, every
   machine.
2. **Stream-independence** — an artifact built from one RNG stream does not shift
   because an *unrelated* stream advanced. `Rng.split` ([[fs-gg-game:fs-gg-game-core]]) hands
   you decorrelated sub-streams precisely so loot draws cannot perturb the floor;
   the test is what proves you wired them that way and not by accident onto one
   shared generator.

Both break silently. A generator that reaches for `System.Random`, the wall clock,
or dictionary/`HashSet` iteration order still returns a plausible artifact and
passes every structural spot-check — it just returns a *different* plausible one
next run. And a stream you meant to keep separate but threaded back into the wrong
place stays invisible until something draws from the other stream a different number
of times. Neither is a type error; both ship.

Assert on the **serialized fixture**, not the live value. You already own a pure
`serialize` for save slots ([[fs-gg-game:fs-gg-persistence]]) — reuse it. Byte-identity on the
serialized form is the strongest cheap determinism check there is (it catches drift
a field-by-field `Expect.equal` misses), it produces a diffable artifact in a
failing test, and it is the *exact* bytes a save would carry.

```fsharp
// The product owns `serialize : Floor -> string` (see [[fs-gg-game:fs-gg-persistence]]); `Rng`
// comes from the product's own `FS.GG.Game.Core` import, and `test`/`Expect` from
// Expecto.
// A generator that draws from TWO streams: `layout` builds the floor, a split-off
// `drops` stream rolls `lootCount` items. `split` keeps them decorrelated — never
// build both from one Rng, and never reuse the input generator (thread the advanced
// one back). `lootCount` is the knob the second test turns to vary the OTHER stream.
let generate (seed: uint64) (lootCount: int) : Floor * Item list =
    let struct (layoutRng, dropRng) = Rng.split (Rng.ofSeed seed)
    buildFloor layoutRng, rollLoot lootCount dropRng   // layout | drops — disjoint streams

[<Tests>]
let seededGeneration =
    testList "seeded floor generation" [

        // 1. Determinism: a fixed seed reproduces the fixture byte-for-byte.
        test "same seed reproduces the fixture byte-for-byte" {
            let a = serialize (generate 42UL 5)
            let b = serialize (generate 42UL 5)
            Expect.equal b a "a fixed seed must reproduce the identical serialized output"
        }

        // 2. Stream-independence: vary how much the DROP stream draws, and the FLOOR
        //    must not move. This fails loudly if layout and drops share one generator.
        test "the floor is unaffected by how many loot items are rolled" {
            let floorOf lootCount = serialize (fst (generate 42UL lootCount))
            Expect.equal (floorOf 50) (floorOf 0)
                "the floor must not depend on how far the drop stream has advanced"
        }
    ]
```

The second test is the one that earns its keep: property 1 alone passes even when
`layout` and `drops` are secretly the same stream, because nothing varies between
the two runs. Only by advancing the unrelated stream — here, rolling more loot — and
re-asserting the floor is unchanged do you prove the `split` actually isolated them.
The shape is generic — serialize-and-compare, then vary-the-other-stream-and-re-assert
— so it pins every later generator (enemy population, determinism audits); write it
once here.

## Assert on a repo document — locate it CWD-independently, then prove the guard can redden

Some of the most valuable tests a product suite carries assert on a **checked-in
document**, not on code: a design doc's milestone table that must not drift from
merged reality, a coverage manifest that must list every host-shell surface, a
roadmap `§`-status line that a test reddens the moment it disagrees with what
actually shipped. The document becomes evidence *because* a test refuses to let it
lie.

Every product that reaches for this reinvents the same three parts, and gets the
same two of them subtly wrong. Write them once, from here.

- **Locate the doc by walking up from `AppContext.BaseDirectory`, never from the
  working directory.** `File.ReadAllText "docs/roadmap.md"` resolves against the
  *current directory*, and a test has no say in what that is — `dotnet test`, the
  IDE runner, and CI each start the process somewhere different, so a CWD-relative
  path is a guess that passes on your machine and reds in CI (or worse, reads a
  *different* repo's file and passes for the wrong reason). `AppContext.BaseDirectory`
  is the `bin/` folder the test assembly runs from, and its distance to the repo
  root is fixed by the build layout rather than by who launched the run. Walk
  *parents* from there until one contains the doc. The framework's own repo-root
  finder is `tests/`-internal and not product-reachable (FS.GG.Rendering#725), and
  a marker-file walk is accepted hand-rolled hygiene — so this helper is yours to
  own, not a package to import.
  <!-- skill-refs: closed-ok FS.GG.Rendering#725 — cited as the ruling that made this a hand-rolled walk, not somewhere to go. Closed is correct; it stays closed. -->

  ```fsharp
  open System
  open System.IO

  // From the test assembly's own location, walk PARENTS until one contains the doc.
  // CWD-independent by construction: it never consults the working directory.
  let findDocUp (relativePath: string) : string =
      let rec up (dir: DirectoryInfo) =
          match dir with
          | null ->
              failwithf "could not locate %s in any ancestor of %s"
                  relativePath AppContext.BaseDirectory
          | d ->
              let candidate = Path.Combine(d.FullName, relativePath)
              if File.Exists candidate then candidate else up d.Parent
      up (DirectoryInfo AppContext.BaseDirectory)
  ```

- **Split by heading, so an assertion names one section.** A whole-file
  `Contains`/`Regex.IsMatch` reddens when an unrelated paragraph three headings away
  is reworded — a false red that trains the next reader to ignore the guard. Parse
  the document into `heading -> body` once and assert against the *section* whose
  fact you are pinning; then the test fails only when the thing it names actually
  moves.

  ```fsharp
  // markdown -> (heading text -> the lines beneath it), keyed by heading.
  let sectionsByHeading (markdown: string) : Map<string, string> =
      let step (map: Map<string, string>, head) (line: string) =
          if line.StartsWith "#" then
              let h = line.TrimStart('#').Trim()
              Map.add h "" map, h
          elif head = "" then map, head
          else Map.add head (map.[head] + line + "\n") map, head
      markdown.Replace("\r\n", "\n").Split('\n')
      |> Array.fold step (Map.empty, "")
      |> fst
  ```

- **Prove the guard can go red — and make its message name the offender — before you
  trust a green.** A doc-invariant test is a *negative* test: it passes by finding no
  disagreement, which is exactly the shape [Test that your UI actually
  responds](#test-that-your-ui-actually-responds) warns about — a guard wired to
  nothing passes just as green as a guard that works. So do not trust the green until
  you have watched the red. **Revert one asserted fact in the doc, run the guard,
  confirm it reddens *and names which item drifted*, then restore and confirm green.**
  A guard whose failure says only "the doc changed" points at nothing; a guard whose
  failure names the row — "§16 lists the Scene-host driver as `pending`, but it
  merged" — points at the fix.

  Better still, bake that proof into the suite so it cannot rot: hand the guard a
  deliberately-mutated *copy* of the parsed doc and assert it reddens, alongside the
  real-doc case that must green. Then the fail-red demonstration is a committed test,
  not a one-time manual dance you have to remember to redo.

  ```fsharp
  // The invariant, as a pure function of the doc text so both cases can drive it.
  // Pins ONE row of the roadmap table: the Scene-host driver milestone, status column.
  let drift (docText: string) : string option =
      let sections = sectionsByHeading docText
      match Map.tryFind "16. Roadmap status" sections with
      | Some body when body.Contains "Scene-host driver" && body.Contains "| merged |" -> None
      | Some _ -> Some "§16 no longer records the Scene-host driver milestone as merged"
      | None -> Some "§16 (Roadmap status) is missing from the doc"

  [<Tests>]
  let roadmapGuard =
      testList "roadmap doc invariant" [
          test "the real doc records merged reality" {
              let doc = File.ReadAllText (findDocUp "docs/design.md")
              Expect.isNone (drift doc) "the roadmap section must match merged reality"
          }
          // The fail-red proof, permanent: a mutated copy MUST redden. If this ever
          // greens, the guard has stopped biting and the test above is worthless.
          test "the guard reddens on a doc that lies" {
              let lying = File.ReadAllText (findDocUp "docs/design.md")
                          |> fun d -> d.Replace("| merged |", "| pending |")
              Expect.isSome (drift lying) "a milestone recorded as pending must fail the guard"
          }
      ]
  ```

## Public Contract

The signatures you consume are bundled with this product at
`docs/api-surface/Testing/Testing.fsi`. The helper modules
(`GeneratedProductAssertions`, `LocalConsumerPackages`, `EvidenceReports`) are
pure functions over value records.

## Usage

```fsharp
open FS.GG.UI.Testing

// Declare what this product expects of its own generated output.
let expectation =
    { Profile = "game" // your product's own profile
      // <YourProduct> = this product's name (its src/ project directory).
      RequiredFiles = [ "src/<YourProduct>/<YourProduct>.fsproj"; "docs/effects-boundary.md" ]
      ForbiddenPrefixes = [ "samples/" ]
      PackageReferences =
        [ { PackageId = "FS.GG.UI.Scene"; Required = true }
          { PackageId = "FS.GG.UI.Testing"; Required = true } ] }

let summary = GeneratedProductAssertions.summarize expectation
```

## `validate` — the entry point that says whether the evidence holds

Building a report and *checking* it are different acts, and only the second can fail. `validate` is the
primary entry point for the second, and it is the same name in every validation module the product
receives — so when you have a value and want the verdict on it, this is the function to look for:

```fsharp
open FS.GG.UI.Testing

EvidenceReports.validate report                   // EvidenceReportValidationResult
GeneratedLayoutValidation.validate layoutCheck    // your generated layout really matches what you declared
ReadinessFileDiscovery.validate discoveryCheck    // the readiness files you claim exist, do
DefaultTextGlyphEvidence.validate glyphCheck      // text actually rasterized glyphs, not empty boxes
PersistentLaunchArtifactValidation.validate launchCheck
```

Each is a pure function from a value you already hold — a *check* record, or the `EvidenceReport` you
just built — to a *result*. No I/O, no runner, no window, so a validation failure is an ordinary
assertion in your suite rather than a thing you eyeball in a log.

**`EvidenceReports.build` gives you a report; it does not give you a passing one.** A report that was
written but never validated is the failure mode this whole section exists for: it looks like evidence,
it reads like evidence, and nothing has checked that it says anything true. Build it, `validate` it,
and assert on the result.

## Build Commands

Run `./fake.sh build -t Dev` then `./fake.sh build -t Verify` in this product.

## Test Commands

Run `./fake.sh build -t Test` to evaluate product expectations and evidence
reports.

## Evidence

Build and write evidence with `EvidenceReports.build` / `write` into this
product's `readiness/` paths, and `EvidenceReports.validate` it — an unvalidated
report is not evidence. Do not copy framework readiness reports into the product.

## Evidence Rules

- Compare your product's `FS.GG.UI.` package pins against the versions you intend
  to ship against; when you validate against a locally built package instead of a
  released one, record that as an explicit caveat so a stale pin is never mistaken
  for a passing check.
- Keep evidence under your product's own `readiness/` paths. Treat generated
  reports as transient: when a path is ignored by default, prove a committed file
  is actually tracked rather than silently dropped.
- Do not run `dotnet test` for the same project/configuration concurrently
  unless each run writes to its own isolated output path.
- Prefer real screenshot evidence, disclose degraded capture, require reviewer
  accepted readiness, and keep manual caveats outside generated summary or
  managed section rewrites.
- Use `--view-image <path> <width> <height>` when the product's authored logical
  canvas differs from the deterministic 1280x720 default. Require positive integer
  dimensions and verify the report's requested size, PNG-header actual size, and
  `dimensions-match=true`; never treat an invalid-size fallback as evidence. Requests
  above 8192 on either axis or 16,777,216 total pixels are a typed resource-limit
  failure before CPU-raster allocation, not a capture attempt to retry.
- Responsiveness evidence must validate pointer and keyboard activation
  separately from screenshot readiness and separate routing from update, render,
  and present latency.
- Canceled, timed-out, skipped, synthetic, substitute, degraded,
  pending-review, or environment-limited checks remain visibly caveated.

## Package Boundary

## Expected-workload performance gate

On `game`, declare idle, movement+aiming, firing, effects/fog, and maximum-content cases **before
feature implementation** in `PerformanceEvidence.expectedWorkloads`. Every untouched row is a failing
`Placeholder`. Replace its initial state and messages with representative product routes, run
`./fake.sh build -t PerformanceEvidence`, review its `definitionDigest`, and copy that digest into
`Authored`; changing the definition makes the declaration stale and red. `Test` and `Verify` repeat the
Release measurement and fail closed on Placeholder/duplicate/stale rows and the active normal-play
budget. `./fake.sh build -t PerformanceIntent` projects the exact workload ids/digests and policy into
the Contracts 7.x SDD `performanceIntent` block; do not stale-copy those fields by hand. Keep normal
play, stress, throughput, and live-compositor workloads separately classified. A linked blocking performance-debt issue permits
deliberate baseline capture, but that baseline never satisfies acceptance. This is bounded headless
update + scene-route evidence, not live compositor or vsync proof.

Run the machine gate before the representativeness critic. Each normal-play row must carry an opaque
FS.GG.Game runner-issued journey receipt. Put a canonical factory at that journey's boot seam;
caller-authored labels/hashes are not provenance. Disclose direct assembly as
`synthetic-constructed`. Keep the independent `performanceCostDrivers` inventory complete, including
every gameplay visual and an inspectable maximum-scale source, and compare declared stimulus with
observed production routing. Present/drop facts from bounded headless runs are **unsupported**, never
zero. Then run `PerformanceCriticRequest` and give its exact digest package to a fresh-context
subagent, or disclose the separated-pass fallback. Record `supported` only in an attributable external
review system at the exact landing commit; in-repo JSON, author-entered reviewer identity, and
same-context mode strings prove no independence. Critic approval cannot waive provenance, coverage,
route, capability, or budget failures.

For continuous pointer pacing, keep two distinct gates. First, distribute 1,000 `PointerMove`
samples across 60 `Viewer.drainInputQueue` presentation boundaries using
`Viewer.enqueueInputWithPointerPolicy ViewerContinuousPointerPolicy.CoalesceLatestPerFrame`; require
at most 60 applied moves and inject one press/release/click sequence that arrives exactly once in
order. Second, measure normal movement+aiming separately and require p95 below 16.67 ms, p99 below
25 ms, and zero sustained catch-up. Production evidence comes from a non-ignored
`ViewerPointerPacingOptions.OnMetrics` sink and records raw/folded/coalesced samples, model updates,
presented frames, repaint causes, and full-render fallbacks.

Keep assertion and evidence logic pure over value records; let your test runner
and `Verify` target perform the actual file and process I/O.

## Generated Product

Every profile that ships a product test project (app, headless-scene, governed,
sample-pack, game) selects Testing alongside Scene so product tests can assert
their own generated structure and package pins.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is
**mandatory** — consult **official online docs first** (the F#/.NET docs and the driven
library's own documentation/API reference), then community sources (forums, Reddit, Q&A
sites, issue trackers and changelogs). If your product uses Spec Kit, record the findings
and resolving links under the feature's `specs/<feature>/feedback/` folder; otherwise record
them in this skill's **Sources** / durable-lessons line (and any product-local `docs/`
location). Offline, the mandate degrades to recording "research blocked — <why>"
rather than hard-failing the phase.

## Related

- [[fs-gg-elmish]] — the runnable interaction-driver recipe (`Perf.runScriptToModel`,
  the `BoundIds` pre-click guard, post-interaction frame capture).
- [[fs-gg-scene]] — the capability whose generated output these tests assert.
- [[fs-gg-game:fs-gg-game-core]] — the seeded `Rng` (`ofSeed`/`nextInt`/`split`) the
  seeded-generation tests pin determinism over.
- [[fs-gg-game:fs-gg-persistence]] — the product-owned `serialize` the byte-identical fixture
  assertions reuse.
- [[fs-gg-project]] — product-level wiring of expectations and readiness gates.

## Sources / links

- Expecto (driven test runner): https://github.com/haf/expecto
- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
