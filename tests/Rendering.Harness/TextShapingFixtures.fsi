namespace Rendering.Harness

open FS.GG.UI.Scene

type TextFixtureCategory =
    | LatinKerning
    | LatinLigature
    | CombiningMark
    | RightToLeft
    | MixedDirection
    | EmojiOrSymbol
    | ArabicContextual
    | DevanagariConjunct
    | ThaiPositioning
    | NewlineControl
    | NegativeMissingGlyph

type TextShapingFixture =
    { Id: string
      Category: TextFixtureCategory
      Text: string
      Font: FontSpec
      ExpectedDirection: TextDirection
      ExpectedScript: TextScript
      ExpectsFallback: bool
      ExpectsMissingGlyph: bool }

module TextShapingFixtures =
    val all: TextShapingFixture list
    val byCategory: category: TextFixtureCategory -> TextShapingFixture list
