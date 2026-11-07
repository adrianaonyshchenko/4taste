using System.Collections.Concurrent;
using System.Windows.Media.Imaging;

namespace FourTaste.WPF
{
    public static class ReviewAvatarCache
    {
        private static readonly ConcurrentDictionary<int, BitmapImage?> Cache = new ();

        public static bool TryGet(int userId, out BitmapImage? avatar)
        {
            return Cache.TryGetValue(userId, out avatar);
        }

        public static void Set(int userId, BitmapImage? avatar)
        {
            Cache[userId] = avatar;
        }

        public static void Remove(int userId)
        {
            Cache.TryRemove(userId, out _);
        }
    }
}