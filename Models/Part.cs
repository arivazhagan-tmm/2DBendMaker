namespace BendMaker;

#region struct Profile ----------------------------------------------------------------------------
public struct Part {
    #region Constructors ---------------------------------------------
    public Part (List<PLine> curves, List<BendLine> bendLines,string materialType, double thickness) {
        mBendLines = bendLines.OrderBy (bl => bl.StartPoint.Y).ThenBy (bl => bl.StartPoint.X).ToList ();
        (mPLines, mVertices) = (curves, []);
        foreach (var c in mPLines) mVertices.Add (c.StartPoint);
        mCentroid = BendUtils.Centroid (mVertices);
        foreach (var bl in mBendLines) {
            mVertices.Add (bl.StartPoint);
            mVertices.Add (bl.EndPoint);
        }
        mCentroid = BendUtils.Centroid (mVertices);
        mBound = new Bound ([.. mVertices]);
        mMaterialType = materialType;
        mThickness = thickness;
    }
    #endregion

    #region Properties -----------------------------------------------
    public readonly List<PLine> PLines => mPLines ?? [];
    public readonly List<BendLine> BendLines => mBendLines ?? [];
    public readonly List<BPoint> Vertices => mVertices ?? [];
    public readonly BPoint Centroid => mCentroid;
    public readonly Bound Bound => mBound;
    public readonly string MaterialType => mMaterialType;
    public readonly double Thickness => mThickness;
    #endregion

    #region Private Data ---------------------------------------------
    List<PLine>? mPLines;
    List<BendLine>? mBendLines;
    List<BPoint>? mVertices;
    BPoint mCentroid;
    Bound mBound;
    string mMaterialType;
    double mThickness;
    #endregion
}
#endregion