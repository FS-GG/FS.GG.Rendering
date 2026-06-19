module SecondAntShowcase.Core.CoverageMap

open SecondAntShowcase.Core.Model

val catalogIds: unit -> string list
val assignedIds: unit -> string list
val check: unit -> CoverageResult
val isClean: result: CoverageResult -> bool
val summary: unit -> string
