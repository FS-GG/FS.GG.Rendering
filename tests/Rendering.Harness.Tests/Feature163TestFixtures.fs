module Feature163TestFixtures

open System
open System.IO

let createTempRoot (name: string) =
    let root = Path.Combine(Path.GetTempPath(), name + "-" + Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory root |> ignore
    root

let private ensureParentDirectory (path: string) =
    match Path.GetDirectoryName path with
    | null
    | "" -> ()
    | directory -> Directory.CreateDirectory directory |> ignore

let writeFile (root: string) (relativePath: string) (text: string) =
    let path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar))
    ensureParentDirectory path
    File.WriteAllText(path, text)
    path

let writePackageProject (root: string) (relativePath: string) (packageId: string) (version: string) =
    writeFile
        root
        relativePath
        $"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>true</IsPackable>
    <PackageId>{packageId}</PackageId>
    <Version>{version}</Version>
  </PropertyGroup>
</Project>
"""

let writeSampleProject (root: string) (relativePath: string) (packageReferences: (string * string) list) =
    let references =
        packageReferences
        |> List.map (fun (packageId, version) -> $"    <PackageReference Include=\"{packageId}\" Version=\"{version}\" />")
        |> String.concat Environment.NewLine

    writeFile
        root
        relativePath
        $"""<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
{references}
  </ItemGroup>
</Project>
"""

let touch (path: string) =
    ensureParentDirectory path
    File.WriteAllText(path, "")

let deleteTempRoot (root: string) =
    if Directory.Exists root then
        Directory.Delete(root, true)
