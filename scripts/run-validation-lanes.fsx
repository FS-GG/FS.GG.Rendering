open System
open System.Diagnostics
open System.IO

let repoRoot = Directory.GetParent(__SOURCE_DIRECTORY__).FullName

let psi = ProcessStartInfo("dotnet")
psi.WorkingDirectory <- repoRoot
psi.UseShellExecute <- false
psi.ArgumentList.Add("run")
psi.ArgumentList.Add("--project")
psi.ArgumentList.Add("tests/Rendering.Harness/Rendering.Harness.fsproj")
psi.ArgumentList.Add("--")
psi.ArgumentList.Add("validation-lanes")

for argument in fsi.CommandLineArgs |> Array.skip 1 do
    psi.ArgumentList.Add(argument)

let proc = Process.Start(psi)
proc.WaitForExit()
exit proc.ExitCode
