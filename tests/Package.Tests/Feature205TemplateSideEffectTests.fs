module Feature205TemplateSideEffectTests

// Feature 205 — generation is side-effect-free by default; git-init/chmod move off the auto-run
// post-actions and behind a single explicit `--initGit` opt-in.
//
// Deterministic, GL-free, NO `dotnet new`: this gate re-derives the structural guarantees directly
// from `.template.config/template.json` and the repo tree. Those structural invariants are exactly
// what produce the observable behavior — a default generation that fires no Run post-action cannot
// spawn a process, hang on the allow-scripts prompt, or create a repo. The live behavioral evidence
// (real `dotnet new` per scenario: default rc=0/0 s/no .git; `--initGit true` ⇒ repo + initial
// commit + executable scripts; existing-repo no-nest; git-absent non-fatal) is recorded under
// `specs/205-scaffold-git-init-chmod/readiness/smoke-after.md`.
//
// Authored failing-first (Principle V): against the pre-feature manifest these cases are RED — the
// `skipGitInit` symbol still exists (GV-2), and the auto-run post-actions are gated on `!skipGitInit`
// rather than `initGit`, so GV-1/GV-3/GV-4 fail. They pass only once the option surface is flipped
// and the auto-run actions are removed.

open System
open System.IO
open System.Text.Json
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

let private templateJsonPath = repositoryPath ".template.config/template.json"

/// The `dotnet new` Run post-action processor id — the only processor that spawns a process.
let private RUN_PROCESSOR = "3A7C4B45-1F5D-4A30-959A-51B88E82B5D2"

let private elemStr (e: JsonElement) : string =
    match e.ValueKind with
    | JsonValueKind.String -> e.GetString() |> Option.ofObj |> Option.defaultValue ""
    | _ -> e.ToString()

let private templateDoc () = JsonDocument.Parse(File.ReadAllText templateJsonPath)

let private symbols (doc: JsonDocument) = doc.RootElement.GetProperty("symbols")

let private tryProp (e: JsonElement) (name: string) =
    match e.TryGetProperty name with
    | true, v -> Some v
    | _ -> None

/// Each post-action flattened to (condition, actionId, argsString, hasManualInstructions, continueOnError).
let private postActions (doc: JsonDocument) =
    [ for pa in doc.RootElement.GetProperty("postActions").EnumerateArray() ->
          let str name = tryProp pa name |> Option.map elemStr |> Option.defaultValue ""
          let argsStr =
              tryProp pa "args"
              |> Option.bind (fun a -> tryProp a "args")
              |> Option.map elemStr
              |> Option.defaultValue ""
          let hasManual =
              match tryProp pa "manualInstructions" with
              | Some m -> m.ValueKind = JsonValueKind.Array && m.GetArrayLength() > 0
              | None -> false
          let continueOnError =
              match tryProp pa "continueOnError" with
              | Some c -> c.ValueKind = JsonValueKind.True
              | None -> false
          str "condition", str "actionId", argsStr, hasManual, continueOnError ]

/// Recursively scan a repo subtree for a literal substring across the given extensions.
let private grepTree (relRoot: string) (exts: string list) (needle: string) =
    let root = repositoryPath relRoot
    if not (Directory.Exists root) then []
    else
        Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
        |> Seq.filter (fun f ->
            let p = f.Replace('\\', '/')
            not (p.Contains "/bin/" || p.Contains "/obj/") && (exts |> List.exists p.EndsWith))
        |> Seq.filter (fun f -> (File.ReadAllText f).Contains needle)
        |> Seq.map (fun f -> Path.GetRelativePath(repositoryRoot, f))
        |> Seq.toList

