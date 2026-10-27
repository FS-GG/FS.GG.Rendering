// Evaluated verbatim against the existing package-backed F# fixture in each browser.
export async function suspensionRecoveryControl(w = window) {
  const f = w.svgFoundation, doc = w.document;
  const check = (ok, code, detail = null) => { if (!ok) throw Object.assign(Error(`suspension recovery ${code}: ${JSON.stringify(detail)}`), { code, detail }); };
  const native = { add: w.EventTarget.prototype.addEventListener, remove: w.EventTarget.prototype.removeEventListener, raf: w.requestAnimationFrame, cancel: w.cancelAnimationFrame };
  let owner = null, fault = null;
  const listeners = [], frames = new Map(), frameLog = [], observers = new Set();
  const owned = (name, action) => { const before = owner; owner = name; try { return action(); } finally { owner = before; } };
  const capture = options => typeof options === 'boolean' ? options : !!options?.capture;
  const census = name => ({ listeners: listeners.filter(r => r.owner === name).length, frames: [...frames.values()].filter(r => r.owner === name).length });
  w.EventTarget.prototype.addEventListener = function(type, callback, options) {
    if (!owner || !callback) return native.add.call(this, type, callback, options);
    let row = listeners.find(r => r.target === this && r.type === type && r.callback === callback && r.capture === capture(options));
    if (!row) {
      row = { owner, target: this, type, callback, capture: capture(options) };
      row.wrapped = function(event) { return owned(row.owner, () => typeof callback === 'function' ? callback.call(this, event) : callback.handleEvent(event)); };
      listeners.push(row);
    }
    return native.add.call(this, type, row.wrapped, options);
  };
  w.EventTarget.prototype.removeEventListener = function(type, callback, options) {
    const index = listeners.findIndex(r => r.target === this && r.type === type && r.callback === callback && r.capture === capture(options));
    if (index < 0) return native.remove.call(this, type, callback, options);
    const row = listeners[index];
    if (fault?.owner === row.owner && fault.kind === 'listener-leak' && (!fault.listener || fault.listener === row)) { fault.listener = row; return; }
    native.remove.call(this, type, row.wrapped, options);
    listeners.splice(index, 1);
  };
  w.requestAnimationFrame = function(callback) {
    if (!owner) return native.raf.call(w, callback);
    const name = owner;
    const shift = fault?.owner === name && fault.kind === 'clock' && fault.shiftNext ? 2000 : 0;
    if (shift) fault.shiftNext = false;
    const id = native.raf.call(w, timestamp => {
      frames.delete(id);
      frameLog.push({ owner: name, native: timestamp, delivered: timestamp - shift });
      // Ownership follows the actual host callback into its next-frame registration.
      owned(name, () => callback(timestamp - shift));
    });
    frames.set(id, { owner: name });
    return id;
  };
  w.cancelAnimationFrame = function(id) {
    if (fault?.owner === frames.get(id)?.owner && fault?.kind === 'frame-leak') return;
    native.cancel.call(w, id); frames.delete(id);
  };
  const frame = (observe = () => null) => new Promise((resolve, reject) => {
    let id;
    const timer = w.setTimeout(() => { native.cancel.call(w, id); observers.delete(id); reject(Error('native frame observation timed out')); }, 10000);
    id = native.raf.call(w, timestamp => {
      observers.delete(id); w.clearTimeout(timer);
      try { check(doc.visibilityState === 'visible', 'FOREGROUND'); resolve({ timestamp, value: observe() }); } catch (error) { reject(error); }
    });
    observers.add(id);
  });
  const rootOf = () => doc.querySelector('#fixture [data-scene-root-id]');
  const scene = () => {
    const root = rootOf(), revision = f.state().revision, dom = root?.getAttribute('data-scene-revision');
    check(root && dom === String(revision), 'SCENE_CORRESPONDENCE');
    return { markup: root.outerHTML, revision, dom, width: root.querySelector('[data-scene-object-id="alpha"] rect')?.getAttribute('width') };
  };
  const receipt = value => ({ revision: value.revision, dom: value.dom, alphaWidth: value.width });
  const sameScene = (before, code) => { const after = scene(); check(JSON.stringify(after) === JSON.stringify(before), code, { before: receipt(before), after: receipt(after) }); };
  const identity = () => {
    const root = rootOf(), layer = root.querySelector('[data-scene-layer-id="units"]'), beta = root.querySelector('[data-scene-object-id="beta"]');
    const nodes = [beta, ...beta.querySelectorAll('*')];
    return () => check(rootOf() === root && root.querySelector('[data-scene-layer-id="units"]') === layer
      && root.querySelector('[data-scene-object-id="beta"]') === beta && beta.isConnected
      && [beta, ...beta.querySelectorAll('*')].every((node, i) => node === nodes[i])
      && beta.querySelectorAll('*').length + 1 === nodes.length, 'UNAFFECTED_IDENTITY');
  };
  const forceRetire = name => {
    for (const [id, row] of frames) if (row.owner === name) { native.cancel.call(w, id); frames.delete(id); }
    for (let i = listeners.length - 1; i >= 0; i--) if (listeners[i].owner === name) {
      const r = listeners[i]; native.remove.call(r.target, r.type, r.wrapped, r.capture); listeners.splice(i, 1);
    }
  };
  const advances = state => state.events.filter(e => e.startsWith('advance:')).map(e => Number(e.slice(8)));
  const disposed = (name, state) => {
    const actual = census(name);
    check(state.disposed && state.requests === 0 && state.listeners === 0, 'DISPOSAL_REPORTED');
    check(actual.listeners === 0, 'DISPOSAL_LISTENER', { actual, reportedListeners: state.listeners });
    check(actual.frames === 0, 'DISPOSAL_FRAME', { actual, reportedFrames: state.frames ?? null });
    return { reported: state, actual };
  };
  async function local(name, cycles, kind = null) {
    f.mount();
    const mounted = owned(name, () => f.sessionMount()), baseline = census(name);
    let final;
    try {
      check(baseline.listeners === 2 && baseline.frames === 1 && mounted.listeners === 2 && mounted.frames === 1, 'LOCAL_OWNERSHIP_POSITIVE');
      await frame(); await frame();
      owned(name, () => f.sessionCompleteCurrent(2, 'initial'));
      const retained = identity(), rows = [];
      for (let cycle = 0; cycle < cycles; cycle++) {
        const pending = owned(name, () => f.sessionDemand()), before = scene();
        check(pending.requests === 1, 'LOCAL_PENDING');
        owned(name, () => w.dispatchEvent(new w.Event('blur')));
        const suspended = f.sessionObserve();
        check(suspended.status === 'Recovering' && suspended.generation === pending.generation + 1
          && suspended.requests === 0 && suspended.frames === 0, 'LOCAL_SUSPENDED');
        check(census(name).listeners === 2 && census(name).frames === 0, 'LOCAL_SUSPENDED_OWNERSHIP');
        sameScene(before, 'SUSPEND_SCENE'); retained();
        owned(name, () => w.dispatchEvent(new w.Event('blur')));
        check(f.sessionObserve().generation === suspended.generation, 'LOCAL_DUPLICATE_BLUR');
        const start = await frame(); let end;
        do { end = await frame(); } while (end.timestamp - start.timestamp < 1100);
        check(advances(f.sessionObserve()).length === advances(suspended).length && f.sessionObserve().requests === 0, 'SUSPENDED_INERT');
        if (kind === 'clock') fault = { owner: name, kind, shiftNext: true };
        const resumed = kind === 'missing-resume' ? f.sessionObserve() : owned(name, () => f.sessionResume());
        check(resumed.status === 'Running' && resumed.frames === 1, 'RESUME_RUNNING', { status: resumed.status, frames: resumed.frames, actual: census(name) });
        const first = await frame(() => f.sessionObserve()), second = await frame(() => f.sessionObserve());
        const oldAdvances = advances(suspended), firstAdvances = advances(first.value), secondAdvances = advances(second.value);
        const expectedElapsed = Math.round((second.timestamp - first.timestamp) * 1000);
        check(first.value.status === 'Running' && firstAdvances.length === oldAdvances.length
          && second.value.status === 'Running' && secondAdvances.length === oldAdvances.length + 1
          && expectedElapsed > 0 && Math.abs(secondAdvances.at(-1) - expectedElapsed) <= 1, 'CLOCK_BOUNDARY', { nativeTimestamps: [first.timestamp, second.timestamp], firstStatus: first.value.status, secondStatus: second.value.status, beforeAdvanceCount: oldAdvances.length, firstAdvanceCount: firstAdvances.length, secondAdvanceCount: secondAdvances.length, expectedElapsed, lastAdvance: secondAdvances.at(-1) ?? null });
        check(second.value.requests === 1 && second.value.frames === 1 && second.value.listeners === 2 && census(name).frames === 1 && census(name).listeners === 2, 'LOCAL_RECOVERED_OWNERSHIP');
        const freshPending = f.sessionObserve(), staleBefore = scene();
        check(pending.generation === freshPending.generation - 1, 'PREVIOUS_GENERATION_MAPPING');
        owned(name, () => kind === 'local-stale' ? f.sessionCompleteCurrent(99, `old-${cycle}`) : f.sessionCompletePrevious(99, `old-${cycle}`));
        const stale = f.sessionObserve();
        sameScene(staleBefore, 'STALE_SCENE');
        check(stale.requests === 1 && stale.revision === freshPending.revision && stale.generation === freshPending.generation, 'STALE_PENDING');
        const revision = 3 + cycle, applied = owned(name, () => f.sessionCompleteCurrent(revision, `fresh-${cycle}`));
        const after = scene();
        check(applied.revision === revision && after.revision === revision && after.width === String(20 + revision) && after.markup !== staleBefore.markup, 'FRESH_APPLIED'); retained();
        check(applied.listeners === 2 && applied.frames === 1 && (applied.requests === 0 || applied.requests === 1) && JSON.stringify(census(name)) === JSON.stringify(baseline), 'LOCAL_CYCLE_BOUNDED');
        rows.push({ cycle, suspended, dwellMilliseconds: end.timestamp - start.timestamp,
          resumed, firstFrame: first, secondFrame: second, expectedElapsedMicroseconds: expectedElapsed,
          stale, applied, before: receipt(before), after: receipt(after), actual: census(name) });
      }
      final = disposed(name, owned(name, () => f.sessionDispose()));
      const terminal = JSON.stringify(f.sessionObserve()), terminalScene = scene(), frameCount = frameLog.filter(r => r.owner === name).length;
      owned(name, () => { f.sessionCompletePrevious(99, 'after-dispose'); f.sessionDemand(); f.sessionDispose(); });
      await frame(); await frame();
      check(JSON.stringify(f.sessionObserve()) === terminal && frameLog.filter(r => r.owner === name).length === frameCount, 'POST_DISPOSE_INERT');
      check(census(name).frames === 0 && census(name).listeners === 0, 'POST_DISPOSE_OWNERSHIP');
      sameScene(terminalScene, 'POST_DISPOSE_SCENE'); retained();
      return { cycles: rows, baseline, mounted, disposed: final };
    } finally { try { owned(name, () => f.sessionDispose()); } finally { forceRetire(name); } }
  }
  async function external(name, cycles, kind = null) {
    f.mount(); owned(name, () => f.externalMount('recovery-epoch'));
    try {
      const baseline = census(name);
      check(baseline.listeners >= 2 && baseline.frames === 0 && f.externalObserve().listeners === 2, 'EXTERNAL_OWNERSHIP_POSITIVE');
      const initial = owned(name, () => f.externalDemand());
      owned(name, () => f.externalComplete(initial.generation, initial.acquisition, initial.epoch, 1, 'initial'));
      const retained = identity(), rows = [];
      for (let cycle = 0; cycle < cycles; cycle++) {
        const old = owned(name, () => f.externalDemand()), before = scene();
        owned(name, () => w.dispatchEvent(new w.Event('blur')));
        const invalidated = f.externalObserve();
        check(invalidated.generation === old.generation + 1 && invalidated.requests === 0
          && invalidated.epoch === old.epoch && invalidated.revision === old.revision && invalidated.listeners === 2 && JSON.stringify(census(name)) === JSON.stringify(baseline), 'EXTERNAL_INVALIDATED');
        sameScene(before, 'EXTERNAL_INVALIDATE_SCENE'); retained();
        const current = owned(name, () => f.externalDemand());
        check(current.requests === 1 && current.acquisition !== old.acquisition && current.generation !== old.generation && current.listeners === 2 && JSON.stringify(census(name)) === JSON.stringify(baseline), 'EXTERNAL_FRESH_PENDING');
        owned(name, () => kind === 'external-stale'
          ? f.externalComplete(current.generation, current.acquisition, current.epoch, 99, `old-${cycle}`)
          : f.externalComplete(old.generation, old.acquisition, old.epoch, 99, `old-${cycle}`));
        const stale = f.externalObserve();
        sameScene(before, 'EXTERNAL_STALE_SCENE');
        check(stale.requests === 1 && stale.acquisition === current.acquisition && stale.revision === current.revision && JSON.stringify(census(name)) === JSON.stringify(baseline), 'EXTERNAL_STALE_PENDING');
        const revision = 2 + cycle;
        const applied = owned(name, () => f.externalComplete(current.generation, current.acquisition, current.epoch, revision, `fresh-${cycle}`));
        const after = scene();
        check(applied.revision === revision && after.revision > before.revision && after.width === String(30 + revision) && after.markup !== before.markup, 'EXTERNAL_FRESH_APPLIED'); retained();
        check(!applied.events.some(e => /^(advance:|pause$|resume$|reset$|step$)/.test(e)), 'EXTERNAL_AUTHORITY');
        check(applied.listeners === 2 && (applied.requests === 0 || applied.requests === 1) && JSON.stringify(census(name)) === JSON.stringify(baseline), 'EXTERNAL_BOUNDED');
        rows.push({ cycle, old, invalidated, current, stale, applied, before: receipt(before), after: receipt(after), actual: census(name) });
      }
      const final = disposed(name, owned(name, () => f.externalDispose()));
      const terminal = JSON.stringify(f.externalObserve()), terminalScene = scene();
      owned(name, () => { f.externalComplete(0, 0, 'recovery-epoch', 99, 'after-dispose'); f.externalDemand(); f.externalDispose(); });
      await frame();
      check(JSON.stringify(f.externalObserve()) === terminal, 'EXTERNAL_POST_DISPOSE_INERT');
      check(census(name).listeners === 0 && census(name).frames === 0, 'EXTERNAL_POST_DISPOSE_OWNERSHIP');
      sameScene(terminalScene, 'EXTERNAL_POST_DISPOSE_SCENE'); retained();
      return { cycles: rows, baseline, reportedHostListeners: 2, inputBridgeListeners: baseline.listeners - 2, disposed: final };
    } finally { try { owned(name, () => f.externalDispose()); } finally { forceRetire(name); } }
  }
  const controls = [];
  async function reject(name, code, action) {
    let caught, evidence;
    try { await action(); } catch (error) { if (error.code !== code) throw error; caught = error.code; evidence = error.detail; }
    finally { fault = null; }
    check(caught === code, 'CONTROL_SURVIVED', name);
    check(census(name).listeners === 0 && census(name).frames === 0, 'CONTROL_CLEANUP', name);
    controls.push({ name, rejectedBy: code, evidence, injectedFaultFrames: frameLog.filter(row => row.owner === name) });
  }
  async function disposalFault(name, kind, externalHost = false) {
    f.mount();
    const mount = externalHost ? () => f.externalMount('recovery-epoch') : () => f.sessionMount();
    const dispose = externalHost ? () => f.externalDispose() : () => f.sessionDispose();
    owned(name, mount);
    try {
      check(census(name).listeners >= 2 && census(name).frames === (externalHost ? 0 : 1), 'RESOURCE_INSTRUMENT_POSITIVE');
      fault = { owner: name, kind };
      // Read before yielding: an uncancelled one-shot RAF must not disappear first.
      disposed(name, owned(name, dispose));
    } finally {
      fault = null;
      try { owned(name, dispose); } finally { forceRetire(name); }
    }
  }
  try {
    await reject('missing-reactivation', 'RESUME_RUNNING', () => local('missing-reactivation', 1, 'missing-resume'));
    await reject('local-stale-misrouting', 'STALE_SCENE', () => local('local-stale-misrouting', 1, 'local-stale'));
    await reject('external-stale-misrouting', 'EXTERNAL_STALE_SCENE', () => external('external-stale-misrouting', 1, 'external-stale'));
    await reject('corrupted-resumed-timestamp', 'CLOCK_BOUNDARY', () => local('corrupted-resumed-timestamp', 1, 'clock'));
    await reject('local-listener-leak', 'DISPOSAL_LISTENER', () => disposalFault('local-listener-leak', 'listener-leak'));
    await reject('external-listener-leak', 'DISPOSAL_LISTENER', () => disposalFault('external-listener-leak', 'listener-leak', true));
    await reject('local-frame-leak', 'DISPOSAL_FRAME', () => disposalFault('local-frame-leak', 'frame-leak'));
    const localResult = await local('local-positive', 6);
    const externalResult = await external('external-positive', 6);
    check(controls.length === 7, 'CONTROL_CENSUS');
    return { result: 'pass', local: localResult, external: externalResult, controls,
      nativeFrameLog: frameLog.filter(row => row.owner.endsWith('-positive')),
      scope: 'Controlled software blur and explicit caller recovery; native timestamps on the normal path. No real OS background, transport, native engine, product, heap, GPU or performance acceptance.' };
  } finally {
    for (const name of new Set([...listeners.map(r => r.owner), ...[...frames.values()].map(r => r.owner)])) forceRetire(name);
    for (const id of observers) native.cancel.call(w, id);
    w.EventTarget.prototype.addEventListener = native.add; w.EventTarget.prototype.removeEventListener = native.remove;
    w.requestAnimationFrame = native.raf; w.cancelAnimationFrame = native.cancel;
    f.mount();
  }
}
