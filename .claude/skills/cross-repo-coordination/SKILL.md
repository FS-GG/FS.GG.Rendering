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

Every request needs one owning repo, acceptance criteria, a narrow `Paths:` declaration, and explicit
dependencies. File it in the receiver, add it to the org board, and set only fields you can support with
evidence. Use `Blocked by:` for real sequencing, not topical relationship or temporary overlap.

Finish by verifying the receiving issue/PR, board state, registry state, and any consumer update against
live sources. Record durable decisions as ADRs in `.github`.

## Receiver workflow migrations

When migrating a receiver from a hand-copied workflow invocation to a
declaration-driven tool mode, first look for the producer-owned behavioral
selftest. Keep resolver behavior there and add a small inline exact pin at each
receiver call site; do not create a second receiver-owned fixture list unless
the tool lacks the behavior coverage. The pin must have executable wrong-call
controls, so a retired form, wrong declaration path, or extra flag becomes red.
See [receiver-proj migration shape](../../../docs/coordination/receiver-proj-migration-shape.md)
for the current `skill-view generate --receiver-proj` decision and acceptance.
