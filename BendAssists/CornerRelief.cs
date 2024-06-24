namespace BendMaker;

#region class CornerRelief ------------------------------------------------------------------------
public class CornerRelief (Part part) {
   #region Properties -----------------------------------------------
   public string ErrorMessage => "No bend line intersection exists";
   #endregion

   #region Methods --------------------------------------------------
   /// <summary>Create a V-Notch corner relief for the intersection of the bend lines.</summary>
   /// <param name="part">The bend processed part.</param>
   /// <returns> Return values:
   /// true: The new BendProcessPart object is sent, and also a preview of the part before
   /// and after corner relief shown in the UI.
   /// false: It send an error message to the user.
   /// </returns>
   public bool ApplyCornerRelief (out BendProcessedPart part) {
      if (mPart.BendLines.Count >= 2) {
         for (int i = 0; i < mPart.BendLines.Count - 1; i++) {
            var start = GetCommonBendPoints (mPart.BendLines[i].StartPoint, i);
            if (start.repeatedPoints > 0) mCommonBendPoints.Add (start.cPoint);
            var end = GetCommonBendPoints (mPart.BendLines[i].EndPoint, i);
            if (end.repeatedPoints > 0) mCommonBendPoints.Add (end.cPoint);
         }
      }
      if (mCommonBendPoints.Count == 0) {
         part = new BendProcessedPart (mPart.PLines, mPart.BendLines);
         return false;
      }
      if (mPart.PLines.Count > 0) {
         foreach (var point in mPart.PLines) {
            mPlinesStartPts.Add (point.StartPoint);
            mPlinesEndPts.Add (point.EndPoint);
         }
         for (int i = 0; i < mCommonBendPoints.Count; i++) {
            if (mPlinesStartPts.Contains (mCommonBendPoints[i]))
               mCommonBendAndPlinesPts.Add (mCommonBendPoints[i]);
            else if (mPlinesEndPts.Contains (mCommonBendPoints[i]))
               mCommonBendAndPlinesPts.Add (mCommonBendPoints[i]);
         }
      }
      UpdatedVertices ();
      part = new BendProcessedPart (UpdatedPLines (), mPart.BendLines);
      return true;
   }

   /// <summary>Find a common point between the two bend lines.</summary>
   /// <param name="point">The point value of a bend line,whether it's starting or ending.</param>
   /// <param name="index">Index value for the list of bend lines.</param>
   /// <returns>Return values:
   ///  0,new BPoint(): When the size of  tempBPoint is equal to zero.
   ///  x,tempBPoint[0]: If tempBPoint is larger than zero and returns the first
   ///  index value in a list. Here, 'x' is a natural number.
   /// </returns>
   (int repeatedPoints, BPoint cPoint) GetCommonBendPoints (BPoint point, int index) {
      List<BPoint> tempBPoint = [];
      while (++index < mPart.BendLines.Count) {
         if (point == mPart.BendLines[index].StartPoint) tempBPoint.Add (point);
         if (point == mPart.BendLines[index].EndPoint) tempBPoint.Add (point);
      }
      if (tempBPoint.Count == 0) return (0, new BPoint ());
      return (tempBPoint.Count, tempBPoint[0]);
   }
   #endregion

   #region Implementation -------------------------------------------
   /// <summary>Find a point that is 45 degrees from a common point for both the bend lines
   /// and the plines.</summary>
   /// <returns>It generates a list of bpoint.</returns>
   List<BPoint> UpdatedVertices () {
      Dictionary<BPoint, List<BPoint>> commonPointAndBendLines = [];
      mBendAllowance = Math.Round (BendUtils.GetBendAllowance (90, 0.38, 2, 2), 3); // Material 1.0038
      for (int i = 0; i < mCommonBendAndPlinesPts.Count; i++) {
         List<BPoint> tempBPoint = [];
         foreach (var pLine in mPart.BendLines) {
            if (pLine.StartPoint == mCommonBendAndPlinesPts[i]) tempBPoint.Add (pLine.EndPoint);
            if (pLine.EndPoint == mCommonBendAndPlinesPts[i]) tempBPoint.Add (pLine.StartPoint);
         }
         commonPointAndBendLines.Add (mCommonBendAndPlinesPts[i], tempBPoint);
      }
      for (int i = 0; i < mCommonBendAndPlinesPts.Count; i++) {
         List<BPoint> bendLinePoints = commonPointAndBendLines[mCommonBendAndPlinesPts[i]];
         double x, y;
         if (mCommonBendAndPlinesPts[i].X < Math.Abs (bendLinePoints[1].X - bendLinePoints[0].X))
            x = mCommonBendAndPlinesPts[i].X + Math.Round (mBendAllowance / 2, 3);
         else x = mCommonBendAndPlinesPts[i].X - Math.Round (mBendAllowance / 2, 3);
         if (mCommonBendAndPlinesPts[i].Y < Math.Abs (bendLinePoints[1].Y - bendLinePoints[0].Y))
            y = mCommonBendAndPlinesPts[i].Y + Math.Round (mBendAllowance / 2, 3);
         else y = mCommonBendAndPlinesPts[i].Y - Math.Round (mBendAllowance / 2, 3);
         mNew45DegVertices.Add (new BPoint (x, y));
      }
      return mNew45DegVertices;
   }

