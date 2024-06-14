using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VA = System.Windows.VerticalAlignment;
using HA = System.Windows.HorizontalAlignment;
using Microsoft.Win32;
using System.Data;
using System.Windows.Controls.Primitives;

namespace BendMaker;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
   #region Constructors ---------------------------------------------
   public MainWindow () {
      InitializeComponent ();
      (Height, Width) = (750, 800);
      WindowStartupLocation = WindowStartupLocation.CenterScreen;
      WindowState = WindowState.Maximized;
      WindowStyle = WindowStyle.SingleBorderWindow;
      Loaded += OnLoaded; ;
   }
   #endregion

   #region Properties -----------------------------------------------
   public static MainWindow? It;
   #endregion

   #region Implementation -------------------------------------------
   void OnLoaded (object sender, RoutedEventArgs e) {
      It = this;
      Title = "Bend Profile Maker";
      mBGColor = new SolidColorBrush (Color.FromArgb (255, 200, 200, 200));
      Background = mBGColor;
      var spStyle = new Style ();
      spStyle.Setters.Add (new Setter (HeightProperty, 20.0));
      spStyle.Setters.Add (new Setter (VerticalContentAlignmentProperty, VA.Top));
      spStyle.Setters.Add (new Setter (BackgroundProperty, mBGColor));
      var menuPanel = new StackPanel () { Style = spStyle };
      var menuStyle = new Style ();
      menuStyle.Setters.Add (new Setter (WidthProperty, 50.0));
      menuStyle.Setters.Add (new Setter (HeightProperty, 20.0));
      var menu = new Menu () { Background = mBGColor };
      var fileMenu = new MenuItem () { Style = menuStyle, Header = "_File" };
      var openMenu = new MenuItem () { Header = "_Open...", IsEnabled = true };
      openMenu.Click += (s, e) => {
         var dlg = new OpenFileDialog () {
            DefaultExt = ".geo",
            Title = "Import Geo file",
            Filter = "Geo files (*.geo)|*.geo"
         };
         if (dlg.ShowDialog () is true) {
            mGeoReader = new (dlg.FileName);
            mBPM ??= new ();
            mProfile = mGeoReader.ParseProfile ();
            if (mViewport != null) mViewport.Profile = mProfile;
         }
      };
      var saveMenu = new MenuItem () { Header = "_Save..." };
      saveMenu.Click += (s, e) => {
         var currentFileName = mGeoReader?.FileName;
         var dlg = new SaveFileDialog () { FileName = $"{currentFileName}_BendProfile", Filter = "GEO|*.geo" };
         if (dlg.ShowDialog () is true) {
            mGeoWriter = new (mBendProfile, currentFileName!);
            mGeoWriter.OverWrite (dlg.FileName);
         }
      };
      fileMenu.Items.Add (openMenu);
      fileMenu.Items.Add (saveMenu);
      menu.Items.Add (fileMenu);
      menuPanel.Children.Add (menu);
      var optionPanel = new StackPanel () { HorizontalAlignment = HA.Left, Margin = new Thickness (0, 50, 0, 0) };
      var btnStyle = new Style ();
      btnStyle.Setters.Add (new Setter (HeightProperty, 40.0));
      btnStyle.Setters.Add (new Setter (BackgroundProperty, Brushes.WhiteSmoke));
      btnStyle.Setters.Add (new Setter (MarginProperty, new Thickness (5.0)));
      btnStyle.Setters.Add (new Setter (HorizontalAlignmentProperty, HA.Left));
      btnStyle.Setters.Add (new Setter (VerticalAlignmentProperty, VA.Center));
      var borderStyle = new Style () { TargetType = typeof (Border) };
      borderStyle.Setters.Add (new Setter (Border.CornerRadiusProperty, new CornerRadius (5.0)));
      borderStyle.Setters.Add (new Setter (Border.BorderThicknessProperty, new Thickness (5.0)));
      btnStyle.Resources = new ResourceDictionary { [typeof (Border)] = borderStyle };
      foreach (var name in Enum.GetNames (typeof (EOption))) {
         var btn = new ToggleButton () {
            Content = name,
            ToolTip = name,
            Style = btnStyle
         };
         btn.Click += OnOptionClicked;
         optionPanel.Children.Add (btn);
      }
      mViewport = new Viewport ();
      var viewportPanel = new WrapPanel ();
      viewportPanel.MouseEnter += (s, e) => Cursor = Cursors.Cross;
      viewportPanel.MouseLeave += (s, e) => Cursor = Cursors.Arrow;
      viewportPanel.MouseDown += (s, e) => {
         if (e.ChangedButton is MouseButton.Middle && e.ButtonState is MouseButtonState.Pressed)
            Cursor = Cursors.Hand;
      };
      viewportPanel.MouseUp += (s, e) => Cursor = Cursors.Cross;
      var context = new ContextMenu ();
      var zoomExtnd = new MenuItem () { Header = "Zoom Extends" };
      zoomExtnd.Click += (s, e) => mViewport.ZoomExtents ();
      context.Items.Add (zoomExtnd);
      viewportPanel.ContextMenu = context;
      viewportPanel.Children.Add (mViewport);
      var sp = new StackPanel ();
      var dp = new DockPanel () { LastChildFill = true };
      dp.Children.Add (menuPanel);
      dp.Children.Add (optionPanel);
      dp.Children.Add (viewportPanel);
      DockPanel.SetDock (menuPanel, Dock.Top);
      DockPanel.SetDock (optionPanel, Dock.Left);
      mMainPanel.Content = dp;
   }

   void OnOptionClicked (object sender, RoutedEventArgs e) {
      if (sender is not ToggleButton btn || mViewport is null) return;
      if (!Enum.TryParse ($"{btn.ToolTip}", out EOption opt)) return;
      switch (opt) {
         case EOption.MakeBendProfile:
            if (mViewport is null || mBPM is null || mViewport.HasBProfile) return;
            mBendProfile = mBPM.MakeBendProfile (mProfile, EBDAlgorithm.EquallyDistributed);
            mViewport.BendProfile = mBendProfile;
            break;
      }
   }
   #endregion

   #region Private Data ---------------------------------------------
   Brush? mBGColor;
   Viewport? mViewport;
   GeoReader? mGeoReader;
   GeoWriter? mGeoWriter;
   Profile mProfile;
   BProfileMaker? mBPM;
   BendProfile mBendProfile;
   #endregion
}

public enum EOption { MakeBendProfile }

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