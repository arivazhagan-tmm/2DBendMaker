
namespace BendMaker;
internal class BProfileMaker {
   #region Constructors ---------------------------------------------
   public BProfileMaker (List<BendLine> bendLines) {
      mBendLines = bendLines;
   }
   public BProfileMaker (Profile profile, EBDAlgorithm algorithm) => (mProfile, mAlgorithm) = (profile, algorithm);
   #endregion

   #region Properties -----------------------------------------------
   #endregion

   #region Methods --------------------------------------------------
   public List<BendLine> GetTranslatedBLines () {
      if (mBendLines == null) return [];
      double totalBD = 0.0;
      List<BendLine> newBendLines = [];
      foreach (var bl in mBendLines) {
         var bendDeduction = bl.BendDeduction;
         newBendLines.Add (bl.Translated (0.0, -(totalBD + bendDeduction * 0.5)));
         totalBD += bendDeduction;
      }
      return newBendLines;
   }

   public BendProfile MakeBendProfile () {
      if (mAlgorithm is EBDAlgorithm.PartialPreserve) {
         double totalBD = 0.0;
         List<BendLine> newBendLines = [];
         foreach (var bl in mProfile.BendLines) {
            var bendDeduction = bl.BendDeduction;
            var bendOffset = totalBD + bendDeduction * 0.5;
            var (dx, dy) = bl.Orientation is EBLOrientation.Horizontal ? (0, bendOffset) : (-bendOffset, 0.0);
            newBendLines.Add (bl.Translated (dx, dy));
            totalBD += bendDeduction;
         }
      } else {
         double totalBD = 0;
         int bendLineCount = mProfile.BendLines.Count;
         for (int i = bendLineCount / 2; i > 0; i--) {
            var bl = mProfile.BendLines[i - 1];
            var bd = bl.BendDeduction;
            double bendOffset = totalBD + bd * 0.5;
            var (dx, dy) = bl.Orientation is EBLOrientation.Horizontal ? (0, bendOffset) : (-bendOffset, 0.0);
            totalBD += bd;
         }
         totalBD = 0;
         for (int i = bendLineCount / 2 + 1; i <= bendLineCount; i++) {
            var bl = mProfile.BendLines[i - 1];
            var bd = bl.BendDeduction;
            double bendOffset = totalBD + bd * 0.5;
            var (dx, dy) = bl.Orientation is EBLOrientation.Horizontal ? (0, -bendOffset) : (bendOffset, 0.0);
            totalBD += bd;
         }
      }
      return mBendProfile;
   }
   #endregion

   #region Implementation -------------------------------------------
   #endregion

   #region Private Data ---------------------------------------------
   List<BendLine>? mBendLines;
   Profile mProfile;
   BendProfile mBendProfile;
   EBDAlgorithm mAlgorithm;
   #endregion
}

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
      var vector = new BVector (dx, dy);
      return new (mStartPt + vector, mEndPt + vector, mBendDeduction);
   }

   //public override string ToString () => $"Angle: {mAngle}\nRadius: {mRadius}\nDeduction: {mBendDeduction}\nKfactor: {mKFactor}";
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

#region struct Profile ----------------------------------------------------------------------------
public struct Profile {
   #region Constructors ---------------------------------------------
   public Profile (List<Curve> curves, double radius, double thickness, double kFactor) {
      (mCurves, mBendLines, mVertices) = ([], [], []);
      foreach (var c in curves) {
         bool isEmptyTag = string.IsNullOrEmpty (c.Tag);
         if (isEmptyTag) {
            mCurves.Add (c);
            mVertices.Add (c.StartPoint);
         } else if (double.TryParse (c.Tag, out var bendAngle)) {
            var bl = new BendLine (c.StartPoint, c.EndPoint, bendAngle, radius, thickness, kFactor);
            if (mBendLines.Any () && mBendLines.First ().StartPoint.Y < bl.StartPoint.Y)
               mBendLines.Insert (0, bl);
            else mBendLines.Add (bl);
         }
      }

      mCentroid = BendUtils.Centroid (mVertices);
   }
   #endregion

   #region Properties -----------------------------------------------
   public readonly List<Curve> Curves => mCurves;
   public readonly List<BendLine> BendLines => mBendLines;
   public readonly List<BPoint> Vertices => mVertices;
   public readonly BPoint Centroid => mCentroid;
   #endregion

   #region Methods --------------------------------------------------
   #endregion

   #region Implementation -------------------------------------------
   #endregion

   #region Private Data ---------------------------------------------
   List<Curve> mCurves;
   List<BendLine> mBendLines;
   List<BPoint> mVertices;
   BPoint mCentroid;

   #endregion
}
#endregion

#region struct BendProfile ------------------------------------------------------------------------
public struct BendProfile {
   #region Constructors ---------------------------------------------
   public BendProfile (EBDAlgorithm algorithm, List<Curve> curves, List<BendLine> bendLines) {
      mBDAlgorithm = algorithm;
      (mBendLines, mCurves) = (bendLines, curves);
   }
   #endregion

   #region Properties -----------------------------------------------
   public readonly EBDAlgorithm BendDeductionAlgorithm => mBDAlgorithm;
   #endregion

   #region Private Data ---------------------------------------------
   List<BendLine> mBendLines;
   List<Curve> mCurves;
   EBDAlgorithm mBDAlgorithm; // Bend deduction algorithm
   #endregion
}
#endregion

#region struct Curve ------------------------------------------------------------------------------
public struct Curve {
   #region Constructors ---------------------------------------------
   public Curve (ECurve curveType, string tag, params BPoint[] points) {
      var pts = points.ToList ();
      (mCurvePoints, mCurveType) = (pts, curveType);
      (mStartPt, mEndPt) = (points[0], points[^1]);
      mIsClosed = pts.Count > 2 && mStartPt.Equals (mEndPt);
      mTag = tag;
   }
   #endregion

   #region Properties -----------------------------------------------
   public BPoint StartPoint => mStartPt;
   public BPoint EndPoint => mEndPt;
   public ELineType LineType => mLineType;
   public ECurve CurveType => mCurveType;
   public string Tag => mTag ??= "";
   #endregion

   #region Private Data ---------------------------------------------
   List<BPoint> mCurvePoints;
   BPoint mStartPt, mEndPt;
   ECurve mCurveType;
   ELineType mLineType;
   bool mIsClosed;
   string? mTag;
   #endregion
}
#endregion

#region struct BPoint -----------------------------------------------------------------------------
public readonly record struct BPoint (double X, double Y) {
   #region Operators ------------------------------------------------
   public static BPoint operator + (BPoint p, BVector v) => new (p.X + v.DX, p.Y + v.DY);
   #endregion

   #region Methods --------------------------------------------------
   public double AngleTo (BPoint p) {
      var angle = Math.Round (Math.Atan2 (p.Y - Y, p.X - X) * (180 / Math.PI), 2);
      return angle < 0 ? 360 + angle : angle;
   }
   public override string ToString () => $"({X}, {Y})";
   public bool IsEqual (BPoint p) => p.X == X && p.Y == Y;
   #endregion
}
#endregion

#region struct BVector ----------------------------------------------------------------------------
public readonly record struct BVector (double DX, double DY);
#endregion

#region Enums -------------------------------------------------------------------------------------
public enum EBLOrientation { Inclined, Horizontal, Vertical }

public enum ECurve { Line, Arc, Spline }

public enum ELineType { Solid, Dashed }

public enum EBDAlgorithm { EquallyDistributed, PartialPreserve }
#endregion