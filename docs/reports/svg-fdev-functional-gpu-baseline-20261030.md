# fdev virtio-gpu functional-GPU and early-baseline profile — 2026-10-30

**Label.** This is the *fdev virtio-gpu functional-GPU and early-baseline profile*. It is **not B1–B8 evidence**, it is **not bare-metal Home**, and the r1.3 outcome **stays Unknown**. It follows the Root/fdev ruling of 2026-10-30 on [FS-GG/.github#4645](https://github.com/FS-GG/.github/issues/4645) (distinct functional-GPU and early-baseline profile; GPU-path correctness and diagnostics only). No budget verdict is computed and no threshold is touched. The plan's [§5 virtual-display and host-main route rules](https://github.com/FS-GG/.github/blob/main/docs/roadmaps/svg-coherence-and-instancing-01.md#5-container-served-fixtures-host-main-browser-execution) still decide the .7 batch.

## Question and short answer

Does the existing page suite pass under a Chromium that really uses the fdev GPU, and is SVG rasterization hardware accelerated there?

- All nine suite runs (three profiles × three interleaved trials) completed the maintained 17-case controls suite: 153 of 153 cases passed. All six suite mutants were killed on both GPU profiles.
- **Yes, observed:** with explicit ANGLE flags, Chromium 151 rasterizes the page on the GPU. `chrome://gpu` reports *Rasterization: Hardware accelerated on all pages* and *Compositing: Hardware accelerated*; a separate trace window shows GPU-process raster decoder work (481 `RasterDecoderImpl::DoRasterCHROMIUM` events for one 120-tick record) and zero such events in the software control. This establishes the GPU raster path for the page's SVG content. It does not establish presentation to a display, per-frame GPU time, or any budget.
- **Without those flags, headless Chromium here is software only** (SwiftShader). Merely omitting `--disable-gpu` is not a GPU run.
- JavaScript update CPU (the timed span) is indistinguishable across the three profiles within the noise of a shared, loaded VM. That is expected, because the timed span is compiled F# plus DOM mutation on the main thread; GPU work sits in raster and composite, which the timed span does not include.

## Environment

| Item | Observed |
| --- | --- |
| Host | fdev, a KVM guest (Arch Linux, kernel `7.2.9-arch1-1`), 12 vCPU, 32 GiB, shared with other workers. 1-minute load average at the start of each profile run: 4.05–13.31 (see below). |
| GPU device | `virtio-pci` 1af4:1050 virtio-gpu, `/dev/dri/renderD128`, DRM native context onto the Home host's AMD iGPU. Vulkan reports `AMD Ryzen 9 7900 12-Core Processor (RADV RAPHAEL_MENDOCINO)`, `PHYSICAL_DEVICE_TYPE_INTEGRATED_GPU`, Vulkan API 1.4.354, Mesa 26.2.4 (RADV), `radeonsi` 26.2.4 via EGL (`DRM 3.64`). |
| Display | None. Headless (`--headless` Playwright default, ozone headless). No scanout or compositor-to-display path was exercised. |
| Browser | Chrome for Testing `151.0.7922.34` (Playwright `1.62.1` selection `chromium-1234`), executable sha256 `0b20b130e7edd9dd51873be867761295fe0cfad490c2b9a64f95bd3cfc08fa71` (the same executable digest as the Home r1 proposal). Playwright-launched, sandbox default (no `--no-sandbox`). |
| Fixture | Rendering `cd2ccd66` plus this PR's test-only changes, built by `build-fixture.sh` (SDK 10.0.401), artifact `sha256:830fe852b7e40c11f2e09f5ee9e50134ae05cfca88fd302b38a0570d2fe12eff`, `artifact-manifest.json` sha256 `307148033977f55a6395a8122ff7deaaaefa27306a9055d8af8ef443e477d266`, served by `serve-fixture.mjs` on loopback. |
| Harness | `drive-suite.mjs` (controls profile, 17 cases, mutants), `fdev-gpu-profile.mjs` driver `sha256:ee0beadc…75f05`, page `sha256:4ca93313…1224`, workload `sha256:e7f601f7…dfc4` (the hosted-benchmark page and workload; unchanged). |
| Limits | Each session ran under `systemd-run --user --scope -p CPUQuota=200% -p MemoryMax=4G`, with `timeout` on every driver. Unlike the hosted software benchmark there is **no one-logical-CPU affinity**. |
| Host libraries | Chromium needed 12 shared libraries that fdev lacked (`nss`, `nspr`, `at-spi2-core`, `libcups`, `libx*`), plus a font set. They were fetched with `pacman -Sw` (download only) and unpacked privately under the operator's private directory with `LD_LIBRARY_PATH` and a private `FONTCONFIG_FILE` (`ttf-dejavu`). Nothing was installed system-wide. A bare `fc-list` on fdev is empty, which crashes Chromium's own `chrome://` pages. |

## Profiles

| Profile | Chromium arguments |
| --- | --- |
| `software` (control) | `--disable-gpu` |
| `gpu-angle-vulkan` | `--use-angle=vulkan --enable-features=Vulkan --ignore-gpu-blocklist --enable-gpu-rasterization` |
| `gpu-angle-gl-egl` | `--use-angle=gl-egl --ignore-gpu-blocklist --enable-gpu-rasterization` |
| `default-flags` (diagnostic only) | none beyond Playwright's defaults |

`--enable-gpu-rasterization` forces rasterization on (the status reads `enabled_force`). A GPU-rasterization decision *without* that flag was not separately tested; see Unknowns. Playwright also adds `--enable-unsafe-swiftshader`, so a software fallback remains available to Chromium; the diagnostics below show which backend was actually active.

## GPU diagnostics (`chrome://gpu` text and CDP `SystemInfo.getInfo`)

| Status line | `software` | `gpu-angle-vulkan` | `gpu-angle-gl-egl` | `default-flags` |
| --- | --- | --- | --- | --- |
| Canvas | Software only | Hardware accelerated | Hardware accelerated | Software only |
| Compositing | Software only | Hardware accelerated | Hardware accelerated | Software only |
| **Rasterization** | **Software only** | **Hardware accelerated on all pages** | **Hardware accelerated on all pages** | **Software only** |
| OpenGL / Vulkan | Disabled / Disabled | Enabled / Enabled | Enabled / Disabled | Disabled / Disabled |
| WebGL | Software only | Hardware accelerated | Hardware accelerated | Software only |
| Skia backend | None | GaneshVulkan | GaneshGL | — |
| ANGLE display | `ANGLE_SWIFTSHADER` | `ANGLE_VULKAN` | `ANGLE_OPENGL_EGL` | SwiftShader |
| WebGL renderer (page-visible) | `SwiftShader Device (Subzero)` | `AMD … (RADV RAPHAEL_MENDOCINO)` | `AMD … (radeonsi raphael_mendocino ACO, Mesa 26.2.4)` | SwiftShader |
| GPU process | present | present | present | present |

Chromium also reports `In-process GPU: false` in every profile and `Sandboxed: false` for the GPU process in every profile, including software. The cause of the latter was not investigated. The GPU device Chromium lists is the virtio PCI id (`0x1af4:0x1050`) with the ANGLE renderer string of the AMD driver behind it; the physical AMD device id is visible only through Vulkan.

## Is SVG rasterization hardware accelerated?

**Yes in the two GPU profiles, with two independent observations; no in the software control and in default flags.**

1. *Observed, browser's own status:* `Rasterization: Hardware accelerated on all pages` (CDP feature `rasterization: enabled_force`), Skia on the GPU backend.
2. *Observed, trace:* a separate 120-tick `shared-symbol/250/sparse-10` record was traced with categories `cc,gpu,viz,disabled-by-default-cc.debug` after the timed records. Counts per profile in each of three trials:

| Profile | `GpuRasterBuffer::Playback` | GPU-process `RasterDecoderImpl::DoRasterCHROMIUM` | `ZeroCopyRasterBuffer::Playback` |
| --- | ---: | ---: | ---: |
| `gpu-angle-vulkan` | 481, 481, 481 | 481, 481, 481 | 0 |
| `gpu-angle-gl-egl` | 481, 482, 481 | 481, 482, 481 | 0 |
| `software` | 0 | 0 | 1,057 (trial 1) |

   The software control rasterizes in the renderer's tile workers through the CPU raster buffer; the GPU profiles issue the same tiles to the GPU process.

*Inferred, not observed:* that this means the page's SVG elements are painted by the GPU (Chromium rasterizes SVG through the same display-list and Skia path as other content). *Not observable here:* GPU execution time per frame, GPU memory, compositor-to-display presentation and device utilization (the guest cannot see the host iGPU's counters).

