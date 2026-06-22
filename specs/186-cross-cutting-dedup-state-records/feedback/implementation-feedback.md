# Implementation Feedback — Cross-Cutting Dedup + State Records (186)

Per-phase friction + generalizable-code candidates captured during implementation of this
byte-identical-by-construction Pattern-C refactor.

## Process friction

- **Stale line numbers in verification commands (low).** SC-002's checks pin literal ranges (e.g.
  `sed -n '1455,1900p' RetainedRender.fs | grep -c 'let mutable'`), but US1's edits shifted every
  downstream line in `ControlsElmish.fs` before US2 ran, so the `runScriptCore` range no longer
  pointed at the carriers. Verified by name instead (`grep -c 'let mutable last*'` → 0). *Lesson:*
  byte-identical refactors that add helper code should express success metrics over **symbols**, not
  line ranges, because the first story invalidates the next story's line numbers.

- **`'exception` is a reserved F# keyword (low).** The generic shared validator naturally wants a
  type parameter named for the exception family; `<'finding, 'exception, …>` fails to parse
  (`FS0010`). Renamed to `'exn`. *Lesson:* worth noting in the data-model when a parameter name
  mirrors a keyword.

- **"One routine" vs genuine type divergence (medium).** US3's two validators share a skeleton but
  diverge in finding type (`VisualInspectionFinding` vs `DamageLocalityFinding`), exception type,
  status type (`VisualInspectionStatus` vs `RetainedInspectionStatus`, the latter adds
  `ReviewRequired`), and result record. A single fully-shared routine is therefore generic over four
  type parameters with the per-family policy injected as a knobs record (`Accept` predicate +
  `DeriveStatus` closure). This honors "written once" for the validate→compute→diagnostics→derive-
  status orchestration while keeping the genuinely type-divergent status policy in each caller's
  closure. *Lesson:* when the plan says "extract to one routine," size the parameterization to the
  real type divergence — a knobs record reads far better than ~16 positional knobs.

## What worked well

- **The existing red/green + byte-identity suites as the only gate.** No new tests; each story was
  validated by re-running the affected suites (Controls 931, Elmish 209, Testing 104, Harness 209)
  and confirming identical counts plus empty `.fsi` diffs. The pre-refactor `baseline-tests.fsx`
  capture (full 16-project red/green, incl. the 2 pre-existing package-feed reds) meant no
  regression could hide as a "new" failure at merge.

- **Compiler-caught completeness for the US2 mutable→field migration.** Collapsing `step`'s 19 loose
  mutables into one `FrameState` record meant every missed usage surfaced as an undefined-name error,
  and any leftover `let mutable` failed SC-002's grep — so the 100+ rewrite sites were mechanically
  verifiable. Mutable record fields (not copy-on-write) preserved the exact accumulation order the
  Edge Cases call out.

- **`mk`/knobs delegation kept all `.fsi` files byte-identical.** Every dedup introduced an internal
  helper and rewrote the public function as a thin delegator, so the only `.fsi` change in the whole
  feature was 37 internal-only lines in `TestingVisual.fsi` (`module internal SharedTesting`); the
  public surface baseline diffed empty (SC-006).

## Generalizable-code candidates

- `SharedTesting.updateManagedSection` (append / replace / fail-loud over a marker pair, result type
  injected via `mk`) is a genuinely reusable managed-section primitive — any future evidence writer
  with begin/end markers should delegate to it rather than copy the marker-counting loop.
