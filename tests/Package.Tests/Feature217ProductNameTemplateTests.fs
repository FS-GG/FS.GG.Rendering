module Feature217ProductNameTemplateTests

// Feature 217 — the always-on gate for the `productName` scaffold symbol (validation contract Layer 1).
//
// Deterministic, GL-free, NO `dotnet new`: it re-derives the additive template-contract fact straight
// from .template.config/template.json (productName parameter present + additive; top-level sourceName
// removed; effectiveName coalesce drives the Product rename via replaces+fileRename; projectSlug
// casing source repointed to effectiveName) AND asserts the gitignored validation report that the
// env-gated regenerator (scripts/validate-productname-template.fsx) writes. The heavy live work —
// real `dotnet new --productName` × name-path matrix + byte-diff + `dotnet build` — runs behind
// FS_GG_RUN_PRODUCTNAME_VALIDATION=1, mirroring Feature204LifecycleTemplateTests +
// validate-lifecycle-template.fsx.
//
// Authored failing-first (Principle V): the report is self-provisioned from the validator's env-free
// `--emit-report` verdict-core path (no `dotnet new`/build/GL/network) before the GV gates evaluate,
// so a fresh checkout (gitignored readiness/ absent) is green-by-construction — but only if the
// verdict core PASSES; if the wiring is missing the verdict core throws, no report is written, and the
// gate fails loudly. Until T008..T012 land the `productName`/`effectiveName` wiring, GV-1 (re-derived
// in-test) fails for the right reason.

open System
open System.Diagnostics
open System.IO
open System.Text.Json
open Expecto
open FS.GG.TestSupport

let private repositoryRoot = RepositoryRoot.value

let private repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

let private validationReportPath =
    repositoryPath "specs/217-template-productname-symbol/readiness/productname-template-validation.md"

let private templateJsonPath = repositoryPath ".template.config/template.json"

// ---- self-provisioning (mirrors Feature204) ---------------------------------------------------

let private selfProvisionReport () =
    if not (File.Exists validationReportPath) then
        let psi = ProcessStartInfo("dotnet")
        psi.WorkingDirectory <- repositoryRoot
        psi.UseShellExecute <- false
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true
        [ "fsi"; "scripts/validate-productname-template.fsx"; "--emit-report" ]
        |> List.iter psi.ArgumentList.Add
        match Process.Start psi with
        | null -> ()
        | started ->
            use proc = started
            proc.StandardOutput.ReadToEnd() |> ignore
            proc.StandardError.ReadToEnd() |> ignore
            proc.WaitForExit()

let private reportProvisioned = selfProvisionReport ()

let private readValidationReport () =
    Expect.isTrue
        (File.Exists validationReportPath)
        (sprintf
            "productName validation report missing at %s — regenerate via FS_GG_RUN_PRODUCTNAME_VALIDATION=1 dotnet fsi scripts/validate-productname-template.fsx (or the env-free --emit-report path; it throws if the wiring is missing)"
            validationReportPath)
    File.ReadAllText validationReportPath

// ---- env-free template-contract facts re-derived in-test (failing-first) ----------------------

let private elemStr (e: JsonElement) : string =
    e.GetString() |> Option.ofObj |> Option.defaultValue ""

type private ContractAudit =
    { ProductNamePresent: bool
      ProductNameTextEmptyDefault: bool
      SourceNameRemoved: bool
      EffectiveNameCoalesce: bool
      EffectiveNameRenameDuties: bool
      ProjectSlugSourcesEffectiveName: bool
      Violations: string list }

