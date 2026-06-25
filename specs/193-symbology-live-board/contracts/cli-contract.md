# Contract — SymbologyBoard CLI subcommands

The sample's user-facing interface. Mirrors the `samples/CanvasDemo` dispatch (FR-010/US3). Invoked as `dotnet run --project samples/SymbologyBoard -- <subcommand>`.

## Dispatch table (`[<EntryPoint>]` in Program.fs)

| Args | Subcommand | Behavior | Exit |
|---|---|---|---|
| *(none)* | default → `evidence` | Run the deterministic headless evidence path (FR-004, US3 scenario 2). | `0` reproducible / non-zero on divergence |
| `evidence` `…` | evidence | Same as default. | `0` / non-zero |
| `interactive` `…` | interactive | Open the live board window if the host supports it; else print a skip notice. | `0` (incl. graceful headless skip) / non-zero on launch failure |
| `<other>` `…` | unknown | Print a usage hint listing supported subcommands. | non-zero |

## `evidence` — deterministic board fingerprint (FR-004/FR-005/FR-006)

> **Two distinct things share the name "evidence" — keep them apart.**
> - **`Board.evidence : seed -> script -> string`** (board-core.md) is the pure fingerprint function: one call ⇒ **one** canonical fingerprint. The reproducibility test asserts `Board.evidence s script = Board.evidence s script` (two equal calls), and the seed-sensitivity test asserts `Board.evidence s1 script <> Board.evidence s2 script`.
> - **The `evidence` subcommand** here is the *repro-check wrapper*: it calls `Board.evidence` **twice from the same seed** and compares the two fingerprints, then reports reproducibility and sets the exit code.

- Builds the fixed seed + scripted `Tick` sequence (no wall clock), calls `Board.evidence` twice from the **same** seed, and compares the two canonical board fingerprints.
- **Match** (byte-identical): prints e.g. `symbology-board: seeded fingerprint = <id>` then `symbology-board: reproducible (two runs byte-identical).`; exit `0`.
- **Divergence**: prints a diff-style `symbology-board: NON-REPRODUCIBLE — <a> <> <b>` to stderr; exit non-zero (FR-005, Constitution VI — never report divergence as success).
- Seed-sensitivity (FR-006/SC-002) is demonstrated by the documented different-seed invocation (see quickstart); the two seeds yield different fingerprints.

## `interactive` — live board window (FR-007/SC-003/SC-004)

- Probes `Viewer.runtimeCapability()`. If `not PersistentWindow`: prints `symbology-board: interactive mode skipped — no live window/GL host.`; exit `0` (never blocks/crashes — Constitution VI).
- Else launches `ControlsElmish.runInteractiveApp` with a board-sized `ViewerOptions` and the MVU host; on `Ok` prints the session status and exits `0`; on `Error` prints the failure and exits non-zero.

## `<unknown>` — usage hint (FR-010/US3 scenario 3)

- Prints to stderr: `symbology-board: unknown subcommand '<x>' (use 'evidence' or 'interactive').`; exit non-zero.

## Canonical output strings (single source of truth)

These exact strings are the canonical expected output; spec.md (FR-007), tasks.md (T012/T016/T018), and the per-SC validation checks reference **these**, not paraphrases:

- headless interactive skip (SC-004): `symbology-board: interactive mode skipped — no live window/GL host.`
- reproducible (SC-001): `symbology-board: reproducible (two runs byte-identical).`
- non-reproducible (FR-005): `symbology-board: NON-REPRODUCIBLE — <a> <> <b>` (stderr)
- unknown subcommand (FR-010): `symbology-board: unknown subcommand '<x>' (use 'evidence' or 'interactive').` (stderr)

## Invariants the contract guarantees

- The evidence path uses **no wall clock, no GPU, no IO in the sim/scene path** (FR-003) — it runs identically in headless CI.
- No subcommand mutates any public package surface; the sample is a pure consumer (FR-012/SC-005).
