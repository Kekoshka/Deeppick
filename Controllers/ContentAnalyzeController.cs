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
        public async Task<IActionResult> AnalyseVideoDefault(byte[] videoByte)
        {
            var predict = await _dataAnalysisService.AnalyzeVideoDefaultAsync(videoByte);
            return Ok(predict);
        }

        [HttpPost("videoNoise")]
        public async Task<IActionResult> AnalyseVideoNoise(byte[] videoByte)
        {
            var predict = await _dataAnalysisService.AnalyzeVideoNoiseAsync(videoByte);
            return Ok(predict);
        }
        [HttpPost("image")]
        public async Task<IActionResult> AnalyseImageDefault(byte[] imageByte)
        {
            var predict = await _dataAnalysisService.AnalyzeImageDefaultAsync(imageByte);
            return Ok(predict);
        }
        [HttpPost("imageNoise")]
        public async Task<IActionResult> AnalyseImageNoise(byte[] imageByte)
        {
            var predict = await _dataAnalysisService.AnalyzeImageNoiseAsync(imageByte);
            return Ok(predict);
        }

    }
}
