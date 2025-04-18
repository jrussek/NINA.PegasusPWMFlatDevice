﻿using NINA.Core.Utility;
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

    private string deviceGUID = device.UniqueKey.ToString();

    public async Task<bool> Connect(CancellationToken token) {
        var Connected = false;
        var active = await UnityClient.DriverUPBv3Active(deviceGUID, token);
        if (active.Code == RJesCode._400) {
            var start = await UnityClient.DriverUPBv2Start(deviceGUID, token);
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
        var resp = await UnityClient.DriverUPBv3PWMHubStatus(deviceGUID, token);
        if (resp.Code == RJesCode._200) {
            Logger.Error($"Retrieved PWM Hub Status, {resp.Message}");
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
            var resp = await UnityClient.DriverUPBv3PWM(portnumber, level, deviceGUID, token);
            if (resp.Code != RJesCode._200) {
                throw new Exception($"Error setting PWM port level: {resp.Message}");
            } else {
                Logger.Debug($"Set PWM port {portnumber} to level {level}. Message: {resp.Message}");
            }
        } catch (OperationCanceledException) {
            Logger.Error("SetBrightness Operation canceled");
        }
    }

    public async Task SwitchPortOnOff(int portnumber, bool state, CancellationToken token) {
        Task<PWMPortMessageDriverMessageRJesResult> call;
        if (state == true) {
            call = UnityClient.DriverUPBv3PWMOn(portnumber, deviceGUID, token);
        } else {
            call = UnityClient.DriverUPBv3PWMOff(portnumber, deviceGUID, token);
        }
        var response = await call;
        if (response.Code != RJesCode._200) {
            Logger.Error($"Error switching PWM port on/off: {response.Message}");
            throw new Exception($"Error switching PWM port: {response.Message}");
        } else {
            Logger.Debug($"Switched PWM port {portnumber} to state {state}. Message: {response.Message}");
        }
    }
}