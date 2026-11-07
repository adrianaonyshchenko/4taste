namespace FourTaste.ApplicationCore.Interfaces
{
    public interface IImageThumbnailGenerator
    {
        byte[]? CreateThumbnail(byte[]? photo);
    }
}