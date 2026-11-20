using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FourTaste.ApplicationCore.Interfaces;

namespace FourTaste.WPF
{
    public sealed class WpfImageThumbnailGenerator : IImageThumbnailGenerator
    {
        private const int MaxWidth = 400;
        private const int JpegQuality = 75;

        public byte[]? CreateThumbnail(byte[]? photo)
        {
            var image = ImageDecoder.Decode(photo);
            if (image is null)
            {
                return null;
            }

            BitmapSource source = image.PixelWidth > MaxWidth
                ? new TransformedBitmap(image, new ScaleTransform((double)MaxWidth / image.PixelWidth, (double)MaxWidth / image.PixelWidth))
                : image;

            var encoder = new JpegBitmapEncoder { QualityLevel = JpegQuality };
            encoder.Frames.Add(BitmapFrame.Create(source));

            using var output = new MemoryStream();
            encoder.Save(output);
            return output.ToArray();
        }
    }
}