namespace BendMaker;

#region class CornerClose --------------------------------------------------------------------------
internal class CornerClose {
    #region Constructors ---------------------------------------------
    public CornerClose () => (mIndex, mStepCurves, mEdgeCurves, mNewCurves, mStepStartPts, mStepEndPts) = (-1, [], [], [], [], []);
    #endregion

    #region Methods --------------------------------------------------
    public BendProcessedPart ApplyCornerClosing (Part profile) {
        var vertices = profile.Vertices; var curves = profile.PLines;
        var halfBA = BendUtils.GetBendAllowance (90, 0.38, 2, 2) / 2;
        var curveShift = halfBA - 2;
        var halfBD = BendUtils.GetBendDeduction (90, 0.38, 2, 2) / 2;
        var repeatedPoints = vertices.GroupBy (p => p).Where (p => p.Count () > 2).Select (g => g.Key).ToList ();
        foreach (var curve in curves)
            if (repeatedPoints.Contains (curve.StartPoint) || repeatedPoints.Contains (curve.EndPoint)) mStepCurves.Add (curve);
            else mEdgeCurves.Add (curve);
        mStepStartPts = GetStartPoints (mStepCurves, mStepStartPts);
        mStepEndPts = GetEndPoints (mStepCurves, mStepEndPts);
        for (int c = 0; c < curves.Count; c++) {
            var curve = curves[c];
            if (mEdgeCurves.Contains (curve)) {
                PLine trimmedEdge;
                if (mStepStartPts.Contains (curve.EndPoint) && mStepEndPts.Contains (curve.StartPoint)) {
                    if (curve.Orientation is ECurveOrientation.Horizontal) {
                        trimmedEdge = (curve.StartPoint.Y < profile.Centroid.Y) ? curve.Trimmed (-halfBD, 0, -curveShift, 0)
                                                                                : curve.Trimmed (halfBD, 0, curveShift, 0);
                        trimmedEdge.Index = ++mIndex;
                        mNewCurves.Add (trimmedEdge);
                    } else if (curve.Orientation is ECurveOrientation.Vertical) {
                        trimmedEdge = (curve.StartPoint.X < profile.Centroid.X) ? curve.Trimmed (0, halfBD, 0, curveShift)
                                                                                : curve.Trimmed (0, -halfBD, 0, -curveShift);
                        trimmedEdge.Index = ++mIndex;
                        mNewCurves.Add (trimmedEdge);
                    }
                } else if (mStepStartPts.Contains (curve.EndPoint)) {
                    if (curve.Orientation is ECurveOrientation.Horizontal) {
                        trimmedEdge = (curve.StartPoint.Y < profile.Centroid.Y) ? curve.Trimmed (0, 0, -curveShift, 0)
                                                                                : curve.Trimmed (0, 0, curveShift, 0);
                        trimmedEdge.Index = ++mIndex;
                        mNewCurves.Add (trimmedEdge);
                    } else if (curve.Orientation is ECurveOrientation.Vertical) {
                        trimmedEdge = (curve.StartPoint.X < profile.Centroid.X) ? curve.Trimmed (0, 0, 0, curveShift)
                                                                                : curve.Trimmed (0, 0, 0, -curveShift);
                        trimmedEdge.Index = ++mIndex;
                        mNewCurves.Add (trimmedEdge);
                    }
                } else if (mStepEndPts.Contains (curve.StartPoint)) {
                    if (curve.Orientation is ECurveOrientation.Horizontal) {
                        trimmedEdge = (curve.StartPoint.Y < profile.Centroid.Y) ? curve.Trimmed (-halfBD, 0, 0, 0)
                                                                                : curve.Trimmed (halfBD, 0, 0, 0);
                        trimmedEdge.Index = ++mIndex;
                        mNewCurves.Add (trimmedEdge);
                    } else if (curve.Orientation is ECurveOrientation.Vertical) {
                        trimmedEdge = (curve.StartPoint.X < profile.Centroid.X) ? curve.Trimmed (0, halfBD, 0, 0)
                                                                                : curve.Trimmed (0, -halfBD, 0, 0);
                        trimmedEdge.Index = ++mIndex;
                        mNewCurves.Add (trimmedEdge);
                    }
                } else {
                    curve.Index = ++mIndex;
                    mNewCurves.Add (curve);
                }
            } else {
                if (mStepCurves.IndexOf (curve) % 2 == 0) {
                    PLine translatedCurve;
                    if (curve.Orientation is ECurveOrientation.Horizontal) {
                        translatedCurve = (curve.EndPoint.Y < profile.Centroid.Y) ? curve.Translated (0, curveShift) : curve.Translated (0, -curveShift);
                        translatedCurve.Index = ++mIndex;
                        mNewCurves.Add (translatedCurve);
                    } else if (curve.Orientation is ECurveOrientation.Vertical) {
                        translatedCurve = (curve.EndPoint.X < profile.Centroid.X) ? curve.Translated (curveShift, 0) : curve.Translated (-curveShift, 0);
                        translatedCurve.Index = ++mIndex;
                        mNewCurves.Add (translatedCurve);
                    }
                } else {
                    PLine trimmedCurve, extrudedCurve;
                    if (curve.Orientation is ECurveOrientation.Horizontal) {
                        if (curve.StartPoint.X < profile.Centroid.X) {
                            extrudedCurve = curve.Trimmed (-halfBA, 0, 0, 0).Translated (0, halfBD);
                            trimmedCurve = curve.Trimmed (curveShift, 0, curve.Length - halfBA, 0);
                        } else {
                            extrudedCurve = curve.Trimmed (halfBA, 0, 0, 0).Translated (0, -halfBD);
                            trimmedCurve = curve.Trimmed (-curveShift, 0, -(curve.Length - halfBA), 0);
                        }
                        trimmedCurve.Index = ++mIndex;
                        mNewCurves.Add (trimmedCurve);
                        mNewCurves.Add (new PLine (ECurve.Line, ++mIndex, trimmedCurve.EndPoint, extrudedCurve.StartPoint));
                        extrudedCurve.Index = ++mIndex;
                        mNewCurves.Add (extrudedCurve);
                    } else if (curve.Orientation is ECurveOrientation.Vertical) {
                        if (curve.StartPoint.Y < profile.Centroid.Y) {
                            extrudedCurve = curve.Trimmed (0, -halfBA, 0, 0).Translated (-halfBD, 0);
                            trimmedCurve = curve.Trimmed (0, curveShift, 0, curve.Length - halfBA);
                        } else {
                            extrudedCurve = curve.Trimmed (0, halfBA, 0, 0).Translated (halfBD, 0);
                            trimmedCurve = curve.Trimmed (0, -curveShift, 0, -(curve.Length - halfBA));
                        }
                        trimmedCurve.Index = ++mIndex;
                        mNewCurves.Add (trimmedCurve);
                        mNewCurves.Add (new PLine (ECurve.Line, ++mIndex, trimmedCurve.EndPoint, extrudedCurve.StartPoint));
                        extrudedCurve.Index = ++mIndex;
                        mNewCurves.Add (extrudedCurve);
                    }
                }
            }
        }
        return new BendProcessedPart (EBDAlgorithm.Unknown, mNewCurves, profile.BendLines, true);
    }
    #endregion

    #region Implementation -------------------------------------------
    List<BPoint> GetStartPoints (List<PLine> curves, List<BPoint> bPoints) {
        foreach (PLine curve in curves) bPoints.Add (curve.StartPoint);
        return bPoints;
    }

    List<BPoint> GetEndPoints (List<PLine> curves, List<BPoint> bPoints) {
        foreach (PLine curve in curves) bPoints.Add (curve.EndPoint);
        return bPoints;
    }
    #endregion

    #region Private Data ---------------------------------------------
    List<PLine> mStepCurves, mEdgeCurves, mNewCurves;
    List<BPoint> mStepStartPts, mStepEndPts;
    int mIndex;
    #endregion
}
#endregion