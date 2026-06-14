# Mechanism Inventory — Verify Imported Rendering & Controls Mechanisms

Feature `006-verify-imported-mechanisms`. Every advertised mechanism imported from
`fs-skia-ui` is listed below as one or more falsifiable **Claim** rows (schema:
`specs/006-verify-imported-mechanisms/contracts/claim-record.md`), each paired with its
**Verification** record (schema: `contracts/verification-record.md`).

`Status` starts `unverified` and is terminal at audit close (SC-002 forbids any
`unverified` row at close). A red `Audit:` test is a *finding*, not a build to green by
weakening an assertion (Constitution Principle V). Capability-absent ⇒ `deferred` with a
tier rationale, never `pass` (Principle VI).

All deterministic audit tests are name-prefixed `Audit: ` and live in `Audit_*.fs` across
the five module test projects; run the whole subset by executing each test exe with
`--filter Audit` (Expecto substring filter — the reliable selector; the `dotnet test --
--filter` form does not forward to the Expecto adapter). Capability-tier claims reuse the
`tests/Rendering.Harness` evidence engine.

**Audit environment**: headless-capable Linux with `DISPLAY=:1` live and an AMD/Mesa GL
4.6 context (direct rendering, refresh 119.93 Hz). The pixel (T1), live (T2), and timing
(T3) tiers therefore **ran here** rather than degrading; on a truly headless runner they
would record `deferred` + tier.

## Claims

14 mechanisms → 25 falsifiable claims. Every mechanism in the plan's mechanism table has ≥1 Claim row (SC-001).

