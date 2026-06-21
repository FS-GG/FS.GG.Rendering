# Phase 0 Research: Shared Test/Util Helpers

This refactor has no NEEDS CLARIFICATION on *technology* (F#/.NET, fixed stack). The real research is
(a) cataloguing the actual duplicates and their **behavioral divergence** (FR-010 requires reconciling
to one intended behavior, documented not silently chosen), and (b) deciding **placement** that gives
"one definition" without changing public surface, package surface, or the module graph.

---

## R1 — Repository-root finder: scope and divergence

### Decision
Single shared finder using the **canonical marker set** `*.sln` ∪ `*.slnx` ∪ `build.fsx`, walking
parents to the filesystem root and **failing loudly** with an actionable message at the root.

### Findings (actual, from grep)
There are **two divergent families**, spread across **~59 files** (not ~26 — the spec under-counted
because it only saw the named finder):

- **Family A — named `findRepositoryRoot`** (~27 definitions, e.g. `tests/Lib.Tests/Tests.fs:173`):
  `let rec findRepositoryRoot dir` testing `Directory.GetFiles(dir,"*.sln")` ∪ `"*.slnx"` ∪
  `File.Exists(build.fsx)`, recursing on `Directory.GetParent`, failing at the root. This is the
  **canonical** variant (it already carries the Feature 045 marker-fix comment).
- **Family B — inline anonymous walk** (~32 more sites, e.g. all `tests/Controls.Tests/*` and
  several `tests/Elmish.Tests/*`): an inline `let rec loop dir = if File.Exists(Path.Combine(dir,
  "FS.GG.Rendering.slnx")) then dir else …`. Hard-codes the *specific* slnx filename rather than the
  marker *pattern*.

### Divergence reconciliation (FR-010)
Both families resolve to the **same** directory in this repo (it contains
`FS.GG.Rendering.slnx`). They differ only in generality:
- Family A's `*.slnx` glob matches any future rename; Family B's literal `FS.GG.Rendering.slnx`
  breaks on rename.
- Family A also accepts the historical `build.fsx` (defensive, harmless).
- **Adopted behavior**: the canonical marker set `*.sln` ∪ `*.slnx` ∪ `build.fsx` (Family A). This
  is a strict superset of Family B's detection for the current tree, so migrating Family B sites
  cannot change the resolved root. The difference is **called out here**, not silently chosen.
- **Fail-loud** (Acceptance #3 / FR-002): when no marker exists up to the filesystem root, raise with
  a clear message (e.g. `"Could not locate repository root (no *.sln/*.slnx/build.fsx marker found
  above <start>)"`). Some Family B variants currently loop or return a sentinel at the root; the
  consolidated finder standardizes on the fail-loud variant.

### Rationale
Highest-volume, lowest-risk duplication (test/tooling only). One finder removes the recurring
"fix the marker logic in N places" bug class (the exact Feature 045 failure mode).

### Alternatives considered
- *Put it in the production `src/Testing` package* — rejected: it would ship a test-only concern in a
  public `FS.GG.UI.Testing` package and add public surface (spec edge case + FR-007 forbid this).
- *Linked source file into each test project* — viable and lighter on assemblies, but a real shared
  test assembly is more discoverable and gives a literal single definition; chosen for the test
  helper (see R4). The linked-file mechanism is still used for clamp (R3) where no cycle-free
  project home exists.

---

## R2 — FNV-1a hash: it is a *primitive*, not one function

### Decision
A shared **primitive** exposing the canonical `offsetBasis` (`0xcbf29ce484222325UL`),
`prime` (`0x100000001b3UL` = `1099511628211UL`), and a small set of fold steps — **not** a single
`hash : 'a -> uint64`. Each of the four sites keeps its own *mixing convention* but draws the
constants and the core `step (h ^^^ x) * prime` from the shared module, byte-for-byte preserving its
output.

### Findings — the four sites use THREE distinct conventions
1. `Composition.fs:156` `fnv1a (text:string)` — folds over **UTF-8 bytes**:
   `for b in Encoding.UTF8.GetBytes text do h <- (h ^^^ uint64 b) * prime`.
2. `Control.fs:2453` `hashScene` — folds over **`uint64` mix values** via
   `let mix x = h <- (h ^^^ x) * prime`, with typed mixers (`mixStr` folds **UTF-16 `char`s** as
   `uint64 (uint16 c)` plus a length prefix).
3. `Control.fs:2830` `fingerprintParts` / `fingerprintString` — same `uint64` mix as (2), using the
   **named** `fnvOffset`/`fnvPrime` already extracted locally; `fingerprintString` prefixes a domain
   tag + length.
4. `RetainedRender.fs:850` `feature159Hash (parts:string list)` — folds over **`int ch`** per char
   with the multiply in a **separate statement** and a `'|'` **separator** between parts:
   `hash <- hash ^^^ uint64 (int ch); hash <- hash * 1099511628211UL` … `hash ^^^ uint64 (int '|')`.

These differ in: input unit (UTF-8 byte vs UTF-16 char vs pre-mixed uint64), whether a length/domain
prefix is added, separator handling, and `uint16 c` vs `int ch` char widening. **A single
one-size hash function cannot reproduce all four outputs.**

### Reconciliation (FR-010) and shape
The shared `Hashing` module exposes (names illustrative; finalized in contracts):
- `offsetBasis : uint64`, `prime : uint64`
- `step : uint64 -> uint64 -> uint64` — `fun h x -> (h ^^^ x) * prime` (the core fold)
- `foldBytes : uint64 -> byte seq -> uint64` (covers site 1)
- `foldChars`/`mixChar` helpers for the `uint16 c` and `int ch` conventions (sites 2–4)

Each site is rewritten to *compose* these while preserving its exact convention (including site 4's
separate-statement multiply and `'|'` separator, and the Phase-0-corrected `feature159Hash`
baseline). **No behavioral change** — the constants and core step are centralized; the
site-specific mixing stays at the site. Hot-path sites keep the single `mutable h` accumulator.

### Rationale
The value of the refactor is single-sourcing the **error-prone constants** (the Phase 0 fix was
exactly a wrong constant in one copy) and the core mix step — not forcing four genuinely different
folds into one signature, which would either change hashes or need a parameter zoo.

### Alternatives considered
- *One `hash : byte[] -> uint64` for all four* — rejected: would change sites 2–4 outputs (char vs
  byte unit, separators), breaking Feature 159 identity. Violates FR-005/SC-003.
- *Place in `src/Scene` or a base package* — unnecessary: all four sites are in `src/Controls`, so an
  assembly-internal module is sufficient and adds no cross-project surface.

### Placement
`src/Controls/Internal/Hashing.fs`, `module internal Hashing`, **no `.fsi`** — mirrors the existing
`Internal/AttrKeys.fs` (and `WidgetLowering`/`SceneRenderer`) precedent: assembly-internal, off the
public surface, not in any surface-area baseline. Compile it **before `Composition.fs`** (alongside
`Internal/AttrKeys.fs` at the top of `Controls.fsproj`) so all four sites can see it with no cycle.

---

## R3 — `clamp`: cross-project placement under a no-public-surface constraint

### Decision
Single `clamp lo hi value = min hi (max lo value)` in a new `src/Shared/Numeric.fs`,
`module internal`, **no `.fsi`**, **linked** (`<Compile Include="..\Shared\Numeric.fs" />` with a
`Link`) into `src/Controls` and `src/SkiaViewer`. One source definition; assembly-internal in each
consumer; zero public/package surface.

### Findings — three local `let clamp`, all semantically identical
- `src/SkiaViewer/Host/OpenGl.fs:461` `let clamp lo hi value = min hi (max lo value)`
- `src/Controls/RetainedRender.fs:714` `let clamp lo hi value = min hi (max lo value)`
- `src/Controls/TextInput.fs:45` `let clamp low high value = value |> max low |> min high`
  — this is `min high (max low value)`, **identical** to the other two (no inverted-range divergence).

Out of scope / not a `clamp`:
- `src/Layout/Layout.fs:26` `clampNonNegative value` — a *different*, single-arg function (returns
  `0.0` for negatives). Left as-is; noted so it isn't mistaken for a duplicate.
- `tests/.../Feature138*` and `Controls.Elmish/ControlsElmish.fs:1002` use **inline** `max/min`
  expressions, not a `let clamp` definition; `Rendering.Harness/Perf.fs:257` uses BCL `Math.Clamp`.
  These are not `clamp` *definitions* and so don't affect SC-004 (which counts definitions). Routing
  them through the shared helper is optional polish, not required.

### The constraint that forces a linked file
clamp's two real consumers are `src/Controls` and `src/SkiaViewer`. Their only shared dependencies
are `Scene` and `Diagnostics`. A shared `clamp` placed in either of those must be **public** to be
visible across the assembly boundary — which adds a symbol to that package's public surface and its
surface-area baseline, **violating FR-007/SC-005**. There is therefore no cycle-free *project* home
for a cross-project clamp that keeps surface unchanged.

### Resolution
A single source file compiled (linked) into both consuming assemblies as `module internal` gives:
- exactly **one** source definition (satisfies SC-004 — a repo-wide search finds one `let clamp`),
- assembly-internal visibility in each consumer (no public/package surface — FR-007),
- no new project/package and no new project-reference edge (no cycle risk).

### Alternatives considered
- *Public clamp in `Scene`/`Diagnostics`* — rejected: changes a published package's surface/baseline.
- *Leave clamp duplicated* — rejected: FR-006/SC-004 require a single definition.
- *New non-packed `src` micro-project referenced by both* — heavier than a linked file for a
  one-line function and still adds a project; linked source is the minimal mechanism.

---

## R4 — Test-helper sharing boundary

### Decision
New **non-packed** project `tests/TestSupport` (`<IsPackable>false</IsPackable>`,
`<IsTestProject>false</IsTestProject>`), referenced by every consuming test/harness project.
Exposes the shared repo-root finder (`RepositoryRoot.find : string -> string` and a resolved
`RepositoryRoot.value`).

### Rationale
- Test projects currently reference `src/*` but **not each other**, so there is no existing
  cross-test home; a dedicated test-support assembly is the cycle-free option.
- `IsPackable=false` guarantees it never becomes an `FS.GG.UI.*` package (spec edge case + FR-007).
- A real shared assembly gives a literal single definition and is the most discoverable place for
  future shared test utilities (this is Phase 1 of an ongoing code-health effort).

### Alternatives considered
- *Linked source file into each test project* — viable, lighter on assemblies, but less discoverable
  and the "one definition" is only at source level. Chosen for clamp (no project home) but not here,
  where a clean test assembly exists as an option.
- *Reuse `src/Testing`* — rejected (would ship test-only code in a public package; R1).

---

## R5 — Independent shippability & evidence (FR-009, SC-006)

### Decision
Sequence the three consolidations as **independent, individually-green change units** in priority
order (repo-root → FNV → clamp). After each, the slnx builds and the full test suite is green except
the two documented package-feed reds. Each unit is independently revertible.

### Evidence approach (matches Phase 0)
- **Repo-root**: full `dotnet test` (path-dependent tests resolve the same root) + `grep` proves zero
  remaining local finders (SC-002).
- **FNV**: Feature 159 identity/reuse/promotion suites + composition/control fingerprint tests stay
  green (validates byte-identical hashes without asserting absolute constants) + `grep` proves zero
  `0xcbf29ce484222325UL` outside the helper (SC-003).
- **clamp**: layout-sizing / text-caret / viewer-scaling tests stay green + `grep` proves one
  definition (SC-004).
- **Surface**: `git diff -- '*.fsi'` shows no published-surface change; surface-area/API-reference
  baselines stay green (SC-005, FR-007).

### Baseline capture (Foundational, before any edit)
Record the pre-refactor `dotnet build` + `dotnet test` result, explicitly noting the two known
package-feed reds (`tests/Package.Tests`, `samples/ControlsGallery/ControlsGallery.Tests`) so they
are not later misread as regressions (SC-001).
