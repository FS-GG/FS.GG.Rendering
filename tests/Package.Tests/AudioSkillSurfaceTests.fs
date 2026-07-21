module AudioSkillSurfaceTests

// FS.GG.Rendering#160 / FS.GG.Game#20 (ADR-0024 step 4) — originally the fs-gg-audio product-skill
// surface guard.
//
// ADR-0063 (2026-07-21 amendment, FS.GG.Rendering#965) RETIRED the fs-gg-audio SKILL copy from this
// provider — it is now owner-sourced from FS.GG.Game.Skills — so the four checks that scanned the shipped
// SKILL.md body (A-MEMBERS: every `Audio.<member>` it names resolves in the bundled surface; the
// runtime-playback pointer; A-BUNDLE: the body cites the bundled surface path; A-RETIRED: the body opens
// FS.GG.Audio.Core and not the retired Canvas namespace) were removed with it: the body is no longer here
// to scan, and FS.GG.Game's own gate holds it against the canonical.
//
// A-NS REMAINS, because it is NOT about the audio skill at all — it is the general anti-drift rule over
// EVERY bundled surface, and it is the reason this file must stay (PackageTestsGateMembershipTests, #613:
// it holds a rule with no PR-gate twin):
//   A-NS — EVERY bundled `docs/api-surface/<Pkg>/*.fsi` declares a namespace carrying `<Pkg>` as a dotted
//          component. The drift check that would have caught Canvas 0.3.0 (#158) at the source: a doc copy
//          can no longer outlive the package whose name it claims. "Dotted component", not "equals":
//          `Controls/*.fsi` legitimately declare `FS.GG.UI.Controls.Typed`, and `Themes.Default/Theming.fsi`
//          declares `FS.GG.UI.Themes.Default.Theming`.

open System.IO
open System.Text.RegularExpressions
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

let private apiSurfaceRoot = repositoryPath "template/base/docs/api-surface"

/// The first `namespace` declaration in a signature file, if any.
let private declaredNamespace (fsiText: string) =
    let m = Regex.Match(fsiText, @"^namespace\s+(\S+)", RegexOptions.Multiline)
    if m.Success then Some(m.Groups.[1].Value) else None

[<Tests>]
let audioSkillSurfaceTests =
    testList
        "fs-gg-audio skill surface (ADR-0024 step 4)"
        [
          // A-NS — the general anti-drift rule. A bundled doc copy cannot outlive the package it claims:
          // its declared namespace must carry the directory name as a dotted component.
          test "every bundled api-surface .fsi declares a namespace carrying its package directory" {
              let offenders =
                  Directory.GetDirectories apiSurfaceRoot
                  |> Seq.collect (fun dir ->
                      let pkg = DirectoryInfo(dir).Name

                      Directory.GetFiles(dir, "*.fsi")
                      |> Seq.choose (fun file ->
                          let text = File.ReadAllText file

                          match declaredNamespace text with
                          | None -> Some(file, "<no namespace declaration>")
                          | Some ns ->
                              let pattern = sprintf @"(^|\.)%s(\.|$)" (Regex.Escape pkg)
                              if Regex.IsMatch(ns, pattern) then None else Some(file, ns)))
                  |> Seq.toList

              Expect.isEmpty offenders "each bundled <Pkg>/*.fsi declares a namespace containing <Pkg>"
          }
        ]
