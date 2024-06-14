namespace BendMaker;

#region struct BendProfile ------------------------------------------------------------------------
public struct BendProfile {
   #region Constructors ---------------------------------------------
   public BendProfile (EBDAlgorithm algorithm, List<Curve> curves, List<BendLine> bendLines) {
      mBDAlgorithm = algorithm;
      (mBendLines, mCurves) = (bendLines, curves);
      mVertices = [];
      mVertices.AddRange (mCurves.Select (c => c.StartPoint));
      mVertices.AddRange (mBendLines.Select (c => c.StartPoint));
      mVertices.AddRange (mBendLines.Select (c => c.EndPoint));
   }
   #endregion

   #region Properties -----------------------------------------------
   public readonly EBDAlgorithm BendDeductionAlgorithm => mBDAlgorithm;
   public readonly List<BPoint> Vertices => mVertices;
   public readonly List<Curve> Curves => mCurves;
   public readonly List<BendLine> BendLines => mBendLines;
   #endregion

   #region Private Data ---------------------------------------------
   List<BendLine> mBendLines;
   List<Curve> mCurves;
   List<BPoint> mVertices;
   EBDAlgorithm mBDAlgorithm; // Bend deduction algorithm
   #endregion
}
#endregion