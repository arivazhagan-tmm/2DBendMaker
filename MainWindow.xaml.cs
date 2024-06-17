using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VA = System.Windows.VerticalAlignment;
using HA = System.Windows.HorizontalAlignment;
using Microsoft.Win32;
using System.Windows.Controls.Primitives;
using System.IO;

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
            mCRProfile = new (mProfile);
            mBendProfile = mCRProfile.Validation ();
            if (mViewport != null) {
               mViewport.Profile = mProfile;
               mViewport.BendProfile = mBendProfile;
               mViewport.ZoomExtents ();
            }
         }
      };
      var saveMenu = new MenuItem () { Header = "_Save..." };
      saveMenu.Click += (s, e) => {
         var currentFileName = mGeoReader?.FileName;
         currentFileName = Path.GetFileNameWithoutExtension (currentFileName);
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
   CRProfile mCRProfile;
   BProfileMaker? mBPM;
   BendProfile mBendProfile;
   #endregion
}

public enum EOption { MakeBendProfile }
