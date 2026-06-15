# Quickstart: Validate Feature 092 (Wire Retained Identity State onto the Live Path)

This is a **conformance backfill** — the code and tests already exist. Validation = the two suites are
green, the readiness evidence regenerates, and the public-surface delta is zero. See
[contracts/live-identity-state.md](./contracts/live-identity-state.md) and
[data-model.md](./data-model.md) for the seam details.

## Prerequisites

- .NET SDK with `net10.0` support; solution `FS.GG.Rendering.slnx` at the repo root.
- No GL context required — 092's proofs are deterministic and headless.

## 1. Build the affected assemblies + their tests

```bash
dotnet build src/Controls/Controls.fsproj
dotnet build src/Controls.Elmish/Controls.Elmish.fsproj
dotnet build tests/Controls.Tests/Controls.Tests.fsproj
dotnet build tests/Elmish.Tests/Elmish.Tests.fsproj
```

## 2. Run the authoritative 092 suites

The headline live-survival proof (US1) lives in `Elmish.Tests`; US2–US5 live in `Controls.Tests`.

```bash
# US1 — live focus + in-progress draft survive a position shift through the real seam,
#        and the rebuild-every-frame baseline loses it
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter "Feature092"

# US2–US5 — hit-test identity distinctness, pre-filled append/MultiLine, theme reuse,
#            work-reduction split, single first-frame paint + frame-0 diagnostics, multi-frame parity
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter "Feature092"
```

Expected: all green. A red test or a weakened assertion is a **finding to report**, not to patch
(Principle V) — never seed focus/text state to green the live-survival test.

## 3. Confirm zero public-surface delta (FR-012)

The entire 092 seam is `internal`. The surface baselines for both affected assemblies must be
byte-unchanged:

```bash
# Surface-drift check must report NO changes for FS.GG.UI.Controls and FS.GG.UI.Controls.Elmish
#   tests/surface-baselines/FS.GG.UI.Controls.txt
#   tests/surface-baselines/FS.GG.UI.Controls.Elmish.txt
```

## 4. Read the captured readiness evidence (optional)

Under `specs/092-wire-retained-identity-state/readiness/` (gitignored), mapped to the success criteria:

- `live-survival/survival.txt`, `live-survival/baseline-fails.txt` — SC-001 (US1)
- `focus-resolution/focus-resolution.txt`, `focus-resolution/prefilled-append.txt` — SC-002 (US2)
- `theme-reuse/theme-reuse.txt` — SC-006 (US3)
- `work-reduction/work-reduction.txt` — SC-003 (US4)
- `multi-frame/first-frame.txt`, `multi-frame/round-trip.txt` — SC-004/SC-005/SC-007 (US5 + parity)

The evidence judges render parity by **structural scene equality** and survival by **draft
continuity** (`hix` → `hixy`); it explicitly does **not** claim pixel-level or desktop-visibility
proofs.

## Done when

- [ ] Both `Feature092` suites are green (US1 in `Elmish.Tests`, US2–US5 in `Controls.Tests`).
- [ ] Surface-drift check reports zero delta for `FS.GG.UI.Controls` and `FS.GG.UI.Controls.Elmish`.
- [ ] Readiness evidence regenerates and matches SC-001..SC-007.
- [ ] No test was weakened, skipped, or hand-seeded to pass.
