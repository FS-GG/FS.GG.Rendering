// Source-only companion control for the existing Rendering BrowserFixture.
// Fixture focus is a test precondition, never a product journey qualification.
export async function producerFocusCompositionControl(page, expectedRetainedFocus) {
  if (typeof expectedRetainedFocus !== 'boolean') throw new Error('literal expected focus outcome required');
  await page.evaluate(() => window.svgFoundation.mount());
  const button = page.locator('#fixture [data-scene-control-id="alpha"]');
  await button.focus();
  const receipt = await page.evaluate(() => {
    const button = document.querySelector('#fixture [data-scene-control-id="alpha"]');
    const before = {focused: document.activeElement === button, revision: window.svgFoundation.state().revision};
    const changed = window.svgFoundation.replaceCurrent();
    const afterButton = document.querySelector('#fixture [data-scene-control-id="alpha"]');
    return {before, after: {focused: document.activeElement === button,
      sameButton: afterButton === button, connected: button.isConnected,
      revision: window.svgFoundation.state().revision}, sourceResult: changed};
  });
  if (!receipt.before.focused || !receipt.after.sameButton || !receipt.after.connected ||
      receipt.sourceResult.error || receipt.after.revision !== 2) {
    throw new Error('original producer fixture/reconcile precondition failed: ' + JSON.stringify(receipt));
  }
  if (receipt.after.focused !== expectedRetainedFocus) {
    throw new Error('production focus outcome differs from selected hypothesis: ' + JSON.stringify(receipt));
  }
  const additional = [];
  if (expectedRetainedFocus) {
    // Every update traverses the actual compiled product host; no copied DOM ordering implementation.
    for (let iteration = 0; iteration < 100; iteration++) {
      const value = await page.evaluate(() => {
        const button = document.querySelector('#fixture [data-scene-control-id="alpha"]');
        const changed = window.svgFoundation.advanceFocusControl('stable');
        return {error: changed.error, focused: document.activeElement === button, connected: button.isConnected};
      });
      if (value.error || !value.focused || !value.connected) throw new Error('continuous actual scene update lost control focus: ' + JSON.stringify(value));
    }
    additional.push({case: '100-accepted-continuous-scene-updates', updates: 100});
    for (const focused of ['alpha', 'beta']) {
      await page.evaluate(() => window.svgFoundation.mount());
      await page.locator(`#fixture [data-scene-control-id="${focused}"]`).focus();
      const value = await page.evaluate(id => {
        const before = document.querySelector(`#fixture [data-scene-control-id="${id}"]`);
        const changed = window.svgFoundation.advanceFocusControl('reordered');
        return {error: changed.error, focused: document.activeElement === before, connected: before.isConnected,
          order: [...document.querySelectorAll('#fixture [data-scene-control-id]')].map(e => e.dataset.sceneControlId)};
      }, focused);
      if (value.error || !value.focused || !value.connected || JSON.stringify(value.order) !== '["beta","alpha"]') {
        throw new Error('actual scene reorder lost original control focus/order: ' + JSON.stringify(value));
      }
      additional.push({case: 'reorder-active-' + focused, receipt: value});
    }
    for (const mode of ['remove-alpha', 'remove-beta']) {
      await page.evaluate(() => window.svgFoundation.mount());
      await page.locator('#fixture [data-scene-control-id="alpha"]').focus();
      const value = await page.evaluate(selected => {
        const before = document.querySelector('#fixture [data-scene-control-id="alpha"]');
        const changed = window.svgFoundation.advanceFocusControl(selected);
        return {error: changed.error, focused: document.activeElement === before, connected: before.isConnected};
      }, mode);
      if (value.error || (mode === 'remove-alpha' ? value.connected || value.focused : !value.connected || !value.focused)) {
        throw new Error('actual scene removal focus semantics changed: ' + JSON.stringify(value));
      }
      additional.push({case: mode, receipt: value});
    }
    await page.evaluate(() => window.svgFoundation.mount());
  }
  return {scope: 'actual-SvgBrowserHost-ReplaceScene-focus-control', expectedRetainedFocus,
    fixtureOnlyProgrammaticFocus: true, productActivation: 0, nativeJourneyAcceptance: false, receipt, additional};
}
