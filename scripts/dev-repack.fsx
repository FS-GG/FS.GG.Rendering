// Idempotent dev-repack (Feature 175 lesson T1).
//
// One invocation to test a local framework change live in a package-consuming sample:
//   1. pick a FRESH dev version V (or take --version),
//   2. pack the whole solution to the local feed at V (one coherent set — every FS.GG.UI.*
//      package and its inter-package dependencies are V, so there is no mixed-version downgrade),
//   3. retarget the selected sample's pins — Core, App AND Tests — to V,
//   4. restore the sample against the local feed to prove it resolves cleanly.
//
// Why a fresh version every run: the sample's real `dotnet run` / `dotnet test` restores from the
// GLOBAL NuGet cache (`~/.nuget/packages`), keyed by version. Re-packing the SAME version leaves the
// global cache holding the OLD bits, so the change never shows up. A fresh V the global cache has
// never seen forces it to pick up the freshly packed assemblies. `-p:Version=V` over the whole
// solution bumps every package without editing the 14 tracked `src/**/*.fsproj` version lines.
//
// Usage:
//   dotnet fsi scripts/dev-repack.fsx --sample samples/SecondAntShowcase
//   dotnet fsi scripts/dev-repack.fsx --sample samples/SecondAntShowcase --version 0.1.99-dev.1
//   dotnet fsi scripts/dev-repack.fsx --sample samples/SecondAntShowcase --no-restore   # pack+pin only
//
// CROSS-SAMPLE CONSISTENCY RULE (surfaced, never silent): this retargets ONLY the sample(s) you pass.
// Sibling samples keep their old pins and their old feed packages — they still restore, but they do
// NOT see this build. To move a sibling, pass it as another --sample. The script prints which samples
// it left untouched so a partial bump can't quietly break (or stale) a neighbour.

open System
open System.IO
open System.Xml.Linq
open System.Diagnostics

let repoRoot = Directory.GetParent(__SOURCE_DIRECTORY__).FullName

// --- args -------------------------------------------------------------------
let args = fsi.CommandLineArgs |> Array.skip 1

let flagValues flag =
    args
    |> Array.indexed
    |> Array.choose (fun (i, a) ->
        if a = flag && i + 1 < args.Length then Some args.[i + 1] else None)
    |> Array.toList

let flagValue flag = flagValues flag |> List.tryHead
let hasFlag flag = args |> Array.contains flag

let samples = flagValues "--sample"
let feed =
    flagValue "--feed"
    |> Option.defaultValue (Path.Combine(Environment.GetFolderPath Environment.SpecialFolder.UserProfile, ".local", "share", "nuget-local"))
let doRestore = not (hasFlag "--no-restore")

if samples.IsEmpty then
    eprintfn "dev-repack: at least one --sample <path> is required (e.g. --sample samples/SecondAntShowcase)"
    exit 2

// --- target version ---------------------------------------------------------
// Base = the numeric core of an existing packable project; dev suffix = monotonic timestamp so each
// run is a version the global cache has never seen. Exact pins make prerelease ordering irrelevant.
let controlsVersion () =
    let proj = Path.Combine(repoRoot, "src", "Controls", "Controls.fsproj")
    let doc = XDocument.Load proj
    doc.Descendants()
    |> Seq.tryFind (fun e -> e.Name.LocalName = "Version")
    |> Option.map (fun e -> e.Value.Trim())
    |> Option.defaultValue "0.1.0-preview.1"

let numericCore (v: string) =
    match v.Split('-') |> Array.tryHead with
    | Some core when not (String.IsNullOrWhiteSpace core) -> core
    | _ -> "0.1.0"

let version =
    match flagValue "--version" with
    | Some v -> v
    | None -> $"{numericCore (controlsVersion ())}-dev.{DateTime.UtcNow:yyyyMMddHHmmss}"

// --- process helper ---------------------------------------------------------
let run (workingDir: string) (fileName: string) (arguments: string list) =
    let psi = ProcessStartInfo(fileName)
    psi.WorkingDirectory <- workingDir
    psi.UseShellExecute <- false
    arguments |> List.iter psi.ArgumentList.Add
    use proc = Process.Start psi
    proc.WaitForExit()
    proc.ExitCode

let rel (full: string) = Path.GetRelativePath(repoRoot, full)