let private auditContract () =
    use doc = JsonDocument.Parse(File.ReadAllText templateJsonPath)
    let root = doc.RootElement
    let symbols = root.GetProperty("symbols")
    let mutable violations = []
    let note ok msg = if not ok then violations <- msg :: violations
    let tryProp (e: JsonElement) (name: string) =
        match e.TryGetProperty name with
        | true, v -> Some v
        | _ -> None

    let productName = tryProp symbols "productName"
    let productNamePresent = productName.IsSome
    note productNamePresent "symbols.productName missing (FR-001)"
    let productNameTextEmpty =
        match productName with
        | Some pn ->
            elemStr (pn.GetProperty "type") = "parameter"
            && elemStr (pn.GetProperty "datatype") = "text"
            && (match tryProp pn "defaultValue" with Some d -> elemStr d = "" | None -> false)
        | None -> false
    note productNameTextEmpty "productName must be parameter/text with empty defaultValue (FR-004)"

    let sourceNameRemoved = (tryProp root "sourceName").IsNone
    note sourceNameRemoved "top-level sourceName must be removed (single rename driver)"

    let effectiveName = tryProp symbols "effectiveName"
    // effectiveName coalesces the whitespace-trimmed productName (productNameTrimmed) over name;
    // coalesce params are nested under "parameters" (dotnet templating schema). The trim symbol
    // (regex over productName) is the FR-006 whitespace-fallback branch (T012).
    let coalesceOk =
        match effectiveName with
        | Some en ->
            elemStr (en.GetProperty "type") = "generated"
            && elemStr (en.GetProperty "generator") = "coalesce"
            && (match tryProp en "parameters" with
                | Some p ->
                    (match tryProp p "sourceVariableName" with Some s -> elemStr s = "productNameTrimmed" | None -> false)
                    && (match tryProp p "fallbackVariableName" with Some f -> elemStr f = "name" | None -> false)
                | None -> false)
            && (match tryProp symbols "productNameTrimmed" with
                | Some t ->
                    elemStr (t.GetProperty "generator") = "regex"
                    && (match tryProp t "parameters" with
                        | Some tp -> (match tryProp tp "source" with Some s -> elemStr s = "productName" | None -> false)
                        | None -> false)
                | None -> false)
            // effectiveNameLower reproduces sourceName's case-aware lowercase content replace
            // ("product" -> lowercased name), no fileRename. Discovered via the T004 byte-diff oracle.
            && (match tryProp symbols "effectiveNameLower" with
                | Some l ->
                    elemStr (l.GetProperty "generator") = "casing"
                    && (match tryProp l "replaces" with Some r -> elemStr r = "product" | None -> false)
                    && (match l.TryGetProperty "fileRename" with true, _ -> false | _ -> true)
                    && (match tryProp l "parameters" with
                        | Some lp -> (match tryProp lp "source" with Some s -> elemStr s = "effectiveName" | None -> false)
                        | None -> false)
                | None -> false)
        | None -> false
    note coalesceOk "effectiveName coalesce(productNameTrimmed ?? name) + productNameTrimmed regex(productName) + effectiveNameLower casing(replaces product) missing/wrong"
    let renameOk =
        match effectiveName with
        | Some en ->
            (match tryProp en "replaces" with Some r -> elemStr r = "Product" | None -> false)
            && (match tryProp en "fileRename" with Some f -> elemStr f = "Product" | None -> false)
        | None -> false
    note renameOk "effectiveName must carry replaces:\"Product\" + fileRename:\"Product\""

    let slugOk =
        match tryProp symbols "projectSlug" with
        | Some ps ->
            match tryProp ps "parameters" with
            | Some p -> (match tryProp p "source" with Some s -> elemStr s = "effectiveName" | None -> false)
            | None -> false
        | None -> false
    note slugOk "projectSlug.parameters.source must be effectiveName (SC-004)"

    { ProductNamePresent = productNamePresent
      ProductNameTextEmptyDefault = productNameTextEmpty
      SourceNameRemoved = sourceNameRemoved
      EffectiveNameCoalesce = coalesceOk
      EffectiveNameRenameDuties = renameOk
      ProjectSlugSourcesEffectiveName = slugOk
      Violations = List.rev violations }

