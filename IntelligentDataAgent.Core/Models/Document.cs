using System;
using System.Collections.Generic;

namespace IntelligentDataAgent.Core.Models
{
    public class Document
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Source { get; set; }
        public DateTime CrawlDate { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
} 