using System.Windows.Controls;

namespace BendMaker;

#region class BProfileMaker -----------------------------------------------------------------------
public class BProfileMaker {
   #region Constructors ---------------------------------------------
   public BProfileMaker (Profile profile, EBDAlgorithm algorithm) => (mProfile, mAlgorithm) = (profile, algorithm);

   public BProfileMaker () { }
   #endregion

   #region Properties -----------------------------------------------
   #endregion

   #region Methods --------------------------------------------------
   public BendProfile MakeBendProfile (Profile profile, EBDAlgorithm algorithm) {
      (mProfile, mAlgorithm) = (profile, algorithm);
      var totalBD = 0.0;
      int verBLCount = 0, horBLCount = 0;
      List<BendLine> newBendLines = [];
      List<Curve> newCurves = [];
      if (mAlgorithm is EBDAlgorithm.PartiallyDistributed) {
         newBendLines.AddRange (GetTranslatedBLines (mProfile.BendLines, out totalBD, out horBLCount, out verBLCount));
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
         var bottomBLines = bendLines.Take (count / 2).Reverse ().ToList (); // Bottom and Left
         var topBLines = bendLines.TakeLast (count - bottomBLines.Count).ToList (); // Top and right
         var tempCurves = new List<Curve> ();

         newBendLines.AddRange (GetTranslatedBLines (topBLines, out totalBD, out horBLCount, out verBLCount, isNegOff: true));
         foreach (var c in GetProfileCurves (mProfile, EBLLocation.Top, verBLCount > 0, horBLCount > 0)) {
            var angle = c.StartPoint.AngleTo (c.EndPoint);
            var (dx, dy) = angle is 0 or 180 ? (0.0, -totalBD) : (-totalBD, 0);
            if (angle is 0 or 180) {
               newCurves.Add (c.Translated (dx, dy));
               foreach (var idx in BendUtils.GetCCIndices (c, mProfile.Curves)) {
                  var conCurve = mProfile.Curves.Where (cC => cC.Index == idx).First ();
                  var trimmed = conCurve.StartPoint.Y < c.StartPoint.Y ? conCurve.Trimmed (0, 0, 0, -totalBD)
                                                                       : conCurve.Trimmed (0, -totalBD, 0, 0);
                  if (bottomBLines.Count == 0) newCurves.Add (trimmed);
                  else tempCurves.Add (trimmed);
               }
            } else {
               newCurves.Add (c.Translated (dx, dy));
               foreach (var idx in BendUtils.GetCCIndices (c, mProfile.Curves)) {
                  var conCurve = mProfile.Curves.Where (cC => cC.Index == idx).First ();
                  var trimmed = conCurve.StartPoint.X < c.StartPoint.X ? conCurve.Trimmed (0, 0, -totalBD, 0)
                                                                       : conCurve.Trimmed (-totalBD, 0, 0, 0);
                  if (bottomBLines.Count == 0) newCurves.Add (trimmed);
                  else tempCurves.Add (trimmed);
               }
            }
         }
         if (bottomBLines.Count == 0) newCurves.Add (mProfile.Curves.Except (tempCurves).First ()); // Incase of a single bend line part

         newBendLines.AddRange (GetTranslatedBLines (bottomBLines, out totalBD, out horBLCount, out verBLCount));
         foreach (var c in GetProfileCurves (mProfile, EBLLocation.Bottom, verBLCount > 0, horBLCount > 0)) {
            var angle = c.StartPoint.AngleTo (c.EndPoint);
            var (dx, dy) = angle is 0 or 180 ? (0.0, totalBD) : (totalBD, 0);
            if (angle is 0 or 180) {
               newCurves.Add (c.Translated (dx, dy));
               foreach (var idx in BendUtils.GetCCIndices (c, mProfile.Curves)) {
                  Curve conCurve = new ();
                  if (tempCurves.Any (tC => tC.Index == idx)) {
                     conCurve = tempCurves.Where (tC => tC.Index == idx).First ();
                  } else {
                     conCurve = mProfile.Curves.Where (pC => pC.Index == idx).First ();
                     foreach (var tC in tempCurves) newCurves.Add (tC);
                  }
                  var trimmed = conCurve.StartPoint.Y > c.StartPoint.Y ? conCurve.Trimmed (0, 0, 0, totalBD)
                                                                       : conCurve.Trimmed (0, totalBD, 0, 0);
                  newCurves.Add (trimmed);
               }
            } else {
               newCurves.Add (c.Translated (dx, dy));
               foreach (var idx in BendUtils.GetCCIndices (c, mProfile.Curves)) {
                  Curve conCurve = new ();
                  if (tempCurves.Any (tC => tC.Index == idx)) {
                     conCurve = tempCurves.Where (tC => tC.Index == idx).First ();
                  } else {
                     conCurve = mProfile.Curves.Where (pC => pC.Index == idx).First ();
                     foreach (var tC in tempCurves) newCurves.Add (tC);
                  }
                  var trimmed = conCurve.StartPoint.X > c.StartPoint.X ? conCurve.Trimmed (0, 0, totalBD, 0)
                                                                       : conCurve.Trimmed (totalBD, 0, 0, 0);
                  newCurves.Add (trimmed);
               }
            }
         }
      }
      mBendProfile = new BendProfile (mAlgorithm, newCurves, newBendLines);
      return mBendProfile;
   }
   #endregion

   #region Implementation -------------------------------------------
   List<BendLine> GetTranslatedBLines (List<BendLine> blines, out double totalBD, out int horCount, out int verCount, bool isNegOff = false) {
      var newBendLines = new List<BendLine> ();
      var offFactor = isNegOff ? -1 : 1;
      totalBD = 0.0;
      horCount = verCount = 0;
      foreach (var bl in blines) {
         var (bd, orient) = (bl.BendDeduction, bl.Orientation);
         var offset = offFactor * (totalBD + 0.5 * bd);
         (double dx, double dy) = (0, 0);
         if (orient is EBLOrientation.Horizontal) {
            horCount += 1;
            (dx, dy) = (0.0, offset);
         } else if (orient is EBLOrientation.Vertical) {
            verCount += 1;
            (dx, dy) = (offset, 0.0);
         }
         totalBD += bd;
         newBendLines.Add (bl.Translated (dx, dy));
      }
      return newBendLines;
   }

   List<Curve> GetProfileCurves (Profile pf, EBLLocation loc, bool HasVerBLine, bool HasHorBLine) {
      var b = pf.Bound;
      List<Curve> alignedCurves = [];
      if (HasVerBLine) { // Handles vertical bend lines and returns nearest vertical curve 
         alignedCurves.Add (pf.Curves.Where (c => loc is EBLLocation.Top ? c.StartPoint.X == b.MaxX && c.EndPoint.X == b.MaxX
                                                                         : c.StartPoint.X == b.MinX && c.EndPoint.X == b.MinX).First ());
      }

      if (HasHorBLine) { // Handles horizontal bend lines and returns nearest horizontal curve 
         alignedCurves.Insert (0, pf.Curves.Where (c => loc is EBLLocation.Top ? c.StartPoint.Y == b.MaxY && c.EndPoint.Y == b.MaxY
                                                                               : c.StartPoint.Y == b.MinY && c.EndPoint.Y == b.MinY).First ());
      }
      return alignedCurves;
   }
   #endregion

   #region Private Data ---------------------------------------------
   Profile mProfile;
   BendProfile mBendProfile;
   EBDAlgorithm mAlgorithm;
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