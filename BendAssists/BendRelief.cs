namespace BendMaker;

#region class BendRelief --------------------------------------------------------------------------
public class BendRelief {
    #region Constructors ---------------------------------------------
    public BendRelief () => mPLines = [];
    #endregion

    #region Methods --------------------------------------------------
    public BendProcessedPart ApplyBendRelief (Part profile) {
        mPLines.Clear ();
        foreach (var curve in profile.PLines) mPLines.Add (curve);
        var hCurves = mPLines.Where (IsHorizontal).ToList ();
        var vCurves = mPLines.Where (IsVertical).ToList ();

        if (profile.BendLines.Count () != 0)
            foreach (var bLine in profile.BendLines) {
                // Intersected Line - Line intersected by the bendline
                var ILine = (IsHorizontal (bLine) ? vCurves : hCurves).Where (x => IsIntersecting (x, bLine)).First ();

                // Nearest line from the bendline
                var NLine = GetNearestParallelCurve (IsHorizontal (bLine) ? hCurves : vCurves, bLine);

                // Non Intersecting Point of the bendline
                var NIPoint = IsHorizontal (bLine) ? (EQ (bLine.StartPoint.X, ILine.StartPoint.X) ? bLine.EndPoint : bLine.StartPoint)
                                                   : (EQ (bLine.StartPoint.Y, ILine.StartPoint.Y) ? bLine.EndPoint : bLine.StartPoint);

                // Intersecting point of bendline and the intersected line
                var IPoint = EQ (bLine.StartPoint, NIPoint) ? bLine.EndPoint : bLine.StartPoint;

                // Check if the profile has a stepcut
                mHasStepCut = IsHorizontal (bLine) ? vCurves.Where (x => EQ (x.StartPoint.X, NIPoint.X) || EQ (x.EndPoint.X, NIPoint.X)).Count () == 1
                                                      : hCurves.Where (x => EQ (x.StartPoint.Y, NIPoint.Y) || EQ (x.EndPoint.Y, NIPoint.Y)).Count () == 1;
                AddBendReliefPoints (bLine, ILine, NLine, IPoint, NIPoint, profile.Vertices.Centroid (), profile.Thickness);
            }
        return new BendProcessedPart (EBDAlgorithm.EquallyDistributed, GetOrderedLines (mPLines), profile.BendLines, true);
    }

    // Get bend relief points and create 
    void AddBendReliefPoints (BendLine bLine, PLine ILine, PLine NLine, BPoint IPoint, BPoint NIPoint, BPoint centre, double thickness) {
        var isHorizontal = IsHorizontal (bLine);

        // Offset between bendline and nearest parallel line
        double offset = GetDistanceToLine (NLine, bLine),
               brHeight = BendUtils.GetBendAllowance (90, 0.38, thickness, 2) / 2,
               brWidth = thickness / 2;

        BPoint p1, p2, p3, p4, p5, p6;
        p1 = new (
            NIPoint.X + (isHorizontal ? 0 : (NIPoint.X < centre.X ? -1 : 1) * offset),
            NIPoint.Y + (isHorizontal ? ((NIPoint.Y > centre.Y ? 1 : -1) * offset) : 0)
        );
        p2 = new (
            NIPoint.X + (isHorizontal ? 0 : (NIPoint.X < centre.X ? 1 : -1) * brHeight),
            NIPoint.Y + (isHorizontal ? ((NIPoint.Y > centre.Y ? -1 : 1) * brHeight) : 0)
        );
        p3 = new (
            p2.X + (isHorizontal ? ((ILine.StartPoint.X > centre.X ? -1 : 1) * brWidth) : 0),
            p2.Y + (isHorizontal ? 0 : ((ILine.StartPoint.Y > centre.Y ? -1 : 1) * brWidth))
        );
        p4 = new (
            (mHasStepCut ? NIPoint.X : p1.X) + (isHorizontal ? ((ILine.StartPoint.X < centre.X ? 1 : -1) * brWidth) : 0),
            (mHasStepCut ? NIPoint.Y : p1.Y) + (isHorizontal ? 0 : ((ILine.StartPoint.Y < centre.Y ? 1 : -1) * brWidth))
        );
        p5 = new (
            (mHasStepCut ? NIPoint.X : IPoint.X) + (isHorizontal ? 0 : (IPoint.X > centre.X ? 1 : -1) * offset),
            (mHasStepCut ? NIPoint.Y : IPoint.Y) + (isHorizontal ? (IPoint.Y > centre.Y ? 1 : -1) * offset : 0)
        );
        p6 = EQ (NLine.StartPoint, p5) ? NLine.EndPoint : NLine.StartPoint;

        List<BPoint> orderedPoints = isHorizontal ? ((IPoint.X > centre.X && IPoint.Y > centre.Y)
                                                 || (IPoint.X < centre.X && IPoint.Y < centre.Y))
                                                  ? [p5, p1, p2, p3, p4, p6] : [p6, p4, p3, p2, p1, p5]
                                                  : ((IPoint.X < centre.X && IPoint.Y > centre.Y)
                                                 || (IPoint.X > centre.X && IPoint.Y < centre.Y))
                                                  ? [p5, p1, p2, p3, p4, p6] : [p6, p4, p3, p2, p1, p5];

        if (mHasStepCut) orderedPoints.Remove (p5);
        for (int i = 0; i <= orderedPoints.Count - 2; i++)
            mPLines.Add (new PLine (ECurve.Line, -1, orderedPoints[i], orderedPoints[i + 1]));
        mPLines.Remove (NLine);
    }

