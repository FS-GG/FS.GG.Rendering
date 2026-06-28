# Contract: Root-buildable generated product (`fs-gg-ui-template`)

This is the **template emission + build interface** contract for Feature 212. It is the surface
FS.GG.SDD's composition-acceptance probes (FS-GG/.github#16) consume. Registry id: `fs-gg-ui-template`.
Tracker: FS-GG/FS.GG.Rendering#9.

## C1 — Emitted root artifacts (template → product)

A product scaffolded with `dotnet new fs-gg-ui --name <Name>` MUST contain, at the product root:

| Artifact | Requirement |
|---|---|
| `<Name>.slnx` | exactly one root solution; references `src/<Name>/<Name>.fsproj` and `tests/<Name>.Tests/<Name>.Tests.fsproj`. |
| `global.json` | pins the `net10.0` SDK band (`rollForward: latestFeature`, `allowPrerelease: false`). |
| `build.sh` | POSIX verb wrapper (see C3). |
| `build.cmd` | Windows verb wrapper (parity with `build.sh`). |
| `Directory.Build.props` | (pre-existing) `net10.0` + lockfile policy; root build inherits it. |

Emission invariants:
- Emitted for **every** `profile` (`app`, `headless-scene`, `governed`, `sample-pack`).
- Emitted for **every** `lifecycle` (`spec-kit`, `sdd`, `none`).
- **Byte-neutral** across `designSystem` (`wcag`, `ant`).
- `<Name>` substitution applied to the `.slnx` filename, its project paths, and project references.

## C2 — Stock toolchain interface (product root)

From the product root, with only the stock .NET CLI (and the pinned SDK), all of the following MUST
succeed for a freshly scaffolded product:

```
dotnet restore            # resolves the root .slnx
dotnet build              # builds src + tests via the root .slnx
dotnet test               # discovers & runs tests/<Name>.Tests
dotnet run --project src/<Name>   # runnable (app) profile: executes entrypoint, exits cleanly
```

- No FAKE invocation, no knowledge of `build.fsx`, required for the above.
- `dotnet run` in a headless environment MUST exit 0 (the entrypoint degrades via
  `UnsupportedEnvironment`); on a display it opens the product window.
- The project set built by this stock path MUST equal the set FAKE builds (no divergence).

## C3 — Verb wrapper interface (`build.sh` / `build.cmd`)

```
./build.sh <verb>      # verb ∈ { restore, build, test, run, verify, pack }
build.cmd <verb>       # Windows equivalent
```

| Verb | Delegates to (FAKE target) | Semantics |
|---|---|---|
| `restore` | `Restore` (new) | stock restore over root `.slnx` |
| `build` | `Build` (new) | stock build over root `.slnx` |
| `test` | `Test` (existing) | **unchanged** from today |
| `run` | `Run` (new) | `dotnet run --project src/<Name>` |
| `verify` | `Verify` (existing) | **unchanged** rich evidence+test path |
| `pack` | `Pack` (new) | stock pack over root `.slnx` (`-c Release`) |

- Every verb routes through `dotnet fsi build.fsx -t <Target>` (single FAKE entry).
- Unknown or missing verb → print the supported-verb list, exit non-zero.
- `verify`/`test` semantics MUST be identical to invoking FAKE `Verify`/`Test` directly (FR-007/SC-004).
- Both shells expose the same verbs with equivalent behavior (SC-003).

## C4 — Release regression gate (`release.yml` → `template-product-tests`)

The release-only job MUST, after instantiating a product:

```
dotnet build  <product-root>            # asserts stock root build succeeds
dotnet test   <product-root>            # asserts stock root test succeeds
dotnet run --project <product-root>/src/<Name>   # asserts exit 0 (runnable profile)
```

- A regression in any of the three MUST fail the job and block the release (SC-005).
- The job runs only on the canonical repo (existing `if:` guard preserved).

## C5 — Cross-repo coherence

- This is a `contract-change` to `fs-gg-ui-template`. Resolution MUST update
  `FS-GG/.github` → `registry/dependencies.yml` + `docs/registry/compatibility.md` to record the
  root-buildable guarantee and link tracker FS-GG/FS.GG.Rendering#9, per ADR-0001.

## Non-goals / frozen

- No change to FAKE `Verify` semantics.
- No change to product source modules (`Program.fs`, `Model.fs`, …), `Directory.Packages.props` central
  versions, or pack output location.
- No new package dependency.
