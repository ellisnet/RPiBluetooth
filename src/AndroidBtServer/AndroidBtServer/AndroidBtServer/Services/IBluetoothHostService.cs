namespace AndroidBtServer.Services
{
    public interface IBluetoothHostService
    {
        int GetPairedDeviceCount();
        bool ToggleDiscoverability(bool enabled, int enabledSeconds = 120);
    }
}
