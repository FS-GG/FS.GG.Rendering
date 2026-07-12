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

/// The script with its `//` comment lines stripped, so prose about waivers cannot be counted as one.
let private scriptCode =
    script.Split '\n'
    |> Array.filter (fun line -> not (line.TrimStart().StartsWith "//"))
    |> String.concat "\n"

/// A crude, INDEPENDENT count of the same records — one `DriftedSha = "…"` assignment per waiver.
///
/// The structured parse above can fail in two ways that look identical from here: the script genuinely
/// declares no waivers, or the regex has rotted against a shape the script now uses. Counting the same
/// thing a second way tells them apart — the trick the guarded script already plays on the org registry
/// ("A PARTIAL parse is not a pass either": it counts the YAML rows and insists the structured parse
/// agrees). Same overloaded-absence bug, same treatment.
///
/// INDEPENDENT is the load-bearing word, and the first version of this was not (#640). It anchored the
/// count at the start of a line (`^\s*DriftedSha`) — the same assumption the structured regex makes — so
/// a waiver written `{ Id = "…"; DriftedSha = "…"` defeated BOTH, both counted zero, `0 = 0` compared
/// equal, and the vacuous green this cross-check exists to prevent came back wearing its badge. A
/// backstop that shares the parser's blind spot is not a backstop. This one matches the assignment
/// wherever it sits.
let private driftedShaAssignments =
    Regex.Matches(scriptCode, @"DriftedSha\s*=\s*""").Count

/// Both readers above scrape one field NAME out of F# source. That is the last assumption they share, and
/// it is the last way they can both go blind at once: rename `DriftedSha` and BOTH count zero, `0 = 0`
/// compares equal, and a waiver list full of unchecked pins sails through green. Cross-checking two parsers
/// against each other cannot catch what neither can see, so the schema itself is asserted rather than
/// assumed — a rename now has to come here and say so.
let private declaresDriftedShaField =
    Regex.IsMatch(script, @"type\s+Waiver\s*=(.|\n)*?DriftedSha\s*:\s*string")

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
          //
          // AN EMPTY WAIVER LIST IS THE END STATE, NOT A BUG — and this test used to say otherwise (#640).
          // It asserted `Expect.isNonEmpty waivers`, so it went RED on the day the last drifted mirror was
          // re-frozen: the script's stated goal ("a waiver whose mirror is back in sync is DELETED") was a
          // state its own test forbade. The two guards contradicted each other at exactly one point — zero
          // waivers — and the repo could not be healthy and green at the same time.
          //
          // The intent behind `isNonEmpty` was sound: with no waivers parsed, the loop below asserts NOTHING,
          // so a rotted regex would pass vacuously. That is a real hazard and it is still guarded — just not
          // by forbidding the healthy state. Absence is now CROSS-CHECKED (`driftedShaAssignments`, plus the
          // `declaresDriftedShaField` schema canary) instead of being read as failure, which is the same fix
          // the script itself applies to the registry parse.
          test "every waiver's pinned digest is the digest of the file it waives" {
              Expect.isTrue
                  declaresDriftedShaField
                  "scripts/check-frozen-mirrors.fsx no longer declares a `DriftedSha: string` field on `type Waiver`. BOTH readers in this file scrape that field name, so a rename blinds them together: they would both count zero, `0 = 0` would compare equal, and every waiver's pin would go unchecked under a green tick. If you renamed the field, rename it here too."

              Expect.equal
                  (List.length waivers)
                  driftedShaAssignments
                  "the waiver parser in this file disagrees with a crude count of `DriftedSha =` assignments in scripts/check-frozen-mirrors.fsx. The regex has rotted against the script's shape, so the per-waiver assertions below would silently assert NOTHING — a vacuous green over unchecked pins. Teach the regex the new shape. (0 = 0 is fine, and is the end state: every mirror in sync, nothing left to waive.)"

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
