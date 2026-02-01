using Deeppick.Interfaces;
using Tensorflow;

namespace Deeppick.Services
{
    public class DataHandleService
    {
        IFaceExtractService _faceExtractService;
        IFileHandleService _fileHandleService;
        INoiseExtractService _noiseExtractService;
        public DataHandleService(IFaceExtractService faceExtractService, 
            IFileHandleService fileHandleService, 
            INoiseExtractService noiseExtractService) 
        {
            _faceExtractService = faceExtractService;
            _fileHandleService = fileHandleService;
            _noiseExtractService = noiseExtractService;
        }

        public void ExtractFacesFromVideo(int msRate, int frameSize, string inputPath, string outputDirectoryPath) 
        {
            var videoByte = _fileHandleService.GetFile(inputPath);
            var videoFrames = _fileHandleService.ExtractFramesFromVideo(videoByte, msRate);
            var faces = new List<byte[]>();
            foreach(var videoFrame in videoFrames)
            {
                faces.AddRange(_faceExtractService.ExtractFaceFromImage(videoFrame));
                if (faces.Count > 100)
                {
                    _fileHandleService.SaveImages(faces, outputDirectoryPath);
                    faces.Clear();
                }
            }
            _fileHandleService.SaveImages(faces, outputDirectoryPath);
        }
        public void ExtractFacesFromAllDirectoryVideos()
        {

        }
        public void ExtractFacesNoiseFromVideo(int msRate, int frameSize, string inputDirectory, string outputDirectory)
        {

        }
        public void ExtractFacesNoiseFromAllDirectoryVideos()
        {

        }
    }
}
