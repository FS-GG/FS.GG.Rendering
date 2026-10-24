# Spatial working-set index: array-backed entry storage

Delivery: FS-GG/FS.GG.Rendering#1469 (parent FS-GG/.github#4645). Baseline source
`f51fad69c648deb1a8212807337e9d9a464c7e74`.

## Change

`SpatialWorkingSetIndex` previously retained the validated source `list`. Every selected ordinal
(`entries[index]`) walked the list from its head, and `TotalEntryCount` (`entries.Length`) walked all of it.
`SpatialWorkingSet.create` now converts the already validated list once into a private array. The public
`create`/`query` signatures and result types, ordering, counters, refusal order, bucket construction and
the returned entry/value records are unchanged.

## Controls (tests/Scene.Tests/SpatialWorkingSetTests.fs)

- An independent linear reference (walks every source entry per query, shares no data structure with the index)
  is joined to the real index on selected identities and order, record identity, visited-chunk,
  candidate and total counters for: the same visible population at the beginning and end of a 5,000-entry
  source; sparse late ordinals in 8,000; multi-chunk overlap with deduplication; boundary hits on both axes and
  just-outside entries; offscreen, missing and visible pins at late ordinals; repeated and interleaved queries.
- Refusal order for a large invalid population and early query refusal are pinned.
- A structure control asserts exactly one private `SpatialEntry[]` is retained.
- A position/length control (300,000 entries) bounds late-ordinal and large-source query cost against first-ordinal
  and 1,000-entry references using minimum per-query time over 40 rounds, with a loose factor-6 bound.
  The structure and position controls fail on the baseline source (24.5 ms against a 0.28 ms reference
  for 100 late visible entries) and pass on the candidate; all other new controls pass on both.

## Isolated before/after measurement (CLR)

Same harness source (`readiness/svg-coherence-1469/Bench.fs`), same method; only the referenced
`FS.GG.UI.Scene.dll` differs. Release, .NET 10.0.401, pinned to 3 CPUs, `ABBA` run order, 30 interleaved rounds
per cell, raw per-round observations in `readiness/svg-coherence-1469/raw/` and joined summary in
`summary.txt`. Every cell's selected identities, hash, visited/candidate/total counters are identical between
baseline and candidate across all four runs and equal an independent linear reference.

Median microseconds per `SpatialWorkingSet.query` call (baseline -> candidate; run-to-run medians of a cell differ by up to about 8%):

| entries | 1 visible, first | 1 visible, last | 100 visible, first | 100 visible, last | 1 late pin |
|---|---|---|---|---|---|
| 1,000 | 19.0 -> 19.1 | 21.6 -> 20.4 | 34.5 -> 32.6 | 113.6 -> 35.6 | 22.8 -> 21.9 |
| 10,000 | 31.6 -> 25.4 | 41.2 -> 26.8 | 47.1 -> 38.2 | 881.6 -> 40.8 | 42.9 -> 27.6 |
| 100,000 | 117.5 -> 29.6 | 200.8 -> 30.9 | 133.7 -> 42.1 | 8,501 -> 45.3 | 202.3 -> 32.8 |
| 300,000 | 291.8 -> 31.6 | 546.6 -> 33.6 | 310.5 -> 44.1 | 25,637 -> 45.9 | 546.1 -> 34.5 |

Baseline cost grows with source length even for the first ordinal (the `TotalEntryCount` traversal) and with
ordinal for late selections; the candidate is flat to within a few microseconds across a 300x range of source length.
No cell regressed beyond run-to-run noise (the largest candidate/baseline median ratio is 1.003 at 1,000 entries).

Adverse and limits:

- `create` gains one `Array.ofList` pass. Median allocation changed by about +0.2% (for example 1,584,095,232 B ->
  1,586,495,240 B at 300,000). Median create time was lower for the candidate at the two largest sizes in every
  run (750 -> 320 ms at 100,000 and 1,512 -> 1,052 ms at 300,000; equal within noise at 1,000 and 10,000). The
  create code differs only by that one pass, so the cause of the difference is unexplained (possibly GC or
  allocation-state order effects); no create-time improvement is claimed and no create regression was observed.
  Creation time is dominated by the unchanged quadratic-append bucket construction.
- Timings are from one shared development host, not the calibrated hosted benchmark; no timing threshold or
  Home B1-B8 verdict is inferred.

## Real M9 browser consumer (packed Fable, Chromium)

`tests/Scene.SvgBrowser.Tests/run-performance.sh` (diagnostic mode, frozen workloads including extent-near and
extent-far, three mutants, 3 CPUs) passed locally on both the baseline and the candidate:
extent cost ratio 1.047 baseline and 1.051 candidate, zero live-node growth, zero missed frames. The existing M9
fixture places its visible entries first, so this result is distinct evidence and does not measure late-index
lookup or a speedup; it shows only that the packed consumer is unchanged. The exact-head hosted `svg-browser`
workflow result is recorded on the pull request.
