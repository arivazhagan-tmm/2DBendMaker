namespace BendMaker;

#region struct BendProcessedPart ------------------------------------------------------------------------
public struct BendProcessedPart {
    #region Constructors ---------------------------------------------
    public BendProcessedPart (EBDAlgorithm algorithm, List<PLine> curves, List<BendLine> bendLines, bool isContourChanged = false) {
        mBDAlgorithm = algorithm; mIsContourChanged = isContourChanged;
        (mBendLines, mCurves) = (bendLines, curves);
        mVertices = [];
        if (mIsContourChanged) (mCurves, mBendLines) = GetChangedContour (curves, bendLines);
        mVertices.AddRange (mCurves.Select (c => c.StartPoint));
        if (mIsContourChanged) mVertices.Add (mCurves[^1].EndPoint);
        mVertices.AddRange (mBendLines.Select (c => c.StartPoint));
        mVertices.AddRange (mBendLines.Select (c => c.EndPoint));
        mBound = new Bound ([.. mVertices]);        // Bound of the bent profile
        mCentroid = BendUtils.Centroid (mVertices); // Centroid of the bent profile
    }
    #endregion

    #region Method ---------------------------------------------------
    public (List<PLine>, List<BendLine>) GetChangedContour (List<PLine> curves, List<BendLine> bendLines) {
        List<PLine> plines = []; List<BendLine> bl = []; int i;
        for (i = 1; i <= curves.Count; i++)
            plines.Add (new PLine (new BPoint (curves[i - 1].StartPoint.X, curves[i - 1].StartPoint.Y, i),
                new BPoint (curves[i - 1].EndPoint.X, curves[i - 1].EndPoint.Y, i + 1)));
        i++;
        for (int j = 1; j <= bendLines.Count; j++) {
            bl.Add (new BendLine (new BPoint (bendLines[j - 1].StartPoint.X, bendLines[j - 1].StartPoint.Y, i),
                new BPoint (bendLines[j - 1].EndPoint.X, bendLines[j - 1].EndPoint.Y, i + 1)));
            i += 2;
        }
        return (plines, bl);
    }
    #endregion

    #region Properties -----------------------------------------------
    public readonly bool IsContourChanged => mIsContourChanged;
    public readonly EBDAlgorithm BendDeductionAlgorithm => mBDAlgorithm;
    public readonly List<BPoint> Vertices => mVertices;
    public readonly List<PLine> Curves => mCurves;
    public readonly List<BendLine> BendLines => mBendLines;
    public readonly Bound Bound => mBound;
    public readonly BPoint Centroid => mCentroid;
    #endregion

    #region Private Data ---------------------------------------------
    Bound mBound;
    BPoint mCentroid;
    List<BendLine> mBendLines;
    List<PLine> mCurves;
    List<BPoint> mVertices;
    bool mIsContourChanged = false;
    EBDAlgorithm mBDAlgorithm; // Bend deduction algorithm
    #endregion
}
#endregion