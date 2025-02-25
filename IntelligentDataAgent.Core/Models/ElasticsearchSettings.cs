using System.Collections.Generic;

namespace IntelligentDataAgent.Core.Models
{
    public class ElasticsearchSettings
    {
        public List<string> Urls { get; set; } = new List<string>();
        public string DefaultIndex { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool EnableDebugMode { get; set; }
    }
} 