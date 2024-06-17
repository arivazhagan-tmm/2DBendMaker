using System.IO;

namespace BendMaker;

#region class GeoWriter ----------------------------------------------------------------
public class GeoWriter {
    #region Constructors ---------------------------------------------
    public GeoWriter (BendProfile bProfile, string filename) => (mBProfile, mFilename) = (bProfile, filename);
    #endregion

    #region Methods --------------------------------------------------
    public void Save (string fileName) {
        var lines = File.ReadAllLines (mFilename).ToList ();
        using StreamWriter writer = new (fileName);
        foreach (string line in lines) {
            switch (line.Trim ()) {
                case "#~1":
                    writer.WriteLine ("#~1{0}1.03{0}1{0}{1}{0}{2}{0}{3}{0}{4}{0}1{0}0.001{0}0{0}1{0}##~~", "\r\n",
                        $"{DateTime.Now:dd.MM.yyyy}", BoundMin, BoundMax, Area);
                    mIsNeeded = false;
                    break;
                case "#~3":
                    writer.WriteLine ("#~3{0}{0}{0}", "\r\n");
                    writer.WriteLine ($"{0:F9} {0:F9} {1:F9}\r\n{1:F9} {0:F9} {0:F9} {0:F9}\r\n" +
                        $"{0:F9} {1:F9} {0:F9} {0:F9}\r\n{0:F9} {0:F9} {1:F9} {0:F9}\r\n{0:F9} {0:F9} {0:F9} {1:F9}");
                    writer.WriteLine ("{1}{0}{2}{0}{3}{0}{4}{0}1{0}0{0}0{0}0{0}0{0}##~~", "\r\n", BoundMin, BoundMax, Centriod, Area);
                    mIsNeeded = false;
                    break;
                case "#~31":
                    writer.WriteLine ("#~31");
                    var vertices = mBProfile.Vertices.OrderBy (x => x.Index).ToList ();
                    foreach (var vertice in vertices)
                        writer.WriteLine ("P{0}{1}{0}{2} {3}{0}{4}", "\r\n", vertice.Index, $"{vertice.X:F9}", $"{vertice.Y:F9} 0.000000000", "|~");
                    writer.WriteLine ("##~~");
                    mIsNeeded = false;
                    break;
                case "#~33":
                    writer.WriteLine ("#~33");
                    var dataLines = new string[4];
                    Array.Copy (lines.ToArray (), lines.IndexOf (line) + 1, dataLines, 0, 4);
                    foreach (var dataline in dataLines) writer.WriteLine (dataline);
                    writer.WriteLine ("{1}{0}{2}{0}{3}{0}{4}{0}0{0}##~~", "\r\n", BoundMin, BoundMax, Centriod, Area);
                    mIsNeeded = false;
                    break;
                case "#~11" or "#~30" or "#~331":
                    writer.WriteLine (line);
                    mIsNeeded = true;
                    break;
                default: if (mIsNeeded) writer.WriteLine (line); break;
            }
        }
    }
    #endregion

    #region Properties -----------------------------------------------
    public string BoundMin => $"{mBProfile.Bound.MinX:F9} {mBProfile.Bound.MinY:F9} 0.000000000";
    public string BoundMax => $"{mBProfile.Bound.MaxX:F9} {mBProfile.Bound.MaxY:F9} 0.000000000";
    public string Centriod => $"{mBProfile.Centroid.X:F9} {mBProfile.Centroid.Y:F9} 0.000000000";
    public string Area => $"{mBProfile.Area (mBProfile.Curves.OrderBy (x => x.Index).Select (x => x.StartPoint).ToList ()):F9}";
    #endregion

    #region Private --------------------------------------------------
    bool mIsNeeded = true;
    readonly BendProfile mBProfile;
    readonly string mFilename;
    #endregion
}
#endregion