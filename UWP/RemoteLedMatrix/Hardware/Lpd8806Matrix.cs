

namespace RemoteLedMatrix
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Hardware;
    using Helpers;
    using Windows.UI;
    using Windows.UI.Xaml.Media.Imaging;

    /// <summary>
    /// Implementation of <see cref="ILedMatrix"/> for LPD8806/WS2801 based RGB leds, arranged into a
    /// single long serial strand.  The first pixel is in the bottom left hand corner with the strand running
    /// upwards.  The second strand starts at the top and runs downwards. Functionally, this means we need to
    /// flip every other column of color data to display properly.
    /// </summary>
    public class Lpd8806Matrix : ILedMatrix
    {
        /// <summary>
        /// Command to configure SPI communication and do initialization of the LED Matrix on the
        /// Arduino side
        /// </summary>
        public const byte LED_CONFIG = 0x44;

        /// <summary>
        /// Command to reset the reset the addressing on all the pixels in the matrix.  Gets sent once
        /// before a "frame"'s data, and once after.
        /// </summary>
        public const byte LED_RESET = 0x43;

        /// <summary>
        /// Magic weird pixel.  Skip or everything gets offset weird.
        /// </summary>
        public const int MagicPixel = 947;

        /// <summary>
        /// Initializes a new instance of the <see cref="Lpd8806Matrix"/> class.
        /// </summary>
        /// <param name="width">Width in pixels of the LED Matrix</param>
        /// <param name="height">Height in pixels of the LED Matrix</param>
        public Lpd8806Matrix(int width, int height)
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
        public void Initialize()
        {
            App.Firmata.sendSysex(LED_CONFIG);
            App.Firmata.sendSysex(LED_RESET);
        }

        /// <summary>
        /// Displays an image on the LED matrix
        /// </summary>
        /// <param name="image">Bitmap to display on the LED matrix</param>
        /// <returns>Task for tracking the status of the async call</returns>
        public async Task DisplayImage(WriteableBitmap image)
        {
            WriteableBitmap resizedBitmap = image.Resize(
                this.PixelWidth,
                this.PixelHeight,
                WriteableBitmapExtensions.Interpolation.Bilinear);

            List<Color> colors = resizedBitmap.Flip(WriteableBitmapExtensions.FlipMode.Horizontal).ToColorList();

            colors = colors.FlipEvenColumns(this.PixelHeight);

            List<Color> perceptualColors = colors.Select(
                color => color.ToPerceptual().ApplyGamma(1.5)).ToList();

            colors.Clear();
            colors.AddRange(perceptualColors);

            colors.RemoveAt(MagicPixel);

            IEnumerable<byte> bytes = colors.Get21BitPixelBytes();

            App.Firmata.sendSysex(LED_RESET);
            await Task.Delay(1);

            App.Firmata.sendPixelBlob(bytes, 30);
            await Task.Delay(1);

            App.Firmata.sendSysex(LED_RESET);
            await Task.Delay(1);
        }
    }
}
