# Contract: Release-only root-buildability gate (real-release evidence)

**Owner**: `.github/workflows/release.yml` (gate jobs delivered by Feature 212; driven by Feature 215)
**Satisfies**: FR-001, FR-002, FR-003, FR-011 · SC-001, SC-002 · US1

This contract defines the load-bearing #9 evidence: the release-only gate that proves a product scaffolded
from the **template under release** is root-buildable with the stock .NET CLI, executed **on the real
release**.

## Trigger surface

| Event | `$VER` source | Publishes? |
|---|---|---|
| `release: published` | `github.event.release.tag_name` (`v…`→`…`) | yes |
| `push: tags: ['v*']` | `GITHUB_REF_NAME` | yes |
| `workflow_dispatch` (with `version`) | `inputs.version` | yes |
| `workflow_dispatch` (no `version`) | `0.0.0-dryrun.<run>` | **no — dry run, NOT valid #9 evidence** |

Repo guard: every job is `if: github.repository == 'FS-GG/FS.GG.Rendering'` (forks never run/publish).

## Required gate jobs (both MUST be green)

1. **`package-tests`** — `dotnet test tests/Package.Tests/Package.Tests.fsproj -c Release`.
2. **`template-product-tests`** — the root-buildability gate:
   ```bash
   set -euo pipefail
   dotnet new install .
   dotnet new fs-gg-ui --name GeneratedProduct --output "$work/GeneratedProduct"
   dotnet build "$PRODUCT_DIR"                          # stock root .slnx resolves src + tests
   dotnet test  "$PRODUCT_DIR"                          # generated Product.Tests run
   dotnet run --project "$PRODUCT_DIR/src/GeneratedProduct"   # app profile; exits 0 headless (safe-degrade)
   ```

## Postconditions / invariants

- **Blocking publish**: `publish-packages` declares `needs: [package-tests, template-product-tests]`; a
  publish is impossible unless both gates pass. A red gate ⇒ no publish ⇒ #9 stays OPEN (Edge "Partial
  publish": if a publish step fails after green gates, #9 stays OPEN until a fully green release exists).
- **No FAKE knowledge**: the gate uses only stock `dotnet` verbs at the product root (SC-001).
- **Behavioral parity**: the gate exercises the same template `dotnet new install .` produces; the published
  package MUST be identical (FR-003). The feature introduces **no** change to what the template emits (FR-011).
- **Real-release only**: a locally demonstrated or `workflow_dispatch`-dry-run gate is **not** acceptable #9
  evidence (spec Edge "Release gate must run on a real release"). The evidence artifact is the **green run URL
  of the real release**.

## Verification

- Confirm the gate job is green on the actual release run (capture URL for the #9 closing comment).
- Confirm `publish-packages` ran only after both gates and pushed the coherent set to
  `https://nuget.pkg.github.com/FS-GG/index.json`.
