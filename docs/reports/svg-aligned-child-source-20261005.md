# Aligned-child reconciliation qualification

The document reconciler moved every matched child even when child count, keys, tags and order already agreed. The candidate synchronizes those positional children directly. A count, key or tag mismatch retains the original matching, cloning, ordering and removal path. Attribute comparison/removal, text updates, validation, export, parsing, accepted-state assignment and public APIs remain unchanged. No cache or presentation clock is added.

The measured baseline is protected-main snapshot `6b661cb44d763661989ecebe402a736546783804`, containing the merged unchanged-attribute guard. The measured candidate is unmerged source `237f66bcce46c9e6227a266231342b34367b060c` based on that snapshot. Both native private feeds compiled successfully and passed their seventeen-case browser suites; the candidate suite adds aligned-mutation counters, keyed reorder/topmost paint, insertion/removal, incompatible primitive tags, unkeyed identity, exact surface faults and repeated disposal. Original fallback and guarded attribute code are retained.

The matched private experiment uses 100 visible instances of a licensed 24-node composition, with 1% motion, identical seeded commands and existing APIs. Each representation has three repetitions per producer, 20 warm and 100 measured actual Replace calls per record. Together the windows contain 12 records, 1,200 measured and 240 warm calls, 112 original exact faults, 8 counter-predicate faults and 54 paired witnesses. Each producer/representation is checked against the independent retained XML oracle, export and live state; paired witnesses preserve raw hashes and the already qualified narrow text-measurement precision policy. Measurement hooks are restored before CPU timing.

Whole-Replace CPU milliseconds, nearest-rank quantiles from each 100-sample record:

| Representation | Repetition | Guarded baseline p50 / p95 | Candidate p50 / p95 |
|---|---:|---:|---:|
| Shared symbol | 0 | 11.9 / 14.6 | 8.7 / 9.0 |
| Shared symbol | 1 | 11.9 / 13.0 | 8.9 / 9.6 |
| Shared symbol | 2 | 11.6 / 13.3 | 9.1 / 10.0 |
| Expanded geometry | 0 | 169.8 / 188.1 | 160.0 / 171.2 |
| Expanded geometry | 1 | 167.3 / 183.8 | 160.0 / 176.3 |
| Expanded geometry | 2 | 167.0 / 174.5 | 159.7 / 173.8 |

Independent counter preflights observed 349 shared and 4,902 expanded already-owned moves per baseline aligned Replace, versus zero for the candidate. Both producers retained zero unchanged attribute writes. Reversed candidate snapshots still made 100 ordering moves and passed geometry, semantic order, identity and topmost-hit checks. Fewer moves coincide with lower p50 in all six matched pairs here; tail improvement is not uniform. Expanded repetition 0 candidate p99/max were 200.0/204.9 ms versus baseline 195.5/199.4 ms.

Qualification used cached full Chrome 153.0.8010.12, executable SHA256 `8c599d43aec53f2460a31ae2f4af6bd863f8258b34ff519564bc5d4726bfaa1e`, with `--disable-gpu`. Cold frame bootstrap is recorded separately; individual warm CPU samples are not serialized. rAF intervals include identity checks outside the CPU span. These three-repetition software diagnostics do not establish GPU cause, historical/headless-shell equivalence, statistical or universal performance, or a frame-budget guarantee. Expanded CPU remains far above 16.67 ms. Earlier unguarded composition timings are separate evidence.

The first nested-loader attempt failed before timing and remains preserved. The repaired shared page completed its full population, but its original driver exited 1 because the driver expected a nonexistent fault outcome field instead of `killed: true` with exact predicate equality. A readonly supplement validated both the retained page and downloaded fields, all controls, artifact integrity and cleanup; the root accepted that supplement without rerunning shared timing. The original exit 1/raw result remain unchanged. Expanded completed with the corrected external driver, exit 0. All allocated browser/server generations were closed and observed absent; this is actual process cleanup, not formal sampled resource custody.

Private evidence identities are native baseline artifact `7432a8eb91fdec74cefb19a3f83477c95610a879e2ba0e1a09fa90ddbbf6b866`, candidate artifact `4385c3b3ef8ac9188d6b28c75582cd5eeabff3227b2bb4d8ef88c1ceacb16ef6`, matched fixture `af9c5c7d9d0477d49cf0debf101e0ac9bc0bbebc9f26cc322bc0e302b6f57652`, shared raw result `cd370c99f29ce09fe1d2d149f5a5e96f3b10a4956a247c1e45ee2b0084acde15`, readonly supplement `ae53a2d4b4437f1c01cfba9ac45d034fc988f5e0a45fa7ad6b36837a52f1557f`, and expanded result `30d47683e8bce4a1ea779a09cbee3456a24ff12b2ecd4c355c6c158b5e4377b9`. Licensed fixture/source bytes and raw evidence stay private and outside producer package payloads. This qualifies the focused source candidate; the owning SVG programme remains open and product adoption/publication are separate.
