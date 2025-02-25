using IntelligentDataAgent.Core.Models;
using System.Threading.Tasks;

namespace IntelligentDataAgent.Core.Interfaces
{
    public interface IInferenceModelService
    {
        Task<InferenceResult> InferAsync(InferenceRequest request);
        Task LoadModelAsync(string modelId);
        Task UnloadModelAsync(string modelId);
        Task<bool> IsModelLoadedAsync(string modelId);
    }
} 