# SVG whole-page sampled stack accounting

Date: 2026-10-10. Scope: SVG-COHERENCE-01.3 partial diagnostic evidence.

One accepted CPU-profiler capture and one separately accepted offline classification account for all 8,296 samples. The conservative source-map rules leave 7,268 samples (87.61%) unresolved. This does not distinguish validation traversal from successful SVG serialization precisely enough to select an optimization. The owning .3/.4 milestones remain incomplete.

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

A subsequent read-only raw-leaf inventory found 3,737 sampled selected-script leaves without an exact map coordinate and 3,461 non-selected/native leaves. Among the latter, `elementFromPoint` accounts for 3,229 leaves/3,578,365 microseconds. These are raw reported leaf identities, not additional semantic attribution or accepted ancestry categories; coordinate presence alone does not establish conflict-free function identity. They explain why map coverage and native/oracle boundaries need attention before another profiler window.

## Qualification and limits

The final offline adapter passed two syntax checks and all 37 composed synthetic controls. Its first cheap attempt failed a controls-fixture syntax check because a closing brace was missing; independent static review had missed it. That consumed attempt remains retained. The fresh successor adds exactly the missing brace, leaves adapter bytes unchanged and passed before actual classification. Earlier failed map inspections and four capture predecessors also remain retained: profile-root custody loss, an asset rejection without its URL, the diagnosed missing-font request, and a live-font getter used after document disposal. They were not converted into successful profiles.

The accepted capture and classification each used CPU 3, a 120-second work bound plus 30-second cleanup reserve, practical sampled 2 GiB, Node heap 512 MiB and 1 MiB logs. Raw profile/result limits were 4/8 MiB after serialization. Classification exited 0 in 42.87 seconds, with sampled peak aggregate RSS 363,212,800 bytes. Its supervisor and registered child birth identities were independently absent afterward; the complete readable process census found no registered group/session members. All 1,207 input pins and four aliases stayed unchanged. These observations do not prove hard containment or release historical custody.

This is one wider whole-page sampled-stack diagnostic. It establishes neither phase CPU nor exact process CPU, total allocation, retained heap, GC duration, overhead-corrected speedup, causal regression, published 0.32.1 equivalence, GPU/default decisions or the owning plan's repeated performance acceptance. No clock-origin join assigns samples to counter, warm-up, measured pass or the historical 19.4 ms event.

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

The next proposed source qualification examines whether missing selected-script function-entry coordinates have a complete, unique, conflict-free generated/original function correspondence. Native/non-selected frames and exporter/shared ambiguity remain barriers. No classifier rule, producer, map or runtime change is selected by this report. Heap sampling stays a separate prerequisite, with ordinals providing no phase timestamps. Existing source/input differences, original failures/holds and the observer inbox/schema coverage gap remain retained; no replacement telemetry was synthesized.
