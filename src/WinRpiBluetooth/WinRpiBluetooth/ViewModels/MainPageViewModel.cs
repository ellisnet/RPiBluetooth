//Most code in this file comes from: https://github.com/Microsoft/Windows-universal-samples/blob/master/Samples/BluetoothRfcommChat/cs/Scenario2_ChatServer.xaml.cs

using System;
using System.Diagnostics;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using WinRpiBluetooth.Config;

namespace WinRpiBluetooth.ViewModels
{
    public class MainPageViewModel : SimpleViewModel
    {
        private StreamSocket _socket;
        private DataWriter _writer;
        private RfcommServiceProvider _rfcommProvider;
        private StreamSocketListener _socketListener;

        #region Bindable properties

        private string _greeting;
        public string Greeting
        {
            get => _greeting;
            set => SetProperty(ref _greeting, value);
        }

        public string ListenButtonDisplay => (IsListening)
            ? "Listening..."
            : "Start listening";

        private string _messageToSend;
        public string MessageToSend
        {
            get => _messageToSend;
            set => SetProperty(ref _messageToSend, value);
        }

        private bool _isListening;
        public bool IsListening
        {
            get => _isListening;
            set
            {
                SetProperty(ref _isListening, value);
                NotifyPropertyChanged(nameof(ListenButtonDisplay));
                BeginListeningCommand.RaiseCanExecuteChanged();
            }
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        #endregion

        #region Commands and their implementations

        #region BeginListeningCommand

        private SimpleCommand _beginListeningCommand;

        public SimpleCommand BeginListeningCommand => _beginListeningCommand
            ?? (_beginListeningCommand = new SimpleCommand(
               () => (!IsListening) && (!IsConnected),
               InitializeRfcommServer));

        #endregion

        #endregion

        #region Private methods

        /// <summary>
        /// Initializes the server using RfcommServiceProvider to advertise the Chat Service UUID and start listening
        /// for incoming connections.
        /// </summary>
        private async void InitializeRfcommServer()
        {
            IsListening = true;

            try
            {
                _rfcommProvider = await RfcommServiceProvider.CreateAsync(RfcommServiceId.FromUuid(Constants.RfcommChatServiceUuid));
            }
            // Catch exception HRESULT_FROM_WIN32(ERROR_DEVICE_NOT_AVAILABLE).
            catch (Exception ex) when ((uint)ex.HResult == 0x800710DF)
            {
                // The Bluetooth radio may be off.
                //rootPage.NotifyUser("Make sure your Bluetooth Radio is on: " + ex.Message, NotifyType.ErrorMessage);
                //Debug.WriteLine("Make sure your Bluetooth Radio is on: " + ex.Message);
                await ShowError($"Make sure your Bluetooth Radio is on: {ex.Message}");
                IsListening = false;
                return;
            }


            // Create a listener for this service and start listening
            _socketListener = new StreamSocketListener();
            _socketListener.ConnectionReceived += OnConnectionReceived;
            var rfcomm = _rfcommProvider.ServiceId.AsString();

            await _socketListener.BindServiceNameAsync(_rfcommProvider.ServiceId.AsString(),
                SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);

            // Set the SDP attributes and start Bluetooth advertising
            InitializeServiceSdpAttributes(_rfcommProvider);

            try
            {
                _rfcommProvider.StartAdvertising(_socketListener, true);
            }
            catch (Exception e)
            {
                // If you aren't able to get a reference to an RfcommServiceProvider, tell the user why.  Usually throws an exception if user changed their privacy settings to prevent Sync w/ Devices.  
                //rootPage.NotifyUser(e.Message, NotifyType.ErrorMessage);
                //Debug.WriteLine($"Error while initializing Rfcomm Server: {e.Message}");
                await ShowError($"Error while initializing Rfcomm Server: {e.Message}");
                IsListening = false;
                return;
            }

            //rootPage.NotifyUser("Listening for incoming connections", NotifyType.StatusMessage);
            //Debug.WriteLine("Listening for incoming connections");

            await ShowInfo("Listening for incoming connections");
        }

        /// <summary>
        /// Creates the SDP record that will be revealed to the Client device when pairing occurs.  
        /// </summary>
        /// <param name="rfcommProvider">The RfcommServiceProvider that is being used to initialize the server</param>
        private void InitializeServiceSdpAttributes(RfcommServiceProvider rfcommProvider)
        {
            var sdpWriter = new DataWriter();

            // Write the Service Name Attribute.
            sdpWriter.WriteByte(Constants.SdpServiceNameAttributeType);

            // The length of the UTF-8 encoded Service Name SDP Attribute.
            sdpWriter.WriteByte((byte)Constants.SdpServiceName.Length);

            // The UTF-8 encoded Service Name value.
            sdpWriter.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
            sdpWriter.WriteString(Constants.SdpServiceName);

            // Set the SDP Attribute on the RFCOMM Service Provider.
            rfcommProvider.SdpRawAttributes.Add(Constants.SdpServiceNameAttributeId, sdpWriter.DetachBuffer());
        }

        /// <summary>
        /// Invoked when the socket listener accepts an incoming Bluetooth connection.
        /// </summary>
        /// <param name="sender">The socket listener that accepted the connection.</param>
        /// <param name="args">The connection accept parameters, which contain the connected socket.</param>
        private async void OnConnectionReceived(
            StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            // Don't need the listener anymore
            try
            {
                _socketListener.Dispose();
                _socketListener = null;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                Debugger.Break();
            }
            IsListening = false;

            try
            {
                _socket = args.Socket;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                Debugger.Break();
                await Window.Current.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    //rootPage.NotifyUser(e.Message, NotifyType.ErrorMessage);
                    //Debug.WriteLine($"Error while establishing socket: {e.Message}");
                    await ShowError($"Error while establishing socket: {e.Message}");
                });
                Disconnect();
                return;
            }

