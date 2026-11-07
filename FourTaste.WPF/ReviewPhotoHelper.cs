using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FourTaste.WPF
{
    public static class ReviewPhotoHelper
    {
        private const int MaxSide = 1000;
        private const int JpegQuality = 80;
        private const int AvatarDecodeWidth = 96;

        public static byte[] CompressToJpeg(string filePath)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(filePath, UriKind.Absolute);
            image.EndInit();

            BitmapSource result = image;
            int longestSide = Math.Max(image.PixelWidth, image.PixelHeight);
            if (longestSide > MaxSide)
            {
                double scale = (double)MaxSide / longestSide;
                result = new TransformedBitmap(image, new ScaleTransform(scale, scale));
            }

            var encoder = new JpegBitmapEncoder { QualityLevel = JpegQuality };
            encoder.Frames.Add(BitmapFrame.Create(result));

            using var stream = new MemoryStream();
            encoder.Save(stream);
            return stream.ToArray();
        }

        public static BitmapImage? ToBitmapImage(byte[]? bytes)
        {
            if (bytes is null || bytes.Length == 0)
            {
                return null;
            }

            using var stream = new MemoryStream(bytes);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }

        public static BitmapImage? ToAvatarImage(byte[]? bytes)
        {
            if (bytes is null || bytes.Length == 0)
            {
                return null;
            }

            try
            {
                using var stream = new MemoryStream(bytes);
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.DecodePixelWidth = AvatarDecodeWidth;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch (Exception ex) when (ex is NotSupportedException or FileFormatException or IOException or InvalidOperationException)
            {
                return null;
            }
        }
    }
}