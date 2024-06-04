using System.ComponentModel.Composition;
using System.Windows;

namespace NINA.PegasusPWMFlatDevice {

    [Export(typeof(ResourceDictionary))]
    public partial class Options : ResourceDictionary {

        public Options() {
            InitializeComponent();
        }
    }
}