#!/usr/bin/env dotnet fsi

open System
open System.Diagnostics

let args = fsi.CommandLineArgs |> Array.skip 1 |> Array.toList

let psi = ProcessStartInfo()
psi.FileName <- "dotnet"
psi.ArgumentList.Add("run")
psi.ArgumentList.Add("--project")
psi.ArgumentList.Add("tests/Rendering.Harness/Rendering.Harness.fsproj")
psi.ArgumentList.Add("--")
psi.ArgumentList.Add("skill-parity")

for arg in args do
    psi.ArgumentList.Add(arg)

psi.UseShellExecute <- false
psi.RedirectStandardOutput <- true
psi.RedirectStandardError <- true

match Process.Start psi with
| null -> exit 3
| proc ->
    proc.OutputDataReceived.Add(fun eventArgs ->
        if not (isNull eventArgs.Data) then
            stdout.WriteLine eventArgs.Data)
    proc.ErrorDataReceived.Add(fun eventArgs ->
        if not (isNull eventArgs.Data) then
            stderr.WriteLine eventArgs.Data)
    proc.BeginOutputReadLine()
    proc.BeginErrorReadLine()
    proc.WaitForExit()
    let exitCode = proc.ExitCode
    proc.Dispose()
    exit exitCode
