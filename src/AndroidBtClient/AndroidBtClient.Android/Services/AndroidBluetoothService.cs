using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Android.Bluetooth;
using AndroidBtClient.Services;
using Java.Util;
using AppBluetoothDevice = AndroidBtClient.Models.BluetoothDevice;

namespace AndroidBtClient.Droid.Services
{
    public class AndroidBluetoothService : IBluetoothService
    {
        private static BluetoothAdapter adapter;
        private static readonly object adapterLocker = new object();
        private static UUID BtServiceUuid => UUID.FromString("34B1CF4D-1069-4AD6-89B6-E161D79BE4D8");

        // Key = Device address
        // Value.Item1 = Connected Bluetooth device
        // Value.Item2 = Connected device socket
        private static readonly Dictionary<string, Tuple<BluetoothDevice, BluetoothSocket>> connectedDevices 
            = new Dictionary<string, Tuple<BluetoothDevice, BluetoothSocket>>();

        private static readonly object connectedDeviceLocker = new object();

        #region Static methods

        private static void AddConnectedDevice(BluetoothDevice device, BluetoothSocket socket)
        {
            if (device != null && socket != null)
            {
                lock (connectedDeviceLocker)
                {
                    if (!connectedDevices.ContainsKey(device.Address))
                    {
                        connectedDevices.Add(device.Address, Tuple.Create(device, socket));
                    }
                }
            }
        }

        private static void RemoveConnectedDevice(string address)
        {
            if (!String.IsNullOrWhiteSpace(address))
            {
                CheckAdapter();

                address = address.Trim();
                lock (connectedDeviceLocker)
                {
                    string deviceKey = null;
                    foreach (string key in connectedDevices.Keys)
                    {
                        if (key.Equals(address, StringComparison.InvariantCultureIgnoreCase))
                        {
                            deviceKey = key;
                            break;
                        }
                    }

                    if (deviceKey != null)
                    {
                        Tuple<BluetoothDevice, BluetoothSocket> deviceToRemove = connectedDevices[deviceKey];

                        BluetoothSocket socket = deviceToRemove.Item2;
                        if (socket.IsConnected)
                        {
                            socket.Close();
                        }
                        socket.Dispose();

                        deviceToRemove.Item1.Dispose();

                        connectedDevices.Remove(deviceKey);
                    }
                }
            }
        }

        private static bool IsConnected(BluetoothDevice device)
        {
            bool result = false;

            lock (connectedDeviceLocker)
            {
                result = (!String.IsNullOrWhiteSpace(device?.Address)) 
                         && connectedDevices.Any(a => a.Key.Equals(device.Address, StringComparison.InvariantCultureIgnoreCase));
            }

            return result;
        }

        private static void CheckAdapter()
        {
            if (adapter == null)
            {
                lock (adapterLocker)
                {
                    adapter = adapter ?? BluetoothAdapter.DefaultAdapter;
                }
            }
        }

        #endregion

        #region IBluetoothService implementation

        public IList<AppBluetoothDevice> GetPairedDevices()
        {
            CheckAdapter();
            return adapter.BondedDevices.Select(s => new AppBluetoothDevice
            {
                Name = s.Name,
                Address = s.Address,
                IsPaired = true,
                IsConnected = IsConnected(s),
            }).ToArray();
        }

        public void DisconnectDevice(AppBluetoothDevice device) => RemoveConnectedDevice(device?.Address);

        public async Task<bool> ConnectToDevice(AppBluetoothDevice device)
        {
            bool result = false;

            if (device != null)
            {
                CheckAdapter();

                do
                {
                    lock (connectedDeviceLocker)
                    {
                        if (connectedDevices.Keys.Any(a =>
                            a.Equals(device.Address, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            //Already connected
                            result = true;
                        }
                    }

                    if (result) { break;}

                    BluetoothDevice btDevice =
                        adapter.BondedDevices.FirstOrDefault(f => f.Address.Equals(device.Address));
                    if (btDevice == null) { break;}

                    try
                    {
                        BluetoothSocket socket = btDevice.CreateRfcommSocketToServiceRecord(BtServiceUuid);
                        await socket.ConnectAsync();
                        await Task.Delay(1000);

                        if (socket.IsConnected)
                        {
                            lock (connectedDeviceLocker)
                            {
                                if (connectedDevices.Keys.All(a =>
                                    !a.Equals(device.Address, StringComparison.InvariantCultureIgnoreCase)))
                                {
                                    connectedDevices.Add(btDevice.Address, Tuple.Create(btDevice, socket));
                                    result = true;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.ToString());
                        Debugger.Break();
                        result = false;
                    }
                } while (false);

                device.IsConnected = result;
                if (result && (!device.IsPaired))
                {
                    device.IsPaired = true;
                }
            }

            return result;
        }

        #endregion
    }
}
