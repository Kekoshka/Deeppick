using Deeppick.Models;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Deeppick.Interfaces;

namespace Deeppick.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MLController : ControllerBase
    {
        private readonly IMLService _modelService;
        private readonly ILogger<MLController> _logger;
        
        public MLController(IMLService modelService, ILogger<MLController> logger)
        {
            _modelService = modelService;
            _logger = logger;
        }


        [HttpPost("train")]
        public async Task<IActionResult> TrainModel([FromBody] TrainingRequest request)
        {
            try
            {
                _logger.LogInformation("Starting model training");

                if (request == null)
                    return BadRequest(new { error = "Training request is required" });

                var result = await _modelService.TrainModelAsync(request);

                if (result.Status == "Error")
                    return StatusCode(500, result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during model training");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/detection/predict
        [HttpPost("predict")]
        public async Task<IActionResult> PredictImage([FromForm] PredictionRequest request)
        {
            try
            {
                if (request.ImageFile == null || request.ImageFile.Length == 0)
                    return BadRequest(new { error = "Image file is required" });

                // Проверка расширения файла
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(request.ImageFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest(new { error = "Only JPG, JPEG and PNG files are allowed" });

                // Проверка существования модели
                if (!_modelService.ModelExists(request.ModelPath))
                    return NotFound(new { error = $"Model not found: {request.ModelPath}" });

                using (var stream = request.ImageFile.OpenReadStream())
                {
                    var result = await _modelService.PredictAsync(request, stream);
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during image prediction");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/detection/predict/batch
        [HttpPost("predict/batch")]
        public async Task<IActionResult> PredictBatch([FromForm] List<IFormFile> imageFiles, [FromQuery] string modelPath = "Models/deepfake_model.zip")
        {
            try
            {
                if (imageFiles == null || !imageFiles.Any())
                    return BadRequest(new { error = "At least one image file is required" });

                // Проверка существования модели
                if (!_modelService.ModelExists(modelPath))
                    return NotFound(new { error = $"Model not found: {modelPath}" });

                var results = new List<PredictionResponse>();
                var request = new PredictionRequest { ModelPath = modelPath };

                foreach (var file in imageFiles)
                {
                    if (file.Length > 0)
                    {
                        using (var stream = file.OpenReadStream())
                        {
                            var result = await _modelService.PredictAsync(request, stream);
                            results.Add(result);
                        }
                    }
                }

                return Ok(new
                {
                    count = results.Count,
                    predictions = results,
                    summary = new
                    {
                        realCount = results.Count(r => r.Prediction == "real"),
                        fakeCount = results.Count(r => r.Prediction == "fake"),
                        processedAt = DateTime.UtcNow
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during batch prediction");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET: api/detection/status
        [HttpGet("status")]
        public IActionResult GetTrainingStatus()
        {
            var status = _modelService.GetTrainingStatus();
            return Ok(status);
        }

        // GET: api/detection/models
        [HttpGet("models")]
        public IActionResult GetAvailableModels()
        {
            var models = _modelService.GetAvailableModels();
            return Ok(new { models, count = models.Count });
        }
    }
}
