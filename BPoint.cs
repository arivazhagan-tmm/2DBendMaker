namespace BendMaker;

#region struct BPoint -----------------------------------------------------------------------------
public readonly record struct BPoint (double X, double Y, int Index = -1) {
    #region Methods --------------------------------------------------
    public double AngleTo (BPoint p) {
        var angle = Math.Round (Math.Atan2 (p.Y - Y, p.X - X) * (180 / Math.PI), 2);
        return angle < 0 ? 360 + angle : angle;
    }
    public override string ToString () => $"({X}, {Y})";
    public bool IsEqual (BPoint p) => p.X == X && p.Y == Y;
    public bool IsInside (Bound b) => X < b.MaxX && X > b.MinX && Y < b.MaxY && Y < b.MinY;
    public BPoint Translated (BVector v) => new (X + v.DX, Y + v.DY, Index);
    #endregion
}
#endregion

#region struct BVector ----------------------------------------------------------------------------
public readonly record struct BVector (double DX, double DY);
#endregion

#region struct Bound ------------------------------------------------------------------------------
public struct Bound {
    #region Constructors ---------------------------------------------
    public Bound (BPoint cornerA, BPoint cornerB) {
        MinX = Math.Min (cornerA.X, cornerB.X);
        MaxX = Math.Max (cornerA.X, cornerB.X);
        MinY = Math.Min (cornerA.Y, cornerB.Y);
        MaxY = Math.Max (cornerA.Y, cornerB.Y);
        (mHeight, mWidth) = (MaxY - MinY, MaxX - MinX);
        mMid = new ((MaxX + MinX) * 0.5, (MaxY + MinY) * 0.5);
    }

    public Bound (params BPoint[] pts) {
        MinX = pts.Min (p => p.X);
        MaxX = pts.Max (p => p.X);
        MinY = pts.Min (p => p.Y);
        MaxY = pts.Max (p => p.Y);
        (mHeight, mWidth) = (MaxY - MinY, MaxX - MinX);
        mMid = new ((MaxX + MinX) * 0.5, (MaxY + MinY) * 0.5);
    }
    #endregion

    #region Properties -----------------------------------------------
    public bool IsEmpty => MinX > MaxX || MinY > MaxY;
    public double MinX { get; init; }
    public double MaxX { get; init; }
    public double MinY { get; init; }
    public double MaxY { get; init; }
    public BPoint Mid => mMid;
    public double Width => mWidth;
    public double Height => mHeight;
    #endregion

    public Bound Inflated (BPoint ptAt, double factor) {
        if (IsEmpty) return this;
        var minX = ptAt.X - (ptAt.X - MinX) * factor;
        var maxX = ptAt.X + (MaxX - ptAt.X) * factor;
        var minY = ptAt.Y - (ptAt.Y - MinY) * factor;
        var maxY = ptAt.Y + (MaxY - ptAt.Y) * factor;
        return new (new (minX, minY), new (maxX, maxY));
    }

    #region Private Data ---------------------------------------------
    readonly BPoint mMid;
    readonly double mHeight, mWidth;
    #endregion
}
#endregion