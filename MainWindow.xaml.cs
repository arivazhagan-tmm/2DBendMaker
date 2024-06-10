using System.Windows;

namespace BendMaker;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
   public MainWindow () {
      InitializeComponent ();
      var (kFactor, thickness, radius) = (0.38, 2.0, 2.0);
      Curve c1 = new (ECurve.Line, "", new (0, 0), new (100, 0)),
         c2 = new (ECurve.Line, "", new (100, 0), new (100, 100)),
         c3 = new (ECurve.Line, "", new (100, 100), new (0, 100)),
         c4 = new (ECurve.Line, "", new (0, 100), new (0, 0)),
         c5 = new (ECurve.Line, "-90", new (0, 10), new (100, 10)),
         c6 = new (ECurve.Line, "90", new (0, 20), new (100, 20)),
         c7 = new (ECurve.Line, "90", new (0, 80), new (100, 80)),
         c8 = new (ECurve.Line, "-90", new (0, 90), new (100, 90)),
         c9 = new (ECurve.Line, "-90", new (10, 0), new (10, 100)),
         c10 = new (ECurve.Line, "90", new (20, 0), new (20, 100)),
         c11 = new (ECurve.Line, "90", new (80, 0), new (80, 100)),
         c12 = new (ECurve.Line, "-90", new (90, 0), new (90, 100));

      var profile = new Profile ([c1, c2, c3, c4, c9, c10, c11, c12], radius, thickness, kFactor);
      var bpm = new BProfileMaker (profile, EBDAlgorithm.PartialPreserve);
      var bProfile = bpm.MakeBendProfile ();
   }
}