# Quickstart — Validating Feature 121 (Frame-Rate Pacing & No-Alloc Idle Tick)

Conformance backfill: code + tests exist. Validation = build green + the 121 suites green + readiness authored
+ zero new public-surface delta.

## 1. Build

```bash
dotnet build FS.GG.Rendering.slnx -c Release
```

## 2. Run the 121 suites (both deterministic-headless, no GL)

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj     -c Release --filter "121"   # US2 no-alloc core
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release --filter "121"   # US1 pacing + validation
```

Expected green:
- `Feature121IdleTickTests` — US2: no active clock ⇒ `advanceStateClocks` reference-equal (`obj.ReferenceEquals`,
  no allocation) (SC-003); an active clock ⇒ rebuilt map + `Elapsed` advanced as `RetainedRender.advance`
  (099/103 unchanged) (FR-004).
- `Feature121LiveHostPacingTests` — US1: `shouldAdvanceFrame` false-before / true-at-and-after the interval
  (SC-001); a tighter cap yields strictly fewer advances (FR-002/SC-001); non-positive `FrameRateCap` rejected
  as a `ProductDefect` ("frame-rate cap") (FR-003/SC-005); a positive cap clears validation (FR-001).
  *(Deterministic-headless — the pure decision + the pre-GL validation seam; the persistent window is not driven.)*

## 3. Author the readiness evidence

121 imported without `readiness/`. Author `specs/121-paced-noalloc-tick/readiness/`: `us1-frame-rate-pacing.md`
(SC-001/SC-005), `us2-no-alloc-idle.md` (SC-003). Gitignored — transient.

## 4. Confirm zero new public-surface delta (FR-005)

```bash
git status -s tests/surface-baselines/   # MUST be empty
```

`advanceStateClocks` internal; `shouldAdvanceFrame`/`FrameRateCap` ride already-baselined public types.

## 5. Full suite (no regression)

```bash
dotnet test FS.GG.Rendering.slnx -c Release   # 0 failures; standing 18 skips unrelated to 121
```

## Success = the 121-close conformance bar

Build green; both 121 suites green; readiness authored; zero new public-surface delta; `/speckit-analyze`
consistent. No pixel/desktop claim — proofs are reference-equality + the pure pacing decision + validation.
The live clock advance/sample (099) and cross-fade (103) are unchanged.
</content>
