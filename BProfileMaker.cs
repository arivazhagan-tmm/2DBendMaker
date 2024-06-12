﻿namespace BendMaker;

internal class BProfileMaker {
   #region Constructors ---------------------------------------------
   public BProfileMaker (Profile profile, EBDAlgorithm algorithm) => (mProfile, mAlgorithm) = (profile, algorithm);
   #endregion

   #region Properties -----------------------------------------------
   #endregion

   #region Methods --------------------------------------------------
   public BendProfile MakeBendProfile () {
      var totalBD = 0.0;
      List<BendLine> newBendLines = [];
      List<Curve> newCurves = [];
      if (mAlgorithm is EBDAlgorithm.PartiallyDistributed) {
         newBendLines.AddRange (GetTranslatedBLines (mProfile.BendLines, out totalBD));
         var centroidY = mProfile.Centroid.Y;
         foreach (var curve in mProfile.Curves) {
            var newCurve = curve.StartPoint.Y < centroidY && curve.EndPoint.Y < centroidY ? curve.Translated (0, totalBD)
                                                                                          : curve;
            var angle = curve.StartPoint.AngleTo (curve.EndPoint);
            if (angle is 90 or 270) {
               if (newCurve.StartPoint.Y < newCurve.EndPoint.Y) newCurve = newCurve.Trimmed (0, totalBD, 0, 0);
               else newCurve = newCurve.Trimmed (0, 0, 0, totalBD);
            }
            newCurves.Add (newCurve);
         }
      } else {
         var bendLines = mProfile.BendLines;
         var count = bendLines.Count;
         var bottomBLines = bendLines.Take (count / 2).Reverse ().ToList ();
         var topBLines = bendLines.TakeLast (count - bottomBLines.Count).ToList ();
         var tempCurves = new List<Curve> ();
         newBendLines.AddRange (GetTranslatedBLines (topBLines, out totalBD, isNegOff: true));
         foreach (var c in GetProfileCurves (mProfile, EBLLocation.Top)) {
            var angle = c.StartPoint.AngleTo (c.EndPoint);
            var (dx, dy) = angle is 0 or 180 ? (0.0, -totalBD) : (totalBD, 0);
            if (angle is 90 or 270) {
               var trimmed = c.StartPoint.Y < c.EndPoint.Y ? c.Trimmed (0, 0, 0, -totalBD)
                                                           : c.Trimmed (0, -totalBD, 0, 0);
               tempCurves.Add (trimmed);
            } else
               newCurves.Add (c.Translated (dx, dy));
         }
         newBendLines.AddRange (GetTranslatedBLines (bottomBLines, out totalBD));
         foreach (var c in GetProfileCurves (mProfile, EBLLocation.Bottom)) {
            var angle = c.StartPoint.AngleTo (c.EndPoint);
            var (dx, dy) = angle is 0 or 180 ? (0.0, totalBD) : (-totalBD, 0);
            if (angle is 0 or 180) newCurves.Add (c.Translated (dx, dy));
         }
         foreach (var c in tempCurves) {
            var trimmed = c.StartPoint.Y < c.EndPoint.Y ? c.Trimmed (0, totalBD, 0, 0)
                                                        : c.Trimmed (0, 0, 0, totalBD);
            newCurves.Add (trimmed);
         }
      }
      mBendProfile = new BendProfile (mAlgorithm, newCurves, newBendLines);
      return mBendProfile;
   }
   #endregion

   #region Implementation -------------------------------------------
   List<BendLine> GetTranslatedBLines (List<BendLine> blines, out double totalBD, bool isNegOff = false) {
      var newBendLines = new List<BendLine> ();
      var offFactor = isNegOff ? -1 : 1;
      totalBD = 0.0;
      foreach (var bl in blines) {
         var (bd, orient) = (bl.BendDeduction, bl.Orientation);
         var offset = offFactor * (totalBD + 0.5 * bd);
         var (dx, dy) = orient is EBLOrientation.Horizontal ? (0.0, offset) : (-offset, 0.0);
         totalBD += bd;
         newBendLines.Add (bl.Translated (dx, dy));
      }
      return newBendLines;
   }

   List<Curve> GetProfileCurves (Profile pf, EBLLocation loc) {
      var b = pf.Bound;
      return pf.Curves.Where (c => loc is EBLLocation.Top ? c.StartPoint.Y == b.MaxY || c.EndPoint.Y == b.MaxY
                                                          : c.StartPoint.Y == b.MinY && c.EndPoint.Y == b.MinY).ToList ();
   }
   #endregion

   #region Private Data ---------------------------------------------
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

#region struct Profile ----------------------------------------------------------------------------
public struct Profile {
   #region Constructors ---------------------------------------------
   public Profile (List<Curve> curves, double radius = 0.0, double thickness = 0.0, double kFactor = 0.0) {
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
      mBound = new Bound (mVertices.ToArray ());
   }

   public Profile (List<Curve> curves, List<BendLine> bendLines) {
      mBendLines = bendLines.OrderBy (bl => bl.StartPoint.Y).ToList ();
      (mCurves, mVertices) = (curves, []);
      foreach (var c in mCurves) mVertices.Add (c.StartPoint);
      mCentroid = BendUtils.Centroid (mVertices);
      foreach (var bl in mBendLines) {
         mVertices.Add (bl.StartPoint);
         mVertices.Add (bl.EndPoint);
      }
      mCentroid = BendUtils.Centroid (mVertices);
      mBound = new Bound (mVertices.ToArray ());
   }
   #endregion

