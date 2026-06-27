# Quickstart: validate the fixed build.fsx governance engine

Runnable validation that the EvidenceGraph/EvidenceAudit gates resolve a real engine and run green in a
freshly generated product. Validation is per-profile against a **generated** product, not against
`template/base` in place. Details live in `contracts/` and `data-model.md`.

## Prerequisites

- .NET 10 SDK; this repo at `/home/developer/projects/FS.GG.Rendering`.
- Local feed dir `~/.local/share/nuget-local/` registered as a NuGet source (`local-feed`).
- The new engine project `src/Build/FS.GG.UI.Build.fsproj` compiles and is registered in
  `FS.GG.Rendering.slnx`.

## Step 1 — Pack a COHERENT feed at a fresh version (includes the engine)

Pick `V` above every per-project `<Version>` so the global NuGet cache cannot serve stale bits
(memory `template-feed-version-model`). The harness auto-discovers `src/Build` (FS.GG.UI.* + IsPackable).

```bash
V=0.1.49-preview.1   # example: above the current max per-project version
dotnet pack FS.GG.Rendering.slnx -c Release -p:Version=$V -o ~/.local/share/nuget-local
ls ~/.local/share/nuget-local/FS.GG.UI.Build.$V.nupkg   # MUST now exist (was the missing producer)
```

Set the single pin to match:

```bash
# template/base/Directory.Packages.props : <FsSkiaUiVersion>$V</FsSkiaUiVersion>
```

## Step 2 — Install template & generate each profile

```bash
dotnet new install . --force
# headless profiles include the evidence gates:
yes | dotnet new fs-gg-ui -o /tmp/p-governed       --profile governed
yes | dotnet new fs-gg-ui -o /tmp/p-headless       --profile headless-scene
# gate-less profiles must still pass Verify without the engine:
yes | dotnet new fs-gg-ui -o /tmp/p-app            --profile app
yes | dotnet new fs-gg-ui -o /tmp/p-sample         --profile sample-pack
```

## Step 3 — Early live smoke run (Foundational; confirms resolve/load/invoke)

Before trusting the audit rules, prove the engine resolves and runs in a generated product:

```bash
cd /tmp/p-governed
dotnet restore src/Product/Product.fsproj
dotnet fsi build.fsx target EvidenceGraph
# Expect: FS.GG.UI.Build resolves from local-feed; readiness/evidence-graph.md is written (real content).
ls readiness/evidence-graph.md
```

If the engine cannot resolve, confirm the failure NAMES `FS.GG.UI.Build $V` and the cache/feed path
(FR-005) — that is the correct honest-failure behavior, not a defect.

## Step 4 — Full Verify per profile

```bash
for d in /tmp/p-governed /tmp/p-headless /tmp/p-app /tmp/p-sample; do
  ( cd "$d" && dotnet restore src/Product/Product.fsproj && dotnet fsi build.fsx target Verify ) \
    && echo "PASS $d" || echo "FAIL $d"
done
```

Expected:
- `governed`, `headless-scene`: EvidenceGraph + EvidenceAudit **execute against the resolved engine**;
  `readiness/evidence-graph.md` and `readiness/evidence-audit.md` (with a `verdict` token) exist;
  Verify exits 0 (US1 #1–2, SC-001).
- `app`, `sample-pack`: Verify exits 0 (gate-less profiles still pass; US1 #3 / FR-008).

## Step 5 — Single-pin + no-pre-rebrand checks (US2, FR-002/004)

```bash
cd /tmp/p-governed
grep -c "FsSkiaUiVersion" Directory.Packages.props            # the single source
! grep -Eri "fs\.skia\.ui\.build|FS\.Skia\.UI" build.fsx       # MUST find nothing (no pre-rebrand id)
# change the single value + restore → engine moves with libraries (resolved engine version == new pin)
```

## Step 6 — Governance tests stay green (FR-007)

```bash
cd /tmp/p-governed
dotnet test tests/Product.Tests/Product.Tests.fsproj -m:1 --disable-build-servers
# in-process engine assertions, single-version resolution, clean text logs, no decommissioned scripts
```

## Step 7 — Engine surface baseline (Principle II, in-repo)

```bash
cd /home/developer/projects/FS.GG.Rendering
# the new engine has a curated .fsi and a surface-area baseline:
test -f readiness/surface-baselines/FS.GG.UI.Build.txt
dotnet build src/Build/FS.GG.UI.Build.fsproj   # compiles; surface-drift check stays green
```

## Pass criteria (maps to spec Success Criteria)

- SC-001: evidence + audit gates actually executed (real `evidence-*.md`), Verify green, for 100% of
  gate-including profiles.
- SC-002: zero extra manual engine-setup steps after restore.
- SC-003: exactly one FS.GG.UI/engine version value; single-value upgrade + restore suffices.
- SC-004: zero `fs.skia.ui.build` / `FS.Skia.UI` identifiers or cache paths in the generated build.
- SC-005: engine-unavailable runs fail with a message naming the engine + feed/location.
