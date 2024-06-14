namespace BendMaker;

#region struct Curve ------------------------------------------------------------------------------
public struct Curve {
   #region Constructors ---------------------------------------------
   public Curve (ECurve curveType, int index, string tag = null!, params BPoint[] points) {
      (mCurvePoints, mCurveType, mIndex) = (points, curveType, index);
      (mStartPt, mEndPt) = (points[0], points[^1]);
      mTag = tag;
   }
   #endregion

   #region Properties -----------------------------------------------
   public readonly BPoint StartPoint => mStartPt;
   public readonly BPoint EndPoint => mEndPt;
   public readonly ELineType LineType => mLineType;
   public readonly ECurve CurveType => mCurveType;
   public string Tag => mTag ??= "";
   public int Index { get => mIndex; set => mIndex = value; }
   public int[] CCIndices { get => mConnectedCurveIndices ??= []; private set => mConnectedCurveIndices = value; }
   #endregion

   #region Methods --------------------------------------------------
   public Curve Translated (double dx, double dy) {
      var v = new BVector (dx, dy);
      var pts = mCurvePoints?.Select (p => p.Translated (v)).ToArray ();
      var translatedCurve = new Curve (mCurveType, mIndex, mTag ??= "", pts);
      return translatedCurve;
   }

   public Curve Trimmed (double startDx, double startDy, double endDx, double endDy) {
      var (startPt, endPt) = (new BPoint (mStartPt.X + startDx, mStartPt.Y + startDy, mStartPt.Index),
                              new BPoint (mEndPt.X + endDx, mEndPt.Y + endDy, mEndPt.Index));
      var trimmedCurve = new Curve (mCurveType, mIndex, "", startPt, endPt);
      return trimmedCurve;
   }

   public void SetCCIndices (int[] indices) => mConnectedCurveIndices = indices;

   public override string ToString () => $"{mIndex}, {mStartPt}, {mEndPt}";
   #endregion

   #region Private Data ---------------------------------------------
   BPoint[]? mCurvePoints;
   BPoint mStartPt, mEndPt;
   ECurve mCurveType;
   ELineType mLineType;
   string? mTag;
   int mIndex;
   int[]? mConnectedCurveIndices;
   #endregion
}
#endregion