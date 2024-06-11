using System.IO;

namespace BendMaker;
public class GeoWriter {
   public GeoWriter (BendProfile bprofile, string filename) => (mBprofile, mFilename) = (bprofile, filename);

   public void Save () {
      SaveFileDialog dlgBox = new () { FileName = $"Modified-{Path.GetFileNameWithoutExtension (mFilename)}", Filter = "GEO|*.geo" };
      DialogResult dr = dlgBox.ShowDialog ();
      if (dr == DialogResult.OK) {
         var fname = dlgBox.FileName;
         string[] lines = File.ReadAllLines (mFilename);
         using (StreamWriter writer = new (fname)) {
            foreach (string line in lines) {
               List<BPoint> vertices = new ();
               if (line.Trim () == "#~31") {
                  writer.WriteLine ("#~31");
                  vertices = mBprofile.Vertices.OrderBy (x => x.Index).ToList ();
                  foreach (var vertice in vertices) {
                     writer.WriteLine ("P");
                     writer.WriteLine ($"{vertice.Index}");
                     writer.WriteLine ($"{Math.Round (vertice.X, 9)} {Math.Round (vertice.Y, 9)} 0.000000000");
                     writer.WriteLine ("|~");
                  }
                  writer.WriteLine ("##~~");
                  mIsNeeded = false;
               }
               if (line.Trim () == "#~33") mIsNeeded = true;
               if (mIsNeeded) writer.WriteLine (line);
            }
         }
      }
   }
   bool mIsNeeded = true;
   BendProfile mBprofile;
   string mFilename;
}
