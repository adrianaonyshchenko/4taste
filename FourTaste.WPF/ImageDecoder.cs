using System;
using System.IO;
using System.Windows.Media.Imaging;
using Serilog;

namespace FourTaste.WPF
{
    public static class ImageDecoder
    {
        public static BitmapImage? Decode(byte[]? bytes, int decodePixelWidth = 0)
        {
            if (bytes is not { Length: > 0 })
            {
                return null;
            }

            try
            {
                using var stream = new MemoryStream(bytes);
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.DecodePixelWidth = decodePixelWidth;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch (Exception ex) when (ex is NotSupportedException or FileFormatException or IOException or InvalidOperationException or ArgumentException)
            {
                Log.Warning(ex, "Не вдалося декодувати зображення ({Length} байт)", bytes.Length);
                return null;
            }
        }
    }
}
