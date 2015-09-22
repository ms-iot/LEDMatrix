// Copyright (c) Microsoft. All rights reserved.

namespace RemoteLedMatrix.Hardware
{
    using System.Threading.Tasks;
    using Windows.UI.Xaml.Media.Imaging;

    /// <summary>
    /// Interface for dealing with different LED matrix hardware types
    /// </summary>
    public interface ILedMatrix
    {
        /// <summary>
        /// Gets a value representing the number of pixels wide the matrix is
        /// </summary>
        /// <value>
        /// A value representing the number of pixels wide matrix is
        /// </value>
        int PixelWidth { get; }

        /// <summary>
        /// Gets a value representing the number of pixels high the matrix is
        /// </summary>
        /// <value>
        /// A value representing the number of pixels high the matrix is
        /// </value>
        int PixelHeight { get; }

        /// <summary>
        /// Sends a command to initialize the LED matrix
        /// </summary>
        void Initialize();

        /// <summary>
        /// Displays an image on the LED matrix
        /// </summary>
        /// <param name="image">Bitmap to display on the LED matrix</param>
        /// <returns>Task for tracking the status of the async call</returns>
        Task DisplayImage(WriteableBitmap image);
    }
}
