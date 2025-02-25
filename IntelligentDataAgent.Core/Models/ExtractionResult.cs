namespace IntelligentDataAgent.Core.Models
{
    public class ExtractionResult
    {
        public bool Success { get; set; }
        public string Text { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
        public string ErrorMessage { get; set; }
        public string? Title { get; set; }
        public string DetectedLanguage { get; set; }
    }
} 