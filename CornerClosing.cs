namespace BendMaker;
internal class CornerClosing {
   public CornerClosing () { }

   public BendProfile MakeCornerClosing (Profile profile) {
      var vertices = profile.Vertices; var curves = profile.Curves;
      var halfBA = BendUtils.GetBendAllowance (90, 0.38, 2, 2) / 2;
      var curveShift = halfBA - 2;
      var halfBD = BendUtils.GetBendDeduction (90, 0.38, 2, 2) / 2;
      var repeatedPoints = vertices.GroupBy (p => p).Where (p => p.Count () > 1).Select (g => g.Key).ToList ();
      foreach (var curve in curves)
         if (repeatedPoints.Contains (curve.StartPoint) || repeatedPoints.Contains (curve.EndPoint)) mStepCurves.Add (curve);
         else mEdgeCurves.Add (curve);
      for (int c = 0; c < curves.Count; c++) {
         var curve = curves[c];
         if (mEdgeCurves.Contains (curve)) {
            Curve trimmedEdge;
            if (curve.Orientation is ECurveOrientation.Horizontal) {
               if (curve.StartPoint.Y < profile.Centroid.Y) trimmedEdge = curve.Trimmed (-halfBD, 0, -curveShift, 0);
               else trimmedEdge = curve.Trimmed (halfBD, 0, curveShift, 0);
               trimmedEdge.Index = ++index;
               mNewCurves.Add (trimmedEdge);
            } else if (curve.Orientation is ECurveOrientation.Vertical) {
               if (curve.StartPoint.X < profile.Centroid.X) trimmedEdge = curve.Trimmed (0, halfBD, 0, curveShift);
               else trimmedEdge = curve.Trimmed (0, -halfBD, 0, -curveShift);
               trimmedEdge.Index = ++index;
               mNewCurves.Add (trimmedEdge);
            }
         } else {
            if (mStepCurves.IndexOf (curve) % 2 == 0) {
               Curve translatedCurve;
               if (curve.Orientation is ECurveOrientation.Horizontal) {
                  if (curve.EndPoint.Y < profile.Centroid.Y) translatedCurve = curve.Translated (0, curveShift);
                  else translatedCurve = curve.Translated (0, -curveShift);
                  translatedCurve.Index = ++index;
                  mNewCurves.Add (translatedCurve);
               } else if (curve.Orientation is ECurveOrientation.Vertical) {
                  if (curve.EndPoint.X < profile.Centroid.X) translatedCurve = curve.Translated (curveShift, 0);
                  else translatedCurve = curve.Translated (-curveShift, 0);
                  translatedCurve.Index = ++index;
                  mNewCurves.Add (translatedCurve);
               }
            } else {
               Curve trimmedCurve, extrudedCurve;
               if (curve.Orientation is ECurveOrientation.Horizontal) {
                  if (curve.StartPoint.X < profile.Centroid.X) {
                     extrudedCurve = curve.Trimmed (-halfBA, 0, 0, 0).Translated (0, halfBD);
                     trimmedCurve = curve.Trimmed (curveShift, 0, curve.Length - halfBA, 0);
                  } else {
                     extrudedCurve = curve.Trimmed (halfBA, 0, 0, 0).Translated (0, -halfBD);
                     trimmedCurve = curve.Trimmed (-curveShift, 0, -(curve.Length - halfBA), 0);
                  }
                  trimmedCurve.Index = ++index;
                  mNewCurves.Add (trimmedCurve);
                  mNewCurves.Add (new Curve (ECurve.Line, ++index, "", trimmedCurve.EndPoint, extrudedCurve.StartPoint));
                  extrudedCurve.Index = ++index;
                  mNewCurves.Add (extrudedCurve);

               } else if (curve.Orientation is ECurveOrientation.Vertical) {
                  if (curve.StartPoint.Y < profile.Centroid.Y) {
                     extrudedCurve = curve.Trimmed (0, -halfBA, 0, 0).Translated (-halfBD, 0);
                     trimmedCurve = curve.Trimmed (0, curveShift, 0, curve.Length - halfBA);
                  } else {
                     extrudedCurve = curve.Trimmed (0, halfBA, 0, 0).Translated (halfBD, 0);
                     trimmedCurve = curve.Trimmed (0, -curveShift, 0, -(curve.Length - halfBA));
                  }
                  trimmedCurve.Index = ++index;
                  mNewCurves.Add (trimmedCurve);
                  mNewCurves.Add (new Curve (ECurve.Line, ++index, "", trimmedCurve.EndPoint, extrudedCurve.StartPoint));
                  extrudedCurve.Index = ++index;
                  mNewCurves.Add (extrudedCurve);
               }
            }
         }
      }
      return new BendProfile (EBDAlgorithm.Unknown, mNewCurves, profile.BendLines);
   }

   List<Curve> mStepCurves = [], mEdgeCurves = [], mNewCurves = [];
   List<BPoint> mVertices = [];
   BendProfile mCCprofile;
   int index = -1;
}