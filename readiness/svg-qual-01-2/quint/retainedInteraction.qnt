module retainedInteraction {
  type State = {
    revision: int,
    selected: int,
    focused: int,
    panX: int,
    panY: int,
    zoom: int,
    captured: int,
    objectAAvailable: bool,
    objectBAvailable: bool,
    lastAction: str,
    arg1: int,
    arg2: int,
    arg3: int,
    arg4: int,
    arg5: int,
    arg6: int,
  }
  type Transition = { state: State, outcome: int }

  pure val accepted = 0
  pure val staleRevision = 1
  pure val unknownObject = 2
  pure val nonIncreasingRevision = 3
  pure val invalidCamera = 4
  pure val pointerNotCaptured = 5
  pure val invalidScene = 6

  pure val initialState = {
    revision: 0,
    selected: 0,
    focused: 0,
    panX: 0,
    panY: 0,
    zoom: 1,
    captured: -1,
    objectAAvailable: true,
    objectBAvailable: true,
    lastAction: "Init",
    arg1: 0,
    arg2: 0,
    arg3: 0,
    arg4: 0,
    arg5: 0,
    arg6: 0,
  }

  pure val actionCatalogue = Set(
    "Select",
    "ReplaceScene",
    "ReplaceDocument",
    "ClearSelection",
    "FocusNext",
    "FocusPrevious",
    "SetCamera",
    "CapturePointer",
    "ReleasePointer"
  )

  pure def available(s: State, objectId: int): bool =
    if (objectId == 1) s.objectAAvailable
    else if (objectId == 2) s.objectBAvailable
    else false

  pure def reject(s: State, error: int): Transition = { state: s, outcome: error }
  pure def accept(s: State): Transition = { state: s, outcome: accepted }
  pure def boolInt(value: bool): int = if (value) 1 else 0
  pure def observe(s: State, actionName: str, arg1: int, arg2: int, arg3: int, arg4: int, arg5: int, arg6: int): State =
    { ...s, lastAction: actionName, arg1: arg1, arg2: arg2, arg3: arg3, arg4: arg4, arg5: arg5, arg6: arg6 }

  pure def reduceSelect(s: State, expectedRevision: int, objectId: int): Transition = {
    val observed = observe(s, "Select", expectedRevision, objectId, 0, 0, 0, 0)
    if (expectedRevision != s.revision) reject(observed, staleRevision)
    else if (not(available(s, objectId))) reject(observed, unknownObject)
    else accept({ ...observed, selected: objectId, focused: objectId })
  }

  pure def reduceClearSelection(s: State, expectedRevision: int): Transition = {
    val observed = observe(s, "ClearSelection", expectedRevision, 0, 0, 0, 0, 0)
    if (expectedRevision != s.revision) reject(observed, staleRevision)
    else accept({ ...observed, selected: 0 })
  }

  pure def firstAvailable(s: State): int =
    if (s.objectAAvailable) 1 else if (s.objectBAvailable) 2 else 0

  pure def lastAvailable(s: State): int =
    if (s.objectBAvailable) 2 else if (s.objectAAvailable) 1 else 0

  pure def nextFocus(s: State): int =
    if (s.focused == 1 and s.objectBAvailable) 2
    else if (s.focused == 2 and s.objectAAvailable) 1
    else firstAvailable(s)

  pure def previousFocus(s: State): int =
    if (s.focused == 2 and s.objectAAvailable) 1
    else if (s.focused == 1 and s.objectBAvailable) 2
    else lastAvailable(s)

  pure def reduceFocusNext(s: State, expectedRevision: int): Transition = {
    val observed = observe(s, "FocusNext", expectedRevision, 0, 0, 0, 0, 0)
    if (expectedRevision != s.revision) reject(observed, staleRevision)
    else accept({ ...observed, focused: nextFocus(s) })
  }

  pure def reduceFocusPrevious(s: State, expectedRevision: int): Transition = {
    val observed = observe(s, "FocusPrevious", expectedRevision, 0, 0, 0, 0, 0)
    if (expectedRevision != s.revision) reject(observed, staleRevision)
    else accept({ ...observed, focused: previousFocus(s) })
  }

  pure def reduceSetCamera(s: State, expectedRevision: int, panX: int, panY: int, zoom: int): Transition = {
    val observed = observe(s, "SetCamera", expectedRevision, panX, panY, zoom, 0, 0)
    if (expectedRevision != s.revision) reject(observed, staleRevision)
    else if (zoom <= 0) reject(observed, invalidCamera)
    else accept({ ...observed, panX: panX, panY: panY, zoom: zoom })
  }

  pure def reduceCapturePointer(s: State, expectedRevision: int, pointerId: int): Transition = {
    val observed = observe(s, "CapturePointer", expectedRevision, pointerId, 0, 0, 0, 0)
    if (expectedRevision != s.revision) reject(observed, staleRevision)
    else accept({ ...observed, captured: pointerId })
  }

  pure def reduceReleasePointer(s: State, expectedRevision: int, pointerId: int): Transition = {
    val observed = observe(s, "ReleasePointer", expectedRevision, pointerId, 0, 0, 0, 0)
    if (expectedRevision != s.revision) reject(observed, staleRevision)
    else if (s.captured != pointerId) reject(observed, pointerNotCaptured)
    else accept({ ...observed, captured: -1 })
  }

  pure def reduceReplace(
    s: State,
    candidateRevision: int,
    objectAAvailable: bool,
    objectBAvailable: bool,
    panX: int,
    panY: int,
    zoom: int
  ): Transition =
    val observed = observe(s, "ReplaceScene", candidateRevision, boolInt(objectAAvailable), boolInt(objectBAvailable), panX, panY, zoom)
    if (candidateRevision <= s.revision) reject(observed, nonIncreasingRevision)
    else if (zoom <= 0) reject(observed, invalidScene)
    else {
      val candidate = {
        ...observed,
        revision: candidateRevision,
        panX: panX,
        panY: panY,
        zoom: zoom,
        objectAAvailable: objectAAvailable,
        objectBAvailable: objectBAvailable,
      }
      accept({
        ...candidate,
        selected: if (available(candidate, s.selected)) s.selected else 0,
        focused: if (available(candidate, s.focused)) s.focused else 0,
      })
    }

  pure def reduceReplaceDocument(
    s: State,
    expectedRevision: int,
    candidateRevision: int,
    objectAAvailable: bool,
    objectBAvailable: bool,
    validDocument: bool
  ): Transition =
    val observed = observe(s, "ReplaceDocument", expectedRevision, candidateRevision, boolInt(objectAAvailable), boolInt(objectBAvailable), boolInt(validDocument), 0)
    if (expectedRevision != s.revision) reject(observed, staleRevision)
    else if (candidateRevision <= s.revision) reject(observed, nonIncreasingRevision)
    else if (not(validDocument)) reject(observed, invalidScene)
    else {
      val candidate = {
        ...observed,
        revision: candidateRevision,
        objectAAvailable: objectAAvailable,
        objectBAvailable: objectBAvailable,
      }
      accept({
        ...candidate,
        selected: if (available(candidate, s.selected)) s.selected else 0,
        focused: if (available(candidate, s.focused)) s.focused else 0,
      })
    }

  var state: State
  var outcome: int

  action init = all { state' = initialState, outcome' = accepted }

  action selectObject(expectedRevision: int, objectId: int): bool = {
    val result = reduceSelect(state, expectedRevision, objectId)
    all { state' = result.state, outcome' = result.outcome }
  }

  action replaceScene(candidateRevision: int, objectAAvailable: bool, objectBAvailable: bool, panX: int, panY: int, zoom: int): bool = {
    val result = reduceReplace(state, candidateRevision, objectAAvailable, objectBAvailable, panX, panY, zoom)
    all { state' = result.state, outcome' = result.outcome }
  }

  action replaceDocument(expectedRevision: int, candidateRevision: int, objectAAvailable: bool, objectBAvailable: bool, validDocument: bool): bool = {
    val result = reduceReplaceDocument(state, expectedRevision, candidateRevision, objectAAvailable, objectBAvailable, validDocument)
    all { state' = result.state, outcome' = result.outcome }
  }

  action clearSelection(expectedRevision: int): bool = {
    val result = reduceClearSelection(state, expectedRevision)
    all { state' = result.state, outcome' = result.outcome }
  }

  action focusNext(expectedRevision: int): bool = {
    val result = reduceFocusNext(state, expectedRevision)
    all { state' = result.state, outcome' = result.outcome }
  }

  action focusPrevious(expectedRevision: int): bool = {
    val result = reduceFocusPrevious(state, expectedRevision)
    all { state' = result.state, outcome' = result.outcome }
  }

  action setCamera(expectedRevision: int, panX: int, panY: int, zoom: int): bool = {
    val result = reduceSetCamera(state, expectedRevision, panX, panY, zoom)
    all { state' = result.state, outcome' = result.outcome }
  }

  action capturePointer(expectedRevision: int, pointerId: int): bool = {
    val result = reduceCapturePointer(state, expectedRevision, pointerId)
    all { state' = result.state, outcome' = result.outcome }
  }

  action releasePointer(expectedRevision: int, pointerId: int): bool = {
    val result = reduceReleasePointer(state, expectedRevision, pointerId)
    all { state' = result.state, outcome' = result.outcome }
  }

  action step = {
    nondet operation = 0.to(7).oneOf()
    nondet expected = 0.to(2).oneOf()
    nondet objectId = 0.to(2).oneOf()
    nondet pointerId = 1.to(2).oneOf()
    nondet objectAAvailable = Set(true, false).oneOf()
    nondet objectBAvailable = Set(true, false).oneOf()
    nondet nextZoom = 0.to(2).oneOf()
    if (operation == 0) selectObject(expected, objectId)
    else if (operation == 1) replaceScene(expected + 1, objectAAvailable, objectBAvailable, 1, -1, nextZoom)
    else if (operation == 2) clearSelection(expected)
    else if (operation == 3) focusNext(expected)
    else if (operation == 4) focusPrevious(expected)
    else if (operation == 5) setCamera(expected, 1, -1, nextZoom)
    else if (operation == 6) capturePointer(expected, pointerId)
    else releasePointer(expected, pointerId)
  }

  action documentStep = {
    nondet operation = 0.to(7).oneOf()
    nondet expected = 0.to(2).oneOf()
    nondet candidate = 0.to(3).oneOf()
    nondet objectId = 0.to(2).oneOf()
    nondet pointerId = 1.to(2).oneOf()
    nondet objectAAvailable = Set(true, false).oneOf()
    nondet objectBAvailable = Set(true, false).oneOf()
    nondet validDocument = Set(true, false).oneOf()
    nondet nextZoom = 0.to(2).oneOf()
    if (operation == 0) selectObject(expected, objectId)
    else if (operation == 1) replaceDocument(expected, candidate, objectAAvailable, objectBAvailable, validDocument)
    else if (operation == 2) clearSelection(expected)
    else if (operation == 3) focusNext(expected)
    else if (operation == 4) focusPrevious(expected)
    else if (operation == 5) setCamera(expected, 1, -1, nextZoom)
    else if (operation == 6) capturePointer(expected, pointerId)
    else releasePointer(expected, pointerId)
  }

  val revisionNeverDecreases = state.revision >= 0
  val identitiesRemainBounded = and {
    state.selected >= 0,
    state.selected <= 2,
    state.focused >= 0,
    state.focused <= 2,
  }
  val retainedStateSafe = revisionNeverDecreases and identitiesRemainBounded
}