   #region Properties -----------------------------------------------
   public readonly List<Curve> Curves => mCurves;
   public readonly List<BendLine> BendLines => mBendLines;
   public readonly List<BPoint> Vertices => mVertices;
   public readonly BPoint Centroid => mCentroid;
   public readonly Bound Bound => mBound;
   #endregion

   #region Methods --------------------------------------------------
   #endregion

   #region Implementation -------------------------------------------
   #endregion

   #region Private Data ---------------------------------------------
   List<Curve>? mCurves;
   List<BendLine>? mBendLines;
   List<BPoint>? mVertices;
   BPoint mCentroid;
   Bound mBound;
   #endregion
}
#endregion

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
   }
   #endregion

   #region Properties -----------------------------------------------
   public readonly EBDAlgorithm BendDeductionAlgorithm => mBDAlgorithm;
   public List<BPoint> Vertices => mVertices;
   #endregion

   #region Private Data ---------------------------------------------
   List<BendLine> mBendLines;
   List<Curve> mCurves;
   List<BPoint> mVertices;
   EBDAlgorithm mBDAlgorithm; // Bend deduction algorithm
   #endregion
}
#endregion

#region struct Curve ------------------------------------------------------------------------------
public struct Curve {
   #region Constructors ---------------------------------------------
   public Curve (ECurve curveType, string tag = null, params BPoint[] points) {
      var pts = points.ToList ();
      (mCurvePoints, mCurveType) = (pts, curveType);
      (mStartPt, mEndPt) = (points[0], points[^1]);
      mTag = tag;
   }

   public Curve (ECurve curveType, BPoint startpoint, BPoint endpoint) {
      (mCurveType, mStartPt, mEndPt) = (curveType, startpoint, endpoint);
   }
   #endregion

   #region Properties -----------------------------------------------
   public readonly BPoint StartPoint => mStartPt;
   public readonly BPoint EndPoint => mEndPt;
   public readonly ELineType LineType => mLineType;
   public readonly ECurve CurveType => mCurveType;
   public string Tag => mTag ??= "";
   public readonly Guid ID => mID;
   #endregion

   #region Methods --------------------------------------------------
   public Curve Translated (double dx, double dy) {
      var v = new BVector (dx, dy);
      var pts = mCurvePoints.Select (p => p.Translated (v)).ToArray ();
      return new (mCurveType, mTag ??= "", pts);
   }

   public Curve Trimmed (double startDx, double startDy, double endDx, double endDy) {
      var (startPt, endPt) = (new BPoint (mStartPt.X + startDx, mStartPt.Y + startDy, mStartPt.Index),
                              new BPoint (mEndPt.X + endDx, mEndPt.Y + endDy, mEndPt.Index));
      return new Curve (ECurve.Line, startPt, endPt);
   }
   #endregion

   #region Private Data ---------------------------------------------
   List<BPoint> mCurvePoints;
   BPoint mStartPt, mEndPt;
   ECurve mCurveType;
   ELineType mLineType;
   string? mTag;
   Guid mID;
   #endregion
}
#endregion

#region struct BPoint -----------------------------------------------------------------------------
public readonly record struct BPoint (double X, double Y, int Index = -1) {
   #region Methods --------------------------------------------------
   public double AngleTo (BPoint p) {
      var angle = Math.Round (Math.Atan2 (p.Y - Y, p.X - X) * (180 / Math.PI), 2);
      return angle < 0 ? 360 + angle : angle;
   }
   public override string ToString () => $"({X}, {Y})";
   public bool IsEqual (BPoint p) => p.X == X && p.Y == Y;
   public bool IsInside (Bound b) => X < b.MaxX && X > b.MinX && Y < b.MaxY && Y < b.MinY;
   public BPoint Translated (BVector v) => new (X + v.DX, Y + v.DY, Index);
   #endregion
}
#endregion

#region struct BVector ----------------------------------------------------------------------------
public readonly record struct BVector (double DX, double DY);
#endregion

#region struct Bound ------------------------------------------------------------------------------
public struct Bound {
   #region Constructors ---------------------------------------------
   public Bound (BPoint cornerA, BPoint cornerB) {
      MinX = Math.Min (cornerA.X, cornerB.X);
      MaxX = Math.Max (cornerA.X, cornerB.X);
      MinY = Math.Min (cornerA.Y, cornerB.Y);
      MaxY = Math.Max (cornerA.Y, cornerB.Y);
      (mHeight, mWidth) = (MaxY - MinY, MaxX - MinX);
   }

   public Bound (params BPoint[] pts) {
      MinX = pts.Min (p => p.X);
      MaxX = pts.Max (p => p.X);
      MinY = pts.Min (p => p.Y);
      MaxY = pts.Max (p => p.Y);
      (mHeight, mWidth) = (MaxY - MinY, MaxX - MinX);
   }
   #endregion

   #region Properties -----------------------------------------------
   public bool IsEmpty => MinX > MaxX || MinY > MaxY;
   public double MinX { get; init; }
   public double MaxX { get; init; }
   public double MinY { get; init; }
   public double MaxY { get; init; }
   public double Width => mWidth;
   public double Height => mHeight;
   #endregion

   #region Private Data ---------------------------------------------
   readonly BPoint mMid;
   readonly double mHeight, mWidth;
   #endregion
}
#endregion

#region Enums -------------------------------------------------------------------------------------
public enum EBLOrientation { Inclined, Horizontal, Vertical }

public enum ECurve { Line, Arc, Spline }

public enum ELineType { Solid, Dashed }

public enum EBDAlgorithm { EquallyDistributed, PartiallyDistributed }

public enum EBLLocation { Top, Bottom }
#endregion