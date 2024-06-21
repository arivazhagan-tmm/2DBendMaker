using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace BendMaker;

#region class BendUtils ---------------------------------------------------------------------------
public static class BendUtils {
   #region Methods --------------------------------------------------
   public static double GetBendDeduction (double angle, double kFactor, double thickness, double radius) {
      angle = angle.ToRadians ();
      var totalSetBack = 2 * ((radius + thickness) * Math.Tan (angle / 2));
      var bendAllowance = angle * (kFactor * thickness + radius);
      return double.Round (Math.Abs (totalSetBack - bendAllowance), 3);
   }

   public static double GetBendAllowance (double angle, double kFactor, double thickness, double radius)
      => angle.ToRadians () * (kFactor * thickness + radius);

   public static double ToRadians (this double theta) => theta * sFactor;

   public static double ToDegrees (this double theta) => theta / sFactor;

   public static Point Transform (this Point pt, Matrix xfm) => xfm.Transform (pt);

   public static BPoint Centroid (this List<BPoint> points) {
      var (xCords, yCords) = (points.Select (p => p.X), points.Select (p => p.Y));
      var (minX, minY, maxX, maxY) = (xCords.Min (), yCords.Min (), xCords.Max (), yCords.Max ());
      return new ((minX + maxX) * 0.5, (minY + maxY) * 0.5, -1);
   }

   public static BPoint ToBPoint (this Point pt) => new (pt.X, pt.Y);

   public static int[] GetCPIndices (this PLine refPline, List<PLine> plines) {
      var (start, end) = (refPline.StartPoint, refPline.EndPoint);
      return plines.Where (c => c.Index != refPline.Index && (c.HasVertex (start) || c.HasVertex (end))).Select (c => c.Index).ToArray ();
   }

   public static Bound Transform (this Bound b, Matrix xfm) {
      var (min, max) = (new Point (b.MinX, b.MinY), new Point (b.MaxX, b.MaxY));
      min = xfm.Transform (min);
      max = xfm.Transform (max);
      return new Bound (new BPoint (min.X, min.Y), new BPoint (max.X, max.Y));
   }

   public static bool HasVertex (this PLine p, BPoint pt) => p.StartPoint.Equals (pt) || p.EndPoint.Equals (pt);

   public static double Area (List<BPoint> pts) {
      int n = pts.Count; double area = 0;
      for (int i = 0; i < n - 1; i++)
         area += pts[i].X * pts[i + 1].Y - pts[i].Y * pts[i + 1].X;
      area += pts[n - 1].X * pts[0].Y - pts[n - 1].Y * pts[0].X;
      return Math.Abs (area) / 2;
   }

   public static string AddSpace (this string str) {
      var result = Regex.Split (str, @"(?=[A-Z])");
      return string.Join (" ", result);
   }

   public static double GetTraceLength (this IEnumerable<BPoint> pts) {
      var tmp = pts.ToArray ();
      var length = 0.0;
      for (int i = 0, len = tmp.Length - 1; i < len; i++) length += tmp[i].DistanceTo (tmp[i + 1]);
      return length;
   }
   #endregion

   #region Private Data ---------------------------------------------
   static double sFactor = Math.PI / 180;
   #endregion
}
#endregion