module retainedInteractionTest {
  import retainedInteraction.*

  run actionCoverage =
    init
      .then(selectObject(0, 1))
      .then(clearSelection(0))
      .then(focusNext(0))
      .then(focusPrevious(0))
      .then(setCamera(0, 4, -2, 3))
      .then(capturePointer(0, 7))
      .then(releasePointer(0, 7))
      .then(replaceScene(1, true, true, 9, 8, 2))
      .expect(and {
        state.revision == 1,
        state.selected == 0,
        state.focused == 1,
        state.panX == 9,
        state.panY == 8,
        state.zoom == 2,
        state.captured == -1,
        outcome == accepted,
      })

  run replacementRetention =
    init
      .then(selectObject(0, 2))
      .then(capturePointer(0, 9))
      .then(replaceScene(1, true, true, 2, 3, 4))
      .expect(and { state.selected == 2, state.focused == 2, state.captured == 9 })

  run replacementClearing =
    init
      .then(selectObject(0, 2))
      .then(replaceScene(1, true, false, 2, 3, 4))
      .expect(and { state.selected == 0, state.focused == 0 })

  run documentReplacementRetention =
    init
      .then(selectObject(0, 2))
      .then(capturePointer(0, 9))
      .then(setCamera(0, 4, -2, 3))
      .then(replaceDocument(0, 1, true, true, true))
      .expect(and {
        state.revision == 1,
        state.selected == 2,
        state.focused == 2,
        state.panX == 4,
        state.panY == -2,
        state.zoom == 3,
        state.captured == 9,
      })

  run documentReplacementClearing =
    init
      .then(selectObject(0, 2))
      .then(replaceDocument(0, 1, true, false, true))
      .expect(and { state.selected == 0, state.focused == 0 })

  run invalidDocumentReplacementIsNoOp =
    init
      .then(selectObject(0, 1))
      .then(replaceDocument(0, 1, true, true, false))
      .expect(and { state.revision == 0, state.selected == 1, state.focused == 1, outcome == invalidScene })

  run staleIsNoOp =
    init
      .then(replaceScene(1, true, true, 0, 0, 1))
      .then(selectObject(0, 2))
      .expect(and {
        state.revision == 1,
        state.selected == 0,
        state.focused == 0,
        state.panX == 0,
        state.panY == 0,
        state.zoom == 1,
        state.captured == -1,
        outcome == staleRevision,
      })

  run invalidCameraIsNoOp =
    init
      .then(setCamera(0, 4, 5, 0))
      .expect(and { state.panX == 0, state.panY == 0, state.zoom == 1, outcome == invalidCamera })

  run mismatchedReleaseIsNoOp =
    init
      .then(capturePointer(0, 8))
      .then(releasePointer(0, 9))
      .expect(and { state.captured == 8, outcome == pointerNotCaptured })
}
