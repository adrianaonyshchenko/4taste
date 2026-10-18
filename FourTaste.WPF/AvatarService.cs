using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace FourTaste.WPF
{
    public static class AvatarService
    {
        private static readonly string AvatarsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FourTaste",
            "Avatars");

        static AvatarService()
        {
            if (!Directory.Exists(AvatarsDirectory))
            {
                Directory.CreateDirectory(AvatarsDirectory);
            }
        }

        public static string GetAvatarFilePath(int userId)
        {
            return Path.Combine(AvatarsDirectory, $"user_{userId}.png");
        }

        public static bool HasAvatar(int userId)
        {
            if (userId <= 0)
            {
                return false;
            }

            return File.Exists(GetAvatarFilePath(userId));
        }

        public static BitmapImage? LoadAvatar(int userId)
        {
            if (userId <= 0)
            {
                return null;
            }

            string path = GetAvatarFilePath(userId);
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        public static bool SaveAvatar(int userId, string sourceFilePath)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
            {
                return false;
            }

            try
            {
                string targetPath = GetAvatarFilePath(userId);
                File.Copy(sourceFilePath, targetPath, overwrite: true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool DeleteAvatar(int userId)
        {
            if (userId <= 0)
            {
                return false;
            }

            try
            {
                string path = GetAvatarFilePath(userId);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

