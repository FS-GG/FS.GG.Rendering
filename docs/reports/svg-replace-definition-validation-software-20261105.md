# SVG Replace definition-validation software comparison

Date: 2026-11-05. Delivery [Rendering #1496](https://github.com/FS-GG/FS.GG.Rendering/issues/1496), parent [.github #4645](https://github.com/FS-GG/.github/issues/4645).

The candidate's relative software regression verdict is **pass** at the unchanged calibrated tolerance of 10.00%. None of the 20 cells improved beyond tolerance. At 250 instances with 10% sparse motion, the median paired p50 ratio was 0.998733 (0.13% lower) and candidate p95 ranged from 68.9–80.6 ms. The licensed Home B5 budget remains unevaluated.

## Change and scope

A host-owned `SvgDocument.createExporter` reuses validation metadata for the last successfully exported immutable definition set. Changed definitions receive full checks. Each call still validates document headers, instances, references, cross-definition IDs, combined node/path limits and instance-dependent expansion limits. Refusals cannot certify a definition set; disposal releases the host validation closure. Stateless `exportSvg` retains its existing contract.

Complete SVG export, candidate parsing and DOM reconciliation remain in the update path. The new API is a source/compiled surface addition; the scaffold mirror continues to describe its actual published 0.32.4 pin.

## Same-profile comparison

Baseline `1ae34fcaf263e8d99c8e895abb82cc612b170ef2`; candidate `eee0693db4ee0ed76b897889a1384b50398b3add`. All 139 manifested source paths per arm matched their respective committed bytes. Their path sets are equal and only `SvgDocument.fs`, `SvgDocument.fsi` and `SvgBrowser.fs` differ. The harness and workload bytes match. The normal pack/private restore/Fable compile/Vite/sealing route produced both artifacts; each arm verified served manifest and file hashes before and after execution.

Profile: AMD Ryzen 9 7900 12-Core Processor as reported by `/proc/cpuinfo`; Linux-7.2.9-arch1-1-x86_64-with-glibc2.44; SDK 10.0.403/runtime 10.0.14; Fable 5.26.0; Node v26.11.1; Playwright 1.64.1; Vite 8.3.5; Chromium 153.0.8010.12 with `--disable-gpu`, 1280×720 viewport and deviceScaleFactor 1. Browser/driver affinity was CPU 1; static servers used CPU 0. The execution scope enforced two CPUs, 2 GiB memory and zero swap. No unrelated task-owned compilation ran during timing; host-wide exclusivity is not established.

Browser executable SHA256: `sha256:ded93a9c9a53a1ae040f08124badcca95c938e9d5015ff340c3b5538c41bf39e`. The existing read-only user library bundle was identified by a 42-file hash inventory. The observed scope peak during the campaign was 1361629184 bytes (an in-flight observation, not a final lifetime peak).

The public circle has two faction symbol definitions, per-unit health bars, selection outlines on every tenth unit, and one camera group. Twenty cells cover shared symbols at 100/250/500 and expanded geometry at 100, each with static, camera-only, sparse 10%, sparse 50% and churn 10% states. The 500 count is saturation. Each record uses 20 warmups and 100 measured ticks. Three independent pairs alternate arm order baseline-first/candidate-first/baseline-first and record order forward/reverse/forward. The five-minute sustained phase is outside this focused update-CPU window.

The complete campaign retained 24,000 measured updates and 4,800 warmup updates across twelve fresh-browser arms (six A/A, six A/B), with 20 records per arm. The synchronous timed span includes F# document construction and compiled Replace; state generation, DOM/export oracles and frame waits are outside it. These are elapsed main-thread update measurements, not separately sampled OS CPU time or style/layout/paint/raster/compositor attribution.

A/A's largest median-of-paired-median deviation was 3.310% (shared-symbol/500/churn-10); largest individual trial deviation was 13.924%. The existing rule max(2 × spread, 10%) selected 10.00%. All 20 calibration and 20 comparison records exceeded the timing-resolution floor; no threshold was changed to fit results.

## B5 reference at 250 instances

The [original budgets-v1 decision](https://github.com/FS-GG/.github/issues/4645#issuecomment-6359585794) defines B5 as update CPU p95 ≤ 8.33 ms at 250 on Home, using the licensed tactical/rifleman shared-symbol workload. The public-circle software geometry and execution profile differ. The arithmetic ratios below show distance from 8.33 ms; they are not a Home B5 verdict.

| Motion | Baseline p50, trials 1/2/3 (ms) | Candidate p50 (ms) | Baseline p95 (ms) | Candidate p95 (ms) | Candidate p95/8.33 range |
| --- | --- | --- | --- | --- | --- |
| static | 36.650, 37.450, 36.750 | 37.200, 39.600, 38.550 | 73.800, 73.000, 76.200 | 65.300, 75.700, 70.400 | 7.84–9.09× |
| camera-only | 37.300, 38.950, 37.350 | 38.000, 40.600, 39.050 | 71.000, 78.100, 74.800 | 63.400, 68.800, 70.100 | 7.61–8.42× |
| sparse-10 | 39.600, 39.450, 36.350 | 38.850, 39.400, 39.300 | 72.100, 69.500, 76.500 | 72.300, 68.900, 80.600 | 8.27–9.68× |
| sparse-50 | 38.250, 39.850, 40.200 | 38.450, 40.450, 39.300 | 73.600, 74.600, 79.800 | 64.400, 67.500, 67.800 | 7.73–8.14× |
| churn-10 | 40.350, 42.250, 41.100 | 40.150, 41.550, 42.200 | 81.600, 82.400, 78.600 | 69.800, 76.800, 77.000 | 8.38–9.24× |

Largest synchronous update in each 250-instance trial (ms):

| Motion | Baseline maxima, trials 1/2/3 | Candidate maxima, trials 1/2/3 |
| --- | --- | --- |
| static | 81.100, 82.100, 84.100 | 88.800, 88.400, 81.300 |
| camera-only | 85.100, 84.700, 82.100 | 68.000, 92.800, 81.100 |
| sparse-10 | 86.500, 89.700, 94.400 | 86.300, 92.500, 93.800 |
| sparse-50 | 88.800, 93.000, 98.600 | 85.600, 76.500, 77.600 |
| churn-10 | 114.700, 114.100, 103.500 | 94.700, 95.100, 99.100 |

All p95s use nearest rank over 100 samples per trial. Raw samples remain in the owner-retained execution bundle; no p99 or sustained-lifetime acceptance is inferred.

## Relative results for all cells

| Cell | Median paired p50 ratio | Ratios, trials 1/2/3 |
| --- | --- | --- |
| shared-symbol/100/static | 1.017 | 1.040, 0.997, 1.017 |
| shared-symbol/100/camera-only | 0.990 | 0.983, 0.990, 1.020 |
| shared-symbol/100/sparse-10 | 1.000 | 1.003, 1.000, 0.981 |
| shared-symbol/100/sparse-50 | 1.014 | 1.014, 1.020, 0.997 |
| shared-symbol/100/churn-10 | 1.012 | 1.007, 1.012, 1.025 |
| shared-symbol/250/static | 1.049 | 1.015, 1.057, 1.049 |
| shared-symbol/250/camera-only | 1.042 | 1.019, 1.042, 1.046 |
| shared-symbol/250/sparse-10 | 0.999 | 0.981, 0.999, 1.081 |
| shared-symbol/250/sparse-50 | 1.005 | 1.005, 1.015, 0.978 |
| shared-symbol/250/churn-10 | 0.995 | 0.995, 0.983, 1.027 |
| shared-symbol/500/static | 1.012 | 1.024, 0.989, 1.012 |
| shared-symbol/500/camera-only | 1.007 | 0.995, 1.007, 1.024 |
| shared-symbol/500/sparse-10 | 0.991 | 0.988, 1.009, 0.991 |
| shared-symbol/500/sparse-50 | 1.015 | 1.012, 1.015, 1.030 |
| shared-symbol/500/churn-10 | 1.001 | 1.001, 0.977, 1.019 |
| expanded-geometry/100/static | 1.017 | 1.017, 0.922, 1.059 |
| expanded-geometry/100/camera-only | 1.008 | 1.008, 0.938, 1.040 |
| expanded-geometry/100/sparse-10 | 1.017 | 1.017, 0.915, 1.048 |
| expanded-geometry/100/sparse-50 | 1.023 | 1.033, 0.969, 1.023 |
| expanded-geometry/100/churn-10 | 1.024 | 1.024, 0.957, 1.027 |

Gate regressions: none. Saturation regressions: none. Cells improving beyond tolerance: none.

Existing oracles checked ordered populations, surviving node identities, transforms, faction symbol references/colours, health bars, selection outlines, live/export structure, stable source-node populations and root removal on disposal. These checks do not establish painted-pixel, assistive-technology, GPU or heap-retention acceptance.

## Local correctness and controls

The full solution build passed with zero warnings/errors. All 155 Scene tests and 533 Package tests passed, with zero skipped, failed or errored tests. The Scene-only roster restriction of the canonical surface generator added exactly one member and no types; it used the unchanged shared reflection/formatting code. The published API teaching manifest remains unchanged.

Six separately compiled source mutants were killed by one intended assertion failure each: bypassed definition equality, certification after a failed export, omitted cached element IDs, omitted semantic IDs, omitted definition node counts and omitted definition path-segment counts. Source was restored, recompiled and all 19 focused document tests passed; the restored assembly differs from all six mutant assemblies. The first controller attempt selected zero tests through the hierarchy filter and was refused, not counted as a killed control. The corrected documented case filter executed one test per mutant; the original attempt is retained.

The additional maintained Chromium correctness runner passed with an explicit Chromium 153 executable and task-private font configuration/cache. It preserved the 22-case byte corpus, four definition-isolation causal controls and 18 appearance causal controls, plus the existing focus, camera, session, export and disposal assertions. Its original caller-digest fields remain `unavailable`; a supplementary source join verifies all 14 sealed distribution files and records the actual launch-wrapper and font-configuration hashes. This local profile does not replace the normal pinned-browser and coherent CI evidence.

The default browser launch first refused because the Playwright-pinned headless shell 1248 was absent. With explicit shell 1243, the first functional run then failed text hit testing. The unchanged baseline and candidate both had zero-sized text and no platform fonts. A task-private configuration pointing at existing read-only bundled fonts gave both the same text bounds, eight fallback glyphs and the expected semantic hit. The complete assertions then passed without source or tolerance changes. This font correction applies to the separate functional run; the performance table retains its original ambient-font software profile. All original failures and causal observations remain in the execution bundle.

## Evidence identity and remaining acceptance

| Arm | Manifested-source SHA256 | Packaged artifact ID |
| --- | --- | --- |
| baseline | `sha256:62639c91755b1c43896db9633f988f641d2d265d6f96ca8c5ac70a131738119b` | `sha256:bc03fcd9b949e28858860ff43c704c37bde3fa0a458b6372e0ad192ed9404747` |
| candidate | `sha256:b311eae9cb2ee1a5d59af91df21815101f9ad18719ffafc369c50205b0cade1b` | `sha256:5b716120a95255843c8f8c8a59057fd756707f91fc575f40cd4ca11a76227aea` |

The local fixture package version 0.32.0 is the existing harness coordinate; its per-arm archive hashes and source joins identify these private builds. Counts and summary values are generated from the actual run JSON and checked by the unchanged analysis module. The owner-retained raw/control bundle is `evidence-cpu-and-controls.tar.gz` (271,658 bytes), SHA256 `6e186777c32f6ad6a637caeff5d65fb03ecd3f27f7e95dd97b80db72cf9cd788`.

The first SDK lookup refused because the ambient installation lacked 10.0.403. Its original transcript is preserved, with an explicitly labelled transcription note. Selecting an existing exact user SDK corrected the lookup. The first browser launch then failed before samples because native libraries were missing; the original loader failure is retained. Selecting the existing read-only user library bundle corrected that launch without a global installation.

Independent exact-candidate review, required/coherent CI, merge/readback and scoped native #1496 acceptance remain pending. The complete owning plan remains governing: realistic licensed workloads, Home/GPU B1–B8 qualification, actual-player/native/accessibility assertions, coherent publication and installed/retained adoption remain with parent #4645. This report does not close those promises.
