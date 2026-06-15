/// Seeded representative content (FR-004): values + collection data so no showcased
/// control renders empty, and so interactive controls have a starting state. All values
/// are literals — no wall-clock, no randomness (FR-009).
module ControlsGallery.Core.DemoState

open ControlsGallery.Core.Model

/// Option sets reused by selection controls and the seeded scripts.
let radioOptions = [ "Comfortable"; "Compact"; "Spacious" ]
let comboItems = [ "Slate"; "Indigo"; "Teal" ]
let listItems = [ "Inbox"; "Drafts"; "Sent"; "Archive" ]
let multiItems = [ "Bold"; "Italic"; "Underline"; "Strike" ]
let treeItems = [ "Workspace"; "  Components"; "  Tokens"; "  Samples" ]
let tabItems = [ "Overview"; "Details"; "Activity" ]
let menuItems = [ "File"; "Edit"; "View"; "Help" ]

/// The single seeded starting state. Deterministic and theme-independent.
let seed: DemoState =
    { ButtonClicks = 0
      TextValue = "Hello, gallery"
      AreaValue = "Indigo & Teal\non Slate"
      NumericValue = 42.0
      SliderValue = 0.6
      Checked = true
      SwitchOn = false
      ToggleOn = true
      RadioSelected = "Compact"
      ComboSelected = "Indigo"
      ListSelected = "Drafts"
      MultiSelected = [ "Bold"; "Underline" ]
      TreeSelected = "  Components"
      Tab = "Overview"
      MenuSelected = "File"
      ProgressValue = 0.65
      ColorSelected = "indigo"
      OverlayOpen = false
      DialogOpen = false }
