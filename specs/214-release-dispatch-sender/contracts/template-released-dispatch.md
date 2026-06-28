# Contract: `fs-gg-ui-template-released` repository_dispatch

**Contract id**: `fs-gg-ui-template` (cross-repo registry) — this document records the **sender**
half. The receiver is the source of truth for the event id and payload shape; the sender MUST match
it. If either side changes, the `fs-gg-ui-template` registry/contract entry is authoritative and MUST
be updated in the same change (FR-009). This is a **contract realization**, not a contract change —
the shape below is dictated by the already-deployed receiver.

## Parties

| Role | Repo | Component |
|------|------|-----------|
| Sender | `FS-GG/FS.GG.Rendering` | `.github/workflows/template-dispatch.yml` + `scripts/template-released-dispatch.sh` (this feature) |
| Receiver | `FS-GG/FS.GG.Templates` | `.github/workflows/upstream-bump.yml` (exists; unchanged) |

## Transport

`POST https://api.github.com/repos/FS-GG/FS.GG.Templates/dispatches` (GitHub REST
`create a repository dispatch event`), authenticated with the cross-repo credential
(`secrets.TEMPLATES_DISPATCH_TOKEN`). Equivalent `gh` form:

```sh
gh api -X POST /repos/FS-GG/FS.GG.Templates/dispatches \
  -f event_type=fs-gg-ui-template-released \
  -F 'client_payload[version]=0.1.50-preview.1'
```

## Payload schema

```jsonc
{
  "event_type": "fs-gg-ui-template-released",   // constant — MUST match exactly
  "client_payload": {
    "version": "0.1.50-preview.1"               // required, non-empty;
                                                // ^[0-9]+\.[0-9]+\.[0-9]+(-preview\.[0-9]+)?$
  }
}
```

- `event_type`: the literal `fs-gg-ui-template-released`. (FR-001)
- `client_payload.version`: the released `FS.GG.UI.Template` coherent-set version, in the receiver's
  exact form. Derived from the tag ref (strip `refs/tags/fs-gg-ui-template/v`). (FR-002, FR-003)
- No other fields are required by the receiver; none are added.

## Preconditions (sender)

1. Trigger tag matches `fs-gg-ui-template/v*`. (FR-007)
2. Running on `FS-GG/FS.GG.Rendering` (not a fork). (FR-005)
3. `client_payload.version` derivable and valid. (FR-002, FR-006)
4. Cross-repo credential present. (FR-004)

If any precondition fails, **no dispatch is sent** and (for 3 and 4) the job fails visibly (FR-006).

> The workflow also exposes a manual `workflow_dispatch` entry. A manual run lands on a non-tag ref
> (e.g. `refs/heads/main`), so precondition 3 (version derivable) fails and the job fails loudly —
> it never sends a spurious dispatch. The manual entry is for operator inspection, not a send path.

## Postconditions (receiver — out of scope, documented for the round trip)

- A successful dispatch causes `upstream-bump.yml` to open a pin-bump PR moving the scaffold-provider
  pin to `client_payload.version` (or no-op if already pinned — idempotent).
- The version arriving at the receiver equals the version sent (SC-002, no drift).

## Compatibility

- Changing `event_type` or the `version` field name/shape is a **contract change**: update both
  repos and the `fs-gg-ui-template` registry entry together (FR-009).
- Adding optional payload fields the receiver ignores is backward-compatible but still warrants a
  registry note.
