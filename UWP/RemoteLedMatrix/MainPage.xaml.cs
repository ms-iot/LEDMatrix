// Copyright (c) Microsoft. All rights reserved.

namespace RemoteLedMatrix
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Windows.ApplicationModel;
    using Windows.Devices.Enumeration;
    using Windows.Foundation;
    using Windows.Graphics.Display;
    using Windows.Graphics.Imaging;
    using Windows.Media;
    using Windows.Media.Capture;
    using Windows.Media.MediaProperties;
    using Windows.System.Display;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;
    using Windows.UI.Xaml.Media.Imaging;
    using Windows.UI.Xaml.Navigation;
    using Microsoft.Maker.Firmata;
    using Microsoft.Maker.RemoteWiring;
    using Microsoft.Maker.Serial;
    using RemoteLedMatrix.Helpers;
    using Panel = Windows.Devices.Enumeration.Panel;

    /// <summary>
    /// Main page of the RemoteLedMatrix application.  Contains the actual controls for the led matrix, as well as the settings bar and instance.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //Size of the LED matrix
        private static readonly int LEDMatrixWidth = 48;
        private static readonly int LEDMatrixHeight = 48;

        // Rotation metadata to apply to the preview stream (MF_MT_VIDEO_ROTATION)
        // Reference: http://msdn.microsoft.com/en-us/library/windows/apps/xaml/hh868174.aspx
        private static readonly Guid RotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");

        public static MainPage Instance = null;

        private static WriteableBitmap tempWriteableBitmap;

        private readonly CoreDispatcher dispatcher;

        public bool IsInSettings = false;
        public Connection currentConnection;
        private DispatcherTimer timeout;
        private DispatcherTimer captureTimer;
        private DisplayRequest keepScreenOnRequest;

        #region Camera variables
        // Receive notifications about rotation of the UI and apply any necessary rotation to the preview stream
        private readonly DisplayInformation _displayInformation = DisplayInformation.GetForCurrentView();
        private DisplayOrientations _displayOrientation = DisplayOrientations.Portrait;

        // Prevent the screen from sleeping while the camera is running
        private readonly DisplayRequest _displayRequest = new DisplayRequest();

        // MediaCapture and its state variables
        private MediaCapture _mediaCapture;
        private bool _isInitialized = false;
        public bool _isPreviewing = false;

        // Information about the camera device
        private bool _mirroringPreview = false;
        private bool _externalCamera = false;
        #endregion Camera variables

        #region Constructor, lifecycle and navigation

        public MainPage()
        {
            Instance = this;

            App.LedMatrix = new Lpd8806Matrix(LEDMatrixWidth, LEDMatrixHeight);

            this.InitializeComponent();

            // Cache the UI to have the checkboxes retain their state, as the enabled/disabled state of the
            // GetPreviewFrameButton is reset in code when suspending/navigating (see Start/StopPreviewAsync)
            this.NavigationCacheMode = NavigationCacheMode.Required;

            this.dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            App.CurrentAppSettings = (AppSettings)Application.Current.Resources["CurrentAppSettings"];

            // Useful to know when to initialize/clean up the camera
            Application.Current.Suspending += this.Application_Suspending;
            Application.Current.Resuming += this.Application_Resuming;

            this.PreviewButton.Checked += this.PreviewButton_Checked;
            this.PreviewButton.Unchecked += this.PreviewButton_Unchecked;

            IAsyncAction action = this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    this.Frame.Navigate(typeof(SettingsPage));
                });
        }

        private void PreviewButton_Unchecked(object sender, RoutedEventArgs e)
        {
            this.CaptureButton.IsEnabled = false;
        }

        private void PreviewButton_Checked(object sender, RoutedEventArgs e)
        {
            this.CaptureButton.IsEnabled = true;
        }

        private async void Application_Suspending(object sender, SuspendingEventArgs e)
        {
            // Handle global application events only if this page is active
            if (this.Frame.CurrentSourcePageType == typeof(MainPage))
            {
                SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();

                await this.CleanupCameraAsync();

                this._displayInformation.OrientationChanged -= this.DisplayInformation_OrientationChanged;

                deferral.Complete();
            }
        }

        private async void Application_Resuming(object sender, object o)
        {
            // Handle global application events only if this page is active
            if (this.Frame.CurrentSourcePageType == typeof(MainPage))
            {
                // Populate orientation variables with the current state and register for future changes
                this._displayOrientation = this._displayInformation.CurrentOrientation;
                this._displayInformation.OrientationChanged += this.DisplayInformation_OrientationChanged;

                await this.InitializeCameraAsync();
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            // Populate orientation variables with the current state and register for future changes
            this._displayOrientation = this._displayInformation.CurrentOrientation;
            this._displayInformation.OrientationChanged += this.DisplayInformation_OrientationChanged;

            await this.InitializeCameraAsync();
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            // Handling of this event is included for completenes, as it will only fire when navigating between pages and this sample only includes one page
            await this.CleanupCameraAsync();

            this._displayInformation.OrientationChanged -= this.DisplayInformation_OrientationChanged;
        }

        #endregion Constructor, lifecycle and navigation

        #region MediaCapture methods

        /// <summary>
        /// Initializes the MediaCapture, registers events, gets camera device information for mirroring and rotating, and starts preview
        /// </summary>
        /// <returns>Task representing the async event status</returns>
        private async Task InitializeCameraAsync()
        {
            Debug.WriteLine("InitializeCameraAsync");

            if (this._mediaCapture == null)
            {
                // Attempt to get the back camera if one is available, but use any camera device if not
                DeviceInformation cameraDevice = await FindCameraDeviceByPanelAsync(Panel.Back);

                if (cameraDevice == null)
                {
                    Debug.WriteLine("No camera device found!");
                    return;
                }

                // Create MediaCapture and its settings
                this._mediaCapture = new MediaCapture();

                // Register for a notification when something goes wrong
                this._mediaCapture.Failed += this.MediaCapture_Failed;

                MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings { VideoDeviceId = cameraDevice.Id };

                // Initialize MediaCapture
                try
                {
                    await this._mediaCapture.InitializeAsync(settings);
                    this._isInitialized = true;
                }
                catch (UnauthorizedAccessException)
                {
                    Debug.WriteLine("The app was denied access to the camera");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception when initializing MediaCapture with {0}: {1}", cameraDevice.Id, ex.ToString());
                }

                // If initialization succeeded, start the preview
                if (this._isInitialized)
                {
                    // Figure out where the camera is located
                    if (cameraDevice.EnclosureLocation == null || cameraDevice.EnclosureLocation.Panel == Panel.Unknown)
                    {
                        // No information on the location of the camera, assume it's an external camera, not integrated on the device
                        this._externalCamera = true;
                    }
                    else
                    {
                        // Camera is fixed on the device
                        this._externalCamera = false;

                        // Only mirror the preview if the camera is on the front panel
                        this._mirroringPreview = cameraDevice.EnclosureLocation.Panel == Panel.Front;
                    }

                    await this.StartPreviewAsync();
                }
            }
        }

        /// <summary>
        /// Starts the preview and adjusts it for for rotation and mirroring after making a request to keep the screen on and unlocks the UI
        /// </summary>
        /// <returns>Task representing the async event status</returns>
        private async Task StartPreviewAsync()
        {
            Debug.WriteLine("StartPreviewAsync");

            // Prevent the device from sleeping while the preview is running
            this._displayRequest.RequestActive();

            // Set the preview source in the UI and mirror it if necessary
            this.PreviewControl.Source = this._mediaCapture;
            this.PreviewControl.FlowDirection = this._mirroringPreview ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

            // Start the preview
            try
            {
                await this._mediaCapture.StartPreviewAsync();
                this._isPreviewing = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception when starting the preview: {0}", ex.ToString());
            }

            // Initialize the preview to the current orientation
            if (this._isPreviewing)
            {
                await this.SetPreviewRotationAsync();
            }

            // Enable / disable the button depending on the preview state
            this.PreviewButton.IsChecked = this._isPreviewing;
        }

        /// <summary>
        /// Gets the current orientation of the UI in relation to the device and applies a corrective rotation to the preview
        /// </summary>
        /// <returns>Task representing the async event status</returns>
        private async Task SetPreviewRotationAsync()
        {
            // Only need to update the orientation if the camera is mounted on the device
            if (this._externalCamera) return;

            // Calculate which way and how far to rotate the preview
            int rotationDegrees = ConvertDisplayOrientationToDegrees(this._displayOrientation);

            // The rotation direction needs to be inverted if the preview is being mirrored
            if (this._mirroringPreview)
            {
                rotationDegrees = (360 - rotationDegrees) % 360;
            }

            // Add rotation metadata to the preview stream to make sure the aspect ratio / dimensions match when rendering and getting preview frames
            IMediaEncodingProperties props = this._mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
            props.Properties.Add(RotationKey, rotationDegrees);
            await this._mediaCapture.SetEncodingPropertiesAsync(MediaStreamType.VideoPreview, props, null);
        }

        /// <summary>
        /// Stops the preview and deactivates a display request, to allow the screen to go into power saving modes, and locks the UI
        /// </summary>
        /// <returns>Task representing the async event status</returns>
        private async Task StopPreviewAsync()
        {
            try
            {
                this._isPreviewing = false;
                await this._mediaCapture.StopPreviewAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception when stopping the preview: {0}", ex.ToString());
            }

            // Use the dispatcher because this method is sometimes called from non-UI threads
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                this.PreviewControl.Source = null;

                // Allow the device to sleep now that the preview is stopped
                this._displayRequest.RequestRelease();

                this.PreviewButton.IsChecked = this._isPreviewing;
            });
        }

        /// <summary>
        /// Gets the current preview frame as a SoftwareBitmap, displays its properties in a TextBlock, and can optionally display the image
        /// in the UI and/or save it to disk as a jpg
        /// </summary>
        /// <returns>Task representing the async event status</returns>
        private async Task GetPreviewFrameAsSoftwareBitmapAsync()
        {
            // Get information about the preview
            VideoEncodingProperties previewProperties = this._mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;

            // Create the video frame to request a SoftwareBitmap preview frame
            VideoFrame videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, (int)previewProperties.Width, (int)previewProperties.Height);

            // Capture the preview frame
            using (VideoFrame currentFrame = await this._mediaCapture.GetPreviewFrameAsync(videoFrame))
            {
                // Collect the resulting frame
                SoftwareBitmap previewFrame = currentFrame.SoftwareBitmap;

                // Show the frame information
                Debug.WriteLine("{0}x{1} {2}", previewFrame.PixelWidth, previewFrame.PixelHeight, previewFrame.BitmapPixelFormat);

                tempWriteableBitmap = new WriteableBitmap(previewFrame.PixelWidth, previewFrame.PixelHeight);
                previewFrame.CopyToBuffer(tempWriteableBitmap.PixelBuffer);

                // Crop to a square, based on the smallest side
                int minEdge = Math.Min(tempWriteableBitmap.PixelWidth, tempWriteableBitmap.PixelHeight);

                tempWriteableBitmap = tempWriteableBitmap
                    .Crop(0, 0, minEdge, minEdge)
                    .Resize(App.LedMatrix.PixelWidth, App.LedMatrix.PixelHeight, WriteableBitmapExtensions.Interpolation.Bilinear);

                WriteableBitmap previewFrameImageSource =
                    tempWriteableBitmap.Rotate(90).Resize(
                        (int)this.postViewbox.Height,
                        (int)this.postViewbox.Width,
                        WriteableBitmapExtensions.Interpolation.NearestNeighbor);

                this.previewFrameImage.Source = previewFrameImageSource;
            }
        }

        /// <summary>
        /// Cleans up the camera resources (after stopping the preview if necessary) and unregisters from MediaCapture events
        /// </summary>
        /// <returns>Task representing the async event status</returns>
        private async Task CleanupCameraAsync()
        {
            if (this._isInitialized)
            {
                if (this._isPreviewing)
                {
                    // The call to stop the preview is included here for completeness, but can be
                    // safely removed if a call to MediaCapture.Dispose() is being made later,
                    // as the preview will be automatically stopped at that point
                    await this.StopPreviewAsync();
                }

                this._isInitialized = false;
            }

            if (this._mediaCapture != null)
            {
                this._mediaCapture.Failed -= this.MediaCapture_Failed;
                this._mediaCapture.Dispose();
                this._mediaCapture = null;
            }
        }

        #endregion MediaCapture methods

        #region Helper functions

        /// <summary>
        /// Queries the available video capture devices to try and find one mounted on the desired panel
        /// </summary>
        /// <param name="desiredPanel">The panel on the device that the desired camera is mounted on</param>
        /// <returns>A DeviceInformation instance with a reference to the camera mounted on the desired panel if available,
        ///          any other camera if not, or null if no camera is available.</returns>
        private static async Task<DeviceInformation> FindCameraDeviceByPanelAsync(Panel desiredPanel)
        {
            // Get available devices for capturing pictures
            DeviceInformationCollection allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            // Get the desired camera by panel
            DeviceInformation desiredDevice = allVideoDevices.FirstOrDefault(x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == desiredPanel);

            // If there is no device mounted on the desired panel, return the first device found
            return desiredDevice ?? allVideoDevices.FirstOrDefault();
        }

        /// <summary>
        /// Converts the given orientation of the app on the screen to the corresponding rotation in degrees
        /// </summary>
        /// <param name="orientation">The orientation of the app on the screen</param>
        /// <returns>An orientation in degrees</returns>
        private static int ConvertDisplayOrientationToDegrees(DisplayOrientations orientation)
        {
            switch (orientation)
            {
                case DisplayOrientations.Portrait:
                    return 90;
                case DisplayOrientations.LandscapeFlipped:
                    return 180;
                case DisplayOrientations.PortraitFlipped:
                    return 270;
                case DisplayOrientations.Landscape:
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Populates the list of connections in the settings UI
        /// </summary>
        public async void PopulateList()
        {
            Connections list;

            try
            {
                list = await this.RefreshConnections();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return;
            }

            if (!list.Any())
            {
                App.CurrentAppSettings.ConnectionList.Clear();
                return;
            }

            App.CurrentAppSettings.ConnectionList = list;
        }

        /// <summary>
        /// Refreshes the connections from the available device sources
        /// </summary>
        /// <returns>Collection of connection objects available to the app</returns>
        public async Task<Connections> RefreshConnections()
        {
            Connections connections = new Connections();

            connections.Clear();

            await BluetoothSerial.listAvailableDevicesAsync().AsTask().ContinueWith(
                listTask =>
                {
                    listTask.Result.ForEach(
                        d => connections.Add(new Connection(d.Name, d, ConnectionType.BluetoothSerial)));
                });

            await UsbSerial.listAvailableDevicesAsync().AsTask().ContinueWith(
                listTask =>
                {
                    listTask.Result.ForEach(
                        d => connections.Add(new Connection(d.Name, d, ConnectionType.UsbSerial)));
                });

            string previousConnection = App.CurrentAppSettings.PreviousConnectionName;

            if (this.currentConnection == null && !string.IsNullOrEmpty(previousConnection) &&
                connections.Any(c => c.DisplayName == App.CurrentAppSettings.PreviousConnectionName))
            {
                await this.Connect(
                    connections.FirstOrDefault(
                        c => c.DisplayName == App.CurrentAppSettings.PreviousConnectionName));
            }

            return connections;
        }

        /// <summary>
        /// Disconnects the current connection and cleans up any related objects
        /// </summary>
        public async void Disconnect()
        {
            if (this.currentConnection != null)
            {
                await this.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    App.CurrentAppSettings.CurrentConnectionState = (int)ConnectionState.Disconnecting;
                });

                if (App.SerialStream != null)
                {
                    App.SerialStream.end();
                }

                App.SerialStream = null;
                App.Firmata = null;
                App.Arduino = null;

                this.currentConnection = null;

                await this.dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () =>
                    {
                        App.CurrentAppSettings.CurrentConnectionState = (int)ConnectionState.NotConnected;
                    });
            }
        }

        /// <summary>
        /// Tries to keep the machine from locking/sleeping by periodically making a DisplayRequest
        /// </summary>
        /// <param name="force">Forces the request, even if AlwaysRunning isn't set</param>
        public void CheckAlwaysRunning(bool? force = null)
        {
            if ((force.HasValue && force.Value) || App.CurrentAppSettings.AlwaysRunning)
            {
                this.keepScreenOnRequest = new DisplayRequest();
                this.keepScreenOnRequest.RequestActive();
            }
            else if (this.keepScreenOnRequest != null)
            {
                this.keepScreenOnRequest.RequestRelease();
                this.keepScreenOnRequest = null;
            }
        }

        /// <summary>
        /// Connects to a remote device over firmata
        /// </summary>
        /// <param name="selectedConnection">Connection to connect to </param>
        /// <returns>true if connection succeeded.</returns>
        public async Task Connect(Connection selectedConnection)
        {
            bool result = false;
            if (this.currentConnection != null)
            {
                App.SerialStream.end();
                this.Disconnect();
                await Task.Delay(1000);
            }

            try
            {
                await this.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    App.CurrentAppSettings.CurrentConnectionState = (int)ConnectionState.Connecting;
                });

                this.currentConnection = selectedConnection;

                switch (this.currentConnection.ConnectionType)
                {
                    case ConnectionType.BluetoothSerial:
                        App.SerialStream = new BluetoothSerial(selectedConnection.Source);
                        break;
                    case ConnectionType.UsbSerial:
                        App.SerialStream = new UsbSerial(selectedConnection.Source);
                        break;
                }

                App.Firmata = new UwpFirmata();
                App.Firmata.FirmataConnectionFailed += this.OnConnectionFailed;
                App.Firmata.FirmataConnectionLost += this.OnConnectionLost;
                App.Firmata.FirmataConnectionReady += this.OnConnectionEstablished;
                App.SerialStream.begin(115200, SerialConfig.SERIAL_8N1);
                App.Firmata.begin(App.SerialStream);
                App.Firmata.startListening();
                App.Arduino = new RemoteDevice(App.Firmata);
            }
            catch (Exception e)
            {
                await this.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    App.CurrentAppSettings.CurrentConnectionState = (int)ConnectionState.CouldNotConnect;
                    Debug.WriteLine("{0} hit while connecting", e.StackTrace);
                });
            }
        }

        #endregion Helper functions

        #region Event handlers

        /// <summary>
        /// This event will fire when the page is rotated
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="args">The event data.</param>
        private async void DisplayInformation_OrientationChanged(DisplayInformation sender, object args)
        {
            this._displayOrientation = sender.CurrentOrientation;

            if (this._isPreviewing)
            {
                await this.SetPreviewRotationAsync();
            }
        }

        private async void GetPreviewFrameButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // If preview is not running, no preview frames can be acquired
            if (!this._isPreviewing)
            {
                return;
            }

            await this.GetPreviewFrameAsSoftwareBitmapAsync();

            this.DisplayButton.IsEnabled = true;
        }

        private async void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            Debug.WriteLine("MediaCapture_Failed: (0x{0:X}) {1}", errorEventArgs.Code, errorEventArgs.Message);

            await this.CleanupCameraAsync();

            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => this.PreviewButton.IsChecked = this._isPreviewing);
        }

        /// <summary>
        /// Handler for clicking the setting button
        /// </summary>
        /// <param name="sender">Control being clicked</param>
        /// <param name="e">Click event</param>
        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingsPage));
        }

        /// <summary>
        /// Handler for clicking the preview button
        /// </summary>
        /// <param name="sender">Control being clicked</param>
        /// <param name="e">Click event</param>
        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (this._isPreviewing)
            {
                this.CleanupCameraAsync();
            }
            else
            {
                this.InitializeCameraAsync();
            }
        }

        /// <summary>
        /// Handler for clicking the test button
        /// </summary>
        /// <param name="sender">Control being clicked</param>
        /// <param name="e">Click event</param>
        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.TestButton.IsChecked.Value)
            {
                this.captureTimer = new DispatcherTimer();
                this.captureTimer.Interval = new TimeSpan(0, 0, 0, 1);
                this.captureTimer.Tick += this.captureTimerTick;
                this.captureTimer.Start();
            }
            else
            {
                this.captureTimer.Stop();
            }
        }

        /// <summary>
        /// Performs a capture every timer tick, to provide continuous streaming
        /// </summary>
        /// <param name="sender">Timer object sending the tick event</param>
        /// <param name="e">Tick event</param>
        private void captureTimerTick(object sender, object e)
        {
            ((DispatcherTimer)sender).Stop();
            IAsyncAction action = this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                async () =>
                {
                    await this.GetPreviewFrameAsSoftwareBitmapAsync();
                    await Task.Delay(10);
                    await App.LedMatrix.DisplayImage(tempWriteableBitmap);
                    ((DispatcherTimer)sender).Start();
                });
        }

        /// <summary>
        /// Handler for lost connection event, logs and displays a UI message
        /// </summary>
        /// <param name="message">Message of why the connection was lost</param>
        private void OnConnectionLost(string message)
        {
            IAsyncAction action = this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    App.CurrentAppSettings.CurrentConnectionState = (int)ConnectionState.NotConnected;
                    this.Frame.Navigate(typeof(SettingsPage));
                });

            Debug.WriteLine(string.Format("Connection lost!  '{0}'", message));
        }

        /// <summary>
        /// Handler for failed connection event, logs and displays a UI message
        /// </summary>
        /// <param name="message">Failure detail message</param>
        private void OnConnectionFailed(string message)
        {
            Debug.WriteLine(string.Format("Connection failed!  '{0}'", message));
            IAsyncAction action = this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    App.CurrentAppSettings.CurrentConnectionState = (int)ConnectionState.CouldNotConnect;
                });
        }

        /// <summary>
        /// 
        /// </summary>
        private void OnConnectionEstablished()
        {
            Debug.WriteLine("Connection established!");

            IAsyncAction action = this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    App.CurrentAppSettings.CurrentConnectionState = (int)ConnectionState.Connected;
                    this.Frame.Navigate(typeof(MainPage));

                    App.LedMatrix.Initialize();
                    App.CurrentAppSettings.PreviousConnectionName = this.currentConnection.DisplayName;
                });
        }

        /// <summary>
        /// Display button tapped event handler
        /// </summary>
        /// <param name="sender">Control sending the event</param>
        /// <param name="e">Tap event</param>
        private async void Display_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (null != tempWriteableBitmap)
            {
                await App.LedMatrix.DisplayImage(tempWriteableBitmap);
            }
        }

        #endregion Event handlers
    }
}
