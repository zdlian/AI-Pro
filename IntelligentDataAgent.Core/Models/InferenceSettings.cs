using System.Collections.Generic;

namespace IntelligentDataAgent.Core.Models
{
    public class InferenceSettings
    {
        public string ModelsDirectory { get; set; }
        public int MaxConcurrentInferences { get; set; }
        public List<ModelConfig> Models { get; set; } = new List<ModelConfig>();
        public string? DefaultTextClassificationModel { get; internal set; }
        public string? DefaultEntityExtractionModel { get; internal set; }
        public string? DefaultEmbeddingModel { get; internal set; }
    }

    public class ModelConfig
    {
        public string Id { get; set; }
        public string Path { get; set; }
        public string Type { get; set; }
        public string ExecutionProvider { get; set; }
        public int MaxBatchSize { get; set; }
    }
} 