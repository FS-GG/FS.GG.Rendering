module Issue1069ApiSurfaceRestoreIsolationTests

open System
open System.Diagnostics
open System.IO
open System.IO.Compression
open Expecto
open FsGg

let private run (psi: ProcessStartInfo) =
    use proc =
        match Process.Start psi with
        | null -> failtest "could not start dotnet restore"
        | started -> started

    let stdout = proc.StandardOutput.ReadToEndAsync()
    let stderr = proc.StandardError.ReadToEndAsync()

    if not (proc.WaitForExit(TimeSpan.FromMinutes 2.0)) then
        proc.Kill true
        failtest "restore timed out"

    proc.ExitCode, stdout.Result + Environment.NewLine + stderr.Result

let private addTextEntry (archive: ZipArchive) (path: string) (text: string) =
    let entry = archive.CreateEntry path
    use writer = new StreamWriter(entry.Open())
    writer.Write text

let private createPackage feed =
    let packagePath = Path.Combine(feed, "Hostile.Mapping.Probe.1.0.0.nupkg")

    use archive = ZipFile.Open(packagePath, ZipArchiveMode.Create)

    addTextEntry
        archive
        "Hostile.Mapping.Probe.nuspec"
        """<?xml version="1.0"?>
<package>
  <metadata>
    <id>Hostile.Mapping.Probe</id>
    <version>1.0.0</version>
    <authors>FS-GG</authors>
    <description>Offline restore-isolation fixture.</description>
  </metadata>
</package>"""

    addTextEntry archive "lib/net10.0/_._" ""

[<Tests>]
let restoreIsolation =
    testList
        "issue-1069 API-surface restore isolation"
        [ test "the real restore selects its isolated config over a hostile enclosing mapping" {
              let root =
                  Path.Combine(Path.GetTempPath(), "fsgg-api-surface-restore-test-" + Guid.NewGuid().ToString("N"))

              let work = Path.Combine(root, "work")
              let feed = Path.Combine(root, "feed")
              Directory.CreateDirectory work |> ignore
              Directory.CreateDirectory feed |> ignore

              try
                  createPackage feed

                  File.WriteAllText(
                      Path.Combine(root, "NuGet.Config"),
                      """<?xml version="1.0"?>
<configuration>
  <packageSources>
    <clear />
    <add key="hostile" value="./source-that-does-not-exist" />
  </packageSources>
  <packageSourceMapping>
    <clear />
    <packageSource key="hostile">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>"""
                  )

                  File.WriteAllText(
                      Path.Combine(work, "probe.fsproj"),
                      """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Hostile.Mapping.Probe" Version="1.0.0" />
  </ItemGroup>
</Project>"""
                  )

                  let isolatedConfig = Path.Combine(work, "probe-isolated.config")

                  File.WriteAllText(
                      isolatedConfig,
                      $"""<?xml version="1.0"?>
<configuration>
  <packageSources>
    <clear />
    <add key="isolated" value="{feed}" />
  </packageSources>
</configuration>"""
                  )

                  let bare = ProcessStartInfo("dotnet")
                  bare.WorkingDirectory <- work
                  bare.RedirectStandardOutput <- true
                  bare.RedirectStandardError <- true
                  [ "restore"; "probe.fsproj"; "--packages"; Path.Combine(root, "bare-packages") ]
                  |> List.iter bare.ArgumentList.Add

                  let bareExit, bareOutput = run bare
                  Expect.notEqual bareExit 0 "the enclosing hostile source mapping controls a bare restore"
                  Expect.stringContains
                      bareOutput
                      "source-that-does-not-exist"
                      "the negative control inherited the enclosing hostile source"

                  let isolated =
                      ApiSurfaceRestore.startInfo
                          work
                          "probe.fsproj"
                          (Path.Combine(root, "isolated-packages"))
                          isolatedConfig

                  let isolatedExit, isolatedOutput = run isolated
                  Expect.equal isolatedExit 0 $"explicit isolated restore failed:{Environment.NewLine}{isolatedOutput}"
                  Expect.isTrue
                      (File.Exists(Path.Combine(work, "obj", "project.assets.json")))
                      "the real restore emitted its assets file"
              finally
                  try
                      Directory.Delete(root, true)
                  with _ ->
                      ()
          } ]
