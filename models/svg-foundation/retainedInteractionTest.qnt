module retainedInteractionTest {
  import retainedInteraction.* from "./retainedInteraction"

  run currentSelectionTest =
    init
      .then(selectStep(0, 1))
      .expect(state == { revision: 0, selected: 1 } and outcome == 0)

  run staleSelectionTest =
    init
      .then(replaceStep(1, true, true))
      .then(selectStep(0, 2))
      .expect(state == { revision: 1, selected: 0 } and outcome == 1)

  run stableSelectionAcrossRevisionTest =
    init
      .then(selectStep(0, 2))
      .then(replaceStep(1, true, true))
      .expect(state == { revision: 1, selected: 2 } and outcome == 0)

  run removedSelectionAcrossRevisionTest =
    init
      .then(selectStep(0, 2))
      .then(replaceStep(1, true, false))
      .expect(state == { revision: 1, selected: 0 } and outcome == 0)
}
