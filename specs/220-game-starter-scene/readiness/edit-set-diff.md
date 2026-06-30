# T022 — Swap edit set matches the scaffold map (quickstart Scenario D; SC-003, SC-005)

From the clean `game` scaffold of [swap-to-pong.md](./swap-to-pong.md), `diff -rq` of the pristine
snapshot vs the swapped tree (excluding `bin`/`obj`/`readiness`):

```
changed: src/Product/Model.fs
changed: src/Product/View.fs
changed: tests/Product.Tests/BehaviorTests.fs
(only-in-swapped, restore byproduct: src/Product/packages.lock.json)
(only-in-swapped, restore byproduct: tests/Product.Tests/packages.lock.json)
```

## Classification check (against `docs/scaffold-map.md` / contract §C4)

| File | Scaffold-map class | In edit set? | OK |
|---|---|---|---|
| `src/Product/Model.fs` | Replaceable | ✅ changed | ✅ |
| `src/Product/View.fs` | Replaceable | ✅ changed | ✅ |
| `tests/Product.Tests/BehaviorTests.fs` | Replaceable | ✅ changed | ✅ |
| `src/Product/LayoutEvidence.fs` | Durable — re-point (only if swap touches its fields) | ⬚ unchanged | ✅ |
| `src/Product/EvidenceCommands.fs` | Durable — re-point (only if swap touches its fields) | ⬚ unchanged | ✅ |
| `src/Product/WindowOptions.fs` | Durable — untouched | ⬚ unchanged | ✅ |
| `src/Product/Product.fsproj` | Durable — untouched | ⬚ unchanged | ✅ |
| `src/Product/Program.fs` | Durable — untouched | ⬚ unchanged | ✅ |
| `tests/Product.Tests/GovernanceTests.fs` | Durable — untouched | ⬚ unchanged | ✅ |

**0 undocumented files forced to change.** The changed set ⊆ the scaffold-map *replaceable*
classification (this additive swap did not need the *re-point* set). `packages.lock.json` are
NuGet restore byproducts (the pristine snapshot predated any `dotnet restore`), not developer edits
or template-tracked source. SC-003 / SC-005: **the map's classification matches the real swap.**
</content>
