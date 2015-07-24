
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml.Media.Imaging;

namespace RemoteLedMatrix.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Windows.UI;

    public static class ColorExtensions
    {
        /// <summary>
        /// Cached hue rotation calculations for Pi/3
        /// </summary>
        private const double PiOverThree = Math.PI / 3.0f;

        /// <summary>
        /// Cached hue rotation calculations for 2Pi/3
        /// </summary>
        private const double TwoPiOverThree = 2 * PiOverThree;

        /// <summary>
        /// Cached hue rotation calculations for 4Pi/3
        /// </summary>
        private const double FourPiOverThree = 4 * PiOverThree;

        /// <summary>
        /// Gets the hue as a 0-360 value, representing the degree of rotation  
        /// </summary>
        /// <param name="color">Color to get the hue of</param>
        /// <returns>Hue as a 0-360 value, representing the degree of rotation</returns>
        public static float GetHue(this Color color)
        {
            // Any shade of white has no hue
            if (color.R == color.G && color.G == color.B)
            {
                return 0;
            }

            float hue = 0;

            float red = (float)color.R / 255.0f;
            float green = (float)color.G / 255.0f;
            float blue = (float)color.B / 255.0f;

            float[] colorArray = new[] { red, green, blue };

            float max = colorArray.Max();
            float min = colorArray.Min();
            float delta = max - min;

            if (red.Equals(max))
            {
                hue = (green - blue) / delta;
            }
            else if (green.Equals(max))
            {
                hue = 2 + ((blue - red) / delta);
            }
            else
            {
                hue = 4 + ((red - green) / delta);
            }

            return ((hue * 60) + 360) % 360;
        }

        /// <summary>
        /// Gets the HSI intensity of the color
        /// </summary>
        /// <param name="color">Color to get the intensity of</param>
        /// <returns>0-1 value indicating the intensity of the color in HSI color space</returns>
        public static float GetIntensity(this Color color)
        {
            byte[] channelValues = { color.R, color.G, color.B };

            byte max = channelValues.Max();

            if (0 == max)
            {
                return 0;
            }

            return max / 255.0f;
        }

        /// <summary>
        /// Gets the HSI saturation of the color
        /// </summary>
        /// <param name="color">Color to get the saturation of</param>
        /// <returns>0-1 value indicating the saturation of the color in HSI color space</returns>
        public static float GetSaturation(this Color color)
        {
            byte[] channelValues = { color.R, color.G, color.B };

            int min = channelValues.Min();
            int max = channelValues.Max();

            // If the brightest channel is black (0), there's zero saturation.
            if (0 == max)
            {
                return 0f;
            }

            return (1 - (min / (float)max));
        }

        /// <summary>
        /// Gamma shifts a color by a given amount
        /// </summary>
        /// <param name="source">Source pixel to gamma shift</param>
        /// <param name="gamma">Amount to gamma shift by</param>
        /// <returns>Task representing the operation</returns>
        public static Color ApplyGamma(this Color source, double gamma)
        {
            return Color.FromArgb(
                255,
                ApplyGammaToChannel(source.R, gamma),
                ApplyGammaToChannel(source.G, gamma),
                ApplyGammaToChannel(source.B, gamma));
        }

        public static Color ToPerceptual(this Color source)
        {
            double hue = source.GetHue() * (Math.PI / 180.0f);
            float intensity = source.GetIntensity() / 3.0f;
            float saturation = source.GetSaturation();
            
            byte red, green, blue;

            if (hue < TwoPiOverThree)
            {
                red = perceptualTransformA(hue, saturation, intensity);
                green = perceptualTransformB(hue, saturation, intensity);
                blue = perceptualTransformC(hue, saturation, intensity);
            }
            else if (hue < FourPiOverThree)
            {
                hue = hue - TwoPiOverThree;

                red = perceptualTransformC(hue, saturation, intensity);
                green = perceptualTransformA(hue, saturation, intensity);
                blue = perceptualTransformB(hue, saturation, intensity);
            }
            else
            {
                hue = hue - FourPiOverThree;

                red = perceptualTransformB(hue, saturation, intensity);
                green = perceptualTransformC(hue, saturation, intensity);
                blue = perceptualTransformA(hue, saturation, intensity);
            }

            return Color.FromArgb(255, red, green, blue);
        }

        private static byte perceptualTransformA(double hue, float saturation, float intensity)
        {
            double value = intensity * (1 + (saturation * (Math.Cos(hue) / Math.Cos(PiOverThree - hue))));

            return (byte)(255 * value);
        }

        private static byte perceptualTransformB(double hue, float saturation, float intensity)
        {
            double value = intensity * (1 + (saturation * (1 - (Math.Cos(hue) / Math.Cos(PiOverThree - hue)))));

            return (byte)(255 * value);
        }

        private static byte perceptualTransformC(double hue, float saturation, float intensity)
        {
            double value = intensity * (1 - saturation);

            return (byte)(255 * value);
        }

        /// <summary>
        /// Applies a gamma shift to a byte value
        /// </summary>
        /// <param name="value">Value to shift</param>
        /// <param name="gamma">Gamma amount to shift by</param>
        /// <returns>Task representing the operation</returns>
        private static byte ApplyGammaToChannel(byte value, double gamma)
        {
            return (byte)(255 * Math.Pow(value / (double)255, gamma));
        }

        public static List<Color> GetColorsFromWriteableBitmap(this WriteableBitmap bitmap)
        {
            List<Color> pixels = new List<Color>();

            for (int y = 0; y < bitmap.PixelHeight; y++)
            {
                for (int x = 0; x < bitmap.PixelWidth; x++)
                {
                    pixels.Add(bitmap.GetPixel(x, y));
                }
            }

            return pixels;

            //return bitmap.PixelBuffer.ToArray().GetColorsFromPixelBytes();
        }


        public static List<Color> GetColorsFromPixelBytes(this byte[] pixelBuffer)
        {
            List<Color> pixels = new List<Color>();

            for (int x = 0; x < 48; x++)
            {
                List<Color> columnData = new List<Color>();

                for (int y = 0; y < 48; y++)
                {
                    int position = ((y * 48) + x) * 4;
                    columnData.Add(
                        // Buffer is in BGRA format
                        Color.FromArgb(
                            255,
                            pixelBuffer[position + 2],
                            pixelBuffer[position + 1],
                            pixelBuffer[position]
                            ));
                }

                pixels.AddRange(columnData);
            }

            return pixels;
        }

        /// <summary>
        /// Converts a 8-bit RGB Color into three 7-bit bytes, each of which was bit-shifted
        /// one to the right.
        /// </summary>
        /// <param name="color">Color to convert</param>
        /// <returns>IEnumerable of bytes of the shifted values</returns>
        public static IEnumerable<byte> To7BitPixelBytes(this Color color)
        {
            return new[]
            {
                (byte)(color.R >> 1),
                (byte)(color.G >> 1),
                (byte)(color.B >> 1)
            };
        }

        public static IEnumerable<byte> To21BitPixelBytes(this Color color)
        {
            return new[]
            {
                (byte)(color.R >> 1),
                (byte)(color.G >> 1),
                (byte)(color.B >> 1)
            };
        }

        /// <summary>
        /// Converts an IEnumerable of colors into an IEnumerable of bytes
        /// </summary>
        /// <param name="colors"></param>
        /// <returns></returns>
        public static IEnumerable<byte> Get21BitPixelBytes(this IEnumerable<Color> colors)
        {
            return colors.SelectMany(c => c.To21BitPixelBytes());
        }

        public static List<Color> FlipEvenColumns(this List<Color> pixels)
        {
            List<Color> pixelsFlipped = new List<Color>();

            var pixelColumns = pixels.InSetsOf(48);

            bool flip = true;
            foreach (var column in pixelColumns)
            {
                if (flip)
                {
                    column.Reverse();
                }

                flip = !flip;

                pixelsFlipped.AddRange(column);
            }

            return pixelsFlipped;
        }
    }
}
