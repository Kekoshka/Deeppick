namespace Deeppick.Models
{
    public class PredictionResponse
    {
        public string Filename { get; set; }
        public string Prediction { get; set; }
        public double RealConfidence { get; set; }
        public double FakeConfidence { get; set; }
        public bool IsFake => Prediction?.ToLower() == "fake";
        public DateTime ProcessedAt { get; set; }
    }
}
