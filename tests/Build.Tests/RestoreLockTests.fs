module RestoreLockTests

open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

// Feature 211 — deterministic policy/coverage assertion for the locked-restore mechanism (research R6,
// Principle V). These are straight filesystem assertions over real committed artifacts (no mocks): the
// committed lockfiles, the slnx membership, and the root Directory.Build.props. They backstop the live
// restore proof (readiness/restore-proof.md) so the policy cannot silently regress.
//
// VR-1: every FS.GG.Rendering.slnx member has a committed packages.lock.json.
// VR-2: the excluded lanes (the 4 shadowing samples) do NOT. (Package.Tests left this set in #540.)
// The props assertion also backstops US2 (NU1603-as-error contract).

let private repoRoot = RepositoryRoot.value

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
//
// #540 removed `tests/Package.Tests` from this list. It is now a slnx member, so VR-1 above REQUIRES the
// lockfile that VR-2 below used to forbid — the two rules would contradict each other if it stayed here.
//
// It was excluded because it was a release-only lane whose FS.GG.UI.* preview pins "churn every merge".
// Neither half survived: #453 rebound those packages as ProjectReferences (so there are no such pins left
// to churn — its only PackageReferences are Expecto, FS.GG.Contracts and the test SDKs), and #540 put the
// project on the gate. The four `samples/*` shadowing lanes are genuinely still excluded and stay.
let private excludedProjectDirs =
    [ "samples/AntShowcase"
      "samples/SampleApps"
      "samples/SecondAntShowcase"
      "samples/ControlsGallery" ]

let private hasLockfile (projDir: string) =
    File.Exists(Path.Combine(repoPath projDir, "packages.lock.json"))

