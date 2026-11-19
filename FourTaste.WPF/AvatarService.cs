using System;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using FourTaste.ApplicationCore.Entities;
using FourTaste.ApplicationCore.Interfaces;
using FourTaste.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FourTaste.WPF
{
    public sealed class AvatarService
    {
        public const int MaxAvatarBytes = 5 * 1024 * 1024;

        private static readonly string DefaultAvatarsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FourTaste",
            "Avatars");

        private readonly ICurrentUserService currentUser;
        private readonly IDbContextFactory<AppDbContext> contextFactory;
        private readonly string avatarsDirectory;

        public AvatarService(ICurrentUserService currentUser, IDbContextFactory<AppDbContext> contextFactory)
            : this(currentUser, contextFactory, DefaultAvatarsDirectory)
        {
        }

        public AvatarService(ICurrentUserService currentUser, IDbContextFactory<AppDbContext> contextFactory, string avatarsDirectory)
        {
            this.currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            this.contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            this.avatarsDirectory = avatarsDirectory;
            Directory.CreateDirectory(avatarsDirectory);
        }

        public string GetAvatarFilePath(int userId)
        {
            return Path.Combine(this.avatarsDirectory, $"user_{userId}.png");
        }

        public bool HasAvatar(int userId)
        {
            return this.LoadAvatarBytes(userId) is not null;
        }

        public BitmapImage? LoadAvatar(int userId)
        {
            return ImageDecoder.Decode(this.LoadAvatarBytes(userId));
        }

        public byte[]? LoadAvatarBytes(int userId)
        {
            if (userId <= 0)
            {
                return null;
            }

            var sessionUser = this.GetSessionUser(userId);
            if (sessionUser?.Avatar is { Length: > 0 } sessionAvatar)
            {
                return sessionAvatar;
            }

            byte[]? bytes = sessionUser is not null
                ? this.LoadAvatarFromDatabase(userId) ?? this.ReadLocalFile(userId)
                : this.ReadLocalFile(userId) ?? this.LoadAvatarFromDatabase(userId);

            if (bytes is null)
            {
                return null;
            }

            this.CacheLocally(userId, bytes);
            sessionUser?.Avatar = bytes;
            return bytes;
        }

        public bool SaveAvatar(int userId, string sourceFilePath)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath) || new FileInfo(sourceFilePath).Length > MaxAvatarBytes)
            {
                return false;
            }

            return this.SaveAvatar(userId, File.ReadAllBytes(sourceFilePath));
        }

        public bool SaveAvatar(int userId, byte[] bytes)
        {
            if (userId <= 0 || bytes is not { Length: > 0 and <= MaxAvatarBytes })
            {
                return false;
            }

            File.WriteAllBytes(this.GetAvatarFilePath(userId), bytes);
            this.GetSessionUser(userId)?.Avatar = bytes;
            this.SaveAvatarToDatabase(userId, bytes);
            return true;
        }

        public bool DeleteAvatar(int userId)
        {
            if (userId <= 0)
            {
                return false;
            }

            File.Delete(this.GetAvatarFilePath(userId));
            this.GetSessionUser(userId)?.Avatar = null;
            this.SaveAvatarToDatabase(userId, null);
            return true;
        }

        private User? GetSessionUser(int userId)
        {
            var user = this.currentUser.CurrentUser;
            return user?.ID == userId ? user : null;
        }

        private byte[]? ReadLocalFile(int userId)
        {
            var file = new FileInfo(this.GetAvatarFilePath(userId));
            return file.Exists && file.Length is > 0 and <= MaxAvatarBytes
                ? File.ReadAllBytes(file.FullName)
                : null;
        }

        private void CacheLocally(int userId, byte[] bytes)
        {
            string path = this.GetAvatarFilePath(userId);
            if (!File.Exists(path))
            {
                File.WriteAllBytes(path, bytes);
            }
        }

        private byte[]? LoadAvatarFromDatabase(int userId)
        {
            using var context = this.contextFactory.CreateDbContext();
            context.EnsureDatabaseReady();
            var avatar = context.Users.AsNoTracking()
                .Where(u => u.ID == userId)
                .Select(u => u.Avatar)
                .FirstOrDefault();
            return avatar is { Length: > 0 } ? avatar : null;
        }

        private void SaveAvatarToDatabase(int userId, byte[]? avatarBytes)
        {
            using var context = this.contextFactory.CreateDbContext();
            context.EnsureDatabaseReady();
            var user = context.Users.FirstOrDefault(u => u.ID == userId);
            if (user is null)
            {
                return;
            }

            user.Avatar = avatarBytes;
            context.SaveChanges();
        }
    }
}
