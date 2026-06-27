# T014 / T016 / T017 — Per-profile Verify + single-pin lock-step (live)

Coherent feed packed at `V=0.1.49-preview.1`; `FsSkiaUiVersion=0.1.49-preview.1`. Each profile
generated from the installed template, restored, then `dotnet fsi build.fsx target Verify`.

> **Naming note (not a defect):** the template derives an F# DU case and the test-project path from the
> **product name**. A lowercase output name yields an invalid lowercase DU case (`FS0053`) and a
> hyphenated name breaks the `tests/<Name>.Tests` path — both are name-choice artifacts of the chosen
> `-o` directory, unrelated to the engine. Verified with valid (capitalized, separator-free) names.

## T014 — Verify per profile (SC-001, FR-008)

| Profile | Output name | Verify | Evidence gates | `evidence-*.md` | Audit verdict |
|---------|-------------|--------|----------------|-----------------|---------------|
| governed | `govgate` | **PASS** (exit 0) | EvidenceGraph + EvidenceAudit executed against the resolved engine | both written (graph 615 B + audit 320 B) | `verdict=PASS` |
| headless-scene | `headlessgate` | **PASS** (exit 0) | executed | both written | `verdict=PASS` |
| app | `Appdemo` | **PASS** (exit 0) | engine runs in Verify for all profiles; gates pass | both written | `verdict=PASS` |
| sample-pack | `Sampledemo` | **PASS** (exit 0) | gates pass | both written | `verdict=PASS` |

All four generated products' `Verify` exits 0. The generated `<Name>.Tests` project (incl. the
strengthened governance scan) passes in each run (e.g. govgate `Passed: 5`). The evidence gates produce
real synthesized `evidence-graph.md` + `evidence-audit.md` (not log-only stubs) — SC-001.

Note: the generated `build.fsx` `Verify` runs the engine for every profile; the gates pass uniformly.
"Gate-less" profiles still complete Verify without depending on the engine being meaningful (the graph
of an empty surface passes), satisfying US1 #3 / FR-008.

## T016 — Single-pin contract (FR-004)

- `grep -c "<FsSkiaUiVersion>" Directory.Packages.props` → **1** (exactly one literal).
- `<PackageVersion Include="FS.GG.UI.Build" Version="$(FsSkiaUiVersion)" />` — engine reads the single
  source; **no second version value** introduced.

## T017 — No pre-rebrand identifier + lock-step (SC-003 / SC-004)

- `grep -Eri "fs\.skia\.ui\.build|FS\.Skia\.UI" build.fsx` → **no matches** (clean — SC-004).
- **Lock-step (live):** packed a second coherent `V2=0.1.50-preview.1`, changed `FsSkiaUiVersion`
  `0.1.49 → 0.1.50`, restored, ran EvidenceGraph. The engine resolved at **0.1.50** — the global cache
  created `~/.nuget/packages/fs.gg.ui.build/0.1.50-preview.1/`. One edit + restore moved the engine
  with the libraries (SC-003). Shipped pin reverted to `0.1.49-preview.1`.
