// Copyright (c) Microsoft. All rights reserved.

namespace RemoteLedMatrix
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Windows.UI;
    using Windows.UI.Xaml.Media.Imaging;
    using RemoteLedMatrix.Hardware;
    using RemoteLedMatrix.Helpers;

    /// <summary>
    /// Implementation of <see cref="ILedMatrix"/> for the AdaFruit 32x32 LED MatrixPanel
    /// </summary>
    public class LedMatrixPanel : ILedMatrix
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LedMatrixPanel"/> class.
        /// </summary>
        /// <param name="width">Width in pixels of the LED Matrix</param>
        /// <param name="height">Height in pixels of the LED Matrix</param>
        public LedMatrixPanel(int width, int height)
        {
            this.PixelWidth = width;
            this.PixelHeight = height;
        }

        /// <summary>
        /// Gets a value representing the number of pixels high the matrix is
        /// </summary>
        /// <value>
        /// A value representing the number of pixels high the matrix is
        /// </value>
        public int PixelHeight { get; }

        /// <summary>
        /// Gets a value representing the number of pixels wide the matrix is
        /// </summary>
        /// <value>
        /// A value representing the number of pixels wide matrix is
        /// </value>
        public int PixelWidth { get; }

        /// <summary>
        /// Sends a command to initialize the LED matrix
        /// </summary>
        /// <remarks>LedMatrixPanel initialization is handled purely on the Arduino side for this
        /// particular hardware.</remarks>
        public void Initialize()
        {
            // No initialization needed.
        }

        /// <summary>
        /// Displays an image on the LED matrix
        /// </summary>
        /// <param name="image">Bitmap to display on the LED matrix</param>
        /// <returns>Task for tracking the status of the async call</returns>
        public async Task DisplayImage(WriteableBitmap image)
        {
            WriteableBitmap resizedBitmap = image.Resize(
                this.PixelHeight,
                this.PixelWidth,
                WriteableBitmapExtensions.Interpolation.Bilinear);

            List<Color> colors = resizedBitmap.ToColorList();

            //colors.ForEach(c => colorsAdjusted.Add(c.ApplyGamma(1.5)));
            //colors.ForEach(c => colorsAdjusted.Add(c.ToPerceptual()));

            IEnumerable<byte> bytes = colors.Get21BitPixelBytes();

            App.Firmata.SendPixelBlob(bytes, 30);
        }
    }
}
