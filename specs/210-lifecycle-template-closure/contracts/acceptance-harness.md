# Contract: Published-Package Acceptance Harness

`scripts/validate-published-acceptance.fsx` — validates the **published** `FS.GG.UI.Template` package (not the
working tree) and emits the Epic Acceptance Record. Mirrors the report-core + env-gated-live pattern of
`scripts/validate-lifecycle-template.fsx`.

## Invocation contract

| Mode | Command | Behavior |
|---|---|---|
| verdict-core (default) | `dotnet fsi scripts/validate-published-acceptance.fsx` | Env-free self-check: confirm the pinned package exists in the feed; assert the env-free facts derivable without `dotnet new`. Exit non-zero on a structural failure. No record write. |
| emit-report (env-free) | `dotnet fsi scripts/validate-published-acceptance.fsx --emit-report` | Write `readiness/epic-acceptance.md` from the verdict core with live-only lines synthesized and `provenance: verdict-core` disclosed (fresh-checkout fallback). |
| live (env-gated) | `FS_GG_RUN_PUBLISHED_ACCEPTANCE=1 dotnet fsi scripts/validate-published-acceptance.fsx` | Install the pinned `.nupkg`, run the 3×4 matrix, assert results, write the record with `provenance: live`. Uninstall/restore on exit (including on failure). |

## Inputs

- **Pinned package**: `FS.GG.UI.Template::0.1.51-preview.1` from `--add-source ~/.local/share/nuget-local/`.
- **Lifecycle values**: `spec-kit` (default), `sdd`, `none`.
- **Profiles**: `app`, `headless-scene`, `governed`, `sample-pack`.
- **Baseline**: pre-lifecycle template output per profile (as captured by Features 204/206).

## Behavior contract (live mode)

1. **Isolate + install**: install the pinned package such that `dotnet new list` resolves `fs-gg-ui` to the
   package version, NOT the working-tree source template. Fail loudly with a distinct, actionable message if
   the package is absent or the source template shadows it (Constitution VI — no silent pass).
2. **Matrix**: for each (lifecycle, profile), `dotnet new fs-gg-ui --profile <p> --lifecycle <l>` into a temp
   dir; record gated-set presence/absence and product presence.
3. **Assertions** (all must hold):
   - `spec-kit` (and the no-flag default): gated set **present**, all 4 profiles; default output is
     **byte-identical** (presence + content) to baseline → `diff-vs-baseline=none`.
   - `sdd`: gated set **absent**, product **present**, all 4 profiles; default−`sdd` differs in
     **only** gated paths.
   - `none`: gated set **absent**, **no orchestrator marker**, product **present**; `none == sdd`
     on the gated set.
   - Unknown lifecycle value is **rejected**.
4. **Build spot-check** (FR-003/FR-004 "buildable"): run `dotnet build` on the `app`-profile output for
   `lifecycle=sdd` and `lifecycle=none`; both MUST exit 0. The `spec-kit` default is not separately built
   (its buildability follows from byte-identity to the baseline). If the build toolchain/restore is
   unavailable, record `buildability: environment-limited` (Constitution V/VI — disclosed, never a silent
   pass) rather than asserting a pass.
5. **Cleanup**: uninstall the pinned package and restore the prior template state, even on assertion failure.
6. **Emit**: write `readiness/epic-acceptance.md` per the acceptance-record contract with `provenance: live`.

## Output contract

- Exit `0` ⇔ all assertions AND the build spot-check pass — or the build is disclosed `environment-limited`
  (live) / verdict core consistent (default). A failed build (exit ≠ 0) is a non-zero exit, not a silent pass.
- The written record satisfies [`acceptance-record.md`](./acceptance-record.md).
- Any synthesized line in non-live modes carries an explicit `provenance: verdict-core` disclosure.
