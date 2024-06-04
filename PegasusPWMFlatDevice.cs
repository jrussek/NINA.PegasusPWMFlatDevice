using NINA.Core.Utility;
using NINA.PegasusPWMFlatDevice.PegasusPWMFlatDeviceDrivers;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.ViewModel;
using Refit;
using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Settings = NINA.PegasusPWMFlatDevice.Properties.Settings;

namespace NINA.PegasusPWMFlatDevice {

    /// <summary>
    /// This class exports the IPluginManifest interface and will be used for the general plugin information and options
    /// The base class "PluginBase" will populate all the necessary Manifest Meta Data out of the AssemblyInfo attributes. Please fill these accoringly
    ///
    /// An instance of this class will be created and set as datacontext on the plugin options tab in N.I.N.A. to be able to configure global plugin settings
    /// The user interface for the settings will be defined by a DataTemplate with the key having the naming convention "NinaPegasusFlat_Options" where NinaPegasusFlat corresponds to the AssemblyTitle - In this template example it is found in the Options.xaml
    /// </summary>
    [Export(typeof(IPluginManifest))]
    public class PegasusPWMFlatDevice : PluginBase, INotifyPropertyChanged {
        private readonly IPluginOptionsAccessor pluginSettings;
        private readonly IProfileService profileService;

        [ImportingConstructor]
        public PegasusPWMFlatDevice(IProfileService profileService, IOptionsVM options) {
            if (Settings.Default.UpdateSettings) {
                Settings.Default.Upgrade();
                Settings.Default.UpdateSettings = false;
                CoreUtil.SaveSettings(Settings.Default);
            }

            // This helper class can be used to store plugin settings that are dependent on the current profile
            pluginSettings = new PluginOptionsAccessor(profileService, Guid.Parse(Identifier));
            this.profileService = profileService;

            // React on a changed profile
            InitializeClient(UnityUrl);
            profileService.ProfileChanged += ProfileService_ProfileChanged;
        }

        public static Lazy<IPegasusAstro> pegasusClient;

        public void InitializeClient(string baseUrl) {
            pegasusClient = new Lazy<IPegasusAstro>(() => RestService.For<IPegasusAstro>(baseUrl));
        }

        public string UnityUrl {
            get {
                return pluginSettings.GetValueString("UnityUrl", Settings.Default.DefaultUnityUrl);
            }
            set {
                pluginSettings.SetValueString("UnityUrl", value);
                InitializeClient(value);
                RaisePropertyChanged();
            }
        }

        public override Task Teardown() {
            // Make sure to unregister an event when the object is no longer in use. Otherwise garbage collection will be prevented.
            profileService.ProfileChanged -= ProfileService_ProfileChanged;

            return base.Teardown();
        }

        private void ProfileService_ProfileChanged(object sender, EventArgs e) {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}