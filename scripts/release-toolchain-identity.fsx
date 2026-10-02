// Executed by the SDK selected in the release job; records the actually loaded F# core.
open System
open System.Text.Json
let core = typeof<Microsoft.FSharp.Core.Unit>.Assembly
printfn "%s" (JsonSerializer.Serialize({| fsharpCorePath = core.Location; fsharpCoreVersion = core.GetName().Version.ToString(); runtimeVersion = Environment.Version.ToString() |}))
