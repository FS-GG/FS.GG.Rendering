# Quickstart — Validate the R8 rebrand

Runnable checks that prove the rebrand is real, complete, coherent, behavior-neutral, and honest.
Each maps to a Success Criterion. Run from the repo root. Details live in
[`contracts/`](./contracts/) and [`data-model.md`](./data-model.md) — not duplicated here.

## Prerequisites

- .NET SDK (`net10.0`), as used by the existing build.
- A GL context for the GL-dependent default-tier suites (the dev baseline provides X11/GL).
- Local pack feed dir `~/.local/share/nuget-local/` (constitution pack location).

## 1. Decision recorded (SC-001 / FR-001)

```bash
sed -n '1,40p' docs/product/decisions/0001-package-identity.md
```
**Expect**: status `accepted` (no longer *deferred*); an explicit **rebrand to `FS.GG.UI.*`**;
the complete old→new map (10 modules + template); the publish-before-deprecate rule; starting
version `0.1.0-preview.1` and the old-ID freeze.

## 2. Coherent identity, no leftover brand prefix (SC-002 / FR-002, FR-006)

```bash
# All four facets read FS.GG.UI.* per module:
grep -RnE '<PackageId>|<AssemblyName>' src/*/*.fsproj
grep -Rn '^namespace FS\.' src --include='*.fsi' | sort -u

# Zero brand-prefix identity tokens left in PRODUCT SOURCE (history dirs excluded):
grep -rn 'FS\.Skia\.UI' src template .template.config .template.package \
  --include='*.fsproj' --include='*.fs' --include='*.fsi' --include='*.json'
# Expect: no output.

# Descriptive Skia usage PRESERVED (must still be present):
grep -rln 'SkiaViewer\|SkiaSharp' src | head
```
**Expect**: every module shows `FS.GG.UI.<M>` on package ID, assembly name, and namespace; the
product-source brand search returns nothing; `SkiaViewer`/`SkiaSharp` descriptive references remain.

## 3. Builds and the default tier passes (SC-003 / FR-004)

```bash
dotnet build FS.GG.Rendering.slnx
# Default tier ("Local inner loop") — see docs/validation/validation-set.md:
dotnet test tests/Color.Tests tests/Scene.Tests tests/Layout.Tests tests/Input.Tests \
  tests/KeyboardInput.Tests tests/Elmish.Tests tests/Controls.Tests tests/Testing.Tests \
  tests/SkiaViewer.Tests tests/Smoke.Tests tests/Lib.Tests
```
**Expect**: build succeeds; all suites pass — zero new failures attributable to the rename.

## 4. Public surface differs only by the prefix (SC-005 / FR-005)

```bash
dotnet fsi scripts/refresh-surface-baselines.fsx   # regenerate from renamed assemblies
git diff --stat tests/surface-baselines/            # renames + re-prefixed contents only
ls tests/surface-baselines/                          # FS.GG.UI.<M>.txt (no FS.Skia.UI.*.txt)
```
**Expect**: baselines are `FS.GG.UI.<M>.txt`; the normalized old↔new diff is **prefix-only** — zero
added/removed/retyped members (see [`contracts/surface-invariance.md`](./contracts/surface-invariance.md)).

## 5. Template instantiates on the new identity (SC-004 / FR-003)

```bash
test -f .template.package/FS.GG.UI.Template.fsproj            # file renamed
grep -n '"identity"\|"shortName"\|"packagePrefix"' .template.config/template.json
ls template/product-skills/                                    # fs-gg-* (no fs-skia-*)

# Generated-consumer contract (mirrors release-only Product.Tests):
dotnet new fs-gg-ui --name SmokeProduct --output /tmp/r8-smoke
grep -rn 'FS\.Skia\.UI' /tmp/r8-smoke || echo "OK: no old brand in generated output"
dotnet restore /tmp/r8-smoke && dotnet build /tmp/r8-smoke
```
**Expect**: template identity `FS.GG.UI.Template`, short name `fs-gg-ui`, skill folders `fs-gg-*`;
the generated project references only `FS.GG.UI.*`, and restore + build succeed.

## 6. Publish-before-deprecate, no overclaiming (SC-006, SC-007 / FR-007–009)

```bash
dotnet pack FS.GG.Rendering.slnx -o ~/.local/share/nuget-local/   # replacements exist FIRST
ls ~/.local/share/nuget-local/ | grep FS.GG.UI                    # 10 modules + Template
sed -n '1,30p' docs/bridge/package-deprecation-notice.md          # or contracts/deprecation-notice.md
```
**Expect**: `FS.GG.UI.*` `0.1.0-preview.1` packages present on the local feed; the deprecation notice
maps every old ID → new replacement, deprecates (not deletes) old IDs, and is marked **NOT yet
applied** — nothing claims the public feed was changed.

## 7. Docs honest, no dead links (SC-008 / FR-010, FR-011)

```bash
grep -rni 'retained\|unchanged\|no rename' docs/bridge PROVENANCE.md README.md
```
**Expect**: no document presents "identity retained/unchanged" as **current** truth; the bridge
note, old-repo-redirect Block B, and PROVENANCE state the rebrand at R8 with the import-time mapping
scoped as history; every in-repo cross-reference touched by the rename still resolves.
