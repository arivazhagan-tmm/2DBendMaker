using System.IO;

namespace BendMaker;

public class GeoRead {
   #region Methods --------------------------------------------------
   public Profile ReadGeo () {
      OpenFileDialog openFile = new () { Filter = "Geo files (*.geo)|*.geo" };
      if (openFile.ShowDialog () == DialogResult.OK && openFile.FileName != "") {
         using StreamReader tr = new StreamReader (openFile.FileName);
         filename = openFile.FileName;
         string? line;
         int i = 1;
         while ((line = tr.ReadLine ())?.Trim () != null) {
            if (line?.Trim () == "#~31") {
               while ((line = tr.ReadLine ())?.Trim () == "P" && (line = tr.ReadLine ())?.Trim () == $"{i}") {
                  var coordinates = ((line = tr.ReadLine ())?.Trim ())?.Split (" ");
                  var (x, y) = (double.Parse (coordinates[0]), double.Parse (coordinates[1]));
                  mPoints.Add (new BPoint (x, y, i));
                  tr.ReadLine ();
                  i++;
               }
            } else if (line?.Trim () == "#~331") {
               while ((line = tr.ReadLine ())?.Trim () == "LIN") {
                  tr.ReadLine ();
                  var coordinates = ((line = tr.ReadLine ())?.Trim ())?.Split (" ");
                  var (p1, p2) = (mPoints[int.Parse (coordinates[0]) - 1], mPoints[int.Parse (coordinates[1]) - 1]);
                  mCurves.Add (new Curve (ECurve.Line, null, p1, p2));
                  tr.ReadLine ();
               }
               i = 0;
            } else if (line?.Trim () == "#~37") {
               for (int j = 0; j < 3; j++) tr.ReadLine ();
               mBendDeduction = -1 * double.Parse (((line = tr.ReadLine ())?.Trim ()) ?? "");
               i++;
            } else if (line?.Trim () == "#~371") {
               for (int j = 0; j < 2; j++) tr.ReadLine ();
               var coordinates = (line = tr.ReadLine ())?.Trim ().Split (" ");
               var (p1, p2) = (mPoints[int.Parse (coordinates[0]) - 1], mPoints[int.Parse (coordinates[1]) - 1]);
               mBendLine = new BendLine (p1, p2, mBendDeduction);
               mBendLines.Add (mBendLine);
            }
         }
      }
      return mProfile = new Profile (mCurves, mBendLines);
   }
   #endregion

   #region Properties -----------------------------------------------
   public List<BendLine> GetBendInfo => mBendLines;
   public List<BPoint> Points => mPoints;
   public string FileName => filename ??= "";
   #endregion

   #region Private Data ---------------------------------------------
   double mBendAngle, mBendDeduction, mBendRadius;
   Profile mProfile;
   List<BPoint> mPoints = new ();
   List<Curve> mCurves = new ();
   List<BendLine> mBendLines = new ();
   BendLine mBendLine;
   string? filename;
   #endregion
}

