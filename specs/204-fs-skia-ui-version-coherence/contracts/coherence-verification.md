# Contract: Per-Profile Coherence Verification (US1 — FR-001, FR-002, FR-008)

The gate that produces the evidence everything else depends on. A consumer scaffolds and builds a
working product against one consistent pinned set, on **every** profile.

## Profiles (all four required)

`app`, `headless-scene`, `governed`, `sample-pack`
(from `.template.config/template.json`).

## Procedure (per profile `<p>`)

1. **Pack** the framework at the resolution commit to the local feed
   (`scripts/refresh-local-feed-and-samples.fsx` → `~/.local/share/nuget-local/`). Record the version
   the packer assigned (expected `0.1.51-preview.1`).
2. **Generate**: `dotnet new fs-gg-ui --profile <p>` into a clean dir (strips inactive `//#if` branches).
3. **Restore** (locked): `dotnet restore` with the committed lockfile / `RestoreLockedMode`.
4. **Build**: `dotnet build`.
5. **Evidence**: run the profile's evidence/governance — the product's evidence CLI
   (`--scene-evidence`, `--layout-evidence`; app/sample-pack launch/screenshot) and
   `Product.Tests` `GovernanceTests`.

## Pass conditions (MUST hold for every profile)

| ID | Condition | Maps to |
|----|-----------|---------|
| CV-1 | Restore reports **no NU1101** (missing package) and **no version conflict**; every `FS.GG.UI.*` reference resolves to the single pinned version. | FR-001, SC-001 |
| CV-2 | Build reports **no compile error** attributable to Scene-API drift. | FR-002, SC-001 |
| CV-3 | The product emits its expected scene/evidence output; `GovernanceTests` green. | FR-002, FR-008 |
| CV-4 | Exactly one FS.GG.UI version **literal** appears (the `$(FsSkiaUiVersion)` value); it is **not** `0.1.0-preview.1`. | FR-004, SC-003 |
| CV-5 | No pinned `FS.GG.UI.*` ID references a non-existent package (no phantom `Color`/`SkillSupport` pins). | FR-001, FR-004 |

## Failure semantics (fail loud and closed)

- Any profile failing CV-1..CV-5 ⇒ the contract is **incoherent**; do **not** flip the registry/issue
  (FR-007; edge case "a profile builds but another does not").
- A missing/partial snapshot (e.g. a pinned ID absent from the feed) ⇒ stays `coherent: false`, request
  stays open with the blocker recorded.

## Evidence to capture (for the US3 `## Response`)

- Per-profile restore + build + evidence transcript (or summary) showing CV-1..CV-3 green.
- The resolved package list proving CV-1/CV-5 (single version, no phantom IDs).
- The verified pinned `<version>`.
