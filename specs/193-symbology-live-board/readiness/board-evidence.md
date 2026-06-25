# Milestone evidence — Symbology Live Board (M6) — FR-013 / SC-006

Captured seeded evidence that closes the M6 milestone: the deterministic board renders non-empty,
reproduces byte-for-byte from a seed, and a different seed diverges. Regenerable from the documented
commands alone (SC-007).

## Seed & fingerprint

| Item | Value |
|---|---|
| Default seed | `1` |
| Script | 120 × `Tick (1.0/60.0)` (no wall clock) |
| Canonical fingerprint (seed 1) | `sha256:4786621d525ea94ae2a78df95893ff175c0abd6053b0fb05f3f0cd2004c96a95` |
| Fingerprint (seed 2) | `sha256:3b434d2ed96b4fa6faabaee58f348146cd2c9d8ad09e8d08e9f4a4b3271b1be4` |

## Two same-seed runs matched (byte-identical)

```
$ dotnet run --project samples/SymbologyBoard -- evidence
symbology-board: seeded fingerprint = sha256:4786621d525ea94ae2a78df95893ff175c0abd6053b0fb05f3f0cd2004c96a95
symbology-board: reproducible (two runs byte-identical).      # exit 0
```

The `evidence` subcommand itself calls `Board.evidence` twice from the same seed and compares; the
"reproducible (two runs byte-identical)" line + exit 0 is the in-run confirmation. Re-invoking the whole
command a second time printed the identical fingerprint (see `smoke.md`), so the result is stable across
process boundaries too.

## Different seed ⇒ different fingerprint (SC-002)

Seed 1 → `sha256:4786…6a95`; seed 2 → `sha256:3b43…1be4` (`s1 <> s2 = true`). The seed materially drives the
board (positions/velocities are derived from `seed` + unit index). See `smoke.md` for the raw run.

## Commands that regenerate this artifact

```bash
dotnet build FS.GG.Rendering.slnx
dotnet run --project samples/SymbologyBoard -- evidence        # seed 1, prints the fingerprint + reproducible line
dotnet test tests/SymbologyBoard.Tests                         # reproducibility + seed-divergence + on-board + non-empty
```

The different-seed fingerprint is produced by `Board.evidence 2 script` (the seed-divergence test, T015,
asserts `Board.evidence 1 script <> Board.evidence 2 script`).

## Determinism guarantee

No member of the sample's core (`integrate`/`update`/`renderScene`/`evidence`) reads a wall clock, performs
IO, or uses render-time randomness (FR-003). Equal `(seed, script)` ⇒ equal final `World` ⇒ equal `Scene` ⇒
equal `SceneCodec.export(...).CanonicalBytes` ⇒ equal `packageIdentity`.