            // Note - this is the supported way to get a Bluetooth device from a given socket
            var remoteDevice = await BluetoothDevice.FromHostNameAsync(_socket.Information.RemoteHostName);

            _writer = new DataWriter(_socket.OutputStream);
            var reader = new DataReader(_socket.InputStream);
            bool remoteDisconnection = false;

            await Window.Current.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                //rootPage.NotifyUser("Connected to Client: " + remoteDevice.Name, NotifyType.StatusMessage);
                //Debug.WriteLine($"Connected to Client: {remoteDevice.Name}");
                await ShowInfo($"Connected to Client: {remoteDevice.Name}");
                IsConnected = true;
            });

            // Infinite read buffer loop
            while (true)
            {
                try
                {
                    // Based on the protocol we've defined, the first uint is the size of the message
                    uint readLength = await reader.LoadAsync(sizeof(uint));

                    // Check if the size of the data is expected (otherwise the remote has already terminated the connection)
                    if (readLength < sizeof(uint))
                    {
                        remoteDisconnection = true;
                        break;
                    }
                    uint currentLength = reader.ReadUInt32();

                    // Load the rest of the message since you already know the length of the data expected.  
                    readLength = await reader.LoadAsync(currentLength);

                    // Check if the size of the data is expected (otherwise the remote has already terminated the connection)
                    if (readLength < currentLength)
                    {
                        remoteDisconnection = true;
                        break;
                    }
                    string message = reader.ReadString(currentLength);

                    await Window.Current.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        //ConversationListBox.Items.Add("Received: " + message);
                        //Debug.WriteLine($"Received: {message}");
                        await ShowInfo($"Received: {message}");
                    });
                }
                // Catch exception HRESULT_FROM_WIN32(ERROR_OPERATION_ABORTED).
                catch (Exception e) when ((uint)e.HResult == 0x800703E3)
                {
                    Debug.WriteLine(e.ToString());
                    Debugger.Break();
                    await Window.Current.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        //rootPage.NotifyUser("Client Disconnected Successfully", NotifyType.StatusMessage);
                        //Debug.WriteLine("Client Disconnected Successfully");
                        await ShowInfo("Client Disconnected Successfully");
                        IsConnected = false;
                    });
                    break;
                }
            }

            reader.DetachStream();
            if (remoteDisconnection)
            {
                Disconnect();
                await Window.Current.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    //rootPage.NotifyUser("Client disconnected", NotifyType.StatusMessage);
                    //Debug.WriteLine("Client disconnected");
                    await ShowInfo("Client disconnected");
                    IsConnected = false;
                });
            }
        }

        private async void SendMessage()
        {
            // There's no need to send a zero length message
            if (!String.IsNullOrWhiteSpace(MessageToSend))
            {
                // Make sure that the connection is still up and there is a message to send
                if (_socket != null)
                {
                    string message = MessageToSend.Trim();
                    _writer.WriteUInt32((uint)message.Length);
                    _writer.WriteString(message);

                    //ConversationListBox.Items.Add("Sent: " + message);
                    // Clear the messageTextBox for a new message
                    MessageToSend = String.Empty;

                    await _writer.StoreAsync();

                }
                else
                {
                    //rootPage.NotifyUser("No clients connected, please wait for a client to connect before attempting to send a message", NotifyType.StatusMessage);
                    await ShowError("No clients connected, please wait for a client to connect before attempting to send a message");
                }
            }
        }

        private async void Disconnect()
        {
            if (_rfcommProvider != null)
            {
                _rfcommProvider.StopAdvertising();
                _rfcommProvider = null;
            }

            if (_socketListener != null)
            {
                _socketListener.Dispose();
                _socketListener = null;
            }

            if (_writer != null)
            {
                _writer.DetachStream();
                _writer = null;
            }

            if (_socket != null)
            {
                _socket.Dispose();
                _socket = null;
            }
            await Window.Current.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                IsListening = false;
            });
        }

        #endregion

        public MainPageViewModel()
        {
            Greeting = "Hello world!";
        }
    }
}
