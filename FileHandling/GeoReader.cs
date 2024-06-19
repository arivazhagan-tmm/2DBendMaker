using System.IO;

namespace BendMaker;

#region class GeoReader ----------------------------------------------------------------
public class GeoReader (string fileName) {
    #region Methods --------------------------------------------------
    public Part ParseProfile () {
        using StreamReader reader = new (FileName);
        var vertices = new List<BPoint> ();
        var curves = new List<PLine> ();
        var bLines = new List<BendLine> ();
        var bDeduction = 0.0;
        string? line;
        string materialType = string.Empty;
        int i = 1;
        double thickness = 0;
        while (ReadLine (out line)) {
            switch (line) {
                case "#~31":
                    while (ReadLine (out line) && line is "P" && ReadLine (out line) && line == $"{i}") {
                        ReadLine (out line);
                        var coordinates = line.Split (" ");
                        var (x, y) = (double.Parse (coordinates[0]), double.Parse (coordinates[1]));
                        vertices.Add (new BPoint (x, y, i));
                        SkipLine ();
                        i++;
                    }
                    break;
                case "#~331":
                    int curveIndex = 0;
                    while (ReadLine (out line) && line is "LIN") {
                        SkipLine ();
                        var (v1, v2) = ParsePoints ();
                        curves.Add (new PLine (ECurve.Line, curveIndex++, v1, v2));
                        SkipLine ();
                    }
                    i = 0;
                    break;

                case "#~37":
                    for (int j = 0; j < 3; j++) SkipLine ();
                    ReadLine (out line);
                    bDeduction = -1 * double.Parse (line);
                    i++;
                    break;

                case "#~371":
                    for (int j = 0; j < 2; j++) SkipLine ();
                    var (p1, p2) = ParsePoints ();
                    bLines.Add (new BendLine (p1, p2, bDeduction));
                    break;
                case "#~11":
                    for (int j = 0; j < 5; j++) SkipLine ();
                    ReadLine (out line);
                    materialType = line;
                    ReadLine (out line);
                    thickness = double.Parse (line);
                    break;
            }
        }
        return new Part (curves, bLines, materialType, thickness);

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