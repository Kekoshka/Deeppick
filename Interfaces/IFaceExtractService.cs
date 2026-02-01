namespace Deeppick.Interfaces
{
    public interface IFaceExtractService
    {
        void GetImagesFromVideo(string inputPath, string outputPath, int rate, int resolution);
        List<byte[]> ExtractFaceFromImage(byte[] image);
}
}
