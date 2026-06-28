# T025 — Quickstart end-to-end validation (Feature 213)

Date: 2026-06-28. Maps `quickstart.md` §1–§7 → SC-001…SC-006.

| Quickstart step | Result | Evidence |
|---|---|---|
| §1 Adopt canonical files (C-1) | ✅ `--adopt` renamed locals, wrote 3 managed files | `smoke-adopt.md` |
| §2 Drift check — managed files pristine (C-1 / INV-1 / SC-001) | ✅ `ok:` ×3, exit 0 | `gate-probes.md`, T010 |
| §3 Regenerate + reproducible restore (C-6 / INV-6 / SC-004) | ✅ exit 0; **0 lockfile churn** → REPRODUCIBLE | `coherence.md` |
| §4 Build + tests green (C-6 / C-7 / R8 / SC-003) | ✅ build exit 0 (0W/0E, `-m:1`); Build.Tests 10/10; SkiaViewer.Tests 207/207 | T013/T016 |
| §5 Gate probes (C-2 / SC-002 / SC-005) | ✅ locked under `GITHUB_ACTIONS`, not blocked without it | `gate-probes.md` |
| §6 Substitution probe — enforcement preserved (C-4 / INV-4) | ✅ locked restore FAILED (`NU1004`), then reverted clean | `substitution-probe.md` |
| §7 Tool parity + scope boundary (C-5 / C-8 / INV-5 / INV-7 / SC-006) | ✅ `fake-cli`==`Fake.Core.*`==`6.1.4`; `template/base UNCHANGED` | `coherence.md`, T022 |

## Success = all of  ✅ (with one disclosed caveat)

zero drift · REPRODUCIBLE · build+tests green · both gate probes as labeled · substitution
fails-then-reverted · `6.1.4`==`6.1.4` · `template/base UNCHANGED`.

**Caveat (environment limitation, disclosed):** the parallel discovery-runner after-baseline (T024)
flaked one sample lane (`ControlsGallery.Tests`) on `System.OutOfMemoryException` under build
parallelism on this dev box; that project is **34/34 green on isolated re-run** (`baseline-diff.md`).
The full slnx build is run with `-m:1` for the same reason. No correctness regression.

## Producer-defect note (raised, not forked)

Adoption surfaced an XML-illegal comment (`` `--check` `` → `--` inside an XML comment) in the
canonical `Directory.Build.props`, byte-identical to the FS-GG/.github source, which MSBuild rejects
(`MSB4024`). Fixed at the **source** (`FS-GG/.github dist/dotnet/`) and re-synced — the managed file in
Rendering stays drift-clean and unforked. The byte-level `diff` drift check cannot detect this class of
defect (bytes match); flagged to the producer for a parse-level check. See completion record / board #11.
