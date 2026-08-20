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

Every request needs one owning repo, acceptance criteria, a narrow touch-set, and explicit dependencies.
Author the complete `fsgg.coord.intake/v1` draft shown in [deep detail](references/deep-detail.md), then
run `scripts/fsgg-coord intake validate` followed by `intake apply` on that same file. Its `paths`,
`class`, `severity`, and optional `blockedBy` fields own the body and board projections; hand-authoring
`Paths:` or `Class:` in a created body is a defect. Use `blockedBy` for real sequencing, not topical
relationship or temporary overlap.

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
