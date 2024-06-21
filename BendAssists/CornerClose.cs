namespace BendMaker;

#region class CornerClose -------------------------------------------------------------------------
public class CornerClose {
   #region Constructors ---------------------------------------------
   public CornerClose () => (mIndex, mStepLines, mEdgeLines, mNewLines, mStepStartPts, mStepEndPts) = (-1, [], [], [], [], []);
   #endregion

   #region Methods --------------------------------------------------
   /// <summary>Checks if a part is eligible for corner closing and outs the corner closed part</summary>
   public bool ApplyCornerClosing (Part part, out BendProcessedPart cornerClosedPart) {
      cornerClosedPart = new BendProcessedPart ();
      var vertices = part.Vertices; var pLines = part.PLines;
      var commonVertices = vertices.GroupBy (p => p).Where (p => p.Count () > 2).Select (g => g.Key).ToList (); // To find the intersecting points of bendlines with stepcut 
      if (commonVertices.Count < 1) return false;
      else {
         var halfBA = BendUtils.GetBendAllowance (90, 0.38, 2, 2) / 2;
         var lineShift = halfBA - 2;
         var halfBD = BendUtils.GetBendDeduction (90, 0.38, 2, 2) / 2;
         foreach (var pLine in pLines)
            if (commonVertices.Contains (pLine.StartPoint) || commonVertices.Contains (pLine.EndPoint)) mStepLines.Add (pLine);
            else mEdgeLines.Add (pLine);
         foreach (var stepLine in mStepLines) {
            mStepStartPts.Add (stepLine.StartPoint);
            mStepEndPts.Add (stepLine.EndPoint);
         }
         for (int i = 0; i < pLines.Count; i++) {
            var pLine = pLines[i];
            if (mEdgeLines.Contains (pLine)) {
               PLine trimmedEdge;
               if (mStepStartPts.Contains (pLine.EndPoint) && mStepEndPts.Contains (pLine.StartPoint)) {
                  if (pLine.Orientation is ECurveOrientation.Horizontal) {
                     trimmedEdge = (pLine.StartPoint.Y < part.Centroid.Y) ? pLine.Trimmed (-halfBD, 0, -lineShift, 0)
                                                                             : pLine.Trimmed (halfBD, 0, lineShift, 0);
                     mNewLines.Insert (++mIndex, trimmedEdge);
                  } else if (pLine.Orientation is ECurveOrientation.Vertical) {
                     trimmedEdge = (pLine.StartPoint.X < part.Centroid.X) ? pLine.Trimmed (0, halfBD, 0, lineShift)
                                                                             : pLine.Trimmed (0, -halfBD, 0, -lineShift);
                     mNewLines.Insert (++mIndex, trimmedEdge);
                  }
               } else if (mStepStartPts.Contains (pLine.EndPoint)) {
                  if (pLine.Orientation is ECurveOrientation.Horizontal) {
                     trimmedEdge = (pLine.StartPoint.Y < part.Centroid.Y) ? pLine.Trimmed (0, 0, -lineShift, 0)
                                                                             : pLine.Trimmed (0, 0, lineShift, 0);
                     mNewLines.Insert (++mIndex, trimmedEdge);
                  } else if (pLine.Orientation is ECurveOrientation.Vertical) {
                     trimmedEdge = (pLine.StartPoint.X < part.Centroid.X) ? pLine.Trimmed (0, 0, 0, lineShift)
                                                                             : pLine.Trimmed (0, 0, 0, -lineShift);
                     mNewLines.Insert (++mIndex, trimmedEdge);
                  }
               } else if (mStepEndPts.Contains (pLine.StartPoint)) {
                  if (pLine.Orientation is ECurveOrientation.Horizontal) {
                     trimmedEdge = (pLine.StartPoint.Y < part.Centroid.Y) ? pLine.Trimmed (-halfBD, 0, 0, 0)
                                                                             : pLine.Trimmed (halfBD, 0, 0, 0);
                     mNewLines.Insert (++mIndex, trimmedEdge);
                  } else if (pLine.Orientation is ECurveOrientation.Vertical) {
                     trimmedEdge = (pLine.StartPoint.X < part.Centroid.X) ? pLine.Trimmed (0, halfBD, 0, 0)
                                                                             : pLine.Trimmed (0, -halfBD, 0, 0);
                     mNewLines.Insert (++mIndex, trimmedEdge);
                  }
               } else mNewLines.Insert (++mIndex, pLine);
            } else {
               if (mStepLines.IndexOf (pLine) % 2 == 0) {
                  PLine translatedLine;
                  if (pLine.Orientation is ECurveOrientation.Horizontal) {
                     translatedLine = (pLine.EndPoint.Y < part.Centroid.Y) ? pLine.Translated (0, lineShift) : pLine.Translated (0, -lineShift);
                     mNewLines.Insert (++mIndex, translatedLine);
                  } else if (pLine.Orientation is ECurveOrientation.Vertical) {
                     translatedLine = (pLine.EndPoint.X < part.Centroid.X) ? pLine.Translated (lineShift, 0) : pLine.Translated (-lineShift, 0);
                     mNewLines.Insert (++mIndex, translatedLine);
                  }
               } else {
                  PLine trimmedLine, extrudedLine;
                  if (pLine.Orientation is ECurveOrientation.Horizontal) {
                     if (pLine.StartPoint.X < part.Centroid.X) {
                        extrudedLine = pLine.Trimmed (-halfBA, 0, 0, 0).Translated (0, halfBD);
                        trimmedLine = pLine.Trimmed (lineShift, 0, pLine.Length - halfBA, 0);
                     } else {
                        extrudedLine = pLine.Trimmed (halfBA, 0, 0, 0).Translated (0, -halfBD);
                        trimmedLine = pLine.Trimmed (-lineShift, 0, -(pLine.Length - halfBA), 0);
                     }
                     mNewLines.Insert (++mIndex, trimmedLine);
                     mNewLines.Add (new PLine (ECurve.Line, ++mIndex, trimmedLine.EndPoint, extrudedLine.StartPoint));
                     mNewLines.Insert (++mIndex, extrudedLine);
                  } else if (pLine.Orientation is ECurveOrientation.Vertical) {
                     if (pLine.StartPoint.Y < part.Centroid.Y) {
                        extrudedLine = pLine.Trimmed (0, -halfBA, 0, 0).Translated (-halfBD, 0);
                        trimmedLine = pLine.Trimmed (0, lineShift, 0, pLine.Length - halfBA);
                     } else {
                        extrudedLine = pLine.Trimmed (0, halfBA, 0, 0).Translated (halfBD, 0);
                        trimmedLine = pLine.Trimmed (0, -lineShift, 0, -(pLine.Length - halfBA));
                     }
                     mNewLines.Insert (++mIndex, trimmedLine);
                     mNewLines.Add (new PLine (ECurve.Line, ++mIndex, trimmedLine.EndPoint, extrudedLine.StartPoint));
                     mNewLines.Insert (++mIndex, extrudedLine);
                  }
               }
            }
         }
         cornerClosedPart = new BendProcessedPart (EBDAlgorithm.Unknown, mNewLines, part.BendLines, true);
         return true;
      }
   }
   #endregion

   #region Private Data ---------------------------------------------
   List<PLine> mStepLines, mEdgeLines, mNewLines;
   List<BPoint> mStepStartPts, mStepEndPts;
   int mIndex;
   #endregion
}
#endregion