namespace FsGg

open System.Diagnostics

[<RequireQualifiedAccess>]
module ApiSurfaceRestore =
    let startInfo workingDirectory project packagesDirectory configPath =
        let psi = ProcessStartInfo("dotnet")
        psi.WorkingDirectory <- workingDirectory
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true
        [ "restore"
          project
          "--packages"
          packagesDirectory
          "--configfile"
          configPath ]
        |> List.iter psi.ArgumentList.Add
        psi