| Mechanism | Claim ID | Kind | Statement | Source | Advertised | Verification Method | Status |
|---|---|---|---|---|---|---|---|
| keyed-reconciliation | reconcile.roundtrip | correctness | `apply prev (diff prev next).Patch` reconstructs a tree structurally equal to `next` for any (prev, next) tree pair (keyed / positional / kind-mismatch) | `Controls/Reconcile.fsi:61` (`diff`), `:67` (`apply`) | documented | discriminating-correctness | verified |
| scene-fingerprint | fingerprint.determinism | determinism | `hashScene` returns an identical 64-bit value across repeated calls on the same scene, and identical scenes hash identically | `Controls/RetainedRender.fsi:326` | documented | adversarial | verified |
| scene-fingerprint | fingerprint.collision | key-completeness | Any single render-affecting field change (geometry, colour, text, opacity, transform) flips the fingerprint — no stale hit can cross a render-affecting diff | `Controls/RetainedRender.fsi:326` | documented | adversarial | verified |
| memo-cache | memo.parity | correctness | Render with `MemoEnabled=true` is byte-identical to `MemoEnabled=false` on the same scene | `Controls/RetainedRender.fsi:161`; seam `:309` (`memoize`) | documented | discriminating-correctness | verified |
| memo-cache | memo.key-completeness | key-completeness | A changed dependency does not collide with a stored entry — an unequal dependency forces a `Miss` (no stale reuse) | `Controls/RetainedRender.fsi:309` | documented | adversarial | verified |
| memo-cache | memo.effectiveness | effectiveness | On a repeated unchanged render, `MemoHits` reaches near-100% steady-state while `MemoMisses`→0 | `Controls/RetainedRender.fsi:213` | documented | counter-effectiveness | verified |
| picture-cache | picture-cache.parity | correctness | Output with `PictureCacheEnabled=true` is byte-identical to `false` on the same scene | `Controls/RetainedRender.fsi:176` | documented | discriminating-correctness | verified |
| picture-cache | picture-cache.effectiveness | effectiveness | On a repeated unchanged render, `PictureCacheHits` reaches steady-state ≫0 while `PictureCacheMisses`→0 (and the counter provably moves — not present-but-dead) | `Controls/RetainedRender.fsi:244` | documented | counter-effectiveness | verified |
| text-measure-cache | text-cache.parity | correctness | Output + layout with `TextCacheEnabled=true` is byte-identical to `false` on the same scene | `Controls/RetainedRender.fsi:186`; seam `:347` (`measureTextCached`) | documented | discriminating-correctness | verified |
| text-measure-cache | text-cache.key-completeness | key-completeness | Two measure requests differing in any one field (text / family / size / weight) are distinct entries; a single-field diff misses and returns a fresh measurement equal to un-cached `Scene.measureText` | `Controls/RetainedRender.fsi:347` | documented | adversarial | verified |
| text-measure-cache | text-cache.effectiveness | effectiveness | On repeated identical measurement, the text-cache hit-rate reaches near-100% (cold miss then all hits) vs the disabled re-measure-every-time baseline | `Controls/RetainedRender.fsi:253` | documented | counter-effectiveness | verified |
| incremental-layout | incremental-layout.equivalence | correctness | `evaluateIncremental previous changed available root` geometry equals a full `evaluate available root` of the same final tree | `Layout/Layout.fsi:10` (`evaluateIncremental`), `:8` (`evaluate`) | documented | discriminating-correctness | verified |
| incremental-layout | incremental-layout.effectiveness | effectiveness | A single localized change in a large tree re-measures ≪ the full-`evaluate` baseline (`LayoutResult.Invalidated` count ≪ N) | `Controls/RetainedRender.fsi:207` (`RemeasuredNodeCount`); `Layout/Layout.fsi:10` | documented | counter-effectiveness | verified |
| animation-clock | animation-clock.determinism | determinism | `advance` is pure: a non-positive delta never rewinds, a large delta clamps `Elapsed` to the duration (no overshoot past the endpoint), and replaying an identical injected-delta sequence reproduces identical state | `Controls/RetainedRender.fsi:371` (`advance`), `:402` (`sampleOnPaint`) | documented | adversarial | verified |
| animation-clock | animation-clock.gating | correctness | `clockActive` is `true` while in flight and `false` once settled — it genuinely gates redraw in both directions | `Controls/RetainedRender.fsi:376` | documented | discriminating-correctness | verified |
| animation-sampling | animation-sampling.determinism | determinism | `applyAt` / `sampleFrames` produce byte-identical output across repeated invocations at fixed time points | `Scene/Animation.fsi:116` (`applyAt`), `:118` (`sampleFrames`) | documented | adversarial | verified |
| animation-sampling | animation-sampling.settled-identity | correctness | A settled animation (opacity ended at 1.0, identity transform) sampled at/after duration is byte-identical to the static scene; an in-flight sample differs (identity-at-rest is non-vacuous) | `Scene/Animation.fsi:116` | documented | discriminating-correctness | verified |
| animation-tick-gating | animation-tick-gating.effectiveness | effectiveness | `tickSubscription` returns `Sub.none` when the model is not animating and a ticking `Sub` when it is — no frame requested while idle | `Elmish/AnimationTick.fsi:24` | documented | counter-effectiveness | verified |
| damage-rect-tracking | damage-rect.union-correctness | correctness | `unionArea` counts overlapping dirty rects once (returns the true union, not the naive sum) and never exceeds `frameArea` | `Controls/RetainedRender.fsi:331` (`unionArea`); `:232`/`:233` (counters) | documented | discriminating-correctness | verified |
| damage-rect-tracking | damage-rect.effectiveness | effectiveness | A localized change damages a small fraction of the full-repaint baseline (`DirtyArea`/`DirtyRectCount` ≪ full frame) | `Controls/RetainedRender.fsi:233` | documented | counter-effectiveness | verified |
| virtualization | virtualization.effectiveness | effectiveness | For a collection larger than the viewport, `VirtualMaterialized` is bounded by the viewport window, not `VirtualTotal` (materialized does not scale with total) | `Controls/RetainedRender.fsi:221` (`VirtualMaterialized`), `:222` (`VirtualTotal`) | documented | counter-effectiveness | verified |
| backend-replay-cache | replay-cache.parity | correctness | `PictureReplayCache.create enabled:true` produces output byte-identical to `enabled:false` (the always-direct parity oracle) on the same boundary | `SkiaViewer/PictureReplayCache.fsi:18` (`create`) | documented | discriminating-correctness | verified |
| backend-replay-cache | replay-cache.effectiveness | effectiveness | Across repeated identical boundary paints, `stats.Hits` reaches steady-state after a single warmup record (`Records`/`Misses` stabilize) | `SkiaViewer/PictureReplayCache.fsi:34` (`stats`) | documented | counter-effectiveness | verified |
| present-mode-selection | present-mode.liveness | liveness | The viewer selects a present mode and brings up a live present path (window creation / visibility / present) | `SkiaViewer/PresentMode.fsi:10` (`ViewerPresentMode`) | inferred | harness-timing | verified |
| frame-rate-cap | frame-rate-cap.timing | timing | With `FrameRateCap` set, frame production stays within the per-frame budget (60 fps ⇒ 16.6 ms); the renderer has ample throughput headroom under a paced-60 run | `SkiaViewer/SkiaViewer.fsi:25` (`FrameRateCap`) | inferred | harness-timing | verified |

