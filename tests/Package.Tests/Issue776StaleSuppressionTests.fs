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

open System
open System.Diagnostics
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

[<Tests>]
let staleSuppressionTests =
    testList
        "Stale ApiCompat suppression (#776)"
        [
          // THE GATE. Every signature the classifier keys on, held against SDK output captured from a real
          // failing pack — including the one that must NOT collapse: a genuine API break alongside a dead
          // suppression is still a BREAK. Classify that as `stale` and a SemVer-major break gets reported as
          // a tidy-up chore and merged.
          test "the pack-log classifier still recognises every state it must" {
              let ec, out = runSelfTest ()

              Expect.equal
                  ec
                  0
                  $"`scripts/apicompat-check.sh --self-test` failed — the classifier no longer recognises the SDK output it keys on. A dead suppression will go back to being reported as `Indeterminate (pack/tool failure — NOT compared)`, which is what sent #443 hunting a build failure that did not exist. Re-capture the real messages from a failing pack and fix the patterns; do NOT delete the fixtures.\n\n{out}"

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
                  5
                  $"--self-test asserted only {oks} signature(s). It is the sole check on the classifier, so a shrinking fixture set is the gate going quietly blind.\n\n{out}"

              Expect.stringContains
                  out
                  "a break alongside a dead suppression is still a BREAK"
                  $"the co-occurrence fixture is gone. It is the one that stops `is_stale_suppression` being tested BEFORE `is_break` — an ordering under which a genuine, unsuppressed API break is reported as a stale-suppression chore and merged.\n\n{out}"
          } ]
