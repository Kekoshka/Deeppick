using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using System.Drawing.Imaging;

namespace Deeppick.Interfaces
{
    public interface IFileHandleService
    {
        string[] GetDirectoryFilesPaths(string directoryPath, string[] filesExtensions);
        byte[] GetFile(string filePath);
        void SaveImage(byte[] image, string path, string fileName);
        void SaveImages(List<byte[]> imagesList, string path, string baseFileName = "image", ImageFormat format = null);
        void SaveImages(List<byte[]> imagesList, string path);
        List<byte[]> ExtractFramesFromVideo(byte[] video, int intervalMs);
        byte[] ResizeImage(int height, int width, byte[] imageByte);
        byte[] ResizeImage(int size, byte[] imageByte);
        List<byte[]> ResizeImageRange(int height, int width, List<byte[]> imagesBytes);
        List<byte[]> ResizeImageRange(int size, List<byte[]> imagesBytes);


    }
}
