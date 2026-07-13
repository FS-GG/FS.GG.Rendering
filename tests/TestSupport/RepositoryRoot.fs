namespace FS.GG.TestSupport

open System
open System.IO

/// Feature 178 (US1): the single shared repository-root finder for every project under `tests/`.
/// Consolidates the two pre-refactor families (the named `findRepositoryRoot` and the inline
/// `FS.GG.Rendering.slnx` walks) onto one canonical marker set. Lives in the non-packed
/// `tests/TestSupport` assembly — no public package surface.
///
/// `tests/` IS THE LIMIT, AND IT IS A STRUCTURAL ONE (#725). This used to claim "every test/harness
/// project", which read as a promise and was false: `samples/**/*.Tests` are test projects too, and
/// they can never consume this. They are shadowing consumers — they reference the PUBLISHED packages
/// (`<PackageReference Include="FS.GG.UI.Controls" />`), never this repo's projects, and each sample
/// tree carries its own `Directory.Build.props` that deliberately shadows the repo's. Building
/// against the packed feed is the proof the consumer path works, and it is the whole reason those
/// suites exist. So a `ProjectReference` to `tests/TestSupport` is not an oversight waiting to be
/// fixed — it would puncture exactly the isolation the samples are there to demonstrate. Do not add
/// one to "share" this finder.
///
/// The samples need no finder anyway: a sample locates its own files from its own tree, which
/// `SampleApps.Tests` now does by having MSBuild copy them next to the test binary rather than by
/// ascending to look for them. `tests/Build.Tests/RepositoryRootSingleFinderTests.fs` (#700) enforces
/// that this stays the only walk under `tests/` — and, since #734 sharpened it to detect a REPEATED
/// ascent rather than a token, over `samples/**/*.Tests` as well. So the samples are CHECKED for a
/// hand-rolled walk, not merely trusted not to have one; they just cannot be sent here to fix it.
module RepositoryRoot =

    /// Nearest ancestor of `start` containing a repository marker (`*.sln`, `*.slnx`, or
    /// `build.fsx`). Walks parents to the filesystem root and raises with an actionable message
    /// if no marker is found (fail-loud — no sentinel, no infinite loop).
    let find (start: string) : string =
        let rec walk (directory: string) =
            if
                Directory.GetFiles(directory, "*.sln").Length > 0
                || Directory.GetFiles(directory, "*.slnx").Length > 0
                || File.Exists(Path.Combine(directory, "build.fsx"))
            then
                directory
            else
                match Directory.GetParent directory |> Option.ofObj with
                | Some parent -> walk parent.FullName
                | None ->
                    failwithf
                        "Could not locate repository root: no *.sln/*.slnx/build.fsx marker at or above %s"
                        start

        walk start

    /// `find AppContext.BaseDirectory`, evaluated once.
    let value = find AppContext.BaseDirectory
