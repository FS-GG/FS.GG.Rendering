# Phase 1 Data Model: Shared Test/Util Helpers

This is a refactor, so there are no persisted data entities or state transitions. The "entities" are
the three shared helpers being introduced and the call-site relationships they collapse. Validation
rules below are the behavior-preservation invariants the implementation MUST honor.

---

## Entity 1 — Repo-root helper

| Field | Type | Notes |
|-------|------|-------|
| `find` | `string -> string` | Given a starting directory, returns the nearest ancestor directory containing a repository marker. |
| `value` | `string` | The resolved repository root: `find AppContext.BaseDirectory`, evaluated once. |

**Home**: `tests/TestSupport/RepositoryRoot.fs` (non-packed test-support assembly).

**Validation rules / invariants**
- Marker set is the canonical union: `*.sln` ∪ `*.slnx` ∪ `build.fsx` (research R1).
- Walks parents toward the filesystem root; on reaching the root with no marker, **fails loudly**
  with an actionable message (FR-002, Acceptance #3). No sentinel return, no infinite loop.
- For the current tree, the resolved root is **identical** to every pre-refactor finder's result
  (both Family A and Family B), so all path-dependent tests resolve unchanged (FR-001, Acceptance #1).

**Relationships**: replaces ~59 local finders (named `findRepositoryRoot` + inline
`FS.GG.Rendering.slnx` walks) across `tests/*` and `tests/Rendering.Harness*`. After migration,
exactly one definition exists (SC-002).

---

## Entity 2 — FNV helper (primitive)

| Field | Type | Notes |
|-------|------|-------|
| `offsetBasis` | `uint64` | `0xcbf29ce484222325UL` — the single literal site after migration. |
| `prime` | `uint64` | `0x100000001b3UL` (= `1099511628211UL`). |
| `step` | `uint64 -> uint64 -> uint64` | `fun h x -> (h ^^^ x) * prime` — the core fold step. |
| `foldBytes` | `uint64 -> byte seq -> uint64` | UTF-8/byte convention (Composition site). |
| char/string mix helpers | (see contract) | Support the `uint16 c` and `int ch` conventions (Control + RetainedRender sites). |

**Home**: `src/Controls/Internal/Hashing.fs`, `module internal Hashing`, no `.fsi`.

**Validation rules / invariants**
- It is a **primitive**, not a single `hash` function: each of the four sites keeps its own mixing
  convention and produces **bitwise-identical** output to its pre-refactor fold for every input,
  including empty byte sequence / empty string (FR-005, edge case, Acceptance #1/#2).
- The Phase-0-corrected `feature159Hash` baseline is preserved exactly.
- Hot-path folds keep their single `mutable h` accumulator and allocation-free shape.
- After migration, `0xcbf29ce484222325UL` appears in exactly one place — inside `Hashing` (SC-003).

**Relationships**: backs four folds — `Composition.fnv1a`, `Control.hashScene`,
`Control.fingerprintParts`/`fingerprintString`, `RetainedRender.feature159Hash`.

---

## Entity 3 — Clamp helper

| Field | Type | Notes |
|-------|------|-------|
| `clamp` | `'a -> 'a -> 'a -> 'a` (comparable) | `fun lo hi value -> min hi (max lo value)`. |

**Home**: `src/Shared/Numeric.fs`, `module internal`, no `.fsi`, linked into `src/Controls` and
`src/SkiaViewer`.

**Validation rules / invariants**
- Returns `value` constrained to `[lo, hi]`; argument order is `(lo, hi, value)`.
- Result is identical to every removed local copy for the same arguments, including boundary and
  inverted-range behavior (all three sites were already `min high (max low value)`) (FR-006,
  Acceptance #1).
- `Layout.clampNonNegative` is a different function and is **not** part of this entity (research R3).

**Relationships**: replaces `let clamp` in `SkiaViewer/Host/OpenGl.fs`, `Controls/RetainedRender.fs`,
`Controls/TextInput.fs`. After migration, exactly one definition exists (SC-004).

---

## Cross-cutting invariants (apply to all three)

- **No public surface change**: no `.fsi` signature added/removed/changed on published `FS.GG.UI.*`
  modules; surface-area/API-reference baselines stay green (FR-007, SC-005).
- **No new package surface**: nothing test-only ships in a package; new helpers are `module internal`
  with no `.fsi` or live in a non-packed assembly.
- **No new module/project cycle** (FR edge cases).
- **Behavior preserved**: no change to rendered pixels, layout output, hash-driven reuse/promotion
  outcomes, or any persisted golden/readiness artifact (FR-008).
- **Independently shippable**: build + full test suite green after each consolidation in isolation
  (FR-009, SC-006).
