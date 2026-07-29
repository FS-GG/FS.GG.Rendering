module Feature706TemplateConditionalBalanceTests

// #706 — the guard that stops an unbalanced `//#if` nest reaching `main`.
//
// `template/base/**` varies by profile through COMMENT-WRAPPED `dotnet new` directives — `//#if` /
// `//#elif` / `//#else` / `//#endif` in F#, `<!--#if -->` / `<!--#endif -->` in XML. The wrapping is
// what lets the template still compile, and it is also why no compiler in this repo can see an
// IMBALANCE: to F#, a directive is a comment.
//
// #680 is what that costs. A stray `//#endif` in `template/base/tests/Product.Tests/GovernanceTests.fs`
// closed the outer region early, the tail of the `#else` branch escaped its guard, and the `governed`
// and `headless-scene` profiles HAD NEVER COMPILED — for weeks, on green `main`.
//
// THIS FILE IS WHERE THE GUARD BECOMES REQUIRED, and that is the whole point of the item.
//
// `scripts/validate-template-conditionals.fsx` is static — no feed, no SDK, no restore, no network — so
// unlike `generated-product-gate` (#680's lane, which restores from nuget.org and must stay NOT-REQUIRED
// because a feed outage is indistinguishable from a real break) it CAN sit in the required tier. It does,
// via this file: `Package.Tests` is a member of `FS.GG.Rendering.slnx` (#540), and the REQUIRED
// `Deterministic gate` job runs `dotnet test` over every slnx `tests/*.Tests` project that is not
// GL-bound. A red assertion here reds the required gate. No `gate.yml` step is needed for that, and none
// is added — see the PR for why the issue's suggested shape is one layer more than the enforcement needs.
//
// WHAT IS ASSERTED IS BEHAVIOUR, NOT SOURCE TEXT. Feature541's first draft guarded four string literals
// and passed against a seven-line stub that printed "everything is fine, trust me". So every test below
// RUNS the script and reads its exit code: on the real template, and on synthetic fixtures for each
// defect class. A guard that cannot fail is not a guard, and the only way to know it can fail is to make
// it fail.

open System
open System.Diagnostics
open System.IO
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private scriptRelative = "scripts/validate-template-conditionals.fsx"

/// The script's contract, and the reason this file can be terse about it:
///   0  balanced
///   1  imbalance — a named, located defect
///   2  guard error — unreadable input, or ZERO directives found (fails closed, FS-GG/.github#266)
type private Verdict =
    { ExitCode: int
      Stdout: string
      Stderr: string }

    member v.Output = v.Stdout + v.Stderr

/// Run the guard. `root` = None scans the real `template/base` (the subject that actually gates merges).
let private runGuard (root: string option) : Verdict =
    let psi = ProcessStartInfo "dotnet"
    psi.WorkingDirectory <- repositoryRoot
    psi.UseShellExecute <- false
    psi.RedirectStandardOutput <- true
    psi.RedirectStandardError <- true

    [ "fsi"; scriptRelative ] |> List.iter psi.ArgumentList.Add

    match root with
    | Some dir -> [ "--root"; dir ] |> List.iter psi.ArgumentList.Add
    | None -> ()

    match Process.Start psi with
    | null -> failwith $"could not start `dotnet fsi {scriptRelative}`"
    | started ->
        use proc = started
        // Read both pipes BEFORE waiting: a full stderr buffer would deadlock the child otherwise, and
        // this guard is chatty on failure — which is exactly the run we must not hang on.
        let stdout = proc.StandardOutput.ReadToEndAsync()
        let stderr = proc.StandardError.ReadToEndAsync()
        proc.WaitForExit()

        { ExitCode = proc.ExitCode
          Stdout = stdout.Result
          Stderr = stderr.Result }

