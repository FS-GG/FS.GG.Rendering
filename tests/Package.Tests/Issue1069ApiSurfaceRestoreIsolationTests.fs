module Issue1069ApiSurfaceRestoreIsolationTests

open System
open System.IO
open Expecto
open FS.GG.TestSupport

let private generator =
    File.ReadAllText(
        Path.Combine(
            RepositoryRoot.value,
            "scripts",
            "refresh-api-surface-mirror.fsx"
        )
    )

[<Tests>]
let restoreIsolation =
    testList
        "issue-1069 API-surface restore isolation"
        [ test "the probe explicitly selects the config it writes" {
              Expect.stringContains
                  generator
                  """let nugetConfigPath = Path.Combine(work, "NuGet.Config")"""
                  "the probe gives its private config the cross-platform canonical name"

              Expect.stringContains
                  generator
                  "File.WriteAllText(\n            nugetConfigPath,"
                  "the generated isolated source list is written to the selected path"

              Expect.stringContains
                  generator
                  "\"--configfile\"\n          nugetConfigPath"
                  "restore explicitly selects the generated config, so an enclosing hostile mapping cannot win"
          }

          test "the restore is argument-list based and has no implicitly configured fallback" {
              Expect.stringContains
                  generator
                  "List.iter psi.ArgumentList.Add"
                  "config and package paths are passed as discrete arguments on Linux"

              Expect.isFalse
                  (generator.Contains("restore probe.fsproj --packages", StringComparison.Ordinal))
                  "no bare restore command remains that can inherit repository or user package-source mapping"
          } ]
