# Contributing to FS.GG.Rendering

## Updating dependency lockfiles

This repo restores its dependencies in **locked mode** in CI (Feature 211). Every project in the
gate solution (`FS.GG.Rendering.slnx`) has a committed `packages.lock.json`; the gate restores against
those lockfiles and **fails** if the resolved graph differs from them, so two CI runs always resolve
the same package versions.

Locked mode is gated to CI (`ContinuousIntegrationBuild=true`) **and** the presence of a lockfile, so a
fresh clone and ordinary local `dotnet build` / `dotnet restore` are **never** blocked — you do not
have to think about lockfiles for day-to-day work.

When you intentionally change a dependency version (in `Directory.Packages.props`), re-lock with the
**single regenerate command**:

```bash
dotnet restore FS.GG.Rendering.slnx --force-evaluate
```

This re-resolves from `Directory.Packages.props`, rewrites every lockfile, and produces a reviewable
diff. **Commit `Directory.Packages.props` and the changed `packages.lock.json` files together** — an
un-updated lockfile is meant to fail CI. See
[`specs/211-lockfile-locked-restore/quickstart.md`](specs/211-lockfile-locked-restore/quickstart.md)
Scenario E.

### Catching lockfile drift before you push

To reproduce the gate's locked-mode failure locally (the stale-lockfile edge case) — for example after
bumping a version but forgetting to regenerate — run the gate's exact restore with CI mode forced on:

```bash
ContinuousIntegrationBuild=true dotnet restore FS.GG.Rendering.slnx --locked-mode
```

If this fails locally, it will fail the gate; run the regenerate command above and commit the result.
See quickstart Scenarios A (locked restore succeeds) and B (drift fails closed).
