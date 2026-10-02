# External authority SVG presentation lifecycle

This is the canonical bounded authority for the presentation-only external session reducer. Epochs are
opaque identities represented by `1` and `2`; `0` means unbound. Local mount generation and acquisition
identity are independent. Each acquisition identity is monotonic across reconnects and epoch replacement.
The model owns no engine clock, simulation, command, receipt, authentication, or transport behavior.

Every state retains the complete bounded ownership projection and two ordered effect slots. Effect kinds are
cancel `1`, request `2`, apply `3`, reject `4`, failure `5`, bind `6`, disconnect `7`, generation exhaustion
`8`, dispose `9`, coalesce `10`, and acquisition-identity exhaustion `11`. Each slot also retains generation,
acquisition, epoch, revision and auxiliary payloads. `-1` represents absence in the bounded projection.

```quint externalSession.qnt +=
module externalSession {
  type State = {
    generation: int, epoch: int, status: int, accepted: int,
    nextAcq: int, pendingAcq: int, queued: bool,
    outcomeKind: int, outcomeAcq: int, outcomeEpoch: int, outcomeRevision: int, rejection: int,
    e1kind: int, e1generation: int, e1acq: int, e1epoch: int, e1revision: int, e1aux: int,
    e2kind: int, e2generation: int, e2acq: int, e2epoch: int, e2revision: int, e2aux: int,
  }

  pure val initialState: State = {
    generation: 0, epoch: 0, status: 0, accepted: -1,
    nextAcq: 0, pendingAcq: -1, queued: false,
    outcomeKind: 0, outcomeAcq: -1, outcomeEpoch: 0, outcomeRevision: -1, rejection: 0,
    e1kind: 0, e1generation: -1, e1acq: -1, e1epoch: 0, e1revision: -1, e1aux: -1,
    e2kind: 0, e2generation: -1, e2acq: -1, e2epoch: 0, e2revision: -1, e2aux: -1,
  }

  var state: State
  action init = state' = initialState

  action bind(epoch: int): bool = all {
    state.status != 2, epoch >= 1, epoch <= 2, state.generation < 3,
    state' = {
      ...state,
      generation: state.generation + 1, epoch: epoch, status: 1,
      accepted: if (state.epoch == epoch) state.accepted else -1,
      pendingAcq: -1, queued: false,
      outcomeKind: 0, outcomeAcq: -1, outcomeEpoch: 0, outcomeRevision: -1, rejection: 0,
      e1kind: if (state.pendingAcq >= 0) 1 else 6,
      e1generation: if (state.pendingAcq >= 0) state.generation else state.generation + 1,
      e1acq: if (state.pendingAcq >= 0) state.pendingAcq else -1,
      e1epoch: if (state.pendingAcq >= 0) state.epoch else epoch,
      e1revision: -1, e1aux: if (state.epoch == epoch) 1 else 0,
      e2kind: if (state.pendingAcq >= 0) 6 else 0,
      e2generation: if (state.pendingAcq >= 0) state.generation + 1 else -1,
      e2acq: -1, e2epoch: if (state.pendingAcq >= 0) epoch else 0,
      e2revision: -1, e2aux: if (state.pendingAcq >= 0) (if (state.epoch == epoch) 1 else 0) else -1,
    },
  }

  action demand: bool = all {
    state.status == 1, state.epoch != 0,
    state' = if (state.pendingAcq >= 0) {
      ...state, queued: true,
      e1kind: 10, e1generation: state.generation, e1acq: state.pendingAcq, e1epoch: state.epoch,
      e1revision: -1, e1aux: -1,
      e2kind: 0, e2generation: -1, e2acq: -1, e2epoch: 0, e2revision: -1, e2aux: -1,
    } else if (state.nextAcq < 4) {
      ...state, nextAcq: state.nextAcq + 1, pendingAcq: state.nextAcq,
      e1kind: 2, e1generation: state.generation, e1acq: state.nextAcq, e1epoch: state.epoch,
      e1revision: -1, e1aux: -1,
      e2kind: 0, e2generation: -1, e2acq: -1, e2epoch: 0, e2revision: -1, e2aux: -1,
    } else {
      ...state,
      e1kind: 11, e1generation: -1, e1acq: state.nextAcq, e1epoch: 0,
      e1revision: -1, e1aux: -1,
      e2kind: 0, e2generation: -1, e2acq: -1, e2epoch: 0, e2revision: -1, e2aux: -1,
    },
  }

  action complete(generation: int, acquisition: int, epoch: int, revision: int): bool = {
    val generationOk = generation == state.generation
    val epochOk = epoch == state.epoch
    val acquisitionOk = acquisition == state.pendingAcq
    val current = generationOk and epochOk and acquisitionOk and state.pendingAcq >= 0
    val increasing = revision > state.accepted
    val drain = current and state.queued
    val rejection = if (not(generationOk)) 1 else if (not(epochOk)) 2 else if (not(acquisitionOk)) 3 else if (not(increasing)) 4 else 5
    all {
      state.status != 2,
      state' = {
        ...state,
        accepted: if (current and increasing) revision else state.accepted,
        nextAcq: if (drain) state.nextAcq + 1 else state.nextAcq,
        pendingAcq: if (drain) state.nextAcq else if (current) -1 else state.pendingAcq,
        queued: if (current) false else state.queued,
        outcomeKind: if (current and increasing) 1 else 2,
        outcomeAcq: if (current and increasing) acquisition else -1,
        outcomeEpoch: if (current and increasing) epoch else 0,
        outcomeRevision: if (current and increasing) revision else -1,
        rejection: if (current and increasing) 0 else rejection,
        e1kind: if (current and increasing) 3 else 4,
        e1generation: generation, e1acq: acquisition, e1epoch: epoch,
        e1revision: revision, e1aux: if (current and increasing) -1 else rejection,
        e2kind: if (drain) 2 else 0,
        e2generation: if (drain) state.generation else -1,
        e2acq: if (drain) state.nextAcq else -1,
        e2epoch: if (drain) state.epoch else 0,
        e2revision: -1, e2aux: -1,
      },
    }
  }

  action acquisitionFail(generation: int, acquisition: int, epoch: int, failure: int): bool = {
    val generationOk = generation == state.generation
    val epochOk = epoch == state.epoch
    val acquisitionOk = acquisition == state.pendingAcq
    val current = generationOk and epochOk and acquisitionOk and state.pendingAcq >= 0
    val drain = current and state.queued
    val rejection = if (not(generationOk)) 1 else if (not(epochOk)) 2 else if (not(acquisitionOk)) 3 else 6
    all {
      state.status != 2,
      state' = {
        ...state,
        nextAcq: if (drain) state.nextAcq + 1 else state.nextAcq,
        pendingAcq: if (drain) state.nextAcq else if (current) -1 else state.pendingAcq,
        queued: if (current) false else state.queued,
        outcomeKind: if (current) failure else 2,
        outcomeAcq: if (current) acquisition else -1,
        outcomeEpoch: 0, outcomeRevision: -1,
        rejection: if (current) 0 else rejection,
        e1kind: if (current) 5 else 4,
        e1generation: generation, e1acq: acquisition, e1epoch: epoch,
        e1revision: -1, e1aux: if (current) failure else rejection,
        e2kind: if (drain) 2 else 0,
        e2generation: if (drain) state.generation else -1,
        e2acq: if (drain) state.nextAcq else -1,
        e2epoch: if (drain) state.epoch else 0,
        e2revision: -1, e2aux: -1,
      },
    }
  }

  action presentationCallbackFailure(acquisition: int, epoch: int, revision: int): bool = all {
    state.status != 2, state.outcomeKind == 1,
    state.outcomeAcq == acquisition, state.outcomeEpoch == epoch, state.outcomeRevision == revision,
    state' = {
      ...state,
      outcomeKind: 5, outcomeAcq: acquisition, outcomeEpoch: 0, outcomeRevision: -1,
      e1kind: 5, e1generation: state.generation, e1acq: acquisition, e1epoch: epoch,
      e1revision: revision, e1aux: 5,
      e2kind: 0, e2generation: -1, e2acq: -1, e2epoch: 0, e2revision: -1, e2aux: -1,
    },
  }

  action invalidate: bool = all {
    state.status != 2, state.generation < 3,
    state' = {
      ...state, generation: state.generation + 1, pendingAcq: -1, queued: false,
      e1kind: if (state.pendingAcq >= 0) 1 else 0,
      e1generation: if (state.pendingAcq >= 0) state.generation else -1,
      e1acq: state.pendingAcq, e1epoch: state.epoch, e1revision: -1, e1aux: -1,
      e2kind: 0, e2generation: -1, e2acq: -1, e2epoch: 0, e2revision: -1, e2aux: -1,
    },
  }

  action disconnect: bool = all {
    state.status != 2, state.generation < 3,
    state' = {
      ...state, generation: state.generation + 1, status: 0, pendingAcq: -1, queued: false,
      e1kind: if (state.pendingAcq >= 0) 1 else 7,
      e1generation: if (state.pendingAcq >= 0) state.generation else state.generation + 1,
      e1acq: state.pendingAcq, e1epoch: if (state.pendingAcq >= 0) state.epoch else 0,
      e1revision: -1, e1aux: -1,
      e2kind: if (state.pendingAcq >= 0) 7 else 0,
      e2generation: if (state.pendingAcq >= 0) state.generation + 1 else -1,
      e2acq: -1, e2epoch: 0, e2revision: -1, e2aux: -1,
    },
  }

  action dispose: bool = all {
    state.status != 2,
    state' = {
      ...state, status: 2, pendingAcq: -1, queued: false,
      e1kind: if (state.pendingAcq >= 0) 1 else 9,
      e1generation: state.generation, e1acq: state.pendingAcq,
      e1epoch: if (state.pendingAcq >= 0) state.epoch else 0, e1revision: -1, e1aux: -1,
      e2kind: if (state.pendingAcq >= 0) 9 else 0,
      e2generation: if (state.pendingAcq >= 0) state.generation else -1,
      e2acq: -1, e2epoch: 0, e2revision: -1, e2aux: -1,
    },
  }

  action generationExhaust: bool = all {
    state.status != 2, state.generation == 3,
    state' = {
      ...state,
      e1kind: 8, e1generation: 3, e1acq: -1, e1epoch: 0, e1revision: -1, e1aux: -1,
      e2kind: 0, e2generation: -1, e2acq: -1, e2epoch: 0, e2revision: -1, e2aux: -1,
    },
  }
}

module externalSessionTest {
  import externalSession.*
  run normalFlow = init.then(bind(1)).then(demand).then(complete(1, 0, 1, 1))
    .expect(and { state.accepted == 1, state.pendingAcq == -1, state.e1kind == 3, state.e1acq == 0 })
  run duplicateCannotDrainNext = init.then(bind(1)).then(demand).then(complete(1, 0, 1, 1)).then(demand)
    .then(complete(1, 0, 1, 1)).then(complete(1, 1, 1, 2))
    .expect(and { state.accepted == 2, state.pendingAcq == -1, state.outcomeAcq == 1 })
  run currentRejectionDrainsQueued = init.then(bind(1)).then(demand).then(complete(1, 0, 1, 2))
    .then(demand).then(demand).then(complete(1, 1, 1, 2))
    .expect(and { state.pendingAcq == 2, not(state.queued), state.e1kind == 4, state.e2kind == 2, state.e2acq == 2 })
  run oldFailureCannotDrainReplacement = init.then(bind(1)).then(demand).then(demand)
    .then(acquisitionFail(1, 0, 1, 3)).then(acquisitionFail(1, 0, 1, 3))
    .expect(and { state.pendingAcq == 1, state.e1kind == 4, state.rejection == 3 })
  run lateCompletionAfterLostCannotApply = init.then(bind(1)).then(demand).then(demand)
    .then(acquisitionFail(1, 0, 1, 3)).then(complete(1, 0, 1, 2))
    .expect(and { state.accepted == -1, state.pendingAcq == 1, state.e1kind == 4, state.rejection == 3 })
  run sameEpochPreservesBaseline = init.then(bind(1)).then(demand).then(complete(1, 0, 1, 2)).then(bind(1))
    .expect(and { state.generation == 2, state.accepted == 2, state.nextAcq == 1 })
  run newEpochRestartsBaseline = init.then(bind(1)).then(demand).then(complete(1, 0, 1, 2)).then(bind(2))
    .expect(and { state.generation == 2, state.accepted == -1, state.nextAcq == 1 })
  run staleGenerationCannotApply = init.then(bind(1)).then(demand).then(complete(0, 0, 1, 1))
    .expect(and { state.pendingAcq == 0, state.rejection == 1, state.e1generation == 0 })
  run staleEpochCannotApply = init.then(bind(1)).then(demand).then(complete(1, 0, 2, 1))
    .expect(and { state.pendingAcq == 0, state.rejection == 2, state.e1epoch == 2 })
  run lostThenQueuedReplacement = init.then(bind(1)).then(demand).then(demand).then(acquisitionFail(1, 0, 1, 3))
    .expect(and { state.pendingAcq == 1, state.outcomeKind == 3, state.e1kind == 5, state.e2kind == 2 })
  run callbackFailureIsExplicit = init.then(bind(1)).then(demand).then(complete(1, 0, 1, 2))
    .then(presentationCallbackFailure(0, 1, 2))
    .expect(and { state.accepted == 2, state.outcomeKind == 5, state.e1kind == 5, state.e1acq == 0 })
  run invalidateCancelsExactRequest = init.then(bind(1)).then(demand).then(invalidate)
    .expect(and { state.generation == 2, state.pendingAcq == -1, state.e1kind == 1, state.e1acq == 0 })
  run disposeCancelsAndTerminates = init.then(bind(1)).then(demand).then(dispose)
    .expect(and { state.status == 2, state.pendingAcq == -1, state.e1kind == 1, state.e2kind == 9 })
  run generationExhaustionRefusesWrap = init.then(bind(1)).then(bind(1)).then(bind(1)).then(generationExhaust)
    .expect(and { state.generation == 3, state.e1kind == 8, state.e1generation == 3 })
  run acquisitionExhaustionRefusesWrap = init.then(bind(1))
    .then(demand).then(complete(1, 0, 1, 1))
    .then(demand).then(complete(1, 1, 1, 2))
    .then(demand).then(complete(1, 2, 1, 3))
    .then(demand).then(complete(1, 3, 1, 4)).then(demand)
    .expect(and { state.nextAcq == 4, state.pendingAcq == -1, state.e1kind == 11, state.e1acq == 4 })
}
```

The bounded model uses two epochs, four acquisition identities, revisions one through four, and three ordinary
mount generations plus the exhaustion boundary. Directed traces are finite safety witnesses; they do not
claim temporal liveness, transport delivery, authentication, or product command authority.
