# Contract: Generated-Product Build & Evidence

A generated product must build and emit its expected scene/evidence for every profile (FR-007/FR-008,
SC-001/SC-005/SC-006). This contract defines the per-profile acceptance the refresh must satisfy and
that `/speckit-tasks` should encode as the verification gate.

## Preconditions

- Local feed packed at version `V` to `~/.local/share/nuget-local/`
  (`dotnet fsi scripts/refresh-local-feed-and-samples.fsx package-feed`).
- `template/base/Directory.Packages.props` `FsSkiaUiVersion` == `V`.

## Per-profile acceptance

For each profile `p ∈ {app, headless-scene, governed, sample-pack}`:

| Step | Command (in generated dir) | Pass condition |
|------|----------------------------|----------------|
| Generate | `dotnet new fs-gg-ui --profile p -o <dir>` | exit 0; inactive `//#if` branch stripped |
| Restore | `dotnet restore` | exit 0; all `FS.GG.UI.*` → `V`; no NU16xx |
| Build | `dotnet build -c Release` | exit 0; **zero** errors / zero API-drift warnings |
| Verify | `dotnet fsi build.fsx target Verify` | exit 0; Test + EvidenceGraph + EvidenceAudit pass |

## Per-branch evidence

**Headless branch (`headless-scene`, `governed`)** — pure Scene:

| Command | Pass condition |
|---------|----------------|
| `dotnet run -- --scene-evidence <out>` | `status=ok`, `SceneEvidence.render` returns `Ok`, deterministic renderer |
| `dotnet run -- --layout-evidence <out>` | `status=ok`, `proof-level=ReadableLayout`, overlap-status clean |

**Interactive branch (`app`, `sample-pack`)** — Controls + Viewer (host launch may report
`unsupported` on a headless CI host; that is a pass for the evidence contract, not a failure):

| Command | Pass condition |
|---------|----------------|
| `dotnet run -- --scene-evidence <out>` | `status=ok` (deterministic scene) |
| `dotnet run -- --layout-evidence <out>` | `status=ok`, `accepted=true` |
| `dotnet run -- --launch-evidence <out>` | `status=ok` **or** `status=unsupported` (host-gated), never `failed` |
| `dotnet run -- --image-evidence <out>` | report written; `status=ok`/`unsupported`, never `failed` |
| `dotnet run` (no args) | host launches **or** reports `classification=UnsupportedEnvironment` (exit 0) |

## Governance gate (FR-008, SC-005)

`template/base/tests/Product.Tests/GovernanceTests.fs` passes for the active profile, including:
- headless: `--scene-evidence` exposed, deterministic renderer, no `Viewer.runApp`/`ControlsElmish`.
- interactive: source split in compile order (Model→View→LayoutEvidence→WindowOptions→
  EvidenceCommands→Program), host selection per family, evidence-graph/audit run in-process via
  reflection + `FsSkiaUiVersion` (no `#r` engine literal), clean text logs.

## Definition of done (maps to Success Criteria)

- All 4 profiles: generate + restore + build green → **SC-001**.
- Every seed Scene construct present in the live surface → **SC-002**.
- One `FsSkiaUiVersion` literal == `V`; no stale literal → **SC-003**.
- Bundled Scene reference matches live surface → **SC-004**.
- Governance + build/evidence green for all profiles → **SC-005**.
- Each profile emits expected scene/evidence (host-gating allowed for interactive launch) → **SC-006**.
