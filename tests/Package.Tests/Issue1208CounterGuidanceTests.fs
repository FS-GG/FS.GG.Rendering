module Issue1208CounterGuidanceTests

open System
open System.Diagnostics
open System.IO
open Expecto
open FS.GG.TestSupport

let private root = RepositoryRoot.value
let private skillPath = Path.Combine(root, "template", "product-skills", "fs-gg-testing", "SKILL.md")

let private fenceUnderPath path (heading: string) =
    let text = File.ReadAllText path
    let start = text.IndexOf(heading, StringComparison.Ordinal)
    if start < 0 then failwith $"counter-guidance heading is missing: {heading}"
    let fenceStart = text.IndexOf("```fsharp", start, StringComparison.Ordinal)
    let bodyStart = text.IndexOf('\n', fenceStart) + 1
    let bodyEnd = text.IndexOf("\n```", bodyStart, StringComparison.Ordinal)
    if fenceStart < 0 || bodyStart = 0 || bodyEnd < 0 then failwith "counter-guidance F# fence is malformed"
    text.Substring(bodyStart, bodyEnd - bodyStart)

let private runnableCounterFence () =
    fenceUnderPath skillPath "### Counter-preserving refactors — exact equality is the cost-driver gate"

let private contextualApiFence path =
    let heading = "The pure fixture above makes the mutation logic easy to transplant."
    fenceUnderPath path heading

type private CommandResult = { ExitCode: int; Output: string }

let private runDotnet workingDirectory dotnetHome arguments =
    let display = String.concat " " arguments
    let start = ProcessStartInfo("dotnet")
    start.WorkingDirectory <- workingDirectory
    start.UseShellExecute <- false
    start.RedirectStandardOutput <- true
    start.RedirectStandardError <- true
    start.Environment["DOTNET_CLI_HOME"] <- dotnetHome
    start.Environment["DOTNET_NOLOGO"] <- "1"
    arguments |> List.iter start.ArgumentList.Add

    use child =
        match Process.Start start |> Option.ofObj with
        | Some started -> started
        | None -> failwith $"could not start dotnet {display}"

    let stdout = child.StandardOutput.ReadToEndAsync()
    let stderr = child.StandardError.ReadToEndAsync()
    if not (child.WaitForExit(TimeSpan.FromMinutes 5.0)) then
        child.Kill true
        failwith $"dotnet {display} timed out"
    { ExitCode = child.ExitCode; Output = stdout.Result + Environment.NewLine + stderr.Result }

[<Tests>]
let tests =
    testList
        "Rendering #1208 counter-guidance"
        [ test "the shipped counter fixture is fence-compiled and carries the review decision rule" {
              let skill = File.ReadAllText skillPath
              [ "changes **any write site**"
                "assert **exact equality**"
                "intentional inequality"
                "complements, never replaces, behavior tests"
                "Enumerate every changed counter write site"
                "drop an increment"
                "target a different counter" ]
              |> List.iter (fun token -> Expect.stringContains skill token $"skill teaches {token}")

              let fixture = runnableCounterFence ()
              [ "type Instrumentation = { PhysicsQueries: int; SceneNodes: int }"
                "type Model = { Entities: int; Instrumentation: Instrumentation }"
                "let droppedIncrementUpdate"
                "let wrongCounterUpdate"
                "let ordinaryBehaviorTest model = model.Entities = 7"
                "let exactCounterEvidence workload counter expected actual"
                "requireCounterFailure droppedIncrementUpdate"
                "requireCounterFailure wrongCounterUpdate" ]
              |> List.iter (fun token -> Expect.stringContains fixture token $"compiled fence preserves {token}")
          }

          test "the contextual witness compiles and executes against a generated product's real API" {
              let fixtureRoot = Path.Combine(Path.GetTempPath(), "fsgg-counter-api-" + Guid.NewGuid().ToString("N"))
              let dotnetHome = Path.Combine(fixtureRoot, "dotnet-home")
              let productRoot = Path.Combine(fixtureRoot, "CounterApiFixture")
              Directory.CreateDirectory fixtureRoot |> ignore

              try
                  let install = runDotnet fixtureRoot dotnetHome [ "new"; "install"; root; "--force" ]
                  Expect.equal install.ExitCode 0 $"template install succeeds:{Environment.NewLine}{install.Output}"

                  let instantiate =
                      runDotnet fixtureRoot dotnetHome
                          [ "new"; "fs-gg-ui"; "--name"; "CounterApiFixture"; "--profile"; "game"
                            "--lifecycle"; "none"; "--output"; productRoot ]
                  Expect.equal instantiate.ExitCode 0 $"game product instantiates:{Environment.NewLine}{instantiate.Output}"

                  let testRoot = Path.Combine(productRoot, "tests", "CounterApiFixture.Tests")
                  let witnessPath = Path.Combine(testRoot, "CounterCostDriverWitness.fs")
                  let materializedSkill = Path.Combine(productRoot, ".agents", "skills", "fs-gg-testing", "SKILL.md")
                  File.WriteAllText(witnessPath, contextualApiFence materializedSkill)

                  let projectPath = Path.Combine(testRoot, "CounterApiFixture.Tests.fsproj")
                  let project = File.ReadAllText projectPath
                  let anchor = "    <Compile Include=\"Program.fs\" />"
                  Expect.stringContains project anchor "generated test project has a stable compile anchor"
                  File.WriteAllText(projectPath, project.Replace(anchor, "    <Compile Include=\"CounterCostDriverWitness.fs\" />" + Environment.NewLine + anchor, StringComparison.Ordinal))

                  let executed =
                      runDotnet productRoot dotnetHome
                          [ "test"; projectPath; "--filter"; "Name~published-template-counter-witness"
                            "--logger"; "console;verbosity=minimal" ]
                  Expect.equal executed.ExitCode 0 $"published template API witness compiles and executes:{Environment.NewLine}{executed.Output}"
              finally
                  if Directory.Exists fixtureRoot then Directory.Delete(fixtureRoot, true)
          } ]
