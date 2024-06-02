using NINA.Core.Utility.WindowService;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace NINA.NinaPegasusPWMFlat.NinaPegasusPWMFlatDrivers {

    [Export(typeof(IEquipmentProvider))]
    public class NinaPegasusPWMFlatDeviceProvider : IEquipmentProvider<IFlatDevice> {
        private IProfileService profileService;
        private IWindowServiceFactory windowServiceFactory;

        [ImportingConstructor]
        public NinaPegasusPWMFlatDeviceProvider(IProfileService profileService) {
            this.profileService = profileService;
        }

        public string Name => "NinaPegasusPWMFlat";

        public IList<IFlatDevice> GetEquipment() {
            var devices = new List<IFlatDevice>();
            devices.Add(new NinaPegasusPWMFlatDeviceDriver(profileService));

            return devices;
        }
    }
}