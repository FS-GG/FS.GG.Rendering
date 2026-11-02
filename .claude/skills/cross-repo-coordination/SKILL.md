---
name: cross-repo-coordination
description: Use when work crosses FS-GG repositories: request another repo, sequence a contract change, reconcile version drift, or record a shared decision on the Coordination board.
---

# Cross-repo coordination (FS-GG)

Coordinate through GitHub issues, the Coordination board, versioned contracts, and ADRs—never a shared
file mailbox.

## Choose the route

- Request or answer work, message a holder, or manipulate board fields: load
  [mailbox-and-board](references/mailbox-and-board.md).
- Change a cross-repo API/schema/behavioral promise: load
  [contract-changes](references/contract-changes.md).
- Publish a coherent package/tool/template set and update consumers: load
  [coherent-releases](references/coherent-releases.md).
For exact request/response recipes, registry details, and incident rationale, load
[deep detail](references/deep-detail.md).

For organization programme requests, read the [mailbox and board boundary](references/mailbox-and-board.md).
Use one genuine owning issue with Owner, Done when, Current deliverable, Next action, Blocked on and
Acceptance evidence; reuse an existing matching outcome and its actual PR. Keep narrow current slice
paths distinct from the broad outcome. A durable cross-team dependency can justify an issue; an edit,
CI failure or checkpoint alone does not. Native dependency edges and owning-plan prerequisites retain
separate action-specific meaning. No organization `fsgg.coord.intake/v1` transaction, Class/severity,
Phase, claim or done-stamp is required or authorized by this route. Project membership changes need
their own admitted enrollment route. Issue-author permission does not grant parent operation authority.

Use [canonical intake](https://github.com/FS-GG/.github/blob/main/.agents/skills/work-board/references/intake.md) for new requests. Resolve the actual
qualified caller and selected binding under the
[organization contract](../check-board/references/organization-v2-inspection.md). Accepted organization
cutover and retained preparation/history have distinct scopes; instruction edits do not activate callers. Product contracts and explicitly historical protocol evidence retain
their scoped meaning, without becoming a fallback for unavailable V2 facts.

Finish by verifying the receiving issue/PR, board state, registry state, and any consumer update against
live sources. Record durable decisions as ADRs in `.github`.

## Receiver workflow migrations

When migrating a receiver from a hand-copied workflow invocation to a
declaration-driven tool mode, first look for the producer-owned behavioral
selftest. Keep resolver behavior there and add a small inline exact pin at each
receiver call site; do not create a second receiver-owned fixture list unless
the tool lacks the behavior coverage. The pin must have executable wrong-call
controls, so a retired form, wrong declaration path, or extra flag becomes red.
See [receiver-proj migration shape](https://github.com/FS-GG/.github/blob/main/docs/coordination/receiver-proj-migration-shape.md)
for the current `skill-view generate --receiver-proj` decision and acceptance. (Absolute URL, not a
relative link: `docs/` is not part of the `kit:` transport `registry/repos.yml` declares — a
coordination-kit receiver never materializes it, so a relative link here would dangle in every
receiver tree. .github#2343.)