## Verifications

One Verification per Claim. Validation rules (`contracts/verification-record.md`): every
correctness `pass` carries `Discriminating Proof=true` (SC-003); every effectiveness `pass`
carries a `Margin` beating the baseline (SC-004); no capability-absent check is a `pass`
without a tier rationale (SC-005). All deterministic tiers ran `local-deterministic`; the
capability tiers ran on this GL-capable runner.

| Claim ID | Method | Evidence Ref | Scenario | Baseline | Result | Discriminating Proof | Margin | Tier | Skip Rationale | Synthetic |
|---|---|---|---|---|---|---|---|---|---|---|
| reconcile.roundtrip | discriminating-correctness | `Audit: apply (diff prev next) reproduces next over >=1000 generated pairs (FR-005)`; `Audit: round-trip holds on the hand-built keyed / positional / kind-mismatch corpus` | 1000 FsCheck tree pairs + keyed/positional/kind-mismatch corpus | forced wrong `Keep` patch; mutated `Replace` patch | pass | true | — | local-deterministic | — | false |
| fingerprint.determinism | adversarial | `Audit: identical scenes hash identically + deterministic across calls (FR-007)` | repeated `hashScene` on equal scenes | — | pass | true | — | local-deterministic | — | false |
| fingerprint.collision | adversarial | `Audit: COLLISION PROBE — any single render-affecting change flips the fingerprint (FR-007)`; `Audit: COLLISION PROBE (FsCheck) — distinct rectangle widths never collide over >=500 cases` | enumerated single-field render-affecting mutations + 500 FsCheck cases | — | pass | true | no collision over probed field diffs | local-deterministic | — | false |
| memo.parity | discriminating-correctness | `Audit: memo-on ≡ memo-off renders byte-identical scenes (FR-004)`; `Audit: DISCRIMINATING — a memo HIT returns the OLD subtree even if compute changed` | representative scene rendered via `step`, `MemoEnabled` true vs false | `MemoEnabled=false` | pass | true | — | local-deterministic | — | false |
| memo.key-completeness | adversarial | `Audit: cache-key completeness — a changed dependency does NOT collide (Miss) (FR-009)` | one-cell dependency change at `memoize` seam + wired `step` | — | pass | true | — | local-deterministic | — | false |
| memo.effectiveness | counter-effectiveness | `Audit: EFFECTIVENESS — repeated unchanged render drives MemoHits→~100%, misses→0 vs disabled baseline (T030)` | 30 identical re-renders | `MemoEnabled=false` (no reuse) | pass | — | hits 30/30 (rate 1.000), misses 0 vs 0 enabled-reuse disabled | local-deterministic | — | false |
| picture-cache.parity | discriminating-correctness | `Audit: cache-on ≡ cache-off byte-identical, with a discriminating divergence check (FR-004)` | repeated themed grid scene, `PictureCacheEnabled` true vs false | `PictureCacheEnabled=false` | pass | true | — | local-deterministic | — | false |
| picture-cache.effectiveness | counter-effectiveness | `Audit: PRESENT-BUT-DEAD — PictureCacheHits provably MOVES`; `Audit: EFFECTIVENESS — PictureCacheHits reach steady-state ≫0 while misses→0 (T031)` | 30 repeated frames of a 3-row stable scene | `PictureCacheEnabled=false` (hits 0) | pass | — | hits 90/90 steady (3/frame), misses_total 0; counter provably moves | local-deterministic | — | false |
| text-cache.parity | discriminating-correctness | `Audit: cache-on ≡ cache-off byte-identical scene + bounds, with a discriminating divergence check (FR-004)` | repeated text scene, `TextCacheEnabled` true vs false | `TextCacheEnabled=false` | pass | true | — | local-deterministic | — | false |
| text-cache.key-completeness | adversarial | `Audit: key-completeness — a single-field difference (text|family|size|weight) MISSES with correct fresh metrics (FR-009)` | single-field diffs across text/family/size/weight on `measureTextCached` | un-cached `Scene.measureText` | pass | true | each single-field diff misses; metrics equal un-cached measure | local-deterministic | — | false |
| text-cache.effectiveness | counter-effectiveness | `Audit: EFFECTIVENESS — repeated identical measure yields a high text-cache hit-rate vs the disabled oracle (T032)` | 30 repeated identical measures | `TextCacheEnabled=false` (re-measures all) | pass | — | hits 29/30 (rate 0.967, one cold miss) vs disabled 0 hits / 30 misses | local-deterministic | — | false |
| incremental-layout.equivalence | discriminating-correctness | `Audit: equivalence — incremental == full across constructed change sets, with discriminating proof (T024)`; `Audit: equivalence — FsCheck byte-identity over generated edits` (500 cases) | constructed change sets + 500 FsCheck generated edits | incremental that OMITS the changed id (diverges) | pass | true | — | local-deterministic | — | false |
| incremental-layout.effectiveness | counter-effectiveness | `Audit: effectiveness — localized change in a large tree re-measures a small subset (T029)` | one localized leaf edit in a 1001-node tree | full `evaluate` (re-measures all N=1001) | pass | — | `Invalidated` 10/1001 re-measured (≈1%, ≪ N/10) | local-deterministic | — | false |
| animation-clock.determinism | adversarial | `Audit: replaying an identical injected-delta sequence reproduces identical state (>=1000 cases)`; `Audit: a very-large delta clamps Elapsed to the duration and settles at the endpoint (NO overshoot)`; `Audit: a non-positive delta is a no-op` | 1000 FsCheck delta sequences + clamp + no-rewind | — | pass | true | sampled opacity exactly 1.0 at clamp, never >1.0 | local-deterministic | — | false |
| animation-clock.gating | discriminating-correctness | `Audit: clockActive gates redraw — TRUE while in flight, FALSE once settled (DISCRIMINATING)` | fresh / mid-flight / settled clocks | constant-return would fail one direction | pass | true | — | local-deterministic | — | false |
| animation-sampling.determinism | adversarial | `Audit: applyAt is byte-identical across repeated invocations at fixed times`; `Audit: sampleFrames is byte-identical across repeated invocations` | repeated sampling at fixed time points | — | pass | true | — | local-deterministic | — | false |
| animation-sampling.settled-identity | discriminating-correctness | `Audit: settled animation sampled at/after duration is byte-identical to the static scene`; `Audit: DISCRIMINATING — an in-flight sample (opacity < 1) DIFFERS from the static scene` | settled vs in-flight sample against the static scene | in-flight sample (opacity<1, non-identity transform) | pass | true | — | local-deterministic | — | false |
| animation-tick-gating.effectiveness | counter-effectiveness | `Audit: NO tick is requested when idle (model not animating => Sub.none) (T035)`; `Audit: a tick IS requested when active`; `Audit: gating discriminates BOTH directions` | idle vs animating model through real Elmish `Sub` plumbing | always-tick / never-tick both fail one direction | pass | — | idle ⇒ `Sub.none`; active ⇒ dispatches `[AnimationTick interval]` | local-deterministic | — | false |
| damage-rect.union-correctness | discriminating-correctness | `Audit: unionArea counts a genuine overlap ONCE — returns the union, not the naive sum (T026)`; `Audit: unionArea matches an independent brute-force union across 200 generated rect sets`; `Audit: unionArea is clamped to frameArea` | overlap A∪B + 200 generated rect sets vs brute-force union | naive sum (20000 vs union 15000) | pass | true | — | local-deterministic | — | false |
| damage-rect.effectiveness | counter-effectiveness | `Audit: a localized change damages a SMALL fraction of the full-repaint baseline (T033)` | 50-row grid, one row changes, via `ControlsElmish.Perf.runScript` | all-rows-change frame | pass | — | `DirtyArea` 4800 vs full 240000 ≈ 2%; `DirtyRectCount` 1/50 = 2% | local-deterministic | — | false |
| virtualization.effectiveness | counter-effectiveness | `Audit: a 10000-row grid materializes a viewport-bounded window, NOT the total (T034)`; `Audit: the materialized window does NOT scale with the total across 100/1000/10000` | 10000-row data-grid via retained `step` | `VirtualTotal` (10000) | pass | — | materialized 30/10000 ≈ 0.3%; window constant while total scales 100→10000 | local-deterministic | — | false |
| replay-cache.parity | discriminating-correctness | `Audit: US2 parity — cache-on output is byte-identical to the disabled oracle, and the comparison is discriminating (T027)` | offscreen raster `SKSurface` paint, `enabled:true` vs `false` | `enabled:false` (always-direct walk) | pass | true | — | T1 | — | false |
| replay-cache.effectiveness | counter-effectiveness | `Audit: US3 effectiveness — repeated identical boundary paints reach a Hit steady-state (T036)` | 10 identical boundary paints | first paint records | pass | — | Hits 9/10 after one warmup record (Records 1, Misses 1, Entries 1) | T1 | — | false |
| present-mode.liveness | harness-timing | `artifacts/harness/run-20260614-224608/T1/run.json` (offscreen, status passed); `artifacts/harness/run-20260614-224617/run.json` (live-x11, status passed) | offscreen pixel readback + live window present | — | pass | — | T1 renderer-pixels + T2 window/visibility/focus both `passed` | T2 | — | false |
| frame-rate-cap.timing | harness-timing | `artifacts/harness/run-20260614-224614/run.json` (perf paced-60, 100 frames, status passed) | `perf --mode paced-60 --frames 100` | uncapped budget 16.6 ms (60 fps) | pass | — | p50 2.684 ms / p95 3.098 ms / p99 3.472 ms ≪ 16.6 ms budget (headroom confirmed) | T3 | vsync-faithful cadence is `notAuthoritativeFor` the T3 throughput proof — see report | false |

