using System;
using System.IO;
using System.Windows.Media.Imaging;
using FourTaste.ApplicationCore.Services;
using FourTaste.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

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
            try
            {
                if (!Directory.Exists(AvatarsDirectory))
                {
                    Directory.CreateDirectory(AvatarsDirectory);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Не вдалося створити директорію для аватарок");
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

            if (SessionContext.CurrentUser != null && SessionContext.CurrentUser.ID == userId)
            {
                if (SessionContext.CurrentUser.Avatar != null && SessionContext.CurrentUser.Avatar.Length > 0)
                {
                    return true;
                }
            }

            string path = GetAvatarFilePath(userId);
            if (File.Exists(path))
            {
                return true;
            }

            var dbBytes = LoadAvatarFromDatabase(userId);
            return dbBytes != null && dbBytes.Length > 0;
        }

        public static BitmapImage? LoadAvatar(int userId)
        {
            if (userId <= 0)
            {
                return null;
            }

            string path = GetAvatarFilePath(userId);

            if (SessionContext.CurrentUser != null && SessionContext.CurrentUser.ID == userId)
            {
                if (SessionContext.CurrentUser.Avatar != null && SessionContext.CurrentUser.Avatar.Length > 0)
                {
                    if (!File.Exists(path))
                    {
                        try
                        {
                            File.WriteAllBytes(path, SessionContext.CurrentUser.Avatar);
                        }
                        catch
                        {
                        }
                    }

                    return ToBitmapImage(SessionContext.CurrentUser.Avatar);
                }

                // В сесії ще немає байтів — перевіряємо базу даних
                byte[]? dbBytes = LoadAvatarFromDatabase(userId);
                if (dbBytes != null && dbBytes.Length > 0)
                {
                    SessionContext.CurrentUser.Avatar = dbBytes;
                    try
                    {
                        File.WriteAllBytes(path, dbBytes);
                    }
                    catch
                    {
                    }

                    return ToBitmapImage(dbBytes);
                }

                // Якщо в БД ще немає, але на цьому комп'ютері вже був збережений локальний файл
                // синхронізуємо його в БД, щоб аватарка підтягнулася і на інших комп'ютерах
                if (File.Exists(path))
                {
                    try
                    {
                        byte[] localBytes = File.ReadAllBytes(path);
                        if (localBytes.Length > 0 && localBytes.Length <= 5 * 1024 * 1024)
                        {
                            SessionContext.CurrentUser.Avatar = localBytes;
                            SaveAvatarToDatabase(userId, localBytes);
                            return ToBitmapImage(localBytes);
                        }
                    }
                    catch
                    {
                    }
                }

                return null;
            }

            // Для іншого користувача: спершу локальний кеш, якщо немає — БД
            if (File.Exists(path))
            {
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
                }
            }

            byte[]? remoteBytes = LoadAvatarFromDatabase(userId);
            if (remoteBytes != null && remoteBytes.Length > 0)
            {
                try
                {
                    File.WriteAllBytes(path, remoteBytes);
                }
                catch
                {
                }

                return ToBitmapImage(remoteBytes);
            }

            return null;
        }

        public static bool SaveAvatar(int userId, string sourceFilePath)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
            {
                return false;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(sourceFilePath);
                return SaveAvatar(userId, bytes);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Помилка читання файлу аватарки для користувача {UserId}", userId);
                return false;
            }
        }

        public static bool SaveAvatar(int userId, byte[] bytes)
        {
            if (userId <= 0 || bytes == null || bytes.Length == 0 || bytes.Length > 5 * 1024 * 1024)
            {
                return false;
            }

            try
            {
                string targetPath = GetAvatarFilePath(userId);
                File.WriteAllBytes(targetPath, bytes);

                if (SessionContext.CurrentUser != null && SessionContext.CurrentUser.ID == userId)
                {
                    SessionContext.CurrentUser.Avatar = bytes;
                }

                SaveAvatarToDatabase(userId, bytes);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Помилка збереження аватарки користувача {UserId}", userId);
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

                if (SessionContext.CurrentUser != null && SessionContext.CurrentUser.ID == userId)
                {
                    SessionContext.CurrentUser.Avatar = null;
                }

                SaveAvatarToDatabase(userId, null);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Помилка видалення аватарки користувача {UserId}", userId);
                return false;
            }
        }

        public static BitmapImage? ToBitmapImage(byte[]? bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            try
            {
                using var stream = new MemoryStream(bytes);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        private static byte[]? LoadAvatarFromDatabase(int userId)
        {
            try
            {
                var factory = App.ServiceProvider?.GetService<IDbContextFactory<AppDbContext>>();
                if (factory != null)
                {
                    using var context = factory.CreateDbContext();
                    context.EnsureDatabaseReady();
                    var user = context.Users.Find(userId);
                    return user?.Avatar;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Помилка завантаження аватарки з бази даних для ID {UserId}", userId);
            }

            return null;
        }

        private static void SaveAvatarToDatabase(int userId, byte[]? avatarBytes)
        {
            try
            {
                var factory = App.ServiceProvider?.GetService<IDbContextFactory<AppDbContext>>();
                if (factory != null)
                {
                    using var context = factory.CreateDbContext();
                    context.EnsureDatabaseReady();
                    var user = context.Users.Find(userId);
                    if (user != null)
                    {
                        user.Avatar = avatarBytes;
                        context.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Помилка збереження аватарки в базі даних для ID {UserId}", userId);
            }
        }
    }
}
