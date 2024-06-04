using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.NinaPegasusPWMFlat.PegasusPWMFlatDeviceDrivers.SDK;
using NINA.Profile.Interfaces;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.PegasusPWMFlatDevice.PegasusPWMFlatDeviceDrivers {

    [Export(typeof(IEquipmentProvider))]
    public class PegasusPWMFlatDeviceProvider : IEquipmentProvider<IFlatDevice> {
        private readonly IProfileService profileService;
        private readonly System.Lazy<IPegasusAstro> unityClient;

        [ImportingConstructor]
        public PegasusPWMFlatDeviceProvider(IProfileService profileService) {
            this.profileService = profileService;
            unityClient = PegasusPWMFlatDevice.pegasusClient;
        }

        public string Name => "PegasusPWMFlatDevice";

        private async Task<List<DeviceInfo>> ListUPBv3Device(CancellationToken token) {
            var resp = await unityClient.Value.ServerDeviceManagerConnected(token);
            var devices = new List<DeviceInfo>();
            if (resp.Code == RJesCode._200 && resp.Message == "success") {
                foreach (var item in resp.Data) {
                    if (item.Name == "UPBv3") {
                        devices.Add(item);
                    }
                }
            }
            return devices;
        }

        public IList<IFlatDevice> GetEquipment() {
            Logger.Error("getEquipment called");
            var devices = new List<IFlatDevice>();
            foreach (DeviceInfo device in ListUPBv3Device(CancellationToken.None).Result) {
                var powerbox = new UPBv3(unityClient, device);
                foreach (var port in powerbox.ListPWMPorts(CancellationToken.None).Result) {
                    devices.Add(new PegasusPWMFlatDeviceDriver(profileService, port, powerbox));
                }
            }

            return devices;
        }
    }
}