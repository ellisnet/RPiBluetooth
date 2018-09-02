using System.Collections.Generic;
using System.Threading.Tasks;
using AndroidBtClient.Models;

namespace AndroidBtClient.Services
{
    public interface IBluetoothService
    {
        IList<BluetoothDevice> GetPairedDevices();
        void DisconnectDevice(BluetoothDevice device);
        Task<bool> ConnectToDevice(BluetoothDevice device);
    }
}
