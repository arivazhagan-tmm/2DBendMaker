
namespace BendMaker;
public class CRProfile (Profile profile) {
   public BendProfile Validation () {
      if (mOrgBp.Curves.Count > 0) {
         foreach (var point in mOrgBp.Curves) {
            mCurvesStartPts.Add (point.StartPoint);
            mCurvesEndPts.Add (point.EndPoint);
         }
      }

      //#region UPCOMING WORK --------------------------------------
      //if (mOrgBp.BendLines.Count > 0) {
      //   foreach (var point in mOrgBp.BendLines) {
      //      mBendLineStartpts.Add (point.StartPoint);
      //      mBendLineEndPts.Add (point.EndPoint);
      //   }

      //   for (int i = 0; i < mBendLineStartpts.Count -1 ; i++) {
      //      if (mBendLineEndPts.GetRange (i, i + 1).Contains (mBendLineStartpts[i]))
      //         mCommonBendPoints.Add (mBendLineStartpts[i]);
      //      else if (mBendLineStartpts.GetRange(i,i+1).Contains (mBendLineEndPts[i]))
      //         mCommonBendPoints.Add (mBendLineEndPts[i]);
      //   }

      //   foreach (var point in mCommonBendPoints) {
      //      if (mCurvesStartPts.Contains (point)) mCommonBendAndCurvePts.Add (point);
      //      else if (mCurvesEndPts.Contains (point)) mCommonBendAndCurvePts.Add (point);
      //   }
      //}

      //if (mCommonBendPoints.Count == 0) {
      //   //need to improve
      //   LinearEqn linearEqn = new ();
      //   linearEqn.MakeLinearEqn (mOrgBp.BendLines[0].StartPoint, mOrgBp.BendLines[0].EndPoint);
      //   linearEqn.MakeLinearEqn (mOrgBp.BendLines[1].StartPoint, mOrgBp.BendLines[1].EndPoint);
      //   mCommonBendPoints.Add (linearEqn.GetIntersectPoint ());

      //}
      //#endregion

      if (mOrgBp.BendLines.Count >= 2) {
         // need to improve
         for (int i = 0; i < mOrgBp.BendLines.Count - 1; i++) {
            if (mOrgBp.BendLines[i].StartPoint == mOrgBp.BendLines[i + 1].StartPoint)
               mCommonBendPoints.Add (mOrgBp.BendLines[i].StartPoint);
            else if (mOrgBp.BendLines[i].StartPoint == mOrgBp.BendLines[i + 1].EndPoint)
               mCommonBendPoints.Add (mOrgBp.BendLines[i].StartPoint);
            else if (mOrgBp.BendLines[i].EndPoint == mOrgBp.BendLines[i + 1].StartPoint)
               mCommonBendPoints.Add (mOrgBp.BendLines[i].EndPoint);
            else if (mOrgBp.BendLines[i].EndPoint == mOrgBp.BendLines[i + 1].EndPoint)
               mCommonBendPoints.Add (mOrgBp.BendLines[i].EndPoint);
         }
      }



      foreach (var point in mCommonBendPoints) {
         if (mCurvesStartPts.Contains (point)) mCommonBendAndCurvePts.Add (point);
         else if (mCurvesEndPts.Contains (point)) mCommonBendAndCurvePts.Add (point);
      }

      UpdatedVertices ();
      //mCurves = mOrgBp.Curves;
      //mOrgBp.Curves = UpdatedCurves ();

      return new BendProfile (EBDAlgorithm.PartiallyDistributed, UpdatedCurves (), mOrgBp.BendLines);
   }

   public List<BPoint> UpdatedVertices () {
      var bendAllowance = BendUtils.GetBendAllowance (90, 0.38, 2, 2); //Material 1.0038
      var commonVertex = mCommonBendAndCurvePts[0];
      mNewVertices.Add (new BPoint (Math.Round (commonVertex.X, 3), Math.Round (commonVertex.Y + (bendAllowance / 2), 3)));
      mNewVertices.Add (new BPoint (Math.Round (commonVertex.X + (bendAllowance / 2), 3), Math.Round (commonVertex.Y - (bendAllowance / 2), 3)));
      mNewVertices.Add (new BPoint (Math.Round (commonVertex.X - (bendAllowance / 2), 3), Math.Round (commonVertex.Y, 3)));
      return mNewVertices;
   }

   public List<Curve> UpdatedCurves () {
      List<int> changingVertex = new ();
      foreach (var a in mOrgBp.Curves) {
         if (a.StartPoint == mCommonBendAndCurvePts[0]) changingVertex.Add (a.Index);
         else if (a.EndPoint == mCommonBendAndCurvePts[0]) changingVertex.Add (a.Index);
      }

      int len = mOrgBp.Curves.Count + mNewVertices.Count - 2;

      for (int i = 0, j = 0; i <= len; i++) {
         if (i <= mOrgBp.Curves.Count - changingVertex.Count && !changingVertex.Contains (i)) mCurves.Add (mOrgBp.Curves[i]);
         else {
            if (i == changingVertex[0])
               mCurves.Add (new Curve (ECurve.Line, i,
                 "", new BPoint (mOrgBp.Curves[i].StartPoint.X, mOrgBp.Curves[i].StartPoint.Y),
                 new BPoint (mNewVertices[j].X, mNewVertices[j].Y)));
            else if (i != len) {
               mCurves.Add (new Curve (ECurve.Line, i,
                  "", new BPoint (mNewVertices[j].X, mNewVertices[j].Y),
                  new BPoint (mNewVertices[j + 1].X, mNewVertices[j + 1].Y)));
               j++;
            } else if (i == len)
               mCurves.Add (new Curve (ECurve.Line, i, "",
                  new BPoint (mNewVertices[j].X, mNewVertices[j].Y),
                  new BPoint (mOrgBp.Curves[i - j].EndPoint.X, mOrgBp.Curves[i - j].EndPoint.Y)));
         }
      }

      return mCurves;
   }



   Profile mOrgBp = profile, mAfterCrBp;
   Dictionary<int, List<BendLine>> mCommonBendLinesIntersect = new Dictionary<int, List<BendLine>> ();
   List<BPoint> mCurvesStartPts = new (), mCurvesEndPts = new (), mBendLineStartpts = new (),
      mBendLineEndPts = new (), mCommonBendPoints = new (), mCommonBendAndCurvePts = new (),
      mNewVertices = new ();
   List<Curve> mCurves = new List<Curve> ();

}