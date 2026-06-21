# Contract: Shared Repository-Root Finder

**Visibility**: internal test-support surface (non-packed `tests/TestSupport` assembly). **Not** a
public package API; no `.fsi` published to any `FS.GG.UI.*` package.

## Shape (illustrative F#)

```fsharp
namespace FS.GG.TestSupport

module RepositoryRoot =
    /// Nearest ancestor of `start` containing a repository marker
    /// (*.sln, *.slnx, or build.fsx). Raises if none is found up to the filesystem root.
    val find : start: string -> string

    /// find AppContext.BaseDirectory, evaluated once.
    val value : string
```

## Behavioral contract

1. **Marker detection** — a directory is the root iff it contains any of: a `*.sln` file, a `*.slnx`
   file, or `build.fsx`. (Canonical union; superset of the inline `FS.GG.Rendering.slnx` variant.)
2. **Walk** — if `start` is not the root, recurse on its parent until a marker is found.
3. **Fail-loud** — if the filesystem root is reached with no marker, raise an exception whose message
   names the failure and the starting directory (e.g. `"Could not locate repository root: no
   *.sln/*.slnx/build.fsx marker at or above <start>"`). No sentinel, no infinite loop.
4. **Determinism** — `value` resolves to the same directory every pre-refactor finder returned for the
   current tree; all path-dependent tests are unaffected.

## Consumers (migration target)
Every `tests/*` and `tests/Rendering.Harness*` file that currently defines a local finder (~59
files). Each deletes its local definition and references `FS.GG.TestSupport`'s `RepositoryRoot`.

## Acceptance mapping
- FR-001, FR-002, FR-003; Acceptance Scenarios 1–3 of User Story 1; SC-002.
