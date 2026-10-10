# SVG whole-page sampled CPU and allocation diagnostics

Date: 2026-10-10. Scope: SVG-COHERENCE-01.3 partial diagnostic evidence.

One accepted CPU-profiler capture and one separately accepted offline classification account for all 8,296 samples. The conservative source-map rules leave 7,268 samples (87.61%) unresolved. A separate accepted heap diagnostic accounts for 2,729 nodes and retains 3,200 original samples but leaves 89.70% of node selfSize weight unresolved. Neither diagnostic distinguishes validation traversal from successful SVG serialization precisely enough to select an optimization. The owning .3/.4 milestones remain incomplete.

The capture uses the repaired local 0.4.0-preview.1 producer and world-preserving fixture from the [software screen](svg-instance-stream-software-20261010.md), with the [passive stage diagnostic](svg-instance-stream-stages-20261010.md) and unchanged semantic controls. A hidden-map build produced a distinct diagnostic script, SHA256 `74ded62af4950cddc253acee77cc568d74018f7a9aaf0f73b469f4ef00712aec`, paired with map `09098680e3ec6b971fc6c068cb611eef176c7e501304917f95db4aac08510a20`. It is not byte-identical to the earlier elapsed-screen bundle. Executed source bytes, script ID 4, execution context 3 and the actual main frame were joined to this pair. Licensed geometry and source maps remain private.

Profiling starts before navigation and stops after terminal producer observation. The window includes setup, fonts, module evaluation, oracle checks and polling as well as all 384 ordered replacements and 1,536 spans; it is not exclusive replacement CPU. The actual 1,000-microsecond sampling setting and single start/stop were acknowledged. All 48 producer reports passed. The intentionally missing font produced exact GET/font empty-404 observations on both page and server channels; font readiness and its diagnostic were not observed.

## Conserved accounting

Sample counts are primary. Interval weights below sum the profile's `timeDeltas` and are sampled elapsed intervals, not measured function/process CPU durations. Disjoint rows conserve every sample and all 9,538,878 microseconds of weights. A further 410 microseconds remain as the profile-end residual.

| Disjoint population | Samples | Interval weight (microseconds) |
|---|---:|---:|
| Unresolved mapping | 7,268 | 8,329,193 |
| Oracle checks | 663 | 769,817 |
| Garbage collector | 213 | 250,321 |
| Host reconciliation | 69 | 82,018 |
| Program | 37 | 49,207 |
| Serialization-only helper | 30 | 39,875 |
| Idle | 12 | 14,200 |
| Validation | 4 | 4,247 |
| Total | 8,296 | 9,538,878 |

Unresolved reasons conserve their own population: exact coordinates without a mapping account for 3,531 samples/4,149,225 microseconds; conflicted generated-function barriers for 276/340,278; non-selected or native frames for 3,461/3,839,690. The map retains 35 conflicting coordinate groups affecting 19 generated functions. No arbitrary winner, nearest segment or ambiguous exporter-self weight was promoted into serialization-only credit.

Inclusive ancestry totals overlap: serialization helpers include 1,050 samples/1,290,625 microseconds, oracle checks 4,668/5,259,558 and validation 9/9,896. These totals must not be summed or subtracted to infer pure serialization. Neither four disjoint validation samples nor the absence of a construction category proves little or no work in those paths.

A subsequent read-only raw-leaf inventory found 3,737 sampled selected-script leaves without an exact map coordinate and 3,461 non-selected/native leaves. Among the latter, `elementFromPoint` accounts for 3,229 leaves/3,578,365 microseconds. These are raw reported leaf identities, not additional semantic attribution or accepted ancestry categories; coordinate presence alone does not establish conflict-free function identity. They motivated the later bounded correspondence inventory; they supply no new semantic credit.

## Accepted function-correspondence inventory

A separate accepted offline inventory retains all 8,296 raw leaf samples and 9,538,878 microseconds of interval weights without changing the accepted ancestry accounting. Its normalized shared tables preserve 65,328 map-coordinate groups, 64,884 unique tuples, 4,424 original functions, 222 raw frames and 227 referenced generated functions. Among selected raw leaves, 3,545 samples have function-correspondence barriers, 1,026 have only homogeneous existing mapped evidence, and two lie outside a parsed function. There are 57 missing-exact-coordinate frames with homogeneous mapped evidence: six oracle-labelled frames/413 samples, 43 neutral/386, one construction/6, three validation/8 and four instrumentation/6. No serialization-only missing-entry evidence was established. These are raw inventory labels, not newly credited semantic samples; unmapped code, native frames, mixed functions and exporter ambiguity remain unresolved.

The original audit failed the unchanged 8 MiB report cap before creating a report; its in-memory size and values remain unknown. A fresh successor preserved the evidence through shared tables and lossless reference encoding, passed two syntax checks and all 46 composed controls, then produced a 4,075,346-byte report in 10.34 seconds. The actual audit kept semantic credit at zero, conserved all raw leaves and left the earlier classification bytes unchanged. Its 1,218 input pins and four aliases matched; the supervisor and registered child birth identities and their group/session were independently absent afterward. Original cap failure, historical holds and practical resource limits remain retained; no cap increase, truncated passing report or classifier rule was used.

