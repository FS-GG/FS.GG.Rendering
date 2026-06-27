# Quickstart: Verify → Record → Reconcile fs-skia-ui-version Coherence

Run guide proving the `fs-skia-ui-version` contract is coherent end-to-end. Order is mandatory:
**verify (US1) → record snapshot (US2) → reconcile cross-repo (US3)**. Never reconcile before verify.

## Prerequisites

- Repo at the resolution commit on `204-fs-skia-ui-version-coherence`.
- .NET 10 SDK; `dotnet new fs-gg-ui` template installed (or installable from `.template.config/`).
- Local feed dir `~/.local/share/nuget-local/`.
- `gh` authenticated with access to `FS-GG/.github` and `FS-GG/FS.GG.Rendering` (verified at plan time).

## Step 0 — Fix the phantom pins (template/base)

Remove the two `<PackageVersion>` entries for non-existent packages and their now-false comments from
`template/base/Directory.Packages.props`:
- `FS.GG.UI.Color` (retired in Feature 179; `src/ColorPolicy` is `IsPackable=false`).
- `FS.GG.UI.SkillSupport` (no producing project; absent from the feed).

Expected: `GovernanceTests` single-source `FsSkiaUiVersion` invariant still green; no remaining pin
references a package missing from the feed (contract CV-5).

## Step 1 — Pack the framework at HEAD

```sh
dotnet fsi scripts/refresh-local-feed-and-samples.fsx   # packs FS.GG.UI.* to ~/.local/share/nuget-local/
```

Note the version the packer assigned (expected `0.1.51-preview.1`). Re-pin
`template/base/Directory.Packages.props` `<FsSkiaUiVersion>` to exactly that value.

Expected: all **16** real `FS.GG.UI.*` IDs present at `<version>` in the feed (contract SM-A).

## Step 2 — Verify every profile (US1 — the gate)

For each profile in `app  headless-scene  governed  sample-pack`:

```sh
dotnet new fs-gg-ui --profile <p> -o /tmp/fsgg-<p>
cd /tmp/fsgg-<p>
dotnet restore   # locked
dotnet build
# evidence: product evidence CLI (--scene-evidence / --layout-evidence; app/sample-pack launch+screenshot)
# governance: dotnet test (Product.Tests / GovernanceTests)
```

Expected (contract `coherence-verification.md` CV-1..CV-5): restore with no NU1101 / no version
conflict; build with no Scene-API compile error; evidence emitted; governance green; exactly one
FS.GG.UI version literal (the `$(FsSkiaUiVersion)` value, not `0.1.0-preview.1`).

**Gate**: all four profiles must pass. Any red ⇒ stop; the contract stays incoherent (do not proceed
to Step 4).

## Step 3 — Record the reproducible snapshot (US2)

```sh
# committed lockfile(s): enable locked restore in the template, restore once, commit packages.lock.json
# manifest: fill contracts/snapshot-manifest.md with the 16 IDs @ <version>
git tag -a fs-skia-ui/v<version> -m "coherent FS.GG.UI.* snapshot for fs-gg-ui template pin"
git push origin fs-skia-ui/v<version>
```

Expected (SM-A..SM-D): restore the pinned template from a clean cache **twice** → identical resolved
set (SC-002); `pinned-version == tag == every manifest row`; re-checkout of the tag reproduces the set.

## Step 4 — Reconcile the cross-repo record (US3 — only after Steps 2 & 3 pass)

1. In `FS-GG/.github`: set the `fs-skia-ui-version` row to `coherent: true` in
   `registry/dependencies.yml` **and** its `docs/registry/compatibility.md` projection (together),
   referencing the resolving change.
2. On the request:

```sh
gh issue comment 1 --repo FS-GG/FS.GG.Rendering --body "## Response ... (option + evidence)"
gh issue close 1 --repo FS-GG/FS.GG.Rendering
```

Expected (XR-A..XR-E): registry coherent + projection agrees; issue #1 has `## Response` and is CLOSED;
both consistent with the Step 2/3 evidence; no FS-GG repo left with a stale "blocked / coherent: false"
signal (SC-005).

## Success summary (maps to Success Criteria)

| Step | Proves |
|------|--------|
| 2 | SC-001 (all profiles restore+build), SC-003 (one literal, not `0.1.0`) |
| 3 | SC-002 (reproducible), FR-003 (immutable snapshot) |
| 4 | SC-004 (registry coherent + issue closed, consistent), SC-005 (no stale signal) |
