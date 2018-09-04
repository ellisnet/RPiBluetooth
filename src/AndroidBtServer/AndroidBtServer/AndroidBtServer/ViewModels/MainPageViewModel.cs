using System;
using System.Threading.Tasks;
using Acr.UserDialogs;
using AndroidBtServer.Services;
using Prism.Navigation;

namespace AndroidBtServer.ViewModels
{
    public class MainPageViewModel : BaseViewModel
    {
        private IBluetoothHostService _bluetoothService;

        #region Bindable properties

        private bool _isDeviceConnected;
        public bool IsDeviceConnected
        {
            get => _isDeviceConnected;
            set => SetProperty(ref _isDeviceConnected, value);
        }

        private bool _isDeviceDiscoveryEnabled;
        public bool IsDeviceDiscoveryEnabled
        {
            get => _isDeviceDiscoveryEnabled;
            set
            {
                SetProperty(ref _isDeviceDiscoveryEnabled, value);
                NotifyPropertyChanged(nameof(DiscoverabilityDisplay));
            }
        }

        private string _pairedDevicesDisplay;
        public string PairedDevicesDisplay
        {
            get => _pairedDevicesDisplay;
            set => SetProperty(ref _pairedDevicesDisplay, value);
        }

        public string DiscoverabilityDisplay =>
            $"Device {(IsDeviceDiscoveryEnabled ? "is" : "is not")} discoverable via Bluetooth.";

        #endregion

        #region Commands and their implementations

        #endregion

        public int UpdatePairedDeviceDisplay()
        {
            int pairedDeviceCount = _bluetoothService.GetPairedDeviceCount();
            PairedDevicesDisplay = $"{pairedDeviceCount} Bluetooth device" 
                                   + $"{(pairedDeviceCount == 1 ? " is" : "s are")} paired.";
            return pairedDeviceCount;
        }

        public override async void OnNavigatedTo(NavigationParameters parameters)
        {
            await Task.Delay(2000); //Wait a couple of secs for page to finish loading
            int pairedDeviceCount = UpdatePairedDeviceDisplay();

            if (pairedDeviceCount < 1)
            {
                IsDeviceDiscoveryEnabled = _bluetoothService.ToggleDiscoverability(true, 300);
                //await Task.Delay(5000);
                //IsDeviceDiscoveryEnabled = _bluetoothService.ToggleDiscoverability(false);
            }
        }

        public MainPageViewModel(
            INavigationService navigationService,
            IUserDialogs dialogService,
            IBluetoothHostService bluetoothService)
            : base(navigationService, dialogService)
        {
            _bluetoothService = bluetoothService ?? throw new ArgumentNullException(nameof(bluetoothService));
        }

        public override void Destroy()
        {
            _bluetoothService = null;
            base.Destroy();
        }
    }
}
