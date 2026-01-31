namespace Deeppick.Models
{
    public class TrainingStatus
    {
        public string Status { get; set; }
        public int Progress { get; set; }
        public string CurrentOperation { get; set; }
        public bool IsComplete { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
