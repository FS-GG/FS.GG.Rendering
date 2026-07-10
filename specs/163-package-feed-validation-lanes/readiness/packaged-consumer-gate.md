# Packaged-consumer gate — readiness evidence (#300)

Captured 2026-07-10 on `item/300-the-packaged-consumer-path-is-ungated-no`, against
`src/*/*.fsproj` `<Version>` = `0.4.0-preview.1`.

The four out-of-solution sample suites (`AntShowcase`, `ControlsGallery`, `SampleApps`,
`SecondAntShowcase` — twelve `.fsproj`) consume the framework only as packed `FS.GG.UI.*` packages.
Before #300 nothing in CI restored or built them, and the mirror rule they depend on was asserted
only by `tests/Package.Tests/Feature163PackageFeedValidationTests.fs`, which is **release-only** and
not in `FS.GG.Rendering.slnx` — so it runs *after* the merge that breaks it, never on the PR.

Both halves now run. See `docs/ci/cadence-map.md` §4c for the required/non-required split.

## 0. The samples are discovered, not listed

Neither workflow names a sample. `PackageFeed.discoverPackageConsumingSamples` returns every
`samples/*/` whose own `nuget.config` maps `FS.GG.UI.*` to the local feed — that mapping *is* what
makes a sample an out-of-solution package consumer:

```
samples/AntShowcase  samples/ControlsGallery  samples/SampleApps  samples/SecondAntShowcase
```

`samples/CanvasDemo` and `samples/SymbologyBoard` carry no such mapping (they are in the slnx) and are
correctly excluded. A hardcoded list of four in two workflows would have rebuilt the very omission
this issue is about: a fifth consumer nothing enumerates is a consumer nothing gates. `package-feed`
exits `2` if discovery finds none, so an empty set cannot pass silently.

## 1. The green path — the samples actually build against a feed packed from the commit

`dotnet pack FS.GG.Rendering.slnx -c Release -o <feed>` produced **17** `.nupkg` (16 `FS.GG.UI.*`
packables + the `FS.GG.UI` BOM), then, with no `--sample` arguments:

```
package-feed --mode proof
```

```
package-feed status: passed
packages: 16
pins: 57
```
Exit `0`. From the emitted `source-proof.md`:

```
- Status: `passed`
- Restore log: `.../restore.log`
- Build log:   `.../build.log`

## Source Rules
- `FS.GG.UI.*` -> <scratch feed>
- `*`          -> https://api.nuget.org/v3/index.json
```

`build.log` records **12** `dotnet build` invocations, **12** `Build succeeded`, and the twelve
consumer assemblies:

```
AntShowcase.dll         AntShowcase.Core.dll         AntShowcase.Tests.dll
ControlsGallery.dll     ControlsGallery.Core.dll     ControlsGallery.Tests.dll
SampleApps.dll          SampleApps.Core.dll          SampleApps.Tests.dll
SecondAntShowcase.dll   SecondAntShowcase.Core.dll   SecondAntShowcase.Tests.dll
```

This is the part `ApiCompat` cannot reach: it compares public surfaces pairwise, so it never sees
that `AntShowcase.Core` opens eight of these packages at once. A restore proves they *resolve*; only
the build proves they *compose*. `--mode proof` therefore builds and does not stop at restore.

## 2. The red path — a hand-reverted PR #233

Renovate PR #233 (`chore(deps): update fs.gg.ui coherent set to 0.4.0`) merged with **4/4 green**
and was nonetheless wrong: it proposed the *published* `0.4.0`, read from `nuget.pkg.github.com`, to
projects whose `nuget.config` maps `FS.GG.UI.*` **exclusively** to the machine-local feed, which only
a local `dotnet pack` fills — at `0.4.0-preview.1`. No local pack can produce `0.4.0`, so the pin
could only ever fail to resolve (`NU1102`).

Reconstructed by rewriting every `samples/**` `FS.GG.UI.*` pin `0.4.0-preview.1` → `0.4.0`
(**17 files changed**), the gate fails:

```
package-feed status: failed
packages: 16
pins: 57
```
Exit `1`, with **57** `stale-pin` lines on stderr, each naming the package, both versions, and the
project to fix:

```
stale-pin: FS.GG.UI.Themes.AntDesign expected 0.4.0-preview.1 actual 0.4.0 in samples/AntShowcase/AntShowcase.Core/AntShowcase.Core.fsproj
stale-pin: FS.GG.UI.Controls        expected 0.4.0-preview.1 actual 0.4.0 in samples/AntShowcase/AntShowcase.Core/AntShowcase.Core.fsproj
... (57 total)
package-feed: the mirror rule: every FS.GG.UI.* pin under samples/ MUST equal <Version> in
src/*/*.fsproj. The four out-of-solution samples restore FS.GG.UI.* ONLY from the local feed, which a
local `dotnet pack` fills at <Version>; a pin naming any other version cannot resolve (NU1102). Fix
the pin, not <Version>.
```

Two properties of that failure are load-bearing:

- **It names the rule, not just the mismatch.** A bare exit code invites the next reader to "fix" the
  red by bumping `src` `<Version>` to match the pin — inverting the dependency and cutting a release
  nobody asked for. The remedy is the reverse: revert the pin. The pin moves only with the `release:`
  commit that moves `<Version>`.
- **It fails *before* restoring.** `source-proof.md` records `Build log: not-run`. Restoring against a
  pin already known to be wrong would replace a precise diagnosis with a confusing `NU1102`, and a
  proof that never compiled the consumer must never render identically to one that compiled it clean.

The same 57 violations red the **required** `Deterministic gate` step ("Sample pins mirror src
`<Version>`"), which is offline — it compares two sets of files as text, packs nothing, and reads no
network — so #233 would have been blocked pre-merge rather than reported after the fact.

`.github/renovate.json` now disables `FS.GG.UI.*` under `samples/**`, so the bot stops proposing,
every cycle, a bump the gate would now reject.

## 3. The red path — a consumer that no longer compiles

The reason `--mode proof` builds rather than stopping at restore. Introducing a type error into
`samples/AntShowcase/AntShowcase.Core/AntTheme.fs` (a package *resolves* fine; it no longer
*composes*):

```
package-feed status: failed
build-failed: `dotnet build .../AntShowcase.Core/AntShowcase.Core.fsproj -c Release --no-restore` exit 1
build-failed: `dotnet build .../AntShowcase.App/AntShowcase.App.fsproj  -c Release --no-restore` exit 1
build-failed: `dotnet build .../AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --no-restore` exit 1
package-feed: build output: <out>/build.log
```

Exit `1`. Three things to note:

- A **restore-only** proof reports `passed` here. The packages resolve; the program does not compile.
  `ApiCompat` also reports nothing, because it compares public surfaces pairwise and never observes
  `AntShowcase.Core` opening eight packages at once.
- The failure is **diagnosed in the CI log**, not only in an artifact. Violations are printed to
  stderr and point at `build.log`, which carries the `FS####` compiler error.
- The **mirror rule is not printed**. It explains a stale pin and explains nothing about a compile
  error, so it is not offered as a diagnosis for one.

## Environment note

The local capture packed with `-m:1`. Parallel `dotnet pack` across concurrently live git worktrees
trips MSB6006 / exit 134 (F# compiler-server contention), which is an artifact of the multi-worktree
dev setup, not of the tree under test. CI runs one worktree per job and needs no such flag.
