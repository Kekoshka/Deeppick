using Deeppick.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Deeppick.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FaceExtractController : ControllerBase
    {
        IFaceAnalysisService _faceAnalysisService;
        public FaceExtractController(IFaceAnalysisService faceAnalysisService) 
        {
            _faceAnalysisService = faceAnalysisService;
        }

        [HttpPost]
        public async Task<IActionResult> ExtractFacesFromVideo(int rate, int resolution, string inputPath, string outputPath)
        {
            _faceAnalysisService.GetImagesFromVideo(inputPath, outputPath, rate, resolution);
            return NoContent();
        }
    }
}
