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
    let private font size = { Family = None; Size = size; Weight = None }
    let private rtl = FS.GG.UI.Scene.TextDirection.RightToLeft
    let private mixed = FS.GG.UI.Scene.TextDirection.MixedDirection

    let private fx
        (id: string)
        (category: TextFixtureCategory)
        (text: string)
        (direction: TextDirection)
        (script: TextScript)
        (fallback: bool)
        (missing: bool)
        =
        { Id = id
          Category = category
          Text = text
          Font = font 18.0
          ExpectedDirection = direction
          ExpectedScript = script
          ExpectsFallback = fallback
          ExpectsMissingGlyph = missing }

    let all =
        [ fx "latin-kerning-av" LatinKerning "AV" LeftToRight LatinScript false false
          fx "latin-kerning-wa" LatinKerning "WA" LeftToRight LatinScript false false
          fx "latin-kerning-to" LatinKerning "To" LeftToRight LatinScript false false
          fx "latin-kerning-yo" LatinKerning "Yo" LeftToRight LatinScript false false
          fx "latin-ligature-office" LatinLigature "office" LeftToRight LatinScript false false
          fx "latin-ligature-affinity" LatinLigature "affinity" LeftToRight LatinScript false false
          fx "latin-ligature-flower" LatinLigature "flower" LeftToRight LatinScript false false
          fx "latin-ligature-file" LatinLigature "file" LeftToRight LatinScript false false
          fx "combining-cafe" CombiningMark "cafe\u0301" LeftToRight LatinScript false false
          fx "combining-ring" CombiningMark "A\u030A" LeftToRight LatinScript false false
          fx "combining-dot" CombiningMark "i\u0307" LeftToRight LatinScript false false
          fx "combining-tilde" CombiningMark "n\u0303" LeftToRight LatinScript false false
          fx "rtl-hebrew" RightToLeft "\u05E9\u05DC\u05D5\u05DD" rtl UnknownScript true false
          fx "rtl-arabic-word" RightToLeft "\u0633\u0644\u0627\u0645" rtl ArabicScript false false
          fx "rtl-arabic-number" RightToLeft "\u0631\u0642\u0645 42" mixed MixedScript false false
          fx "rtl-arabic-short" RightToLeft "\u0646\u0635" rtl ArabicScript false false
          fx "mixed-english-arabic" MixedDirection "abc \u0633\u0644\u0627\u0645" mixed MixedScript false false
          fx "mixed-arabic-english" MixedDirection "\u0633\u0644\u0627\u0645 abc" mixed MixedScript false false
          fx "mixed-hebrew-english" MixedDirection "abc \u05E9\u05DC\u05D5\u05DD" mixed MixedScript true false
          fx "mixed-symbol-run" MixedDirection "A \u0633 B" mixed MixedScript false false
          fx "emoji-smile" EmojiOrSymbol "hello \u263A" LeftToRight MixedScript false false
          fx "emoji-heart" EmojiOrSymbol "love \u2665" LeftToRight MixedScript false false
          fx "symbol-arrow" EmojiOrSymbol "next \u25B8" LeftToRight MixedScript true false
          fx "symbol-bullet" EmojiOrSymbol "item \u00B7" LeftToRight MixedScript true false
          fx "arabic-contextual-salaam" ArabicContextual "\u0633\u0644\u0627\u0645" rtl ArabicScript false false
          fx "arabic-contextual-mohammad" ArabicContextual "\u0645\u062D\u0645\u062F" rtl ArabicScript false false
          fx "arabic-contextual-ain" ArabicContextual "\u0639\u0631\u0628\u064A" rtl ArabicScript false false
          fx "arabic-contextual-lam-alef" ArabicContextual "\u0644\u0627" rtl ArabicScript false false
          fx "devanagari-ksha" DevanagariConjunct "\u0915\u094D\u0937" LeftToRight DevanagariScript false false
          fx "devanagari-shri" DevanagariConjunct "\u0936\u094D\u0930\u0940" LeftToRight DevanagariScript false false
          fx "devanagari-hindi" DevanagariConjunct "\u0939\u093F\u0928\u094D\u0926\u0940" LeftToRight DevanagariScript false false
          fx "devanagari-tra" DevanagariConjunct "\u0924\u094D\u0930" LeftToRight DevanagariScript false false
          fx "thai-sawasdee" ThaiPositioning "\u0E2A\u0E27\u0E31\u0E2A\u0E14\u0E35" LeftToRight ThaiScript false false
          fx "thai-vowel" ThaiPositioning "\u0E40\u0E01\u0E49\u0E32" LeftToRight ThaiScript false false
          fx "thai-mark" ThaiPositioning "\u0E01\u0E34" LeftToRight ThaiScript false false
          fx "thai-tone" ThaiPositioning "\u0E01\u0E49" LeftToRight ThaiScript false false
          fx "newline-single" NewlineControl "one\ntwo" LeftToRight LatinScript false false
          fx "newline-carriage" NewlineControl "one\rtwo" LeftToRight LatinScript false false
          fx "newline-leading" NewlineControl "\nlead" LeftToRight LatinScript false false
          fx "negative-private-use" NegativeMissingGlyph "\uE000" AutoDirection UnknownScript true true
          fx "negative-noncharacter" NegativeMissingGlyph "\uFFFF" AutoDirection UnknownScript true true
          fx "negative-pua-inline" NegativeMissingGlyph "x\uE001y" LeftToRight MixedScript true true ]

    let byCategory category =
        all |> List.filter (fun fixture -> fixture.Category = category)
