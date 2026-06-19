
Microsoft (R) F# Interactive version 15.2.301.0 for F# 10.0
Copyright (c) Microsoft Corporation. All Rights Reserved.

For help type #help;;

> 
--> Referenced '/home/developer/projects/FS.GG.Rendering/src/Scene/bin/Debug/net10.0/FS.GG.UI.Scene.dll' (file may be locked by F# Interactive process)

> 
--> Referenced '/home/developer/projects/FS.GG.Rendering/src/Layout/bin/Debug/net10.0/FS.GG.UI.Layout.dll' (file may be locked by F# Interactive process)

> 
--> Referenced '/home/developer/projects/FS.GG.Rendering/tests/Controls.Tests/bin/Debug/net10.0/Yoga.Net.dll' (file may be locked by F# Interactive process)

> 
--> Referenced '/home/developer/projects/FS.GG.Rendering/src/KeyboardInput/bin/Debug/net10.0/FS.GG.UI.KeyboardInput.dll' (file may be locked by F# Interactive process)

> 
--> Referenced '/home/developer/projects/FS.GG.Rendering/src/DesignSystem/bin/Debug/net10.0/FS.GG.UI.DesignSystem.dll' (file may be locked by F# Interactive process)

> 
--> Referenced '/home/developer/projects/FS.GG.Rendering/src/Themes.Default/bin/Debug/net10.0/FS.GG.UI.Themes.Default.dll' (file may be locked by F# Interactive process)

> 
--> Referenced '/home/developer/projects/FS.GG.Rendering/src/Controls/bin/Debug/net10.0/FS.GG.UI.Controls.dll' (file may be locked by F# Interactive process)

> 
--> Referenced '/home/developer/projects/FS.GG.Rendering/src/Testing/bin/Debug/net10.0/FS.GG.UI.Testing.dll' (file may be locked by F# Interactive process)

> > > > > type Msg = | Noop

> val scope: VisualInspectionScope = { ScopeId = "fsi"
                                     Title = "FSI"
                                     Required = true }

> val finding: VisualInspectionFinding =
  { FindingId = "text-contained-in-owner:title"
    RuleId = "text-contained-in-owner"
    Severity = Blocking
    AffectedNodeIds = ["title"]
    AffectedRegionIds = []
    Message = "overflow"
    Expected = "inside"
    Actual = "overflow"
    ExceptionId = None
    Diagnostics = [] }

> val tree: Control<Msg> =
  { Kind = "text-block"
    Key = Some "title"
    Attributes = [{ Name = "text"
                    Category = Content
                    Value = TextValue "Hello inspection" }]
    Children = []
    Content = Some "Hello inspection"
    Accessibility = Some { Role = StaticText
                           NameSource = "Hello inspection"
                           State = ["normal"]
                           FocusOrder = None
                           Keyboard = { Focusable = false
                                        ActivationKeys = []
                                        NavigationKeys = [] }
                           Contrast = Some { Foreground = { Red = 0uy
                                                            Green = 0uy
                                                            Blue = 0uy
                                                            Alpha = 255uy }
                                             Background = { Red = 255uy
                                                            Green = 255uy
                                                            Blue = 255uy
                                                            Alpha = 255uy }
                                             Ratio = 7.0
                                             RequiredRatio = 4.5 }
                           Navigation = None
                           Collection = None } }

> val artifact: VisualInspectionArtifact =
  { ArtifactId = "fsi:320x200:light"
    Scope = { ScopeId = "fsi"
              Title = "FSI"
              Required = true }
    OutputSize = { Width = 320
                   Height = 200 }
    Presentation = "light"
    ReadinessStatus = Accepted
    Nodes = [{ NodeId = "title"
               ParentId = None
               Kind = Root
               OwnerId = Some "title"
               Bounds = Some { X = 0.0
                               Y = 0.0
                               Width = 320.0
                               Height = 200.0 }
               Clip = None
               ZOrder = 0
               PaintRole = Background
               SurfaceRole = Root
               TextRunIds = ["title:text"]
               Children = []
               Dynamic = false
               UnsupportedFacts = [] }]
    Regions = [{ RegionId = "title"
                 Name = "title"
                 Role = Root
                 Bounds = Some { X = 0.0
                                 Y = 0.0
                                 Width = 320.0
                                 Height = 200.0 }
                 Required = true
                 OwnerNodeIds = ["title"]
                 AllowedOverlapRoles = [Overlay; Popup; Floating] }]
    TextRuns = [{ TextId = "title:text"
                  OwnerNodeId = "title"
                  Text = "Hello inspection"
                  TextBounds = Some { X = 8.0
                                      Y = 93.0
                                      Width = 129.92
                                      Height = 14.0 }
                  OwnerBounds = Some { X = 0.0
                                       Y = 0.0
                                       Width = 320.0
                                       Height = 200.0 }
                  Baseline = Some 11.2
                  MeasurementMode = Approximate
                  FitStatus = Inside
                  Required = true
                  Diagnostics = [] }]
    PaintCoverage = [{ CoverageId = "title:paint"
                       TargetId = "title"
                       PaintRole = Background
                       CoverageBounds = Some { X = 0.0
                                               Y = 0.0
                                               Width = 320.0
                                               Height = 200.0 }
                       CoverageStatus = Complete
                       Reason = None }]
    ClipFacts = [{ ClipId = "title:clip"
                   NodeId = "title"
                   ClipBounds = Some { X = 0.0
                                       Y = 0.0
                                       Width = 320.0
                                       Height = 200.0 }
                   ClipStatus = None
                   Reason = None
                   AffectedTextRunIds = ["title:text"] }]
    Findings = []
    UnsupportedFacts = []
    Diagnostics = []
    GeneratedAtUtc = "2026-06-19T09:38:31.4115444+00:00" }

> val validation: VisualInspectionValidationResult =
  { ArtifactId = "fsi:320x200:light"
    ReadinessStatus = Accepted
    Findings = []
    AppliedExceptions = []
    InvalidExceptions = []
    UnusedExceptions = []
    Diagnostics = [] }

> val summary: VisualInspectionSummary = { RunId = "fsi"
                                         OverallStatus = Accepted
                                         ArtifactCount = 1
                                         InspectedScopes = ["fsi"]
                                         NotInspectedScopes = []
                                         NotRunScopes = []
                                         StatusCounts = [("accepted", 1)]
                                         FindingCounts = []
                                         BlockingFindings = []
                                         UnsupportedFacts = []
                                         AcceptedExceptions = []
                                         InvalidExceptions = []
                                         RelatedVisualEvidence = []
                                         Caveats = []
                                         Diagnostics = [] }

> finding=text-contained-in-owner:title status=accepted nodes=1 textRuns=1 summary=accepted
val it: unit = ()

> 