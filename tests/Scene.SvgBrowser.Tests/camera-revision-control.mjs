// Self-contained so Playwright evaluates this exact function against the packed host.
export function cameraRevisionControl(w = window) {
  const api = w.svgFoundation;
  const root = w.document.querySelector("[data-scene-root-id]");
  function snapshot(stage) {
    const value = {
      state: api.state().revision,
      observed: api.observe().revision,
      dom: root?.getAttribute("data-scene-revision"),
    };
    if (!root || w.document.querySelector("[data-scene-root-id]") !== root
        || !Number.isSafeInteger(value.state) || value.state < 0
        || value.observed !== value.state || value.dom !== String(value.state)) {
      throw new Error(`camera revision correspondence failed (${stage}): ${JSON.stringify(value)}`);
    }
    return value;
  }
  function unchanged(left, right) {
    return left.state === right.state && left.observed === right.observed && left.dom === right.dom;
  }
  const beforeRevision = snapshot("before");
  if (beforeRevision.state !== 1) throw new Error("camera fixture must start at revision 1");
  const before = api.scenePoint(60, 50);
  const zoom = api.zoomAt(60, 50, 4);
  const zoomCamera = api.state().camera;
  const zoomRevision = snapshot("zoom");
  if (!unchanged(beforeRevision, zoomRevision)) {
    throw new Error(`camera zoom manufactured a scene revision: ${JSON.stringify({ beforeRevision, zoomRevision })}`);
  }
  const after = api.scenePoint(60, 50);
  const anchoredHit = api.hit(60, 50);
  const pan = api.panBy(10, -5);
  const panCamera = api.state().camera;
  const panRevision = snapshot("pan");
  if (!unchanged(beforeRevision, panRevision)) {
    throw new Error(`camera pan manufactured a scene revision: ${JSON.stringify({ beforeRevision, panRevision })}`);
  }
  const movedHit = api.hit(70, 45);
  if (before.x !== 20 || before.y !== 20 || after.x !== 20 || after.y !== 20
      || anchoredHit !== "alpha" || movedHit !== "alpha" || zoom.error || pan.error) {
    throw new Error(`camera anchor/inverse contract failed: ${JSON.stringify({ before, after, anchoredHit, movedHit, zoom, pan })}`);
  }
  if (zoomCamera.zoom !== 4 || zoomCamera.panX !== -20 || zoomCamera.panY !== -30
      || panCamera.zoom !== 4 || panCamera.panX !== -10 || panCamera.panY !== -35) {
    throw new Error(`camera actions did not change the camera as requested: ${JSON.stringify({ zoomCamera, panCamera })}`);
  }
  // A genuine ReplaceScene dispatch proves these same observations detect scene changes.
  const replacement = api.replaceCurrent();
  const replacementRevision = snapshot("replace");
  if (replacement.error || replacementRevision.state !== 2
      || unchanged(panRevision, replacementRevision)) {
    throw new Error(`camera revision positive control failed: ${JSON.stringify({ replacement, replacementRevision })}`);
  }
  return {
    beforeRevision, zoomRevision, panRevision, replacementRevision,
    before, after, anchoredHit, movedHit, zoomCamera, panCamera,
    positiveControl: "actual-replace-scene-advanced-revision",
  };
}
