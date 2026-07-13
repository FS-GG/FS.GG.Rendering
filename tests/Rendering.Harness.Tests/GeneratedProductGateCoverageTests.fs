module GeneratedProductGateCoverageTests

// FS-GG/FS.GG.Rendering#719 — the guard that pays for `TemplateLaunchExpressionCoherenceTests`'
// REWRITTEN premise, in the pattern #613 established with `PackageTestsGateMembershipTests`.
//
// WHY THIS EXISTS. #680 (PR #704) added gate.yml's `generated-product-gate`, which scaffolds EVERY
// profile and runs `dotnet build` + `dotnet test` on each, on every PR. That falsified the sentence the
// surviving coherence twin rested on — "no PR job instantiates the template" — and #719 re-derived what
// is actually true: the instantiated `Product.Tests` DO run pre-merge now, so they catch a family whose
// launch call drifts; what they cannot catch is a launch expression NOBODY ASSERTS, because every
// assertion they make is existential (`Expect.stringContains`). The twin supplies that closure, and it
// survives on those grounds instead. Read its header — this file is the half of that argument that has
// to stay TRUE for the other half to mean anything.
//
// Both of the twin's residues are stated against this job, so both die quietly if it does:
//   - a launch expression no `Product.Test` asserts reds in the twin, not in the gate — but only
//     because the gate is what covers the per-family drift that the twin would otherwise be sole
//     guard of;
//   - a family branch present in `Program.fs` but ABSENT from this job's profile list is never
//     instantiated at all, so the twin is the ONLY thing that reads it.
//
// Delete this job, or narrow it back to "one representative profile" (its own header warns against
// exactly that), and the twin's header silently becomes a lie a second time — with no gate underneath
// it. That is the failure #613 refused to leave implicit, and this is the same answer it gave: assert
// the premise, do not assume it.
//
// WHAT IT LOCKS.
//   G-JOB       gate.yml declares a `generated-product-gate` job, and that job really does INSTANTIATE
//               the template (`dotnet new fs-gg-ui`) and RUN the emitted product's tests (`dotnet
//               test`). A job that scaffolds without testing, or is renamed away, fails here.
//   G-PROFILES  the profile list the job loops over is EXACTLY the set `.template.config/template.json`
//               declares. This is the check with teeth: add a sixth profile to the template and forget
//               the loop, and the gate scaffolds five of six while cheerfully reporting success — the
//               new family's `Product.Tests` never run, and its launch call is covered by nothing but
//               the twin. Narrow the loop, and the same hole opens for an EXISTING family.

open System.IO
open System.Text.Json
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

let private gateWorkflow = ".github/workflows/gate.yml"
let private templateConfig = ".template.config/template.json"

/// The profiles the TEMPLATE declares — the `profile` symbol's choices, which is what
/// `dotnet new fs-gg-ui --profile …` will actually accept. This is the source of truth the gate's loop
/// has to keep up with, not the other way round.
let private declaredProfiles () : Set<string> =
    use document = JsonDocument.Parse(File.ReadAllText(repositoryPath templateConfig))

    document.RootElement
        .GetProperty("symbols")
        .GetProperty("profile")
        .GetProperty("choices")
        .EnumerateArray()
    |> Seq.choose (fun choice ->
        match choice.TryGetProperty "choice" with
        | true, value -> Option.ofObj (value.GetString())
        | _ -> None)
    |> Set.ofSeq

/// The `generated-product-gate:` job's body — from its key to the next job key at the same indent.
///
/// Sliced rather than matched against the whole file on purpose: `dotnet test` and the profile names
/// both appear in OTHER jobs of this workflow, so a whole-file scan would report this job as present
/// and complete while it had been deleted outright — green over a missing subject, which is the exact
/// failure the guard it backs (#623, #613) exists to end.
let private gateJobBody () : string option =
    let text = File.ReadAllText(repositoryPath gateWorkflow)

    match Regex.Match(text, @"(?m)^  generated-product-gate:[ \t]*$") with
    | key when not key.Success -> None
    | key ->
        let rest = text.Substring(key.Index + key.Length)

        match Regex.Match(rest, @"(?m)^  [A-Za-z0-9_.-]+:[ \t]*$") with
        | next when next.Success -> Some(rest.Substring(0, next.Index))
        | _ -> Some rest

