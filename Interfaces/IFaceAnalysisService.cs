namespace Deeppick.Interfaces
{
    public interface IFaceAnalysisService
    {
        void GetImagesFromVideo(string inputPath, string outputPath, int rate, int resolution);
    }
}
