using BendMaker;
using System.Windows;
using System.Windows.Media;


namespace BendMaker;

static public class BendUtils {
   static double sFactor = Math.PI / 180;
   static public double GetBendDeduction (double angle, double kFactor, double thickness, double radius) {
      angle = angle.ToRadians ();
      var totalSetBack = 2 * ((radius + thickness) * Math.Tan (angle / 2));
      var bendAllowance = angle * (kFactor * thickness + radius);
      return double.Round (Math.Abs (totalSetBack - bendAllowance), 3);
   }
   public static double ToRadians (this double theta) => theta * sFactor;
   public static double ToDegrees (this double theta) => theta / sFactor;
   /// <summary>Returns a new point with applied transformation</summary>
   public static Point Transform (this Point pt, Matrix xfm) => xfm.Transform (pt);
   public static BPoint Centroid (this List<BPoint> points) {
      var (xCords, yCords) = (points.Select (p => p.X), points.Select (p => p.Y));
      var (minX, minY, maxX, maxY) = (xCords.Min (), yCords.Min (), xCords.Max (), yCords.Max ());
      return new ((minX + maxX) * 0.5, (minY + maxY) * 0.5, -1);
   }

   public static BPoint ToBPoint (this Point pt) => new (pt.X, pt.Y);

   public static int[] GetCCIndices (this Curve refCurve, List<Curve> curves) {
      var (start, end) = (refCurve.StartPoint, refCurve.EndPoint);
      return curves.Where (c => c.Index != refCurve.Index && (c.HasVertex (start) || c.HasVertex (end))).Select (c => c.Index).ToArray ();
   }

   /// <summary>Returns a new bound with applied transformation</summary>
   public static Bound Transform (this Bound b, Matrix xfm) {
      var (min, max) = (new Point (b.MinX, b.MinY), new Point (b.MaxX, b.MaxY));
      min = xfm.Transform (min);
      max = xfm.Transform (max);
      return new Bound (new BPoint (min.X, min.Y), new BPoint (max.X, max.Y));
   }

   public static bool HasVertex (this Curve c, BPoint p) => c.StartPoint.Equals (p) || c.EndPoint.Equals (p);

    public static double Area (List<BPoint> vertices) {
        int n = vertices.Count; double area = 0;
        for (int i = 0; i < n - 1; i++)
            area += vertices[i].X * vertices[i + 1].Y - vertices[i].Y * vertices[i + 1].X;
        area += vertices[n - 1].X * vertices[0].Y - vertices[n - 1].Y * vertices[0].X;
        return Math.Abs (area) / 2;
    }
}