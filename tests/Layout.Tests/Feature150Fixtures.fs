module Feature150Fixtures

open FS.GG.UI.Layout

let measuredLeaf id width height =
    let measure _ =
        { Width = width
          Height = height
          Diagnostics = [] }

    { Defaults.layoutNode id with
        Intent =
            { Defaults.layoutIntent with
                Size = { Width = Some width; Height = Some height } }
        Measure = Some measure }

let container id width height children =
    { Defaults.layoutNode id with
        Intent =
            { Defaults.layoutIntent with
                Direction = Column
                Size = { Width = Some width; Height = Some height }
                AlignItems = Stretch
                Gap = { Row = 4.0; Column = 0.0 } }
        Children = children }

let intrinsicColumn () =
    container "root" 120.0 180.0 [ measuredLeaf "a" 60.0 24.0; measuredLeaf "b" 80.0 32.0 ]

let dynamicColumn height =
    container "root" 120.0 240.0 [ measuredLeaf "a" 60.0 height; measuredLeaf "b" 80.0 32.0 ]

let available = Defaults.availableSpace 120.0 180.0

