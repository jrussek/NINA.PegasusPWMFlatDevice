using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Refit;
using NINA.Core.Utility.Notification;
using NINA.Core.Utility.WindowService;
using NINA.Profile.Interfaces;
using System.Collections.ObjectModel;

namespace NINA.NinaPegasusPWMFlat.NinaPegasusPWMFlatDrivers {

    public class NinaPegasusPWMFlatDeviceDriver : BaseINPC, IDevice, IFlatDevice {
        private readonly IPegasusAstro client;
        private readonly IProfileService _profileService;

        public NinaPegasusPWMFlatDeviceDriver(IProfileService profileService) {
            _profileService = profileService;
            client = RestService.For<IPegasusAstro>(UnityBaseUrl);
        }

        /* contains the GUID of the pegasus device */
        private System.Guid DeviceGUID { get; set; }

        /* contains the port number the flat panel is connected to */
        private int PortNumber { get; set; }

        private bool PortActive { get; set; }

        private static string UnityBaseUrl = "http://localhost:32000/";

        private string DeviceName { get; set; }

        private string SerialNumber;

        public string Name { get; }

        public string Category => "NinaPegasusFlat Weather Driver";

        public string Description => "Configure a flat device from a pegasus powerbox";

        public string DriverInfo { get; }

        public string DriverVersion { get; private set; }

        public bool Connected { get; private set; }

        public IList<string> SupportedActions => new List<string>();

        public IWindowService WindowService { get; set; } = new WindowService();

        public bool HasSetupDialog => !Connected;

        public string Id { get; }

        CoverState IFlatDevice.CoverState => throw new NotImplementedException();

        int IFlatDevice.MaxBrightness => 100;

        int IFlatDevice.MinBrightness => 5;

        private async Task<Collection<DeviceInfo>> ListUPBv3Device(CancellationToken token) {
            var resp = await client.ServerDeviceManagerConnected(token);
            var devices = new Collection<DeviceInfo>();
            if (resp.Code == RJesCode._200 && resp.Message == "success") {
                foreach (var item in resp.Data) {
                    if (item.Name == "UPBv3") {
                        devices.Add(item);
                    }
                }
            }
            return devices;
        }

        private async Task<bool> SwitchPort(bool state, CancellationToken token) {
            Task<PWMPortMessageDriverMessageRJesResult> call;
            if (state == true) {
                call = client.DriverUPBv3PWMOn(PortNumber, DeviceGUID.ToString(), token);
            } else {
                call = client.DriverUPBv3PWMOff(PortNumber, DeviceGUID.ToString(), token);
            }
            var response = await call;
            if (response.Code == RJesCode._200 && response.Message == "success") {
                return state;
            }
            return false;
        }

        private async Task<int> ChangeBrightness(int brightness, CancellationToken token) {
            var resp = await client.DriverUPBv3PWM(PortNumber, brightness, DeviceGUID.ToString(), token);
            if (resp.Code == RJesCode._200 && resp.Message == "success") {
                return brightness;
            } else {
                Notification.ShowError($"Error setting brightness: {resp.Message}");
            }
            return -1;
        }

        bool IFlatDevice.LightOn {
            get { return PortActive; }
            set { PortActive = SwitchPort(value, new CancellationToken()).Result; RaisePropertyChanged(); }
        }

        int IFlatDevice.Brightness { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        string IFlatDevice.PortName {
            get;
            set; /* set during setup */
        }

        bool IFlatDevice.SupportsOpenClose => throw new NotImplementedException();

        bool IFlatDevice.SupportsOnOff => true;

        public async Task<bool> Connect(CancellationToken token) {
            var active = await client.DriverUPBv3Active(DeviceGUID.ToString(), token);
            if (active.Code == RJesCode._400) {
                var start = await client.DriverUPBv2Start(DeviceGUID.ToString(), token);
                if (start.Code == RJesCode._200 && start.Data.Message.State == "Started") {
                    Connected = true;
                }
            } else if (active.Code == RJesCode._200 && active.Data.Message.State == "Connected") {
                Connected = true;
            } else {
                Connected = false;

                return Connected;
            }

            Connected = true;

            return Connected;
        }

        public void Disconnect() {
            Connected = false;
        }

        #region Unsupported

        public void SendCommandBlind(string command, bool raw = true) {
            throw new NotImplementedException();
        }

        public bool SendCommandBool(string command, bool raw = true) {
            throw new NotImplementedException();
        }

        public string SendCommandString(string command, bool raw = true) {
            throw new NotImplementedException();
        }

        public void SetupDialog() {
            WindowService.ShowDialog(this, "UPBv3 PWM Flat Port Setup", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.SingleBorderWindow);
        }

        public string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
        }

        Task<bool> IFlatDevice.Open(CancellationToken ct, int delay) {
            throw new NotImplementedException();
        }

        Task<bool> IFlatDevice.Close(CancellationToken ct, int delay) {
            throw new NotImplementedException();
        }

        #endregion Unsupported
    }
}