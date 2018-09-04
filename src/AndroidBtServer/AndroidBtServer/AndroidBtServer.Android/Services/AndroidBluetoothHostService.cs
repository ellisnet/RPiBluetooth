using System;
using System.Diagnostics;
using Android.Bluetooth;
using Android.Content;
using Android.Support.V4.Util;
using AndroidBtServer.Services;

namespace AndroidBtServer.Droid.Services
{
    public class AndroidBluetoothHostService : IBluetoothHostService
    {
        private static BluetoothAdapter adapter;
        private static readonly object adapterLocker = new object();
        private static readonly object discoverabilityLocker = new object();

        private readonly Context _context;

        private static void CheckAdapter()
        {
            if (adapter == null)
            {
                lock (adapterLocker)
                {
                    adapter = adapter ?? BluetoothAdapter.DefaultAdapter;
                    Debug.WriteLine($"Adapter address: {adapter.Address}");
                    Debug.WriteLine($"Is adapter enabled? {adapter.IsEnabled}");
                }
            }
        }

        #region Implementation of IBluetoothService

        public int GetPairedDeviceCount()
        {
            CheckAdapter();
            return adapter.BondedDevices.Count;
        }

        public bool ToggleDiscoverability(bool enabled, int enabledSeconds = 120)
        {
            if (enabledSeconds < 0) { throw new ArgumentOutOfRangeException(nameof(enabledSeconds));}

            CheckAdapter();
            bool result;

            lock (discoverabilityLocker)
            {
                Intent discoverableIntent = new Intent(BluetoothAdapter.ActionRequestDiscoverable);
                discoverableIntent.PutExtra(BluetoothAdapter.ExtraDiscoverableDuration, enabledSeconds);
                _context.StartActivity(discoverableIntent);




                result = enabled;

            }

            return result;
        }

        #endregion

        public AndroidBluetoothHostService(Context context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }
    }
}