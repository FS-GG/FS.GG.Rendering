# Quickstart: Running the Mechanism Audit

This guide shows how to run the audit's verification suite and regenerate the findings report. It is a run/validation guide — the actual checks and report content are produced during implementation (`/speckit-tasks` → `/speckit-implement`).

## Prerequisites

- .NET `net10.0` SDK (the pinned toolchain).
- For the deterministic audit subset: **no display needed** (counter-based, headless).
- For pixel-parity (T1), live (T2), and timing (T3) claims: an X11 + GL-capable session (`DISPLAY=:1`). Absent that, those checks degrade-and-disclose (recorded `deferred`, never `pass`).

## 1. Build

```bash
dotnet build FS.GG.Rendering.slnx -c Release
```

## 2. Run the deterministic audit subset (the fast inner loop)

Audit tests are named with an `Audit:` prefix and live in `Audit_*.fs` across the module test projects, so they can be run as one filtered pass:

```bash
# whole audit subset, headless (discriminating-power + counter-effectiveness + adversarial)
dotnet test FS.GG.Rendering.slnx -c Release -- --filter "Audit"

# or a single mechanism, e.g. the picture cache
dotnet test tests/Controls.Tests -c Release -- --filter "Audit: picture cache"
```

**Expected**: green for every mechanism whose claims are verifiable headlessly. A red `Audit:` test means a divergence was found — that is a *successful audit outcome*, captured as a Finding in the report (it is not a build to "fix" by weakening the assertion — see Principle V).

### What each kind proves
- **discriminating-correctness** — output is identical with the mechanism on vs bypassed (oracle flag off), AND the assertion is proven to go red when bypassed (SC-003).
- **counter-effectiveness** — the work-reduction counter beats the disabled baseline by the claimed margin; equal-to-baseline is reported as a no-op (SC-004).
- **adversarial** — cache-key completeness, determinism under reordering, `hashScene` collision resistance, settled-animation byte-identity.

## 3. Run the capability-tier claims (timing / pixels / live)

These reuse the R5 harness as the evidence engine; they run on the scheduled/manual cadence (spec 005), not in the gate:

```bash
# present-mode / offscreen pixel readback (T1) — degrades cleanly if no GL
dotnet run --project tests/Rendering.Harness -- offscreen

# frame-rate cap / pacing (T3)
dotnet run --project tests/Rendering.Harness -- perf --mode paced-60 --frames 100

# live present + input (T2) — requires a live desktop
DISPLAY=:1 dotnet run --project tests/Rendering.Harness -- live-x11
```

**Expected on a headless runner**: exit `0` with `status:"skipped"` and a `SkipReason` naming the missing capability — recorded in the report as `unverifiable-here / defer-to-tier`, never as a pass (Principle VI). On a capable runner: `run.json` + `metrics.csv` + `summary.md` under `artifacts/harness/run-*/`, cited by the report.

## 4. Regenerate the audit artifacts

After running the suites, refresh the two durable deliverables:

- `docs/audit/mechanism-inventory.md` — every mechanism as Claim rows (schema: `contracts/claim-record.md`) with each Claim's Verification status (schema: `contracts/verification-record.md`).
- `docs/audit/mechanism-audit.md` — one Verdict per mechanism (schema: `contracts/verdict-record.md`) plus the coverage-summary footer (SC-008).

## 5. Validate the audit is complete

- [ ] Every mechanism in the plan's mechanism table has ≥1 Claim row, none `unverified` (SC-001, SC-002).
- [ ] Every correctness `pass` has Discriminating Proof = true (SC-003).
- [ ] Every effectiveness claim has a recorded margin vs baseline, classified realized / no-op / deferred (SC-004).
- [ ] Every capability-absent check is `skipped/deferred` with rationale + required tier — zero passes without evidence (SC-005).
- [ ] Every divergence has a severity + recommendation (SC-006); each verdict is reproducible from its `Reproduce` field (SC-007).
- [ ] The coverage-summary footer states the counts of correctness-defects, silent-no-ops, and overstated claims found (SC-008).
