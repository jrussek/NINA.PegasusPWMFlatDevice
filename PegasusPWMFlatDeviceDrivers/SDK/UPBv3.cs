using NINA.Core.Utility;
using NINA.PegasusPWMFlatDevice.PegasusPWMFlatDeviceDrivers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.NinaPegasusPWMFlat.PegasusPWMFlatDeviceDrivers.SDK;

public class UPBv3(Lazy<IPegasusAstro> lazyUnityClient, DeviceInfo device) {
    public readonly DeviceInfo device = device;
    private readonly Lazy<IPegasusAstro> lazyUnityClient = lazyUnityClient;
    private IPegasusAstro UnityClient => lazyUnityClient.Value;

    public async Task<bool> Connect(CancellationToken token) {
        var Connected = false;
        var active = await UnityClient.DriverUPBv3Active(device.DeviceID, token);
        if (active.Code == RJesCode._400) {
            var start = await UnityClient.DriverUPBv2Start(device.DeviceID, token);
            if (start.Code == RJesCode._200 && start.Data.Message.State == "Started") {
                Connected = true;
            }
        } else if (active.Code == RJesCode._200 && active.Data.Message.State == "Connected") {
            Connected = true;
        }
        return Connected;
    }

    public async Task<int> GetLevel(int portnumber, CancellationToken token) {
        try {
            var ports = await ListPWMPorts(token);
            var port = ports.Find(p => p.PortNumber == portnumber);
            if (port != null) {
                return port.Level;
            }
        } catch (OperationCanceledException) {
            Logger.Error("GetBrightness Operation canceled");
        }
        return -1;
    }

    public async Task<List<PWMPortMessage>> ListPWMPorts(CancellationToken token) {
        var ports = new List<PWMPortMessage>();
        var resp = await UnityClient.DriverUPBv3PWMHubStatus(device.DeviceID, token);
        if (resp.Code == RJesCode._200 && resp.Message == "success") {
            ICollection<PWMPortMessage> hub = resp.Data.Message.Hub;
            foreach (PWMPortMessage port in hub) {
                /* port 1 and 2 do not support PWM */
                if (port.PortNumber > 2) {
                    ports.Add(port);
                }
            }
        } else {
            throw new Exception($"Error listing PWM ports: {resp.Message}");
        }
        return ports;
    }

    public async Task SetLevel(int portnumber, int level, CancellationToken token) {
        try {
            var resp = await UnityClient.DriverUPBv3PWM(portnumber, level, device.DeviceID, token);
            if (resp.Code != RJesCode._200 || resp.Message != "success") {
                throw new Exception($"Error setting PWM port level: {resp.Message}");
            }
        } catch (OperationCanceledException) {
            Logger.Error("SetBrightness Operation canceled");
        }
    }

    public async Task SwitchPortOnOff(int portnumber, bool state, CancellationToken token) {
        Task<PWMPortMessageDriverMessageRJesResult> call;
        if (state == true) {
            call = UnityClient.DriverUPBv3PWMOn(portnumber, device.DeviceID, token);
        } else {
            call = UnityClient.DriverUPBv3PWMOff(portnumber, device.DeviceID, token);
        }
        var response = await call;
        if (response.Code != RJesCode._200 || response.Message != "success") {
            throw new Exception($"Error switching PWM port: {response.Message}");
        }
    }
}