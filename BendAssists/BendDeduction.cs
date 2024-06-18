namespace BendMaker;

#region class BProfileMaker -----------------------------------------------------------------------
public class BendDeduction {
   #region Constructors ---------------------------------------------
   public BendDeduction (Part profile, EBDAlgorithm algorithm) => (mPart, mAlgorithm) = (profile, algorithm);

   public BendDeduction () { }
   #endregion

   #region Properties -----------------------------------------------
   #endregion

   #region Methods --------------------------------------------------
   public BendProcessedPart ApplyBendDeduction (Part part, EBDAlgorithm algorithm) {
      (mPart, mAlgorithm) = (part, algorithm);
      var totalBD = 0.0;
      int verBLCount = 0, horBLCount = 0;
      List<BendLine> newBendLines = [];
      List<PLine> newPlines = [];
      if (mAlgorithm is EBDAlgorithm.PartiallyDistributed) {
         var tmp = mPart.BendLines.Select (bl => bl.Clone ()).ToList ();
         tmp.Reverse ();
         newBendLines.AddRange (GetTranslatedBLines (tmp, out totalBD, out horBLCount, out verBLCount));
         var centroidY = mPart.Centroid.Y;
         foreach (var pline in mPart.PLines) {
            var newPline = pline.StartPoint.Y < centroidY && pline.EndPoint.Y < centroidY ? pline.Translated (0, totalBD)
                                                                                          : pline;
            var angle = pline.StartPoint.AngleTo (pline.EndPoint);
            if (angle is 90 or 270) {
               if (newPline.StartPoint.Y < newPline.EndPoint.Y) newPline = newPline.Trimmed (0, totalBD, 0, 0);
               else newPline = newPline.Trimmed (0, 0, 0, totalBD);
            }
            newPlines.Add (newPline);
         }
      } else {
         var bendLines = mPart.BendLines;
         var count = bendLines.Count;
         var bottomBLines = bendLines.Take (count / 2).Reverse ().ToList (); // Bottom and Left
         var topBLines = bendLines.TakeLast (count - bottomBLines.Count).ToList (); // Top and right
         var tempCurves = new List<PLine> ();

         newBendLines.AddRange (GetTranslatedBLines (topBLines, out totalBD, out horBLCount, out verBLCount, isNegOff: true));
         foreach (var c in GetProfileCurves (mPart, EBLLocation.Top, verBLCount > 0, horBLCount > 0)) {
            var angle = c.StartPoint.AngleTo (c.EndPoint);
            var (dx, dy) = angle is 0 or 180 ? (0.0, -totalBD) : (-totalBD, 0);
            if (angle is 0 or 180) {
               newPlines.Add (c.Translated (dx, dy));
               foreach (var idx in BendUtils.GetCPIndices (c, mPart.PLines)) {
                  var conCurve = mPart.PLines.Where (cC => cC.Index == idx).First ();
                  var trimmed = conCurve.StartPoint.Y < c.StartPoint.Y ? conCurve.Trimmed (0, 0, 0, -totalBD)
                                                                       : conCurve.Trimmed (0, -totalBD, 0, 0);
                  if (bottomBLines.Count == 0) newPlines.Add (trimmed);
                  else tempCurves.Add (trimmed);
               }
            } else {
               newPlines.Add (c.Translated (dx, dy));
               foreach (var idx in BendUtils.GetCPIndices (c, mPart.PLines)) {
                  var conCurve = mPart.PLines.Where (cC => cC.Index == idx).First ();
                  var trimmed = conCurve.StartPoint.X < c.StartPoint.X ? conCurve.Trimmed (0, 0, -totalBD, 0)
                                                                       : conCurve.Trimmed (-totalBD, 0, 0, 0);
                  if (bottomBLines.Count == 0) newPlines.Add (trimmed);
                  else tempCurves.Add (trimmed);
               }
            }
         }
         if (bottomBLines.Count == 0) newPlines.Add (mPart.PLines.Except (tempCurves).First ()); // Incase of a single bend line part

         newBendLines.AddRange (GetTranslatedBLines (bottomBLines, out totalBD, out horBLCount, out verBLCount));
         foreach (var c in GetProfileCurves (mPart, EBLLocation.Bottom, verBLCount > 0, horBLCount > 0)) {
            var angle = c.StartPoint.AngleTo (c.EndPoint);
            var (dx, dy) = angle is 0 or 180 ? (0.0, totalBD) : (totalBD, 0);
            if (angle is 0 or 180) {
               newPlines.Add (c.Translated (dx, dy));
               foreach (var idx in BendUtils.GetCPIndices (c, mPart.PLines)) {
                  PLine conPline = new ();
                  if (tempCurves.Any (tC => tC.Index == idx)) {
                     conPline = tempCurves.Where (tC => tC.Index == idx).First ();
                  } else {
                     conPline = mPart.PLines.Where (pC => pC.Index == idx).First ();
                     foreach (var tC in tempCurves) newPlines.Add (tC);
                  }
                  var trimmed = conPline.StartPoint.Y > c.StartPoint.Y ? conPline.Trimmed (0, 0, 0, totalBD)
                                                                       : conPline.Trimmed (0, totalBD, 0, 0);
                  newPlines.Add (trimmed);
               }
            } else {
               newPlines.Add (c.Translated (dx, dy));
               foreach (var idx in BendUtils.GetCPIndices (c, mPart.PLines)) {
                  PLine conCurve = new ();
                  if (tempCurves.Any (tC => tC.Index == idx)) {
                     conCurve = tempCurves.Where (tC => tC.Index == idx).First ();
                  } else {
                     conCurve = mPart.PLines.Where (pC => pC.Index == idx).First ();
                     foreach (var tC in tempCurves) newPlines.Add (tC);
                  }
                  var trimmed = conCurve.StartPoint.X > c.StartPoint.X ? conCurve.Trimmed (0, 0, totalBD, 0)
                                                                       : conCurve.Trimmed (totalBD, 0, 0, 0);
                  newPlines.Add (trimmed);
               }
            }
         }
      }
      mProcessedPart = new BendProcessedPart (mAlgorithm, newPlines, newBendLines);
      return mProcessedPart;
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

   List<PLine> GetProfileCurves (Part pf, EBLLocation loc, bool HasVerBLine, bool HasHorBLine) {
      var b = pf.Bound;
      List<PLine> alignedCurves = [];
      if (HasVerBLine) { // Handles vertical bend lines and returns nearest vertical pline 
         alignedCurves.Add (pf.PLines.Where (c => loc is EBLLocation.Top ? c.StartPoint.X == b.MaxX && c.EndPoint.X == b.MaxX
                                                                         : c.StartPoint.X == b.MinX && c.EndPoint.X == b.MinX).First ());
      }

      if (HasHorBLine) { // Handles horizontal bend lines and returns nearest horizontal pline 
         alignedCurves.Insert (0, pf.PLines.Where (c => loc is EBLLocation.Top ? c.StartPoint.Y == b.MaxY && c.EndPoint.Y == b.MaxY
                                                                               : c.StartPoint.Y == b.MinY && c.EndPoint.Y == b.MinY).First ());
      }
      return alignedCurves;
   }
   #endregion

   #region Private Data ---------------------------------------------
   Part mPart;
   BendProcessedPart mProcessedPart;
   EBDAlgorithm mAlgorithm;
   #endregion
}
#endregion

#region Enums -------------------------------------------------------------------------------------
public enum EBLOrientation { Inclined, Horizontal, Vertical }

public enum ECurve { Line, Arc, Spline }

public enum ECurveOrientation { Inclined, Horizontal, Vertical }

public enum ELineType { Solid, Dashed }

public enum EBDAlgorithm { EquallyDistributed, PartiallyDistributed, Unknown }

public enum EBLLocation { Top, Bottom }
#endregion