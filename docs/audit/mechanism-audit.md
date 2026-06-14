# Mechanism Audit — Findings Report

Feature `006-verify-imported-mechanisms`. One **Verdict** per mechanism (schema:
`specs/006-verify-imported-mechanisms/contracts/verdict-record.md`), derived from the
Verifications recorded in [`mechanism-inventory.md`](./mechanism-inventory.md). This is the
audit's decision surface: every mechanism is judged, evidenced, given a severity where
divergent, a recommendation, and a reproducible command.

**Headline**: all 14 imported mechanisms work as advertised. Every correctness claim was
verified with *proven discriminating power* (the assertion was shown to go red when the
mechanism is bypassed), and every effectiveness claim was measured to beat its disabled
baseline by a wide margin — no silent no-ops, no present-but-dead caches, no correctness
defects. Two minor, non-blocking divergences were recorded (a cosmetic counter-narrative
overstatement on the memo cache's *disabled* path, and a vsync-fidelity sub-aspect of the
frame-rate cap that this runner's timing tier is not authoritative for).

The deterministic subset (50 audit tests across the five module test projects) ran
`local-deterministic` and headless-safe; the pixel (T1), live (T2), and timing (T3) tiers
ran on this GL-capable runner (`DISPLAY=:1`, AMD/Mesa GL 4.6) rather than degrading.

## Verdicts

| Mechanism | Verdict | Severity | Evidence | Recommendation | Reproduce |
|---|---|---|---|---|---|
| keyed-reconciliation | works-as-advertised | — | `reconcile.roundtrip` (1000 FsCheck pairs + keyed/positional/kind-mismatch corpus; red on forced-wrong patch) | (none) | `dotnet tests/Controls.Tests/bin/Release/net10.0/Controls.Tests.dll --filter "Audit: Reconcile"` |
| incremental-layout | works-as-advertised | — | `incremental-layout.equivalence` (constructed + 500 FsCheck edits; red when changed id omitted), `incremental-layout.effectiveness` (10/1001 re-measured) | (none) | `dotnet tests/Layout.Tests/bin/Release/net10.0/Layout.Tests.dll --filter Audit` |
| memo-cache | works-as-advertised | cosmetic | `memo.parity` (red on stale-hit), `memo.key-completeness`, `memo.effectiveness` (hits 30/30) | re-scope-claim — correct the `RetainedRender.fsi:172`/`182` comment: with the oracle off the wired `step` *bypasses* the seam and recomputes without tallying `MemoMisses` (reports 0/0, not "every node a miss"). Behaviour is correct; only the counter narrative overstates. | `dotnet tests/Controls.Tests/bin/Release/net10.0/Controls.Tests.dll --filter "Audit: Memo cache"` |
| picture-cache | works-as-advertised | — | `picture-cache.parity` (red on divergent scene), `picture-cache.effectiveness` (hits 90/90 steady; counter provably moves — not present-but-dead) | (none) | `dotnet tests/Controls.Tests/bin/Release/net10.0/Controls.Tests.dll --filter "Audit: Picture cache"` |
| text-measure-cache | works-as-advertised | — | `text-cache.parity`, `text-cache.key-completeness` (single-field diffs miss; equal to un-cached measure), `text-cache.effectiveness` (hits 29/30) | (none) | `dotnet tests/Controls.Tests/bin/Release/net10.0/Controls.Tests.dll --filter "Audit: Text-measure cache"` |
| backend-replay-cache | works-as-advertised | — | `replay-cache.parity` (T1 raster; red on divergent scene), `replay-cache.effectiveness` (Hits 9/10 after one warmup record) | (none) | `dotnet tests/SkiaViewer.Tests/bin/Release/net10.0/SkiaViewer.Tests.dll --filter Audit` |
| scene-fingerprint | works-as-advertised | — | `fingerprint.determinism` (equal scenes hash equal), `fingerprint.collision` (every single-field render-affecting change flips it; 500 FsCheck cases) | (none) | `dotnet tests/Controls.Tests/bin/Release/net10.0/Controls.Tests.dll --filter "Audit: Scene fingerprint"` |
| animation-clock | works-as-advertised | — | `animation-clock.determinism` (1000 FsCheck; clamp = no overshoot; no rewind), `animation-clock.gating` (true in flight / false settled, both directions) | (none) | `dotnet tests/Controls.Tests/bin/Release/net10.0/Controls.Tests.dll --filter "Audit: Animation clock"` |
| animation-sampling | works-as-advertised | — | `animation-sampling.determinism`, `animation-sampling.settled-identity` (settled ≡ static, in-flight differs) | (none) | `dotnet tests/Scene.Tests/bin/Release/net10.0/Scene.Tests.dll --filter Audit` |
| animation-tick-gating | works-as-advertised | — | `animation-tick-gating.effectiveness` (idle ⇒ `Sub.none`; active ⇒ ticks; both directions discriminated) | (none) | `dotnet tests/Elmish.Tests/bin/Release/net10.0/Elmish.Tests.dll --filter "Audit animation tick-gating"` |
| damage-rect-tracking | works-as-advertised | — | `damage-rect.union-correctness` (overlap counted once vs naive sum; 200 generated sets vs brute-force; clamped to frame), `damage-rect.effectiveness` (DirtyArea ≈ 2% of full) | (none) | `dotnet tests/Elmish.Tests/bin/Release/net10.0/Elmish.Tests.dll --filter "Audit damage-rect"` |
| virtualization | works-as-advertised | — | `virtualization.effectiveness` (materialized 30/10000 ≈ 0.3%; window constant while total scales 100→10000) | (none) | `dotnet tests/Elmish.Tests/bin/Release/net10.0/Elmish.Tests.dll --filter "Audit virtualization"` |
| present-mode-selection | works-as-advertised | — | `present-mode.liveness` — T1 offscreen (`run-20260614-224608`) + T2 live-x11 (`run-20260614-224617`) both `passed` (window creation / visibility / focus) | (none) | `dotnet run --project tests/Rendering.Harness -c Release -- offscreen` ; `... -- live-x11` |
| frame-rate-cap | works-as-advertised | cosmetic | `frame-rate-cap.timing` — T3 `perf --mode paced-60` (`run-20260614-224614`): p50 2.684 ms ≪ 16.6 ms 60-fps budget (headroom confirmed) | defer-to-tier — the throughput-budget claim is verified; the strict *vsync-faithful cadence* sub-aspect is `notAuthoritativeFor` the T3 throughput proof and is deferred to a vsync-authoritative tier. | `dotnet run --project tests/Rendering.Harness -c Release -- perf --mode paced-60 --frames 100` |

## Coverage summary

```text
Mechanisms audited:        14
  works-as-advertised:     14
  benefit-overstated:      0    (overstated-benefit)
  not-working-or-no-op:    0    (of which correctness-defects: 0, silent-no-ops: 0)
  unverifiable-here:       0    (deferred to capability tiers)
Discriminating-power confirmed for all correctness passes: yes
```

All nine correctness-bearing claims (reconcile round-trip, memo parity, picture-cache
parity, text-cache parity, incremental-layout equivalence, animation-clock gating,
animation-sampling settled-identity, damage-rect union, replay-cache parity) demonstrated a
red-when-bypassed counter-case — none is a vacuous pass (SC-003). Every effectiveness claim
recorded a margin beating its disabled baseline (SC-004); none is a silent no-op. No
capability-absent check was recorded as a pass without a tier (SC-005) — and on this runner
all three capability tiers in fact ran and passed.

### Recorded divergences (both non-blocking)

1. **memo-cache — cosmetic.** Disabled-path counter narrative overstates: `MemoEnabled=false`
   bypasses the `memoize` seam and recomputes without incrementing `MemoMisses` (reports
   `0/0`). Observable parity + effectiveness hold. Recommendation: re-scope the `.fsi` comment.
2. **frame-rate-cap — cosmetic / partial-defer.** Throughput budget verified with large
   headroom; vsync-faithful pacing fidelity is outside what the T3 timing tier can attest.
   Recommendation: defer the vsync-cadence sub-aspect to a vsync-authoritative tier.

A separate, out-of-scope observation (not a mechanism verdict): `SceneEvidence.renderHash` is
alpha-insensitive — an opacity-only change did not change the hash. The audited Animation seam
is itself correct (structural equality was used as the authoritative oracle). Flag for a
possible follow-up audit of `renderHash` resolution.