[<Tests>]
let feature205TemplateSideEffectTests =
    testList
        "Feature205 template side-effect-free generation"
        [
          // GV-1 (FR-003/FR-012, data-model): `initGit` opt-in symbol — bool, default false,
          // self-describing for --help (mentions repository + commit + executable scripts).
          test "GV-1 initGit opt-in symbol is present, bool, default false, and self-describing" {
              use doc = templateDoc ()
              let initGit =
                  match tryProp (symbols doc) "initGit" with
                  | Some s -> s
                  | None -> failtest "initGit symbol missing from template.json"
              Expect.equal (tryProp initGit "type" |> Option.map elemStr) (Some "parameter") "initGit is a parameter"
              Expect.equal (tryProp initGit "datatype" |> Option.map elemStr) (Some "bool") "initGit is a bool"
              Expect.equal (tryProp initGit "defaultValue" |> Option.map elemStr) (Some "false") "initGit defaults to false (side-effect-free)"
              let desc = (tryProp initGit "description" |> Option.map elemStr |> Option.defaultValue "").ToLowerInvariant()
              Expect.isTrue (desc.Contains "git") "description mentions git"
              Expect.isTrue (desc.Contains "commit") "description mentions the initial commit"
              Expect.isTrue (desc.Contains "executable") "description mentions making scripts executable"
              Expect.isTrue (desc.Contains "scaffold") "description notes it is unnecessary under the scaffold path"
          }

          // GV-2 (FR-001/FR-002, R1): the opt-out `skipGitInit` symbol is gone, and no condition
          // anywhere in the manifest still references it.
          test "GV-2 skipGitInit symbol is removed and unreferenced" {
              use doc = templateDoc ()
              Expect.isNone (tryProp (symbols doc) "skipGitInit") "skipGitInit symbol must be removed"
              let referencingSkip =
                  postActions doc |> List.filter (fun (c, _, _, _, _) -> c.Contains "skipGitInit")
              Expect.isEmpty referencingSkip "no post-action condition may reference skipGitInit"
          }

          // GV-3 (FR-001/G2/G3, SC-001): no Run post-action fires by default. Every process-spawning
          // post-action is gated on `initGit`, so a default generation (initGit=false) runs nothing —
          // it cannot hang on the allow-scripts prompt or create a repo.
          test "GV-3 every process-spawning post-action is gated on initGit (no auto-run default)" {
              use doc = templateDoc ()
              let runActions = postActions doc |> List.filter (fun (_, a, _, _, _) -> a = RUN_PROCESSOR)
              Expect.isNonEmpty runActions "expected the opt-in Run post-actions to exist"
              for (cond, _, _, _, _) in runActions do
                  Expect.isTrue (cond.Contains "initGit") (sprintf "Run post-action must be gated on initGit; got condition %s" cond)
              // none is unconditionally-on or gated only on OS (which would auto-run).
              let autoRun =
                  runActions |> List.filter (fun (c, _, _, _, _) -> not (c.Contains "initGit"))
              Expect.isEmpty autoRun "no Run post-action may run without the initGit opt-in"
          }

          // GV-4 (R2, C1/C2/C3): the gated actions keep the hardened guards verbatim — existing-repo
          // detection, git-presence check, allow-empty initial commit, continueOnError.
          test "GV-4 the initGit-gated actions retain the hardened safety guards" {
              use doc = templateDoc ()
              let gated =
                  postActions doc
                  |> List.filter (fun (c, a, _, _, _) -> a = RUN_PROCESSOR && c.Contains "initGit")
              Expect.isTrue (gated.Length >= 2) (sprintf "expected a Unix and a Windows gated action, found %d" gated.Length)
              // every gated Run action is non-fatal (continueOnError) and carries manual instructions.
              for (_, _, _, hasManual, continueOnError) in gated do
                  Expect.isTrue continueOnError "gated action must keep continueOnError: true (cannot fail generation)"
                  Expect.isTrue hasManual "gated action carries manualInstructions"
              // the combined argument strings carry the existing-repo guard, git-presence check, and
              // the spec-kit initial commit message.
              let allArgs = gated |> List.map (fun (_, _, args, _, _) -> args) |> String.concat "\n"
              Expect.stringContains allArgs "--is-inside-work-tree" "existing-repo guard present (C2)"
              Expect.stringContains allArgs "[Spec Kit] Initial commit" "initial commit message present (C1)"
              Expect.stringContains allArgs "--allow-empty" "allow-empty keeps the initial commit non-fatal"
              Expect.isTrue
                  (allArgs.Contains "command -v git" || allArgs.Contains "Get-Command git")
                  "git-presence check present (C3)"
          }

          // GV-5 (FR-011): cross-platform parity — both a non-Windows and a Windows opt-in action,
          // each gated on initGit, mirror each other.
          test "GV-5 cross-platform parity: gated actions for both Windows and non-Windows" {
              use doc = templateDoc ()
              let gatedConds =
                  postActions doc
                  |> List.filter (fun (c, a, _, _, _) -> a = RUN_PROCESSOR && c.Contains "initGit")
                  |> List.map (fun (c, _, _, _, _) -> c)
              Expect.isTrue
                  (gatedConds |> List.exists (fun c -> c.Contains "OS != \"Windows_NT\""))
                  "a non-Windows opt-in action exists"
              Expect.isTrue
                  (gatedConds |> List.exists (fun c -> c.Contains "OS == \"Windows_NT\""))
                  "a Windows opt-in action exists"
          }

          // GV-6 (FR-010/SC-002, G5): no defensive opt-out flag remains in the repo's own tooling or
          // tests — the kill-loop's reason for existing is gone.
          test "GV-6 no skipGitInit defensive flag survives in scripts or tests" {
              let hits =
                  grepTree "scripts" [ ".fsx"; ".fs"; ".sh" ] "skipGitInit"
                  @ grepTree "tests" [ ".fs"; ".fsx" ] "skipGitInit"
                  // this gate names the removed flag in prose; exclude self.
                  |> List.filter (fun p -> not (p.EndsWith "Feature205TemplateSideEffectTests.fs"))
              Expect.isEmpty hits (sprintf "no skipGitInit references may remain; found in: %s" (String.concat ", " hits))
          }
        ]
