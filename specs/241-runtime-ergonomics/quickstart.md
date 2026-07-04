# Quickstart / Validation: FS.GG.UI runtime ergonomics polish

End-to-end validation that the three items work. Run from repo root. See
[contracts/adapter-noop.md](./contracts/adapter-noop.md) and [contracts/guidance.md](./contracts/guidance.md)
for the surfaces; this is the run guide only.

## Prerequisites

- .NET SDK `net10.0`; repo builds (`dotnet build`).
- The behavioral test and surface gate live in `tests/Package.Tests`.

## 1. §3.5 — no-op aliases compile and equal `[]`

```sh
# after adding the .fsi/.fs no-ops and refreshing the baseline:
dotnet fsi scripts/refresh-surface-baselines.fsx      # writes the +2 lines
git diff readiness/surface-baselines/FS.GG.UI.Controls.Elmish.txt   # expect +Cmd, +Sub only
dotnet test tests/Package.Tests --filter SurfaceArea   # Principle II drift gate: green
dotnet test tests/Package.Tests --filter <NoOpAlias test name>      # laws: Cmd.none = [], Sub.none = []
```

Expected: baseline gains exactly `FS.GG.UI.Controls.Elmish.Cmd` and `…Sub`; the law test passes;
`AdapterCmd.productMessages Cmd.none = []`.

## 2. Live scaffold reproduction (Foundational — before finalizing guidance)

```sh
# scaffold a game product, then in it:
#   §3.4  add `type Msg = KeyDown of KeyId | ...` + a mapKey returning `Some (KeyDown k)`
#         -> reproduce the `does not match 'ViewerKey'` error PRE-fix;
#         after following the product.md collision line (qualify KeyboardMsg), it compiles.
#   §3.5  set update to `model, Cmd.none` and subscriptions to `Sub.none` -> compiles, behaves as [].
#   §3.6  place a HUD label from `(Scene.measureText text font).Width` -> renders, no magic numbers.
dotnet build   # real compiler feedback (NOT `fake.sh -t Dev`, which is a marker only)
```

Expected: pre-fix collision reproduced; post-guidance all three compile; HUD text positioned from
measured metrics.

## 3. §3.4 / §3.6 discoverability

```sh
grep -n 'KeyboardMsg' template/base/docs/product.md        # collision line present (beside Text/CloseRequested/Rect)
grep -n 'measureText' template/base/docs/product.md        # HUD idiom present
grep -rn 'measureText' template/product-skills/            # named in >=1 product skill
```

Expected: each grep hits; a consumer can find the pure measurer and the collision remedy without
reading framework source (SC-002).

## 4. Full gates

```sh
dotnet test tests/Package.Tests    # surface + skill-manifest/currency gates green (FR-008)
dotnet build template/... && dotnet test   # generated-product template still builds/tests (FR-006)
```

Expected: all green; no existing consumer, sample, or viewer call site regresses (SC-005).
