# Spatial working-set index: linear bucket construction

Delivery: FS-GG/FS.GG.Rendering#1472 (parent FS-GG/.github#4645). Baseline source
`8574c14d37b0c4742b9bc93a4e85df5c8e9035a6`, which already holds the array entry storage of #1469/#1470.

## Change

`SpatialWorkingSet.create` built each chunk's bucket incrementally: for every member entry it looked the chunk up in
an immutable map, copied the bucket's list with `values @ [ index ]` and re-added it. Copying a bucket per
insertion is quadratic in the population of one chunk, and every membership paid a map lookup plus a map add.

`create` now collects members in one forward pass over a hash table (ordinals are prepended), remembers the order in
which chunk keys first appear, and then adds each key to the map once, in that order, with its list reversed to
ascending ordinals (a single-member bucket is used as is). Because the immutable map receives the same keys in the same
order and replacing a value does not change a tree's shape, the private map has the shape and bucket contents the
previous build produced. Public types and signatures, the private index shape, array entry storage, ordering,
counters, validation and refusal order, the identity map and the query path are unchanged.

## Controls (tests/Scene.Tests/SpatialWorkingSetTests.fs)

The existing independent linear reference joins the real index on selected identities and order, record identity and
every counter. New cases, all passing on the candidate (151 tests in `Scene.Tests`, 4 added):

- a population sharing one chunk (3,000 entries) with narrow viewports, pins at the first, middle, last and a missing
  ordinal, and a whole-chunk query with overscan;
- a seeded mixed population (2,000 rectangles, some very wide or tall) queried at three chunk sizes, seven viewports
  including degenerate and edge-touching ones, two overscans and with and without pins;
- a structure control that compares every private bucket with an independent direct scan over 800 scattered rectangles
  and a 1,200-member cluster in one chunk: the same chunks are populated and each bucket lists exactly its member
  ordinals in ascending order;
- a construction-cost control: 30,000 entries in one chunk against 30,000 spread entries, and 15,000 against 30,000
  entries in one chunk, minimum over rounds, with deliberately loose bounds.

Control sensitivity on this source (logs in `readiness/svg-coherence-1472/control-logs/`):

| Source | Result |
|---|---|
| Baseline `create` | the construction-cost control fails (2,884 ms for 30,000 entries in one chunk against 78 ms spread; bound 311 ms); the other 150 pass, as expected, because the baseline bucket contents are correct |
| Mutant: buckets left in descending order | the structure control fails |
| Mutant: a later member replaces the bucket instead of joining it | nine controls fail |
| Mutant: only the first row of chunks is registered for an entry | four controls fail |
| Mutant: buckets of more than 1,000 members left in descending order | the structure control fails (added after review showed the earlier controls could not see this) |
| Mutant: a key recorded twice in the first-appearance list | no control fails; the mutant is behaviourally equivalent (the second `Map.add` of a key replaces its own value) |

## Isolated before/after measurement (CLR)