[<Tests>]
let restoreLockTests =
    testList "Feature 211 — locked-restore policy" [

        test "the gate solution membership is the expected 40-project LOCKED set" {
            // Guards against the slnx silently gaining/losing a project without the lockfile coverage
            // assertion below being updated; 18 src + 19 tests + 2 samples + 1 tools = 40.
            //
            // 38 -> 39 in #540, which added tests/Package.Tests. It arrived with NO packages.lock.json and
            // an explicit <RestorePackagesWithLockFile>false</RestorePackagesWithLockFile>, and VR-1 below
            // is what said so — the moment the project entered the slnx, the policy it had been exempt from
            // for two features applied to it and it was one line short. Which is the point of #540: this
            // guard is scoped to slnx MEMBERS, so a project outside the solution was invisible to it.
            //
            // 39 -> 40 in #695 (spec 255), which added tests/DocFences.Tests — the doc-fence-compile harness.
            Expect.equal slnxProjectDirs.Length 40
                (sprintf "expected 40 slnx projects, found %d: %A" slnxProjectDirs.Length slnxProjectDirs)
        }

        test "VR-1: every FS.GG.Rendering.slnx member has a committed packages.lock.json" {
            let missing = slnxProjectDirs |> List.filter (hasLockfile >> not)
            Expect.isEmpty missing
                (sprintf "these slnx members are missing a committed packages.lock.json: %A" missing)
        }

        test "VR-2: the excluded lanes (the 4 shadowing samples) do NOT have a lockfile" {
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
            // Feature 213 (H3 / ADR-0006): the unified gate is spelled GITHUB_ACTIONS, not
            // ContinuousIntegrationBuild. Assert the actual gate CONDITION fragment (not a bare
            // substring — the canonical file mentions ContinuousIntegrationBuild only in a comment
            // explaining the migration; the Feature 175 lesson is that a substring can pass while the
            // effective gate differs).
            Expect.stringContains props "'$(GITHUB_ACTIONS)' == 'true'"
                "RestoreLockedMode must be gated on the GITHUB_ACTIONS CI signal so a fresh local clone is never blocked (FR-003)"
            // NU1603 (silent substitution) promoted to error backs US2's enforcement contract (FR-004).
            let warnAsErrorsHasNu1603 =
                Regex.IsMatch(props, "<WarningsAsErrors>[^<]*NU1603")
            Expect.isTrue warnAsErrorsHasNu1603
                "NU1603 must appear in WarningsAsErrors so silent version substitution fails the build (FR-004)"
        }

        // 186 — a lockfile pins a contentHash, so it only means something if every machine resolves
        // each package from the same source. Two things make that true, and both are asserted here
        // because either one silently going missing restores the environment-dependent restore.
        test "the repo pins a root nuget.config that clears inherited sources" {
            let path = repoPath "nuget.config"
            Expect.isTrue (File.Exists path)
                "the repo must commit a root nuget.config, or restore inherits whatever sources the machine has"
            let cfg = File.ReadAllText path
            Expect.stringContains cfg "<clear />"
                "nuget.config must <clear /> inherited packageSources, or a user-level or corporate feed still contributes"
            Expect.stringContains cfg "https://api.nuget.org/v3/index.json"
                "nuget.config must declare nuget.org explicitly"
            // The local pack-as-you-go feed is deliberately NOT a source for this repo's restore:
            // every slnx member takes its FS.GG.* packages from nuget.org.
            Expect.isFalse (cfg.Contains "nuget-local" || cfg.Contains "local-feed")
                "the local pack-as-you-go feed must not be a source for this repo's restore"
        }

        test "the SDK's implicit FSharp.Core library-packs source is disabled" {
            // Microsoft.FSharp.NetSdk.targets appends `FSharp/library-packs` to
            // RestoreAdditionalProjectSources unless this property is set. That folder ships an
            // FSharp.Core nupkg with nuget.org's VERSION but different BYTES, so leaving it live
            // makes the recorded contentHash depend on the installed SDK patch.
            let props = File.ReadAllText(repoPath "Directory.Build.local.props")
            Expect.stringContains props "<DisableImplicitLibraryPacksFolder>true</DisableImplicitLibraryPacksFolder>"
                "Directory.Build.local.props must disable the SDK's implicit library-packs restore source"
        }

        // 482 — the locked restore has ONE definition now, the .github/actions/locked-restore
        // composite action, so gate.yml `uses:` it instead of spelling the flags inline. The 186
        // invariant is unchanged; only its home moved, which is why this reads the action. The gate
        // is still asserted to route through it: a definition nobody invokes would satisfy the flag
        // check below while the gate quietly restored warm.
        test "the gate restores against the committed nuget.config" {
            let gate = File.ReadAllText(repoPath ".github/workflows/gate.yml")
            Expect.stringContains gate "uses: ./.github/actions/locked-restore"
                "gate.yml must run its restore through the locked-restore action, or the cold locked restore is defined but never reached"
            let action = File.ReadAllText(repoPath ".github/actions/locked-restore/action.yml")
            Expect.stringContains action "--locked-mode --configfile nuget.config"
                "the locked restore must name the committed config, so the pinned sources are intentional rather than inherited"
        }

        // 482 — COLDNESS is the invariant this action exists to create, and the #186 check above
        // cannot see it: strip the fresh package folder and the http-cache clear and --locked-mode
        // goes straight back to validating the committed contentHash against whatever copy is
        // already on the runner, with every other test in this suite still green. Both lanes are
        // asserted because a second inline restore is exactly how the warm one survived last time.
        test "the locked restore is cold, and both gate lanes route through it" {
            let action = File.ReadAllText(repoPath ".github/actions/locked-restore/action.yml")
            Expect.stringContains action "NUGET_PACKAGES=\"$(mktemp -d)\""
                "the locked restore must resolve into a FRESH package folder, or --locked-mode compares the committed contentHash against a record of reality rather than against the feed"
            Expect.stringContains action "dotnet nuget locals http-cache --clear"
                "the locked restore must clear the HTTP cache — NUGET_PACKAGES relocates the global-packages folder but NOT the http cache, which will replay stale .nupkg bytes into the fresh folder"

            let consumer = File.ReadAllText(repoPath ".github/workflows/packaged-consumer.yml")
            Expect.stringContains consumer "uses: ./.github/actions/locked-restore"
                "packaged-consumer.yml must restore through the same action, or it reintroduces the second, warm, inline locked restore that 482 removed"
        }

        // 482 — the action exports NUGET_PACKAGES to $GITHUB_ENV, which is JOB-WIDE, and the
        // NUGET_PACKAGES *env var* OVERRIDES a `globalPackagesFolder` set in a nuget.config. The
        // version-coherence smoke isolates its clean consumer with exactly that config key, so
        // inheriting the variable would redirect its "clean restore" into the shared folder, where a
        // cached FS.GG.UI@V could satisfy a member the freshly-packed feed never produced — the
        // restore-partial proof would then report green on a partial graph. Cheap line, silent
        // fail-open if it is ever dropped.
        test "the version-coherence guard does not inherit the locked restore's package folder" {
            let gate = File.ReadAllText(repoPath ".github/workflows/gate.yml")
            Expect.stringContains gate "unset NUGET_PACKAGES"
                "gate.yml's version-coherence step must unset NUGET_PACKAGES, or the env var overrides the globalPackagesFolder the smoke uses to isolate its clean consumer, and restore-partial can pass on a partial graph"
        }

        // 1089 — THE PUBLISHED TEMPLATE PACKAGE IS LOCKED TOO, AND NOTHING ABOVE SAYS SO.
        //
        // VR-1 is scoped to slnx MEMBERS. `.template.package/FS.GG.UI.Template.fsproj` is deliberately
        // NOT a member (release.yml: "the two packs never overlap"), so it was invisible to every rule
        // in this file — which is the #540 lesson repeating one project later. The consequence was not
        // theoretical: the 0.24.0 release lane printed
        //
        //   [lockfile-restore-enforcement] packages.lock.json is missing in CI (GITHUB_ACTIONS=true)
        //   — locked restore is NOT enforced for FS.GG.UI.Template; restore is running UNLOCKED
        //
        // i.e. the ONE project whose output is the published template package was the one restoring
        // unlocked, on the publish path. `RestoreLockedMode` in the root props is conditioned on
        // `Exists(packages.lock.json)` so a new project can bootstrap; the cost of that fail-open is
        // that DELETING the lock re-disarms the gate silently and every test above stays green.
        // This is the rule that reds instead.
        //
        // Both halves are asserted because either one alone fails open: the lock file must be there,
        // AND the project must not opt out of lockfile restore the way tests/Package.Tests once did
        // (`<RestorePackagesWithLockFile>false</RestorePackagesWithLockFile>`, removed in #540) —
        // an opt-out would leave a committed lock sitting next to a restore that ignores it.
        test "1089: the published template package project is locked, though it is not an slnx member" {
            let templateDir = ".template.package"
            let fsproj = repoPath (templateDir + "/FS.GG.UI.Template.fsproj")
            Expect.isTrue (File.Exists fsproj)
                "the template package project must exist — it is what release.yml packs and publishes as FS.GG.UI.Template"

            Expect.isFalse (List.contains templateDir slnxProjectDirs)
                "if .template.package ever becomes an slnx member, VR-1 covers it and this rule should be retired rather than duplicated"

            Expect.isTrue (hasLockfile templateDir)
                "the template package project must commit a packages.lock.json, or release.yml's `dotnet pack .template.package/...` restores UNLOCKED in CI (bootstrap fail-open) on the publish path itself (#1089)"

            let proj = File.ReadAllText fsproj
            Expect.isFalse (Regex.IsMatch(proj, @"<RestorePackagesWithLockFile>\s*false\s*</RestorePackagesWithLockFile>"))
                "the template package project must not opt out of lockfile restore, or the committed lock is inert"
        }

        // 1089 — AND PRESENCE IS NOT ENOUGH. THIS IS THE ONLY PR-TIME SIGNAL THIS LOCK HAS.
        //
        // The rule above checks the lock EXISTS, which is all VR-1 checks — and for slnx members that
        // is fine, because the gate's cold locked restore then validates the contents on every PR. This
        // project has no such backstop: `.template.package` is restored/packed in exactly ONE place in
        // the repo, `release.yml`'s publish-packages job. No gate.yml job touches it. So an existence
        // check is the whole of the PR-time signal, and an existence check passes on a lock that is
        // empty, truncated, or simply STALE.
        //
        // Staleness is the live case, not a hypothetical. FSharp.Core's version is pinned in the
        // ORG-SHARED Directory.Packages.props, synced in from FS-GG/.github. When it moves,
        // `lockfile-sync.yml` regenerates the 40 slnx locks and NOT this one — its restore-target is
        // FS.GG.Rendering.slnx, and the shared reusable workflow takes a single target string. Without
        // the assertions below, that bump merges green and the staleness surfaces as an NU1004 in the
        // RELEASE lane (fail-closed, before either `dotnet nuget push`, so nothing partially publishes —
        // but the release is blocked with no earlier warning).
        //
        // Comparing the lock against the central pin moves that discovery back onto the PR that causes
        // it: bump the pin without regenerating this lock and THIS test reds, naming the command.
        test "1089: the template package's lock is not merely present — it matches the central FSharp.Core pin" {
            let lockPath = repoPath ".template.package/packages.lock.json"
            let lockText = File.ReadAllText lockPath

            // The central pin is the authority; the lock must agree with it.
            let centralPin =
                let props = File.ReadAllText(repoPath "Directory.Packages.props")
                let m = Regex.Match(props, @"<PackageVersion\s+Include=""FSharp\.Core""\s+Version=""([^""]+)""")
                Expect.isTrue m.Success
                    "Directory.Packages.props must centrally pin FSharp.Core — it is the version this lock is compared against"
                m.Groups.[1].Value

            let entry =
                Regex.Match(
                    lockText,
                    @"""FSharp\.Core""\s*:\s*\{[^}]*?""resolved""\s*:\s*""(?<resolved>[^""]+)""[^}]*?""contentHash""\s*:\s*""(?<hash>[^""]+)""",
                    RegexOptions.Singleline)

            Expect.isTrue entry.Success
                "the template package's packages.lock.json must actually RECORD FSharp.Core with a resolved version and a contentHash — an empty or truncated lock file satisfies Exists(), so RestoreLockedMode arms on it while this project has no gate-lane restore to catch the contents (#1089)"

            Expect.equal (entry.Groups.["resolved"].Value) centralPin
                (sprintf "the template package's lock resolves FSharp.Core %s but Directory.Packages.props pins %s. lockfile-sync.yml does NOT regenerate this lock (its restore-target is the slnx, and .template.package is not a member), so regenerate it in THIS PR: dotnet restore .template.package/FS.GG.UI.Template.fsproj --use-lock-file --force-evaluate --configfile nuget.config"
                    (entry.Groups.["resolved"].Value) centralPin)

            Expect.isGreaterThan (entry.Groups.["hash"].Value.Length) 0
                "the recorded contentHash must be non-empty, or locked restore has nothing to validate against the feed"
        }
    ]
