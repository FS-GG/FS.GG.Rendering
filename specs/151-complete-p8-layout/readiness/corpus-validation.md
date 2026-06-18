# Feature151 Representative Layout Corpus

Status: `accepted`

The corpus is executable through `tests/Layout.Tests/Feature151CorpusFixtures.fs`
and the focused Feature151 Layout test suites.

| Case Id | Category | Expected Bounds | Expected Placements | Diagnostics | Verdict | Evidence |
|---|---|---|---|---|---|---|
| finite-root | constrained root | root, header, body finite | stable child order | none | `accepted` | `Feature151RepresentativeCorpus` |
| zero-root | zero constraint | root finite, zero-compatible | no negative placements | none | `accepted` | `Feature151RepresentativeCorpus` |
| very-small-root | small constraint | root and children finite | clipped but deterministic | none | `accepted` | `Feature151RepresentativeCorpus` |
| very-large-root | large constraint | root and children finite | deterministic | none | `accepted` | `Feature151RepresentativeCorpus` |
| measured-leaves | measured leaves | measured child sizes applied | direct child placements | none | `accepted` | `Feature151RepresentativeCorpus` |
| intrinsic-content | intrinsic content | intrinsic natural size reachable | child dependency keys present | none | `accepted` | `Feature151ScrollLayoutProtocol` |
| empty-container | empty container | root finite | no children | none | `accepted` | `Feature151RepresentativeCorpus` |
| single-child | single child | root and child finite | one placement | none | `accepted` | `Feature151RepresentativeCorpus` |
| deep-nesting | deep nesting | all nested nodes finite | stable hierarchy | none | `accepted` | `Feature151RepresentativeCorpus` |
| dynamic-content | dynamic content | changed geometry differs as expected | changed incremental equals full | none | `accepted` | `Feature151FullIncrementalParity` |
| child-insert-remove | child insertion/removal | changed child set reflected | stale geometry rejected by parity | none | `accepted` | `Feature151FullIncrementalParity` |
| child-reorder | child reorder | child order changes dependency key | placements remain deterministic | none | `accepted` | `Feature151MeasuredReuse` |
| visibility-change | hidden/collapsed | hidden retained, collapsed zero-sized | stable siblings | none | `accepted` | `Feature151RepresentativeCorpus` |
| invalid-available | invalid constraints | fallback finite bounds | no misleading negative bounds | `InvalidAvailableSpace` | `accepted` | `Feature151Diagnostics` |
| contradictory-size | contradictory constraints | fallback finite bounds | maximum used | `UnsatisfiedConstraint` | `accepted` | `Feature151Diagnostics` |
| duplicate-node | diagnostic | duplicate participant blocked from reuse | duplicate placement classified | `DuplicateLayoutNodeId`, `DuplicateMeasurement` | `accepted` | `Feature151Diagnostics` |

## Scope

Accepted cases use the existing `FS.GG.UI.Layout` public protocol. The feature did
not require a public contract delta beyond Feature150.
