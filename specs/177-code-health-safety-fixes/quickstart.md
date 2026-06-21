# Quickstart / Validation Guide: Code Health — Quick Safety Fixes

This guide proves the three Phase 0 items end-to-end. It is a run/validation guide; the concrete
edits live in `tasks.md` and the implementation. See [`research.md`](./research.md) for the recorded
decisions and [`data-model.md`](./data-model.md) for the before/after of each artifact.

## Prerequisites

- .NET `net10.0` SDK; repo restored.
- A clean `git status` before starting (so the FR-002 golden review can read a meaningful diff).

## Baseline (before any edit)

Capture the pre-change state so byte-identity and golden invariants are checkable afterward:

```bash
# 1. Full green baseline.
dotnet build
dotnet test

# 2. Record the four duplicated layout-version sites (for FR-006 byte-identity comparison later).
grep -n 'rev=150\|Revision = 150' src/Layout/Layout.fs

# 3. Confirm the hash literal and its private/internal status (FR-001 / Tier 2 evidence).
grep -n '1469598103934665603UL' src/Controls/RetainedRender.fs
grep -n 'feature159Hash\|feature159ContentIdentity' src/Controls/RetainedRender.fsi   # private + val internal
```

## Validation scenarios

### Scenario 1 — `feature159Hash` typo fixed, no golden silently invalidated (FR-001, FR-002)

```bash
# After the edit, the seed matches its three siblings (hex form) and the old literal is gone.
grep -rn '0xcbf29ce484222325UL' src/Controls/RetainedRender.fs        # expect: line 851
grep -rn '1469598103934665603UL' src/                                  # expect: NO matches

# Feature 159 relational suites stay green (identity split, reuse counts, promotion, readiness).
dotnet test --filter 'FullyQualifiedName~Feature159'

# FR-002 golden gate: review any regenerated evidence. Must be empty OR an explicitly accepted diff.
git status specs/**/readiness/ 2>/dev/null
git diff --stat specs/                                                  # review before merge
```

**Expected**: hex literal present, old literal absent, Feature 159 suites pass, and any persisted
artifact change is reviewed and accepted (not silent). `RetainedRender.fsi` shows no diff (Tier 2).

### Scenario 2 — both placeholders are now falsifiable (FR-003, FR-004, SC-003)

```bash
# No always-true assertion remains in the two touched files.
grep -rn 'Expect.isTrue true' tests/Controls.Tests/Feature093ParityTests.fs \
                              tests/Controls.Tests/TypedMigrationTests.fs    # expect: NO matches

dotnet test --filter 'FullyQualifiedName~Feature093Parity'
dotnet test --filter 'FullyQualifiedName~TypedMigration'
```

**Expected**: zero `Expect.isTrue true` matches; both test lists build and pass; each affected test
still has at least one meaningful assertion (T020 asserts baseline files exist; the SC-003 facade
test asserts typed `init` equals the canonical underlying `init`).

### Scenario 3 — layout cache version centralized, byte-identical output (FR-005, FR-006, SC-004)

```bash
# The literal token now appears once; both former sites derive from the constant.
grep -cn '"rev=150"' src/Layout/Layout.fs        # expect: at most one raw literal (the constant def)
grep -n 'rev=150\|Revision = 150\|layoutCacheRevision' src/Layout/Layout.fs

dotnet test --filter 'FullyQualifiedName~Layout'
```

**Expected**: one source of truth for the revision; the byte-identity test asserts composed
`QueryIdentity` / `cacheEntry.EntryId` strings are unchanged from the recorded baseline;
`Layout.fsi` shows no diff (Tier 2).

## Final acceptance (whole change set)

```bash
dotnet build          # succeeds
dotnet test           # full suite green, no newly skipped tests (SC-001)
git diff --stat -- '*.fsi'                 # expect: empty (no .fsi surface change, Tier 2)
```

Map back to success criteria:

- **SC-001** — `dotnet build` + full `dotnet test` green, nothing newly skipped.
- **SC-002** — `feature159Hash` resolved to the canonical basis (hex), zero ambiguity.
- **SC-003** — zero always-true assertions in the two files; each test keeps a meaningful assertion.
- **SC-004** — `rev=150` literal in exactly one place; cache identity/key strings byte-identical.
- **SC-005** — no unintended runtime/`.fsi`/golden change; the internal hash value is the sole
  reviewed exception (FR-002).