/// Write `files` into a scratch tree and run the guard over it.
let private onFixture (files: (string * string) list) (assertion: Verdict -> unit) =
    let dir = Path.Combine(Path.GetTempPath(), "tc706-" + Guid.NewGuid().ToString("N").Substring(0, 8))

    try
        for name, content in files do
            let path = Path.Combine(dir, name.Replace('/', Path.DirectorySeparatorChar))

            // `GetDirectoryName` is nullable (a rootless path has no parent). Every `name` here is a
            // relative file under `dir`, so this cannot be null in practice — but the repo compiles with
            // nullness checking on, and asserting the invariant beats suppressing the warning.
            match Path.GetDirectoryName path with
            | null -> failwith $"fixture entry `{name}` has no parent directory"
            | parent -> Directory.CreateDirectory parent |> ignore

            File.WriteAllText(path, content)

        assertion (runGuard (Some dir))
    finally
        try
            Directory.Delete(dir, true)
        with _ ->
            ()

let private balancedFs =
    "//#if (profile == \"game\")\nlet a = 1\n//#else\nlet a = 2\n//#endif\n"

/// The real template's verdict, scanned ONCE. Two tests below read it, and spawning a second `dotnet fsi`
/// to rescan the same tree for the same answer is pure duplicate work in the required gate.
let private realTemplate = lazy (runGuard None)

