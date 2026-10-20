# Public-circle count-scaling software qualification — 2026-10-20

Existing [PR1447](https://github.com/FS-GG/FS.GG.Rendering/pull/1447) and [Delivery1448](https://github.com/FS-GG/FS.GG.Rendering/issues/1448) retain the source and acceptance boundary. Three fresh serial browser trials passed all17 maintained controls each, in shared-first, expanded-first, shared-first order. The downloaded results contain36 complete research state rows and180 raw replacement samples per trial:108 rows and540 samples in total. This establishes the declared software research stream only.

The compiled source candidate was `f4af9350a81d128f6414a25c4cc2240c43cd2703`. The maintained `build-fixture.sh` completed package creation, restore, actual Fable compilation, Vite build and artifact sealing with equal before/after source manifests. The research workload covers100/250/500 public circles, both representations and all six command states, with two warmups and five measured replacement samples per state. The common viewBox is410×510. This changes comparability with older fixtures.

Chromium151.0.7922.34/revision1234, selected by source-pinned Playwright1.62.1, ran headless with `--disable-gpu`, viewport1280×720 and device scale1, with CPU affinity restricted to one logical CPU. Each maintained driver retained its180-second bound including cleanup reserve and the existing30-second case timeout. Observed trial durations were31.51,31.43 and31.64 seconds; the server and browsers reached terminal state. This is a container software observation, with no host-GPU or historical-baseline equivalence.

Artifact: `sha256:e4efc563c601dbc43f1a2925ad6c28f21d43af685dce39595fbab843b399a7ec`. Source-manifest identity: `sha256:62a3aae81b2c17491c6fe2cd0facaf135f2a7d75454226024dca6a7b06718e13`. Browser executable SHA256: `e11fc9ce65c96313476f7ee9844b6fb6a9220fb048693cfe9eee00acf4170a9f`.

## Sparse-motion replacement observations

Values below are five-sample medians in milliseconds; ranges retain the actual minimum and maximum. Replacement timing excludes hit-witness scrolling and other semantic checks. The per-trial ratios compare500 with250 instances on the same producer; the shared/expanded ratio at500 is a representation comparison.

| Trial | Representation | 250 median [min,max] | 500 median [min,max] | 500/250 |
| --- | --- | --- | --- | --- |
| 1 | shared-symbol | 18.90 [12.40,45.80] | 30.10 [25.90,61.20] | 1.59 |
| 1 | expanded-geometry | 9.00 [7.30,13.00] | 18.40 [17.10,54.90] | 2.04 |
| 2 | shared-symbol | 12.60 [12.40,14.70] | 27.80 [24.60,29.30] | 2.21 |
| 2 | expanded-geometry | 9.80 [7.60,15.30] | 16.00 [15.30,19.60] | 1.63 |
| 3 | shared-symbol | 14.50 [12.50,31.40] | 28.90 [24.80,127.40] | 1.99 |
| 3 | expanded-geometry | 7.80 [7.40,10.00] | 17.40 [15.30,20.20] | 2.23 |

Shared/expanded500-instance median ratios were 1.64, 1.74, 1.66. Shared symbols were slower in these sparse-motion samples; no speedup, total allocation, GPU or broader performance acceptance follows. Other state samples and original downloaded results remain in the owning executor’s private evidence custody.

## Failures and repair scope

The original source shadowing failure remains retained. The first freshly compiled trial at the predecessor candidate failed the real instance-hit assertion. Focused browser observations separated two fixture defects: centering the tall SVG root left points outside the viewport; scrolling alone did not repair implicit-symbol clipping at the origin. Expanded geometry hit correctly. Matching explicit symbol/use viewports and target scrolling repaired the witness. The current browser oracle checks viewport mapping, circle radius/style, transforms, live/export equality, population and retained ownership; the transparent viewport proxy means a center hit proves semantic routing rather than independent visible circle paint.

The source suite retains the original shadowing mutant, wrong-hit and missing-scroll refusals. Both current source controls and independent exact-candidate review passed. Earlier NU1403 from the shared package cache remains a failed predecessor; isolated package inputs restored the committed hash without deleting the shared cache or changing its lockfile. Browser-library recovery resolved the original16 missing native libraries.

Required exact-head CI, merge/readback and scoped native closure remain integration requirements at this report’s preparation. The legacy PR metadata-updater refusal remains separate. No publication, installed adoption, complex-glyph scaling or whole-parent completion is claimed. All439 inherited and46 later original Unknown holds remain distinct and unreleased.