// --- 1+2: pack whole solution to the feed at V -----------------------------
printfn "dev-repack: version %s  ->  feed %s\n" version feed
Directory.CreateDirectory feed |> ignore

printfn "[1/3] pack FS.GG.Rendering.slnx -c Release -p:Version=%s" version
let packExit =
    run repoRoot "dotnet"
        [ "pack"; "FS.GG.Rendering.slnx"; "-c"; "Release"; "-p:Version=" + version; "--no-restore"; "-o"; feed ]
if packExit <> 0 then
    eprintfn "dev-repack: pack failed (exit %d)" packExit
    exit packExit

// --- 3: retarget the selected samples' FS.GG.UI.* pins to V -----------------
let retargetProject (projectPath: string) =
    let doc = XDocument.Load projectPath
    let mutable changed = false
    for el in doc.Descendants() |> Seq.filter (fun e -> e.Name.LocalName = "PackageReference") do
        let include' =
            el.Attributes()
            |> Seq.tryFind (fun a -> a.Name.LocalName = "Include" || a.Name.LocalName = "Update")
            |> Option.map (fun a -> a.Value.Trim())
        match include' with
        | Some id when id.StartsWith("FS.GG.UI.", StringComparison.Ordinal) ->
            let attr = el.Attributes() |> Seq.tryFind (fun a -> a.Name.LocalName = "Version")
            match attr with
            | Some a when a.Value <> version -> a.Value <- version; changed <- true
            | Some _ -> ()
            | None ->
                let child = el.Elements() |> Seq.tryFind (fun e -> e.Name.LocalName = "Version")
                match child with
                | Some c when c.Value <> version -> c.Value <- version; changed <- true
                | Some _ -> ()
                | None -> el.SetAttributeValue(XName.Get "Version", version); changed <- true
        | _ -> ()
    if changed then doc.Save projectPath
    changed

printfn "\n[2/3] retarget pins -> %s" version
let mutable retargeted = 0
for sample in samples do
    let dir = Path.GetFullPath(Path.Combine(repoRoot, sample))
    if not (Directory.Exists dir) then
        eprintfn "  ! sample not found: %s" sample
    else
        for proj in Directory.GetFiles(dir, "*.fsproj", SearchOption.AllDirectories) do
            if retargetProject proj then
                retargeted <- retargeted + 1
                printfn "  pinned %s" (rel proj)

if retargeted = 0 then printfn "  (no pins changed — already at %s)" version

// --- 4: restore the sample(s) against the feed ------------------------------
let mutable restoreFailures = 0
if doRestore then
    printfn "\n[3/3] restore samples against the local feed"
    for sample in samples do
        let dir = Path.GetFullPath(Path.Combine(repoRoot, sample))
        if Directory.Exists dir then
            for proj in Directory.GetFiles(dir, "*.fsproj", SearchOption.AllDirectories) do
                let exit = run dir "dotnet" [ "restore"; proj ]
                if exit <> 0 then
                    restoreFailures <- restoreFailures + 1
                    eprintfn "  ! restore failed: %s (exit %d)" (rel proj) exit
    if restoreFailures = 0 then printfn "  all sample projects restored cleanly at %s" version
else
    printfn "\n[3/3] restore skipped (--no-restore)"

// --- cross-sample consistency surfacing -------------------------------------
let allSamples =
    let root = Path.Combine(repoRoot, "samples")
    if Directory.Exists root then
        Directory.GetDirectories root
        |> Array.map (fun d -> "samples/" + Path.GetFileName d)
        |> Array.sort
        |> Array.toList
    else []

let touched = samples |> List.map (fun s -> s.TrimEnd('/', '\\')) |> Set.ofList
let untouched = allSamples |> List.filter (fun s -> not (touched.Contains s))
if not untouched.IsEmpty then
    printfn "\nNOTE (cross-sample consistency): left untouched at their existing pins — %s." (String.concat ", " untouched)
    printfn "      They still restore from their old feed packages but do NOT see this %s build." version
    printfn "      Re-run with --sample <path> for any you also want on this build."

printfn "\ndev-repack done: version %s, %d project(s) re-pinned, %d restore failure(s)." version retargeted restoreFailures
exit (if restoreFailures = 0 then 0 else 1)
