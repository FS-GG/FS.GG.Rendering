module SecondAntShowcase.Core.InteractionContracts

open SecondAntShowcase.Core.Model

type InteractionContract =
    { ContractId: string
      ControlIds: string list
      PageId: string
      StartingState: string
      Action: string
      ExpectedStateChange: string
      VisibleEvidence: string
      ScriptStep: SecondAntShowcaseMsg option
      ThemeInvariant: bool
      DisplayOnlyReason: string option }

type InteractionCoverage =
    { MissingContractOrReason: string list
      ContractedControls: string list
      DisplayOnlyControls: string list }

val all: InteractionContract list
val displayOnlyReasons: Map<string, string>
val forControl: controlId: string -> InteractionContract list
val coverage: unit -> InteractionCoverage
val isClean: coverage: InteractionCoverage -> bool
val summaryMarkdown: unit -> string
