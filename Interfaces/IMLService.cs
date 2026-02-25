using Deeppick.Models;

namespace Deeppick.Interfaces
{
    public interface IMLService
    {
        Task<TrainingResponse> TrainModelAsync(TrainingRequest request);
        Task<PredictionResponse> PredictAsync(PredictionRequest request, Stream imageStream);
        Task<float> PredictImageAsync(byte[] imageBytes, string modelPath = "Models/deepfake_model.zip");
        TrainingStatus GetTrainingStatus();
        List<string> GetAvailableModels();
        bool ModelExists(string modelPath);
    }
}
