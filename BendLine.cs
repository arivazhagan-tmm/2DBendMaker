namespace BendMaker;

#region struct BendLine ---------------------------------------------------------------------------
public struct BendLine {
    #region Constructors ---------------------------------------------
    public BendLine (BPoint startPt, BPoint endPt, double angle, double radius, double thickness, double kFactor) {
        (mStartPt, mEndPt) = (startPt, endPt);
        mBendDeduction = BendUtils.GetBendDeduction (angle, kFactor, thickness, radius);
        UpdateOrientation ();
    }

    public BendLine (BPoint startPt, BPoint endPt, double bendDeduction) {
        (mStartPt, mEndPt, mBendDeduction) = (startPt, endPt, bendDeduction);
        UpdateOrientation ();
    }
    #endregion

    #region Properties -----------------------------------------------
    public BPoint StartPoint => mStartPt;
    public BPoint EndPoint => mEndPt;
    public double BendDeduction => mBendDeduction;
    public EBLOrientation Orientation => mOrientation;
    #endregion

    #region Methods --------------------------------------------------
    public BendLine Translated (double dx, double dy) {
        var v = new BVector (dx, dy);
        return new (mStartPt.Translated (v), mEndPt.Translated (v), mBendDeduction);
    }

    public override string ToString () => $"{mStartPt}, {mEndPt}";
    #endregion

    #region Implementation -------------------------------------------
    void UpdateOrientation () {
        var theta = mStartPt.AngleTo (mEndPt);
        mOrientation = mOrientation = theta is 0.0 or 90.0 ? theta is 0.0
                                            ? EBLOrientation.Horizontal
                                            : EBLOrientation.Vertical
                                            : EBLOrientation.Inclined;
    }

    #endregion

    #region Private Data ---------------------------------------------
    readonly double mBendDeduction;
    readonly BPoint mStartPt, mEndPt;
    EBLOrientation mOrientation;
    #endregion
}
#endregion