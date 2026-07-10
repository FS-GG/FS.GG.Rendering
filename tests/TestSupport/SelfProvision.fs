namespace FS.GG.TestSupport

open System
open System.Collections.Concurrent
open System.Diagnostics
open System.IO

/// Feature 255 — the ONE staleness rule for a self-provisioned validation report.
///
/// Several always-on gates assert a validation report that an env-gated regenerator writes into a
/// gitignored `specs/<feature>/readiness/`. Because the report outlives runs and branch switches, a
/// gate that regenerates it only when ABSENT can assert against a report written for inputs it never
/// saw — the verdict core throws, nothing is rewritten, and the gates stay green (feature 252).
///
/// This module carries the two properties 252 established, once, so each call site's only remaining
/// job is to enumerate its own inputs honestly:
///
///   1. Regenerate when the report is absent OR older than any declared input — and treat a VANISHED
///      input as stale, never as "no evidence of change".
///   2. DELETE the stale report before regenerating. These validators run their verdict core before
///      any write, so they throw and write nothing precisely when the thing they gate is broken.
///      Absent beats stale: the gate then fails loudly instead of asserting against a stale report.
///
/// Applicability: mtime staleness is only meaningful for an artifact that GIT NEVER WRITES. A tracked,
/// committed report is stamped with the checkout time like its inputs, so `report vs input` ordering is
/// arbitrary — and deleting it would destroy committed evidence. Use this only for gitignored, derived
/// reports. (`specs/207-ui-bom-metapackage/readiness/` is un-ignored and committed; it deliberately does
/// NOT use this helper. See Feature207BomConsumerTests.)
module SelfProvision =

    /// The `#load`-ed repo sources of an `.fsi`/`.fsx` script, resolved to absolute paths.
    ///
    /// A validator that `#load`s `src/**/*.fs` COMPILES those sources into its verdict, so they are
    /// verdict inputs as surely as the data files it reads: change the contrast math and the rendered
    /// verdict changes. Deriving the list from the script means the declaration cannot drift out of
    /// step with the `#load` block — the alternative (hand-copying paths into the gate) is precisely
    /// the "four independent ways to get it wrong" this feature exists to remove.
    ///
    /// One level deep: these validators `#load` leaf `.fs` sources, which do not `#load` further.
    let fsiLoadedSources (scriptPath: string) : string list =
        let fullScriptPath = Path.GetFullPath scriptPath

        let scriptDirectory =
            Path.GetDirectoryName fullScriptPath
            |> Option.ofObj
            |> Option.defaultWith (fun () -> failwithf "Script has no containing directory: %s" fullScriptPath)

        File.ReadAllLines scriptPath
        |> Array.choose (fun line ->
            let trimmed = line.TrimStart()

            if trimmed.StartsWith("#load", StringComparison.Ordinal) then
                let openingQuote = trimmed.IndexOf '"'
                let closingQuote = if openingQuote < 0 then -1 else trimmed.IndexOf('"', openingQuote + 1)

                if closingQuote > openingQuote then
                    trimmed.Substring(openingQuote + 1, closingQuote - openingQuote - 1)
                    |> fun relative -> Path.Combine(scriptDirectory, relative.Replace('/', Path.DirectorySeparatorChar))
                    |> Path.GetFullPath
                    |> Some
                else
                    None
            else
                None)
        |> Array.toList

    /// A report is only as trustworthy as its OLDEST input. A vanished input counts as stale so that
    /// regenerating lets the verdict core throw on it loudly, rather than silently dropping it here.
    let private isStale (reportPath: string) (verdictCoreInputs: string list) =
        not (File.Exists reportPath)
        || (let writtenUtc = File.GetLastWriteTimeUtc reportPath

            verdictCoreInputs
            |> List.exists (fun input ->
                not (File.Exists input) || File.GetLastWriteTimeUtc input > writtenUtc))

    let private regenerate (command: string list) =
        let psi = ProcessStartInfo "dotnet"
        psi.WorkingDirectory <- RepositoryRoot.value
        psi.UseShellExecute <- false
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true
        command |> List.iter psi.ArgumentList.Add

        match Process.Start psi with
        | null -> ()
        | started ->
            use proc = started
            // Drained, not asserted: the caller's report-exists gate is what fails loudly. A verdict
            // core that throws writes nothing, and step 2 already removed the stale report.
            proc.StandardOutput.ReadToEnd() |> ignore
            proc.StandardError.ReadToEnd() |> ignore
            proc.WaitForExit()

    /// Memoised per report path. Two gates can derive from one report (Feature204 and Feature219 both
    /// assert the lifecycle report), and both run this at module initialization; regenerating once
    /// keeps the delete-then-write window atomic no matter which module initializes first.
    ///
    /// The declaration is memoised alongside the work so that two gates sharing a report cannot quietly
    /// disagree about its inputs: whoever initialized first would otherwise decide the staleness rule
    /// for both, and the other's declaration would be dead text. Disagreement fails loudly instead.
    let private provisioned =
        ConcurrentDictionary<string, (string list * string list) * Lazy<unit>>()

    /// Ensure `reportPath` is newer than every input the regenerator's verdict core reads, by running
    /// `dotnet <command>` from the repository root. `verdictCoreInputs` and `reportPath` are absolute.
    ///
    /// Call sites bind this at module level (`let private reportProvisioned = ensureFresh ...`) so it
    /// runs before the `[<Tests>]` value is evaluated.
    let ensureFresh (reportPath: string) (verdictCoreInputs: string list) (command: string list) : unit =
        if List.isEmpty verdictCoreInputs then
            invalidArg
                (nameof verdictCoreInputs)
                "declare every file the verdict core reads; an empty list would make the report permanently fresh"

        let key = Path.GetFullPath reportPath
        // Order-insensitive: staleness is "newer than the OLDEST input", so only the set matters.
        let declaration = (verdictCoreInputs |> List.map Path.GetFullPath |> List.sort), command

        let declared, work =
            provisioned.GetOrAdd(
                key,
                fun _ ->
                    declaration,
                    lazy
                        (if isStale key verdictCoreInputs then
                             if File.Exists key then
                                 File.Delete key

                             regenerate command)
            )

        if declared <> declaration then
            failwithf
                "Two gates declare different verdict-core inputs for the same report %s.\n  first: %A\n  this:  %A\nThey derive from one artifact, so they must agree on what makes it stale."
                key
                declared
                declaration

        work.Force()
