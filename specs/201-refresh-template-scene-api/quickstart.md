# Quickstart: Validate the Refreshed fs-gg-ui Template

A runnable guide to prove the refresh end-to-end. Run from the repo root
(`/home/developer/projects/FS.GG.Rendering`). This is a validation/run guide — the actual edits live
in `tasks.md` and the implementation phase. See [contracts/](./contracts/) for pass conditions.

## Prerequisites

- .NET 10 SDK; `dotnet` on PATH.
- Local NuGet feed configured at `~/.local/share/nuget-local/` (the template ships a `nuget.config`
  pointing there).

## Step 1 — Pack the local feed and note the version `V`

```bash
dotnet fsi scripts/refresh-local-feed-and-samples.fsx package-feed
ls ~/.local/share/nuget-local/ | grep -i 'fs.gg.ui.scene'   # → V appears in the .nupkg name
```

Record `V` (the produced version). This is the re-pin target for `FsSkiaUiVersion`.

## Step 2 — Generate, restore, and build every profile

```bash
for p in headless-scene governed app sample-pack; do
  dir="$(mktemp -d)/Product-$p"
  dotnet new fs-gg-ui --name Product --profile "$p" -o "$dir"   # strips the inactive //#if branch
  ( cd "$dir" \
    && dotnet restore \
    && dotnet build -c Release \
    && echo "PROFILE $p: BUILD OK" )
done
```

Expected: each profile restores all `FS.GG.UI.*` to `V` (no NU16xx) and builds with **zero** errors and
zero API-drift warnings (contract C1 / SC-001). Any compile error here is a concrete drift item to fix
in the seed source — do not edit blind from the `.fsi` diff (see research Decision 1).

> `template/base` itself is **not** buildable directly: each seed file carries both the profile branch
> and its `//#else` branch, which collide for the F# compiler. Always generate first.

## Step 3 — Emit evidence per branch

Headless profiles (`headless-scene`, `governed`):

```bash
cd "$dir"   # a headless-generated product
dotnet run -- --scene-evidence  readiness/headless-scene-evidence.txt   # status=ok
dotnet run -- --layout-evidence readiness/layout-evidence.txt           # status=ok, ReadableLayout
```

Interactive profiles (`app`, `sample-pack`) — host launch may report `unsupported` on a headless host;
that is acceptable (never `failed`):

```bash
cd "$dir"   # an interactive-generated product
dotnet run -- --scene-evidence  readiness/scene-evidence.txt            # status=ok
dotnet run -- --layout-evidence readiness/layout-evidence.txt           # status=ok, accepted=true
dotnet run -- --launch-evidence readiness/evidence-launch-mode.txt      # ok | unsupported
dotnet run -- --image-evidence  readiness/game-image-evidence.png       # ok | unsupported
```

## Step 4 — Run the governance + verify gate

```bash
cd "$dir"
dotnet fsi build.fsx target Verify    # Dev + GeneratedGuidanceCheck + TemplateDrift + EvidenceGraph + EvidenceAudit + Test
```

Expected: exit 0 for every profile (FR-008 / SC-005).

## Step 5 — Confirm the version invariants

```bash
# Pin equals produced feed version V (set this in Directory.Packages.props during implementation)
grep '<FsSkiaUiVersion>' template/base/Directory.Packages.props        # → V

# Exactly one FS.GG.UI version literal; all pins use the property
grep -c '<FsSkiaUiVersion>' template/base/Directory.Packages.props      # → 1
grep -E 'Include="FS.GG.UI[^"]*" Version="[0-9]' template/base/Directory.Packages.props  # → no matches

# No stale (superseded) literal anywhere in the template that represents the live pin
grep -rn '<SUPERSEDED-VERSION>' template/base/                          # → none (or only illustrative docs)
```

## Step 6 — Confirm the bundled Scene reference matches the live surface

```bash
# Every type/member presented as current in the bundled reference resolves in the live surface.
# (Compare conceptually against the split live files, not byte-for-byte — they are organized differently.)
ls src/Scene/*.fsi
sed -n '1,40p' template/base/docs/api-surface/Scene/Scene.fsi
```

Expected: no construct presented as current in the bundled reference is absent from `src/Scene/*.fsi`
(FR-005 / SC-004).

## Success = all of

- Steps 2 & 4 green for all four profiles (SC-001, SC-005).
- Step 3 evidence `ok` (interactive launch may be host-`unsupported`) (SC-006).
- Step 5 invariants hold (SC-003).
- Step 6 reference agrees with the live surface (SC-004).
