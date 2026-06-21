namespace FS.GG.TestSupport

open System
open System.IO

/// Feature 178 (US1): the single shared repository-root finder for every test/harness project.
/// Consolidates the two pre-refactor families (the named `findRepositoryRoot` and the inline
/// `FS.GG.Rendering.slnx` walks) onto one canonical marker set. Lives in the non-packed
/// `tests/TestSupport` assembly — no public package surface.
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
