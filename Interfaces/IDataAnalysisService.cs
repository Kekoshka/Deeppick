using Deeppick.Services;

namespace Deeppick.Interfaces
{
    public interface IDataAnalysisService
    {
        Task<float> AnalyzeVideoDefaultAsync(byte[] videoByte);
        Task<float> AnalyzeImageDefaultAsync(byte[] imageByte);
        Task<float> AnalyzeVideoNoiseAsync(byte[] videoByte);
        Task<float> AnalyzeImageNoiseAsync(byte[] imageByte);

    }
}