    // Checks if line and bendline intersects
    bool IsIntersecting (PLine line, BendLine bLine) =>
         IsHorizontal (bLine) ? (EQ (line.StartPoint.X, bLine.StartPoint.X) || EQ (line.StartPoint.X, bLine.EndPoint.X))
                              : (EQ (line.StartPoint.Y, bLine.StartPoint.Y) || EQ (line.StartPoint.Y, bLine.EndPoint.Y));

    // Gets the nearest parallel curve to the bendline
    PLine GetNearestParallelCurve (List<PLine> curves, BendLine bLine) =>
        curves.OrderBy (curve => GetDistanceToLine (curve, bLine)).FirstOrDefault ();

    // Get the distance of a line from the bendline
    double GetDistanceToLine (PLine line, BendLine bLine) =>
        IsHorizontal (bLine) ? Math.Abs (line.StartPoint.Y - bLine.StartPoint.Y)
                             : Math.Abs (line.StartPoint.X - bLine.StartPoint.X);

    public List<PLine> GetOrderedLines (List<PLine> pLines) {
        List<PLine> orderedPLines = new () { pLines[0] };
        pLines.RemoveAt (0);
        while (pLines.Count > 0) {
            BPoint LEPoint = orderedPLines.Last ().EndPoint; // Endpoint of last line
            var nxtLine = pLines.FirstOrDefault (line => EQ (line.StartPoint, LEPoint));
            orderedPLines.Add (nxtLine);
            pLines.Remove (nxtLine);
        }
        return orderedPLines;
    }
    #endregion

    #region Private Data --------------------------------------------
    bool mHasStepCut;
    List<PLine> mPLines;
    #endregion

    #region Commented ------------------------------------------------
    //Methods to compare two doubles or two points
    // Can be added inside Bend Utils class
    bool EQ (double x, double y) => Math.Abs (x - y) < 1e-9;
    bool EQ (BPoint p1, BPoint p2) {
        double epsilon = 1e-9;
        return Math.Abs (p1.X - p2.X) < epsilon && Math.Abs (p1.Y - p2.Y) < epsilon;
    }

    // Methods to check if a line or bendline is horizontal or vertical or inclined
    // Can be added as properties to respective classes
    // Used IsHorizontal method above
    bool IsHorizontal (PLine line) => EQ (line.StartPoint.Y, line.EndPoint.Y);
    bool IsVertical (PLine line) => EQ (line.StartPoint.X, line.EndPoint.X);
    bool IsInclined (PLine line) => !EQ (line.StartPoint.Y, line.EndPoint.Y) && !EQ (line.StartPoint.X, line.EndPoint.X);
    bool IsHorizontal (BendLine bLine) => bLine.Orientation == EBLOrientation.Horizontal;
    bool IsVertical (BendLine bLine) => bLine.Orientation == EBLOrientation.Vertical;
    bool IsInclined (BendLine bLine) => bLine.Orientation == EBLOrientation.Inclined;
    #endregion
}
#endregion