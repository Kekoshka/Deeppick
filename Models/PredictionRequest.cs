namespace Deeppick.Models
{
    public class PredictionRequest
    {
        public IFormFile ImageFile { get; set; }
        public string ModelPath { get; set; } = "Models/deepfake_model.zip";
    }
}
