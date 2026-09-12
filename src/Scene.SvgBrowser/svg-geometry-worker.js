import polygonClipping from "polygon-clipping";

self.onmessage = ({ data }) => {
  try {
    // polygon-clipping models a polygon as a list of rings. The portable wire
    // format sends one winding contour per list entry, so wrap each contour as
    // one polygon before invoking the library.
    const subjects = data.subjects.map(contour => [contour]);
    const clips = data.clips.map(contour => [contour]);
    const args = [...subjects, ...clips];
    let contours;
    if (data.operation === "union") contours = polygonClipping.union(...args);
    else if (data.operation === "intersection") contours = polygonClipping.intersection(...args);
    else if (data.operation === "difference") contours = polygonClipping.difference(subjects[0], ...subjects.slice(1), ...clips);
    else if (data.operation === "xor") contours = polygonClipping.xor(...args);
    else throw new Error("unsupported polygon operation");
    const flattened = contours.flat();
    const vertexCount = flattened.reduce((count, contour) => count + contour.length, 0);
    if (flattened.length > 128 || vertexCount > 20000) throw new Error("geometry result exceeds contour or vertex budget");
    self.postMessage({ operationId:data.operationId, acceptedRevision:data.acceptedRevision, inputContentHash:data.inputContentHash, contours:flattened });
  } catch (error) {
    self.postMessage({ operationId:data.operationId, acceptedRevision:data.acceptedRevision, inputContentHash:data.inputContentHash, error:String(error?.message ?? error) });
  }
};
