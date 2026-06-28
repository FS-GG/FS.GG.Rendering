# Contract: `shared-build-config` adoption (FS.GG.Rendering consumer side)

This feature is the **consumer** side of the org `shared-build-config` contract. The producer side
(source of truth, sync script, drift semantics) is FS-GG/.github `dist/dotnet/` + ADR-0006
(`.github#19`, merged). FS.GG.Rendering does not define the contract; it satisfies the adoption
obligations below. These are the acceptance guards that `/speckit-tasks` turns into work + tests.

## Obligations this repo must satisfy

| ID | Obligation | Verified by |
|---|---|---|
| C-1 | The three managed files (`Directory.Build.props`, `Directory.Packages.props`, `.config/dotnet-tools.json`) are present and byte-identical to the canonical source. | `sync-build-config.sh --check .` exits 0 |
| C-2 | The restore-lock gate is spelled `GITHUB_ACTIONS` (not `ContinuousIntegrationBuild`) and guarded by `Exists(lockfile)`. | `RestoreLockTests` (updated) + locked/unlocked gate probes |
| C-3 | No package pinned by the org baseline is re-declared locally; `FSharp.Core` resolves to `10.1.301`. | clean restore (no `NU1504`/`NU1011`); lockfile shows `10.1.301` |
| C-4 | The effective build still promotes `NU1603`/`NU1608` to errors (the `WarningsAsErrors` append rule held). | deliberate-substitution probe fails `--locked-mode` restore |
| C-5 | `.config/dotnet-tools.json` `fake-cli` version equals the `Fake.Core.*` library pin (`6.1.4`); the compiled-FAKE build path is unaffected. | manifest vs `Directory.Packages.local.props`; `dotnet run --project build` still works |
| C-6 | Restore→build→test is green; a second restore is byte-reproducible (all lockfiles unchanged). | full cycle + repeated restore (no git diff) |
| C-7 | All repo-specific settings still take effect (warnings, metadata, fsdocs, package pins) via the local override files. | build behaves as before adoption; package-version reads pass |
| C-8 | `template/base/**` is unchanged (separate template contract). | `git diff --quiet -- template/base` |

## Out of scope (explicit)

- The org reusable drift-check workflow `contract-coherence.yml` (`.github#18`) — Backlog/blocked;
  ongoing CI drift gating is a bounded follow-up (research R6). This feature delivers a drift-clean
  adoption and relies on the existing `--locked-mode` gate for dependency enforcement.
- Any change to `template/base/` emitted build files (those keep their own gate for generated
  products under the `fs-gg-ui-template` contract).
- Adopting the shared config in the other three repos (separate H3 board items: SDD#11,
  Governance#16, Templates#16).

## Cross-repo coordination

On completion, the Coordination board item FS.GG.Rendering#11 moves Ready → In review/Done. No new
cross-repo request is created; this consumes an already-merged contract. If transitive pinning or the
baseline surfaces an incoherence with the producer, raise it per the `cross-repo-coordination` skill
against FS-GG/.github rather than forking the managed files.
