module inputCommand {
  type State = {
    pending: int,
    deadlineRequested: bool,
    heldA: bool,
    heldB: bool,
    heldAggregate: bool,
    acceptedPresses: int,
    invocations: int,
    lastCommand: int,
  }

  pure val initialState = {
    pending: 0,
    deadlineRequested: false,
    heldA: false,
    heldB: false,
    heldAggregate: false,
    acceptedPresses: 0,
    invocations: 0,
    lastCommand: 0,
  }

  pure def aggregate(a: bool, b: bool): bool = a or b

  var state: State

  action init = state' = initialState

  action pressPrefix = all {
    state.pending == 0,
    state' = {
      ...state,
      pending: 1,
      deadlineRequested: true,
      acceptedPresses: state.acceptedPresses + 1,
    },
  }

  action pressContinuation = all {
    state.pending == 1,
    state' = {
      ...state,
      pending: 0,
      deadlineRequested: false,
      acceptedPresses: state.acceptedPresses + 1,
      invocations: state.invocations + 1,
      lastCommand: 2,
    },
  }

  action pressRoot = all {
    state' = {
      ...state,
      pending: 0,
      deadlineRequested: false,
      acceptedPresses: state.acceptedPresses + 1,
      invocations: state.invocations + 1,
      lastCommand: 3,
    },
  }

  action deadline(available: bool): bool = all {
    state.pending == 1,
    state' = {
      ...state,
      pending: 0,
      deadlineRequested: false,
      invocations: if (available) state.invocations + 1 else state.invocations,
      lastCommand: if (available) 1 else state.lastCommand,
    },
  }

  action pressHeld(source: int): bool = {
    val nextA = state.heldA or source == 1
    val nextB = state.heldB or source == 2
    all {
      source >= 1,
      source <= 2,
      state' = {
        ...state,
        heldA: nextA,
        heldB: nextB,
        heldAggregate: aggregate(nextA, nextB),
      },
    }
  }

  action releaseHeld(source: int): bool = {
    val nextA = state.heldA and source != 1
    val nextB = state.heldB and source != 2
    all {
      source >= 1,
      source <= 2,
      state' = {
        ...state,
        heldA: nextA,
        heldB: nextB,
        heldAggregate: aggregate(nextA, nextB),
      },
    }
  }

  action cancelTransient = state' = {
    ...state,
    pending: 0,
    deadlineRequested: false,
    heldA: false,
    heldB: false,
    heldAggregate: false,
  }

  action step = {
    nondet operation = 0.to(6).oneOf()
    nondet source = 1.to(2).oneOf()
    nondet available = Set(true, false).oneOf()
    if (operation == 0 and state.pending == 0) pressPrefix
    else if (operation == 1 and state.pending == 1) pressContinuation
    else if (operation == 2) pressRoot
    else if (operation == 3 and state.pending == 1) deadline(available)
    else if (operation == 4) pressHeld(source)
    else if (operation == 5) releaseHeld(source)
    else cancelTransient
  }

  val pendingIsBounded = state.pending >= 0 and state.pending <= 1
  val deadlineMatchesPending = state.deadlineRequested == (state.pending == 1)
  val heldOwnershipIsExact = state.heldAggregate == aggregate(state.heldA, state.heldB)
  val dispatchIsAtMostOnce = and {
    state.invocations >= 0,
    state.invocations <= state.acceptedPresses,
  }
  val inputCommandSafe = and {
    pendingIsBounded,
    deadlineMatchesPending,
    heldOwnershipIsExact,
    dispatchIsAtMostOnce,
  }
}

module inputCommandTest {
  import inputCommand.*

  run terminalPrefixWitness =
    init
      .then(pressPrefix)
      .then(deadline(true))
      .expect(and {
        state.pending == 0,
        not(state.deadlineRequested),
        state.invocations == 1,
        state.lastCommand == 1,
      })

  run unavailableDeadlineDoesNotDispatch =
    init
      .then(pressPrefix)
      .then(deadline(false))
      .expect(and { state.pending == 0, state.invocations == 0 })

  run continuationCancelsTerminal =
    init
      .then(pressPrefix)
      .then(pressContinuation)
      .expect(and { state.pending == 0, state.invocations == 1, state.lastCommand == 2 })

  run nonmatchRetriesAtRootOnce =
    init
      .then(pressPrefix)
      .then(pressRoot)
      .expect(and { state.pending == 0, state.invocations == 1, state.lastCommand == 3 })

  run lostReleaseWitness =
    init
      .then(pressHeld(1))
      .then(pressHeld(2))
      .then(cancelTransient)
      .then(releaseHeld(1))
      .expect(and {
        not(state.heldA),
        not(state.heldB),
        not(state.heldAggregate),
      })

  run cancellationNeutralizesBeforeLostRelease =
    init
      .then(pressHeld(1))
      .then(cancelTransient)
      .expect(and {
        not(state.heldA),
        not(state.heldB),
        not(state.heldAggregate),
      })

  run oneSourceReleasePreservesOtherOwner =
    init
      .then(pressHeld(1))
      .then(pressHeld(2))
      .then(releaseHeld(1))
      .expect(and { not(state.heldA), state.heldB, state.heldAggregate })
}
