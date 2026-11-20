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

        public static byte[]? CompressToJpeg(string filePath)
        {
            var image = ImageDecoder.Decode(File.ReadAllBytes(filePath));
            if (image is null)
            {
                return null;
            }

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
            return ImageDecoder.Decode(bytes);
        }

        public static BitmapImage? ToAvatarImage(byte[]? bytes)
        {
            return ImageDecoder.Decode(bytes, AvatarDecodeWidth);
        }
    }
}
