# Feature Specification: Fix Startup Flicker (Interactive Gallery Window)

**Feature Branch**: `124-fix-startup-flicker`

**Created**: 2026-06-15

**Status**: Draft

**Input**: User description: "fix the flickering after startup."

## Context

The Controls Gallery's **interactive windowed mode** (feature 123, the advisory GL-gated
path) visibly **flickers from the moment the window appears until the user first
interacts** — pushing a button or navigating to another page makes the flicker stop and
the window settle into a stable image. The gallery's *content* is correct (the
deterministic offscreen render is validated non-blank); the problem is purely the
**live-window presentation at and shortly after startup**. This feature makes the
interactive window present a steady, non-flickering image as soon as it is shown,
without requiring the user to interact first — and without disturbing the headless
evidence / determinism path, which is already correct.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A steady window from the first frame (Priority: P1)

Someone launches the gallery in interactive mode on a desktop with a display. The window
appears already showing a steady image of the gallery (app bar, nav rail, current page,
status strip). They do **not** have to click anything to make it stop flickering.

**Why this priority**: This is the entire feature. The current behavior — a window that
flickers until the user interacts — reads as broken and undermines the gallery's job as
living documentation of the framework. A stable first impression is the core value.

**Independent Test**: Launch `interactive` on the live desktop and watch the window for
the first few seconds without touching keyboard or mouse; confirm the image is steady
(no flicker) the whole time.

**Acceptance Scenarios**:

1. **Given** a desktop with a live display/GL, **When** the gallery is launched in
   interactive mode and the window becomes visible, **Then** the window shows a steady,
   non-flickering image without any user input.
2. **Given** the window has just appeared, **When** no input is provided for several
   seconds, **Then** no flicker is observed during that time.

---

### User Story 2 - Stays steady while idle (Priority: P2)

After the window has appeared and settled, the user leaves it untouched. The image stays
steady — it does not begin flickering again after a period of inactivity.

**Why this priority**: A fix that only masks the first instant but lets flicker return
when idle would not be trusted. Idle stability is what makes the first-frame fix durable.

**Independent Test**: Launch the window, leave it with no input for an extended period,
and confirm the image remains steady throughout.

**Acceptance Scenarios**:

1. **Given** the window is open and idle, **When** no input occurs for an extended period,
   **Then** the window continues to display a steady, non-flickering image.

---

### User Story 3 - No distracting side effects (Priority: P3)

The way the window is kept steady does not introduce something visually distracting (a
constantly spinning counter, a blinking element, or other ongoing motion) and does not
noticeably tax the machine just to stay still.

**Why this priority**: It is easy to "fix" flicker by forcing constant motion; that
trades one annoyance for another. The steady image should look genuinely still.

**Independent Test**: Watch the idle window and confirm it shows a still image with no
ongoing motion introduced solely to keep it stable; confirm no obvious resource drain.

**Acceptance Scenarios**:

1. **Given** the window is open and idle, **When** a reviewer observes it, **Then** they
   see a still image with no persistent animation added only to prevent flicker.

---

### Edge Cases

- **Window disturbances**: After a resize, minimize-then-restore, or focus loss and
  regain, does the window return to a steady image without the user having to interact?
- **Initial theme/accent**: Does the steady-startup behavior hold for every launch
  option (light/dark mode, indigo/teal accent)?
- **No display / no GL host**: The headless path must keep its existing clean
  degrade-and-disclose behavior — this feature must not change it.
- **Headless evidence**: The deterministic evidence run and its byte-identical output
  must be unaffected by any change made here.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The interactive gallery window MUST present a steady, non-flickering image
  from the moment it becomes visible, without requiring any user interaction.
- **FR-002**: The window MUST remain steady (non-flickering) while idle after it first
  appears, for as long as it stays open with no input.
- **FR-003**: The window MUST return to a steady image, without user interaction, after
  ordinary window events that can disturb its surface (resize, minimize/restore, focus
  change).
- **FR-004**: The chosen approach MUST NOT introduce a persistent, visible animation or
  ongoing motion whose only purpose is to keep the image steady, and MUST NOT impose a
  noticeable resource cost solely for that purpose.
- **FR-005**: The change MUST NOT alter the deterministic headless evidence path or its
  byte-identical, same-seed output (the existing determinism guarantee is preserved).
- **FR-006**: On a host with no live window/GL, behavior MUST remain the existing clean
  degrade-and-disclose; this feature applies only to the live-window presentation path.
- **FR-007**: Steady-startup behavior MUST hold for every supported initial launch option
  (light/dark mode and each accent).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On a supported live desktop, **100%** of interactive launches show a steady
  image without requiring the user to interact first (current behavior: flickers on every
  launch until first interaction).
- **SC-002**: No flicker is observed in the **first 2 seconds** after the window becomes
  visible, with no user input during that window.
- **SC-003**: No flicker is observed after the window has been left **idle for at least 30
  seconds**.
- **SC-004**: The headless evidence determinism check (two same-seed runs are
  byte-identical) continues to pass unchanged after the fix.
- **SC-005**: A reviewer watching the idle window sees a still image — no persistent
  motion was introduced only to prevent flicker.

## Assumptions

- The flicker originates in the **live-window presentation path**. *(Post-implementation: the
  precise cause turned out to be a **double `SwapBuffers` per frame** — Silk.NET's
  `ShouldSwapAutomatically` default plus the framework's explicit swap — presenting an undefined
  buffer as a black flash. The "only after enough repaints" framing in this assumption was a
  guess and was wrong; see plan.md / research.md.)*
- The gallery consumes the framework's interactive presentation as a packed package. If
  the public surface does not already expose what is needed to guarantee a steady first
  frame, the fix may be a sample-side mitigation or a small added framework capability —
  the specific approach is decided in planning, not here.
- "Steady / no flicker" means no flicker perceptible to a person watching the window;
  it is verified by observation on the development desktop (the live X11 display used for
  feature 123).
- **Out of scope**: the headless evidence / CI path, determinism, coverage, theme
  invariance, and the non-interactive rendering — all already correct and only protected
  (not changed) by this feature.
- The interactive window remains **advisory and GL-gated** (feature 123, FR-016). This
  feature improves its presentation quality; it does **not** promote interactive mode into
  the required CI gate.
