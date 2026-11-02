# Mechanical reconciliation

Historical/compatibility evidence only. The current [check-board entrypoint](../SKILL.md)
is read-only and grants no repair, claim, queue replay or field-write authority.
Any genuine product repair/recovery requires its own actual qualified caller, target,
original custody and separately admitted operation. Archived commands are not a live fallback.

## Organization scope is read-only

For prepared .9/.11 organization consumers use only
[organization inspection](organization-v2-inspection.md), after its actual adoption gates. Report human
planning discrepancies and scoped read gaps; never use claim/done/merged facts to auto-write Status.
The remaining reconcile/apply/receipt mechanics belong only to separately qualified product or legacy
history. Historical organization examples are data only, never a live opt-in. Any live product
reconcile/apply first requires [check-board's verified product target/repository gate](../SKILL.md#separately-qualified-product-scope).
These recipes grant no current organization writes, enrollment or V1 fallback. The canonical generator
retires organization V1 filing/reconcile/status/snapshot projections without changing Core/product facts.


`scripts/fsgg-coord reconcile` owns classification. Do not recreate its finding table with `jq`.
The six typed chores are stale-claim collection, claim/Status lag, claim/review lag, closed issue/Done
lag, cleared-blocker/Ready lag, and open-blocker/Blocked lag. `--apply` performs only those remedies.

Within that separately qualified scope, use `--repo NAME` to narrow reported/applied subjects.
Omitting it never selects the current organization V2 queue.
Use `--json` when another tool consumes the dry-run result. Apply after inspecting that result;
`--apply` is deliberately human-readable so mutation output cannot corrupt a JSON document.

## Executable receipt examples

These offline probes are bounded and make no board writes. The positive receipt proves both intended
values were freshly observed. The partial receipt proves a failed comparison retains both actual
values, including the stale dependency. The missing-row receipt proves no observation is invented.

```bash
bash .agents/skills/check-board/references/receipt-examples.sh
```




Manual REST reads are reserved for `lint` findings whose facts were unknown. Read the exact issue or
PR named by the finding; preserve unknown when the read fails. Never use a broad issue-body rewrite as
a board repair.
