namespace BendMaker;

#region class CornerRelief -------------------------------------------------------------------------
public class CornerRelief (Part part) {

    #region Methods --------------------------------------------------
    public BendProcessedPart ApplyCornerRelief () {
        if (mOrgBp.PLines.Count > 0) {
            foreach (var point in mOrgBp.PLines) {
                mCurvesStartPts.Add (point.StartPoint);
                mCurvesEndPts.Add (point.EndPoint);
            }
        }

        if (mOrgBp.BendLines.Count >= 2) {
            for (int i = 0, j = 0; i < mOrgBp.BendLines.Count - 1; i++) {
                var a = GetCommonBendPoints (mOrgBp.BendLines[i].StartPoint, i);
                if (a.Item2.Count > 0) mCommonBendPoints.Add (j++, a.Item2);
                var b = GetCommonBendPoints (mOrgBp.BendLines[i].EndPoint, i);
                if (b.Item2.Count > 0) mCommonBendPoints.Add (j++, b.Item2);
            }
        }


        for (int i = 0; i < mCommonBendPoints.Count; i++) {
            var a = mCommonBendPoints[i];
            if (mCurvesStartPts.Contains (a[0])) mCommonBendAndCurvePts.Add (a[0]);
            else if (mCurvesEndPts.Contains (a[0])) mCommonBendAndCurvePts.Add (a[0]);
        }

        UpdatedVertices ();
        return new BendProcessedPart (EBDAlgorithm.PartiallyDistributed, UpdatedPLines (), mOrgBp.BendLines, true);
    }

    public (int, List<BPoint>) GetCommonBendPoints (BPoint point, int i) {
        List<BPoint> tempBL = new ();
        int j = i, k = i;
        while (++j < mOrgBp.BendLines.Count) {
            if (point == mOrgBp.BendLines[++k].StartPoint && !mTempBPoints.Contains (point))
                tempBL.Add (point);
            if (point == mOrgBp.BendLines[k].EndPoint && !mTempBPoints.Contains (point))
                tempBL.Add (point);
        }
        if (!mTempBPoints.Contains (point)) mTempBPoints.Add (point);
        return (tempBL.Count, tempBL);
    }
    #endregion

    #region Implementation -------------------------------------------
    List<BPoint> UpdatedVertices () {
        Dictionary<BPoint, List<BPoint>> commonPointAndBendLines = new ();
        mBendAllowance = BendUtils.GetBendAllowance (90, 0.38, 2, 2); //Material 1.0038
        for (int i = 0; i < mCommonBendAndCurvePts.Count; i++) {
            List<BPoint> tempBPoint = new ();
            foreach (var a in mOrgBp.BendLines) {
                if (a.StartPoint == mCommonBendAndCurvePts[i]) tempBPoint.Add (a.EndPoint);
                if (a.EndPoint == mCommonBendAndCurvePts[i]) tempBPoint.Add (a.StartPoint);
            }
            commonPointAndBendLines.Add (mCommonBendAndCurvePts[i], tempBPoint);
        }

        for (int i = 0; i < mCommonBendAndCurvePts.Count; i++) {
            var point = mCommonBendAndCurvePts[i];
            List<BPoint> bpoint = commonPointAndBendLines[point];
            double x = 0, y = 0;

            if (point.X < Math.Abs (bpoint[1].X - bpoint[0].X)) x = point.X + Math.Round ((mBendAllowance / 2), 3);
            else x = point.X - Math.Round ((mBendAllowance / 2), 3);

            if (point.Y < Math.Abs (bpoint[1].Y - bpoint[0].Y)) y = point.Y + Math.Round ((mBendAllowance / 2), 3);
            else y = point.Y - Math.Round ((mBendAllowance / 2), 3);

            mNew45DegVertices.Add (new BPoint (x, y));
        }

        return mNew45DegVertices;
    }

