﻿using System.Text.RegularExpressions;
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

   public static int[] GetCPIndices (this PLine refCurve, List<PLine> curves) {
      var (start, end) = (refCurve.StartPoint, refCurve.EndPoint);
      return curves.Where (c => c.Index != refCurve.Index && (c.HasVertex (start) || c.HasVertex (end))).Select (c => c.Index).ToArray ();
   }

   public static Bound Transform (this Bound b, Matrix xfm) {
      var (min, max) = (new Point (b.MinX, b.MinY), new Point (b.MaxX, b.MaxY));
      min = xfm.Transform (min);
      max = xfm.Transform (max);
      return new Bound (new BPoint (min.X, min.Y), new BPoint (max.X, max.Y));
   }

   public static bool HasVertex (this PLine c, BPoint p) => c.StartPoint.Equals (p) || c.EndPoint.Equals (p);

   public static double Area (List<BPoint> vertices) {
      int n = vertices.Count; double area = 0;
      for (int i = 0; i < n - 1; i++)
         area += vertices[i].X * vertices[i + 1].Y - vertices[i].Y * vertices[i + 1].X;
      area += vertices[n - 1].X * vertices[0].Y - vertices[n - 1].Y * vertices[0].X;
      return Math.Abs (area) / 2;
   }

   public static string AddSpace (this string str) {
      var result = Regex.Split (str, @"(?=[A-Z])");
      return string.Join (" ", result);
   }
   #endregion

   #region Private Data ---------------------------------------------
   static double sFactor = Math.PI / 180;
   #endregion
}
#endregion