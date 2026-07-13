# Package proof — as of

**This directory is a dated record of one validation run. It is not a live claim, and nothing gates
it.** Read it as "the packaged-consumer path worked, on this commit, against these versions" — not as
"the packaged-consumer path works". The live claim is the `packaged-consumer` CI lane, which re-packs
and re-proves on every push and uploads its own copy of exactly these files as the
`packaged-consumer-proof` artifact.

## What this record proves

| | |
|---|---|
| Commit | `a93786b6b5b589e12808a15e45d1940fff1ddc20` |
| Generated | `2026-07-13T14:52:43Z` |
| Status | `passed` |
| Packages | 16, all at `0.4.0-preview.1` |
| Sample pins | 57 |
| Package-consuming samples | `AntShowcase`, `ControlsGallery`, `SampleApps`, `SecondAntShowcase` (all 4 discovered) |

It proves the stronger of the two available claims: the samples not only **restore** against a feed
packed from this commit, they **build** against it (`build.log`). Restore proves the packages resolve;
only the build proves they compose.

**All four samples were restored and built** — `restore.log` and `build.log` each name all four, and
`assets/` holds all twelve `project.assets.json`. Do not read the single `Restore command:` line in
`source-proof.md` (and `restoreCommand` in `source-proof.json`) as the whole run: that field records
only the *first* sample's command, a leftover from when the lane proved exactly one sample. The logs,
not that field, are the record of what ran.

## How to regenerate it

The record is written only when you ask for it by name. `--out` no longer defaults here (#702) — a bare
`package-feed --mode proof` writes to the gitignored `artifacts/package-proof`, so merely running the
tool cannot overwrite this record.

```sh
dotnet restore FS.GG.Rendering.slnx          # --pack runs `dotnet pack --no-restore`
dotnet build tools/Rendering.Harness/Rendering.Harness.fsproj -c Debug

dotnet run --project tools/Rendering.Harness/Rendering.Harness.fsproj -c Debug --no-build -- \
  package-feed --mode proof --pack \
  --feed artifacts/package-feed \
  --isolated-cache artifacts/package-feed-cache \
  --out specs/163-package-feed-validation-lanes/readiness/package-proof
```

**Pack into a throwaway feed under the repo, as above — not into your real one.** `--feed` is where
`--pack` packs, so the default (`~/.local/share/nuget-local`) would mutate the machine-global feed that
other repos and every concurrent worker share. Keeping the feed and cache inside the repo also means
every path this evidence records lands *under the repository root*, and the #686 relativiser renders it
relative — which is why the tables below name `artifacts/package-feed/…` rather than someone's `$HOME`.

## Which files are reproducible, and which are not

**Machine-independent** — regenerating on another machine at the same commit **with the command above**
reproduces these byte-for-byte. (The command matters: point `--feed` at your own feed instead and the
`Feed package` column in `package-versions.md` goes back to naming your `$HOME`, because the relativiser
can only relativise what sits under the repository root.) They are the reviewable evidence:

- `package-versions.md` — the 16 discovered packages and their versions
- `package-pins.md` — the 57 sample pins and their stale/current status
- `source-proof.md`, `source-proof.json` — the proof result (modulo `generatedAtUtc`)

**Machine-specific, by necessity** — these are *verbatim tool output*, kept verbatim on purpose:

- `restore.log`, `build.log` — what `dotnet` actually printed, NuGet's and MSBuild's own absolute paths
  included
- `assets/*.json` — byte copies of each project's `project.assets.json`
- `source-rules.nuget.config` — the generated config fed to `dotnet restore`; its feed path must stay
  absolute for NuGet to resolve it

Rewriting these to be machine-independent would mean editing what the tools reported, which is
falsifying the evidence. They churn per machine, and that is the honest cost of recording them. It is
also why this directory is a snapshot and not a gate: you cannot diff-gate output that legitimately
differs per machine without first rewriting it into something that is no longer the output.
