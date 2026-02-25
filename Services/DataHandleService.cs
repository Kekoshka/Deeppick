using Deeppick.Interfaces;
using Microsoft.AspNetCore.Components.Forms;
using Tensorflow;

namespace Deeppick.Services
{
    public class DataHandleService : IDataHandleService
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
                

                faces.AddRange(_fileHandleService.ResizeImageRange(frameSize, _faceExtractService.ExtractFaceFromImage(videoFrame)));
                if (faces.Count > 100)
                {
                    _fileHandleService.SaveImages(faces, outputDirectoryPath);
                    faces.Clear();
                }
            }
            _fileHandleService.SaveImages(faces, outputDirectoryPath);
        }
        public void ExtractFacesFromAllDirectoryVideos(int msRate, int frameSize, string inputDirectory, string outputDirectory)
        {
            var filesPaths = _fileHandleService.GetDirectoryFilesPaths(inputDirectory, ["mp4"]);
            foreach (var filePath in filesPaths)
                ExtractFacesFromVideo(msRate, frameSize, filePath, outputDirectory);

        }
        public async Task ExtractFacesNoiseFromVideoAsync(int msRate, int frameSize, string inputPath, string outputDirectoryPath)
        {
            var videoByte = _fileHandleService.GetFile(inputPath);
            var videoFrames = _fileHandleService.ExtractFramesFromVideo(videoByte, msRate);
            var resizedFaces = new List<byte[]>();
            foreach (var videoFrame in videoFrames)
            {
                var faces = await _noiseExtractService.ProcessImageRangeAsync(_faceExtractService.ExtractFaceFromImage(videoFrame));
                resizedFaces.AddRange(_fileHandleService.ResizeImageRange(frameSize, faces));
                if (resizedFaces.Count > 100)
                {
                    _fileHandleService.SaveImages(resizedFaces, outputDirectoryPath);
                    resizedFaces.Clear();
                }
            }
            _fileHandleService.SaveImages(resizedFaces, outputDirectoryPath);
        }
        public async Task ExtractFacesNoiseFromAllDirectoryVideosAsync(int msRate, int frameSize, string inputDirectory, string outputDirectory)
        {
            var filesPaths = _fileHandleService.GetDirectoryFilesPaths(inputDirectory, ["mp4"]);
            foreach (var filePath in filesPaths)
                await ExtractFacesNoiseFromVideoAsync(msRate, frameSize, filePath, outputDirectory);
        }
    }
}