Same harness source for both arms (`readiness/svg-coherence-1472/CreateBench.fs` for construction; the unchanged
#1469 query harness `readiness/svg-coherence-1469/Bench.fs` for query), same method; only the referenced
`FS.GG.UI.Scene.dll` differs (the measured baseline and candidate assemblies had sha256 `e6c9ea59...` and
`2662227f...`, recorded in `scene-dll-sha256.txt`; a rebuild from the same source differs in bytes because builds embed
paths). Release, .NET 10.0.401 SDK with
the 10.0.12 runtime, pinned to 3 CPUs, run order baseline, candidate, candidate, baseline, 9 interleaved rounds per
construction cell (12 per query cell), concurrent and server GC off. Raw per-round observations are in
`readiness/svg-coherence-1472/raw/` and the joined summary is `summary.txt`. In every cell the selected identities,
hash and visited, candidate and total counters are identical across all runs and equal an independent linear
reference.

Median milliseconds and megabytes allocated per `SpatialWorkingSet.create` call, baseline -> candidate (the two runs of
each arm agree to a few percent):

| Layout | Entries | Time (ms) | Allocated (MB) |
|---|---|---|---|
| spread, one entry per chunk | 1,000 | 1.35 -> 0.98 | 3.4 -> 2.7 |
| | 100,000 | 370 -> 275 | 495 -> 386 |
| | 300,000 | 1,037 -> 883 | 1,590 -> 1,258 |
| regular grid, 4 per chunk | 100,000 | 308 -> 130 | 500 -> 222 |
| soldier-like grid, 8x8 at spacing 36 | 250 | 0.233 -> 0.102 | 0.65 -> 0.35 |
| | 1,000 | 1.50 -> 0.56 | 3.7 -> 1.7 |
| 100 per chunk | 100,000 | 279 -> 83 | 532 -> 170 |
| mixed, chunk-spanning (20,000) | 20,000 | 55.9 -> 14.0 | 105 -> 33 |
| all in one chunk | 1,000 | 3.17 -> 0.36 | 17.9 -> 1.3 |
| | 5,000 | 68.2 -> 2.65 | 410 -> 7.1 |
| | 20,000 | 1,150 -> 12.6 | 6,443 -> 31 |
| | 40,000 | 4,827 -> 36.4 | 25,689 -> 65 |

Baseline cost for a single chunk grows about fourfold when the population doubles (1,150 ms at 20,000 and 4,827 ms
at 40,000); the candidate grows about threefold from 12.6 to 36.4 ms (the remaining growth includes the larger
retained lists and garbage collection). Beyond the quadratic term, the new form is also faster for spread populations
(about 15 to 28 percent) because it performs one map insertion per chunk rather than a lookup and an insertion per
membership, and it allocates about 20 percent less. The full table, including every layout and size, is in
`summary.txt`.

Query cost is unchanged. Across the 36 cells of the #1469 query harness (1,000 to 300,000 entries, first, middle and
late ordinals, pinned) the largest candidate over baseline median ratio is 1.053, run-to-run medians of one cell
differ by a similar amount, and most cells are within 3 percent. The query path, the map shape and the bucket
contents are unchanged by construction; this is an empirical confirmation, not a speedup.

### Rejected first design and why it is retained

The first candidate prepended ordinals while visiting entries from last to first (`List.rev` of the indexed source,
`index :: values` into the same map). It removed the quadratic term (one chunk of 40,000 entries: 4,717 -> 48 ms) but
inserted the map keys in descending order, which changed the map's shape. In the #1469 query harness the
candidate was then consistently slower on cells whose lookups hit one end of the key range (for example 17.7 -> 23.3
microseconds for one visible first entry in 1,000, both candidate runs 23.2 and 23.4 against baseline 17.6 and 19.2;
largest median ratio 1.31) and on spread populations construction was 1 to 6 percent slower with about 3 percent
more allocation. That is an adverse result, not noise: it was resolved by changing the design to the form above, which
keeps the original key order. The rejected design's raw observations and summary are retained in
`readiness/svg-coherence-1472/raw-rejected-list-rev/`.

## Limits

- One shared development host with 3 pinned CPUs. Timings are not the calibrated hosted benchmark and imply no timing
  threshold or Home B1-B8 verdict.
- Construction cost is paid when a world or index is created, not per frame. The synthetic 250-entry grid of 8-unit boxes
  at spacing 36 (about three entries per 64-unit chunk; nothing ties it to the real soldier workload's density) gains
  0.13 ms per create from this change; the large gains are for dense populations, where one chunk holds thousands of
  entries. No product speedup claim follows.
- The real M9 packed Fable/browser consumer is exercised by the exact-head hosted `svg-browser` workflow on the pull
  request; its result is recorded there and is read as pending until that check completes. A passing result shows the
  packed consumer unchanged; it does not measure construction.
- No publication, installed or retained adoption, GPU or licensed-glyph claim follows. The other source hotspots in the
  owning plan (browser host replace, reconciliation sibling search, interaction synchronization, cache ownership) and
  incremental index updates remain open.