[<Tests>]
let feature706TemplateConditionalBalanceTests =
    testList
        "Feature706 template conditional balance"
        [
          // THE GATE. Everything else in this file exists to prove that this assertion can fail.
          test "the real template/base is balanced" {
              let v = realTemplate.Value

              Expect.equal
                  v.ExitCode
                  0
                  $"`dotnet fsi {scriptRelative}` says template/base's `//#if` nesting is unbalanced. This is the #680 defect class: the directives are COMMENTS to every compiler in this repo, so an imbalance emits the wrong code for a profile — or silently drops a whole region — and still builds green. The guard names the file:line; fix that, do not silence this.\n\n{v.Output}"

              Expect.stringContains
                  v.Stdout
                  "BALANCED"
                  $"the guard exited 0 without reporting BALANCED — exit code and verdict have drifted apart.\n\n{v.Output}"
          }

          // A guard whose subject has vanished must not report success. `template/base` with no
          // directives in it is not a balanced template — it is the WRONG TREE (a bad merge, a moved
          // directory, a mis-pointed --root), and "nothing to check" must never share an exit code with
          // "checked, and it's fine" (FS-GG/.github#266). This is the fails-open class the whole item is
          // about, so it is asserted rather than trusted.
          test "zero directives is a GUARD ERROR, not a pass — the fail-closed rule" {
              onFixture
                  [ "Product.fs", "let a = 1\n// no conditionals here at all\n" ]
                  (fun v ->
                      Expect.equal
                          v.ExitCode
                          2
                          $"a tree with ZERO `//#if` directives must exit 2 (guard error), not report success. 'No conditionals found' and 'conditionals are balanced' sharing an exit code is the exact fails-open lie (#266) this guard exists to refuse.\n\n{v.Output}"

                      Expect.notEqual
                          v.ExitCode
                          0
                          "an empty subject must never be a pass")
          }

          // Each defect class, on a fixture built to contain exactly one of them. These are what make the
          // green tick above mean something: a guard that cannot fail is indistinguishable from one that
          // is not running.
          test "a STRAY #endif is caught — the #680 defect, exactly" {
              onFixture
                  [ "Product.fs", "let a = 1\n//#endif\n" ]
                  (fun v ->
                      Expect.equal v.ExitCode 1 $"an `#endif` with no open `#if` must be reported as an imbalance.\n\n{v.Output}"
                      Expect.stringContains v.Output "orphan-endif" $"the defect must be NAMED, so a reader knows what to fix.\n\n{v.Output}")
          }

          test "an UNCLOSED #if is caught" {
              onFixture
                  [ "Product.fs", "//#if (profile == \"game\")\nlet a = 1\n" ]
                  (fun v ->
                      Expect.equal v.ExitCode 1 $"an `#if` never closed by an `#endif` must be reported.\n\n{v.Output}"
                      Expect.stringContains v.Output "unclosed-if" $"the defect must be NAMED.\n\n{v.Output}")
          }

          test "an #else outside any #if is caught" {
              onFixture
                  [ "Product.fs", "let a = 1\n//#else\nlet b = 2\n" ]
                  (fun v ->
                      Expect.equal v.ExitCode 1 $"an `#else` with no open `#if` guards nothing — its body is emitted for EVERY profile.\n\n{v.Output}"
                      Expect.stringContains v.Output "orphan-else" $"the defect must be NAMED.\n\n{v.Output}")
          }

          test "a SECOND #else in one region is caught" {
              onFixture
                  [ "Product.fs",
                    "//#if (profile == \"game\")\nlet a = 1\n//#else\nlet a = 2\n//#else\nlet a = 3\n//#endif\n" ]
                  (fun v ->
                      Expect.equal v.ExitCode 1 $"two `#else` branches in one region means one of them is dead, and which one is up to the template engine.\n\n{v.Output}"
                      Expect.stringContains v.Output "duplicate-else" $"the defect must be NAMED.\n\n{v.Output}")
          }

          test "an #elif AFTER the #else is caught — it can never be reached" {
              onFixture
                  [ "Product.fs",
                    "//#if (profile == \"game\")\nlet a = 1\n//#else\nlet a = 2\n//#elif (profile == \"app\")\nlet a = 3\n//#endif\n" ]
                  (fun v ->
                      Expect.equal v.ExitCode 1 $"an `#elif` after the catch-all `#else` is unreachable.\n\n{v.Output}"
                      Expect.stringContains v.Output "elif-after-else" $"the defect must be NAMED.\n\n{v.Output}")
          }

          // THE XML ARM IS LIVE, AND THIS IS THE TEST THAT PROVES IT.
          //
          // The template's `.fsproj` / `.props` regions use `<!--#if -->`, a SECOND directive syntax. If
          // that arm of the parser were dead, this fixture would contain zero recognised directives and
          // the guard would exit 2 (the fail-closed path) — not 1. So insisting on exit 1 here separates
          // "parsed the XML and found the imbalance" from "never parsed the XML at all", which a balanced
          // XML fixture could not do: it would pass vacuously either way.
          test "an unbalanced <!--#if --> region is caught — the XML syntax is really parsed" {
              onFixture
                  [ "Product.fsproj", "<Project>\n  <!--#if (profile == \"game\") -->\n  <X/>\n</Project>\n" ]
                  (fun v ->
                      Expect.equal
                          v.ExitCode
                          1
                          $"an unclosed `<!--#if -->` must be an IMBALANCE (exit 1). Exit 2 here would mean the guard recognised no directives at all — i.e. the XML arm is dead and every `.fsproj`/`.props` region in template/base is going unchecked.\n\n{v.Output}"

                      Expect.stringContains v.Output "unclosed-if" $"the defect must be NAMED.\n\n{v.Output}")
          }

          // ...and the real template's XML is IN SCOPE, not merely parseable. A skip-list that quietly
          // stopped matching `.fsproj`/`.props` would leave the arm above green while the actual regions
          // it guards went unread.
          test "the real scan covers BOTH syntaxes — F# and XML files are both reported" {
              // The guard lists its scanned set on BOTH verdicts, so this reads the same evidence whether
              // or not the template happens to be balanced. It used to depend on the success-only listing,
              // which meant a genuine imbalance made THIS test fail too — with a false "the XML files have
              // fallen out of scope", sending the next reader after a scope bug that was never there.
              let v = realTemplate.Value
              let reported = v.Stdout.Split '\n' |> Array.map (fun l -> l.Trim())

              let mentions (suffixes: string list) =
                  reported
                  |> Array.exists (fun l -> suffixes |> List.exists (fun s -> l.Contains(s + " ") || l.EndsWith s))

              Expect.isTrue
                  (mentions [ ".fs" ])
                  $"the scan of template/base reported no F# file carrying directives — the `//#if` arm has stopped seeing its subject.\n\n{v.Stdout}"

              Expect.isTrue
                  (mentions [ ".fsproj"; ".props" ])
                  $"the scan of template/base reported no `.fsproj`/`.props` file carrying directives, yet both carry `<!--#if -->` regions. The XML files have fallen out of the scan's scope, so their regions are unchecked.\n\n{v.Stdout}"
          }

          // THE CHAIN THAT MAKES THIS FILE REQUIRED, ASSERTED RATHER THAN ASSUMED.
          //
          // This guard has NO `gate.yml` step of its own — it
          // does not need one, because Package.Tests already runs inside the REQUIRED `Deterministic gate`
          // job. But that means the enforcement rides on a three-link chain THIS ITEM DOES NOT OWN:
          //
          //   1. `Package.Tests` is a member of `FS.GG.Rendering.slnx` (true since #540);
          //   2. the `Deterministic gate` job DERIVES its test list from the slnx and runs `dotnet test`
          //      over each project — so membership implies execution, by construction (#235);
          //   3. `Package.Tests` is not diverted into the GL tier, which runs under a display probe.
          //
          // Break any link and every assertion in this file keeps passing locally while running NOWHERE on
          // a PR — the guard would be silently un-required, and the #680 defect class would be unguarded
          // again under a full set of green ticks. That is precisely the fails-open shape (#266) this item
          // exists to close, so the chain is asserted here instead of trusted. A future PR that reverts
          // #540 now has to come here and say so.
          test "this suite really is REQUIRED — Package.Tests is slnx-derived, and the gate runs the slnx" {
              let path rel = Path.Combine(repositoryRoot, (rel: string).Replace('/', Path.DirectorySeparatorChar))
              let slnx = File.ReadAllText(path "FS.GG.Rendering.slnx")
              let gate = File.ReadAllText(path ".github/workflows/gate.yml")

              // Link 1 — membership.
              Expect.stringContains
                  slnx
                  "tests/Package.Tests/Package.Tests.fsproj"
                  "Package.Tests has fallen out of FS.GG.Rendering.slnx. The Deterministic gate derives its test list FROM the slnx, so this suite — and with it the only REQUIRED check on template `//#if` balance — would stop running on PRs while still passing locally. Re-add it, or give the guard its own gate.yml step."

              // The Deterministic gate job's body: from its `name:` to the next 2-space job key.
              let deterministic =
                  let start = gate.IndexOf "name: Deterministic gate"
                  Expect.isGreaterThan start 0 "gate.yml declares the Deterministic gate job"
                  let rest = gate.Substring start
                  let next = Text.RegularExpressions.Regex.Match(rest, @"\n  [a-z][a-z0-9-]*:\n")
                  if next.Success then rest.Substring(0, next.Index) else rest

              // Link 2 — the required job derives its test list from the slnx and runs it.
              Expect.stringContains
                  deterministic
                  "FS.GG.Rendering.slnx"
                  "the REQUIRED Deterministic gate no longer derives its test projects from the slnx. Membership no longer implies execution, so this suite may not run on PRs at all."

              Expect.stringContains
                  deterministic
                  "dotnet test"
                  "the REQUIRED Deterministic gate no longer runs `dotnet test` over its derived project list — a suite nobody invokes is a suite that passes."

              // Link 3 — Package.Tests is not diverted to the display-bound GL tier.
              let glProjects =
                  Text.RegularExpressions.Regex.Match(gate, @"GL_TEST_PROJECTS:\s*""(?<v>[^""]*)""").Groups.["v"].Value

              Expect.isFalse
                  (glProjects.Split(' ') |> Array.contains "Package")
                  $"Package.Tests has been moved into the GL tier (GL_TEST_PROJECTS = \"{glProjects}\"), which runs behind a display/GL probe and degrades rather than blocking. This guard is static — no feed, no SDK, no display — and belongs in the deterministic tier that a merge cannot walk past."
          }

          // Prose ABOUT a directive is not a directive. This is not pedantry: `Product.fsproj:44` reads
          // "behind its own `#if`, rather than pretending one cue map fits both" inside an XML comment,
          // and the product skills are full of markdown anchors like `(#if-you-own-the-backend)`. A guard
          // that matched the bare keyword would red this repo on its own documentation — and a guard that
          // reds on correct input gets silenced, which is how you end up with no guard at all.
          test "prose mentioning #if is not a directive — no false positive" {
              onFixture
                  [ "README.md",
                    "See [If you own the backend](#if-you-own-the-backend).\nIt sits behind its own `#if`, and the #endif is implied.\n"
                    "Product.fs", balancedFs ]
                  (fun v ->
                      Expect.equal
                          v.ExitCode
                          0
                          $"markdown anchors (`(#if-you-own-the-backend)`) and backticked prose (`` `#if` ``) are NOT directives — only a line whose first non-space content is `//#` or `<!--#` is. Matching the bare keyword would red this repo on its own docs.\n\n{v.Output}")
          }
        ]