## Controls and results

| Control | Result |
| --- | --- |
| Maintained controls suite (`drive-suite.mjs --profile controls`), 17 cases, three trials in three interleaved orders for each of the three profiles | 9 of 9 runs `container-controls-completed`, integrity `verified-before-and-after`; 153 of 153 cases passed |
| Suite mutants `unnecessary-rebuild`, `listener-leak`, `excessive-document-acceptance` on each of `gpu-angle-vulkan` and `gpu-angle-gl-egl` | 6 of 6 killed by their named gates (`unnecessary-rebuild gate`, `listener/resource-leak gate`, `excessive-document gate`) |
| Public-circle records (20 per run: shared symbol 100/250/500 and expanded geometry 100, each static / camera-only / sparse-10 / sparse-50 / churn-10; 20 warm-up and 100 measured ticks), 9 runs | 180 of 180 records completed with their oracles; no oracle failure |
| Profile status gate | `software` and `default-flags` report software raster; both GPU profiles report hardware raster in every trial |

Trial orders (profile sequence): software, vulkan, gl-egl; vulkan, gl-egl, software; gl-egl, software, vulkan. One-minute load average at each profile's start ranged from 4.05 to 13.31 because other workers were active on fdev; the highest values belong to the third trial. These are *not* quiet-host conditions.

## Observations (milliseconds; median over three trials of each trial's 100-sample nearest-rank p50 / p95, range of the trial values in brackets)

