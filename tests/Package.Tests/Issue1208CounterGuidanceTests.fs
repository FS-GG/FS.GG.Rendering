module Issue1208CounterGuidanceTests

open System
open System.IO
open Expecto
open FS.GG.TestSupport

let private root = RepositoryRoot.value
let private skillPath = Path.Combine(root, "template", "product-skills", "fs-gg-testing", "SKILL.md")

let private runnableCounterFence () =
    let text = File.ReadAllText skillPath
    let heading = "### Counter-preserving refactors — exact equality is the cost-driver gate"
    let start = text.IndexOf(heading, StringComparison.Ordinal)
    if start < 0 then failwith "counter-guidance heading is missing"
    let fenceStart = text.IndexOf("```fsharp", start, StringComparison.Ordinal)
    let bodyStart = text.IndexOf('\n', fenceStart) + 1
    let bodyEnd = text.IndexOf("\n```", bodyStart, StringComparison.Ordinal)
    if fenceStart < 0 || bodyStart = 0 || bodyEnd < 0 then failwith "counter-guidance F# fence is malformed"
    text.Substring(bodyStart, bodyEnd - bodyStart)

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
          } ]
