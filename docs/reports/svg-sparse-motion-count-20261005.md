# Sparse-motion and visible-count software qualification

The merged aligned-child fast path reduced median document-update CPU in all nine matched pairs from the later 10% motion windows: six pairs at 100 instances across shared symbols and expanded geometry, plus three shared-symbol pairs at 250 instances. The 250-instance candidate still exceeded 16.67 ms of update CPU, and individual tails regressed. These measurements extend the [original aligned-child qualification](svg-aligned-child-source-20261005.md); they do not close the [owning SVG milestones](https://github.com/FS-GG/.github/blob/main/docs/roadmaps/svg-coherence-and-instancing-01.md).

The guarded baseline is `6b661cb44d763661989ecebe402a736546783804`. The measured aligned implementation is `237f66bcce46c9e6227a266231342b34367b060c`, whose three measured implementation/test files remain byte-identical at final head `e526680462ea53751db445e2d35dd3bb70cf0cde`. [Rendering #1386](https://github.com/FS-GG/FS.GG.Rendering/pull/1386) delivered it at protected merge `68dbd8b845683c613967c7ab4bebe154d7bc5b23`. Later private research reused the exact accepted compiled bundles; no producer API, validation limit or package publication changed.

## Workload and accepted population

Both counts use the same licensed 24-node tactical composition, normalized to a 24-unit proxy in a fixed 720 × 720 SVG viewport. Each Replace changes 10% of the poses: ten at count 100 and 25 at count 250. Count 100 uses a ten-column grid; count 250 uses sixteen columns, both with 30-unit spacing. These are separately frozen workloads with all proxy corners visible; the count comparison also changes occupied layout and is descriptive.

Each representation/count has three repetitions per producer, alternating producer order across repetitions. Every record contains 20 warm and 100 measured Replace calls. Timed hooks are absent from principal CPU measurements; independent XML/export/live checks, semantic order, hit testing, invalid-input atomicity, identity and disposal remain required.

| Window | Records | Measured / warm calls | Exact faults / counter faults | Paired witnesses |
|---|---:|---:|---:|---:|
| 100 shared symbols | 6 | 600 / 120 | 56 / 4 | 27 |
| 100 expanded geometry | 6 | 600 / 120 | 56 / 4 | 27 |
| 250 shared symbols | 6 | 600 / 120 | 28 / 4 | 9 |

The 100-instance windows preflight both representations for each producer. The accepted 250-instance window preflights shared symbols only: both producer suites retain every fourteen-fault control type and the same six positive XML/export/live ticks. Its nine pairs compare the producers at six control ticks and three final repetition ticks. It makes no 250-instance cross-representation claim.

## Update CPU and tails

Values are milliseconds. Each p50/p95 is the nearest-rank quantile of one 100-sample record; short-run p99 values remain descriptive.

| Workload | Repetition | Guarded p50 / p95 | Aligned p50 / p95 |
|---|---:|---:|---:|
| 100 shared | 0 | 12.1 / 16.2 | 9.2 / 11.9 |
| 100 shared | 1 | 12.1 / 13.1 | 9.0 / 9.7 |
| 100 shared | 2 | 11.9 / 12.7 | 9.1 / 9.7 |
| 100 expanded | 0 | 173.3 / 188.7 | 162.2 / 173.3 |
| 100 expanded | 1 | 171.2 / 182.4 | 165.1 / 180.7 |
| 100 expanded | 2 | 171.5 / 183.0 | 163.7 / 178.0 |
| 250 shared | 0 | 29.2 / 35.1 | 20.1 / 21.4 |
| 250 shared | 1 | 27.9 / 33.0 | 20.0 / 21.8 |
| 250 shared | 2 | 28.0 / 31.4 | 20.2 / 21.0 |

At count 250, paired median reductions range from 27.86% to 31.16%. Candidate repetition 0 maximum worsened to 48.4 ms from baseline 37.5 ms despite its lower p95/p99. In the 100-instance expanded window, candidate repetition 0 p99 worsened to 205.2 ms from 196.1 ms; repetition 2 rAF maximum worsened to 233.3 ms from 216.7 ms. These regressions remain part of the result.

Counter preflights observed 799 already-owned child moves in each aligned baseline update at count 250, versus zero for the candidate. Changed snapshots wrote exactly 25 attributes; both producers retained zero unchanged attribute writes. The candidate's reversed snapshot made 250 ordering moves and still passed independent geometry, order, identity and topmost-hit checks. At count 100, the corresponding shared aligned baseline made 349 moves and the candidate zero; changed snapshots wrote ten attributes.

Source DOM counts at the initial preflight are 350 for 100 shared instances, 4,903 for 100 expanded instances and 800 for 250 shared instances. Independent normalized composition counts are respectively 2,100, 2,100 and 5,250 primitives. Source DOM counts do not measure SVG instance shadow trees or GPU work.

The software browser was cached full Chrome 153.0.8010.12, executable SHA-256 `8c599d43aec53f2460a31ae2f4af6bd863f8258b34ff519564bc5d4726bfaa1e`, with `--disable-gpu`. The rAF p50s were 33.3 versus 16.7 ms for count-100 shared, approximately 183.3 versus 166.7 for count-100 expanded, and 66.7–66.8 versus 16.7 for count-250 shared. These callback timestamps include fixture work outside the update CPU span and are not physical display presentation or a frame-budget guarantee. In particular, the 250-instance candidate's 20.0–20.2 ms median CPU exceeds 16.67 ms.

Module/font bootstrap is recorded separately from asset mount and steady-state Replace CPU. Warm-operation counts remain available, but individual warm CPU samples were not serialized. No allocation/GC or post-optimization composition stage fractions were measured. Separate 1% and 10% windows do not prove statistical equality or motion-percentage scaling; earlier rifleman stage fractions cannot be transferred to this composition.

## Preserved refusals and fixture repairs

The first 250-instance attempt completed one shared baseline suite, then expanded geometry failed its first positive mount: producer validation reported `node-limit`, “document exceeds 10000 nodes.” No timing records resulted. The private fixture's separate 12,500-node estimate did not establish acceptance under [the producer's default document limits](../../src/Scene/SvgDocument.fs). No limit was raised. Expanded geometry at this composition/count remains unsupported.

The shared-only successor completed both shared suites and counter preflights, then its first paired witness refused three stale declarations: 2,100 primitives, 100 instances and 100 text witnesses. Both actual surfaces contained 5,250 primitives and 250 instances; every positive preflight recorded 250 text witnesses. No indexed geometry or semantic-order mismatch appeared in that failed pair. A successor changed only the three expected counts and diagnostic labels to 5,250 / 250 / 250. Original comparison fields, matrix tolerance and S7 text precision remain unchanged.

Both failed driver exits and raw results remain immutable. The final shared-only successor passed all required records, faults, pairs, artifact checks and disposal checks. All owned driver/server/browser processes were retired. Cleanup observations apply to those runs; they do not prove general leak freedom.

## Evidence identities and remaining scope

Licensed art, XML, command payloads and raw results remain private and excluded from public package payloads. Retained result SHA-256 identities are:

| Evidence | SHA-256 |
|---|---|
| 100 shared accepted result | `3caec78ddec6e7ee96bfbf8ec6f373a5d71a6c5647d7948b706486a695a6b8cb` |
| 100 expanded accepted result | `be83a0ac50a8dfe12f38de88950fe52b38f8b908a457d5350f092f8dc8e0c3d7` |
| 250 expanded-preflight refusal | `5e9f6ad414eb3b8726d883bad79077111aeca95a9aca019b7a601f40cbfe9a86` |
| 250 paired-count declaration refusal | `e938f1f93d35e92b835a55d722e4d55953b3f7a30b4b34f7fb27c2cee294f794` |
| 250 shared accepted result | `4dedb130d68c18a697e28dfee93ce0d730c0ff47a69433c03f9a41f14d76008a` |

The accepted 100-instance artifact is `dc399e08813e1755a0461323402da1b9dce6ae40a7c86942715a7afec13309a0`; the accepted 250-instance artifact is `506d2195ef610e8bd4bee21406e5e394e62fffdd4a564eb5ddcc72af550f3cd6`. The latter preserves the exact accepted producer bundles while binding the shared-only scope and corrected count declarations.

Milestone .3 still requires its remaining selected count, dynamic-composition and allocation/stage evidence. The complex fixture also refuses count 500 through its own expansion estimate and count 1,000 through its pose cap; those are fixture support boundaries, not measured producer saturation. No cap expansion is selected. Milestone .4 retains its further instance-update/index/cache and compatibility decisions; the results do not select a new API or cache by themselves. Reference and product journeys, package candidates, retained upgrades, the late host/GPU batch and final publication/installed adoption remain with their existing owners and sequence.
