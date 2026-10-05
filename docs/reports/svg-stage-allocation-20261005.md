# Composition stage and sampled-allocation qualification

Validation/export dominated instrumented Replace CPU for the accepted optimized 250-instance shared-symbol composition: 13.40 ms, or 63.71% of enclosing CPU. A separately accepted heap-sampling window also attributed most profile weights to export call chains, while revealing substantial fixture hashing and oracle work. These scoped diagnostics extend the [sparse-motion/count evidence](svg-sparse-motion-count-20261005.md). They select no optimization architecture and do not close the [owning .3/.4 milestones](https://github.com/FS-GG/.github/blob/main/docs/roadmaps/svg-coherence-and-instancing-01.md).

## Exact workload and stage scope

Both profiles use the same merged aligned implementation delivered by [Rendering #1386](https://github.com/FS-GG/FS.GG.Rendering/pull/1386). Original generated modules remain exact; two private copied modules add four passive synchronous spans to the probed profile. No DOM prototype hooks, cache, producer API, document limit or fixture cap changed. The fresh diagnostic artifact is shared by stage and allocation windows; its bundles differ from the earlier uninstrumented comparison artifact.

The workload remains 250 shared instances, 25 changed poses per tick and the same licensed composition, stream and independent controls. Each window passed six records, 600 measured and 120 warm calls, 28 exact fault controls, four counter controls and nine paired witnesses. Each producer profile retains zero unchanged attribute writes, zero aligned child moves and the real reorder fallback. Expanded geometry at count 250 remains unsupported under the default producer node limit.

The stage window alternates original/probed order across three repetitions. Each span sum fits enclosing update CPU. Pooled means below describe the 300 measured probed calls; percentages use their enclosing mean CPU of 21.034 ms.

| Instrumented span | Mean ms | Share of enclosing CPU |
|---|---:|---:|
| Consumer decode/document construction | 0.074 | 0.35% |
| Portable validation/export | 13.401 | 63.71% |
| XML parse/import | 5.335 | 25.36% |
| Live reconciliation | 2.206 | 10.49% |

Original mean CPU was 23.217 / 21.021 / 20.945 ms; probed mean CPU was 21.102 / 21.046 / 20.954 ms. Paired differences of −2.115 / +0.025 / +0.009 ms prevent a stable instrumentation-overhead estimate. No difference is subtracted to invent uninstrumented stage timings. The probed p95 ranged from 23.5 to 25.2 ms; repetition 2 maximum worsened to 32.3 ms from original 31.8 ms. Update CPU still exceeds 16.67 ms. Older rifleman fractions remain separate evidence.

The software profile remains full Chrome 153.0.8010.12 with `--disable-gpu`; these are neither GPU nor physical-presentation measurements.

## Sampled attribution includes the fixture

Allocation sampling ran separately on the same artifact with a fixed 1 MiB average interval, stack depth 64 and major/minor collected-object inclusion requested. It covers the whole owned page queue: both profiles, positive/negative controls, XML/export/live oracle work, identity checks, hashing and stage probes. The accepted profile contains 6,198 nodes and 16,818 samples; its serialized size is 1,984,511 bytes. The complete compact result is 3,460,439 bytes.

The following categories sum each profile node's `selfSize` weight once. A node takes the first matching observed ancestor category in table order; anonymous/builtin frames inherit that call-chain context. These shares are sampled attribution estimates across the whole queue, not measured total allocation or allocation per Replace.

| Exclusive observed call-chain category | Share of profile self-size weights |
|---|---:|
| `exportSvg` beneath browser-host Replace | 70.21% |
| `exportSvg` outside browser-host Replace | 2.86% |
| Host Replace excluding export | 2.42% |
| Fixture document construction excluding export/Replace | 0.61% |
| Fixture hashing | 9.52% |
| Normalizer/oracle | 12.88% |
| Other/unclassified | 1.51% |

An inclusive 34.08% of profile weights lie on chains containing `SvgDocumentModule_encodedId`, within the export category. Observed descendants include string replacement, iteration and anonymous generated functions. [Exported ID construction](../../src/Scene/SvgDocument.fs) repeats namespace/document/local encoding; [browser Replace](../../src/Scene.SvgBrowser/SvgBrowser.fs) validates/exports before parsing and reconciliation. This supports inspecting export/encoding work before selecting the next optimization; it does not establish a cache contract or attribute export's entire CPU span to ID encoding.

Endpoint used JS heap changed from 5,627,312 to 18,303,600 bytes, nodes from 27 to 207,360, documents from 4 to 81 and listeners from 15 to 37. No forced GC or matched post-GC endpoint was taken. These owned-target observations do not establish leaks, retained producer memory, total allocation, GC counts/pauses, native memory or GPU memory. Per-case disposal and owned-process retirement passed independently.

## Preserved capture failures

| Allocation attempt | Result under unchanged bounds |
|---|---|
| 32 KiB interval | Profile exceeded 4 MiB; actual size/profile not retained. Page semantics passed independently. |
| 256 KiB interval | Profile was 5,212,422 bytes, exceeding 4 MiB. Profile not retained; page semantics passed independently. |
| 1 MiB, pretty JSON | Driver cleared the profile/page validators and artifact postcheck, then final pretty JSON exceeded 8 MiB. Actual payload/profile sizes were lost; only the failed size receipt survived. |
| 1 MiB, compact JSON | Accepted complete profile and every result field under the same 4 MiB profile / 8 MiB final bounds. |

Each successor was separately admitted. No automatic retry, cap increase, profile truncation, workload reduction or silent fallback occurred. The successful compact serializer changes formatting only. Synthetic controls demonstrated 13,485,508 pretty bytes versus 6,084,074 compact bytes with identical parsed fields; that ratio does not estimate the lost native payload. Every failed result and cleanup remains retained. All owned browser/server/driver processes retired, and source/artifact invariance passed for the accepted windows.

## Evidence and remaining decisions

Licensed art, XML, command payloads and raw profiles remain private and excluded from producer/package payloads. Exact retained identities are:

| Evidence | SHA-256 |
|---|---|
| Accepted stage result | `068dad984ef527c4c70fc9bab2bf40265a686afa3ba895fb79a7ede20ec0a818` |
| First allocation profile-size refusal | `9395bd0e83b17d7020a56f0ddd122098d0029183d44cb3cf38ccf9d6e32bb5e8` |
| Coarse allocation profile-size refusal | `4678fc5bef0b7001ab5371b466c4e648bfc742ce6e9085d583c232b5f1142a63` |
| Pretty-result size refusal | `5d93e2442a4d2d3232c0b31f51663de8ab0401c7e3f7bd9d7564e662f2ed00f5` |
| Accepted sampled allocation result | `ff91b340fb448565cfd043b18073321724b578ac1b754d2fcca904377f0e20af` |

Both accepted windows use artifact `b0fdc83333bdb0d26dea13353dd993a5f37e65955e6f8762296a357b5bfc72bf`. This supplies selected post-optimization composition stage and whole-fixture sampled-allocation evidence. Milestone .3 retains remaining count/dynamic-composition coverage and any allocation/GC questions outside this scope. Milestone .4 still requires an explicit instance-update/index/cache architecture decision and compatibility evidence. No further producer implementation is selected here. Existing reference/product, package-candidate, late host/GPU and publication/adoption sequencing remains unchanged; telemetry coverage remains unknown because the parent token was unavailable.
