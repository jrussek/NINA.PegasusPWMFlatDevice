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
        private readonly UPBv3 _powerbox;
        private readonly PWMPortMessage _port;

        public PegasusPWMFlatDeviceDriver(IProfileService profileService, PWMPortMessage port, UPBv3 powerbox) {
            Logger.Debug($"new device created for {powerbox} and port {port}");
            _powerbox = powerbox;
            _port = port;
        }

        public string Id { get => _powerbox.device.UniqueKey.ToString(); }

        public string Name => $"{_powerbox.device.FullName} port {_port.Name}";

        /* contains the port number the flat panel is connected to */
        private int PortNumber { get => _port.PortNumber; set { } }

        public string DisplayName => $"{_powerbox.device.FullName} - {_port.Name} (port {PortNumber})";

        public string Category => "Pegasus Astro";

        public string Description => "Configure a flat device from a pegasus powerbox PWM port";

        public string DriverInfo { get => _powerbox.device.DeviceID + " " + _powerbox.device.Firmware + " " + _powerbox.device.Revision; }

        public string DriverVersion { get => "1.0.1"; }

        public bool Connected { get; private set; }

        public IList<string> SupportedActions => [];

        public bool HasSetupDialog => false;

        public CoverState CoverState => CoverState.NeitherOpenNorClosed;

        public int MaxBrightness => 100;

        public int MinBrightness => 5;

        public string PortName {
            get => _port.Name; set { }
        }

        public bool SupportsOpenClose => false;

        public bool SupportsOnOff => true;

        private bool _lightOn;
        private int _level;

        public bool LightOn {
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
                    _level = 100;
                    RaisePropertyChanged("Brightness");
                    RaisePropertyChanged("LightOn");
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

        public int Brightness {
            get {
                if (!_lightOn) {
                    _level = 0;
                    return 0;
                }
                return _level;
            }
            set {
                if (_level != value) {
                    if (value == 0) {
                        LightOn = false;
                        _level = 0;
                        RaisePropertyChanged("LightOn");
                    } else {
                        try {
                            UpdateLevel(value);
                            LightOn = true;
                            RaisePropertyChanged("LightOn");
                            RaisePropertyChanged();
                        } catch (Exception e) {
                            Logger.Error($"Error setting port {_port.Name} level: {e.Message}");
                            Connected = false;
                        }
                    }
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