## Conserved sampled-allocation diagnostic

A separate fresh heap-only capture reused the exact diagnostic script/map, unchanged world-preserving fixture and semantic controls. `HeapProfiler.startSampling` acknowledged a 1,048,576-byte sampling interval, stack depth 64 and both major/minor collected-object flags; one stop followed terminal producer observation. No simultaneous CPU sampling, forced collection or fallback was used. The actual executed source, new script/context/main-frame join, all 48 producer reports and the full 384-row/1,536-span queue passed. The wider whole-page window still includes setup, fonts, module evaluation, oracle work and polling. Font readiness remains unobserved; both exact negative-font empty-404 channels were retained.

The raw profile contains 2,729 nodes and 3,200 sample records. Offline accounting counts each node's `selfSize` once and conserves **3,368,864,664 sampled allocation-profile bytes**. Original sample size/node/ordinal records are retained separately and never added to that total. These are sampled profile weights under the actual collected-object flags, not total allocations, live/retained heap, leaks, process memory or GC duration.

| Disjoint population | Node selfSize weight (bytes) |
|---|---:|
| Unresolved mapping | 3,021,775,064 |
| Oracle checks | 227,547,672 |
| Serialization-only helper | 118,493,348 |
| Host reconciliation | 1,048,580 |
| Validation | 0 |
| Root | 0 |
| Total | 3,368,864,664 |

Unresolved mapping is 89.6971% of the conserved weight: non-selected/native frames account for 1,135,576,064 bytes, unmapped exact coordinates for 1,720,518,900 and conflicted/generated-only function barriers for 165,680,100. No nearest-segment guess, function-name substitution or ancestry bypass gained semantic credit. Zero disjoint validation weight means no weight was attributed there; it does not establish zero validation allocation.

Inclusive validation weight is 2,097,240 bytes and inclusive serialization-helper weight is 1,070,997,912. These overlapping ancestry totals must not be summed or subtracted to infer serialization-only allocation. Assigning the 3,021,775,064 ambiguous bytes to either explanation can reverse the observed comparison, so the sensitivity verdict is **not distinguished**. It is a bound on observed accounting, not a confidence interval, causal proof or performance budget. Heap ordinals supply no phase timestamps; `phaseJoin` remains null. CPU interval weights and heap weights are separate instruments, not a ratio or matched per-pass measurement.

The new heap adapter passed two syntax checks, 57 composed adapter/core/lifecycle controls, the unchanged 67 capture controls and six actual owner-join controls over inert process data. Initial static review found and closed missing accepted-artifact/body joins and a pending-descriptor report-write defect before execution; the original drafts/review remain retained. Both pending classification proposals remain separate from the genuinely accepted ready successor. One read-only capture-packet assembly assertion compared string and integer process births; integer normalization resolved that comparison without changing the native evidence or rerunning capture.

Capture exited 0 in 13.55 seconds with sampled aggregate peak RSS 1,072,451,584 bytes; its 4,324 input pins and 26 aliases stayed unchanged. Classification exited 0 in 25.36 seconds with sampled peak 364,343,296 bytes; all 4,343 pins and 26 aliases matched. The nine captured process births and both registered groups/sessions, then the classifier supervisor/child births and its group/session, were independently absent. Bounds remained CPU 3, 120-second work plus 30-second cleanup, practical sampled 2 GiB, Node 512 MiB, 1 MiB logs and 4/8 MiB raw/report serialization caps. Point censuses and sampled ownership do not establish hard/transient containment or an empty inherited parent session. Historical holds and failed predecessors remain unchanged.

## Qualification and limits

The CPU offline adapter passed two syntax checks and all 37 composed synthetic controls. Its first cheap attempt failed a controls-fixture syntax check because a closing brace was missing; independent static review had missed it. That consumed attempt remains retained. The fresh successor adds exactly the missing brace, leaves adapter bytes unchanged and passed before actual classification. Earlier failed map inspections and four capture predecessors also remain retained: profile-root custody loss, an asset rejection without its URL, the diagnosed missing-font request, and a live-font getter used after document disposal. They were not converted into successful profiles.

The accepted CPU capture and classification each used CPU 3, a 120-second work bound plus 30-second cleanup reserve, practical sampled 2 GiB, Node heap 512 MiB and 1 MiB logs. Raw profile/result limits were 4/8 MiB after serialization. Classification exited 0 in 42.87 seconds, with sampled peak aggregate RSS 363,212,800 bytes. Its supervisor and registered child birth identities were independently absent afterward; the complete readable process census found no registered group/session members. All 1,207 input pins and four aliases stayed unchanged. These observations do not prove hard containment or release historical custody.

