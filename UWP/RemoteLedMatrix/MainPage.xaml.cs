﻿using RemoteLedMatrix.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Devices.Spi;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Diagnostics;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Microsoft.Maker.Firmata;
using Microsoft.Maker.RemoteWiring;
using Microsoft.Maker.Serial;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RemoteLedMatrix
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static MainPage Instance = null;

        private static WriteableBitmap tempWriteableBitmap;

        //private AppSettings appSettings = null;
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

        // Rotation metadata to apply to the preview stream (MF_MT_VIDEO_ROTATION)
        // Reference: http://msdn.microsoft.com/en-us/library/windows/apps/xaml/hh868174.aspx
        private static readonly Guid RotationKey = new Guid("C380465D-2271-428C-9B83-ECEA3B4A85C1");

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
            MainPage.Instance = this;

            //App.LedMatrix = new Lpd8806Matrix(48, 48);
            App.LedMatrix = new LedPanel(32, 32);

            this.InitializeComponent();

            // Cache the UI to have the checkboxes retain their state, as the enabled/disabled state of the
            // GetPreviewFrameButton is reset in code when suspending/navigating (see Start/StopPreviewAsync)
            this.NavigationCacheMode = NavigationCacheMode.Required;

            this.dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            App.CurrentAppSettings = (AppSettings)App.Current.Resources["CurrentAppSettings"];

            // Useful to know when to initialize/clean up the camera
            Application.Current.Suspending += Application_Suspending;
            Application.Current.Resuming += Application_Resuming;

            PreviewButton.Checked += PreviewButton_Checked;
            PreviewButton.Unchecked += PreviewButton_Unchecked;

            IAsyncAction action = this.Dispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Normal,
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
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();

                await CleanupCameraAsync();

                _displayInformation.OrientationChanged -= DisplayInformation_OrientationChanged;

                deferral.Complete();
            }
        }

        private async void Application_Resuming(object sender, object o)
        {
            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType == typeof(MainPage))
            {
                // Populate orientation variables with the current state and register for future changes
                _displayOrientation = _displayInformation.CurrentOrientation;
                _displayInformation.OrientationChanged += DisplayInformation_OrientationChanged;

                await InitializeCameraAsync();
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            // Populate orientation variables with the current state and register for future changes
            _displayOrientation = _displayInformation.CurrentOrientation;
            _displayInformation.OrientationChanged += DisplayInformation_OrientationChanged;

            await InitializeCameraAsync();
        }

        protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            // Handling of this event is included for completenes, as it will only fire when navigating between pages and this sample only includes one page

            await CleanupCameraAsync();

            _displayInformation.OrientationChanged -= DisplayInformation_OrientationChanged;
        }

        #endregion Constructor, lifecycle and navigation

        #region Event handlers

        /// <summary>
        /// This event will fire when the page is rotated
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="args">The event data.</param>
        private async void DisplayInformation_OrientationChanged(DisplayInformation sender, object args)
        {
            _displayOrientation = sender.CurrentOrientation;

            if (_isPreviewing)
            {
                await SetPreviewRotationAsync();
            }
        }

        private async void GetPreviewFrameButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // If preview is not running, no preview frames can be acquired
            if (!_isPreviewing) return;

            //if ((ShowFrameCheckBox.IsChecked == true) || (SaveFrameCheckBox.IsChecked == true))
            //{
                await GetPreviewFrameAsSoftwareBitmapAsync();

            this.DisplayButton.IsEnabled = true;
            //}
            //else
            //{
            //    await GetPreviewFrameAsD3DSurfaceAsync();
            //}
        }

        private async void MediaCapture_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            Debug.WriteLine("MediaCapture_Failed: (0x{0:X}) {1}", errorEventArgs.Code, errorEventArgs.Message);

            await CleanupCameraAsync();

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => this.PreviewButton.IsChecked = _isPreviewing);
        }

        #endregion Event handlers

        #region MediaCapture methods

        /// <summary>
        /// Initializes the MediaCapture, registers events, gets camera device information for mirroring and rotating, and starts preview
        /// </summary>
        /// <returns></returns>
        private async Task InitializeCameraAsync()
        {
            Debug.WriteLine("InitializeCameraAsync");

            if (_mediaCapture == null)
            {
                // Attempt to get the back camera if one is available, but use any camera device if not
                DeviceInformation cameraDevice = await FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel.Back);

                if (cameraDevice == null)
                {
                    Debug.WriteLine("No camera device found!");
                    return;
                }

                // Create MediaCapture and its settings
                _mediaCapture = new MediaCapture();

                // Register for a notification when something goes wrong
                _mediaCapture.Failed += MediaCapture_Failed;

                MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings { VideoDeviceId = cameraDevice.Id };

                // Initialize MediaCapture
                try
                {
                    await _mediaCapture.InitializeAsync(settings);
                    _isInitialized = true;
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
                if (_isInitialized)
                {
                    // Figure out where the camera is located
                    if (cameraDevice.EnclosureLocation == null || cameraDevice.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Unknown)
                    {
                        // No information on the location of the camera, assume it's an external camera, not integrated on the device
                        _externalCamera = true;
                    }
                    else
                    {
                        // Camera is fixed on the device
                        _externalCamera = false;

                        // Only mirror the preview if the camera is on the front panel
                        _mirroringPreview = (cameraDevice.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Front);
                    }

                    await StartPreviewAsync();
                }
            }
        }

        /// <summary>
        /// Starts the preview and adjusts it for for rotation and mirroring after making a request to keep the screen on and unlocks the UI
        /// </summary>
        /// <returns></returns>
        private async Task StartPreviewAsync()
        {
            Debug.WriteLine("StartPreviewAsync");

            // Prevent the device from sleeping while the preview is running
            _displayRequest.RequestActive();

            // Set the preview source in the UI and mirror it if necessary
            PreviewControl.Source = _mediaCapture;
            PreviewControl.FlowDirection = _mirroringPreview ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

            // Start the preview
            try
            {
                await _mediaCapture.StartPreviewAsync();
                _isPreviewing = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception when starting the preview: {0}", ex.ToString());
            }

            // Initialize the preview to the current orientation
            if (_isPreviewing)
            {
                await SetPreviewRotationAsync();
            }

            // Enable / disable the button depending on the preview state
            this.PreviewButton.IsChecked = _isPreviewing;
        }

        /// <summary>
        /// Gets the current orientation of the UI in relation to the device and applies a corrective rotation to the preview
        /// </summary>
        private async Task SetPreviewRotationAsync()
        {
            // Only need to update the orientation if the camera is mounted on the device
            if (_externalCamera) return;

            // Calculate which way and how far to rotate the preview
            int rotationDegrees = ConvertDisplayOrientationToDegrees(_displayOrientation);

            // The rotation direction needs to be inverted if the preview is being mirrored
            if (_mirroringPreview)
            {
                rotationDegrees = (360 - rotationDegrees) % 360;
            }

            // Add rotation metadata to the preview stream to make sure the aspect ratio / dimensions match when rendering and getting preview frames
            IMediaEncodingProperties props = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
            props.Properties.Add(RotationKey, rotationDegrees);
            await _mediaCapture.SetEncodingPropertiesAsync(MediaStreamType.VideoPreview, props, null);
        }

        /// <summary>
        /// Stops the preview and deactivates a display request, to allow the screen to go into power saving modes, and locks the UI
        /// </summary>
        /// <returns></returns>
        private async Task StopPreviewAsync()
        {
            try
            {
                _isPreviewing = false;
                await _mediaCapture.StopPreviewAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception when stopping the preview: {0}", ex.ToString());
            }
            
            // Use the dispatcher because this method is sometimes called from non-UI threads
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PreviewControl.Source = null;

                // Allow the device to sleep now that the preview is stopped
                _displayRequest.RequestRelease();

                this.PreviewButton.IsChecked = _isPreviewing;
            });
        }

        /// <summary>
        /// Gets the current preview frame as a SoftwareBitmap, displays its properties in a TextBlock, and can optionally display the image
        /// in the UI and/or save it to disk as a jpg
        /// </summary>
        /// <returns></returns>
        private async Task GetPreviewFrameAsSoftwareBitmapAsync()
        {
            // Get information about the preview
            VideoEncodingProperties previewProperties = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;

            // Create the video frame to request a SoftwareBitmap preview frame
            VideoFrame videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, (int)previewProperties.Width, (int)previewProperties.Height);

            // Capture the preview frame
            using (VideoFrame currentFrame = await _mediaCapture.GetPreviewFrameAsync(videoFrame))
            {
                // Collect the resulting frame
                SoftwareBitmap previewFrame = currentFrame.SoftwareBitmap;

                // Show the frame information
                Debug.WriteLine("{0}x{1} {2}", previewFrame.PixelWidth, previewFrame.PixelHeight, previewFrame.BitmapPixelFormat);

                tempWriteableBitmap = new WriteableBitmap(previewFrame.PixelWidth, previewFrame.PixelHeight);
                previewFrame.CopyToBuffer(tempWriteableBitmap.PixelBuffer);

                int minEdge = Math.Min(tempWriteableBitmap.PixelWidth, tempWriteableBitmap.PixelHeight);

                tempWriteableBitmap = tempWriteableBitmap
                    .Crop(0, 0, minEdge, minEdge)
                    .Resize(App.LedMatrix.PixelWidth, App.LedMatrix.PixelHeight, WriteableBitmapExtensions.Interpolation.Bilinear);

                WriteableBitmap previewFrameImageSource =
                    tempWriteableBitmap.Resize(
                        (int) this.postViewbox.Height,
                        (int) this.postViewbox.Width,
                        WriteableBitmapExtensions.Interpolation.NearestNeighbor);

                this.previewFrameImage.Source = previewFrameImageSource;
            }
        }

        /// <summary>
        /// Cleans up the camera resources (after stopping the preview if necessary) and unregisters from MediaCapture events
        /// </summary>
        /// <returns></returns>
        private async Task CleanupCameraAsync()
        {
            if (_isInitialized)
            {
                if (_isPreviewing)
                {
                    // The call to stop the preview is included here for completeness, but can be
                    // safely removed if a call to MediaCapture.Dispose() is being made later,
                    // as the preview will be automatically stopped at that point
                    await StopPreviewAsync();
                }

                _isInitialized = false;
            }

            if (_mediaCapture != null)
            {
                _mediaCapture.Failed -= MediaCapture_Failed;
                _mediaCapture.Dispose();
                _mediaCapture = null;
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
        private static async Task<DeviceInformation> FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel desiredPanel)
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
        /// Saves a SoftwareBitmap to the Pictures library with the specified name
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        private static async Task SaveSoftwareBitmapAsync(SoftwareBitmap bitmap)
        {
            StorageFile file = await KnownFolders.PicturesLibrary.CreateFileAsync("PreviewFrame.jpg", CreationCollisionOption.GenerateUniqueName);
            using (IRandomAccessStream outputStream = await file.OpenAsync(FileAccessMode.ReadWrite))
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, outputStream);

                // Grab the data from the SoftwareBitmap
                encoder.SetSoftwareBitmap(bitmap);
                await encoder.FlushAsync();
            }
        }

        /// <summary>
        /// Applies a basic effect to a Bgra8 SoftwareBitmap in-place
        /// </summary>
        /// <param name="bitmap">SoftwareBitmap that will receive the effect</param>
        private unsafe void ApplyGreenFilter(SoftwareBitmap bitmap)
        {
            // Effect is hard-coded to operate on BGRA8 format only
            if (bitmap.BitmapPixelFormat == BitmapPixelFormat.Bgra8)
            {
                // In BGRA8 format, each pixel is defined by 4 bytes
                const int BYTES_PER_PIXEL = 4;

                using (BitmapBuffer buffer = bitmap.LockBuffer(BitmapBufferAccessMode.ReadWrite))
                {
                    using (IMemoryBufferReference reference = buffer.CreateReference())
                    {
                        if (reference is IMemoryBufferByteAccess)
                        {
                            // Get a pointer to the pixel buffer
                            byte* data;
                            uint capacity;
                            ((IMemoryBufferByteAccess) reference).GetBuffer(out data, out capacity);

                            // Get information about the BitmapBuffer
                            BitmapPlaneDescription desc = buffer.GetPlaneDescription(0);

                            // Iterate over all pixels
                            for (uint row = 0; row < desc.Height; row++)
                            {
                                for (uint col = 0; col < desc.Width; col++)
                                {
                                    // Index of the current pixel in the buffer (defined by the next 4 bytes, BGRA8)
                                    long currPixel = desc.StartIndex + desc.Stride*row + BYTES_PER_PIXEL*col;

                                    // Read the current pixel information into b,g,r channels (leave out alpha channel)
                                    byte b = data[currPixel + 0]; // Blue
                                    byte g = data[currPixel + 1]; // Green
                                    byte r = data[currPixel + 2]; // Red

                                    // Boost the green channel, leave the other two untouched
                                    data[currPixel + 0] = b;
                                    data[currPixel + 1] = (byte) Math.Min(g + 80, 255);
                                    data[currPixel + 2] = r;
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion Helper functions 

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SettingsPage));
        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isPreviewing)
            {
                CleanupCameraAsync();
            }
            else
            {
                InitializeCameraAsync();
            }
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            if (TestButton.IsChecked.Value)
            {
                this.captureTimer = new DispatcherTimer();
                this.captureTimer.Interval = new TimeSpan(0, 0, 0, 1);
                this.captureTimer.Tick += captureTimerTick;
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
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        public async void PopulateList()
        {
            Connections list;

            try
            {
                list = await this.RefreshConnections();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return;
            }

            if (!list.Any())
            {
                App.CurrentAppSettings.ConnectionList.Clear();
                return;
            }

            App.CurrentAppSettings.ConnectionList = list;
        }

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

            if (!string.IsNullOrEmpty(previousConnection) &&
                connections.Any(c => c.DisplayName == App.CurrentAppSettings.PreviousConnectionName))
            {
                await this.Connect(
                    connections.FirstOrDefault(
                        c => c.DisplayName == App.CurrentAppSettings.PreviousConnectionName));
            }

            return connections;
        }

        public async void Disconnect()
        {
            if (currentConnection != null)
            {
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    App.CurrentAppSettings.CurrentConnectionState = (int)ConnectionState.Disconnecting;
                });

                App.SerialStream.end();
                App.Firmata.finish();
                App.Arduino.Dispose();
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
        /// <param name="selectedConnection"></param>
        /// <returns></returns>
        public async Task<bool> Connect(Connection selectedConnection)
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
                        BluetoothSerial bluetooth = new BluetoothSerial(selectedConnection.Source as DeviceInformation);
                        bluetooth.ConnectionEstablished += this.OnConnectionEstablished;
                        bluetooth.ConnectionFailed += this.OnConnectionFailed;
                        bluetooth.begin(115200, 0);

                        App.SerialStream = bluetooth;
                        break;
                    case ConnectionType.UsbSerial:
                        UsbSerial usbSerial = new UsbSerial(selectedConnection.Source as DeviceInformation);
                        usbSerial.ConnectionEstablished += this.OnConnectionEstablished;
                        usbSerial.ConnectionFailed += this.OnConnectionFailed;
                        usbSerial.ConnectionLost += this.OnConnectionLost;
                        usbSerial.begin(115200, SerialConfig.SERIAL_8N1);

                        App.SerialStream = usbSerial;
                        break;
                }

                App.Firmata = new UwpFirmata();
                App.Arduino = new RemoteDevice(App.Firmata);
                App.Firmata.begin(App.SerialStream);
                App.Firmata.startListening();

                //start a timer for connection timeout
                this.timeout = new DispatcherTimer();
                this.timeout.Interval = new TimeSpan(0, 0, 30);
                this.timeout.Tick += Connection_TimeOut;
                this.timeout.Start();

                result = true;
            }
            catch (Exception e)
            {
                await this.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    App.CurrentAppSettings.CurrentConnectionState = (int)ConnectionState.CouldNotConnect;
                    Debug.WriteLine("{0} hit while connecting", e.StackTrace);
                });
            }

            return result;
        }

        private void OnConnectionLost()
        {
            IAsyncAction action = this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    App.CurrentAppSettings.CurrentConnectionState = (int) ConnectionState.NotConnected;
                    this.Frame.Navigate(typeof(SettingsPage));
                });

            Debug.WriteLine("Connection lost!");

        }

        private void OnConnectionFailed(string message)
        {
            this.timeout.Stop();
            IAsyncAction action = this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, 
                () =>
                {
                    App.CurrentAppSettings.CurrentConnectionState = (int)ConnectionState.CouldNotConnect;
                });
        }

        private void OnConnectionEstablished()
        {
            this.timeout.Stop();

            App.LedMatrix.Initialize();
            App.CurrentAppSettings.PreviousConnectionName = this.currentConnection.DisplayName;

            IAsyncAction action = this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, 
                () =>
                {
                    this.Frame.Navigate(typeof(MainPage));
                });
        }

        private void Connection_TimeOut(object sender, object e)
        {
            IAsyncAction action = this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, 
                () =>
                {
                    App.CurrentAppSettings.CurrentConnectionState = (int)ConnectionState.CouldNotConnect;
                });
        }

        private async void Display_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (null != tempWriteableBitmap)
            {
                await App.LedMatrix.DisplayImage(tempWriteableBitmap);
            }
        }
    }
}
