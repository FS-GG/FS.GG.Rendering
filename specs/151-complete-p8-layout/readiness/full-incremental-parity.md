# Feature151 Full and Incremental Parity

Status: `accepted`

| Case Id | Full | Cold Incremental | Warm Incremental | Changed Input | Reuse | Verdict | Evidence |
|---|---|---|---|---|---|---|---|
| finite-root | bounds/diagnostics stable | equivalent | equivalent | not applicable | accepted hit | `accepted` | `Feature151FullIncrementalParity` |
| measured-leaves | bounds/diagnostics stable | equivalent | equivalent | measured size change classified | stale rejected | `accepted` | `Feature151FullIncrementalParity` |
| intrinsic-content | content extent stable | equivalent | equivalent | intrinsic dependency change classified | stale rejected | `accepted` | `Feature151IntrinsicReuse` |
| dynamic-content | initial full stable | equivalent | equivalent | changed geometry equals full changed result | stale rejected | `accepted` | `Feature151FullIncrementalParity` |
| visibility-change | hidden/collapsed stable | equivalent | equivalent | visibility key changes | stale rejected | `accepted` | `Feature151FullIncrementalParity` |
| child-insert-remove | child set stable | equivalent | equivalent | child key list changes | stale rejected | `accepted` | `Feature151MeasuredReuse` |
| child-reorder | order stable | equivalent | equivalent | order key changes | stale rejected | `accepted` | `Feature151MeasuredReuse` |
| ScrollViewer extent | viewport stable | equivalent | equivalent | content extent changes | stale rejected | `accepted` | `Feature151DisabledCacheParity` |

All accepted comparisons require equal observable bounds, placements, scroll
extents, diagnostics, and result identities where the mode applies.
