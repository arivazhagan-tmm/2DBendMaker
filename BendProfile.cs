namespace BendMaker;

#region struct BendProfile ------------------------------------------------------------------------
public struct BendProfile {
    #region Constructors ---------------------------------------------
    public BendProfile (EBDAlgorithm algorithm, List<Curve> curves, List<BendLine> bendLines) {
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

    #region Method ---------------------------------------------------
    public double Area (List<BPoint> vertices) {
        int n = vertices.Count; double area = 0;
        for (int i = 0; i < n - 1; i++)
            area += vertices[i].X * vertices[i + 1].Y - vertices[i].Y * vertices[i + 1].X;
        area += vertices[n - 1].X * vertices[0].Y - vertices[n - 1].Y * vertices[0].X;
        return Math.Abs (area) / 2;
    }
    #endregion

    #region Properties -----------------------------------------------
    public readonly EBDAlgorithm BendDeductionAlgorithm => mBDAlgorithm;
    public readonly List<BPoint> Vertices => mVertices;
    public readonly List<Curve> Curves => mCurves;
    public readonly List<BendLine> BendLines => mBendLines;
    public readonly Bound Bound => mBound;
    public readonly BPoint Centroid => mCentroid;
    #endregion

    #region Private Data ---------------------------------------------
    Bound mBound;
    BPoint mCentroid;
    List<BendLine> mBendLines;
    List<Curve> mCurves;
    List<BPoint> mVertices;
    EBDAlgorithm mBDAlgorithm; // Bend deduction algorithm
    #endregion
}
#endregion