[<Tests>]
let feature217ProductNameTemplateTests =
    testList
        "Feature217 productName template validation"
        [
          // GV-1 (FR-001..FR-006, env-free verdict-core fact re-derived in-test): the additive
          // productName contract is wired exactly as the data-model specifies. Failing-first until
          // T008..T012 land.
          test "GV-1 template.json wires the additive productName contract" {
              let a = auditContract ()
              Expect.isEmpty a.Violations (sprintf "contract violations: %s" (String.concat "; " a.Violations))
              Expect.isTrue a.ProductNamePresent "productName parameter present"
              Expect.isTrue a.ProductNameTextEmptyDefault "productName is text with empty default"
              Expect.isTrue a.SourceNameRemoved "top-level sourceName removed"
              Expect.isTrue a.EffectiveNameCoalesce "effectiveName coalesce present"
              Expect.isTrue a.EffectiveNameRenameDuties "effectiveName carries Product rename duties"
              Expect.isTrue a.ProjectSlugSourcesEffectiveName "projectSlug sources effectiveName"
          }

          // GV-2: the report records the template-contract fact.
          test "GV-2 report records the template-contract fact" {
              let report = readValidationReport ()
              Expect.stringContains
                  report
                  "template-contract: productName-present=ok sourceName-removed=ok effectiveName-coalesce=ok projectSlug-source=effectiveName"
                  "report records the additive contract"
          }

          // GV-3 (G1/G2, SC-001/FR-007): low-level --productName (no -n) instantiates, named Acme.
          test "GV-3 --productName (no -n) instantiates and is named Acme" {
              let report = readValidationReport ()
              Expect.stringContains report "g1-instantiate: instantiated=ok no-exit-127=ok" "G1 no exit 127"
              Expect.stringContains report "g2-named-acme: named=Acme paths+namespaces+slug=ok" "G2 named Acme"
          }

          // GV-4 (G3/FR-005): productName wins over -n, no half-rename.
          test "GV-4 productName precedence over -n" {
              let report = readValidationReport ()
              Expect.stringContains report "g3-precedence: productName-over-n=Acme no-half-rename=ok" "G3 precedence"
          }

          // GV-5 (G4/FR-006): empty AND whitespace productName fall back to -n/default.
          test "GV-5 empty/whitespace productName falls back" {
              let report = readValidationReport ()
              Expect.stringContains report "g4-fallback: empty=fallback ok whitespace=fallback ok" "G4 fallback"
          }

          // GV-6 (G5/SC-003 + SC-004): backward-compat matrix byte-identical; paths converge.
          test "GV-6 backward-compat matrix and path convergence are zero-diff" {
              let report = readValidationReport ()
              Expect.stringContains report "g5-backward-compat: matrix=M1..M4 diffs=0" "SC-003 zero diff"
              Expect.stringContains report "sc004-convergence: productName-Acme==n-Acme diffs=0" "SC-004 convergence"
          }

          // GV-7 (G6/SC-002): the Acme product builds clean in Release.
          test "GV-7 Acme product builds clean in Release" {
              let report = readValidationReport ()
              Expect.stringContains report "sc002-build: build=Release warn=0 err=0" "SC-002 0 warn / 0 err"
          }

          // GV-8 (Constitution V): provenance disclosed (verdict-core env-free OR live).
          test "GV-8 provenance is disclosed" {
              let report = readValidationReport ()
              Expect.isTrue
                  (report.Contains "provenance: live"
                   || report.Contains "provenance: verdict-core (env-free; full live proof gated behind FS_GG_RUN_PRODUCTNAME_VALIDATION=1)")
                  "report discloses self-provisioned env-free vs live"
          }

          // GV-9 (Principle VI): result: pass only when GV-1..GV-8 hold.
          test "GV-9 overall result is pass" {
              let report = readValidationReport ()
              Expect.stringContains report "result: pass" "validation result is pass"
          }
        ]
