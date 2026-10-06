# SVG export prefix qualification

The SVG exporter now encodes the mount namespace and document ID once per export,
then appends each encoded local ID. The previous implementation repeated all three
encodings for every ID and reference. The prefix remains local to one validated
export; there is no cross-document cache, new API or simulation state. The
[producer change](../../src/Scene/SvgDocument.fs) preserves the existing encoding,
XML escaping, validation and ordered refusals.

The candidate retains a 22-case compatibility corpus with frozen pre-change
serialization/export digests for .NET and Fable separately. It covers punctuation,
Unicode IDs, namespace/document separation, interleaved exports, definition and
reference changes, removal and invalid-input ordering. Runtime-specific bytes are
compared within each runtime. The public browser consumer checks all 22 prefix cases, stale and wrong references,
invalid-input atomicity and five owned mount cycles alongside its existing controls.
The exact compiled candidate passed this public prefix corpus before the comparison.

The accepted candidate also repairs the curated Fable package view in Scene,
Scene.SvgBrowser and KeyboardInput. Those projects replace only compiled source
and the SDK project entry, retaining declared package content such as README,
licenses and the browser geometry worker. A literal `fable/` destination avoids
metadata batching in the source-item list. Both comparison profiles used the same
packaging repair; the sole compared producer difference was export-prefix factoring.

## Software-browser result

Comparison14 completed naturally with exit 0 in 49.140214 seconds. It used full
Chrome 153.0.8010.12 with `--disable-gpu`, 250 shared-symbol instances and 120 ticks,
changing 25 instances per tick. Both profiles include the previously qualified
attribute and aligned-child reconciliation guards. Three paired repetitions used
order B0/C0/C1/B1/B2/C2, 20 warm and 100 measured whole-Replace calls per record.

Whole-Replace CPU milliseconds; columns show p50 / p95 / p99 / maximum using
nearest-rank quantiles from 100 samples per record:

| Pair | Baseline CPU | Prefix candidate CPU | p50 reduction |
|---|---:|---:|---:|
| 0 | 23.6 / 45.3 / 80.0 / 120.0 | 17.0 / 30.4 / 33.7 / 43.5 | 27.97% |
| 1 | 22.9 / 44.5 / 80.0 / 103.6 | 17.3 / 34.0 / 45.8 / 111.8 | 24.45% |
| 2 | 21.4 / 39.5 / 40.1 / 40.6 | 15.9 / 28.5 / 35.2 / 36.9 | 25.70% |

All three candidate medians are lower. The median paired reduction is 25.7009%,
exceeding the selected 5% threshold. CPU p95 improves in every pair. Pair1's CPU
maximum rises from 103.6 to 111.8 ms; this individual tail regression is retained.
p99 is descriptive at this sample population. Two candidate medians, 17.0 and
17.3 ms, still exceed 16.67 ms; the result is no general frame-budget guarantee.

Animation-frame intervals in milliseconds, with the same p50 / p95 / p99 / maximum
columns, include identity checks outside the measured CPU span:

| Pair | Baseline RAF interval | Prefix candidate RAF interval |
|---|---:|---:|
| 0 | 33.4 / 66.7 / 99.9 / 150.1 | 16.7 / 33.4 / 33.4 / 33.4 |
| 1 | 33.3 / 50.0 / 100.0 / 116.6 | 16.7 / 33.4 / 50.1 / 133.4 |
| 2 | 33.3 / 50.0 / 50.1 / 66.7 | 16.7 / 33.4 / 50.0 / 50.0 |

Pair1's maximum frame interval rises from 116.6 to 133.4 ms. These samples do not
establish GPU, GC, statistical or universal performance improvement.

## Correctness, custody and limits

The exact result contains six records, 600 measured calls, 120 warm calls,
28 original negative controls, four counter-predicate controls and nine paired
surface witnesses. The original connected XML, export and live geometry/paint,
font, ordering, hit, identity, atomicity and disposal checks passed. Counter hooks
were restored before timing; unchanged attribute writes and aligned child moves
remained zero. All nine paired witnesses passed after their independent source
checks; raw hash equality is not substituted for semantic admission.

The original wrapper, terminal output, retirement and completion hashes join.
Retirement records no violations, remaining owned processes, zombies or unreadable
rows, and verifies source/artifact pins. Peak sampled summed owned RSS was
2,583,908,352 bytes under the separately authorized SVG development 8 GiB allowance.
That peak exceeds 2 GiB: this result establishes no 2 GiB fit or product/LEARN budget
change. One CPU, 32 owned processes, CLR10 and the original 180-second work,
30-second cleanup and 210-second whole-operation bounds were retained.

Earlier failed attempts remain failed. Comparison12 was refused on a foreign
telemetry CLR; comparison13 was refused on the container's periodic SDD updater.
For comparison14, root verified the exact maintenance parent/script/sleep interval
and an initial zero-CLR census after its telemetry cycle finished. This was a
bounded quiet observation, not a global exclusion lock; runtime foreign-CLR guards
remained strict. No failed result or consumed admission was replayed.

The root accepted this narrow software comparison. Expanded count250 remains
unsupported under the existing node limit. Publication, installed consumer
adoption, GPU qualification and the owning .3/.4 milestones remain open. The
[owning SVG plan](../roadmaps/svg-coherence-and-instancing-01.md) retains those
boundaries.

Evidence SHA256 identities (private raw and licensed fixture bytes remain outside
producer payloads):

| Evidence | SHA256 |
|---|---|
| Root scoped comparison acceptance | `73981542a70f024e4cb7fbba90eb6269f3512db23a0a4ec053e6e3dc6018e8a6` |
| Browser result | `cc096410192b65d36318f715110032eda3c8b2669a7eb0957e8d526a2a6350e4` |
| Retirement | `6015596e73b22edcc0fa5355d5a9adef8a7012e6a2747cdbb1c13aca0ef9d9ab` |
| Completion | `7205e06b2d6320c4213c0098d6f00af848fcb20b35bd73c3c3215e98eeca8700` |
| Original wrapper terminal | `f01dc048120ace81dcbf9382b9fbf013ab0330a6f362bb3ab0e5053c081566da` |
| Compared candidate producer source | `23cf14a210b917716edc1082acca92a0c6410f690cacfac4ee7f724ecc726595` |
| Compared candidate compiled producer | `fbe1e0bf5fc46c8ebe553fedb782711b2e3b77258bebbbf0530499619b45b3da` |
