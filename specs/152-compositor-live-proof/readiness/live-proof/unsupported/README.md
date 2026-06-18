# Feature 152 Unsupported Host Proof Record

Status: `environment-limited`

Unsupported-host proof execution is expected to complete without accepting partial redraw.

| Field | Value |
|-------|-------|
| Verdict | `environment-limited` |
| Accepted partial-redraw artifacts | `0` |
| Fallback | full redraw |
| Reason | missing or unavailable display/GL/readback facts in the current environment |

This record satisfies the unsupported-host disclosure path only. It cannot satisfy the capable-host proof set, parity, or timing acceptance gates.