   /// <summary>To locate the new plines for the bend process part.</summary>
   /// <returns>It generates a list of pline.</returns>
   List<PLine> UpdatedPLines () {
      List<int> changingIndex = [];
      List<List<BPoint>> orgPLines = [];
      foreach (var pLine in mPart.PLines) {
         for (int i = 0; i < mCommonBendAndPlinesPts.Count; i++) {
            if (pLine.StartPoint == mCommonBendAndPlinesPts[i]) changingIndex.Add (pLine.Index);
            else if (pLine.EndPoint == mCommonBendAndPlinesPts[i]) changingIndex.Add (pLine.Index);
         }
         orgPLines.Add ([pLine.StartPoint, pLine.EndPoint]);
      }
      int len = mPart.PLines.Count + (mNew45DegVertices.Count * 2);
      List<PLine> tempPLines = [];
      for (int i = 0, indexer = 0, loopBreaker = 0; tempPLines.Count < len; i++) {
         if (loopBreaker == 0 && !changingIndex.Contains (i))
            tempPLines.Add (mPart.PLines[i]);
         else if (!changingIndex.Contains (i)) {
            List<BPoint> temp = [.. orgPLines[i]];
            tempPLines.Add (new PLine (ECurve.Line, indexer++, temp[0], temp[1]));
         } else {
            if (loopBreaker == 0) {
               indexer = i; loopBreaker = 1;
            }
            int choose = 0;
            List<BPoint> tempBPoint = [];
            foreach (var Point in mCommonBendAndPlinesPts) {
               if (mPart.PLines[i].StartPoint == Point) {
                  tempBPoint = GetPLines (mPart.PLines[i], mPart.PLines[i + 1], Point, mNew45DegVertices[choose]);
                  break;
               } else if (mPart.PLines[i].EndPoint == Point) {
                  tempBPoint = GetPLines (mPart.PLines[i], mPart.PLines[i + 1], Point, mNew45DegVertices[choose]);
                  break;
               }
               choose++;
            }
            tempPLines.Add (new PLine (ECurve.Line, indexer++, tempBPoint[0], tempBPoint[1]));
            tempPLines.Add (new PLine (ECurve.Line, indexer++, tempBPoint[1], tempBPoint[2]));
            tempPLines.Add (new PLine (ECurve.Line, indexer++, tempBPoint[2], tempBPoint[3]));
            tempPLines.Add (new PLine (ECurve.Line, indexer++, tempBPoint[3], tempBPoint[4]));
            i += 1;
         }
      }
      return tempPLines;
   }

   /// <summary>To find a new pline for the part.</summary>
   /// <param name="first">The present index pline for a particular part.</param>
   /// <param name="second">The consecutive index ("first") pline for a particular part.</param>
   /// <param name="cPoint">A point that is shared by both the lines and the bend lines.</param>
   /// <param name="new45DegPoint">The common point is located at a 45-degree angle.</param>
   /// <returns>It generates a list of bpoint.</returns>
   List<BPoint> GetPLines (PLine first, PLine second, BPoint cPoint, BPoint new45DegPoint) {
      List<BPoint> tempBPoint = [];
      double px1 = Math.Round (first.StartPoint.X, 3), px2 = Math.Round (second.EndPoint.X, 3),
         py1 = Math.Round (first.StartPoint.Y, 3), py2 = Math.Round (second.EndPoint.Y, 3),
         cpx1 = Math.Round (cPoint.X, 3), cpx2 = Math.Round (cPoint.Y, 3);
      tempBPoint.Add (first.StartPoint);
      tempBPoint.Add (GetBPoint (px1, py1, cpx1, cpx2, mBendAllowance));
      tempBPoint.Add (new45DegPoint);
      tempBPoint.Add (GetBPoint (px2, py2, cpx1, cpx2, mBendAllowance));
      tempBPoint.Add (second.EndPoint);
      return tempBPoint;
   }

   /// <summary>To find a new bpoint for the pline.</summary>
   /// <param name="px">The x-coordinate value for a pline, it can be either starting point or ending point.</param>
   /// <param name="py">The y-coordinate value for a pline, it can be either starting point or ending point..</param>
   /// <param name="cx">The x-coordinate value for a common point.</param>
   /// <param name="cy">The y-coordinate value for a common point.</param>
   /// <param name="BA">Bend allowance value for a material (1.0038).</param>
   /// <returns>It returns a new bpoint.</returns>
   static BPoint GetBPoint (double px, double py, double cx, double cy, double BA) {
      if (cx == px && cy > py) return new BPoint (cx, (cy - BA / 2));
      else if (cx == px && cy < py) return new BPoint (cx, cy + BA / 2);
      else if (cy == py && cx > px) return new BPoint (cx - BA / 2, cy);
      else if (cy == py && cx < px) return new BPoint (cx + BA / 2, cy);
      return new BPoint ();
   }
   #endregion

   #region Private Data ---------------------------------------------
   Part mPart = part;
   List<BPoint> mCommonBendPoints = [], 
                mPlinesStartPts = [], 
                mPlinesEndPts = [],
                mCommonBendAndPlinesPts = [], 
                mNew45DegVertices = [];
   double mBendAllowance;
   #endregion
}
#endregion