using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.UI.Xaml.Media.Imaging;

namespace RemoteLedMatrix.Hardware
{
    public interface ILedMatrix
    {
        void Initialize();
        Task DisplayImage(WriteableBitmap image);

        int PixelWidth { get; }
        int PixelHeight { get; }
    }
}
