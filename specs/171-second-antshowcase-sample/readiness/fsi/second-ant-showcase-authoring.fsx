#I "../../../../samples/SecondAntShowcase/SecondAntShowcase.App/bin/Release/net10.0"
#r "SecondAntShowcase.Core.dll"

open SecondAntShowcase.Core
open SecondAntShowcase.Core.Model

let coverage = CoverageMap.check ()
if not (CoverageMap.isClean coverage) then
    failwithf "coverage drift: %A" coverage

let interactionCoverage = InteractionContracts.coverage ()
if not (InteractionContracts.isClean interactionCoverage) then
    failwithf "interaction contract drift: %A" interactionCoverage.MissingContractOrReason

let model =
    { Host.initModel with
        CurrentPage = "tpl-form"
        PageState = Model.updatePage (FormFieldChanged("Email", "review@example.com")) Host.initModel.PageState }

let switched = Model.update ToggleMode model
if switched.CurrentPage <> "tpl-form" then
    failwith "theme switch changed current page"

let finding =
    ReviewFindings.create
        "FSI-001"
        [ "tpl-form-antLight-preferred" ]
        ReviewFindings.Alignment
        ReviewFindings.Warning
        "FSI sample finding"
        "aligned fields"
        "sample actual"

let closed =
    finding
    |> ReviewFindings.markFixed "fsi"
    |> ReviewFindings.markReviewed "fsi-run"
    |> ReviewFindings.close

printfn "SecondAntShowcase FSI surface OK: %s, findings=%d, mode=%s" (CoverageMap.summary ()) (ReviewFindings.unresolvedCount [ closed ]) (AntTheme.modeName switched.Mode)
