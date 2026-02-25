namespace Deeppick.Models
{
    public class TrainingResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public double Accuracy { get; set; }
        public double LogLoss { get; set; }
        public string ModelPath { get; set; }
        public DateTime TrainingCompleted { get; set; }

    }
}
