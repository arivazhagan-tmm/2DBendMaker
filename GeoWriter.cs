using System.IO;

namespace BendMaker;
public class GeoWriter {
   #region Constructors ---------------------------------------------
   public GeoWriter (BendProfile bProfile, string filename) => (mBProfile, mFilename) = (bProfile, filename);
   #endregion

   #region Methods --------------------------------------------------
   public void OverWrite (string fileName) {
      var lines = File.ReadAllLines (mFilename);
      using (StreamWriter writer = new (fileName)) {
         foreach (string line in lines) {
            if (line.Trim () == "#~31") {
               writer.WriteLine ("#~31");
               foreach (var vertex in mBProfile.Vertices.OrderBy (x => x.Index)) {
                  writer.WriteLine ($"P \n" +
                                     $"{vertex.Index}\n" +
                                     $"{Math.Round (vertex.X, 9)} {Math.Round (vertex.Y, 9)} 0.000000000\n" +
                                     $"|~");
               }
               writer.WriteLine ("##~~");
               mIsNeeded = false;
            }
            if (line.Trim () == "#~33") mIsNeeded = true;
            if (mIsNeeded) writer.WriteLine (line);
         }
      }
   }
   #endregion

   #region Private --------------------------------------------------
   bool mIsNeeded = true;
   readonly BendProfile mBProfile;
   readonly string mFilename;
   #endregion
}