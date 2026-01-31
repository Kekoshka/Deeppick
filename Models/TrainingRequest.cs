namespace Deeppick.Models
{
    public class TrainingRequest
    {
        public string TrainingDataPath { get; set; } = "data/train";
        public string TestDataPath { get; set; } = "data/test";
        public string ModelSavePath { get; set; } = "Models/deepfake_model.zip";
        public int Epochs { get; set; } = 50;
        public int BatchSize { get; set; } = 10;
    }
}
