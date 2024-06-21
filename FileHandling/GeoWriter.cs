using System.IO;

namespace BendMaker;

#region class GeoWriter ----------------------------------------------------------------------------
public class GeoWriter {
    #region Constructors --------------------------------------------
    public GeoWriter (BendProcessedPart bProfile, string filename) => (mBProfile, mFilename) = (bProfile, filename);
    #endregion

    #region Methods -------------------------------------------------
    /// <summary>Writes a new geo file</summary>
    public void Save (string fileName) {
        var lines = File.ReadAllLines (mFilename).ToList (); // Reads the imported file
        using StreamWriter writer = new (fileName);
        foreach (string line in lines) {
            switch (line.Trim ()) {
                case "#~1":
                    writer.WriteLine ("#~1{0}1.03{0}1{0}{1}{0}{2}{0}{3}{0}{4}{0}1{0}0.001{0}0{0}1{0}##~~", "\r\n",
                        $"{DateTime.Now:dd.MM.yyyy}", BoundMin, BoundMax, Area);
                    mIsLineNeeded = false;
                    break;
                case "#~3":
                    writer.WriteLine ("#~3{0}{0}{0}", "\r\n" + $"{0:F9} {0:F9} {1:F9}\r\n{1:F9} {0:F9} {0:F9} {0:F9}\r\n" +
                        $"{0:F9} {1:F9} {0:F9} {0:F9}\r\n{0:F9} {0:F9} {1:F9} {0:F9}\r\n{0:F9} {0:F9} {0:F9} {1:F9}" +
                        "{1}{0}{2}{0}{3}{0}{4}{0}1{0}0{0}0{0}0{0}0{0}##~~", "\r\n", BoundMin, BoundMax, Centriod, Area);
                    mIsLineNeeded = false;
                    break;
                case "#~31": // Writes the new vertices
                    writer.WriteLine ("#~31");
                    var vertices = mBProfile.Vertices.OrderBy (x => x.Index).ToList ();
                    foreach (var vertice in vertices)
                        writer.WriteLine ("P{0}{1}{0}{2} {3}{0}{4}", "\r\n", vertice.Index, $"{vertice.X:F9}", $"{vertice.Y:F9} 0.000000000", "|~");
                    writer.WriteLine ("##~~");
                    mIsLineNeeded = false;
                    break;
                case "#~33":
                    writer.WriteLine ("#~33");
                    var dataLines = new string[4];
                    Array.Copy (lines.ToArray (), lines.IndexOf (line) + 1, dataLines, 0, 4);
                    foreach (var dataline in dataLines) writer.WriteLine (dataline);
                    writer.WriteLine ("{1}{0}{2}{0}{3}{0}{4}{0}0{0}##~~", "\r\n", BoundMin, BoundMax, Centriod, Area);
                    mIsLineNeeded = false;
                    break;
                case "#~11" or "#~30" or "#~37" or "#~331" or "#~371" or "#~END" or "#~EOF":
                    writer.WriteLine (line);
                    if (line.Trim () == "#~331" && mBProfile.IsContourChanged) {    // Writes the vertices of contour with new indices for mapping them
                        foreach (var curve in mBProfile.Curves) {
                            writer.WriteLine ("LIN{0}1 0{0}{1} {2}{0}|~", "\r\n", curve.StartPoint.Index, curve.EndPoint.Index);
                        }
                        writer.WriteLine ("##~~\n#~KONT_END");
                        mIsLineNeeded = false;
                    } else if (line.Trim () == "#~371" && mBProfile.IsContourChanged) {    // Writes the vertices of bendlines with new indices for mapping them
                        int totalCount = mBProfile.BendLines.Count;
                        if (blCount < totalCount) {
                            var startPtIdx = mBProfile.BendLines[blCount].StartPoint.Index;               // #~371 section is written after each bendline's
                            var endPtIdx = mBProfile.BendLines[blCount].EndPoint.Index;                   // #~37 section which holds info about the each bendline
                            writer.WriteLine ("LIN{0}4 0{0}{1} {2}{0}|~", "\r\n", startPtIdx, endPtIdx);
                        }
                        blCount++;
                        mIsLineNeeded = false;
                        writer.WriteLine ("##~~\n#~BIEG_END");
                    } else mIsLineNeeded = true;
                    break;
                default: if (mIsLineNeeded) writer.WriteLine (line); break; // Copies the lines from imported file which are not affected
            }
        }
    }
    #endregion

    #region Properties ----------------------------------------------
    public string BoundMin => $"{mBProfile.Bound.MinX:F9} {mBProfile.Bound.MinY:F9} 0.000000000";
    public string BoundMax => $"{mBProfile.Bound.MaxX:F9} {mBProfile.Bound.MaxY:F9} 0.000000000";
    public string Centriod => $"{mBProfile.Centroid.X:F9} {mBProfile.Centroid.Y:F9} 0.000000000";
    public string Area => $"{BendUtils.Area (mBProfile.Curves.OrderBy (x => x.Index).Select (x => x.StartPoint).ToList ()):F9}";
    #endregion

    #region Private -------------------------------------------------
    int blCount;    // Holds the count of the bendlines written
    bool mIsLineNeeded = true;    // Set to false if the line in the imported file to be skipped
    readonly BendProcessedPart mBProfile;
    readonly string mFilename;    // Name of the imported file
    #endregion

    #region Commented -----------------------------------------------
    //public (List<PLine>, List<BendLine>) GetChangedContour (List<PLine> curves, List<BendLine> bendLines) {
    //    List<PLine> plines = []; List<BendLine> bl = []; int i;
    //    for (i = 1; i <= curves.Count; i++)
    //        plines.Add (new PLine (new BPoint (curves[i - 1].StartPoint.X, curves[i - 1].StartPoint.Y, i),
    //            new BPoint (curves[i - 1].EndPoint.X, curves[i - 1].EndPoint.Y, i + 1)));
    //    for (int j = 1; j <= bendLines.Count; j++)
    //        bl.Add (new BendLine (new BPoint (bendLines[j - 1].StartPoint.X, bendLines[j - 1].StartPoint.Y, i + j + 2),
    //            new BPoint (bendLines[j - 1].EndPoint.X, bendLines[j - 1].EndPoint.Y, i + j + 3)));
    //    return (plines, bl);
    //}
    #endregion
}
#endregion