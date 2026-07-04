# Quickstart / validation: Scaffold discoverability sharpening (spec 242)

Runnable checks that prove both asks end-to-end. Run from the repo root unless noted.

## Prerequisites

- .NET SDK per `global.json`; the `fs-gg-ui` template installable/instantiable locally (`dotnet new install` on the packed template, as the `scripts/validate-*-template.fsx` harness does).

## 1. Build-entry help banner (Contract A)

In a scaffolded product (or `template/base/` treated as an instantiated `app` tree):

```sh
dotnet fsi build.fsx --help          # also: -h, help
```

**Expect**: banner lists targets and states that `Dev` does not compile, `Test` is the first real `dotnet test`, and `Verify`'s audit hard-blocks until every task is `[X]`. Exit code `0`. Then confirm no side effect:

```sh
test ! -e readiness/logs/Dev.txt && echo "no Dev log written by --help"   # passes if help wrote nothing
./build.sh --help                                                          # shell wrapper shows the same banner
```

## 2. Per-profile SWAP-CHECKLIST.md (Contracts B/C)

Instantiate each profile and confirm the checklist ships with family-correct content:

```sh
for p in game app sample-pack governed headless-scene; do
  dotnet new fs-gg-ui --profile "$p" -o /tmp/swapcheck-$p >/dev/null
  test -e /tmp/swapcheck-$p/SWAP-CHECKLIST.md && echo "$p: SWAP-CHECKLIST.md present"
done
```

**Expect** for the `game` tree: the checklist lists `mapKey` (EvidenceCommands), the `LayoutEvidence` readers (`activeGameplayBoundsForSize`, `scoreTextBounds`, …), the rewrite-wholesale files, and a `docs/scaffold-map.md` pointer. Spot-check that every symbol named exists:

```sh
grep -o 'activeGameplayBoundsForSize\|scoreTextBounds\|mapKey' /tmp/swapcheck-game/SWAP-CHECKLIST.md | sort -u
grep -rl 'activeGameplayBoundsForSize\|scoreTextBounds\|mapKey' /tmp/swapcheck-game/src/*/    # symbols are real
```

## 3. Deterministic gates (no `dotnet new` needed)

```sh
# Template-authoring correctness: no phantom symbols, all durable re-point functions covered.
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter "SwapChecklist|BuildHelpBanner"

# Generated-product presence + banner/product.md sync (durable governance spine).
dotnet test template/base/tests/Product.Tests/Product.Tests.fsproj --filter "product-governance"
```

**Expect**: green. The template gate fails if the checklist names a symbol absent from the source or omits a known re-point function; the governance gate fails if `SWAP-CHECKLIST.md` is missing or the banner/`product.md` semantics drift.

## 4. Swap-survival regression (SC-005)

Confirm a legitimate additive model swap still passes the durable gate (the new artifacts add no new failing scan): apply a trivial additive model field in a scaffolded tree and run `-t Test`, then `product-governance` — both stay green because the checklist is advisory and presence-only.

## Done when

- Help banner surfaces the three targets' semantics with no side effects (checks 1).
- Every profile ships a family-correct `SWAP-CHECKLIST.md` with only real symbols (checks 2).
- Template + governance gates green; sync gate catches induced drift (checks 3).
- Additive swap regression stays green (check 4).
