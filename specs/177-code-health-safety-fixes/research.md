# Phase 0 Research: Code Health — Quick Safety Fixes

All three items were investigated directly against the tree at `177-code-health-safety-fixes`.
There were no open `NEEDS CLARIFICATION` items; this document records the resolved decisions.

## Decision 1 — `feature159Hash` offset constant (FR-001, FR-002)

**Decision**: Change `src/Controls/RetainedRender.fs:851` from `1469598103934665603UL` to
`0xcbf29ce484222325UL`. Then verify (FR-002) that no persisted/golden hash artifact is silently
invalidated and that the Feature 159 suites stay green.

**Rationale**:
- The standard 64-bit FNV-1a offset basis is `0xcbf29ce484222325UL` = `14695981039346656037UL`
  (20 digits). The current literal `1469598103934665603UL` is **19 digits — the standard value with
  the trailing `7` dropped**. This is the signature of a transcription typo, not a chosen constant.
- The same fold already uses the correct FNV-1a prime `1099511628211UL` (lines 855/857) and its
  comment reads `compact deterministic FNV-1a fold` — the author intended canonical FNV-1a.
- Every other hash accumulator in the repo uses the canonical basis in **hex** form:
  `Composition.fs:157` (`0xcbf29ce484222325UL`), `Control.fs:2454` (`0xcbf29ce484222325UL`),
  `Control.fs:2830` (`let private fnvOffset = 0xcbf29ce484222325UL`). Using the hex form here makes
  the literal visually identical to its siblings, so a future reader can pattern-match it as "the
  standard basis" at a glance — which is exactly the property whose absence caused this bug.

**Surface/behavior impact**:
- `feature159Hash` is **private** (absent from `RetainedRender.fsi`); `feature159ContentIdentity` is
  `val internal` with an unchanged signature. → **Tier 2, no `.fsi`/baseline edit.**
- The computed `ContentId` *values* change. This is internal identity used for layer reuse/promotion
  comparisons; it is compared computed-to-computed within a run, never against a stored absolute
  number (see below). No pixels, layout, or user-facing output change.

**Golden / regression evidence gathered**:
- `grep` for any 15+ digit `ContentId` literal across `tests/` and `specs/` → **no hits**. The
  Feature 159 tests (`Feature159IdentitySplitTests`, `Feature159ReuseCounterTests`,
  `Feature159PromotionEvidenceTests`, `Feature159ReadinessPackageTests`) assert *relations* between
  freshly computed identities (split equality, reuse counts, promotion evidence), not equality to a
  hardcoded number. Changing the seed shifts all computed identities together, preserving every
  relation. The `specs/*/data-model.md` matches for `ContentId` are design prose, not goldens.
- **FR-002 gate (must run, not assume)**: before merge, do a full `dotnet build` + `dotnet test`, and
  `git status`/`git diff` any regenerated `specs/*/readiness/**` evidence. If a persisted artifact
  embeds an absolute hash, treat its change as an explicit, reviewed acceptance — never a silent one.

**Alternatives considered**:
- *Document the divergent value as intentional*: rejected — there is no plausible intent for a
  one-digit-short FNV basis sitting beside a correct FNV prime and three correct siblings.
- *Use the decimal `14695981039346656037UL`*: correct value, but rejected in favor of hex to match
  the three sibling accumulators verbatim and maximize future pattern-recognition.

## Decision 2 — Tautological test placeholders (FR-003, FR-004)

**Decision**: Replace each `Expect.isTrue true` with a real, falsifiable assertion over state already
in scope; keep each test non-empty.

**`tests/Controls.Tests/Feature093ParityTests.fs:77`** (test `T020 — capture the pre-refactor
procedural baselines`): the body calls `captureBaselines ()`, which writes
`button/check-box/check-box-checked.<theme>.normal.scene.txt` under
`specs/093-visual-state-style-layer/readiness/parity/`. **New assertion**: after the call, assert the
expected baseline files actually exist on disk (e.g. `Expect.isTrue (File.Exists path) …` for each
written file, or assert the file set is present and non-empty). This is falsifiable — it fails if
`captureBaselines` silently writes nothing or to the wrong place.