## Findings carried to the report

- **memo-cache disabled-path counter semantics (minor / cosmetic).** With `MemoEnabled=false`
  the wired `step` bypasses the `memoize` seam and recomputes directly *without* incrementing
  `MemoMisses`, so the disabled oracle reports `MemoHits=0, MemoMisses=0` (0/0), not "every node
  a miss" as the `.fsi` comment phrasing implies (`RetainedRender.fsi:172`/`182` describe the
  net visual effect). Observable parity and effectiveness both hold; only the disabled-path
  *counter* narrative is overstated. Recorded as a cosmetic finding (recommendation: re-scope the
  comment). The picture cache and text cache disabled paths *do* re-miss correctly (contrast).
- **`SceneEvidence.renderHash` is alpha-insensitive (out-of-audit-scope note).** An opacity-only
  change (fill alpha 255→128) left `renderHash` unchanged. Not a defect of any audited mechanism
  (the Animation seam itself is correct — structural equality is the authoritative oracle and was
  used); flagged for a possible follow-up audit of `renderHash` resolution if pixel-level opacity
  evidence is ever relied upon.
- **frame-rate-cap vsync fidelity deferred-in-part.** The T3 `perf` harness is authoritative for
  offscreen render throughput (which has ample headroom) but explicitly `notAuthoritativeFor`
  vsync-faithful pacing; the strict "cadence locked to the cap by vblank" sub-aspect remains
  deferred to a vsync-authoritative tier. The throughput-budget claim is verified.
