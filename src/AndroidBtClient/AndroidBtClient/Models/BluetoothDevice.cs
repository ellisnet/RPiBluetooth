namespace AndroidBtClient.Models
{
    public class BluetoothDevice
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public bool IsPaired { get; set; }
        public bool IsConnected { get; set; }
    }
}