    List<PLine> UpdatedPLines () {
        List<int> changingIndex = new ();
        foreach (var a in mOrgBp.PLines) {
            for (int i = 0; i < mCommonBendAndCurvePts.Count; i++) {
                if (a.StartPoint == mCommonBendAndCurvePts[i]) changingIndex.Add (a.Index);
                else if (a.EndPoint == mCommonBendAndCurvePts[i]) changingIndex.Add (a.Index);
            }
            mOrgCurves.Add ([a.StartPoint, a.EndPoint]);
        }

        int len = 0;
        if (mNew45DegVertices.Count == 1) len = mOrgBp.PLines.Count + (mNew45DegVertices.Count * 2) - 1;
        else len = mOrgBp.PLines.Count + (mNew45DegVertices.Count * 2) - 2;
        for (int i = 0, j = 0, k = 0; mPLines.Count <= len; i++) {
            if (k == 0 && i <= mOrgBp.PLines.Count - changingIndex.Count && !changingIndex.Contains (i)) {
                mPLines.Add (mOrgBp.PLines[i]);
            } else if (!changingIndex.Contains (i)) {
                List<BPoint> temp = [.. mOrgCurves[i]];

                mPLines.Add (new PLine (ECurve.Line, j++, temp[0], temp[1]));
            } else {
                if (k == 0) {
                    j = i; k = 1;
                }
                List<BPoint> temp = [];
                int choose = 0;
                foreach (var Point in mCommonBendAndCurvePts) {

                    if (mOrgBp.PLines[i].StartPoint == Point) {
                        temp = GetPLines (mOrgBp.PLines[i], mOrgBp.PLines[i + 1], Point, i, mNew45DegVertices[choose]);
                        break;
                    } else if (mOrgBp.PLines[i].EndPoint == Point) {
                        temp = GetPLines (mOrgBp.PLines[i], mOrgBp.PLines[i + 1], Point, i, mNew45DegVertices[choose]);
                        break;
                    }
                    choose++;
                }

                mPLines.Add (new PLine (ECurve.Line, j++, temp[0], temp[1]));
                mPLines.Add (new PLine (ECurve.Line, j++, temp[1], temp[2]));
                mPLines.Add (new PLine (ECurve.Line, j++, temp[2], temp[3]));
                mPLines.Add (new PLine (ECurve.Line, j++, temp[3], temp[4]));
                i += 1;
            }
        }

        return mPLines;
    }

    List<BPoint> GetPLines (PLine first, PLine second, BPoint commonPoint, int index, BPoint newPoint) {
        List<BPoint> tmp = [];
        double px1 = first.StartPoint.X, px2 = second.EndPoint.X, py1 = first.StartPoint.Y,
           py2 = second.EndPoint.Y, cpx1 = commonPoint.X, cpx2 = commonPoint.Y;
        tmp.Add (first.StartPoint);
        tmp.Add (GetBPoint (px1, py1, cpx1, cpx2, mBendAllowance));
        tmp.Add (newPoint);
        tmp.Add (GetBPoint (px2, py2, cpx1, cpx2, mBendAllowance));
        tmp.Add (second.EndPoint);
        return tmp;
    }

    BPoint GetBPoint (double px, double py, double cx, double cy, double BA) {
        if (cx - px == 0 && cy > py) return new BPoint (cx, (cy - BA / 2));
        else if (cx - px == 0 && cy < py) return new BPoint (cx, cy + BA / 2);
        else if (cy - py == 0 && cx > px) return new BPoint (cx - BA / 2, cy);
        else if (cy - py == 0 && cx < px) return new BPoint (cx + BA / 2, cy);
        return new BPoint ();
    }
    #endregion

    #region Private Data ---------------------------------------------
    Part mOrgBp = part, mAfterCrBp;
    Dictionary<int, List<BPoint>> mCommonBendLinesIntersect = new Dictionary<int, List<BPoint>> (), mCommonBendPoints = new ();
    List<BPoint> mCurvesStartPts = new (), mCurvesEndPts = new (), mBendLineStartpts = new (),
       mBendLineEndPts = new (), mCommonBendAndCurvePts = new (),
       mNewVertices = new (), mTempBPoints = new (), mNew45DegVertices = new ();
    List<PLine> mPLines = new List<PLine> ();
    Dictionary<BPoint, List<BPoint>> mUpdatedVertices = new Dictionary<BPoint, List<BPoint>> ();
    double mBendAllowance;
    List<List<BPoint>> mOrgCurves = new ();
    #endregion
}
#endregion