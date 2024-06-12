using System.Windows;

namespace BendMaker;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
   public MainWindow () {
      InitializeComponent ();
   }

   void OnImport_Click (object sender, RoutedEventArgs e) {
      mIsImported = true;
      mProfile = geoReader.ReadGeo ();
      mBPM = new BProfileMaker (mProfile, EBDAlgorithm.EquallyDistributed);
      mBendProfile = mBPM.MakeBendProfile ();
      System.Windows.MessageBox.Show ("The file has been modified and ready to export", "Ready");
   }

   void OnExport_Click (object sender, RoutedEventArgs e) {
      if (mIsImported) new GeoWriter (mBendProfile, geoReader.FileName).Save ();
      else System.Windows.MessageBox.Show ("Please import a geo file", "Error");
      geoReader = new GeoRead ();
   }

   GeoRead geoReader = new ();
   bool mIsImported = false;
   Profile mProfile;
   BProfileMaker mBPM;
   BendProfile mBendProfile;
}