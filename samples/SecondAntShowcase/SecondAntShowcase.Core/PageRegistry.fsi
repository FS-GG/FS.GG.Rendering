module SecondAntShowcase.Core.PageRegistry

open SecondAntShowcase.Core.Model

val all: Page list
val catalogPages: Page list
val templatePages: Page list
val byId: id: string -> Page
