module Issue776StaleSuppressionTests

// #776 — the transient ApiCompat suppression, and the gate that finally collects it.
//
// THE LOOP. A release that removes public API needs a transient `CompatibilitySuppressions.xml`:
// `scripts/apicompat-check.sh` baselines off the PUBLISHED feed, so on the release commit the removed
// member still exists in the baseline, ApiCompat reports a real CP0002, and the merge cannot happen
// without the suppression. The moment that release publishes, the baseline moves to the version that just
// shipped — which does not have the member either — so the entry now suppresses NOTHING and .NET fails the
// pack with `error : Unnecessary suppressions found.`
//
// `API compatibility gate` is REQUIRED on `main` with `enforce_admins`. The transition happens ON THE FEED,
// not in a commit, so the first PR after a publish reds with no diff having caused it, and every PR in the
// repo is unmergeable until somebody deletes the file. This repo has paid that three times — `1159d906`,
// `67d39e68`, `855e75f2` — each time because the only thing that said "delete me after the release" was a
// COMMENT INSIDE THE FILE. #441 named the class and asked for a sweep; nobody built it, and it predicted
// its own recurrence: *"this will red `main` again"*. It did.
//
// AND WHEN IT FIRED, IT LIED. `apicompat-check.sh` grepped for `error CP[0-9]`. The line .NET actually
// emits for a dead suppression is `error : [Baseline] CP0002 (Target: '...')` — which that pattern cannot
// match — so the package fell through to `Indeterminate`, announcing *"pack failed, so this package was
// never compared"*. The tool had run fine. #443's author went looking for a build failure.
//
// WHY THIS TEST EXISTS RATHER THAN A LIVE PACK. The real check needs the feed, and a required-tier test may
// not (ADR-0105: a feed dependency hands the merge button to someone else's uptime). So the classifier is a
// pure function of the pack log, `--self-test` drives it over SDK output captured verbatim, and this test
// is the thing that runs it. No network, no pack, no token.
//
// A classifier nobody exercises is a classifier that rots silently: the SDK rewords its message, the stale
// branch stops matching, and a dead suppression goes quietly back to being "pack failed, never compared" —
// the exact defect, restored, under a green gate.
//
// THAT ARGUMENT HAD A HOLE IN IT FOR AS LONG AS IT EXISTED, AND #871 IS THE HOLE. It was made for the two
// classifier predicates and applied to only those two, while the `grep` that turns a stale log into the
// LIST OF ENTRIES TO DELETE sat twelve lines below them, written against one remembered message shape and
// exercised by nothing. It was wrong: a removed TYPE carries `Left:`/`Right:` after the target, so the
// pattern matched a removed MEMBER and missed a type — and a type is what a major removes. The gate did its
// loud, correct, fail-closed thing and then printed `DELETE the entries above` above an empty list (#869).
// So `--self-test` now grades the extractor too, on the SDK's real messages for both shapes.

open System
open System.Diagnostics
open System.IO
open Expecto
open FS.GG.TestSupport

let private root = RepositoryRoot.value

let private runSelfTest () =
    let psi = ProcessStartInfo("bash")
    psi.WorkingDirectory <- root
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    [ "scripts/apicompat-check.sh"; "--self-test" ] |> List.iter psi.ArgumentList.Add

    // Scrubbed, so the child cannot reach a feed even by accident: `--self-test` must be provably offline,
    // or it is not fit for the tier that runs it.
    for v in [ "NUGET_FEED_TOKEN"; "GH_TOKEN"; "GITHUB_TOKEN" ] do
        psi.Environment.Remove v |> ignore

    match Process.Start psi with
    | null -> failwith "could not start bash"
    | p ->
        use p = p
        let out = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd()
        p.WaitForExit()
        p.ExitCode, out

let private runProcess workingDirectory environment command arguments =
    let psi = ProcessStartInfo(command)
    psi.WorkingDirectory <- workingDirectory
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true
    arguments |> List.iter psi.ArgumentList.Add
    environment
    |> List.iter (fun (name, value) -> psi.Environment[name] <- value)

    match Process.Start psi with
    | null -> failwith $"could not start {command}"
    | child ->
        use child = child
        let stdout = child.StandardOutput.ReadToEndAsync()
        let stderr = child.StandardError.ReadToEndAsync()
        child.WaitForExit()
        child.ExitCode, stdout.Result + stderr.Result