The CPU evidence is one wider whole-page sampled-stack diagnostic. It establishes neither phase CPU nor exact process CPU, total allocation, retained heap, GC duration, overhead-corrected speedup, causal regression, published 0.32.1 equivalence, GPU/default decisions or the owning plan's repeated performance acceptance. No clock-origin join assigns samples to counter, warm-up, measured pass or the historical 19.4 ms event.

Private evidence remains under `programme-resume-20261009`:

| Artifact locator | SHA256 |
|---|---|
| `svg-profiler-capture-font-transport-source-20261010/cpu-run/cpu-profile.json` — original raw profile | `eaca6d7296b957128f909fe79b53f49d01c5617af9228de106d2367978b30c8d` |
| `svg-profiler-mapping-prep-review/root-capture-font-transport-acceptance.json` — capture acceptance | `405175bc93c62c7c77c3e22f926391f2195f132e3e240d99949468460453cf5e` |
| `svg-profiler-mapping-grouped-source-20261010/emitted-map-inspection.json` — useful-partial map inventory | `498c110aab37a76205c0817cf94889891e5bd4c6106d7c2d686b8b22778ac9c4` |
| `svg-profiler-cpu-classification-brace-source-20261010/cpu-classification.json` — complete accounting | `ad64500ce195e6f13d592733ecc2248aaed7dcfe931b06faee6c598cf5dd88db` |
| `svg-profiler-mapping-prep-review/cpu-classification-brace-classify-return.json` — inputs, artifacts and cleanup | `40b33ddf3850485b9f8d1392f6cd3293b7c26e8642487c85445751ce252bf9f5` |
| `svg-profiler-mapping-prep-review/root-cpu-classification-acceptance.json` — independent accounting/custody acceptance | `37c43e0c8e460cea8205e234011b1945d2f3429499b49a71a2005557f8005458` |
| `svg-profiler-mapping-prep-review/cpu-classification-cheap-failure-return.json` — original syntax failure | `e66562d229eafa67d3e17b9d597df21bb8e57f48849096de59f9c60322e8af9d` |
| `svg-profiler-mapping-prep-review/cpu-unresolved-readonly-leaf-audit.json` — separate raw-leaf coordinate inventory | `ea05bf597bd5fa50df2cb39575806467d44659d079daa7c8b2824b8584c6237c` |
| `svg-profiler-entry-coverage-tables-source-20261010/entry-coverage.json` — lossless accepted inventory | `7e533ac0ad6595ed2afab82029a42b52e2a3191918fa25466cd495efad8d8cc8` |
| `svg-profiler-mapping-prep-review/root-entry-coverage-tables-inspect-acceptance.json` — independent inventory/custody acceptance | `35db1b882df61e280201956aa3a89f3d5a1182ae9f10158f4937bfa3641de1bb` |
| `svg-profiler-mapping-prep-review/entry-coverage-inspect-failure-return.json` — retained report-cap failure | `a84bc671493d5e3fc0354936fbde73914fc7910168e6ddbe31b62ade76753bee` |
| `svg-heap-profile-source-20261010/heap-run/heap-profile.json` — genuine original heap profile | `e669fe2ccba823d133b3a9aa6ad85b4070921041aebf9cd757856970bcab433a` |
| `svg-profiler-mapping-prep-review/root-heap-profile-capture-acceptance.json` — heap capture acceptance | `39688f1f8dfeb650a9658cad21c0577c1f1791ce5a219dcabed55c5ab0417763` |
| `svg-heap-profile-classification-source-20261010/heap-classification.json` — complete sampled-allocation accounting | `94634e2dd6eb04a70973def388fb17184e3da2a36ff9f482359538fa2425bf7f` |
| `svg-profiler-mapping-prep-review/heap-profile-classify-return.json` — actual inputs, artifacts and custody | `106d7cfb819dab5621cbb6b045c961e519a176ddd0c49cf190b5cd730375d424` |
| `svg-profiler-mapping-prep-review/heap-profile-classify-independent-acceptance-view.json` — independent conservation/custody verification | `9cce224a141b5b3dbc6d17bbfcc4416dfbcd83606785edf164c5e673998c0b4b` |
| `svg-profiler-mapping-prep-review/root-heap-profile-classification-acceptance.json` — conserved, inconclusive diagnostic acceptance | `cf81fcdae3dc1deb7674cfe261d1f637832e834ac93a18d3513794f42f17c049` |

The accepted inventory supplies mapped-function evidence at missing entry coordinates without changing semantic accounting. The separately conserved heap diagnostic remains attribution-limited. This finite mapping/profiling refinement chain stops: no identical capture, map refinement, instrument or optimization follows from these results. A later proposed scientific question is whether actual validation-call work and successful serialization can be separated in a distinct private source-correspondent diagnostic while preserving validation count/order/errors, exact output/ownership and measured probe overhead. That question requires new root selection and qualification; no source or runtime is selected here.

The broader .3/.4, required repeated performance comparison, product adoption and late host qualification remain open. Existing source/input differences, failures/holds and the observer inbox/schema gap remain retained; no replacement telemetry was synthesized.
