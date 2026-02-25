using Deeppick.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Deeppick.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContentAnalyzeController : ControllerBase
    {
        IDataAnalysisService _dataAnalysisService;
        public ContentAnalyzeController(IDataAnalysisService dataAnalysisService)
        {
            _dataAnalysisService = dataAnalysisService;
        }

        [HttpPost("video")]
        public async Task<IActionResult> AnalyseVideoDefault(IFormFile file)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();
            var predict = await _dataAnalysisService.AnalyzeVideoDefaultAsync(bytes);
            return Ok(predict);
        }

        [HttpPost("videoNoise")]
        public async Task<IActionResult> AnalyseVideoNoise(IFormFile file)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();
            var predict = await _dataAnalysisService.AnalyzeVideoNoiseAsync(bytes);
            return Ok(predict);
        }

        [HttpPost("image")]
        public async Task<IActionResult> AnalyseImageDefault(IFormFile file)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();
            var predict = await _dataAnalysisService.AnalyzeImageDefaultAsync(bytes);
            return Ok(predict);
        }

        [HttpPost("imageNoise")]
        public async Task<IActionResult> AnalyseImageNoise(IFormFile file)
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();
            var predict = await _dataAnalysisService.AnalyzeImageNoiseAsync(bytes);
            return Ok(predict);
        }

    }
}
