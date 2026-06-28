# Data Model: Adopt Reusable App-Token Dispatch-Sender

No persistent storage. The "entities" are the values flowing across the workflow boundary on a
template release. The dispatched event shape is dictated by the receiver (the `fs-gg-ui-template`
contract is realized, not redefined).

## Entities

### Template-scoped release tag (trigger + version source)
- **Form**: `refs/tags/fs-gg-ui-template/v<version>` (Feature 206).
- **Constraint**: this glob (`fs-gg-ui-template/v*`) is the *sole* trigger and is disjoint from
  `release.yml`'s `v*` (FR-006, research R6).
- **Derivation**: `version = ref` with prefix `refs/tags/fs-gg-ui-template/v` stripped.

### Derived version (job output `derive.outputs.version`)
- **Validation rule**: non-empty AND matches `^[0-9]+\.[0-9]+\.[0-9]+(-preview\.[0-9]+)?$`.
- **Failure**: undeterminable (non-tag ref / empty after strip) or malformed → `derive` job fails
  loudly, `dispatch` (which `needs: derive`) never runs, nothing is sent (FR-005, Principle VI).
- **Producer**: `scripts/derive-template-version.sh`. **Consumer**: the reusable workflow's `version`
  input.

### Reusable-workflow call (the `dispatch` job)
| Field | Bound to | Source |
|-------|----------|--------|
| `uses` | `FS-GG/.github/.github/workflows/dispatch-sender.yml@<sha>` | SHA pin (research R2) |
| `with.target-repo` | `FS-GG/FS.GG.Templates` | constant (contract) |
| `with.event-type` | `fs-gg-ui-template-released` | constant (contract, FR-001) |
| `with.version` | `${{ needs.derive.outputs.version }}` | derived above |
| `secrets.app-id` | `${{ secrets.<ORG_APP_ID_SECRET> }}` | org App, `.github#21` (R1 — name to confirm) |
| `secrets.app-private-key` | `${{ secrets.<ORG_APP_PRIVATE_KEY_SECRET> }}` | org App, `.github#21` (R1) |

### Dispatched event (emitted by the callee, observed at the receiver)
```jsonc
{
  "event_type": "fs-gg-ui-template-released",          // constant — MUST match receiver (FR-001)
  "client_payload": {
    "version": "0.1.52-preview.1",                     // required; the derived version (FR-002)
    "source_repo": "FS-GG/FS.GG.Rendering",            // added by callee — inert at receiver (R5)
    "source_sha":  "<sha>",                            // added by callee — inert
    "source_ref":  "refs/tags/fs-gg-ui-template/v…"    // added by callee — inert
  }
}
```
- The receiver (`upstream-bump.yml`) reads only `version`; extra fields are backward-compatible (R5).

### Org App credential (run-time, never stored on Rendering)
- A least-privilege GitHub App installation token, scoped to `target-repo` only, minted **inside the
  reusable workflow** by `actions/create-github-app-token@v1`. Rendering passes only the App id +
  private key (themselves org secrets), never a long-lived cross-repo PAT (FR-002, SC-003).

## State transitions (a single release)

```
tag fs-gg-ui-template/v<version> pushed (canonical repo)
        │
        ▼  derive job (guard: github.repository == FS-GG/FS.GG.Rendering)
   derive+validate version ──fail──► job fails loudly, no dispatch  (non-tag/malformed; FR-005)
        │ ok
        ▼  dispatch job (needs: derive; uses: reusable@<sha>)
   App secrets present? ──no──► reusable preflight fails closed, pointer to .github#21  (FR-005)
        │ yes
        ▼
   mint scoped token → POST repository_dispatch → receiver upstream-bump.yml opens/updates pin-bump PR
        │
        ▼
   live evidence: sender run URL + receiver PR URL  → closes Rendering#10 (FR-009, SC-005)
```

Fork path: `derive`'s guard is false → `derive` skipped → `dispatch` skipped → no send, no credential
exposure (FR-004, US3).
