// Feature 207 — FS.GG.UI BOM / metapackage.
//
// The metapackage ships NO assembly: this project sets IncludeBuildOutput=false and packs
// purely from FS.GG.UI.nuspec (16 exact-version member dependencies). F# requires at least
// one compile input, so this single empty module exists only to satisfy the compiler; it is
// never packed and carries no public surface (no .fsi needed — nothing is exposed).
module internal FS.GG.UI.Meta
