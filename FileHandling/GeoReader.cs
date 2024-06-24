using System.IO;

namespace BendMaker;

#region class GeoReader ----------------------------------------------------------------
public class GeoReader (string fileName) {
   #region Methods --------------------------------------------------
   public Part ParseToPart () {
      using StreamReader reader = new (FileName);
      var vertices = new List<BPoint> ();
      var plines = new List<PLine> ();
      var bLines = new List<BendLine> ();
      var (bd, th) = (0.0, 0.0); // Bend deduction and thickness
      var materialType = string.Empty;
      int i = 1;
      while (ReadLine (out string? str)) {
         switch (str) {
            case "#~31":
               while (ReadLine (out str) && str is "P" && ReadLine (out str) && str == $"{i}") {
                  ReadLine (out str);
                  var cords = str.Split (" "); // Co-ordinates
                  var (x, y) = (double.Parse (cords[0]), double.Parse (cords[1]));
                  vertices.Add (new BPoint (x, y, i));
                  SkipLine ();
                  i++;
               }
               break;
            case "#~331":
               int lineIndex = 0;
               while (ReadLine (out str) && str is "LIN") {
                  SkipLine ();
                  var (v1, v2) = ParsePoints ();
                  plines.Add (new PLine (ECurve.Line, lineIndex++, v1, v2));
                  SkipLine ();
               }
               i = 0;
               break;
            case "#~37":
               for (int j = 0; j < 3; j++) SkipLine ();
               ReadLine (out str);
               bd = -1 * double.Parse (str);
               i++;
               break;
            case "#~371":
               for (int j = 0; j < 2; j++) SkipLine ();
               var (p1, p2) = ParsePoints ();
               bLines.Add (new BendLine (p1, p2, bd));
               break;
            case "#~11":
               for (int j = 0; j < 5; j++) SkipLine ();
               ReadLine (out str);
               materialType = str;
               ReadLine (out str);
               th = double.Parse (str);
               break;
         }
      }
      return new Part (plines, bLines, materialType, th);

      bool ReadLine (out string str) {
         str = reader.ReadLine ()!;
         if (str is null) return false;
         else {
            str = str.Trim ();
            return true;
         }
      }

      (BPoint, BPoint) ParsePoints () {
         ReadLine (out string str);
         var coords = str.Split (" ");
         return (vertices[int.Parse (coords[0]) - 1], vertices[int.Parse (coords[1]) - 1]);
      }

      void SkipLine () => reader.ReadLine ();
   }
   #endregion

   #region Properties -----------------------------------------------
   public readonly string FileName = fileName;
   #endregion
}
#endregion