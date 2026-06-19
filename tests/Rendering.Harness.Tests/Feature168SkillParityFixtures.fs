module Feature168SkillParityFixtures

open System
open System.IO
open System.Security.Cryptography
open System.Text
open Rendering.Harness

let createTempRoot name =
    let root = Path.Combine(Path.GetTempPath(), name + "-" + Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory root |> ignore
    root

let deleteTempRoot root =
    if Directory.Exists root then
        Directory.Delete(root, true)

let request root fixtureName =
    let outDir = Path.Combine(root, "out", "parity")

    { SkillParity.defaultRequest root with
        OutDir = outDir
        ReportPath = Path.Combine(root, "docs", "reports", "skills-parity.md")
        SummaryJsonPath = Path.Combine(root, "readiness", "skill-parity-summary.json")
        FixtureMode = Some fixtureName }

let repositoryRequest root =
    { SkillParity.defaultRequest root with
        OutDir = Path.Combine(root, "specs", "168-skill-parity-evidence", "readiness", "parity")
        ReportPath = Path.Combine(root, "docs", "reports", "skills-parity.md")
        SummaryJsonPath = Path.Combine(root, "specs", "168-skill-parity-evidence", "readiness", "skill-parity-summary.json") }

let fileHash path =
    use sha = SHA256.Create()
    let bytes = File.ReadAllBytes path

    sha.ComputeHash bytes
    |> Array.map (fun b -> b.ToString("x2"))
    |> String.concat ""

let coveredBody =
    "FS.GG.UI. package pins use scripts/refresh-local-feed-and-samples.fsx and package-feed local feed proof for stale package pins. specs/*/readiness/ is allowlisted through .gitignore and git check-ignore. dotnet test for the same project/configuration needs isolated output or BaseOutputPath. screenshot evidence records degraded capture, reviewer accepted readiness, and generated summary caveats. pointer and keyboard responsiveness separate routing from update render present latency. package bump uses local feed, sample package pins, restore validation, and readiness ledger updates. canceled timed-out synthetic substitute environment-limited pending-review checks keep caveats visible."

let entry (path: string) (name: string) (description: string) (body: string) : SkillParity.SkillEntry =
    { SkillName = name
      Description = description
      Path = path
      AbsolutePath = path
      SurfaceId = "synthetic"
      EntryKind = SkillParity.CanonicalEntry
      Metadata = Map [ "name", name; "description", description ]
      BodyHash = string body.Length
      Content = body
      WrapperTarget = None }
