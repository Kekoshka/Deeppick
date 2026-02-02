using Deeppick.Services;
using System.Diagnostics;

namespace Deeppick.Interfaces
{
    public interface INoiseExtractService
    {
        int Iterations { get; set; }
        double ErrorScale { get; set; }
        bool EqualizeHistogram { get; set; }
        Task<byte[]> ProcessImageAsync(byte[] imageBytes);
        byte[] ProcessImage(byte[] imageBytes);
        Task<List<byte[]>> ProcessImageRangeAsync(List<byte[]> imageBytes);
}
}
