import polygonClipping from "polygon-clipping";
self.onmessage = ({data}) => {
  try {
    const subjects = data.subjects.map(contour => [contour]);
    const clips = data.clips.map(contour => [contour]);
    const args = [...subjects, ...clips];
    const polygons = data.operation === "difference"
      ? polygonClipping.difference(subjects[0], ...subjects.slice(1), ...clips)
      : polygonClipping[data.operation](...args);
    self.postMessage({operationId:data.operationId, acceptedRevision:data.acceptedRevision, inputContentHash:data.inputContentHash, contours:polygons.flat()});
  } catch (error) { self.postMessage({error:String(error?.message ?? error)}); }
};
