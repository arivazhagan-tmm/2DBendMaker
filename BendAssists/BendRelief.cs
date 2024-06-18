namespace BendMaker;

#region class BendRelief --------------------------------------------------------------------------
public class BendRelief {
   #region Constructors ---------------------------------------------
   public BendRelief () {
      (mHasCornerNotch, mPLines) = (false, []);
   }
   #endregion

   #region Methods --------------------------------------------------
   public BendProcessedPart ApplyBendRelief (Part profile) {
      mPLines.Clear ();
      foreach (var curve in profile.PLines) mPLines.Add (curve);
      var hCurves = mPLines.Where (x => x.StartPoint.Y == x.EndPoint.Y).ToList (); // Horizontal curves
      var vCurves = mPLines.Where (x => x.StartPoint.X == x.EndPoint.X).ToList (); // Vertical curves
      var centroid = profile.Vertices.Centroid ();
      PLine ICurve, NPCurve; // Intersected line, Nearest parallel line
      BPoint NIPoint, IPoint; // Non intersecting point, Intersecting point
      double offset; // Offset from nearest parallel line and bendline
      if (profile.BendLines.Count () != 0) {
         foreach (var bLine in profile.BendLines) {
            ICurve = (bLine.Orientation == EBLOrientation.Horizontal ? vCurves : hCurves).Where (x => IsIntersecting (x, bLine)).First ();
            NIPoint = (bLine.Orientation == EBLOrientation.Horizontal) ?
                                  (bLine.StartPoint.X == ICurve.StartPoint.X ? bLine.EndPoint : bLine.StartPoint) :
                                 (bLine.StartPoint.Y == ICurve.StartPoint.Y ? bLine.EndPoint : bLine.StartPoint);
            mHasCornerNotch = (bLine.Orientation == EBLOrientation.Horizontal) ? vCurves.Where (x => x.StartPoint.X == NIPoint.X || x.EndPoint.X == NIPoint.X).Count () == 1
                                                                                : hCurves.Where (x => x.StartPoint.Y == NIPoint.Y || x.EndPoint.Y == NIPoint.Y).Count () == 1;
            IPoint = bLine.StartPoint == NIPoint ? bLine.EndPoint : bLine.StartPoint;
            NPCurve = GetNearestParallelCurve ((bLine.Orientation == EBLOrientation.Horizontal) ? hCurves : vCurves, bLine);
            offset = GetDistanceToLine (NPCurve, bLine);
            AddBendReliefPoints (bLine, centroid, NIPoint, NPCurve, ICurve, offset, IPoint);
         }
      }
      return new BendProcessedPart (EBDAlgorithm.EquallyDistributed, mPLines, profile.BendLines);
   }

   bool IsIntersecting (PLine curve, BendLine bLine) =>
        bLine.Orientation == EBLOrientation.Horizontal ? (curve.StartPoint.X == bLine.StartPoint.X || curve.StartPoint.X == bLine.EndPoint.X) :
                                                         (curve.StartPoint.Y == bLine.StartPoint.Y || curve.StartPoint.Y == bLine.EndPoint.Y);

   PLine GetNearestParallelCurve (List<PLine> curves, BendLine bLine) => curves.OrderBy (curve => GetDistanceToLine (curve, bLine)).FirstOrDefault ();

   double GetDistanceToLine (PLine curve, BendLine bLine) =>
       bLine.Orientation == EBLOrientation.Horizontal
       ? Math.Abs (curve.StartPoint.Y - bLine.StartPoint.Y)
       : Math.Abs (curve.StartPoint.X - bLine.StartPoint.X);

   void AddBendReliefPoints (BendLine bLine, BPoint centre, BPoint NIPoint, PLine NPCurve, PLine ICurve, double offset, BPoint IPoint) {
      double brHeight = BendUtils.GetBendAllowance (90, 0.38, 2, 2) / 2, brWidth = 1;
      BPoint p1, p2, p3, p4, p5, p6;
      bool isHorizontal = bLine.Orientation == EBLOrientation.Horizontal;
      p1 = new (
          NIPoint.X + (isHorizontal ? 0 : (NIPoint.X < centre.X ? -1 : 1) * offset),
          NIPoint.Y + (isHorizontal ? ((NIPoint.Y > centre.Y ? 1 : -1) * offset) : 0)
      );
      p2 = new (
          NIPoint.X + (isHorizontal ? 0 : (NIPoint.X < centre.X ? 1 : -1) * brHeight),
          NIPoint.Y + (isHorizontal ? ((NIPoint.Y > centre.Y ? -1 : 1) * brHeight) : 0)
      );
      p3 = new (
          p2.X + (isHorizontal ? ((ICurve.StartPoint.X > centre.X ? -1 : 1) * brWidth) : 0),
          p2.Y + (isHorizontal ? 0 : ((ICurve.StartPoint.Y > centre.Y ? -1 : 1) * brWidth))
      );
      p4 = new (
          (mHasCornerNotch ? NIPoint.X : p1.X) + (isHorizontal ? ((ICurve.StartPoint.X < centre.X ? 1 : -1) * brWidth) : 0),
          (mHasCornerNotch ? NIPoint.Y : p1.Y) + (isHorizontal ? 0 : ((ICurve.StartPoint.Y < centre.Y ? 1 : -1) * brWidth))
      );
      p5 = new (
          (mHasCornerNotch ? NIPoint.X : IPoint.X) + (isHorizontal ? 0 : (IPoint.X > centre.X ? 1 : -1) * offset),
          (mHasCornerNotch ? NIPoint.Y : IPoint.Y) + (isHorizontal ? (IPoint.Y > centre.Y ? 1 : -1) * offset : 0)
      );
      p6 = (NPCurve.StartPoint.X, NPCurve.StartPoint.Y) == (p5.X, p5.Y)
                            ? NPCurve.EndPoint : NPCurve.StartPoint;

      if (!mHasCornerNotch) {
         mPLines.Add (new PLine (ECurve.Line, NPCurve.Index, p5, p1));
      }

      mPLines.AddRange ([
                new PLine(ECurve.Line, NPCurve.Index + 1,  p1, p2),
                new PLine(ECurve.Line, NPCurve.Index + 2,  p2, p3),
                new PLine(ECurve.Line, NPCurve.Index + 3, p3, p4),
                new PLine(ECurve.Line, NPCurve.Index, p4, p6)
      ]);
      mPLines.Remove (NPCurve);
   }
   #endregion

   #region Private Data ---------------------------------------------
   bool mHasCornerNotch;
   List<PLine> mPLines;
   #endregion
}
#endregion