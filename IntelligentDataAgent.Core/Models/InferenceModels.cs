using System.Collections.Generic;

namespace IntelligentDataAgent.Core.Models
{
    public enum ModelType
    {
        TextClassification,
        EntityExtraction,
        Embedding
    }

    public class InferenceRequest
    {
        public string ModelId { get; set; }
        public ModelType ModelType { get; set; }
        public Dictionary<string, object> Inputs { get; set; } = new Dictionary<string, object>();
    }

    public class InferenceResult
    {
        public string ModelId { get; set; }
        public Dictionary<string, object> Outputs { get; set; } = new Dictionary<string, object>();
        public ModelType ModelType { get; set; }
    }
} 