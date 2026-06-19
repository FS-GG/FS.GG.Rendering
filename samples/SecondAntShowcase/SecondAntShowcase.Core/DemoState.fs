/// Seeded representative content (FR-004 / R5): values + collection data so no showcased
/// control renders empty, and so interactive controls have a starting state. All values
/// are literals — no wall-clock, no randomness (FR-011) — so headless evidence is
/// byte-identical across same-seed runs. Static content (option sets, trails, slides)
/// lives here as module literals; only interaction-bearing values are `DemoState` fields.
module SecondAntShowcase.Core.DemoState

open SecondAntShowcase.Core.Model

// --- option sets reused by selection / navigation controls and the seeded scripts -----
let radioOptions = [ "Comfortable"; "Compact"; "Spacious" ]
let segmentedOptions = [ "List"; "Board"; "Calendar" ]
let comboItems = [ "Design"; "Engineering"; "Product"; "Support" ]
let listItems = [ "Inbox"; "Drafts"; "Sent"; "Archive" ]
let multiItems = [ "Bold"; "Italic"; "Underline"; "Strike" ]
let treeItems = [ "Workspace"; "  Components"; "  Tokens"; "  Samples" ]
let tabItems = [ "Overview"; "Details"; "Activity" ]
let menuItems = [ "File"; "Edit"; "View"; "Help" ]
let cascaderItems = [ "Asia / China / Beijing"; "Asia / Japan / Tokyo"; "Europe / France / Paris" ]
let autoCompleteSuggestions = [ "ant@design.com"; "ant@example.com"; "ant@fs.gg" ]

// --- net-new Ant primitive content (R5) — literal, deterministic ----------------------
let timelineItems = [ "Created"; "In review"; "Approved"; "Shipped" ]
let stepsItems = [ "Account"; "Profile"; "Confirm"; "Done" ]
let collapsePanels = [ "General"; "Security"; "Advanced" ]
let breadcrumbTrail = [ "Home"; "Library"; "Controls" ]
let anchorItems = [ "Introduction"; "Tokens"; "Usage"; "API" ]
let carouselSlides = [ "Welcome"; "What's new"; "Get started" ]
let tagSamples = [ "Stable"; "Beta"; "Deprecated" ]
let descriptionsItems = [ "Name"; "Ada Lovelace"; "Role"; "Engineer"; "Team"; "Platform" ]
let cardItems = [ "Active users"; "1,284" ]
let avatarInitials = "AL"
let paginationTotal = 8

// --- enterprise template demo data (US2 / R6) -----------------------------------------
let workbenchRows =
    [ "Build #142 — passed · 8 min"
      "Release candidate — running · 3/5 jobs"
      "Design-token audit — needs review" ]

let listRows =
    [ "Order 1001 — shipped · Enterprise"
      "Order 1002 — pending approval"
      "Order 1003 — shipped · Renewal" ]

let detailFacts =
    [ "Status"; "Open"
      "Owner"; "Ada"
      "Priority"; "High"
      "SLA"; "24h response" ]

/// The seeded form starting state (Editing, populated with a valid baseline; the form
/// page's seeded script drives an invalid-then-valid sequence over it).
let formSeed: FormState =
    { Name = "Ada Lovelace"
      Email = "ada@example.com"
      Role = "Engineer"
      Agree = true
      Phase = Editing }

/// The single seeded starting state. Deterministic and theme-independent.
let seed: DemoState =
    { TextValue = "Hello, Ant showcase"
      AreaValue = "One semantic control set,\nrestyled to the Ant language."
      NumericValue = 42.0
      SliderValue = 0.6
      RateValue = 4.0
      AutoCompleteValue = "ant@"
      UploadValue = "report.pdf"
      ButtonClicks = 0
      ToggleOn = true
      Checked = true
      SwitchOn = false
      RadioSelected = "Compact"
      SegmentedSelected = "Board"
      ComboSelected = "Engineering"
      ListSelected = "Drafts"
      MultiSelected = [ "Bold"; "Underline" ]
      TreeSelected = "  Components"
      CascaderSelected = "Asia / Japan / Tokyo"
      ColorSelected = "ant-blue"
      Tab = "Overview"
      MenuSelected = "File"
      StepsCurrent = 1
      PaginationPage = 1
      CollapseOpen = "General"
      ProgressValue = 0.65
      OverlayOpen = false
      DialogOpen = false
      DrawerOpen = false
      DatePickerOpen = false
      DatePickerSelected = Some(System.DateOnly(2026, 6, 15))
      DatePickerFocused = None
      Form = formSeed }
