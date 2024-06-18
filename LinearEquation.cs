namespace BendMaker {
   public class LinearEqn {

      #region Implementation ----------------------------------------
      public BPoint GetIntersectPoint () {
         int x = 0, y = 0;
         if (mLineFirstEqn["X"] != 0 && mLineFirstEqn["Y"] != 0) {
            x = (int)((mLineFirstEqn["C"] - mLineFirstEqn["Y"]) / mLineFirstEqn["X"]);
         } else if (mLineFirstEqn["X"] != 0 && mLineFirstEqn["Y"] == 0) {
            x = (int)((mLineFirstEqn["C"] - mLineFirstEqn["Y"]) / mLineFirstEqn["X"]);
         }

         if (x > 0 || x < 0) {
            y = (int)((mLineSecondEqn["C"] - mLineSecondEqn["X"]) / (mLineSecondEqn["Y"]));
         }
         return new BPoint (x, y);
      }

      public void MakeLinearEqn (BPoint startPoint, BPoint endPoint) {
         (mX1, mY1) = (startPoint.X, startPoint.Y);
         (mX2, mY2) = (endPoint.X, endPoint.Y);
         if (mLineFirstEqn.Count == 0 && Counter == 0) {
            mLineFirstEqn.Add ("X", Y2 - Y1);
            mLineFirstEqn.Add ("Y", -(X2 - X1));
            mLineFirstEqn.Add ("C", (X1 * (Y2 - Y1)) - (Y1 * (X2 - X1)));
            mCounter = 1;
         } else if (mLineSecondEqn.Count == 0 && Counter == 1) {
            mLineSecondEqn.Add ("X", Y2 - Y1);
            mLineSecondEqn.Add ("Y", -(X2 - X1));
            mLineSecondEqn.Add ("C", (X1 * (Y2 - Y1)) - (Y1 * (X2 - X1)));
            mCounter = 0;
         }
      }
      #endregion

      #region Properties --------------------------------------------
      double X1 => mX1;

      double Y1 => mY1;

      double X2 => mX2;

      double Y2 => mY2;

      int Counter => mCounter;
      #endregion

      #region Private Data ------------------------------------------
      int mCounter = 0;
      double mX1, mX2, mY1, mY2;
      Dictionary<string, double> mLineFirstEqn = new Dictionary<string, double> (),
         mLineSecondEqn = new Dictionary<string, double> ();
      #endregion
   }
}
