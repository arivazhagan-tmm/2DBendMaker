namespace BendMaker;

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
      mBendLines = bendLines.OrderBy (bl => bl.StartPoint.Y).ThenBy (bl => bl.StartPoint.X).ToList ();
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