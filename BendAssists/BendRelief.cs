namespace BendMaker;

#region class BendRelief --------------------------------------------------------------------------
public class BendRelief {
    #region Constructors --------------------------------------------
    public BendRelief () => mPLines = [];
    #endregion

    #region Methods -------------------------------------------------
    /// <summary>Adds bend relief to the profile</summary>
    /// Takes the profile and finds bend relief points
    /// creates new lines based on the bend relief points
    /// returns the bend processed part based on the new list of lines
    public bool ApplyBendRelief (Part profile, out BendProcessedPart part) {
        part = new BendProcessedPart ();
        mPLines.Clear ();
        foreach (var line in profile.PLines) mPLines.Add (line);
        var hLines = mPLines.Where (IsHorizontal).ToList ();
        var vLines = mPLines.Where (IsVertical).ToList ();
        if (profile.BendLines.Count () != 0)
            foreach (var bLine in profile.BendLines) {
                // Line intersected by the bendline
                var iLine = (IsHorizontal (bLine) ? vLines : hLines).Where (x => IsIntersecting (x, bLine)).First ();

                // Nearest line from the bendline
                var nLine = GetNearestParallelLine (IsHorizontal (bLine) ? hLines : vLines, bLine);

                // Non Intersecting Point of the bendline
                var niPoint = IsHorizontal (bLine) ? (EQ (bLine.StartPoint.X, iLine.StartPoint.X) ? bLine.EndPoint : bLine.StartPoint)
                                                   : (EQ (bLine.StartPoint.Y, iLine.StartPoint.Y) ? bLine.EndPoint : bLine.StartPoint);

                // Intersecting point of bendline and the intersected line
                var iPoint = EQ (bLine.StartPoint, niPoint) ? bLine.EndPoint : bLine.StartPoint;

                // Check if the profile has a stepcut
                mHasStepCut = IsHorizontal (bLine) ? vLines.Where (x => EQ (x.StartPoint.X, niPoint.X) || EQ (x.EndPoint.X, niPoint.X)).Count () == 1
                                                      : hLines.Where (x => EQ (x.StartPoint.Y, niPoint.Y) || EQ (x.EndPoint.Y, niPoint.Y)).Count () == 1;
                AddBendReliefPoints (bLine, iLine, nLine, iPoint, niPoint, profile.Vertices.Centroid (), profile.Thickness);
            }
        else return false;
        part = new BendProcessedPart (EBDAlgorithm.EquallyDistributed, GetOrderedLines (mPLines), profile.BendLines, true);
        return true;
    }
    #endregion

    #region Implementation ------------------------------------------
    // Get bend relief points and create new lines
    void AddBendReliefPoints (BendLine bLine, PLine iLine, PLine nLine, BPoint iPoint, BPoint niPoint, BPoint centre, double thickness) {
        var isHorizontal = IsHorizontal (bLine);

        // Offset between bendline and nearest parallel line
        double offset = GetDistanceToLine (nLine, bLine),
               brHeight = BendUtils.GetBendAllowance (90, 0.38, thickness, 2) / 2,
               brWidth = thickness / 2;

        BPoint p1, p2, p3, p4, p5, p6; // Bend relief points
        p1 = new (
            niPoint.X + (isHorizontal ? 0 : (niPoint.X < centre.X ? -1 : 1) * offset),
            niPoint.Y + (isHorizontal ? ((niPoint.Y > centre.Y ? 1 : -1) * offset) : 0)
        );
        p2 = new (
            niPoint.X + (isHorizontal ? 0 : (niPoint.X < centre.X ? 1 : -1) * brHeight),
            niPoint.Y + (isHorizontal ? ((niPoint.Y > centre.Y ? -1 : 1) * brHeight) : 0)
        );
        p3 = new (
            p2.X + (isHorizontal ? ((iLine.StartPoint.X > centre.X ? -1 : 1) * brWidth) : 0),
            p2.Y + (isHorizontal ? 0 : ((iLine.StartPoint.Y > centre.Y ? -1 : 1) * brWidth))
        );
        p4 = new (
            (mHasStepCut ? niPoint.X : p1.X) + (isHorizontal ? ((iLine.StartPoint.X < centre.X ? 1 : -1) * brWidth) : 0),
            (mHasStepCut ? niPoint.Y : p1.Y) + (isHorizontal ? 0 : ((iLine.StartPoint.Y < centre.Y ? 1 : -1) * brWidth))
        );
        p5 = new (
            (mHasStepCut ? niPoint.X : iPoint.X) + (isHorizontal ? 0 : (iPoint.X > centre.X ? 1 : -1) * offset),
            (mHasStepCut ? niPoint.Y : iPoint.Y) + (isHorizontal ? (iPoint.Y > centre.Y ? 1 : -1) * offset : 0)
        );
        p6 = EQ (nLine.StartPoint, p5) ? nLine.EndPoint : nLine.StartPoint;

        List<BPoint> orderedPoints = isHorizontal ? ((iPoint.X > centre.X && iPoint.Y > centre.Y)
                                                 || (iPoint.X < centre.X && iPoint.Y < centre.Y))
                                                  ? [p5, p1, p2, p3, p4, p6] : [p6, p4, p3, p2, p1, p5]
                                                  : ((iPoint.X < centre.X && iPoint.Y > centre.Y)
                                                 || (iPoint.X > centre.X && iPoint.Y < centre.Y))
                                                  ? [p5, p1, p2, p3, p4, p6] : [p6, p4, p3, p2, p1, p5];

        if (mHasStepCut) orderedPoints.Remove (p5);
        for (int i = 0; i <= orderedPoints.Count - 2; i++)
            mPLines.Add (new PLine (ECurve.Line, -1, orderedPoints[i], orderedPoints[i + 1]));
        mPLines.Remove (nLine);
    }

    // Checks if line and bendline intersects
    bool IsIntersecting (PLine line, BendLine bLine) =>
         IsHorizontal (bLine) ? (EQ (line.StartPoint.X, bLine.StartPoint.X) || EQ (line.StartPoint.X, bLine.EndPoint.X))
                              : (EQ (line.StartPoint.Y, bLine.StartPoint.Y) || EQ (line.StartPoint.Y, bLine.EndPoint.Y));

    // Gets the nearest parallel curve to the bendline
    PLine GetNearestParallelLine (List<PLine> lines, BendLine bLine) =>
        lines.OrderBy (line => GetDistanceToLine (line, bLine)).FirstOrDefault ();

    // Get the distance of a line from the bendline
    double GetDistanceToLine (PLine line, BendLine bLine) =>
        IsHorizontal (bLine) ? Math.Abs (line.StartPoint.Y - bLine.StartPoint.Y)
                             : Math.Abs (line.StartPoint.X - bLine.StartPoint.X);

    // Get ordered lines 
    List<PLine> GetOrderedLines (List<PLine> pLines) {
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