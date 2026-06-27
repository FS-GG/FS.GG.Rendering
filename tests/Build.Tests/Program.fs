module BuildTestsProgram

open Expecto

[<EntryPoint>]
let main argv =
    Tests.runTestsInAssemblyWithCLIArgs [] argv
