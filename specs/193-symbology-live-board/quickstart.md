# Quickstart — Symbology Live Board Sample (M6)

Validation/run guide for the live board sample. Implementation details live in `tasks.md`; type/contract details in [data-model.md](./data-model.md) and [contracts/](./contracts/).

## Prerequisites

- .NET `net10.0` SDK; repo builds with normal tooling.
- The existing `FS.GG.UI.Symbology`, `FS.GG.UI.Canvas`, `FS.GG.UI.Scene`, `FS.GG.UI.SkiaViewer`, `FS.GG.UI.Controls(.Elmish)`, `FS.GG.UI.Themes.Default` projects (already in `FS.GG.Rendering.slnx`).

## Build

```bash
dotnet build FS.GG.Rendering.slnx
```

Expected: the solution builds with the new `samples/SymbologyBoard` and `tests/SymbologyBoard.Tests` projects included, **zero new build errors** (FR-008/SC-005).

## Run — deterministic evidence (default, headless-safe)

```bash
dotnet run --project samples/SymbologyBoard               # default == evidence
dotnet run --project samples/SymbologyBoard -- evidence   # explicit
```

Expected: prints `symbology-board: seeded fingerprint = <id>` then `symbology-board: reproducible (two runs byte-identical).`; exit `0` (SC-001). Runs with no wall clock and no GPU.

## Run — interactive live board

```bash
dotnet run --project samples/SymbologyBoard -- interactive
```

- Live-window/GL host: a board window opens showing **every** roster unit as its fixed-grammar symbol, each animating continuously and smoothly, none drifting off-board over a sustained run (SC-003).
- Headless host: prints `symbology-board: interactive mode skipped — no live window/GL host.`; exit `0`, never blocks/crashes (SC-004).

## Run — unknown subcommand

```bash
dotnet run --project samples/SymbologyBoard -- frobnicate
```

Expected: `symbology-board: unknown subcommand 'frobnicate' (use 'evidence' or 'interactive').` to stderr; non-zero exit (US3 scenario 3).

## Tests — deterministic core

```bash
dotnet test tests/SymbologyBoard.Tests
```

Expected green: same-seed reproducibility (SC-001), different-seed divergence (SC-002), on-board invariant over N steps (FR-011/SC-003), non-empty board for a degenerate roster (edge case).

## Capture milestone evidence (FR-013/SC-006)

1. Confirm same-seed reproducibility and capture the fingerprint:
   ```bash
   dotnet run --project samples/SymbologyBoard -- evidence
   ```
2. Confirm the seed drives the board (different seed ⇒ different fingerprint) using the documented second-seed invocation (see `contracts/cli-contract.md`).
3. Record both fingerprints, the "two runs matched" confirmation, and the exact commands in `specs/193-symbology-live-board/readiness/board-evidence.md`.

The artifact is regenerable from the documented commands alone (SC-007).

## Per-success-criterion validation map

| SC | How to validate |
|---|---|
| SC-001 | `evidence` twice, same seed → identical fingerprint, exit `0` |
| SC-002 | `evidence` from two different seeds → different fingerprints |
| SC-003 | interactive on a live host → all units animate, none leaves the board; on-board test green |
| SC-004 | interactive on a headless host → skip notice + exit `0` |
| SC-005 | `dotnet build` clean; surface-drift gate shows zero drift on existing baselines |
| SC-006 | `readiness/board-evidence.md` present and regenerable from the documented command |
| SC-007 | a first-time dev runs the single documented build+evidence command; unknown subcommand prints usage |
