# Feature151 Measured and Intrinsic Reuse

Status: `accepted`

## Measured Reuse

| Scenario | Dependency Evidence | Outcome | Verdict | Evidence |
|---|---|---|---|---|
| equivalent warm run | participant id, entry kind, normalized constraints, input key, child keys, revision | cache entry identity stable | `accepted` | `Feature151MeasuredReuse` |
| constraint change | normalized constraint identity differs | miss/stale rejection | `accepted` | `Feature151MeasuredReuse` |
| content identity change | layout input key differs | miss/stale rejection | `accepted` | `Feature151MeasuredReuse` |
| child reorder | ordered child dependency keys differ | miss/stale rejection | `accepted` | `Feature151MeasuredReuse` |
| duplicate participant | duplicate measurement diagnostic recorded | rejected for acceptance | `accepted` | `Feature151Diagnostics` |

## Intrinsic Reuse

| Scenario | Dependency Evidence | Outcome | Verdict | Evidence |
|---|---|---|---|---|
| equivalent query | participant, axis, cross-axis, input key, source, revision | query identity stable | `accepted` | `Feature151IntrinsicReuse` |
| axis change | query identity differs | miss | `accepted` | `Feature151IntrinsicReuse` |
| cross-axis change | query identity differs | miss | `accepted` | `Feature151IntrinsicReuse` |
| dynamic content change | input key and dependency identities differ | stale intrinsic rejected | `accepted` | `Feature151IntrinsicReuse` |
| participant mismatch | unsupported query diagnostic | rejected | `accepted` | `Feature151Diagnostics` |

## Disabled Cache

Disabled-cache parity is classified through full/incremental output equality and
Controls ScrollViewer extent equality. Work metrics may differ; observable bounds,
placements, extents, and diagnostics may not.
