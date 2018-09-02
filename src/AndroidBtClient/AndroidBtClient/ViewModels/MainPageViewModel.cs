using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Acr.UserDialogs;
using AndroidBtClient.Models;
using AndroidBtClient.Services;
using Prism.Commands;
using Prism.Navigation;

namespace AndroidBtClient.ViewModels
{
    public class MainPageViewModel : BaseViewModel
    {
        private IBluetoothService _bluetoothService;
        private readonly string _devicePrefixToLookFor = "display";

        private BluetoothDevice _pairedDevice;

        #region Bindable properties

        public bool IsDevicePaired => _pairedDevice?.IsPaired ?? false;
        public bool IsDeviceConnected => _pairedDevice?.IsConnected ?? false;

        private bool _isDeviceConnecting;
        public bool IsDeviceConnecting
        {
            get => _isDeviceConnecting;
            set => SetProperty(ref _isDeviceConnecting, value);
        }

        #endregion

        #region Commands and their implementations

        #region ConnectToDeviceCommand

        private DelegateCommand _connectToDeviceCommand;
        public DelegateCommand ConnectToDeviceCommand => _connectToDeviceCommand
            ?? (_connectToDeviceCommand = new DelegateCommand(
             async () =>
             {
                 IsDeviceConnecting = true;
                 try
                 {
                     if (_pairedDevice == null)
                     {
                         throw new InvalidOperationException("Couldn't find a paired bluetooth device to connect to.");
                     }
                     await _bluetoothService.ConnectToDevice(_pairedDevice);
                 }
                 catch (Exception ex)
                 {
                     await ShowErrorAsync($"Error while connecting: {ex.Message}");
                 }
                 finally
                 {
                     IsDeviceConnecting = false;
                     NotifyPropertyChanged(nameof(IsDeviceConnected));
                 }
             },
             () => IsDevicePaired && (!IsDeviceConnected) && (!IsDeviceConnecting))
             .ObservesProperty(() => IsDevicePaired)
             .ObservesProperty(() => IsDeviceConnected)
             .ObservesProperty(() => IsDeviceConnecting));

        #endregion

        #endregion

        public override async void OnNavigatedTo(NavigationParameters parameters)
        {
            await Task.Delay(2000); //Wait a couple of secs for page to finish loading

            IList<BluetoothDevice> pairedDevices = _bluetoothService.GetPairedDevices();

            if (pairedDevices.Count < 1)
            {
                await ShowInfoAsync("No paired bluetooth devices.");
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine($"{pairedDevices.Count} paired bluetooth device{(pairedDevices.Count > 1 ? "s" : "")} found:");
                foreach (BluetoothDevice device in pairedDevices)
                {
                    sb.AppendLine($" - {device.Name} - {device.Address}");
                }

                await ShowInfoAsync(sb.ToString());

                _pairedDevice = pairedDevices.FirstOrDefault(f =>
                    f.Name.StartsWith(_devicePrefixToLookFor, StringComparison.InvariantCultureIgnoreCase));
                NotifyPropertyChanged(nameof(IsDevicePaired));
            }
        }

        public MainPageViewModel(
            INavigationService navigationService,
            IUserDialogs dialogService,
            IBluetoothService bluetoothService)
            : base(navigationService, dialogService)
        {
            _bluetoothService = bluetoothService ?? throw new ArgumentNullException(nameof(bluetoothService));
        }

        public override void Destroy()
        {
            _bluetoothService?.DisconnectDevice(_pairedDevice);
            _pairedDevice = null;
            _bluetoothService = null;
            base.Destroy();
        }
    }
}
