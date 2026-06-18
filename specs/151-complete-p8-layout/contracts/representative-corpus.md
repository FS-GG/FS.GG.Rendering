# Representative Corpus Contract

## Purpose

Define the minimum executable and reviewable corpus required before P8 layout acceptance can be
claimed.

## Required Layout Cases

The corpus must include:

- finite constrained roots;
- zero-sized, very small, very large, finite, unbounded, invalid, and contradictory constraints;
- measured leaves;
- intrinsic-capable content;
- empty and single-child containers;
- deep nesting;
- dynamic content whose natural size changes after initial layout;
- layout-affecting attribute changes;
- child insertion, removal, reorder, and identity changes;
- visible, hidden, and collapsed visibility changes;
- diagnostic cases for unsupported or contradictory intrinsic behavior.

Each case records:

- stable `CaseId`;
- fixture/test path;
- input tree and constraints;
- expected bounds and child placements;
- expected diagnostics;
- accepted/failed/skipped/environment-limited verdict;
- evidence command or output path.

## Required ScrollViewer Cases

The ScrollViewer corpus must include at least:

- empty content;
- smaller-than-viewport content;
- exact-fit content;
- barely overflowing content;
- substantially overflowing content;
- nested scroll content;
- clipped parent;
- layered parent;
- text/content-driven natural size;
- dynamic content change;
- invalid intrinsic fallback.

Each ScrollViewer case records viewport bounds, content extent, horizontal/vertical max offsets,
extent source, diagnostics, and verdict.

## Acceptance Rules

- Accepted cases must have finite deterministic expected geometry.
- ScrollViewer accepted extent comes from the Layout intrinsic/content extent path, not a rendered
  descendant-bounds walk.
- Invalid or unsupported cases are accepted only when the expected diagnostic is present and
  misleading output is not accepted.
- Skipped, failed, synthetic-only, missing, and environment-limited required cases block final P8
  accepted status unless explicitly scoped out as non-required follow-up with reviewer-visible
  rationale.
- The final readiness summary links every required case to executable evidence.
