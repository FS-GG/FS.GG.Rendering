module retainedInteraction {
  // One shared-state reducer. Browser events and game commands are outside this model.
  type State = { revision: int, selected: int }
  type Transition = { state: State, outcome: int }

  var state: State
  var outcome: int

  pure def reduceSelect(s: State, expectedRevision: int, objectId: int): Transition =
    if (expectedRevision != s.revision)
      { state: s, outcome: 1 }
    else if (objectId < 1 or objectId > 2)
      { state: s, outcome: 2 }
    else
      { state: { ...s, selected: objectId }, outcome: 0 }

  pure def reduceReplace(s: State, candidateRevision: int, objectAAvailable: bool, objectBAvailable: bool): Transition =
    if (candidateRevision <= s.revision)
      { state: s, outcome: 3 }
    else
      { state:
          { revision: candidateRevision,
            selected:
              if (s.selected == 1 and objectAAvailable) 1
              else if (s.selected == 2 and objectBAvailable) 2
              else 0 },
        outcome: 0 }

  action init = all {
    state' = { revision: 0, selected: 0 },
    outcome' = 0,
  }

  action selectStep(expectedRevision: int, objectId: int): bool = {
    val result = reduceSelect(state, expectedRevision, objectId)
    all { state' = result.state, outcome' = result.outcome }
  }

  action replaceStep(candidateRevision: int, objectAAvailable: bool, objectBAvailable: bool): bool = {
    val result = reduceReplace(state, candidateRevision, objectAAvailable, objectBAvailable)
    all { state' = result.state, outcome' = result.outcome }
  }

  action step = {
    nondet operation = Set(0, 1).oneOf()
    nondet revision = 0.to(3).oneOf()
    nondet objectId = 0.to(3).oneOf()
    nondet objectAAvailable = Set(true, false).oneOf()
    nondet objectBAvailable = Set(true, false).oneOf()
    if (operation == 0)
      selectStep(revision, objectId)
    else
      replaceStep(revision, objectAAvailable, objectBAvailable)
  }

  val revisionNeverDecreases = state.revision >= 0
  val currentSelectionWitness = state.selected != 0 and outcome == 0
  val staleSelectionWitness = outcome == 1
}
