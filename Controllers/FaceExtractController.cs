using Deeppick.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Deeppick.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FaceExtractController : ControllerBase
    {
        IFaceExtractService _faceAnalysisService;
        public FaceExtractController(IFaceExtractService faceAnalysisService) 
        {
            _faceAnalysisService = faceAnalysisService;
        }

        //[HttpPost]
        //public async Task<IActionResult> ExtractFacesFromVideo(int rate, int resolution, string inputPath, string outputPath)
        //{
        //    _faceAnalysisService.GetImagesFromVideo(inputPath, outputPath, rate, resolution);
        //    return NoContent();
        //}
        [HttpPost]
        public async Task<IActionResult> ExtractFacesFromFolder(int rate, int resolution, string folderPath, string outputPath)
        {
            string[] _videoExtensions = { ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg", ".3gp", ".ts" };
            
            var files = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
                .Where(file => _videoExtensions.Contains(Path.GetExtension(file).ToLower()))
                .ToArray();

            foreach(var file in files)
            {
                _faceAnalysisService.GetImagesFromVideo(file, outputPath, rate, resolution);
            }
            return NoContent();
        }
    }
}
