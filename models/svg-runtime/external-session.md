# External authority SVG presentation lifecycle

This is the canonical bounded authority for the presentation-only external session reducer. Epochs are
opaque identities represented by `1` and `2`; `0` means unbound. Revisions are bounded samples. Local
mount generation is deliberately separate from epoch. Effect codes preserve order: cancel `1`, request
`2`, apply `3`, reject `4`, fail `5`, bind `6`, disconnect `7`, exhausted `8`, dispose `9`, coalesce `10`.

The model owns no engine clock, simulation, command, receipt, authentication, or transport behavior.
Commands and receipts remain ordered at the product gateway outside presentation coalescing.

```quint externalSession.qnt +=
module externalSession {
  type State = {
    generation: int,
    epoch: int,
    status: int,
    accepted: int,
    pending: bool,
    queued: bool,
    outcome: int,
    effect1: int,
    effect2: int,
  }

  pure val initialState: State = {
    generation: 0, epoch: 0, status: 0, accepted: -1,
    pending: false, queued: false, outcome: 0, effect1: 0, effect2: 0,
  }

  var state: State
  action init = state' = initialState

  action bind(epoch: int): bool = all {
    state.status != 2,
    epoch >= 1,
    epoch <= 2,
    state.generation < 3,
    state' = {
      ...state,
      generation: state.generation + 1,
      epoch: epoch,
      status: 1,
      accepted: if (state.epoch == epoch) state.accepted else -1,
      pending: false,
      queued: false,
      outcome: 0,
      effect1: if (state.pending) 1 else 6,
      effect2: if (state.pending) 6 else 0,
    },
  }

  action demand: bool = all {
    state.status == 1,
    state.epoch != 0,
    state' = {
      ...state,
      pending: true,
      queued: state.pending,
      effect1: if (state.pending) 10 else 2,
      effect2: 0,
    },
  }

  action complete(generation: int, epoch: int, revision: int): bool = {
    val current = generation == state.generation and epoch == state.epoch and state.pending
    val increasing = revision > state.accepted
    all {
      state.status != 2,
      state' = {
        ...state,
        accepted: if (current and increasing) revision else state.accepted,
        pending: if (current) state.queued else state.pending,
        queued: if (current) false else state.queued,
        outcome: if (current and increasing) 1 else 2,
        effect1: if (current and increasing) 3 else 4,
        effect2: if (current and state.queued) 2 else 0,
      },
    }
  }

  action acquisitionFail(generation: int, epoch: int, failure: int): bool = {
    val current = generation == state.generation and epoch == state.epoch and state.pending
    all {
      state.status != 2,
      state' = {
        ...state,
        pending: if (current) state.queued else state.pending,
        queued: if (current) false else state.queued,
        outcome: if (current) failure else 2,
        effect1: if (current) 5 else 4,
        effect2: if (current and state.queued) 2 else 0,
      },
    }
  }

  action presentationCallbackFailure(epoch: int, revision: int): bool = all {
    state.status != 2,
    state.epoch == epoch,
    state.accepted == revision,
    state' = { ...state, outcome: 5, effect1: 5, effect2: 0 },
  }

  action invalidate: bool = all {
    state.status != 2,
    state.generation < 3,
    state' = {
      ...state,
      generation: state.generation + 1,
      pending: false,
      queued: false,
      effect1: if (state.pending) 1 else 0,
      effect2: 0,
    },
  }

  action disconnect: bool = all {
    state.status != 2,
    state.generation < 3,
    state' = {
      ...state,
      generation: state.generation + 1,
      status: 0,
      pending: false,
      queued: false,
      effect1: if (state.pending) 1 else 7,
      effect2: if (state.pending) 7 else 0,
    },
  }

  action dispose: bool = all {
    state.status != 2,
    state' = {
      ...state,
      status: 2,
      pending: false,
      queued: false,
      effect1: if (state.pending) 1 else 9,
      effect2: if (state.pending) 9 else 0,
    },
  }

  action exhaust: bool = all {
    state.status != 2,
    state.generation == 3,
    state' = { ...state, effect1: 8, effect2: 0 },
  }

  action step = {
    nondet operation = 0.to(6).oneOf()
    nondet epoch = 1.to(2).oneOf()
    nondet revision = 0.to(2).oneOf()
    if (operation == 0 and state.generation < 3) bind(epoch)
    else if (operation == 1 and state.status == 1) demand
    else if (operation == 2) complete(state.generation, epoch, revision)
    else if (operation == 3) acquisitionFail(state.generation, epoch, 3)
    else if (operation == 4 and state.generation < 3) invalidate
    else if (operation == 5 and state.generation < 3) disconnect
    else dispose
  }

  val safe = and {
    state.generation >= 0,
    state.generation <= 3,
    state.epoch >= 0,
    state.epoch <= 2,
    not(state.queued) or state.pending,
    state.status != 2 or not(state.pending),
    state.status != 2 or not(state.queued),
  }
}

module externalSessionTest {
  import externalSession.*

  run normalFlow = init.then(bind(1)).then(demand).then(complete(1, 1, 1))
    .expect(and { state.accepted == 1, not(state.pending), state.effect1 == 3 })

  run sameEpochPreservesBaseline = init.then(bind(1)).then(demand).then(complete(1, 1, 2)).then(bind(1))
    .expect(and { state.generation == 2, state.accepted == 2, state.effect1 == 6 })

  run newEpochRestartsBaseline = init.then(bind(1)).then(demand).then(complete(1, 1, 2)).then(bind(2))
    .expect(and { state.generation == 2, state.accepted == -1, state.epoch == 2 })

  run staleEpochCannotApply = init.then(bind(1)).then(demand).then(complete(1, 2, 1))
    .expect(and { state.accepted == -1, state.pending, state.effect1 == 4 })

  run queuedAfterRevisionRejection = init.then(bind(1)).then(demand).then(complete(1, 1, 2))
    .then(demand).then(demand).then(complete(1, 1, 1))
    .expect(and { state.accepted == 2, state.pending, not(state.queued), state.effect1 == 4, state.effect2 == 2 })

  run lostThenQueuedReplacement = init.then(bind(1)).then(demand).then(demand).then(acquisitionFail(1, 1, 3))
    .expect(and { state.pending, not(state.queued), state.outcome == 3, state.effect1 == 5, state.effect2 == 2 })

  run cancelledThenQueuedReplacement = init.then(bind(1)).then(demand).then(demand).then(acquisitionFail(1, 1, 4))
    .expect(and { state.pending, not(state.queued), state.outcome == 4, state.effect1 == 5, state.effect2 == 2 })

  run callbackFailureThenQueuedReplacement = init.then(bind(1)).then(demand).then(demand).then(acquisitionFail(1, 1, 5))
    .expect(and { state.pending, not(state.queued), state.outcome == 5, state.effect1 == 5, state.effect2 == 2 })

  run presentationCallbackFailureIsExplicit = init.then(bind(1)).then(demand).then(complete(1, 1, 2))
    .then(presentationCallbackFailure(1, 2))
    .expect(and { state.accepted == 2, not(state.pending), state.outcome == 5, state.effect1 == 5 })

  run invalidateCancels = init.then(bind(1)).then(demand).then(invalidate)
    .expect(and { state.generation == 2, not(state.pending), state.effect1 == 1 })

  run disposeCancelsAndTerminates = init.then(bind(1)).then(demand).then(dispose)
    .expect(and { state.status == 2, not(state.pending), state.effect1 == 1, state.effect2 == 9 })

  run generationExhaustionRefusesWrap = init.then(bind(1)).then(bind(1)).then(bind(1)).then(exhaust)
    .expect(and { state.generation == 3, state.epoch == 1, state.effect1 == 8 })
}
```

The bounded domain covers two epochs, revisions zero through two, and three mount generations. Directed
witnesses cover normal flow, reconnect, replacement, stale completion, current rejection with queued
replacement, lost/cancelled/callback-failed acquisition, invalidation, disposal, and generation exhaustion.
Production tests separately cover the actual `uint64` exhaustion boundary and larger revisions.
