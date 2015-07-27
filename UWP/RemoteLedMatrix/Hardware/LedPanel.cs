
using Windows.UI.Xaml.Media.Imaging;
using RemoteLedMatrix.Hardware;

namespace RemoteLedMatrix
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Windows.UI;
    using Microsoft.Maker.Firmata;
    using RemoteLedMatrix.Helpers;


    public class LedPanel : ILedMatrix
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
        /// Command telling the arduino to parse the blob data that comes next as monochrome pixel data
        /// </summary>
        public const byte LED_PIXEL1 = 0x46;

        /// <summary>
        /// Command telling the arduino to parse the blob data that comes next as indexed palette pixel data
        /// </summary>
        public const byte LED_PIXEL7 = 0x45;

        /// <summary>
        /// Command telling the arduino to parse the blob data that comes next as palette data for a 7-bit indexed color stream
        /// </summary>
        public const byte LED_PIXEL7_PALETTE = 0x47;

        /// <summary>
        /// Command telling the arduino to parse the blob data that comes next as full 21-bit RGB pixel data
        /// </summary>
        public const byte LED_PIXEL21 = 0x42;

        // Magic weird pixel.  Skip or everything gets offset weird.
        public const int MagicPixel = 947;

        public LedPanel(int width, int height)
        {
            this.PixelWidth = width;
            this.PixelHeight = height;
        }

        public int PixelHeight { get; private set; }
        public int PixelWidth { get; private set; }

        public void Initialize()
        {
            App.Firmata.sendSysex(LED_CONFIG);
            //App.Firmata.sendSysex(LED_RESET);
        }

        public async Task DisplayImage(WriteableBitmap bitmap)
        {
            WriteableBitmap resizedBitmap = bitmap.Resize(
                32, 
                32,
                WriteableBitmapExtensions.Interpolation.Bilinear);

            List<Color> colors = resizedBitmap.GetColorsFromWriteableBitmap();

            //colors = colors.FlipEvenColumns();

            //List<Color> perceptualColors = new List<Color>();

            //foreach (var color in colors)
            //{
            //    perceptualColors.Add(color.ToPerceptual().ApplyGamma(1.5));
            //}

            //colors.Clear();

            //colors.AddRange(perceptualColors);

            //List<Color> colors = new List<Color>();

            //for (int i = 0; i < 2304; i++)
            //{
            //    colors.Add(Color.FromArgb(255, 255, 0, 0));
            //}

            //List<Color> colorsAdjusted = new List<Color>();

            //colorsAdjusted.AddRange(colors);

            //colors.ForEach(c => colorsAdjusted.Add(c.ApplyGamma(0.15)));
            //colors.ForEach(c => colorsAdjusted.Add(c.ToPerceptual()));

            //colors.RemoveAt(MagicPixel);

            var bytes = colors.Get21BitPixelBytes();

            App.Firmata.sendSysex(LED_RESET);
            await Task.Delay(1);

            App.Firmata.sendSysex(LED_PIXEL21);
            await Task.Delay(1);

            App.Firmata.sendPixelBlob(bytes, 30);
            await Task.Delay(1);

            App.Firmata.sendSysex(LED_RESET);
            await Task.Delay(1);
        }
    }
}
