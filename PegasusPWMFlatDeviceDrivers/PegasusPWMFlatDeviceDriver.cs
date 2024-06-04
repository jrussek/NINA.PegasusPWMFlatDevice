using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.NinaPegasusPWMFlat.PegasusPWMFlatDeviceDrivers.SDK;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.PegasusPWMFlatDevice.PegasusPWMFlatDeviceDrivers {

    public class PegasusPWMFlatDeviceDriver : BaseINPC, IDevice, IFlatDevice {
        private readonly IProfileService _profileService;
        private readonly UPBv3 _powerbox;
        private readonly PWMPortMessage _port;

        public PegasusPWMFlatDeviceDriver(IProfileService profileService, PWMPortMessage port, UPBv3 powerbox) {
            Logger.Error("new device created");
            _profileService = profileService;
            _powerbox = powerbox;
            _port = port;
        }

        public string Id { get => _powerbox.device.DeviceID; }

        public string Name { get => _powerbox.device.Name; }

        public string Category => "Pegasus Astro";

        public string Description => "Configure a flat device from a pegasus powerbox PWM port";

        public string DriverInfo { get => _powerbox.device.Firmware; }

        public string DriverVersion { get => _powerbox.device.Revision; }

        /* contains the port number the flat panel is connected to */
        private int PortNumber { get => _port.PortNumber; set { } }

        public bool Connected { get; private set; }

        public IList<string> SupportedActions => [];

        public bool HasSetupDialog => false;

        CoverState IFlatDevice.CoverState => CoverState.NeitherOpenNorClosed;

        int IFlatDevice.MaxBrightness => 100;

        int IFlatDevice.MinBrightness => 5;

        private bool _lightOn;
        private int _level;

        bool IFlatDevice.LightOn {
            get {
                return _lightOn;
            }
            set {
                if (_lightOn != value) {
                    try {
                        UpdateOnOff(value);
                    } catch (Exception e) {
                        Logger.Error($"Error setting port {_port.Name} on/off: {e.Message}");
                        Connected = false;
                    }

                    RaisePropertyChanged();
                }
            }
        }

        private async void UpdateOnOff(bool value) {
            CancellationTokenSource cts = new();
            await _powerbox.SwitchPortOnOff(PortNumber, value, cts.Token);
            _lightOn = value;
            if (cts.IsCancellationRequested) {
                Logger.Error("Switching port on/off was canceled");
            }
        }

        int IFlatDevice.Brightness {
            get {
                return _level;
            }
            set {
                if (_level != value) {
                    try {
                        UpdateLevel(value);
                    } catch (Exception e) {
                        Logger.Error($"Error setting port {_port.Name} level: {e.Message}");
                        Connected = false;
                    }
                    RaisePropertyChanged();
                }
            }
        }

        private async void UpdateLevel(int value) {
            CancellationTokenSource cts = new();
            await _powerbox.SetLevel(PortNumber, value, cts.Token);
            _level = value;
            if (cts.IsCancellationRequested) {
                Logger.Error("Setting port level was canceled");
            }
        }

        string IFlatDevice.PortName {
            get => _port.Name; set { }
        }

        bool IFlatDevice.SupportsOpenClose => false;

        bool IFlatDevice.SupportsOnOff => true;

        string IDevice.DisplayName => $"{_powerbox.device.FullName} port {_port.Name}";

        public async Task<bool> Connect(CancellationToken token) {
            Connected = await _powerbox.Connect(token);
            try {
                if (Connected) {
                    _level = await _powerbox.GetLevel(PortNumber, token);
                    _lightOn = _level > 0;
                }
            } catch (Exception e) {
                Connected = false;
            }

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

        public void SetupDialog() => throw new NotImplementedException();

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