using Deeppick.Interfaces;
using System.Threading.Tasks;
using Tensorflow;

namespace Deeppick.Services
{
    public class DataAnalysisService : IDataAnalysisService
    {
        IFileHandleService _fileHandleService;
        IFaceExtractService _faceExtractService;
        IMLService _mlService;
        INoiseExtractService _noiseExtractService;
        public DataAnalysisService(IFileHandleService fileHandleService,
            IFaceExtractService faceExtractService,
            IMLService mlService,
            INoiseExtractService noiseExtractService)
        {
            _fileHandleService = fileHandleService;
            _faceExtractService = faceExtractService;
            _mlService = mlService;
            _noiseExtractService = noiseExtractService;
        }
        public async Task<float> AnalyzeVideoDefaultAsync(byte[] videoByte)
        {
            var frames = _fileHandleService.ExtractFramesFromVideo(videoByte, 1000);
            var faces = new List<byte[]>();
            float predictionSum = 0;
            foreach (var frame in frames)
            {
                var extractedFace = _faceExtractService.ExtractFaceFromImage(frame);
                var resizedFace = _fileHandleService.ResizeImageRange(200, extractedFace);
            }
            foreach (var face in faces)
                predictionSum += await _mlService.PredictImageAsync(face, "Models/default.zip");
            return predictionSum / faces.Count;
        }
        public async Task<float> AnalyzeImageDefaultAsync(byte[] imageByte)
        {
            var faces = new List<byte[]>();
            float predictionSum = 0;
            var extractedFace = _faceExtractService.ExtractFaceFromImage(imageByte);
            var resizedFace = _fileHandleService.ResizeImageRange(200, extractedFace);
            faces.AddRange(resizedFace);
            foreach (var face in faces)
                predictionSum += await _mlService.PredictImageAsync(face, "Models/default.zip");
            return predictionSum / faces.Count;
        }
        public async Task<float> AnalyzeVideoNoiseAsync(byte[] videoByte)
        {
            var frames = _fileHandleService.ExtractFramesFromVideo(videoByte, 1000);
            var faces = new List<byte[]>();
            float predictionSum = 0;
            foreach (var frame in frames)
            {
                var face = _faceExtractService.ExtractFaceFromImage(frame);
                var noiseFace = await _noiseExtractService.ProcessImageRangeAsync(face);
                var resizedFace = _fileHandleService.ResizeImageRange(200, noiseFace);
                faces.AddRange(resizedFace);
            }
            foreach (var face in faces)
                predictionSum += await _mlService.PredictImageAsync(face,"Models/noise.zip");
            return predictionSum / faces.Count;
        }
        public async Task<float> AnalyzeImageNoiseAsync(byte[] imageByte)
        {
            var faces = new List<byte[]>();
            float predictionSum = 0;
            var extractedFace = _faceExtractService.ExtractFaceFromImage(imageByte);
            var noiseFace = await _noiseExtractService.ProcessImageRangeAsync(extractedFace);
            var resizedFace = _fileHandleService.ResizeImageRange(200, noiseFace);
            faces.AddRange(resizedFace);
            foreach (var face in faces)
                predictionSum += await _mlService.PredictImageAsync(face, "Models/noise.zip");
            return predictionSum / faces.Count;
        }
    }
}
