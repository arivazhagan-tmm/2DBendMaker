namespace BendMaker;

#region struct BendProcessedPart ------------------------------------------------------------------------
public struct BendProcessedPart {
    #region Constructors ---------------------------------------------
    public BendProcessedPart (EBDAlgorithm algorithm, List<PLine> curves, List<BendLine> bendLines) {
        mBDAlgorithm = algorithm;
        (mBendLines, mCurves) = (bendLines, curves);
        mVertices = [];
        mVertices.AddRange (mCurves.Select (c => c.StartPoint));
        mVertices.AddRange (mBendLines.Select (c => c.StartPoint));
        mVertices.AddRange (mBendLines.Select (c => c.EndPoint));
        mBound = new Bound ([.. mVertices]);        // Bound of the bent profile
        mCentroid = BendUtils.Centroid (mVertices); // Centroid of the bent profile
    }
    #endregion

    #region Properties -----------------------------------------------
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
    EBDAlgorithm mBDAlgorithm; // Bend deduction algorithm
    #endregion
}
#endregion