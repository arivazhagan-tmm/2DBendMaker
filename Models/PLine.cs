namespace BendMaker;

#region struct PLine ------------------------------------------------------------------------------
public struct PLine {
    #region Constructors ---------------------------------------------
    public PLine (ECurve curveType, int index, params BPoint[] points) {
        (mCurvePoints, mCurveType, mIndex) = (points, curveType, index);
        (mStartPt, mEndPt) = (points[0], points[^1]);
        UpdateOrientation ();
    }

    public PLine (BPoint startPt, BPoint endPt) => (mStartPt, mEndPt) = (startPt, endPt);

    #endregion

    #region Properties -----------------------------------------------
    public readonly BPoint StartPoint => mStartPt;
    public readonly BPoint EndPoint => mEndPt;
    public readonly ECurveOrientation Orientation => mOrientation;
    public int Index { readonly get => mIndex; set => mIndex = value; }
    public readonly double Length => mStartPt.DistanceTo (mEndPt);
    #endregion

    #region Methods --------------------------------------------------
    public PLine Translated (double dx, double dy) {
        var v = new BVector (dx, dy);
        var pts = mCurvePoints?.Select (p => p.Translated (v)).ToArray ();
        var translatedCurve = new PLine (mCurveType, mIndex, pts ?? []);
        return translatedCurve;
    }

    public PLine Trimmed (double startDx, double startDy, double endDx, double endDy) {
        var (startPt, endPt) = (new BPoint (mStartPt.X + startDx, mStartPt.Y + startDy, mStartPt.Index),
                                new BPoint (mEndPt.X + endDx, mEndPt.Y + endDy, mEndPt.Index));
        var trimmedCurve = new PLine (mCurveType, mIndex, startPt, endPt);
        return trimmedCurve;
    }

    public override string ToString () => $"{mIndex}, {mStartPt}, {mEndPt}";

    void UpdateOrientation () {
        var theta = mStartPt.AngleTo (mEndPt);

        if (theta is 0.0 or 180.0) mOrientation = ECurveOrientation.Horizontal;
        else if (theta is 90.0 or 270.0) mOrientation = ECurveOrientation.Vertical;
        else mOrientation = ECurveOrientation.Inclined;
    }
    #endregion

    #region Private Data ---------------------------------------------
    BPoint[]? mCurvePoints;
    BPoint mStartPt, mEndPt;
    ECurve mCurveType;
    ECurveOrientation mOrientation;
    int mIndex;
    #endregion
}
#endregion