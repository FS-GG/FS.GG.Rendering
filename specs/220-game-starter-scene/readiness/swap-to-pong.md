# T020 — Swap-to-Pong (quickstart Scenario B; SC-001, SC-004, FR-008)

**Setup:** a *fresh* `game` scaffold (`dotnet new fs-gg-ui --profile game --name Product`), snapshot
taken pristine, then the developer replaces the starter by editing **only the developer-owned
seam**: `src/Product/Model.fs`, `src/Product/View.fs`, `tests/Product.Tests/BehaviorTests.fs`.

**The swap performed (a representative "developer's own game" change):** add a developer-owned
`Rally` counter to the game `Model` that advances each `Tick`, surface `rally: N` in the `View`
HUD, and add a `BehaviorTests` assertion for it.

## Result — `Test` is GREEN

```
Passed!  - Failed: 0, Passed: 27, Skipped: 0, Total: 27 - Product.Tests.dll (net10.0)
```

(13 durable GovernanceTests + 14 replaceable BehaviorTests — the swap added one behaviour test.)

## SC-001 / SC-004 / FR-008 invariants — held

| Invariant | Evidence |
|---|---|
| **0 edits to `GovernanceTests.fs`** | `diff` pristine vs swapped → `GovernanceTests.fs: IDENTICAL (0 edits)` |
| **No `-- pong`-style flag** introduced | no pong arg/flag anywhere in the generated `src/` |
| Changed files ⊆ developer seam | `Model.fs`, `View.fs`, `BehaviorTests.fs` only (see [edit-set-diff.md](./edit-set-diff.md)) |
| Build + product tests green | 27/27 above |

This swap was purely **additive** (a new model field), so it did not even need the documented
`LayoutEvidence.fs` / `EvidenceCommands.fs` re-point — those re-points are only required when a swap
changes the specific model fields they read. The durable governance spine
(`GovernanceTests.fs`, `Program.fs`, `WindowOptions.fs`, `Product.fsproj`) never calls
`view`/`update`, so it kept compiling and passing untouched. **The few-file replace promise holds.**
</content>
