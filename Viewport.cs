using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Data;

namespace BendMaker;

internal class Viewport : Canvas {
   #region Constructors ---------------------------------------------
   public Viewport () => Loaded += OnLoaded;
   #endregion

   #region Properties -----------------------------------------------
   public Profile Profile { get => mProfile; set { mProfile = value; } }
   public BendProfile BendProfile { get => mBendProfile; set { mBendProfile = value; UpdateBound (); } }
   public bool HasBProfile => mBendProfile.Curves != null;
   #endregion

   #region Methods --------------------------------------------------
   public void ZoomExtents () {
      UpdateBound ();
      InvalidateVisual ();
   }
   #endregion

   #region Implementation -------------------------------------------
   void OnLoaded (object sender, RoutedEventArgs e) {
      Background = Brushes.Transparent;
      mStartPt = mCurrentMousePt = new BPoint ();
      mDwgLineWeight = 1.0;
      mDwgBrush = Brushes.ForestGreen;
      mBGPen = new (Brushes.Gray, 0.5);
      mDwgPen = new (Brushes.Black, mDwgLineWeight);
      mBLPen = new (Brushes.ForestGreen, 2.0) { DashStyle = DashStyles.DashDot };

      if (MainWindow.It != null)
         mViewportRect = new Rect (new Size (MainWindow.It.ActualWidth - 120, MainWindow.It.ActualHeight - 100));
      (mViewportWidth, mViewportHeight) = (mViewportRect.Width, mViewportRect.Height);
      mViewportBound = new Bound (new (0.0, 0.0), new (mViewportWidth, mViewportHeight));
      mViewportCenter = new Point (mViewportBound.Mid.X, mViewportBound.Mid.Y);
      UpdateProjXform (mViewportBound);

      #region Attaching events --------------------------------------
      MouseUp += OnMouseUp; ;
      MouseMove += OnMouseMove; ;
      MouseLeftButtonDown += OnMouseLeftButtonDown;
      MouseWheel += OnMouseWheel;
      #endregion

      mCords = new TextBlock () { Background = Brushes.Transparent };
      Children.Add (mCords);
   }

   void OnMouseWheel (object sender, MouseWheelEventArgs e) {
      double zoomFactor = 1.05;
      if (e.Delta > 0) zoomFactor = 1 / zoomFactor;
      UpdateProjXform (mViewportBound.Transform (mInvProjXfm).Inflated (mCurrentMousePt, zoomFactor));
      InvalidateVisual ();
   }

   void OnMouseLeftButtonDown (object sender, MouseButtonEventArgs e) { }

   void OnMouseMove (object sender, MouseEventArgs e) {
      mCurrentMousePt = e.GetPosition (this).Transform (mInvProjXfm).ToBPoint ();
      if (mCords != null) mCords.Text = $"X : {double.Round (mCurrentMousePt.X, 2)}  Y : {double.Round (mCurrentMousePt.Y, 2)}";
      InvalidateVisual ();
   }

   void OnMouseUp (object sender, MouseButtonEventArgs e) { }

   protected override void OnRender (DrawingContext dc) {
      dc.DrawRectangle (Background, mBGPen, mViewportRect);
      if (mProfile.Curves is null) return;
      var v = new BVector (mProfile.Bound.MaxX + 5, 0);
      foreach (var c in mProfile.Curves) {
         var (start, end) = (mProjXfm.Transform (ToWP (c.StartPoint)), mProjXfm.Transform (ToWP (c.EndPoint)));
         dc.DrawLine (mDwgPen, start, end);
      }
      foreach (var bl in mProfile.BendLines) {
         var (start, end) = (mProjXfm.Transform (ToWP (bl.StartPoint)), mProjXfm.Transform (ToWP (bl.EndPoint)));
         dc.DrawLine (mBLPen, start, end);
      }
      if (HasBProfile) {
         foreach (var c in mBendProfile.Curves) {
            var (start, end) = (mProjXfm.Transform (ToWP (c.StartPoint.Translated (v))), mProjXfm.Transform (ToWP (c.EndPoint.Translated (v))));
            dc.DrawLine (mDwgPen, start, end);
         }
         foreach (var bl in mBendProfile.BendLines) {
            var (start, end) = (mProjXfm.Transform (ToWP (bl.StartPoint.Translated (v))), mProjXfm.Transform (ToWP (bl.EndPoint.Translated (v))));
            dc.DrawLine (mBLPen, start, end);
         }
      }
      base.OnRender (dc);
      Point ToWP (BPoint p) => new (p.X, p.Y);
   }

   void UpdateBound () {
      var pts = mProfile.Vertices;
      var v = new BVector (mProfile.Bound.MaxX + 5, 0);
      if (HasBProfile)
         pts.AddRange (mBendProfile.Vertices.Select (a => a.Translated (v)));
      UpdateProjXform (new Bound (pts.ToArray ()));
   }

   void UpdateProjXform (Bound b) {
      var margin = 20.0;
      double scaleX = (mViewportWidth - margin) / b.Width,
             scaleY = (mViewportHeight - margin) / b.Height;
      double scale = Math.Min (scaleX, scaleY);
      var xfm = Matrix.Identity;
      xfm.Scale (scale, -scale);
      Point projectedMidPt = xfm.Transform (new Point (b.Mid.X, b.Mid.Y));
      var (dx, dy) = (mViewportCenter.X - projectedMidPt.X, mViewportCenter.Y - projectedMidPt.Y);
      xfm.Translate (dx, dy);
      mProjXfm = xfm;
      mInvProjXfm = mProjXfm;
      mInvProjXfm.Invert ();
      mSnapTolerance = b.MaxX * 0.01;
   }
   #endregion

   #region Private Data ---------------------------------------------
   double mDwgLineWeight, mViewportWidth, mViewportHeight, mSnapTolerance;
   Rect mViewportRect;
   Bound mViewportBound;
   Point mViewportCenter;
   Matrix mProjXfm, mInvProjXfm;
   BPoint mCurrentMousePt, mStartPt, mSnapPoint;
   Pen? mBLPen, mBGPen, mDwgPen, mRemnantPen;
   Brush? mDwgBrush;
   TextBlock? mCords;
   Profile mProfile;
   BendProfile mBendProfile;
   #endregion
}