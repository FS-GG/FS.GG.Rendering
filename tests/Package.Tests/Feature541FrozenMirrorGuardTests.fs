module Feature541FrozenMirrorGuardTests

// #541 — the guard that stops a Rendering PR editing a frozen skill mirror it does not own.
//
// THIS FILE GUARDS THE GUARD, and the first version of it did not. Every assertion was a
// `stringContains` on the script's source text, so all four passed against a seven-line stub that kept
// the magic strings and printed "everything is fine, trust me". They guarded four string literals.
//
// What is asserted here now is what can be checked OFFLINE and would actually rot:
//   * the waiver digests still match the files they waive — the single most likely thing to go stale,
//     and the exact canonical-vs-drifted copy-paste that would make a waiver never fire;
//   * every mirrored body still exists (a DELETED mirror is as much a break as an edited one);
//   * the guard is wired into the REQUIRED job, not merely mentioned in the workflow file.
//
// The digest comparison against the ORG REGISTRY stays in the script: it needs the network and a token,
// and a mock of it here would assert that the mock works.

open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relative: string) =
    Path.Combine(repositoryRoot, relative.Replace('/', Path.DirectorySeparatorChar))

let private scriptRelative = "scripts/check-frozen-mirrors.fsx"
let private script = File.ReadAllText(repositoryPath scriptRelative)

let private sha256Of (path: string) =
    use sha = System.Security.Cryptography.SHA256.Create()

    sha.ComputeHash(File.ReadAllBytes path)
    |> Array.map (fun b -> b.ToString "x2")
    |> String.concat ""

/// The waivers the script declares: `{ Id = "…"; DriftedSha = "…"; … }`.
let private waivers =
    Regex.Matches(script, @"Id\s*=\s*""(?<id>[^""]+)""\s*\n\s*DriftedSha\s*=\s*""(?<sha>[0-9a-f]{64})""")
    |> Seq.cast<Match>
    |> Seq.map (fun m -> m.Groups.["id"].Value, m.Groups.["sha"].Value)
    |> List.ofSeq

/// The skills the script declares this repo MIRRORS (as opposed to `NoCounterpart`).
let private mirrored =
    Regex.Matches(script, @"""(?<id>fs-gg-[a-z-]+)"",\s*Mirrored")
    |> Seq.cast<Match>
    |> Seq.map (fun m -> m.Groups.["id"].Value)
    |> List.ofSeq

[<Tests>]
let feature541FrozenMirrorGuardTests =
    testList
        "Feature541 frozen skill-mirror guard"
        [
          // THE HIGHEST-VALUE OFFLINE CHECK, and it needs no network. A waiver pins the digest of a body
          // that was ALREADY drifted when the guard landed. If somebody edits a waived mirror and updates
          // the pin, this stays green (that is the script's job to catch) — but if a pin is simply WRONG,
          // the waiver never matches and the guard reds `main` for a reason nobody can see. And a pin
          // copy-pasted from the CANONICAL digest instead of the drifted one would never fire at all,
          // silently un-waiving the drift it was written for.
          test "every waiver's pinned digest is the digest of the file it waives" {
              Expect.isNonEmpty waivers "the script declares waivers (if this parses to nothing, the assertion below is vacuous)"

              for id, pinned in waivers do
                  let body = repositoryPath $"template/product-skills/{id}/SKILL.md"

                  Expect.isTrue (File.Exists body) $"a waiver names `{id}`, so this repo must still ship that mirror"

                  Expect.equal
                      (sha256Of body)
                      pinned
                      $"the waiver for `{id}` pins a digest that is NOT the current body's. A wrong pin is not a small error: the waiver stops matching and the gate reds for a reason nobody can see — or, if the digest was copy-pasted from the CANONICAL instead of the drifted body, it never fires and silently un-waives the drift it exists for."
          }

          // A DELETED mirror is as much a break as an edited one — a `--profile game` scaffold just loses
          // the skill. Checked here as well as in the script, because it is free offline.
          test "every mirror the script declares is still shipped" {
              Expect.isNonEmpty mirrored "the script declares which foreign skills this repo mirrors"

              for id in mirrored do
                  Expect.isTrue
                      (File.Exists(repositoryPath $"template/product-skills/{id}/SKILL.md"))
                      $"`{id}` is declared `Mirrored`, so this repo must ship a byte-identical copy of its canonical (ADR-0022 §6). Deleting it is a break: a `--profile game` scaffold loses the skill."
          }

          // The step is the gate. A guard the workflow does not call is a guard that reports green — which
          // is the whole shape of #541, and of the .github#266 fail-open family.
          //
          // Asserted against the REQUIRED job, not just the file: moving the step into an advisory workflow
          // would keep a "does gate.yml mention it" grep green while the check stopped blocking anything.
          test "the REQUIRED Deterministic gate job runs it — a check nobody invokes is a check that passes" {
              let gate = File.ReadAllText(repositoryPath ".github/workflows/gate.yml")

              Expect.stringContains
                  gate
                  "dotnet fsi scripts/check-frozen-mirrors.fsx"
                  "gate.yml must invoke the frozen-mirror guard"

              // The Deterministic gate is the required job; everything from its `name:` to the next job's
              // 2-space `key:` is its body.
              let deterministic =
                  let start = gate.IndexOf "name: Deterministic gate"
                  Expect.isGreaterThan start 0 "gate.yml declares the Deterministic gate job"
                  let rest = gate.Substring start
                  let next = Regex.Match(rest, @"\n  [a-z][a-z0-9-]*:\n")
                  if next.Success then rest.Substring(0, next.Index) else rest

              Expect.stringContains
                  deterministic
                  "dotnet fsi scripts/check-frozen-mirrors.fsx"
                  "the guard must run in the REQUIRED Deterministic gate job. In an advisory workflow it would still be 'mentioned in gate.yml' and would block nothing — and every previous mirror break already merged under a full set of green checks (#541)."
          }
        ]