[<Tests>]
let staleSuppressionTests =
    testList
        "Stale ApiCompat suppression (#776)"
        [
          // THE GATE. Every signature the classifier keys on, held against SDK output captured from a real
          // failing pack — including the one that must NOT collapse: a genuine API break alongside a dead
          // suppression is still a BREAK. Classify that as `stale` and a SemVer-major break gets reported as
          // a tidy-up chore and merged.
          //
          // AND the EXTRACTOR (#871), which used to be graded by nothing. Classifying the log `stale` is
          // only half the job: the gate must also NAME the entries to delete, and `stale_entries` had been
          // written against the one captured shape where `Target` is the message's last field. A removed
          // TYPE is not that shape, so the gate printed "DELETE the entries above" over an empty list.
          test "the pack-log classifier and extractor still recognise every SDK shape they must" {
              let ec, out = runSelfTest ()

              Expect.equal
                  ec
                  0
                  $"`scripts/apicompat-check.sh --self-test` failed — the classifier or the extractor no longer recognises the SDK output it keys on. A dead suppression will go back to being reported as `Indeterminate (pack/tool failure — NOT compared)`, which is what sent #443 hunting a build failure that did not exist, or to being reported with none of its entries named (#871). Re-capture the real messages from a failing pack and fix the patterns; do NOT delete the fixtures.\n\n{out}"

              Expect.stringContains
                  out
                  "all classifier signatures hold"
                  $"--self-test exited 0 without reporting that it checked anything. 'nothing to check' and 'checked, and it is fine' must not share an exit code (FS-GG/.github#266).\n\n{out}"
          }

          // THE TEETH. The test above is worthless the moment `--self-test` stops actually asserting — an
          // empty fixture list exits 0 forever. So this pins the fixtures themselves: each `ok <name>` line
          // is one signature, and the co-occurrence case is named explicitly because it is the one whose
          // removal would be invisible AND unsafe.
          test "--self-test is not vacuous — it exercises every branch, break-over-stale included" {
              let _, out = runSelfTest ()
              let oks = out.Split '\n' |> Array.filter (fun l -> l.TrimStart().StartsWith "ok ") |> Array.length

              Expect.isGreaterThanOrEqual
                  oks
                  8
                  $"--self-test asserted only {oks} signature(s). It is the sole check on the classifier AND on the extractor, so a shrinking fixture set is the gate going quietly blind.\n\n{out}"

              Expect.stringContains
                  out
                  "a break alongside a dead suppression is still a BREAK"
                  $"the co-occurrence fixture is gone. It is the one that stops `is_stale_suppression` being tested BEFORE `is_break` — an ordering under which a genuine, unsuppressed API break is reported as a stale-suppression chore and merged.\n\n{out}"

              // #871's fixture, pinned by name for the same reason the co-occurrence one is: its absence
              // is invisible. The extractor was written against the CP0002 shape, where `Target` is the
              // message's last field, and a removed TYPE puts `Left:`/`Right:` after it — so the gate
              // reported a stale suppression and named NONE of its entries, printing "DELETE the entries
              // above" over an empty list. That is what #869 met on the live 0.12.0 transition.
              Expect.stringContains
                  out
                  "a dead TYPE suppression is named (Target is NOT the last field)"
                  $"the #871 fixture is gone. It is the one that stops `stale_entries` regressing to a pattern that only matches a removed MEMBER — under which a removed TYPE (what a SemVer major actually does) reds the required gate and tells the next worker to delete entries it has not named.\n\n{out}"
          }

          test "a poisoned ambient same-version package cannot shadow the published baseline (#1033)" {
              let fixtureRoot =
                  Path.Combine(Path.GetTempPath(), $"fsgg-apicompat-cache-{Guid.NewGuid():N}")
              Directory.CreateDirectory fixtureRoot |> ignore

              try
                  let packageId = "FS.GG.ApiCompat.CacheProbe"
                  let baselineVersion = "1.0.0"
                  let directory name =
                      let path = Path.Combine(fixtureRoot, name)
                      Directory.CreateDirectory path |> ignore
                      path
                  let authoritative = directory "authoritative"
                  let poison = directory "poison"
                  let candidate = directory "candidate"
                  let consumer = directory "consumer"
                  let authoritativeFeed = directory "authoritative-feed"
                  let poisonFeed = directory "poison-feed"
                  let ambientPackages = directory "ambient-packages"
                  let ambientHttpCache = directory "ambient-http-cache"
                  let buildPackages = directory "build-packages"
                  let buildHttpCache = directory "build-http-cache"
                  let gateProbe = Path.Combine(fixtureRoot, "gate-env.txt")

                  let project =
                      $"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <PackageId>{packageId}</PackageId>
    <AssemblyName>{packageId}</AssemblyName>
    <Version>{baselineVersion}</Version>
  </PropertyGroup>
  <Target Name="CaptureApiCompatCacheEnvironment"
          BeforeTargets="Pack"
          Condition="'$(APICOMPAT_ENV_PROBE)' != ''">
    <WriteLinesToFile File="$(APICOMPAT_ENV_PROBE)"
                      Lines="$(NUGET_PACKAGES)|$(NUGET_HTTP_CACHE_PATH)"
                      Overwrite="true" />
  </Target>
</Project>
"""
                  for path in [ authoritative; poison; candidate ] do
                      File.WriteAllText(Path.Combine(path, "Probe.csproj"), project)

                  let compatibleContract =
                      """namespace FS.GG.ApiCompat.CacheProbe;
public sealed class Contract
{
    public string Value() => "authoritative";
}
"""
                  File.WriteAllText(Path.Combine(authoritative, "Contract.cs"), compatibleContract)
                  File.WriteAllText(Path.Combine(candidate, "Contract.cs"), compatibleContract)
                  File.WriteAllText(
                      Path.Combine(poison, "Contract.cs"),
                      """namespace FS.GG.ApiCompat.CacheProbe;
public sealed class Contract
{
    public string Value() => "poison";
    public string PoisonOnly() => "ambient";
}
"""
                  )

                  let buildEnvironment =
                      [ "NUGET_PACKAGES", buildPackages
                        "NUGET_HTTP_CACHE_PATH", buildHttpCache ]
                  for source, feed in [ authoritative, authoritativeFeed; poison, poisonFeed ] do
                      let exitCode, output =
                          runProcess
                              fixtureRoot
                              buildEnvironment
                              "dotnet"
                              [ "pack"; Path.Combine(source, "Probe.csproj")
                                "-c"; "Release"; "-o"; feed; "--nologo"; "--verbosity"; "quiet" ]
                      Expect.equal exitCode 0 $"fixture package failed to pack:\n{output}"

                  File.WriteAllText(
                      Path.Combine(consumer, "Consumer.csproj"),
                      $"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
  <ItemGroup>
    <PackageReference Include="{packageId}" Version="[{baselineVersion}]" />
  </ItemGroup>
</Project>
"""
                  )
                  let poisonConfig = Path.Combine(consumer, "nuget.config")
                  File.WriteAllText(
                      poisonConfig,
                      $"""<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="poison" value="{poisonFeed}" />
  </packageSources>
</configuration>
"""
                  )
                  let poisonEnvironment =
                      [ "NUGET_PACKAGES", ambientPackages
                        "NUGET_HTTP_CACHE_PATH", ambientHttpCache ]
                  let restoreExit, restoreOutput =
                      runProcess
                          fixtureRoot
                          poisonEnvironment
                          "dotnet"
                          [ "restore"; Path.Combine(consumer, "Consumer.csproj")
                            "--configfile"; poisonConfig; "--nologo"; "--verbosity"; "quiet" ]
                  Expect.equal restoreExit 0 $"could not seed the ambient poison cache:\n{restoreOutput}"
                  Expect.isTrue
                      (File.Exists(
                          Path.Combine(
                              ambientPackages,
                              "fs.gg.apicompat.cacheprobe",
                              baselineVersion,
                              "lib",
                              "net10.0",
                              packageId + ".dll"
                          )
                      ))
                      "the same-ID/version poison package must actually be present in the ambient cache"

                  let gateEnvironment =
                      [ "NUGET_PACKAGES", ambientPackages
                        "NUGET_HTTP_CACHE_PATH", ambientHttpCache
                        "NUGET_FEED_TOKEN", "functional-test-token"
                        "APICOMPAT_TEST_PROJECT", Path.Combine(candidate, "Probe.csproj")
                        "APICOMPAT_TEST_FEED_URL", authoritativeFeed
                        "APICOMPAT_ENV_PROBE", gateProbe ]
                  let gateExit, gateOutput =
                      runProcess
                          root
                          gateEnvironment
                          "bash"
                          [ "scripts/apicompat-check.sh"; "--baseline"; baselineVersion ]
                  Expect.equal
                      gateExit
                      0
                      $"the production gate consumed the poisoned ambient package instead of the configured-feed baseline:\n{gateOutput}"
                  Expect.stringContains gateOutput packageId "the candidate package was inspected"
                  Expect.stringContains gateOutput "OK            (compatible with 1.0.0)" "the authoritative compatible baseline was compared"

                  let gateCaches = File.ReadAllText(gateProbe).Trim().Split('|')
                  Expect.equal gateCaches.Length 2 "the pack probe records both gate-owned cache paths"
                  let gatePackages, gateHttpCache = gateCaches[0], gateCaches[1]
                  Expect.notEqual gatePackages ambientPackages "the gate must replace ambient NUGET_PACKAGES"
                  Expect.notEqual gateHttpCache ambientHttpCache "the gate must replace the ambient HTTP cache"
                  Expect.equal (Path.GetFileName gatePackages) "packages" "global packages live under the gate workdir"
                  Expect.equal (Path.GetFileName gateHttpCache) "http-cache" "HTTP cache lives under the gate workdir"
                  Expect.equal
                      (Path.GetDirectoryName gatePackages)
                      (Path.GetDirectoryName gateHttpCache)
                      "both caches share the gate-owned workdir"
                  Expect.isFalse (Directory.Exists gatePackages) "the exit trap removes the gate-owned package cache"
                  Expect.isFalse (Directory.Exists gateHttpCache) "the exit trap removes the gate-owned HTTP cache"
              finally
                  if Directory.Exists fixtureRoot then
                      Directory.Delete(fixtureRoot, true)
          } ]
