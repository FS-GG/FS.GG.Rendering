# Quickstart: Validating Shared ReadinessStatus (Phase 3)

This is a **behavior-preserving** refactor. The validation strategy is a byte-for-byte diff against a
baseline captured before any edit, plus a full test run showing no new failures. There is **no live app
smoke run** (no defect/root-cause hypothesis — see plan Standing Assumption).

## Prerequisites

- .NET SDK with `net10.0`; an X display for tests (`DISPLAY=:1`).
- Clean working tree on branch `180-shared-readiness-status`.
- Reference: contracts in [`contracts/`](./contracts/), entities in [`data-model.md`](./data-model.md).

## Step 0 — Capture baseline (before any edit)

```bash
mkdir -p specs/180-shared-readiness-status/readiness
# Full test sweep over EVERY *.Tests.fsproj (incl. release-only / sample lanes):
dotnet fsi scripts/baseline-tests.fsx --out specs/180-shared-readiness-status/readiness/baseline.md
```

Record the **allowed pre-existing non-green** lanes in `baseline.md` (expected from feature-179
baseline): `tests/Package.Tests` and `samples/ControlsGallery/ControlsGallery.Tests`. These are baseline,
not regressions. The bar is **no new failures vs. this capture**.

## Step 1 — Build & full test (sanity, matches baseline)

```bash
dotnet build FS.GG.Rendering.slnx -c Release
DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release
```

Expected: same pass/fail set as `baseline.md`.

## Per-story validation

Run after each story lands. Each story is independently shippable (FR-009) and must leave the repo green
relative to baseline.

### US1 — shared `ReadinessStatus` (Tier 1)
1. `dotnet build FS.GG.Rendering.slnx -c Release` — clean.
2. `DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release` — no new failures vs baseline; **every
   readiness golden assertion green** (proves serialized status text is byte-identical — FR-006).
3. Confirm de-duplication (SC-001):
   ```bash
   # Exactly one shared statusToken / blocksAcceptance; old generic mappers gone.
   rg -n "statusToken|blocksAcceptance" src/Diagnostics src/Testing
   ```
4. Regenerate surface baselines and confirm only the additive Diagnostics surface moved:
   ```bash
   dotnet fsi scripts/refresh-surface-baselines.fsx
   git diff --stat readiness/surface-baselines/
   # Expect: FS.GG.UI.Diagnostics.txt changed (additive); FS.GG.UI.Testing.txt UNCHANGED.
   ```

### US2 — parameterized validator (Tier 2)
1. Build + full test — no new failures vs baseline.
2. The **159/160/161 readiness suites** (status + diagnostics + missing-artifact assertions) stay green
   — byte-for-byte oracle (FR-004, SC-002).
3. Confirm the three original modules are gone:
   ```bash
   rg -n "module Feature159Readiness|module Feature160ThroughputReadiness|module Feature161HostLaneReadiness" src/Testing
   # Expect: no module *bodies* — only config records + optional wrapper validates.
   ```

### US3 — shared formatting helper (Tier 2)
1. Build + full test — no new failures vs baseline.
2. Visual / VisualInspection / RetainedInspection readiness golden assertions stay green (byte oracle).
3. Confirm one definition each (SC-003):
   ```bash
   rg -n "let esc |let q |let jsonStringArray |let countsText |let statusCountsText " src/Testing
   # Expect: the three Testing.fs copies collapsed to shared-module references.
   ```
4. Confirm the `Diagnostics.fs` `System.Text.Json` variant was not forced into a byte change.

## Step N — Polish (final)

```bash
dotnet build FS.GG.Rendering.slnx -c Release
dotnet fsi scripts/baseline-tests.fsx --config Release \
  --out specs/180-shared-readiness-status/readiness/post-change.md
diff specs/180-shared-readiness-status/readiness/baseline.md \
     specs/180-shared-readiness-status/readiness/post-change.md
```

**Success (all must hold):**
- `post-change.md` shows **no new failures** vs `baseline.md` (SC, FR-008).
- All serialized readiness/evidence artifacts byte-identical to baseline (SC-004).
- One `statusToken` + one `blocksAcceptance` default (SC-001); one parameterized validator + 3 config
  entries (SC-002); one definition per formatting helper (SC-003).
- Net source-line count for the touched reporting code is **reduced** vs baseline (SC-005).
- A same-shaped feature is addable as a single config entry (SC-006).
