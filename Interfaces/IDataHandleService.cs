using Deeppick.Services;

namespace Deeppick.Interfaces
{
    public interface IDataHandleService
    {
        void ExtractFacesFromVideo(int msRate, int frameSize, string inputPath, string outputDirectoryPath);
        void ExtractFacesFromAllDirectoryVideos(int msRate, int frameSize, string inputDirectory, string outputDirectory);
        Task ExtractFacesNoiseFromVideoAsync(int msRate, int frameSize, string inputPath, string outputDirectoryPath);
        Task ExtractFacesNoiseFromAllDirectoryVideosAsync(int msRate, int frameSize, string inputDirectory, string outputDirectory);
    }
}
