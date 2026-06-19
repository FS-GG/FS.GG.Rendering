module AntShowcase.Tests.Feature167ResponsivenessFixtures

open System.IO
open FS.GG.UI.Scene

let size: Size = { Width = 1024; Height = 768 }

let tempDir () =
    Path.Combine(Path.GetTempPath(), "antshowcase-feature167-" + System.Guid.NewGuid().ToString("N"))