/// The job body, or a loud failure naming what its absence breaks. Both checks below need it, and
/// "the job is gone" is the same finding for both — so it is stated once.
let private requireGateJobBody () =
    match gateJobBody () with
    | Some body -> body
    | None ->
        failtest
            "gate.yml no longer declares a `generated-product-gate:` job. TemplateLaunchExpressionCoherenceTests' header credits that job with running the instantiated product's tests on every PR, and its surviving twin's scope is stated against it — restore the job, or re-derive that header (again) and say what is now true."

/// The profiles that job's loop actually iterates.
let private gatedProfiles (jobBody: string) : Set<string> =
    match Regex.Match(jobBody, @"for\s+profile\s+in\s+([^;\n]+?)\s*;\s*do") with
    | loop when not loop.Success -> Set.empty
    | loop ->
        loop.Groups.[1].Value.Split(' ')
        |> Array.map (fun profile -> profile.Trim())
        |> Array.filter (fun profile -> profile <> "")
        |> Set.ofArray

[<Tests>]
let generatedProductGateCoverageTests =
    testList
        "#719 — generated-product-gate instantiates every profile (the premise the surviving twin rests on)"
        [
          // G-JOB — the job exists, and it does the two things the twin's header credits it with.
          test "gate.yml declares a generated-product-gate job that scaffolds the template and runs its tests" {
              let body = requireGateJobBody ()

              Expect.stringContains
                  body
                  "dotnet new fs-gg-ui"
                  "the generated-product-gate job must INSTANTIATE the template — without `dotnet new fs-gg-ui` it is not the job the twin's header describes, whatever it is named"

              Expect.stringContains
                  body
                  "dotnet test"
                  "the generated-product-gate job must RUN the emitted product's tests — scaffolding a product without testing it leaves template/base/tests/Product.Tests unexercised on the PR, which is precisely the pre-#680 state the twin's header used to describe"
          }

          // G-PROFILES — every declared profile is actually scaffolded. The narrowing hole, closed.
          test "the gate scaffolds exactly the profiles the template declares" {
              let body = requireGateJobBody ()
              let declared = declaredProfiles ()
              let gated = gatedProfiles body

              // Fail loud, never vacuous: either set coming back empty would satisfy the equality below
              // trivially, and this guard would report green having compared nothing.
              Expect.isNonEmpty
                  (Set.toList declared)
                  "no `profile` choices parsed out of .template.config/template.json — this guard is not reading the template it thinks it is"

              Expect.isNonEmpty
                  (Set.toList gated)
                  "no profiles parsed out of the generated-product-gate job's `for profile in …; do` loop — either the loop was rewritten into a form this guard cannot read (fix the guard, in this PR) or the job no longer loops over profiles at all (fix the job)"

              let unscaffolded = Set.difference declared gated |> Set.toList
              let unknown = Set.difference gated declared |> Set.toList

              Expect.isEmpty
                  unscaffolded
                  (sprintf
                      "these profiles are declared by the template but NEVER SCAFFOLDED by the gate — their Product.Tests run in no PR cadence at all, and the gate still exits 0 saying so: %A. Add them to the `for profile in …` loop in .github/workflows/gate.yml."
                      unscaffolded)

              Expect.isEmpty
                  unknown
                  (sprintf
                      "the gate scaffolds profiles the template does not declare — `dotnet new fs-gg-ui --profile` will reject these, so the job is red for a reason that has nothing to do with the tree under review: %A"
                      unknown)
          }
        ]
