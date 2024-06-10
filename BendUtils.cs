
using System;
using System.Reflection.Metadata;

namespace BendMaker;
static public class BendUtils {
   static double mFactor = Math.PI / 180;

   static public double GetBendDeduction (double angle, double kFactor, double thickness, double radius) {
      angle = angle.ToRadians ();
      var totalSetBack = 2 * ((radius + thickness) * Math.Tan (angle / 2));
      var bendAllowance = angle * (kFactor * thickness + radius);
      return double.Round (Math.Abs (totalSetBack - bendAllowance), 3);
   }

   public static double ToRadians (this double theta) => theta * mFactor;
   public static double ToDegrees (this double theta) => theta / mFactor;

   public static BPoint Centroid (this List<BPoint> points) {
      var (xCords, yCords) = (points.Select (p => p.X), points.Select (p => p.Y));
      var (minX, minY, maxX, maxY) = (xCords.Min (), yCords.Min (), xCords.Max (), yCords.Max ());
      return new ((minX + maxX) * 0.5, (minY + maxY) * 0.5);
   }
}

