module RestoreLockTests

open System
open System.IO
open System.Text.RegularExpressions
open Expecto

// Feature 211 — deterministic policy/coverage assertion for the locked-restore mechanism (research R6,
// Principle V). These are straight filesystem assertions over real committed artifacts (no mocks): the
// committed lockfiles, the slnx membership, and the root Directory.Build.props. They backstop the live
// restore proof (readiness/restore-proof.md) so the policy cannot silently regress.
//
// VR-1: every FS.GG.Rendering.slnx member has a committed packages.lock.json.
// VR-2: the excluded lanes (Package.Tests + the 4 shadowing samples) do NOT.
// The props assertion also backstops US2 (NU1603-as-error contract).

// Walk up from the test's base directory until the repo root (the dir holding FS.GG.Rendering.slnx).
let private repoRoot =
    let rec up (dir: DirectoryInfo | null) =
        match dir with
        | null -> failwith "could not locate repo root (FS.GG.Rendering.slnx) walking up from test base dir"
        | d ->
            if File.Exists(Path.Combine(d.FullName, "FS.GG.Rendering.slnx")) then d.FullName
            else up d.Parent
    up (DirectoryInfo(AppContext.BaseDirectory))

let private repoPath (rel: string) = Path.Combine(repoRoot, rel.Replace('/', Path.DirectorySeparatorChar))

// The projects listed in the gate solution — exactly the LOCKED set (each must own a lockfile).
let private slnxProjectDirs =
    let slnx = File.ReadAllText(repoPath "FS.GG.Rendering.slnx")
    Regex.Matches(slnx, "Path=\"([^\"]+\\.fsproj)\"")
    |> Seq.map (fun m -> m.Groups.[1].Value)
    |> Seq.map (fun p ->
        match Path.GetDirectoryName(p) with
        | null -> ""
        | d -> d.Replace('\\', '/'))
    |> Seq.distinct
    |> Seq.sort
    |> Seq.toList

// EXCLUDED lanes — never locked (data-model.md / contracts/restore-policy.md G5/G6).
let private excludedProjectDirs =
    [ "tests/Package.Tests"
      "samples/AntShowcase"
      "samples/SampleApps"
      "samples/SecondAntShowcase"
      "samples/ControlsGallery" ]

let private hasLockfile (projDir: string) =
    File.Exists(Path.Combine(repoPath projDir, "packages.lock.json"))

[<Tests>]
let restoreLockTests =
    testList "Feature 211 — locked-restore policy" [

        test "the gate solution membership is the expected 38-project LOCKED set" {
            // Guards against the slnx silently gaining/losing a project without the lockfile coverage
            // assertion below being updated; 18 src + 17 tests + 2 samples + 1 tools = 38.
            Expect.equal slnxProjectDirs.Length 38
                (sprintf "expected 38 slnx projects, found %d: %A" slnxProjectDirs.Length slnxProjectDirs)
        }

        test "VR-1: every FS.GG.Rendering.slnx member has a committed packages.lock.json" {
            let missing = slnxProjectDirs |> List.filter (hasLockfile >> not)
            Expect.isEmpty missing
                (sprintf "these slnx members are missing a committed packages.lock.json: %A" missing)
        }

        test "VR-2: the excluded lanes (Package.Tests + the 4 shadowing samples) do NOT have a lockfile" {
            let leaked = excludedProjectDirs |> List.filter hasLockfile
            Expect.isEmpty leaked
                (sprintf "these EXCLUDED lanes must never be locked but have a packages.lock.json: %A" leaked)
        }

        test "root Directory.Build.props carries the restore policy (RestorePackagesWithLockFile + gated RestoreLockedMode + NU1603 as-error)" {
            let props = File.ReadAllText(repoPath "Directory.Build.props")
            Expect.stringContains props "<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>"
                "root props must enable lockfile restore (FR-001)"
            Expect.stringContains props "<RestoreLockedMode"
                "root props must declare the gated RestoreLockedMode (FR-002)"
            Expect.stringContains props "ContinuousIntegrationBuild"
                "RestoreLockedMode must be gated to CI so a fresh local clone is never blocked (FR-003)"
            // NU1603 (silent substitution) promoted to error backs US2's enforcement contract (FR-004).
            let warnAsErrorsHasNu1603 =
                Regex.IsMatch(props, "<WarningsAsErrors>[^<]*NU1603")
            Expect.isTrue warnAsErrorsHasNu1603
                "NU1603 must appear in WarningsAsErrors so silent version substitution fails the build (FR-004)"
        }
    ]
