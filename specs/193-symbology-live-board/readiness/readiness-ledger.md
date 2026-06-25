# Readiness ledger — Symbology Live Board (M6)

## Committed evidence (gitignore allowlist + `git check-ignore` proof)

`specs/*/readiness/` is gitignored by default; this feature was added to the `.gitignore` allowlist
(`!specs/193-symbology-live-board/readiness/` + `/**`) before staging, with the transient baseline run logs
deliberately kept ignored (`specs/193-symbology-live-board/readiness/*-run.log`). Proof:

| Path | `git check-ignore` |
|---|---|
| `readiness/board-evidence.md` | tracked-ok (committed) |
| `readiness/smoke.md` | tracked-ok (committed) |
| `readiness/baseline.md` | tracked-ok (committed) |
| `readiness/baseline-after.md` | tracked-ok (committed) |
| `readiness/no-regression.md` | tracked-ok (committed) |
| `readiness/us1-interactive.md` | tracked-ok (committed) |
| `readiness/readiness-ledger.md` | tracked-ok (committed) |
| `readiness/baseline-run.log` | IGNORED (transient) |
| `readiness/baseline-after-run.log` | IGNORED (transient) |

## Evidence index

- **smoke.md** — early evidence smoke run (T008): same-seed stable + divergent-seed, real CLI runs.
- **board-evidence.md** — milestone exit record (T017, FR-013/SC-006): seed, fingerprint, two-runs-matched,
  second-seed divergence, regenerating commands.
- **us1-interactive.md** — US1 interactive launch evidence (T013); visual smoothness disclosed as
  environment-limited, on-board guarantee proven by tests.
- **baseline.md / baseline-after.md / no-regression.md** — full discovery-based test baseline before/after
  (T002/T022): zero new reds; the two pre-existing reds (`Package.Tests` 8, `ControlsGallery.Tests` 2) are
  unchanged; new `SymbologyBoard.Tests` 5 🟢.
