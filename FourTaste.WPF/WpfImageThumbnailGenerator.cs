using System;
using System.IO;
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
            if (photo is null || photo.Length == 0)
            {
                return null;
            }

            try
            {
                int originalWidth;
                using (var probe = new MemoryStream(photo))
                {
                    originalWidth = BitmapDecoder
                        .Create(probe, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None)
                        .Frames[0]
                        .PixelWidth;
                }

                using var input = new MemoryStream(photo);
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.DecodePixelWidth = Math.Min(MaxWidth, originalWidth);
                image.StreamSource = input;
                image.EndInit();
                image.Freeze();

                var encoder = new JpegBitmapEncoder { QualityLevel = JpegQuality };
                encoder.Frames.Add(BitmapFrame.Create(image));

                using var output = new MemoryStream();
                encoder.Save(output);
                return output.ToArray();
            }
            catch (Exception ex) when (ex is NotSupportedException or FileFormatException or IOException or InvalidOperationException)
            {
                return null;
            }
        }
    }
}