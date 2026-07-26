---
name: intra-repo-parallel-work
description: Use when multiple workers must operate concurrently inside one FS-GG repository. Coordinate minted identities, claim locks, isolated worktrees, disjoint touch-sets, leases, and overlap messages.
---

# Intra-repo parallel work (FS-GG)

Parallel work is safe only while identity, claim, worktree, touch-set, and lease remain aligned.

## Required invariants

1. Mint one unique `FSGG_WORKER` per worker; never key ownership on the shared GitHub account.
2. Claim through `take`/`claim` and proceed only when its receipt converges.
3. Use one fresh worktree and `item/<number>-*` branch per claim.
4. Edit only the declared `Paths:` touch-set. Widen before editing; stop if the widened set overlaps.
5. Heartbeat live work. A lease is not broken until the typed reaper collects it.
6. Use `say`, `inbox`, and coordination rooms for overlap; do not communicate through shared files.
7. Merge through the verified green gate, satisfy post-merge obligations, stamp done, then release/clean.

The scheduler, touch-set grammar, claim order, liveness, and messaging facts are generated from the
typed core in [protocol-facts](references/protocol-facts.md). Load
[worktrees-and-overlap](references/worktrees-and-overlap.md) when creating workers, changing paths, or
handling contention.
For extended rationale, lease/CAS failure modes, and historical edge cases, load
[deep detail](references/deep-detail.md).

For the end-to-end worker loop use [pnext-item](../pnext-item/SKILL.md). For cross-repository
dependencies use [cross-repo-coordination](../cross-repo-coordination/SKILL.md).
