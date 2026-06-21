# Quickstart: Backward-Compatibility Shim Removal (validation guide)

Runnable steps that prove the four removals are byte-stable, surface-exact, and at baseline red/green.
All commands run from repo root; GL tests need `DISPLAY=:1`. See [plan.md](./plan.md),
[research.md](./research.md), and `contracts/` for the binding details.

## 0. Prerequisites
```bash
dotnet build FS.GG.Rendering.slnx -c Debug      # needed before refreshing surface baselines
mkdir -p specs/184-backcompat-cleanup/readiness/{baseline,post-change}
```

## 1. Capture baseline (before any edit)
```bash
# 1a. surface baselines (12 files) — snapshot for the empty-diff check later
dotnet fsi scripts/refresh-surface-baselines.fsx
git stash || true   # or copy readiness/surface-baselines/ aside

# 1b. full Release sweep → record the red/green set (expect Package.Tests 8-fail,
#     ControlsGallery 2-fail, 14 others green)
DISPLAY=:1 dotnet fsi scripts/baseline-tests.fsx --config Release \
  --out specs/184-backcompat-cleanup/readiness/baseline/test-baseline.md

# 1c. behavior snapshots for the production-path removals (US2 overlay chain + fingerprint,
#     US4 typed chartValues) — captured by the focused tests added in those stories.
```
Record allowed pre-existing reds in `readiness/baseline/known-reds.md` (copy from
`specs/183-…/readiness/baseline/known-reds.md`).

## 2. Per-story loop (US1 → US2 → US3 → US4)
For each story: make the edits in its contract → `dotnet build FS.GG.Rendering.slnx -c Release` →
run the affected `*.Tests.fsproj` under `DISPLAY=:1` → verify the story's invariants:

| Story | Surface check | Behavior check |
|---|---|---|
| US1 | `git diff src/Controls/Control.fsi` = only `MaxOffset` line gone | 3 tests pass via `MaxVerticalOffset` |
| US2 | no `.fsi` public change; baseline `.txt` unchanged | overlay chain + `Composition.fingerprint` = baseline |
| US3 | `git diff src/Controls/Types.fsi` = only `Payload` line (+ any accessor) | event/widget/nav tests pass via `Nav` |
| US4 | none (internal) | typed `chartValues` = baseline; fallback test deleted |

## 3. Surface confirmation (after all stories)
```bash
dotnet build FS.GG.Rendering.slnx -c Debug
dotnet fsi scripts/refresh-surface-baselines.fsx
git diff readiness/surface-baselines/      # MUST be empty (type-granular — research D2)
git diff src/Controls/Control.fsi src/Controls/Types.fsi   # the real surface delta (US1 + US3)
```

## 4. Bump + feed + sample alignment (US1/US3 — the public removals)
```bash
# bump FS.GG.UI.Controls 0.1.45-preview.1 -> 0.1.46-preview.1 (one bump covers US1+US3)
#   edit src/Controls/Controls.fsproj <Version>, re-pin Controls.Elmish consumer
# write specs/184-…/readiness/compatibility-ledger.md (US1 + US3 entries)
dotnet fsi scripts/dev-repack.fsx --sample samples/SecondAntShowcase   # pack -> ~/.local/share/nuget-local/, re-pin, restore
DISPLAY=:1 dotnet test samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj -c Release
```
> US2/US4 are internal (Tier 2) → no bump, no ledger entry.

## 5. Final parity + capture
```bash
DISPLAY=:1 dotnet fsi scripts/baseline-tests.fsx --config Release \
  --out specs/184-backcompat-cleanup/readiness/post-change/test-baseline.md
diff specs/184-backcompat-cleanup/readiness/{baseline,post-change}/test-baseline.md
```
Expected: **same** red/green (Package.Tests 8-fail, ControlsGallery 2-fail, 14 green). Record FR-010
retentions (`ModifierSource.LegacyOverlaySource`, widget `*.create`, `LegacyHostMsg`, `-v1`/`-v2`) in
`readiness/post-change/retentions.md`.

## Success = spec SC-001…SC-007
- SC-001 four identities removed or descoped-with-reason (US4 in-scope per D4); none kept-but-unused.
- SC-002 public surface strictly smaller (`Control.fsi`/`Types.fsi` each drop one field).
- SC-003 production-path output byte-identical (US2 overlay, US4 chart).
- SC-004 full sweep at baseline red/green.
- SC-005 every public-surface change (US1/US3) bumped + ledgered.
- SC-006 zero sample/template breakage (rebuilt + passing against the bumped package).
- SC-007 no weakened test; removed-behavior tests deleted.