**Rationale**: the test's real purpose is "baselines got captured"; asserting their on-disk existence
makes the previously vacuous claim checkable without changing what the test is *about*.

**`tests/Controls.Tests/TypedMigrationTests.fs:555`** (test `stateful facades introduce no parallel
model type (SC-003)`): the guarantee ("typed facades reuse existing model/effect types, no fork") is
proven at compile time by the local type-annotated bindings `taInit`/`lvInit`. The runtime
`Expect.isTrue true` adds nothing, and `ignore taInit/lvInit` exists only to consume the bindings.
**New assertion**: replace the vacuous assert with a runtime equality that exercises the reused types
— call the typed `init` and assert it equals the canonical underlying `init` for the same input
(e.g. `TextArea.init`/`ListView.init` results equal the `TextInput`/`Collections` baseline already
used elsewhere in this file). This proves reuse behaviorally, not just structurally, and removes the
now-unnecessary `ignore` lines.

**Rationale**: keeps the SC-003 intent, yields a falsifiable check, and leaves the test with a
meaningful assertion (FR-004). Confirm exact canonical `init` signatures/values during implementation
to keep the equality honest.

**Alternatives considered**:
- *Delete the assertions and rely on compilation*: rejected for `TypedMigrationTests` — FR-004
  forbids leaving a test with no assertion; a compile-only "test" gives no run-time signal.
- *Keep `T020` as a pure capture step with no assertion*: rejected — same FR-004 reason; existence
  checks are cheap and genuinely catch a broken capture.

## Decision 3 — Centralize `"rev=150"` layout cache version (FR-005, FR-006)

**Decision**: Introduce one private constant in `src/Layout/Layout.fs` and derive every occurrence
from it, preserving byte-identical output.

**Occurrences** (all four must agree): token `rev=150` at `:839` (inside an interpolated
`$"…|rev=150"`) and `:964` (standalone `"rev=150"` joined by `|`); plus the integer field
`Revision = 150` at `:847` and `:974`.

**Approach**: define a single source of truth for the revision *number* and derive the token from it,
e.g. `[<Literal>] layoutCacheRevision = 150` then `rev={layoutCacheRevision}` at both string sites and
`Revision = layoutCacheRevision` at both record sites. Using one numeric literal feeding both the
token and the `Revision` field gives true single-source (a future bump is one edit) while the composed
strings remain exactly `…|rev=150` and `rev=150`.

**Byte-identity constraint (FR-006)**: the constant MUST render to the exact bytes `rev=150` at each
site. An interpolated `$"rev={layoutCacheRevision}"` over an `int` literal `150` yields `rev=150`
(invariant integer formatting, no culture risk). The implementation MUST add/confirm a test asserting
the composed `QueryIdentity` and `cacheEntry` `EntryId` strings are byte-identical to the pre-change
output for representative inputs.

**Surface impact**: the new constant is **omitted from `Layout.fsi`** → private, no surface change.
The `Revision` *field* already exists on the record types (in `.fsi`); only the literal value's
*source* moves. **Tier 2, no `.fsi`/baseline edit.**

**Alternatives considered**:
- *A string constant `"rev=150"` referenced at both string sites only* (leaving `Revision = 150`
  untouched): satisfies FR-005 literally but leaves the number duplicated in the int fields, so a
  bump still needs multiple edits. Rejected in favor of one numeric source feeding all four sites.
- *Hand-bumping both sites in lockstep (status quo)*: rejected — that is the exact drift hazard this
  item exists to remove.

## Cross-cutting confirmations

- **No new dependencies.** Expecto, SkiaSharp, and the existing project graph are unchanged.
- **Tier 2 throughout.** No `.fsi` or surface-area baseline edits in any of the three items.
- **Build+test is the smoke test.** There is no app-visible behavior to observe; `dotnet build` +
  full `dotnet test` green (with Feature 159 suites intact) plus a golden/evidence `git diff` review
  is the complete verification surface. This run is scheduled early (Foundational) and repeated at
  the end per the plan's standing-assumption note.
