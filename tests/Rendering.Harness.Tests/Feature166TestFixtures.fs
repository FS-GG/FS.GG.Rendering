module Feature166TestFixtures

open System
open System.IO
open Rendering.Harness

let createTempRoot name =
    let root = Path.Combine(Path.GetTempPath(), name + "-" + Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory root |> ignore
    root

let deleteTempRoot root =
    if Directory.Exists root then
        Directory.Delete(root, true)

let bashCommand script : ValidationLanes.LaneCommand =
    { FileName = "bash"
      Arguments = [ "-lc"; script ] }

let laneWith
    root
    id
    role
    script
    timeout
    noProgress
    concurrencyGroup
    outputScope
    : ValidationLanes.LaneDefinition =
    let dir = Path.Combine(root, "lanes", id)

    { Id = id
      DisplayName = id
      Description = id
      ReadinessRole = role
      Command = bashCommand script
      WorkingDirectory = root
      Timeout = timeout
      NoProgressTimeout = noProgress
      ProgressInterval = TimeSpan.FromSeconds 1.0
      EvidenceDirectory = dir
      LogPath = Path.Combine(dir, "log.txt")
      ResultPath = Path.Combine(dir, "result.json")
      DiagnosticsPath = Path.Combine(dir, "diagnostics.md")
      OutputRoot = Path.Combine(dir, "out")
      ConcurrencyGroup = concurrencyGroup
      OutputScope = outputScope
      IsAggregate = id = "aggregate-solution"
      SubstitutesFor = if id = "aggregate-solution" then None else Some "aggregate-solution" }

let lane root id script =
    laneWith
        root
        id
        ValidationLanes.Required
        script
        (TimeSpan.FromSeconds 2.0)
        None
        (Some id)
        (Some id)

let result id role status : ValidationLanes.LaneResult =
    { LaneId = id
      ReadinessRole = role
      Status = status
      Command = "synthetic command"
      StartedUtc = None
      CompletedUtc = None
      Elapsed = Some(TimeSpan.FromMilliseconds 1.0)
      TimeoutBudget = Some(TimeSpan.FromSeconds 1.0)
      LastActivityUtc = None
      LastActivityText = None
      ExitCode = None
      LogPath = $"lanes/{id}/log.txt"
      ResultPath = $"lanes/{id}/result.json"
      DiagnosticsPath = $"lanes/{id}/diagnostics.md"
      ResultArtifacts = [ $"lanes/{id}/result.json"; $"lanes/{id}/log.txt" ]
      RuntimeDiagnostics = None
      Reason = if status = ValidationLanes.Passed then None else Some(status |> ValidationLanes.statusToken)
      Diagnostics = []
      Caveats = []
      AcceptedEnvironmentLimitation = None
      Substitution = None
      IsAggregate = id = "aggregate-solution" }

let summary root results : ValidationLanes.ValidationSummary =
    { RunId = "synthetic-run"
      PolicyVersion = "validation-lanes-v1"
      OverallReadiness = ValidationLanes.computeOverallReadiness results
      ArtifactRoot = root
      StartedUtc = DateTime.UtcNow
      CompletedUtc = DateTime.UtcNow
      FirstBlockingRequiredLane = ValidationLanes.firstBlockingRequiredLane results
      LaneResults = results
      Caveats = []
      ReplacementNotice = None }

let assertFileContains path text =
    let content = File.ReadAllText path
    Expecto.Expect.stringContains content text text
