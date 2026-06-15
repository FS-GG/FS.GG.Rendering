# Quickstart — Validating Feature 110 (Retained Pointer Routing → Authored Control ID)

Conformance backfill: code + tests exist. Validation = build green + the 110 suites green + readiness authored
+ zero new public-surface delta.

## 1. Build (Release, zero warnings)

```bash
dotnet build FS.GG.Rendering.slnx -c Release
```

## 2. Run the 110 suites (headless)

```bash
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj -c Release --filter "110"
```

Expected green:
- `Feature110RetainedRoutingTests` — US1: a routed move/click performs zero routing full renders
  (`ViewCalled=false`, `FullRenderCount=0`, no fallback); a move burst coalesces to ≤ 1 processed move
  (SC-001/SC-002/SC-009/FR-012).
- `Feature110RetainedRoutingParityTests` — US2: keyed controls, unkeyed siblings, composite (binding above),
  nested containers, `MapPointer` fallback, and focus identity all dispatch identically to the oracle
  (SC-003/SC-004/FR-005/FR-006).
- `Feature110FallbackTests` — US3: every normal scenario reports `FullRenderFallbackCount = 0`; a constructed
  unroutable case increments it by exactly one, matching the oracle (SC-005/SC-006).

## 3. Author the readiness evidence

110 imported without `readiness/`. The backfill authors `specs/110-retained-authored-routing/readiness/`:
`us1-zero-render-routing.md` (SC-001/SC-002/SC-009), `us2-oracle-parity.md` (SC-003/SC-004), `us3-counted-fallback.md`
(SC-005/SC-006). The directory is gitignored (`specs/*/readiness/`) — transient, never committed.

## 4. Confirm zero new public-surface delta (FR-013)

```bash
git status -s tests/surface-baselines/   # MUST be empty
```

The routing functions are internal; `FullRenderFallbackCount` is additive on the already-baselined public
`FrameMetrics` (type-granular baseline). No source changed ⇒ baselines unchanged.

## 5. Full suite (no regression)

```bash
dotnet test FS.GG.Rendering.slnx -c Release
```

Expected: 0 failures (the standing 18 honest skips remain, unrelated to 110).

## Success = the C5 conformance bar

Build green; the three 110 suites green; readiness authored; zero new public-surface delta; `/speckit-analyze`
consistent. What this does NOT prove: no pixel/desktop claim — parity is message-list + work-count equality,
deterministic.
</content>
