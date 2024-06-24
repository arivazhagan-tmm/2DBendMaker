namespace BendMaker;

#region struct BendProcessedPart ------------------------------------------------------------------------
public struct BendProcessedPart {
   #region Constructors ---------------------------------------------
   public BendProcessedPart (List<PLine> curves, List<BendLine> bendLines, bool isContourChanged = false) {
      mIsContourChanged = isContourChanged;
      (mBendLines, mPlines) = (bendLines, curves);
      mVertices = [];
      if (mIsContourChanged) (mPlines, mBendLines) = Sort (curves, bendLines);
      mVertices.AddRange (mPlines.Select (c => c.StartPoint));
      if (mIsContourChanged) mVertices.Add (mPlines[^1].EndPoint);
      mVertices.AddRange (mBendLines.Select (c => c.StartPoint));
      mVertices.AddRange (mBendLines.Select (c => c.EndPoint));
      mBound = new Bound ([.. mVertices]);        // Bound of the bent profile
      mCentroid = BendUtils.Centroid (mVertices); // Centroid of the bent profile
   }
   #endregion

   #region Method ---------------------------------------------------
   public (List<PLine>, List<BendLine>) Sort (List<PLine> plines, List<BendLine> bendLines) {
      List<PLine> newPlines = []; List<BendLine> newBendLines = []; int i = 1;
      foreach (var pline in plines) {
         var (start, end) = (pline.StartPoint, pline.EndPoint);
         newPlines.Add (new PLine (new BPoint (start.X, start.Y, i), new BPoint (end.X, end.Y, ++i)));
      }
      i++;
      foreach (var bendline in bendLines) {
         var (start, end) = (bendline.StartPoint, bendline.EndPoint);
         newBendLines.Add (new BendLine (new BPoint (start.X, start.Y, i), new BPoint (end.X, end.Y, ++i)));
         i++;
      }
      return (newPlines, newBendLines);
   }
   #endregion

   #region Properties -----------------------------------------------
   public readonly bool IsContourChanged => mIsContourChanged;
   public readonly List<BPoint> Vertices => mVertices;
   public readonly List<PLine> PLines => mPlines;
   public readonly List<BendLine> BendLines => mBendLines;
   public readonly Bound Bound => mBound;
   public readonly BPoint Centroid => mCentroid;
   #endregion

   #region Private Data ---------------------------------------------
   Bound mBound;
   BPoint mCentroid;
   List<BendLine> mBendLines;
   List<PLine> mPlines;
   List<BPoint> mVertices;
   bool mIsContourChanged = false;
   #endregion
}
#endregion