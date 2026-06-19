namespace Rendering.Harness

open System
open System.Diagnostics
open System.Globalization
open System.IO
open System.Text
open System.Text.Json
open System.Xml.Linq

module PackageFeed =

    type PackagePinStatus =
        | Current
        | Stale
        | MissingExpectedPackage
        | CompatibilityException
        | NotSelected

    type ProofStatus =
        | Passed
        | Failed
        | EnvironmentLimited

    type PackageFeedMode =
        | Check
        | Refresh
        | Proof

    type PackablePackage =
        { PackageId: string
          Version: string
          ProjectPath: string
          IsPackable: bool
          PackageFilePath: string }

    type CompatibilityException =
        { Id: string
          PackageId: string
          DeclaredVersion: string
          ExpectedVersion: string
          SamplePath: string
          Reason: string
          Owner: string
          Review: string }

    type PackagePin =
        { PackageId: string
          DeclaredVersion: string
          ExpectedVersion: string option
          ProjectFilePath: string
          Status: PackagePinStatus
          CompatibilityExceptionId: string option }

    type FeedPackageStatus =
        { PackageId: string
          Version: string
          PackageFilePath: string
          Present: bool }

    type SourceRule =
        { RuleId: string
          PackagePattern: string
          AllowedSources: string list }

    type SourceProof =
        { Status: ProofStatus
          FeedPath: string
          CachePath: string
          GlobalCacheCleared: bool
          SelectedSamples: string list
          SourceRules: SourceRule list
          RestoreCommand: string option
          RestoreLogPath: string option
          AssetsFiles: string list
          Violations: string list }

    type PackageFeedOptions =
        { RepositoryRoot: string
          SelectedSamples: string list
          FeedPath: string
          OutDir: string
          Mode: PackageFeedMode
          PackBeforeCheck: bool
          IsolatedCachePath: string option
          Cold: bool
          ClearGlobalCache: bool
          AllowedExceptionIds: Set<string>
          CompatibilityExceptions: CompatibilityException list }

    type PackageFeedResult =
        { Status: ProofStatus
          CurrentPackages: PackablePackage list
          PackagePins: PackagePin list
          FeedPackages: FeedPackageStatus list
          ChangedFiles: string list
          SourceProof: SourceProof option
          EvidenceFiles: string list
          Diagnostics: string list }

    type Model =
        { RepositoryRoot: string
          SelectedSamples: string list
          FeedPath: string
          CurrentPackages: PackablePackage list
          PackagePins: PackagePin list
          FeedPackages: FeedPackageStatus list
          Proof: SourceProof option
          Status: ProofStatus option
          Diagnostics: string list }

    type Msg =
        | DiscoverPackagesRequested
        | PackagesDiscovered of PackablePackage list
        | SamplePinsRead of PackagePin list
        | LocalFeedChecked of FeedPackageStatus list
        | PinsRefreshRequested
        | PinsRefreshed of changedFiles: string list
        | SourceProofRequested
        | SourceProofClassified of SourceProof
        | EvidenceWritten of paths: string list
        | WorkflowFailed of reason: string

    type Effect =
        | ReadProjectFiles
        | ReadSampleProjects
        | PackLocalFeed
        | WriteSamplePins
        | CheckLocalFeed
        | CreateGeneratedNuGetConfig
        | RunRestore
        | ReadRestoreAssets
        | WritePackageEvidence

    let defaultFeedPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "nuget-local")

    let statusToken status =
        match status with
        | Current -> "current"
        | Stale -> "stale"
        | MissingExpectedPackage -> "missing-expected-package"
        | CompatibilityException -> "compatibility-exception"
        | NotSelected -> "not-selected"

    let proofStatusToken status =
        match status with
        | Passed -> "passed"
        | Failed -> "failed"
        | EnvironmentLimited -> "environment-limited"

    let modeToken mode =
        match mode with
        | Check -> "check"
        | Refresh -> "refresh"
        | Proof -> "proof"

    let tryParseMode token =
        match token with
        | "check" -> Some Check
        | "refresh" -> Some Refresh
        | "proof" -> Some Proof
        | _ -> None

    let init options =
        let effects =
            [ if options.PackBeforeCheck then PackLocalFeed
              ReadProjectFiles
              ReadSampleProjects
              CheckLocalFeed
              if options.Mode = Refresh then WriteSamplePins
              if options.Mode = Proof then
                  CreateGeneratedNuGetConfig
                  RunRestore
                  ReadRestoreAssets
              WritePackageEvidence ]

        { RepositoryRoot = options.RepositoryRoot
          SelectedSamples = options.SelectedSamples
          FeedPath = options.FeedPath
          CurrentPackages = []
          PackagePins = []
          FeedPackages = []
          Proof = None
          Status = None
          Diagnostics = [] },
        effects

    let update (msg: Msg) (model: Model) =
        match msg with
        | DiscoverPackagesRequested -> model, [ ReadProjectFiles ]
        | PackagesDiscovered packages -> { model with CurrentPackages = packages }, [ ReadSampleProjects; CheckLocalFeed ]
        | SamplePinsRead pins -> { model with PackagePins = pins }, []
        | LocalFeedChecked feed -> { model with FeedPackages = feed }, []
        | PinsRefreshRequested -> model, [ WriteSamplePins ]
        | PinsRefreshed changed -> { model with Diagnostics = model.Diagnostics @ changed }, [ WritePackageEvidence ]
        | SourceProofRequested -> model, [ CreateGeneratedNuGetConfig; RunRestore; ReadRestoreAssets ]
        | SourceProofClassified proof -> { model with Proof = Some proof; Status = Some proof.Status }, [ WritePackageEvidence ]
        | EvidenceWritten paths -> { model with Diagnostics = model.Diagnostics @ paths }, []
        | WorkflowFailed reason -> { model with Status = Some (Failed: ProofStatus); Diagnostics = model.Diagnostics @ [ reason ] }, [ WritePackageEvidence ]

    let absolutePath (root: string) (path: string) =
        if String.IsNullOrWhiteSpace path then
            path
        elif Path.IsPathRooted path then
            Path.GetFullPath path
        else
            Path.GetFullPath(Path.Combine(root, path))

    let relativePath (root: string) (path: string) =
        try
            Path.GetRelativePath(root, path).Replace('\\', '/')
        with _ ->
            path.Replace('\\', '/')

    let ensureParentDirectory (path: string) =
        match Path.GetDirectoryName path with
        | null
        | "" -> ()
        | directory -> Directory.CreateDirectory directory |> ignore

    let xmlValue (doc: XDocument) (name: string) =
        doc.Descendants()
        |> Seq.tryFind (fun e -> e.Name.LocalName = name)
        |> Option.map (fun e -> e.Value.Trim())

    let xmlBool (value: string option) =
        match value with
        | Some v -> String.Equals(v, "true", StringComparison.OrdinalIgnoreCase)
        | None -> false

    let discoverPackablePackages (repositoryRoot: string) (feedPath: string) : PackablePackage list =
        let src = Path.Combine(repositoryRoot, "src")

        if not (Directory.Exists src) then
            []
        else
            Directory.GetFiles(src, "*.fsproj", SearchOption.AllDirectories)
            |> Array.choose (fun projectPath ->
                try
                    let doc = XDocument.Load projectPath
                    let packageId = xmlValue doc "PackageId"
                    let version = xmlValue doc "Version"
                    let isPackable = xmlBool (xmlValue doc "IsPackable")

                    match packageId, version with
                    | Some id, Some ver when id.StartsWith("FS.GG.UI.", StringComparison.Ordinal) && isPackable ->
                        let package: PackablePackage =
                            { PackageId = id
                              Version = ver
                              ProjectPath = relativePath repositoryRoot projectPath
                              IsPackable = true
                              PackageFilePath = Path.Combine(feedPath, $"{id}.{ver}.nupkg") }

                        Some package
                    | _ -> None
                with _ ->
                    None)
            |> Array.sortBy _.PackageId
            |> Array.toList

    let projectFilesForSample (repositoryRoot: string) (sample: string) : string list =
        let path = absolutePath repositoryRoot sample

        if File.Exists path && path.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase) then
            [ path ]
        elif Directory.Exists path then
            Directory.GetFiles(path, "*.fsproj", SearchOption.AllDirectories) |> Array.toList
        else
            []

    let versionFromPackageReference (element: XElement) =
        let attr =
            element.Attributes()
            |> Seq.tryFind (fun a -> a.Name.LocalName = "Version")
            |> Option.map _.Value

        match attr with
        | Some value when not (String.IsNullOrWhiteSpace value) -> value.Trim()
        | _ ->
            element.Elements()
            |> Seq.tryFind (fun e -> e.Name.LocalName = "Version")
            |> Option.map (fun e -> e.Value.Trim())
            |> Option.defaultValue ""

    let includeFromPackageReference (element: XElement) =
        element.Attributes()
        |> Seq.tryFind (fun a -> a.Name.LocalName = "Include" || a.Name.LocalName = "Update")
        |> Option.map (fun a -> a.Value.Trim())

    let compatibilityMatch
        (allowedIds: Set<string>)
        (exceptions: CompatibilityException list)
        (packageId: string)
        (declaredVersion: string)
        (expectedVersion: string)
        (projectPath: string)
        : CompatibilityException option =
        exceptions
        |> List.tryFind (fun ex ->
            allowedIds.Contains ex.Id
            && String.Equals(ex.PackageId, packageId, StringComparison.Ordinal)
            && String.Equals(ex.DeclaredVersion, declaredVersion, StringComparison.Ordinal)
            && String.Equals(ex.ExpectedVersion, expectedVersion, StringComparison.Ordinal)
            && (String.IsNullOrWhiteSpace ex.SamplePath
                || projectPath.EndsWith(ex.SamplePath.Replace('/', Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)))

    let classifyPackagePins
        (currentPackages: PackablePackage list)
        (allowedExceptionIds: Set<string>)
        (compatibilityExceptions: CompatibilityException list)
        (pins: PackagePin list)
        : PackagePin list =
        let expected =
            currentPackages
            |> List.map (fun package -> package.PackageId, package.Version)
            |> Map.ofList

        pins
        |> List.map (fun pin ->
            match expected |> Map.tryFind pin.PackageId with
            | None ->
                { pin with
                    ExpectedVersion = None
                    Status = MissingExpectedPackage
                    CompatibilityExceptionId = None }
            | Some version ->
                match compatibilityMatch allowedExceptionIds compatibilityExceptions pin.PackageId pin.DeclaredVersion version pin.ProjectFilePath with
                | Some ex ->
                    { pin with
                        ExpectedVersion = Some version
                        Status = CompatibilityException
                        CompatibilityExceptionId = Some ex.Id }
                | None when String.Equals(pin.DeclaredVersion, version, StringComparison.Ordinal) ->
                    { pin with
                        ExpectedVersion = Some version
                        Status = Current
                        CompatibilityExceptionId = None }
                | None ->
                    { pin with
                        ExpectedVersion = Some version
                        Status = Stale
                        CompatibilityExceptionId = None })

    let readSelectedPackagePins
        (repositoryRoot: string)
        (selectedSamples: string list)
        (currentPackages: PackablePackage list)
        (allowedExceptionIds: Set<string>)
        (compatibilityExceptions: CompatibilityException list)
        : PackagePin list =
        selectedSamples
        |> List.collect (projectFilesForSample repositoryRoot)
        |> List.collect (fun projectPath ->
            try
                let doc = XDocument.Load projectPath

                doc.Descendants()
                |> Seq.filter (fun e -> e.Name.LocalName = "PackageReference")
                |> Seq.choose (fun reference ->
                    match includeFromPackageReference reference with
                    | Some id when id.StartsWith("FS.GG.UI.", StringComparison.Ordinal) ->
                        let pin: PackagePin =
                            { PackageId = id
                              DeclaredVersion = versionFromPackageReference reference
                              ExpectedVersion = None
                              ProjectFilePath = projectPath
                              Status = NotSelected
                              CompatibilityExceptionId = None }

                        Some pin
                    | _ -> None)
                |> Seq.toList
            with _ ->
                [])
        |> classifyPackagePins currentPackages allowedExceptionIds compatibilityExceptions

    let checkLocalFeed (currentPackages: PackablePackage list) : FeedPackageStatus list =
        currentPackages
        |> List.map (fun package ->
            { PackageId = package.PackageId
              Version = package.Version
              PackageFilePath = package.PackageFilePath
              Present = File.Exists package.PackageFilePath })

    let refreshSamplePins (pins: PackagePin list) : string list =
        pins
        |> List.filter (fun pin -> pin.Status = Stale && pin.ExpectedVersion.IsSome)
        |> List.groupBy _.ProjectFilePath
        |> List.choose (fun (projectPath, filePins) ->
            let fullPath = Path.GetFullPath projectPath

            if not (File.Exists fullPath) then
                None
            else
                let doc = XDocument.Load fullPath
                let expected = filePins |> List.map (fun pin -> pin.PackageId, pin.ExpectedVersion.Value) |> Map.ofList
                let mutable changed = false

                for reference in doc.Descendants() |> Seq.filter (fun e -> e.Name.LocalName = "PackageReference") do
                    match includeFromPackageReference reference with
                    | Some packageId when expected.ContainsKey packageId ->
                        let version = expected[packageId]
                        let attr =
                            reference.Attributes()
                            |> Seq.tryFind (fun a -> a.Name.LocalName = "Version")

                        match attr with
                        | Some a ->
                            if a.Value <> version then
                                a.Value <- version
                                changed <- true
                        | None ->
                            match reference.Elements() |> Seq.tryFind (fun e -> e.Name.LocalName = "Version") with
                            | Some element ->
                                if element.Value <> version then
                                    element.Value <- version
                                    changed <- true
                            | None ->
                                reference.Add(XElement(XName.Get "Version", version))
                                changed <- true
                    | _ -> ()

                if changed then
                    doc.Save fullPath
                    Some projectPath
                else
                    None)

    let generatedSourceRules feedPath =
        [ { RuleId = "nuget-local"
            PackagePattern = "FS.GG.UI.*"
            AllowedSources = [ feedPath ] }
          { RuleId = "nuget.org"
            PackagePattern = "*"
            AllowedSources = [ "https://api.nuget.org/v3/index.json" ] } ]

    let writeGeneratedNuGetConfig (path: string) (feedPath: string) =
        ensureParentDirectory path
        let fullFeedPath = Path.GetFullPath feedPath
        let rules = generatedSourceRules fullFeedPath

        let doc =
            XDocument(
                XElement(
                    XName.Get "configuration",
                    XElement(
                        XName.Get "packageSources",
                        XElement(XName.Get "clear"),
                        XElement(XName.Get "add", XAttribute(XName.Get "key", "nuget-local"), XAttribute(XName.Get "value", fullFeedPath)),
                        XElement(XName.Get "add", XAttribute(XName.Get "key", "nuget.org"), XAttribute(XName.Get "value", "https://api.nuget.org/v3/index.json"))),
                    XElement(
                        XName.Get "packageSourceMapping",
                        XElement(
                            XName.Get "packageSource",
                            XAttribute(XName.Get "key", "nuget-local"),
                            XElement(XName.Get "package", XAttribute(XName.Get "pattern", "FS.GG.UI.*"))),
                        XElement(
                            XName.Get "packageSource",
                            XAttribute(XName.Get "key", "nuget.org"),
                            XElement(XName.Get "package", XAttribute(XName.Get "pattern", "*"))))))

        doc.Save path
        rules

    let quoteArg (arg: string) =
        if arg.Contains(' ') then "\"" + arg.Replace("\"", "\\\"") + "\"" else arg

    let runProcess (repositoryRoot: string) (fileName: string) (arguments: string list) (timeout: TimeSpan) =
        let psi = ProcessStartInfo(fileName)
        psi.WorkingDirectory <- repositoryRoot
        psi.UseShellExecute <- false
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true

        for argument in arguments do
            psi.ArgumentList.Add argument

        use proc = new Process()
        let output = StringBuilder()
        proc.StartInfo <- psi
        proc.OutputDataReceived.Add(fun args -> if not (isNull args.Data) then lock output (fun () -> output.AppendLine(args.Data) |> ignore))
        proc.ErrorDataReceived.Add(fun args -> if not (isNull args.Data) then lock output (fun () -> output.AppendLine(args.Data) |> ignore))
        let started = DateTime.UtcNow
        let commandText = String.concat " " (fileName :: (arguments |> List.map quoteArg))

        try
            if not (proc.Start()) then
                3, commandText, "process did not start"
            else
                proc.BeginOutputReadLine()
                proc.BeginErrorReadLine()

                let rec wait () =
                    if proc.WaitForExit(200) then
                        proc.WaitForExit()
                        proc.ExitCode, commandText, lock output (fun () -> output.ToString())
                    elif DateTime.UtcNow - started > timeout then
                        try
                            proc.Kill(true)
                        with _ ->
                            ()

                        124, commandText, lock output (fun () -> output.ToString() + Environment.NewLine + "Timed out.")
                    else
                        wait ()

                wait ()
        with ex ->
            3, commandText, ex.Message

    let writeLines (path: string) (lines: string list) =
        ensureParentDirectory path
        File.WriteAllText(path, String.concat Environment.NewLine lines + Environment.NewLine)

    let writePackageVersions (outDir: string) (packages: PackablePackage list) =
        let path = Path.Combine(outDir, "package-versions.md")
        let lines =
            [ "# Package Versions"
              ""
              "| Package | Version | Project | Feed package |"
              "|---------|---------|---------|--------------|" ]
            @ (packages
               |> List.map (fun package ->
                   $"| `{package.PackageId}` | `{package.Version}` | `{package.ProjectPath}` | `{package.PackageFilePath}` |"))

        writeLines path lines
        path

    let writePackagePins (outDir: string) (pins: PackagePin list) =
        let path = Path.Combine(outDir, "package-pins.md")
        let lines =
            [ "# Package Pins"
              ""
              "| Package | Declared | Expected | Status | Project | Exception |"
              "|---------|----------|----------|--------|---------|-----------|" ]
            @ (pins
               |> List.map (fun pin ->
                   let expected = pin.ExpectedVersion |> Option.defaultValue "(missing)"
                   let ex = pin.CompatibilityExceptionId |> Option.defaultValue ""
                   $"| `{pin.PackageId}` | `{pin.DeclaredVersion}` | `{expected}` | `{statusToken pin.Status}` | `{pin.ProjectFilePath}` | `{ex}` |"))

        writeLines path lines
        path

    let jsonEscape (text: string) =
        JsonSerializer.Serialize(text)

    let writeSourceProof (outDir: string) (proof: SourceProof) (pins: PackagePin list) (packages: PackablePackage list) =
        let markdown = Path.Combine(outDir, "source-proof.md")
        let json = Path.Combine(outDir, "source-proof.json")
        let restoreCommand = proof.RestoreCommand |> Option.defaultValue "not-run"
        let restoreLogPath = proof.RestoreLogPath |> Option.defaultValue "not-written"
        let selectedSamples = String.concat ", " proof.SelectedSamples

        let lines =
            [ "# Package Source Proof"
              ""
              $"- Status: `{proofStatusToken proof.Status}`"
              $"- Local feed: `{proof.FeedPath}`"
              $"- Package cache: `{proof.CachePath}`"
              $"- Global cache cleared: `{proof.GlobalCacheCleared.ToString().ToLowerInvariant()}`"
              $"- Selected samples: `{selectedSamples}`"
              $"- Restore command: `{restoreCommand}`"
              $"- Restore log: `{restoreLogPath}`"
              ""
              "## Source Rules"
              "" ]
            @ (proof.SourceRules
               |> List.map (fun rule ->
                   let sources = String.concat ", " rule.AllowedSources
                   $"- `{rule.PackagePattern}` -> `{sources}`"))
            @ [ ""
                "## Violations"
                "" ]
            @ (if proof.Violations.IsEmpty then [ "- None." ] else proof.Violations |> List.map (fun v -> "- " + v))

        writeLines markdown lines

        let pinJson =
            pins
            |> List.map (fun pin ->
                "{" +
                String.concat
                    ","
                    [ "\"packageId\":" + jsonEscape pin.PackageId
                      "\"declaredVersion\":" + jsonEscape pin.DeclaredVersion
                      "\"expectedVersion\":" + jsonEscape (pin.ExpectedVersion |> Option.defaultValue "")
                      "\"projectFilePath\":" + jsonEscape pin.ProjectFilePath
                      "\"status\":" + jsonEscape (statusToken pin.Status) ]
                + "}")
            |> String.concat ","

        let packageJson =
            packages
            |> List.map (fun package ->
                "{" +
                String.concat
                    ","
                    [ "\"packageId\":" + jsonEscape package.PackageId
                      "\"version\":" + jsonEscape package.Version
                      "\"projectPath\":" + jsonEscape package.ProjectPath ]
                + "}")
            |> String.concat ","

        let rulesJson =
            proof.SourceRules
            |> List.map (fun rule ->
                let sources = rule.AllowedSources |> List.map jsonEscape |> String.concat ","
                $"{{\"ruleId\":{jsonEscape rule.RuleId},\"packagePattern\":{jsonEscape rule.PackagePattern},\"allowedSources\":[{sources}]}}")
            |> String.concat ","

        let assetsJson = proof.AssetsFiles |> List.map jsonEscape |> String.concat ","
        let violationsJson = proof.Violations |> List.map jsonEscape |> String.concat ","
        let selectedSamplesJson = proof.SelectedSamples |> List.map jsonEscape |> String.concat ","

        let jsonText =
            "{"
            + String.concat
                ","
                [ "\"status\":" + jsonEscape (proofStatusToken proof.Status)
                  "\"feedPath\":" + jsonEscape proof.FeedPath
                  "\"cachePath\":" + jsonEscape proof.CachePath
                  "\"globalCacheCleared\":" + proof.GlobalCacheCleared.ToString().ToLowerInvariant()
                  "\"selectedSamples\":[" + selectedSamplesJson + "]"
                  "\"currentPackages\":[" + packageJson + "]"
                  "\"packagePins\":[" + pinJson + "]"
                  "\"sourceRules\":[" + rulesJson + "]"
                  "\"resolvedPackages\":[" + pinJson + "]"
                  "\"violations\":[" + violationsJson + "]"
                  "\"restoreCommand\":" + jsonEscape (proof.RestoreCommand |> Option.defaultValue "")
                  "\"restoreLog\":" + jsonEscape (proof.RestoreLogPath |> Option.defaultValue "")
                  "\"assetsFiles\":[" + assetsJson + "]"
                  "\"generatedAtUtc\":" + jsonEscape (DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)) ]
            + "}"

        File.WriteAllText(json, jsonText + Environment.NewLine)
        [ markdown; json ]

    let copyAssets (repositoryRoot: string) (outDir: string) (projectFiles: string list) =
        let assetsDir = Path.Combine(outDir, "assets")
        Directory.CreateDirectory assetsDir |> ignore

        projectFiles
        |> List.choose (fun project ->
            let projectDirectory =
                match Path.GetDirectoryName project with
                | null -> ""
                | directory -> directory

            let objAssets = Path.Combine(projectDirectory, "obj", "project.assets.json")

            if File.Exists objAssets then
                let projectName =
                    match Path.GetFileNameWithoutExtension project with
                    | null -> "project"
                    | value -> value

                let name = projectName.Replace(" ", "-").ToLowerInvariant() + "-project.assets.json"

                let target = Path.Combine(assetsDir, name)
                File.Copy(objAssets, target, true)
                Some(relativePath repositoryRoot target)
            else
                None)

    let runSourceProof
        (options: PackageFeedOptions)
        (pins: PackagePin list)
        (packages: PackablePackage list)
        (feedStatuses: FeedPackageStatus list)
        : SourceProof =
        let outDir = options.OutDir
        let cachePath =
            options.IsolatedCachePath
            |> Option.defaultValue (Path.Combine(outDir, "nuget-cache"))
            |> absolutePath options.RepositoryRoot

        let configPath = Path.Combine(outDir, "source-rules.nuget.config")
        let restoreLog = Path.Combine(outDir, "restore.log")
        let sourceRules = writeGeneratedNuGetConfig configPath options.FeedPath
        let initial: SourceProof =
            { Status = Failed
              FeedPath = Path.GetFullPath options.FeedPath
              CachePath = cachePath
              GlobalCacheCleared = false
              SelectedSamples = options.SelectedSamples
              SourceRules = sourceRules
              RestoreCommand = None
              RestoreLogPath = Some restoreLog
              AssetsFiles = []
              Violations = [] }

        let pinViolations =
            pins
            |> List.choose (fun pin ->
                match pin.Status with
                | Current
                | CompatibilityException -> None
                | Stale ->
                    let expectedVersion = pin.ExpectedVersion |> Option.defaultValue "(missing)"
                    Some $"stale-pin: {pin.PackageId} expected {expectedVersion} actual {pin.DeclaredVersion} in {pin.ProjectFilePath}"
                | MissingExpectedPackage -> Some $"missing-expected-package: {pin.PackageId} in {pin.ProjectFilePath}"
                | NotSelected -> Some $"not-selected: {pin.PackageId} in {pin.ProjectFilePath}")

        let feedViolations =
            feedStatuses
            |> List.choose (fun status ->
                if status.Present then None
                else Some $"missing-local-package: {status.PackageId} {status.Version} at {status.PackageFilePath}")

        if options.SelectedSamples.IsEmpty then
            { initial with Violations = [ "no-selected-samples: no package-consuming samples were selected" ] }
        elif pins.IsEmpty then
            { initial with Violations = [ "no-package-pins: selected samples have no FS.GG.UI.* package references" ] }
        elif options.ClearGlobalCache && not options.Cold then
            { initial with Violations = [ "cache-policy-violation: --clear-global-cache requires --cold" ] }
        elif not pinViolations.IsEmpty || not feedViolations.IsEmpty then
            { initial with Violations = pinViolations @ feedViolations }
        else
            Directory.CreateDirectory cachePath |> ignore
            let projectFiles =
                options.SelectedSamples
                |> List.collect (projectFilesForSample options.RepositoryRoot)
                |> List.filter (fun project -> pins |> List.exists (fun pin -> Path.GetFullPath(Path.Combine(options.RepositoryRoot, pin.ProjectFilePath)) = project))

            let restoreResults =
                projectFiles
                |> List.map (fun project ->
                    let args =
                        [ "restore"
                          project
                          "--configfile"
                          configPath
                          "--packages"
                          cachePath ]

                    runProcess options.RepositoryRoot "dotnet" args (TimeSpan.FromMinutes 5.0))

            let restoreText =
                restoreResults
                |> List.map (fun (_, command, output) -> command + Environment.NewLine + output)
                |> String.concat Environment.NewLine

            writeLines restoreLog [ restoreText ]

            let failures =
                restoreResults
                |> List.choose (fun (exitCode, command, _) ->
                    if exitCode = 0 then None else Some $"restore-failed: `{command}` exit {exitCode}")

            let assets = copyAssets options.RepositoryRoot outDir projectFiles
            let status = if failures.IsEmpty then Passed else Failed

            { initial with
                Status = status
                GlobalCacheCleared = options.ClearGlobalCache
                RestoreCommand = restoreResults |> List.tryHead |> Option.map (fun (_, command, _) -> command)
                AssetsFiles = assets
                Violations = failures }

    let resultStatus (mode: PackageFeedMode) (pins: PackagePin list) (proof: SourceProof option) : ProofStatus =
        let pinFailure =
            pins
            |> List.exists (fun pin ->
                match pin.Status with
                | Current
                | CompatibilityException -> false
                | Stale
                | MissingExpectedPackage
                | NotSelected -> true)

        match mode, proof with
        | Proof, Some p -> p.Status
        | _ when pinFailure -> Failed
        | _ -> Passed

    let runPackIfRequested (options: PackageFeedOptions) (diagnostics: string list) =
        if not options.PackBeforeCheck then
            diagnostics
        else
            Directory.CreateDirectory options.FeedPath |> ignore
            let exitCode, command, output =
                runProcess
                    options.RepositoryRoot
                    "dotnet"
                    [ "pack"; "FS.GG.Rendering.slnx"; "-c"; "Release"; "--no-restore"; "-o"; options.FeedPath ]
                    (TimeSpan.FromMinutes 10.0)

            let line = $"{command} -> exit {exitCode}"
            if exitCode = 0 then diagnostics @ [ line ] else diagnostics @ [ line; output ]

    let runWorkflow (options: PackageFeedOptions) : PackageFeedResult =
        Directory.CreateDirectory options.OutDir |> ignore
        let diagnostics = runPackIfRequested options []
        let packages = discoverPackablePackages options.RepositoryRoot options.FeedPath
        let initialPins =
            readSelectedPackagePins
                options.RepositoryRoot
                options.SelectedSamples
                packages
                options.AllowedExceptionIds
                options.CompatibilityExceptions

        let changedFiles =
            if options.Mode = Refresh then
                refreshSamplePins initialPins
            else
                []

        let pins =
            if options.Mode = Refresh && not changedFiles.IsEmpty then
                readSelectedPackagePins
                    options.RepositoryRoot
                    options.SelectedSamples
                    packages
                    options.AllowedExceptionIds
                    options.CompatibilityExceptions
            else
                initialPins

        let feedStatuses = checkLocalFeed packages
        let versionEvidence = writePackageVersions options.OutDir packages
        let pinEvidence = writePackagePins options.OutDir pins

        let proof =
            if options.Mode = Proof then
                Some(runSourceProof options pins packages feedStatuses)
            else
                None

        let proofFiles =
            match proof with
            | Some p -> writeSourceProof options.OutDir p pins packages
            | None -> []

        let status = resultStatus options.Mode pins proof

        { Status = status
          CurrentPackages = packages
          PackagePins = pins
          FeedPackages = feedStatuses
          ChangedFiles = changedFiles
          SourceProof = proof
          EvidenceFiles = [ versionEvidence; pinEvidence ] @ proofFiles
          Diagnostics = diagnostics }
