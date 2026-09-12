# SVG-SCENE-02.6 Preview-A performance and resource evidence

Date: 2026-09-12

Rendering base: `70f8fbf2aedacee0f14ce7548423c51c3c026572`

## Frozen workloads and exact reference environment

The checked-in [raw receipt](../../readiness/svg-scene-02-6/preview-a-performance.json) records the
frozen workloads, every latency sample, browser stages, byte categories, environment predicates,
and control outcomes. The accepted run used Arch Linux `20260906.0.587075`, kernel
`7.2.2-arch1-1`, AMD Ryzen 9 7900, 24 logical CPUs, 66,509,987,840 bytes RAM, no swap, the
`powersave` governor, and start/end one-minute load of 0.35/0.81. AC state is unavailable inside
the container and remains separately reported. The browser was Playwright Chromium
151.0.7922.34 in headless software-rendering mode, served by Vite 8.1.5 over loopback HTTP.
The end-load predicate is sampled after the browser and server close and the reference host settles,
so measurement-owned processes are not mistaken for background contention. All exact-reference predicates passed.

The ordinary fixture contains 100 semantic objects across four layers, each with a shape,
12-segment path, short label, and shared gradient/symbol. The dense fixture contains 200 such
objects plus 20 clip/mask groups and a selection overlay. Each workload discarded five warmups,
captured 200 surface-backed samples per run, and repeated three independent runs. The definition
gallery mounted and captured 200 paths with 256 segments each, 64 gradients, 32 masks, 100 text
runs, and 100 symbol instances. Its stage timings are retained for diagnosis, not treated as a
latency budget.

## Accepted measurements

| Predicate | Declared threshold | Exact-reference observation |
| --- | ---: | ---: |
| Ordinary updated-surface p95 | <= 100 ms | 83.505 / 83.517 / 83.516 ms |
| Dense updated-surface p95 | <= 150 ms | 116.905 / 116.884 / 116.913 ms |
| Cold first usable interaction | < 2,000 ms | maximum 214.459 ms |
| Executable JS and CSS | <= 153,600 gzip bytes | 42,294 gzip bytes |
| Idle | 10 seconds, zero work | 0 frames, mutations, and rebuilds |
| Retained updates | no unaffected rebuild | 100 camera + 100 one-object revisions, zero unaffected rebuilds |
| Lifecycle | no owned resource remains | 100 cycles; zero roots, listeners, frames, observers, and font handles |

The executable assets are 147,933 raw bytes. The selected self-hosted Noto Sans font is reported
separately at 13,120 raw / 13,143 gzip bytes, including its exact file and license hashes. The
definition/curve gallery content is also separate at 1,099,499 raw / 177,295 gzip bytes. Gallery
validation/reconciliation took 168.700 ms and the completed pixel capture stage took 257.923 ms;
neither value is folded into the ordinary or dense predicates.

During qualification the gallery exposed duplicate `fill-rule` attributes when a Scene path also
had an identified-document presentation. Browsers parsing as HTML could recover, but standalone
SVG/XML correctly refused the document. Export now gives the presentation sole precedence over
the path fallback, and a .NET XML parse regression plus the full browser gallery prove the repair.

## Negative controls and unavailable dimensions

The unnecessary-rebuild mutant failed at the rebuild gate, the listener-leak mutant failed at the
owned-resource gate, and the excessive-document mutant failed at the node-limit gate while
retaining the existing root. A surviving or wrongly classified control makes the runner fail.

Chromium does not expose a stable cross-family retained-heap measurement for this contract, so
owned lifecycle counters and roots are reported while heap remains unavailable. Headless software
rendering provides updated SVG pixel captures but no physical compositor/display presentation
timestamp. Physical mobile and mobile-GPU behavior were not observed; the existing touch evidence
remains browser emulation. These dimensions are not folded into the aggregate pass.

This accepts the declared Preview-A workload and resource budgets only. It does not claim complete
C19/M9 or physical-mobile qualification. Rendering remains independent of Game; candidate packages
were local, unpublished, and not activated by default. S.I.R. was not accessed.
