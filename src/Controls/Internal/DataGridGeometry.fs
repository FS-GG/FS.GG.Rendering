namespace FS.GG.UI.Controls

open FS.GG.UI.Scene
open FS.GG.UI.DesignSystem

/// Feature 175 (issue #175): geometry for the keyed child tree `DataGrid.create` builds. Its
/// `data-grid-header-cell` and `data-grid-cell` leaves were not registered as rich kinds, so they
/// painted through the generic box+ellipsized-label leaf branch: the values reached the screen, but
/// with no tabular affordance — no header emphasis, no column rules, nothing that read as a grid.
///
/// `data-grid-header` and `data-grid-row` deliberately have NO painter here. They own the cells, so
/// they are containers, and a container never reaches `faithfulContent`: `paintNode` paints its
/// frame and recurses into the children. That frame is exactly the row rule a grid wants.
module internal DataGridGeometry =
    open ControlPrimitives

    /// Body-text size, matched to `gridGeom`'s schematic so the bound grid and the unbound chrome
    /// read as the same control.
    let private cellFontSize = 11.0

    /// A cell owns the rule on its trailing and bottom edge; together the cells' rules draw the grid.
    /// The leading/top edges belong to the neighbour, so rules are never painted twice.
    let private cellRules (theme: Theme) (box: Rect) : Scene list =
        let rule = Paint.stroke theme.Muted 1.0

        [ Scene.line
              { X = box.X + box.Width; Y = box.Y }
              { X = box.X + box.Width; Y = box.Y + box.Height }
              rule
          Scene.line
              { X = box.X; Y = box.Y + box.Height }
              { X = box.X + box.Width; Y = box.Y + box.Height }
              rule ]

    /// Clipped to the cell so a value wider than its column can never bleed into the neighbour.
    let private cellText (theme: Theme) (box: Rect) (color: Color) (value: string) : Scene =
        Scene.clipped
            (RectClip box)
            (Scene.group [ mkText theme (box.X + 6.0) (box.Y + box.Height * 0.66) cellFontSize color value ])

    /// A header cell: the muted header band carrying the column's label.
    let headerCellGeom (theme: Theme) (box: Rect) (label: string) : Scene list =
        Scene.rectangle (box.X, box.Y, box.Width, box.Height) theme.Muted
        :: cellText theme box theme.Foreground label
        :: cellRules theme box

    /// A body cell: the grid surface carrying the cell's value.
    let cellGeom (theme: Theme) (box: Rect) (value: string) : Scene list =
        Scene.rectangle (box.X, box.Y, box.Width, box.Height) theme.Background
        :: cellText theme box theme.Foreground value
        :: cellRules theme box
