# Phase 1 Data Model: Release → Templates Dispatch Sender

This feature carries one message across repos; its "data model" is the notification payload and the
inputs the sender derives it from. There is no persistent storage.

## Entity: Template-released notification

The cross-repo message Rendering sends to FS.GG.Templates.

| Field | Type | Value / Source | Rules |
|-------|------|----------------|-------|
| `event_type` | string (constant) | `fs-gg-ui-template-released` | Fixed by the receiver contract; MUST match exactly (FR-001, FR-009). |
| `client_payload.version` | string | derived released version (see below) | Non-empty; receiver-expected form `<major>.<minor>.<patch>[-preview.N]` (FR-002). |

- Target: `FS-GG/FS.GG.Templates` (the receiver repo).
- Transport: GitHub REST `POST /repos/FS-GG/FS.GG.Templates/dispatches`.
- This is the **sent** half of an existing send/receive contract; the receiver (`upstream-bump.yml`)
  is unchanged (spec Assumptions).

## Entity: Released template version

The sole meaningful field of the payload.

- **Source**: the triggering tag ref `refs/tags/fs-gg-ui-template/v<version>`.
- **Derivation**: strip the literal prefix `refs/tags/fs-gg-ui-template/v` from `github.ref`.
- **Validation rule** (FR-002, FR-003, edge case "version cannot be determined"):
  - MUST be non-empty after the prefix strip.
  - MUST match `^[0-9]+\.[0-9]+\.[0-9]+(-preview\.[0-9]+)?$`.
  - On any failure → abort the send with a non-zero exit (never send empty/placeholder).
- **Example**: `refs/tags/fs-gg-ui-template/v0.1.50-preview.1` → `0.1.50-preview.1`.

## Entity: Cross-repo credential

The authorization permitting the dispatch into FS.GG.Templates.

- **Surface in this repo**: `secrets.TEMPLATES_DISPATCH_TOKEN`, passed to the script as `GH_TOKEN`.
- **Provisioning**: owned outside this repo (org-level — FS-GG/.github#21/#22). BLOCKING for the live
  path; not required to author or dry-run the sender.
- **Validation rule** (FR-004, FR-006): the script MUST fail loudly if the token env is empty/unset
  (no attempt to send unauthenticated, no silent skip).

## Guards (not payload data, but gate whether a notification is produced)

| Guard | Condition | Requirement |
|-------|-----------|-------------|
| Canonical-repo guard | `github.repository == 'FS-GG/FS.GG.Rendering'` | FR-005 / US3 — forks never send. |
| Genuine-template guard | trigger tag matches `fs-gg-ui-template/v*` | FR-007 — non-template events never send. |
| Version-determinable guard | derived version passes validation | FR-006 / edge case — undeterminable version fails, never sends. |
| Credential-present guard | `GH_TOKEN` non-empty | FR-004 / FR-006 — missing credential fails visibly. |

## State transitions

The send is a single, stateless event (no retained workflow state). Outcomes:

```
tag push (fs-gg-ui-template/v*) on canonical repo
        │
        ├─ version underivable ──────────────► FAIL (no send)            [edge: version cannot be determined]
        ├─ credential missing ───────────────► FAIL (no send)            [edge: credential missing/unauthorized]
        ├─ dispatch error (network/receiver) ► FAIL (visible)            [edge: receiver temporarily unavailable]
        └─ dispatch ok ──────────────────────► SENT (receiver opens/no-ops pin-bump PR)
```

Duplicate/concurrent sends are not deduped on the sender side; the receiver no-ops a repeat version
and the latest version wins (spec edge cases; research R7).