These are raw descriptive observations on a loaded virtual machine. They are not budget verdicts, and no threshold is applied.

| Record | `software` update p50 / p95 | `gpu-angle-vulkan` update p50 / p95 | `gpu-angle-gl-egl` update p50 / p95 |
| --- | --- | --- | --- |
| shared 100 sparse-10 | 14.70 / 20.40 | 14.60 / 19.60 | 14.75 / 20.90 |
| shared 250 static | 38.85 / 51.20 | 36.40 / 47.90 | 36.75 / 50.60 |
| shared 250 camera-only | 38.55 / 48.60 | 37.20 / 45.20 | 36.65 / 45.00 |
| shared 250 sparse-10 | 37.00 / 45.80 | 37.90 / 45.60 | 35.55 / 45.90 |
| shared 250 sparse-50 | 36.80 / 44.90 | 38.10 / 47.20 | 35.30 / 42.30 |
| shared 250 churn-10 | 39.55 / 53.40 | 40.50 / 54.50 | 37.85 / 48.50 |
| shared 500 sparse-10 (saturation) | 83.00 / 96.90 | 73.00 / 89.60 | 83.20 / 106.30 |
| expanded 100 sparse-10 | 13.55 / 19.00 | 12.00 / 15.50 | 12.30 / 17.20 |

Per-trial ranges are within a few milliseconds of these medians for the 100- and 250-unit records and are retained in the private raw results; the 500-unit saturation record's trial p50 values span 71.9–91.0 across all profiles and trials. The timed span ("harness F# document construction plus synchronous compiled Replace") excludes raster and composite, so its near equality across profiles is the expected result and says nothing about GPU benefit.

The `requestAnimationFrame` intervals sit on 16.7 ms multiples in every profile (p50 16.7 ms at 100 units; 33.4 ms at 250; 83 ms at 500), consistent with a 60 Hz virtual vsync in headless mode and with frame production being gated by the update cost. The frame cadence observation does not demonstrate display presentation.

## Licensed glyph

The licensed 24-node tactical composition / rifleman glyph was **not located** in fdev's private custody by a bounded search (the operator's private work directories and an Atlas search; no recursive scan of the bulk disk). The earlier private research directory `programme-resume-20261009` is not present in fdev. Everything above therefore uses the **public circle fixture only**. The Templates MIT original soldier is exercised by the Templates composition lane, not by this profile. No licensed geometry, hash of it or derived number appears in this report, the repository or any workflow. Licensed-glyph rows remain Unknown until the owner supplies the asset in fdev.

## Reproduction

```sh
# private scratch only; fdev lacks Chromium's native libraries, so load them from a private directory
export PLAYWRIGHT_BROWSERS_PATH=<private>/browsers LD_LIBRARY_PATH=<private>/libs/usr/lib FONTCONFIG_FILE=<private>/fonts.conf
(cd tests/Scene.SvgBrowser.Tests && npm ci && npx playwright-core install chromium)
systemd-run --user --scope -p CPUQuota=200% -p MemoryMax=4G \
  bash tests/Scene.SvgBrowser.Tests/build-fixture.sh <private>/fixture
systemd-run --user --scope -p CPUQuota=200% -p MemoryMax=4G \
  bash tests/Scene.SvgBrowser.Tests/run-fdev-gpu-profile.sh <private>/session <private>/fixture/Browser/dist <chromium-executable> 47820
```

Source changes: `drive-suite.mjs` gains an explicit `--browser-args-json` plus mandatory `--profile-label` (the default remains `--disable-gpu` and the historical profile name); `fdev-gpu-profile.mjs` and `run-fdev-gpu-profile.sh` are new. The session runner's first mutant check inverted its own pass/fail test (the driver exits 0 when a mutant is killed); that is repaired in the committed runner, and the six kills were read from the driver's own `"result":"killed"` output, which is retained in the private logs.

Private evidence (not committed): 64 files, index `SHA256SUMS` sha256 `e59f52b61e9de5c4511def20fdb3deaa465f058ee622f02f88b4c1ca856efabd`, under the operator's private fdev directory. An earlier identical session was lost when fdev restarted at about 22:00 and was repeated; only the repeat is reported.

## Unknowns

- **B1–B8** and bare-metal Home behaviour (RTX 4070, Wayland, visible display): unevaluated by design. The r1.3 outcome stays Unknown.
- Whether GPU rasterization is selected **without** `--enable-gpu-rasterization` (the flag forces it). Not tested.
- Display presentation, compositor-to-scanout timing, GPU frame time and GPU memory: not observable in this profile.
- Why Chromium reports `Sandboxed: false` for the GPU process here (including in the software control).
- Quiet-host variance: every run shared the VM with other workers, with no CPU affinity.
- Licensed-glyph behaviour: not measured; the asset was not found in fdev.
- Firefox and WebKit GPU behaviour: not exercised.
- Physical input, assistive technology and the actual-player composition: outside this profile.
- Publication, installed adoption and the .7 frozen-packet batch remain open. The 439 inherited and 46 later original Unknown holds are unchanged.
